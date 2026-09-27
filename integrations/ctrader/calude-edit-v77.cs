// ============================================================================
// CFIP-PRO cTrader — v77 Clean Architecture Foundation
// Phase: 3 · Time, MTF and data pipeline
//
// This version intentionally does NOT copy the v73 monolith.
// v73 remains the frozen behavioral/reference baseline.
// v77 now extends the type/ownership boundaries with the first normalized market model.
//
// Architectural invariants:
//   1. Analysis is side-effect free.
//   2. Decision != execution.
//   3. TradePlan != broker state.
//   4. BrokerGateway is the only broker-mutation boundary.
//   5. LifecycleManager is the only lifecycle-state authority.
//   6. UI/presentation is downstream and has no trading authority.
//   7. Entry terminology is explicit:
//        IdealEntry, EntryZone, Trigger, RequestedEntry,
//        ActualFill, Invalidation.
//   8. BUY=+1, SELL=-1, WAIT=0.
//   9. Execution idempotency is explicit.
//  10. Fallbacks/provenance are explicit; no silent semantic fallback.
//
// Migration rule:
//   Build new behavior behind these contracts in later phases.
//   Do not reintroduce v73 shared mutable state simply to make a migration
//   shortcut compile.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;

namespace cAlgo
{
    // ------------------------------------------------------------------------
    // Canonical scalar semantics
    // ------------------------------------------------------------------------

    public enum CFIPClean77Direction
    {
        Sell = -1,
        Wait = 0,
        Buy = 1
    }

    public static class CFIPClean77DirectionRules
    {
        public static bool IsDirectional(CFIPClean77Direction direction)
        {
            return direction != CFIPClean77Direction.Wait;
        }

        public static CFIPClean77Direction Opposite(CFIPClean77Direction direction)
        {
            if (direction == CFIPClean77Direction.Buy)
                return CFIPClean77Direction.Sell;

            if (direction == CFIPClean77Direction.Sell)
                return CFIPClean77Direction.Buy;

            return CFIPClean77Direction.Wait;
        }

        public static bool IsProtectiveMove(
            CFIPClean77Direction direction,
            double currentStop,
            double candidateStop)
        {
            if (!IsDirectional(direction) ||
                currentStop <= 0 ||
                candidateStop <= 0)
                return false;

            return direction == CFIPClean77Direction.Buy
                ? candidateStop > currentStop
                : candidateStop < currentStop;
        }
    }

    public enum CFIPClean77DecisionPolicyMode
    {
        Confirmed = 0,
        Soft = 1,
        Aggressive = 2,
        Pending = 3
    }

    public enum CFIPClean77ExecutionKind
    {
        None = 0,
        Market = 1,
        Stop = 2,
        Limit = 3
    }

    public enum CFIPClean77EntryMode
    {
        None = 0,
        RetestMarket = 1,
        BreakoutMarket = 2,
        ContinuationStop = 3,
        ReversalLimit = 4
    }

    public enum CFIPClean77LifecycleState
    {
        Flat = 0,
        SignalDetected = 1,
        PlanReady = 2,
        ExecutionReady = 3,
        PendingOrder = 4,
        LivePosition = 5,
        ExitRequested = 6,
        RecoveryRequired = 7,
        Closed = 8,
        Rejected = 9,
        Error = 10
    }

    public enum CFIPClean77BlockReason
    {
        None = 0,
        DataIncomplete = 1,
        NoDirection = 2,
        ConfidenceTooLow = 3,
        EvidenceInsufficient = 4,
        MtfDisagreement = 5,
        StructureInvalid = 6,
        EntryInvalid = 7,
        RiskInvalid = 8,
        TargetInvalid = 9,
        SpreadBlocked = 10,
        SessionBlocked = 11,
        NewsBlocked = 12,
        VolatilityBlocked = 13,
        DailyLossBlocked = 14,
        ExistingExposureBlocked = 15,
        DuplicateExecutionBlocked = 16,
        BrokerUnavailable = 17,
        BrokerConstraintsBlocked = 18,
        CooldownBlocked = 19,
        PolicyBlocked = 20,
        Unknown = 99
    }

    public enum CFIPClean77FallbackKind
    {
        None = 0,
        Structural = 1,
        HigherTimeframe = 2,
        ExecutionFrame = 3,
        Atr = 4,
        Synthetic = 5
    }

    public enum CFIPClean77TargetStage
    {
        TP1 = 1,
        TP2 = 2,
        TP3 = 3,
        TP4 = 4
    }

    public enum CFIPClean77ProtectionState
    {
        Unknown = 0,
        Unprotected = 1,
        PartiallyProtected = 2,
        FullyProtected = 3,
        RecoveryRequired = 4
    }

    public enum CFIPClean77BrokerObjectKind
    {
        None = 0,
        Position = 1,
        PendingOrder = 2
    }

    public enum CFIPClean77TargetState
    {
        Proposed = 0,
        Active = 1,
        Hit = 2,
        Invalidated = 3,
        Consumed = 4
    }

    // ------------------------------------------------------------------------
    // Provenance
    // ------------------------------------------------------------------------

    public sealed class CFIPClean77Provenance
    {
        public string Source { get; private set; }
        public string Rule { get; private set; }
        public CFIPClean77FallbackKind Fallback { get; private set; }
        public string Detail { get; private set; }

        public CFIPClean77Provenance(
            string source,
            string rule,
            CFIPClean77FallbackKind fallback,
            string detail)
        {
            Source = source ?? string.Empty;
            Rule = rule ?? string.Empty;
            Fallback = fallback;
            Detail = detail ?? string.Empty;
        }

        public static CFIPClean77Provenance Direct(
            string source,
            string rule)
        {
            return new CFIPClean77Provenance(
                source,
                rule,
                CFIPClean77FallbackKind.None,
                string.Empty);
        }

        public static CFIPClean77Provenance FallbackFrom(
            string source,
            string rule,
            CFIPClean77FallbackKind fallback,
            string detail)
        {
            return new CFIPClean77Provenance(
                source,
                rule,
                fallback,
                detail);
        }
    }

    // ------------------------------------------------------------------------
    // Price / zone primitives
    // ------------------------------------------------------------------------

    public sealed class CFIPClean77PriceLevel
    {
        public double Price { get; private set; }
        public string Name { get; private set; }
        public CFIPClean77Provenance Provenance { get; private set; }

        public CFIPClean77PriceLevel(
            double price,
            string name,
            CFIPClean77Provenance provenance)
        {
            if (price <= 0)
                throw new ArgumentOutOfRangeException("price");

            Price = price;
            Name = name ?? string.Empty;
            Provenance = provenance ??
                         CFIPClean77Provenance.Direct(
                             "UNKNOWN",
                             "UNSPECIFIED");
        }
    }

    public sealed class CFIPClean77PriceZone
    {
        public double Lower { get; private set; }
        public double Upper { get; private set; }
        public string Name { get; private set; }
        public CFIPClean77Provenance Provenance { get; private set; }

        public CFIPClean77PriceZone(
            double lower,
            double upper,
            string name,
            CFIPClean77Provenance provenance)
        {
            if (lower <= 0 || upper <= 0 || upper < lower)
                throw new ArgumentException("Invalid price zone.");

            Lower = lower;
            Upper = upper;
            Name = name ?? string.Empty;
            Provenance = provenance ??
                         CFIPClean77Provenance.Direct(
                             "UNKNOWN",
                             "UNSPECIFIED");
        }

        public bool Contains(double price)
        {
            return price >= Lower && price <= Upper;
        }

        public double Midpoint
        {
            get { return (Lower + Upper) / 2.0; }
        }
    }

    // ------------------------------------------------------------------------
    // Canonical entry model
    // ------------------------------------------------------------------------

    public sealed class CFIPClean77EntryModel
    {
        public CFIPClean77Direction Direction { get; private set; }

        // Preferred price inside the structural execution area.
        public CFIPClean77PriceLevel IdealEntry { get; private set; }

        // Allowed structural retest region.
        public CFIPClean77PriceZone EntryZone { get; private set; }

        // Structural activation threshold for breakout/continuation.
        public CFIPClean77PriceLevel Trigger { get; private set; }

        // Exact strategy/broker protection boundary.
        public CFIPClean77PriceLevel Invalidation { get; private set; }

        public CFIPClean77EntryModel(
            CFIPClean77Direction direction,
            CFIPClean77PriceLevel idealEntry,
            CFIPClean77PriceZone entryZone,
            CFIPClean77PriceLevel trigger,
            CFIPClean77PriceLevel invalidation)
        {
            if (!CFIPClean77DirectionRules.IsDirectional(direction))
                throw new ArgumentException("A directional entry model is required.");

            Direction = direction;
            IdealEntry = idealEntry;
            EntryZone = entryZone;
            Trigger = trigger;
            Invalidation = invalidation;
        }
    }

    // ------------------------------------------------------------------------
    // Canonical targets
    // ------------------------------------------------------------------------

    public sealed class CFIPClean77TargetLevel
    {
        public CFIPClean77TargetStage Stage { get; private set; }
        public CFIPClean77PriceLevel Level { get; private set; }
        public CFIPClean77TargetState State { get; private set; }
        public int Quality { get; private set; }

        public CFIPClean77TargetLevel(
            CFIPClean77TargetStage stage,
            CFIPClean77PriceLevel level,
            int quality,
            CFIPClean77TargetState state)
        {
            Stage = stage;
            Level = level;
            Quality = Math.Max(0, Math.Min(100, quality));
            State = state;
        }
    }

    public sealed class CFIPClean77TargetLadder
    {
        private readonly ReadOnlyCollection<CFIPClean77TargetLevel> _levels;

        public IReadOnlyList<CFIPClean77TargetLevel> Levels
        {
            get { return _levels; }
        }

        public CFIPClean77TargetLadder(
            IList<CFIPClean77TargetLevel> levels)
        {
            if (levels == null)
                throw new ArgumentNullException("levels");

            var copy =
                new List<CFIPClean77TargetLevel>(
                    levels);

            copy.Sort(
                delegate (
                    CFIPClean77TargetLevel left,
                    CFIPClean77TargetLevel right)
                {
                    return left.Stage.CompareTo(right.Stage);
                });

            _levels =
                new ReadOnlyCollection<CFIPClean77TargetLevel>(
                    copy);
        }

        public bool ValidateForDirection(
            CFIPClean77Direction direction)
        {
            if (!CFIPClean77DirectionRules.IsDirectional(direction) ||
                _levels.Count == 0)
                return false;

            for (int i = 1; i < _levels.Count; i++)
            {
                double previous =
                    _levels[i - 1].Level.Price;

                double current =
                    _levels[i].Level.Price;

                if (direction == CFIPClean77Direction.Buy &&
                    current <= previous)
                    return false;

                if (direction == CFIPClean77Direction.Sell &&
                    current >= previous)
                    return false;
            }

            return true;
        }

        public CFIPClean77TargetLevel Find(
            CFIPClean77TargetStage stage)
        {
            for (int i = 0; i < _levels.Count; i++)
            {
                if (_levels[i].Stage == stage)
                    return _levels[i];
            }

            return null;
        }
    }

    // ------------------------------------------------------------------------
    // Runtime / time snapshots
    // ------------------------------------------------------------------------

    public sealed class CFIPClean77BrokerConstraints
    {
        public double MinVolumeInUnits { get; private set; }
        public double VolumeStepInUnits { get; private set; }
        public double MinStopDistancePips { get; private set; }
        public double MinTakeProfitDistancePips { get; private set; }

        public CFIPClean77BrokerConstraints(
            double minVolumeInUnits,
            double volumeStepInUnits,
            double minStopDistancePips,
            double minTakeProfitDistancePips)
        {
            MinVolumeInUnits = Math.Max(0, minVolumeInUnits);
            VolumeStepInUnits = Math.Max(0, volumeStepInUnits);
            MinStopDistancePips = Math.Max(0, minStopDistancePips);
            MinTakeProfitDistancePips =
                Math.Max(0, minTakeProfitDistancePips);
        }
    }

    public sealed class CFIPClean77RuntimeSnapshot
    {
        public DateTime ServerUtc { get; private set; }
        public string Symbol { get; private set; }
        public double Bid { get; private set; }
        public double Ask { get; private set; }
        public double SpreadPips { get; private set; }
        public bool SymbolTradingEnabled { get; private set; }
        public double Equity { get; private set; }
        public double FreeMargin { get; private set; }
        public CFIPClean77BrokerConstraints BrokerConstraints { get; private set; }
        public int ManagedPositionCount { get; private set; }
        public int ManagedPendingOrderCount { get; private set; }

        public CFIPClean77RuntimeSnapshot(
            DateTime serverUtc,
            string symbol,
            double bid,
            double ask,
            double spreadPips,
            bool symbolTradingEnabled,
            double equity,
            double freeMargin,
            CFIPClean77BrokerConstraints brokerConstraints,
            int managedPositionCount,
            int managedPendingOrderCount)
        {
            if (bid < 0 || ask < 0)
                throw new ArgumentOutOfRangeException("bid");

            ServerUtc = serverUtc;
            Symbol = symbol ?? string.Empty;
            Bid = bid;
            Ask = ask;
            SpreadPips = Math.Max(0, spreadPips);
            SymbolTradingEnabled = symbolTradingEnabled;
            Equity = equity;
            FreeMargin = freeMargin;
            BrokerConstraints =
                brokerConstraints ??
                throw new ArgumentNullException("brokerConstraints");
            ManagedPositionCount =
                Math.Max(0, managedPositionCount);
            ManagedPendingOrderCount =
                Math.Max(0, managedPendingOrderCount);
        }
    }

    public enum CFIPClean77MtfDataStatus
    {
        Ready = 0,
        PrimaryHistoryInsufficient = 1,
        MissingTimeframeData = 2,
        InvalidReference = 3,
        StaleReference = 4
    }

    public sealed class CFIPClean77MtfBarSnapshot
    {
        public string Timeframe { get; private set; }
        public int ClosedIndex { get; private set; }
        public DateTime BarOpenUtc { get; private set; }
        public DateTime NextBarOpenUtc { get; private set; }
        public bool IsAvailable { get; private set; }
        public bool IsFullyClosedAtReference { get; private set; }
        public bool HasMinimumHistory { get; private set; }

        public CFIPClean77MtfBarSnapshot(
            string timeframe,
            int closedIndex,
            DateTime barOpenUtc,
            DateTime nextBarOpenUtc,
            bool isAvailable,
            bool isFullyClosedAtReference,
            bool hasMinimumHistory)
        {
            Timeframe = timeframe ?? string.Empty;
            ClosedIndex = closedIndex;
            BarOpenUtc = barOpenUtc;
            NextBarOpenUtc = nextBarOpenUtc;
            IsAvailable = isAvailable;
            IsFullyClosedAtReference = isFullyClosedAtReference;
            HasMinimumHistory = hasMinimumHistory;
        }

        public static CFIPClean77MtfBarSnapshot Missing(
            string timeframe)
        {
            return new CFIPClean77MtfBarSnapshot(
                timeframe,
                -1,
                DateTime.MinValue,
                DateTime.MinValue,
                false,
                false,
                false);
        }
    }

    public sealed class CFIPClean77MtfSnapshot
    {
        public DateTime ServerUtc { get; private set; }
        public DateTime ReferenceUtc { get; private set; }
        public DateTime UserLocalTime { get; private set; }
        public TimeSpan ReferenceAge { get; private set; }
        public string ChartTimeframe { get; private set; }

        public CFIPClean77MtfBarSnapshot Chart { get; private set; }
        public CFIPClean77MtfBarSnapshot M1 { get; private set; }
        public CFIPClean77MtfBarSnapshot M5 { get; private set; }
        public CFIPClean77MtfBarSnapshot M15 { get; private set; }
        public CFIPClean77MtfBarSnapshot M30 { get; private set; }
        public CFIPClean77MtfBarSnapshot H1 { get; private set; }
        public CFIPClean77MtfBarSnapshot H4 { get; private set; }
        public CFIPClean77MtfBarSnapshot D1 { get; private set; }
        public CFIPClean77MtfBarSnapshot W1 { get; private set; }

        public CFIPClean77MtfDataStatus DataStatus { get; private set; }

        public bool IsReferenceValid
        {
            get
            {
                return ReferenceUtc != DateTime.MinValue;
            }
        }

        public bool IsReferenceFresh
        {
            get
            {
                return
                    IsReferenceValid &&
                    ReferenceAge >= TimeSpan.Zero &&
                    ReferenceAge <= TimeSpan.FromMinutes(10);
            }
        }

        public bool IsPrimaryDecisionReady
        {
            get
            {
                return
                    IsReferenceValid &&
                    IsReferenceFresh &&
                    M5 != null &&
                    M15 != null &&
                    M30 != null &&
                    H1 != null &&
                    H4 != null &&
                    M5.IsAvailable &&
                    M15.IsAvailable &&
                    M30.IsAvailable &&
                    H1.IsAvailable &&
                    H4.IsAvailable &&
                    M5.IsFullyClosedAtReference &&
                    M15.IsFullyClosedAtReference &&
                    M30.IsFullyClosedAtReference &&
                    H1.IsFullyClosedAtReference &&
                    H4.IsFullyClosedAtReference &&
                    M5.HasMinimumHistory &&
                    M15.HasMinimumHistory &&
                    M30.HasMinimumHistory &&
                    H1.HasMinimumHistory &&
                    H4.HasMinimumHistory;
            }
        }

        public bool IsAllAvailableTimeframesClosed
        {
            get
            {
                return
                    M1 != null &&
                    M5 != null &&
                    M15 != null &&
                    M30 != null &&
                    H1 != null &&
                    H4 != null &&
                    D1 != null &&
                    W1 != null &&
                    M1.IsAvailable &&
                    M5.IsAvailable &&
                    M15.IsAvailable &&
                    M30.IsAvailable &&
                    H1.IsAvailable &&
                    H4.IsAvailable &&
                    D1.IsAvailable &&
                    W1.IsAvailable &&
                    M1.IsFullyClosedAtReference &&
                    M5.IsFullyClosedAtReference &&
                    M15.IsFullyClosedAtReference &&
                    M30.IsFullyClosedAtReference &&
                    H1.IsFullyClosedAtReference &&
                    H4.IsFullyClosedAtReference &&
                    D1.IsFullyClosedAtReference &&
                    W1.IsFullyClosedAtReference;
            }
        }

        public CFIPClean77MtfSnapshot(
            DateTime serverUtc,
            DateTime referenceUtc,
            DateTime userLocalTime,
            TimeSpan referenceAge,
            string chartTimeframe,
            CFIPClean77MtfBarSnapshot chart,
            CFIPClean77MtfBarSnapshot m1,
            CFIPClean77MtfBarSnapshot m5,
            CFIPClean77MtfBarSnapshot m15,
            CFIPClean77MtfBarSnapshot m30,
            CFIPClean77MtfBarSnapshot h1,
            CFIPClean77MtfBarSnapshot h4,
            CFIPClean77MtfBarSnapshot d1,
            CFIPClean77MtfBarSnapshot w1,
            CFIPClean77MtfDataStatus dataStatus)
        {
            ServerUtc = serverUtc;
            ReferenceUtc = referenceUtc;
            UserLocalTime = userLocalTime;
            ReferenceAge =
                referenceAge < TimeSpan.Zero
                    ? TimeSpan.Zero
                    : referenceAge;
            ChartTimeframe = chartTimeframe ?? string.Empty;
            Chart = chart ?? CFIPClean77MtfBarSnapshot.Missing("CHART");
            M1 = m1 ?? CFIPClean77MtfBarSnapshot.Missing("M1");
            M5 = m5 ?? CFIPClean77MtfBarSnapshot.Missing("M5");
            M15 = m15 ?? CFIPClean77MtfBarSnapshot.Missing("M15");
            M30 = m30 ?? CFIPClean77MtfBarSnapshot.Missing("M30");
            H1 = h1 ?? CFIPClean77MtfBarSnapshot.Missing("H1");
            H4 = h4 ?? CFIPClean77MtfBarSnapshot.Missing("H4");
            D1 = d1 ?? CFIPClean77MtfBarSnapshot.Missing("D1");
            W1 = w1 ?? CFIPClean77MtfBarSnapshot.Missing("W1");
            DataStatus = dataStatus;
        }
    }

    public static class CFIPClean77MtfSnapshotBuilder
    {
        public static DateTime ResolveM5Reference(
            Bars m5Bars,
            DateTime serverUtc)
        {
            if (m5Bars == null ||
                m5Bars.Count < 2 ||
                serverUtc == DateTime.MinValue)
                return DateTime.MinValue;

            int last =
                m5Bars.Count - 1;

            DateTime reference =
                m5Bars.OpenTimes[last];

            if (reference > serverUtc)
                return DateTime.MinValue;

            return reference;
        }

        public static CFIPClean77MtfSnapshot Build(
            DateTime serverUtc,
            DateTime userLocalTime,
            Bars chartBars,
            string chartTimeframe,
            Bars m1Bars,
            Bars m5Bars,
            Bars m15Bars,
            Bars m30Bars,
            Bars h1Bars,
            Bars h4Bars,
            Bars d1Bars,
            Bars w1Bars,
            int minimumHistory)
        {
            DateTime reference =
                ResolveM5Reference(
                    m5Bars,
                    serverUtc);

            if (reference == DateTime.MinValue)
            {
                return new CFIPClean77MtfSnapshot(
                    serverUtc,
                    DateTime.MinValue,
                    userLocalTime,
                    TimeSpan.Zero,
                    chartTimeframe,
                    CFIPClean77MtfBarSnapshot.Missing(
                        "CHART"),
                    CFIPClean77MtfBarSnapshot.Missing("M1"),
                    CFIPClean77MtfBarSnapshot.Missing("M5"),
                    CFIPClean77MtfBarSnapshot.Missing("M15"),
                    CFIPClean77MtfBarSnapshot.Missing("M30"),
                    CFIPClean77MtfBarSnapshot.Missing("H1"),
                    CFIPClean77MtfBarSnapshot.Missing("H4"),
                    CFIPClean77MtfBarSnapshot.Missing("D1"),
                    CFIPClean77MtfBarSnapshot.Missing("W1"),
                    CFIPClean77MtfDataStatus.InvalidReference);
            }

            TimeSpan age =
                serverUtc >= reference
                    ? serverUtc - reference
                    : TimeSpan.Zero;

            CFIPClean77MtfBarSnapshot chart =
                ResolveClosedBar(
                    chartBars,
                    reference,
                    chartTimeframe,
                    minimumHistory);

            CFIPClean77MtfBarSnapshot m1 =
                ResolveClosedBar(
                    m1Bars,
                    reference,
                    "M1",
                    minimumHistory);

            CFIPClean77MtfBarSnapshot m5 =
                ResolveClosedBar(
                    m5Bars,
                    reference,
                    "M5",
                    minimumHistory);

            CFIPClean77MtfBarSnapshot m15 =
                ResolveClosedBar(
                    m15Bars,
                    reference,
                    "M15",
                    minimumHistory);

            CFIPClean77MtfBarSnapshot m30 =
                ResolveClosedBar(
                    m30Bars,
                    reference,
                    "M30",
                    minimumHistory);

            CFIPClean77MtfBarSnapshot h1 =
                ResolveClosedBar(
                    h1Bars,
                    reference,
                    "H1",
                    minimumHistory);

            CFIPClean77MtfBarSnapshot h4 =
                ResolveClosedBar(
                    h4Bars,
                    reference,
                    "H4",
                    minimumHistory);

            CFIPClean77MtfBarSnapshot d1 =
                ResolveClosedBar(
                    d1Bars,
                    reference,
                    "D1",
                    minimumHistory);

            CFIPClean77MtfBarSnapshot w1 =
                ResolveClosedBar(
                    w1Bars,
                    reference,
                    "W1",
                    minimumHistory);

            bool primaryHistory =
                HasPrimaryHistory(
                    m5,
                    m15,
                    m30,
                    h1,
                    h4,
                    minimumHistory);

            bool allTimeframesAvailable =
                m1.IsAvailable &&
                m5.IsAvailable &&
                m15.IsAvailable &&
                m30.IsAvailable &&
                h1.IsAvailable &&
                h4.IsAvailable &&
                d1.IsAvailable &&
                w1.IsAvailable;

            CFIPClean77MtfDataStatus status;

            if (age > TimeSpan.FromMinutes(10))
                status =
                    CFIPClean77MtfDataStatus.StaleReference;
            else if (!primaryHistory)
                status =
                    CFIPClean77MtfDataStatus.PrimaryHistoryInsufficient;
            else if (!m1.IsAvailable ||
                     !m5.IsAvailable ||
                     !m15.IsAvailable ||
                     !m30.IsAvailable ||
                     !h1.IsAvailable ||
                     !h4.IsAvailable)
                status =
                    CFIPClean77MtfDataStatus.MissingTimeframeData;
            else
                status =
                    CFIPClean77MtfDataStatus.Ready;

            return new CFIPClean77MtfSnapshot(
                serverUtc,
                reference,
                userLocalTime,
                age,
                chartTimeframe,
                chart,
                m1,
                m5,
                m15,
                m30,
                h1,
                h4,
                d1,
                w1,
                status);
        }

        public static bool HasPrimaryHistory(
            CFIPClean77MtfBarSnapshot m5,
            CFIPClean77MtfBarSnapshot m15,
            CFIPClean77MtfBarSnapshot m30,
            CFIPClean77MtfBarSnapshot h1,
            CFIPClean77MtfBarSnapshot h4,
            int minimumHistory)
        {
            int minimum =
                Math.Max(
                    2,
                    minimumHistory);

            return
                m5 != null &&
                m15 != null &&
                m30 != null &&
                h1 != null &&
                h4 != null &&
                m5.ClosedIndex >= minimum &&
                m15.ClosedIndex >= minimum &&
                m30.ClosedIndex >= minimum &&
                h1.ClosedIndex >= minimum &&
                h4.ClosedIndex >= minimum;
        }

        public static CFIPClean77MtfBarSnapshot ResolveClosedBar(
            Bars bars,
            DateTime reference,
            string timeframe,
            int minimumHistory)
        {
            if (bars == null ||
                bars.Count < 2 ||
                reference == DateTime.MinValue ||
                reference < bars.OpenTimes[0])
                return
                    CFIPClean77MtfBarSnapshot.Missing(
                        timeframe);

            int probe =
                bars.OpenTimes.GetIndexByTime(
                    reference);

            if (probe < 0)
                probe = bars.Count - 1;

            probe =
                Math.Max(
                    0,
                    Math.Min(
                        probe,
                        bars.Count - 1));

            // The final series item is treated as potentially forming.
            // Never return it as a closed analysis bar.
            if (probe == bars.Count - 1)
                probe--;

            for (int i = probe; i >= 0; i--)
            {
                DateTime open =
                    bars.OpenTimes[i];

                if (open >= reference)
                    continue;

                if (i + 1 >= bars.Count)
                    continue;

                DateTime nextOpen =
                    bars.OpenTimes[i + 1];

                if (nextOpen > reference)
                    continue;

                bool minimum =
                    i >= Math.Max(
                        2,
                        minimumHistory);

                return
                    new CFIPClean77MtfBarSnapshot(
                        timeframe,
                        i,
                        open,
                        nextOpen,
                        true,
                        true,
                        minimum);
            }

            return
                CFIPClean77MtfBarSnapshot.Missing(
                    timeframe);
        }

        public static bool IsCoherent(
            CFIPClean77MtfSnapshot snapshot)
        {
            if (snapshot == null ||
                !snapshot.IsReferenceValid ||
                !snapshot.IsReferenceFresh)
                return false;

            CFIPClean77MtfBarSnapshot[] items =
            {
                snapshot.M1,
                snapshot.M5,
                snapshot.M15,
                snapshot.M30,
                snapshot.H1,
                snapshot.H4,
                snapshot.D1,
                snapshot.W1
            };

            if (!snapshot.IsPrimaryDecisionReady)
                return false;

            for (int i = 0; i < items.Length; i++)
            {
                CFIPClean77MtfBarSnapshot item =
                    items[i];

                // D1/W1 may legitimately be unavailable when history is
                // insufficient. Missing optional data is not temporal leakage.
                if (item == null ||
                    !item.IsAvailable)
                    continue;

                if (!item.IsFullyClosedAtReference ||
                    item.ClosedIndex < 0 ||
                    item.BarOpenUtc >= snapshot.ReferenceUtc ||
                    item.NextBarOpenUtc >
                    snapshot.ReferenceUtc)
                    return false;
            }

            return true;
        }
    }

    // ------------------------------------------------------------------------
    // Market model
    // ------------------------------------------------------------------------

    public enum CFIPClean77Regime
    {
        Unknown = 0,
        Trend = 1,
        Expansion = 2,
        Compression = 3,
        Range = 4,
        Transition = 5
    }

    public enum CFIPClean77MarketFeature
    {
        Trend = 0,
        Momentum = 1,
        Rsi = 2,
        Dmi = 3,
        EmaSlope = 4,
        Rejection = 5,
        VolumeExpansion = 6,
        MacdBias = 7,
        VwapBias = 8,
        HealthyVolatility = 9
    }

    public sealed class CFIPClean77FeatureEvidence
    {
        public CFIPClean77MarketFeature Feature { get; private set; }
        public CFIPClean77Direction Direction { get; private set; }
        public double Value { get; private set; }
        public int Weight { get; private set; }
        public bool Triggered { get; private set; }
        public bool CountsAsEvidence { get; private set; }
        public bool CountsAsGate { get; private set; }
        public CFIPClean77Provenance Provenance { get; private set; }

        public CFIPClean77FeatureEvidence(
            CFIPClean77MarketFeature feature,
            CFIPClean77Direction direction,
            double value,
            int weight,
            bool triggered,
            bool countsAsEvidence,
            bool countsAsGate,
            CFIPClean77Provenance provenance)
        {
            Feature = feature;
            Direction = direction;
            Value = Clamp01(value);
            Weight = Math.Max(0, weight);
            Triggered = triggered;
            CountsAsEvidence = countsAsEvidence;
            CountsAsGate = countsAsGate;
            Provenance =
                provenance ??
                CFIPClean77Provenance.Direct(
                    "MARKET_MODEL",
                    feature.ToString());
        }

        private static double Clamp01(double value)
        {
            if (double.IsNaN(value) ||
                double.IsInfinity(value))
                return 0;

            return
                Math.Max(
                    0,
                    Math.Min(
                        1,
                        value));
        }
    }

    public sealed class CFIPClean77MarketFrame
    {
        private readonly ReadOnlyCollection<CFIPClean77FeatureEvidence> _features;

        public string Timeframe { get; private set; }
        public DateTime ClosedBarTimeUtc { get; private set; }

        public double Open { get; private set; }
        public double High { get; private set; }
        public double Low { get; private set; }
        public double Close { get; private set; }

        // Raw indicator measurements.
        public double Atr { get; private set; }
        public double AtrRatio { get; private set; }
        public double Rsi { get; private set; }
        public double Adx { get; private set; }
        public double DmiBias { get; private set; }
        public double EmaFast { get; private set; }
        public double EmaSlow { get; private set; }
        public double EmaSpreadAtr { get; private set; }
        public double EmaSlopeAtr { get; private set; }
        public double MomentumAtr { get; private set; }
        public double MacdHistogram { get; private set; }
        public double Vwap { get; private set; }
        public double VolumeRatio { get; private set; }
        public double BodyAtr { get; private set; }
        public double RangeAtr { get; private set; }

        // Typed interpretation; this is market bias, not final trade direction.
        public CFIPClean77Direction BiasDirection { get; private set; }
        public int BiasStrength { get; private set; }
        public int BullScore { get; private set; }
        public int BearScore { get; private set; }
        public int BullScoreNormalized { get; private set; }
        public int BearScoreNormalized { get; private set; }
        public int IndependentEvidence { get; private set; }
        public int EnabledEvidenceFeatures { get; private set; }

        public CFIPClean77Regime Regime { get; private set; }
        public int RegimeQuality { get; private set; }
        public bool Choppy { get; private set; }
        public bool HealthyVolatility { get; private set; }
        public bool DataValid { get; private set; }

        public IReadOnlyList<CFIPClean77FeatureEvidence> Features
        {
            get { return _features; }
        }

        public int DirectionScore
        {
            get
            {
                return
                    BiasDirection == CFIPClean77Direction.Buy
                        ? BiasStrength
                        : BiasDirection == CFIPClean77Direction.Sell
                            ? -BiasStrength
                            : 0;
            }
        }

        public CFIPClean77MarketFrame(
            string timeframe,
            DateTime closedBarTimeUtc,
            double open,
            double high,
            double low,
            double close,
            double atr,
            double atrRatio,
            double rsi,
            double adx,
            double dmiBias,
            double emaFast,
            double emaSlow,
            double emaSpreadAtr,
            double emaSlopeAtr,
            double momentumAtr,
            double macdHistogram,
            double vwap,
            double volumeRatio,
            double bodyAtr,
            double rangeAtr,
            CFIPClean77Direction biasDirection,
            int biasStrength,
            int bullScore,
            int bearScore,
            int bullScoreNormalized,
            int bearScoreNormalized,
            int independentEvidence,
            int enabledEvidenceFeatures,
            CFIPClean77Regime regime,
            int regimeQuality,
            bool choppy,
            bool healthyVolatility,
            bool dataValid,
            IList<CFIPClean77FeatureEvidence> features)
        {
            Timeframe = timeframe ?? string.Empty;
            ClosedBarTimeUtc = closedBarTimeUtc;
            Open = open;
            High = high;
            Low = low;
            Close = close;
            Atr = Math.Max(0, atr);
            AtrRatio = Math.Max(0, atrRatio);
            Rsi = Math.Max(0, Math.Min(100, rsi));
            Adx = Math.Max(0, Math.Min(100, adx));
            DmiBias =
                Math.Max(
                    -1,
                    Math.Min(
                        1,
                        dmiBias));
            EmaFast = emaFast;
            EmaSlow = emaSlow;
            EmaSpreadAtr = emaSpreadAtr;
            EmaSlopeAtr = emaSlopeAtr;
            MomentumAtr = momentumAtr;
            MacdHistogram = macdHistogram;
            Vwap = Math.Max(0, vwap);
            VolumeRatio = Math.Max(0, volumeRatio);
            BodyAtr = Math.Max(0, bodyAtr);
            RangeAtr = Math.Max(0, rangeAtr);
            BiasDirection = biasDirection;
            BiasStrength = Math.Max(0, Math.Min(100, biasStrength));
            BullScore = Math.Max(0, bullScore);
            BearScore = Math.Max(0, bearScore);
            BullScoreNormalized =
                Math.Max(
                    0,
                    Math.Min(
                        100,
                        bullScoreNormalized));
            BearScoreNormalized =
                Math.Max(
                    0,
                    Math.Min(
                        100,
                        bearScoreNormalized));
            IndependentEvidence = Math.Max(0, independentEvidence);
            EnabledEvidenceFeatures =
                Math.Max(
                    0,
                    enabledEvidenceFeatures);
            Regime = regime;
            RegimeQuality =
                Math.Max(
                    0,
                    Math.Min(
                        100,
                        regimeQuality));
            Choppy = choppy;
            HealthyVolatility = healthyVolatility;
            DataValid = dataValid;
            _features =
                new ReadOnlyCollection<CFIPClean77FeatureEvidence>(
                    new List<CFIPClean77FeatureEvidence>(
                        features ??
                        new List<CFIPClean77FeatureEvidence>()));
        }
    }

    public sealed class CFIPClean77MarketModel
    {
        private readonly ReadOnlyCollection<CFIPClean77MarketFrame> _frames;

        public DateTime ReferenceUtc { get; private set; }
        public bool IsCoherent { get; private set; }
        public bool IsPrimaryReady { get; private set; }
        public string DataStatus { get; private set; }

        public IReadOnlyList<CFIPClean77MarketFrame> Frames
        {
            get { return _frames; }
        }

        public CFIPClean77MarketFrame M5
        {
            get { return FindFrame("M5"); }
        }

        public CFIPClean77MarketFrame M15
        {
            get { return FindFrame("M15"); }
        }

        public CFIPClean77MarketFrame FindFrame(string timeframe)
        {
            string key = timeframe ?? string.Empty;

            for (int i = 0; i < _frames.Count; i++)
            {
                if (string.Equals(
                        _frames[i].Timeframe,
                        key,
                        StringComparison.OrdinalIgnoreCase))
                    return _frames[i];
            }

            return null;
        }

        public CFIPClean77MarketModel(
            DateTime referenceUtc,
            bool isCoherent,
            bool isPrimaryReady,
            string dataStatus,
            IList<CFIPClean77MarketFrame> frames)
        {
            ReferenceUtc = referenceUtc;
            IsCoherent = isCoherent;
            IsPrimaryReady = isPrimaryReady;
            DataStatus = dataStatus ?? string.Empty;
            _frames =
                new ReadOnlyCollection<CFIPClean77MarketFrame>(
                    new List<CFIPClean77MarketFrame>(
                        frames ??
                        new List<CFIPClean77MarketFrame>()));
        }
    }

    public sealed class CFIPClean77NativeIndicatorSet
    {
        public Bars Bars { get; private set; }
        public ExponentialMovingAverage Fast { get; private set; }
        public ExponentialMovingAverage Slow { get; private set; }
        public AverageTrueRange Atr { get; private set; }
        public RelativeStrengthIndex Rsi { get; private set; }
        public DirectionalMovementSystem Dms { get; private set; }
        public ExponentialMovingAverage MacdFast { get; private set; }
        public ExponentialMovingAverage MacdSlow { get; private set; }

        public CFIPClean77NativeIndicatorSet(
            Bars bars,
            ExponentialMovingAverage fast,
            ExponentialMovingAverage slow,
            AverageTrueRange atr,
            RelativeStrengthIndex rsi,
            DirectionalMovementSystem dms,
            ExponentialMovingAverage macdFast,
            ExponentialMovingAverage macdSlow)
        {
            Bars = bars;
            Fast = fast;
            Slow = slow;
            Atr = atr;
            Rsi = rsi;
            Dms = dms;
            MacdFast = macdFast;
            MacdSlow = macdSlow;
        }
    }

    public sealed class CFIPClean77NativeIndicatorCatalog
    {
        private readonly IIndicatorsAccessor _indicators;
        private readonly List<CFIPClean77NativeIndicatorSet> _sets =
            new List<CFIPClean77NativeIndicatorSet>();

        public CFIPClean77NativeIndicatorCatalog(
            IIndicatorsAccessor indicators)
        {
            _indicators =
                indicators ??
                throw new ArgumentNullException("indicators");
        }

        public CFIPClean77NativeIndicatorSet GetOrCreate(
            Bars bars,
            CFIPClean77ConfigSnapshot configuration)
        {
            if (bars == null)
                return null;

            for (int i = 0; i < _sets.Count; i++)
            {
                if (ReferenceEquals(
                        _sets[i].Bars,
                        bars))
                    return _sets[i];
            }

            int fastEmaPeriod =
                Math.Max(
                    2,
                    configuration.Get(
                        "FastEma",
                        21));

            int slowEmaPeriod =
                Math.Max(
                    fastEmaPeriod + 1,
                    configuration.Get(
                        "SlowEma",
                        55));

            int atrPeriod =
                Math.Max(
                    2,
                    configuration.Get(
                        "AtrPeriod",
                        14));

            int rsiPeriod =
                Math.Max(
                    2,
                    configuration.Get(
                        "RsiPeriod",
                        14));

            int adxPeriod =
                Math.Max(
                    2,
                    configuration.Get(
                        "AdxPeriod",
                        14));

            int macdFastPeriod =
                Math.Max(
                    2,
                    configuration.Get(
                        "MacdFastPeriod",
                        12));

            int macdSlowPeriod =
                Math.Max(
                    macdFastPeriod + 1,
                    configuration.Get(
                        "MacdSlowPeriod",
                        26));

            try
            {
                CFIPClean77NativeIndicatorSet set =
                    new CFIPClean77NativeIndicatorSet(
                        bars,
                        _indicators.ExponentialMovingAverage(
                            bars.ClosePrices,
                            fastEmaPeriod),
                        _indicators.ExponentialMovingAverage(
                            bars.ClosePrices,
                            slowEmaPeriod),
                        _indicators.AverageTrueRange(
                            bars,
                            atrPeriod,
                            MovingAverageType.WilderSmoothing),
                        _indicators.RelativeStrengthIndex(
                            bars.ClosePrices,
                            rsiPeriod),
                        _indicators.DirectionalMovementSystem(
                            bars,
                            adxPeriod,
                            MovingAverageType.WilderSmoothing),
                        _indicators.ExponentialMovingAverage(
                            bars.ClosePrices,
                            macdFastPeriod),
                        _indicators.ExponentialMovingAverage(
                            bars.ClosePrices,
                            macdSlowPeriod));

                _sets.Add(set);
                return set;
            }
            catch
            {
                return null;
            }
        }
    }

    // ------------------------------------------------------------------------
    // Decision
    // ------------------------------------------------------------------------

    public sealed class CFIPClean77DecisionSnapshot
    {
        private readonly ReadOnlyCollection<CFIPClean77BlockReason> _blockReasons;

        public CFIPClean77Direction Direction { get; private set; }
        public int Confidence { get; private set; }
        public int Quality { get; private set; }
        public int Edge { get; private set; }
        public int MtfAgreement { get; private set; }
        public int IndependentEvidence { get; private set; }
        public int StructuralConfirmations { get; private set; }
        public string Regime { get; private set; }
        public int RegimeQuality { get; private set; }
        public bool EntryEligible { get; private set; }
        public CFIPClean77DecisionPolicyMode PolicyMode { get; private set; }
        public IReadOnlyList<CFIPClean77BlockReason> BlockReasons
        {
            get { return _blockReasons; }
        }
        public CFIPClean77Provenance Provenance { get; private set; }

        public CFIPClean77DecisionSnapshot(
            CFIPClean77Direction direction,
            int confidence,
            int quality,
            int edge,
            int mtfAgreement,
            int independentEvidence,
            int structuralConfirmations,
            string regime,
            int regimeQuality,
            bool entryEligible,
            CFIPClean77DecisionPolicyMode policyMode,
            IList<CFIPClean77BlockReason> blockReasons,
            CFIPClean77Provenance provenance)
        {
            Direction = direction;
            Confidence = Math.Max(0, Math.Min(100, confidence));
            Quality = Math.Max(0, Math.Min(100, quality));
            Edge = Math.Max(-100, Math.Min(100, edge));
            MtfAgreement = Math.Max(0, Math.Min(100, mtfAgreement));
            IndependentEvidence = Math.Max(0, independentEvidence);
            StructuralConfirmations = Math.Max(0, structuralConfirmations);
            Regime = regime ?? string.Empty;
            RegimeQuality = Math.Max(0, Math.Min(100, regimeQuality));
            EntryEligible = entryEligible;
            PolicyMode = policyMode;
            _blockReasons =
                new ReadOnlyCollection<CFIPClean77BlockReason>(
                    new List<CFIPClean77BlockReason>(
                        blockReasons ??
                        new List<CFIPClean77BlockReason>()));
            Provenance =
                provenance ??
                CFIPClean77Provenance.Direct(
                    "UNKNOWN",
                    "UNSPECIFIED");
        }
    }

    // ------------------------------------------------------------------------
    // Trade identity / idempotency
    // ------------------------------------------------------------------------

    public sealed class CFIPClean77TradeIdentity
    {
        public string StrategyId { get; private set; }
        public string Symbol { get; private set; }
        public string PlanId { get; private set; }
        public string SignalId { get; private set; }

        public CFIPClean77TradeIdentity(
            string strategyId,
            string symbol,
            string planId,
            string signalId)
        {
            StrategyId = strategyId ?? string.Empty;
            Symbol = symbol ?? string.Empty;
            PlanId = planId ?? string.Empty;
            SignalId = signalId ?? string.Empty;
        }
    }

    public sealed class CFIPClean77ExecutionIdentity
    {
        public string IntentId { get; private set; }
        public string IdempotencyKey { get; private set; }
        public DateTime CreatedUtc { get; private set; }

        public CFIPClean77ExecutionIdentity(
            string intentId,
            string idempotencyKey,
            DateTime createdUtc)
        {
            IntentId = intentId ?? string.Empty;
            IdempotencyKey = idempotencyKey ?? string.Empty;
            CreatedUtc = createdUtc;

            if (string.IsNullOrWhiteSpace(IdempotencyKey))
                throw new ArgumentException(
                    "IdempotencyKey is mandatory.",
                    "idempotencyKey");
        }
    }

    // ------------------------------------------------------------------------
    // Trade plan
    // ------------------------------------------------------------------------

    public sealed class CFIPClean77TradePlan
    {
        public CFIPClean77TradeIdentity Identity { get; private set; }
        public CFIPClean77Direction Direction { get; private set; }
        public CFIPClean77EntryMode EntryMode { get; private set; }
        public CFIPClean77EntryModel Entry { get; private set; }
        public CFIPClean77PriceLevel StructuralStop { get; private set; }
        public CFIPClean77TargetLadder TargetLadder { get; private set; }
        public double RiskRewardToTp1 { get; private set; }
        public double RiskRewardToFinalTarget { get; private set; }
        public int LevelQuality { get; private set; }
        public bool IsValid { get; private set; }
        public DateTime CreatedUtc { get; private set; }
        public int ReferenceBarIndex { get; private set; }
        public CFIPClean77Provenance Provenance { get; private set; }

        public CFIPClean77TradePlan(
            CFIPClean77TradeIdentity identity,
            CFIPClean77Direction direction,
            CFIPClean77EntryMode entryMode,
            CFIPClean77EntryModel entry,
            CFIPClean77PriceLevel structuralStop,
            CFIPClean77TargetLadder targetLadder,
            double riskRewardToTp1,
            double riskRewardToFinalTarget,
            int levelQuality,
            bool isValid,
            DateTime createdUtc,
            int referenceBarIndex,
            CFIPClean77Provenance provenance)
        {
            Identity =
                identity ??
                throw new ArgumentNullException("identity");
            Direction = direction;
            EntryMode = entryMode;
            Entry =
                entry ??
                throw new ArgumentNullException("entry");
            StructuralStop =
                structuralStop ??
                throw new ArgumentNullException("structuralStop");
            TargetLadder =
                targetLadder ??
                throw new ArgumentNullException("targetLadder");
            RiskRewardToTp1 = Math.Max(0, riskRewardToTp1);
            RiskRewardToFinalTarget = Math.Max(0, riskRewardToFinalTarget);
            LevelQuality = Math.Max(0, Math.Min(100, levelQuality));
            IsValid = isValid;
            CreatedUtc = createdUtc;
            ReferenceBarIndex = referenceBarIndex;
            Provenance =
                provenance ??
                CFIPClean77Provenance.Direct(
                    "UNKNOWN",
                    "UNSPECIFIED");
        }
    }

    // ------------------------------------------------------------------------
    // Execution intent / result
    // ------------------------------------------------------------------------

    public sealed class CFIPClean77RiskRequest
    {
        public double RiskPercentEquity { get; private set; }
        public double RequestedVolumeInUnits { get; private set; }
        public double MaxRiskAmount { get; private set; }

        public CFIPClean77RiskRequest(
            double riskPercentEquity,
            double requestedVolumeInUnits,
            double maxRiskAmount)
        {
            RiskPercentEquity =
                Math.Max(0, riskPercentEquity);
            RequestedVolumeInUnits =
                Math.Max(0, requestedVolumeInUnits);
            MaxRiskAmount =
                Math.Max(0, maxRiskAmount);
        }
    }

    public sealed class CFIPClean77ExecutionIntent
    {
        public CFIPClean77ExecutionIdentity Identity { get; private set; }
        public CFIPClean77TradeIdentity TradeIdentity { get; private set; }
        public CFIPClean77Direction Direction { get; private set; }
        public CFIPClean77ExecutionKind Kind { get; private set; }
        public CFIPClean77EntryMode EntryMode { get; private set; }

        // Exact value requested from the broker.
        public CFIPClean77PriceLevel RequestedEntry { get; private set; }

        // Structural activation threshold, when applicable.
        public CFIPClean77PriceLevel Trigger { get; private set; }

        // Protection requested at execution time.
        public CFIPClean77PriceLevel StopLoss { get; private set; }

        // This is the requested active broker target, not the whole ladder.
        public CFIPClean77PriceLevel EffectiveTarget { get; private set; }

        public double VolumeInUnits { get; private set; }
        public CFIPClean77RiskRequest Risk { get; private set; }
        public CFIPClean77DecisionPolicyMode PolicyMode { get; private set; }
        public DateTime CreatedUtc { get; private set; }
        public DateTime? ExpiryUtc { get; private set; }

        public CFIPClean77ExecutionIntent(
            CFIPClean77ExecutionIdentity identity,
            CFIPClean77TradeIdentity tradeIdentity,
            CFIPClean77Direction direction,
            CFIPClean77ExecutionKind kind,
            CFIPClean77EntryMode entryMode,
            CFIPClean77PriceLevel requestedEntry,
            CFIPClean77PriceLevel trigger,
            CFIPClean77PriceLevel stopLoss,
            CFIPClean77PriceLevel effectiveTarget,
            double volumeInUnits,
            CFIPClean77RiskRequest risk,
            CFIPClean77DecisionPolicyMode policyMode,
            DateTime createdUtc,
            DateTime? expiryUtc)
        {
            Identity =
                identity ??
                throw new ArgumentNullException("identity");
            TradeIdentity =
                tradeIdentity ??
                throw new ArgumentNullException("tradeIdentity");
            Direction = direction;
            Kind = kind;
            EntryMode = entryMode;
            RequestedEntry = requestedEntry;
            Trigger = trigger;
            StopLoss =
                stopLoss ??
                throw new ArgumentNullException("stopLoss");
            EffectiveTarget = effectiveTarget;
            VolumeInUnits =
                Math.Max(0, volumeInUnits);
            Risk =
                risk ??
                throw new ArgumentNullException("risk");
            PolicyMode = policyMode;
            CreatedUtc = createdUtc;
            ExpiryUtc = expiryUtc;
        }
    }

    public sealed class CFIPClean77ExecutionResult
    {
        public bool Accepted { get; private set; }
        public string BrokerOrderId { get; private set; }
        public string BrokerPositionId { get; private set; }
        public CFIPClean77PriceLevel ActualFill { get; private set; }
        public double RequestedVsActualDelta { get; private set; }
        public string BrokerError { get; private set; }
        public CFIPClean77ProtectionState ProtectionState { get; private set; }
        public bool ReconciliationRequired { get; private set; }

        public CFIPClean77ExecutionResult(
            bool accepted,
            string brokerOrderId,
            string brokerPositionId,
            CFIPClean77PriceLevel actualFill,
            double requestedVsActualDelta,
            string brokerError,
            CFIPClean77ProtectionState protectionState,
            bool reconciliationRequired)
        {
            Accepted = accepted;
            BrokerOrderId = brokerOrderId ?? string.Empty;
            BrokerPositionId = brokerPositionId ?? string.Empty;
            ActualFill = actualFill;
            RequestedVsActualDelta = requestedVsActualDelta;
            BrokerError = brokerError ?? string.Empty;
            ProtectionState = protectionState;
            ReconciliationRequired = reconciliationRequired;
        }
    }

    // ------------------------------------------------------------------------
    // Broker state
    // ------------------------------------------------------------------------

    public sealed class CFIPClean77BrokerPositionSnapshot
    {
        public string BrokerPositionId { get; private set; }
        public string Label { get; private set; }
        public string Symbol { get; private set; }
        public CFIPClean77Direction Direction { get; private set; }
        public double EntryPrice { get; private set; }
        public double VolumeInUnits { get; private set; }
        public double? StopLoss { get; private set; }
        public double? TakeProfit { get; private set; }
        public double NetProfit { get; private set; }
        public bool IsOpen { get; private set; }

        public CFIPClean77BrokerPositionSnapshot(
            string brokerPositionId,
            string label,
            string symbol,
            CFIPClean77Direction direction,
            double entryPrice,
            double volumeInUnits,
            double? stopLoss,
            double? takeProfit,
            double netProfit,
            bool isOpen)
        {
            BrokerPositionId = brokerPositionId ?? string.Empty;
            Label = label ?? string.Empty;
            Symbol = symbol ?? string.Empty;
            Direction = direction;
            EntryPrice = entryPrice;
            VolumeInUnits = volumeInUnits;
            StopLoss = stopLoss;
            TakeProfit = takeProfit;
            NetProfit = netProfit;
            IsOpen = isOpen;
        }
    }

    public sealed class CFIPClean77BrokerPendingOrderSnapshot
    {
        public string BrokerOrderId { get; private set; }
        public string Label { get; private set; }
        public string Symbol { get; private set; }
        public CFIPClean77Direction Direction { get; private set; }
        public CFIPClean77ExecutionKind Kind { get; private set; }
        public double RequestedEntry { get; private set; }
        public double VolumeInUnits { get; private set; }
        public double? StopLoss { get; private set; }
        public double? TakeProfit { get; private set; }
        public bool IsActive { get; private set; }

        public CFIPClean77BrokerPendingOrderSnapshot(
            string brokerOrderId,
            string label,
            string symbol,
            CFIPClean77Direction direction,
            CFIPClean77ExecutionKind kind,
            double requestedEntry,
            double volumeInUnits,
            double? stopLoss,
            double? takeProfit,
            bool isActive)
        {
            BrokerOrderId = brokerOrderId ?? string.Empty;
            Label = label ?? string.Empty;
            Symbol = symbol ?? string.Empty;
            Direction = direction;
            Kind = kind;
            RequestedEntry = requestedEntry;
            VolumeInUnits = volumeInUnits;
            StopLoss = stopLoss;
            TakeProfit = takeProfit;
            IsActive = isActive;
        }
    }

    public sealed class CFIPClean77BrokerStateSnapshot
    {
        private readonly ReadOnlyCollection<CFIPClean77BrokerPositionSnapshot> _positions;
        private readonly ReadOnlyCollection<CFIPClean77BrokerPendingOrderSnapshot> _pendingOrders;

        public IReadOnlyList<CFIPClean77BrokerPositionSnapshot> Positions
        {
            get { return _positions; }
        }

        public IReadOnlyList<CFIPClean77BrokerPendingOrderSnapshot> PendingOrders
        {
            get { return _pendingOrders; }
        }

        public CFIPClean77BrokerStateSnapshot(
            IList<CFIPClean77BrokerPositionSnapshot> positions,
            IList<CFIPClean77BrokerPendingOrderSnapshot> pendingOrders)
        {
            _positions =
                new ReadOnlyCollection<CFIPClean77BrokerPositionSnapshot>(
                    new List<CFIPClean77BrokerPositionSnapshot>(
                        positions ??
                        new List<CFIPClean77BrokerPositionSnapshot>()));

            _pendingOrders =
                new ReadOnlyCollection<CFIPClean77BrokerPendingOrderSnapshot>(
                    new List<CFIPClean77BrokerPendingOrderSnapshot>(
                        pendingOrders ??
                        new List<CFIPClean77BrokerPendingOrderSnapshot>()));
        }
    }

    // ------------------------------------------------------------------------
    // Lifecycle authority
    // ------------------------------------------------------------------------

    public sealed class CFIPClean77LifecycleTransition
    {
        public CFIPClean77LifecycleState From { get; private set; }
        public CFIPClean77LifecycleState To { get; private set; }
        public DateTime TimeUtc { get; private set; }
        public string Reason { get; private set; }

        public CFIPClean77LifecycleTransition(
            CFIPClean77LifecycleState from,
            CFIPClean77LifecycleState to,
            DateTime timeUtc,
            string reason)
        {
            From = from;
            To = to;
            TimeUtc = timeUtc;
            Reason = reason ?? string.Empty;
        }
    }

    public sealed class CFIPClean77LifecycleManager
    {
        private CFIPClean77LifecycleState _state;

        public CFIPClean77LifecycleState State
        {
            get { return _state; }
        }

        public CFIPClean77LifecycleManager()
        {
            _state = CFIPClean77LifecycleState.Flat;
        }

        public bool TryTransition(
            CFIPClean77LifecycleState target,
            DateTime timeUtc,
            string reason)
        {
            if (!IsAllowed(_state, target))
                return false;

            _state = target;
            return true;
        }

        private static bool IsAllowed(
            CFIPClean77LifecycleState from,
            CFIPClean77LifecycleState to)
        {
            if (from == to)
                return true;

            switch (from)
            {
                case CFIPClean77LifecycleState.Flat:
                    return
                        to == CFIPClean77LifecycleState.SignalDetected ||
                        to == CFIPClean77LifecycleState.Closed;

                case CFIPClean77LifecycleState.SignalDetected:
                    return
                        to == CFIPClean77LifecycleState.PlanReady ||
                        to == CFIPClean77LifecycleState.Rejected ||
                        to == CFIPClean77LifecycleState.Flat;

                case CFIPClean77LifecycleState.PlanReady:
                    return
                        to == CFIPClean77LifecycleState.ExecutionReady ||
                        to == CFIPClean77LifecycleState.Rejected ||
                        to == CFIPClean77LifecycleState.Flat;

                case CFIPClean77LifecycleState.ExecutionReady:
                    return
                        to == CFIPClean77LifecycleState.PendingOrder ||
                        to == CFIPClean77LifecycleState.LivePosition ||
                        to == CFIPClean77LifecycleState.Rejected ||
                        to == CFIPClean77LifecycleState.Error;

                case CFIPClean77LifecycleState.PendingOrder:
                    return
                        to == CFIPClean77LifecycleState.LivePosition ||
                        to == CFIPClean77LifecycleState.RecoveryRequired ||
                        to == CFIPClean77LifecycleState.Flat ||
                        to == CFIPClean77LifecycleState.Error;

                case CFIPClean77LifecycleState.LivePosition:
                    return
                        to == CFIPClean77LifecycleState.ExitRequested ||
                        to == CFIPClean77LifecycleState.RecoveryRequired ||
                        to == CFIPClean77LifecycleState.Closed ||
                        to == CFIPClean77LifecycleState.Error;

                case CFIPClean77LifecycleState.ExitRequested:
                    return
                        to == CFIPClean77LifecycleState.Closed ||
                        to == CFIPClean77LifecycleState.RecoveryRequired ||
                        to == CFIPClean77LifecycleState.Error;

                case CFIPClean77LifecycleState.RecoveryRequired:
                    return
                        to == CFIPClean77LifecycleState.LivePosition ||
                        to == CFIPClean77LifecycleState.Closed ||
                        to == CFIPClean77LifecycleState.Error ||
                        to == CFIPClean77LifecycleState.Flat;

                case CFIPClean77LifecycleState.Closed:
                    return
                        to == CFIPClean77LifecycleState.SignalDetected ||
                        to == CFIPClean77LifecycleState.Flat;

                case CFIPClean77LifecycleState.Rejected:
                    return
                        to == CFIPClean77LifecycleState.Flat ||
                        to == CFIPClean77LifecycleState.SignalDetected;

                case CFIPClean77LifecycleState.Error:
                    return
                        to == CFIPClean77LifecycleState.RecoveryRequired ||
                        to == CFIPClean77LifecycleState.Flat;

                default:
                    return false;
            }
        }
    }

    // ------------------------------------------------------------------------
    // Configuration / runtime separation
    // ------------------------------------------------------------------------

    public sealed class CFIPClean77ExecutionEnvelope
    {
        public double MaxEntryChaseAtr { get; private set; }
        public double MaxBrokerSlippagePips { get; private set; }
        public double MaxBreakoutFillDeviationPips { get; private set; }
        public double MaxPlanRebaseDistancePips { get; private set; }

        public CFIPClean77ExecutionEnvelope(
            double maxEntryChaseAtr,
            double maxBrokerSlippagePips,
            double maxBreakoutFillDeviationPips,
            double maxPlanRebaseDistancePips)
        {
            MaxEntryChaseAtr =
                Math.Max(0, maxEntryChaseAtr);
            MaxBrokerSlippagePips =
                Math.Max(0, maxBrokerSlippagePips);
            MaxBreakoutFillDeviationPips =
                Math.Max(0, maxBreakoutFillDeviationPips);
            MaxPlanRebaseDistancePips =
                Math.Max(0, maxPlanRebaseDistancePips);
        }
    }

    public enum CFIPClean77ConfigurationSection
    {
        Decision,
        Mtf,
        Structure,
        Zones,
        Liquidity,
        Indicators,
        Entry,
        SmartWeights,
        RiskTargets,
        LiveManagement,
        Filters,
        Alerts,
        Automation,
        SmartExecution,
        Display,
        Control,
        ConfluenceExtensions,
        CompleteIntelligence,
        StructuralExecution,
        SafetyPrecision,
        EarlyIntelligence,
        Accuracy,
        SmartEngine,
        Unknown
    }

    public sealed class CFIPClean77ConfigurationSectionSnapshot
    {
        private readonly ReadOnlyDictionary<string, object> _values;

        public CFIPClean77ConfigurationSection Section { get; private set; }
        public IReadOnlyDictionary<string, object> Values { get { return _values; } }

        public CFIPClean77ConfigurationSectionSnapshot(
            CFIPClean77ConfigurationSection section,
            IDictionary<string, object> values)
        {
            Section = section;
            _values = new ReadOnlyDictionary<string, object>(
                new Dictionary<string, object>(
                    values ??
                    new Dictionary<string, object>()));
        }

        public T Get<T>(string name, T fallback)
        {
            object value;
            if (!_values.TryGetValue(name ?? string.Empty, out value) ||
                value == null)
                return fallback;

            if (value is T)
                return (T)value;

            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return fallback;
            }
        }
    }

    public sealed class CFIPClean77ConfigSnapshot
    {
        private readonly ReadOnlyDictionary<string, object> _values;
        private readonly ReadOnlyDictionary<string, CFIPClean77ConfigurationSectionSnapshot> _sections;

        public string StrategyId { get; private set; }
        public int ParameterCount { get { return _values.Count; } }
        public bool ManualTradeEntryControlsSupported { get { return false; } }

        public IReadOnlyDictionary<string, object> Values { get { return _values; } }
        public IReadOnlyDictionary<string, CFIPClean77ConfigurationSectionSnapshot> Sections
        {
            get { return _sections; }
        }

        private CFIPClean77ConfigSnapshot(
            string strategyId,
            IDictionary<string, object> values,
            IDictionary<string, CFIPClean77ConfigurationSectionSnapshot> sections)
        {
            StrategyId = strategyId ?? string.Empty;
            _values = new ReadOnlyDictionary<string, object>(
                new Dictionary<string, object>(values));

            _sections =
                new ReadOnlyDictionary<string, CFIPClean77ConfigurationSectionSnapshot>(
                    new Dictionary<string, CFIPClean77ConfigurationSectionSnapshot>(sections));
        }

        public static CFIPClean77ConfigSnapshot Build(
            object parameterSource,
            string strategyId)
        {
            if (parameterSource == null)
                throw new ArgumentNullException("parameterSource");

            var values =
                new Dictionary<string, object>(StringComparer.Ordinal);

            var buckets =
                new Dictionary<CFIPClean77ConfigurationSection, IDictionary<string, object>>();

            foreach (CFIPClean77ConfigurationSection section in Enum.GetValues(
                typeof(CFIPClean77ConfigurationSection)))
            {
                buckets[section] =
                    new Dictionary<string, object>(StringComparer.Ordinal);
            }

            foreach (var property in parameterSource.GetType().GetProperties())
            {
                if (!property.CanRead ||
                    property.GetIndexParameters().Length != 0)
                    continue;

                var attributes = property.GetCustomAttributes(false);
                bool isParameter = false;
                string group = string.Empty;

                for (int i = 0; i < attributes.Length; i++)
                {
                    object attribute = attributes[i];
                    if (attribute == null)
                        continue;

                    if (!string.Equals(
                        attribute.GetType().Name,
                        "ParameterAttribute",
                        StringComparison.Ordinal))
                        continue;

                    isParameter = true;

                    var groupProperty =
                        attribute.GetType().GetProperty("Group");

                    if (groupProperty != null)
                    {
                        object rawGroup =
                            groupProperty.GetValue(
                                attribute,
                                null);

                        group =
                            rawGroup == null
                                ? string.Empty
                                : rawGroup.ToString();
                    }

                    break;
                }

                if (!isParameter)
                    continue;

                object value = property.GetValue(parameterSource, null);
                values[property.Name] = value;

                CFIPClean77ConfigurationSection section =
                    ResolveSection(group);

                buckets[section][property.Name] = value;
            }

            var sections =
                new Dictionary<string, CFIPClean77ConfigurationSectionSnapshot>(
                    StringComparer.Ordinal);

            foreach (var pair in buckets)
            {
                sections[pair.Key.ToString()] =
                    new CFIPClean77ConfigurationSectionSnapshot(
                        pair.Key,
                        pair.Value);
            }

            return new CFIPClean77ConfigSnapshot(
                strategyId,
                values,
                sections);
        }

        public object Get(
            string name,
            object fallback)
        {
            object value;
            return
                _values.TryGetValue(
                    name ?? string.Empty,
                    out value)
                    ? value
                    : fallback;
        }

        public T Get<T>(
            string name,
            T fallback)
        {
            object value;
            if (!_values.TryGetValue(
                    name ?? string.Empty,
                    out value) ||
                value == null)
                return fallback;

            if (value is T)
                return (T)value;

            try
            {
                return (T)Convert.ChangeType(
                    value,
                    typeof(T));
            }
            catch
            {
                return fallback;
            }
        }

        public static CFIPClean77ConfigurationSection ResolveSection(
            string group)
        {
            string value = group ?? string.Empty;

            if (value.Contains("01 · Decision"))
                return CFIPClean77ConfigurationSection.Decision;
            if (value.Contains("02 · MTF"))
                return CFIPClean77ConfigurationSection.Mtf;
            if (value.Contains("03 · Structure"))
                return CFIPClean77ConfigurationSection.Structure;
            if (value.Contains("04 · Zones"))
                return CFIPClean77ConfigurationSection.Zones;
            if (value.Contains("05 · Liquidity"))
                return CFIPClean77ConfigurationSection.Liquidity;
            if (value.Contains("06 · Indicators"))
                return CFIPClean77ConfigurationSection.Indicators;
            if (value.Contains("07 · Entry"))
                return CFIPClean77ConfigurationSection.Entry;
            if (value.Contains("08 · Smart Weights"))
                return CFIPClean77ConfigurationSection.SmartWeights;
            if (value.Contains("09 · Risk & Targets"))
                return CFIPClean77ConfigurationSection.RiskTargets;
            if (value.Contains("10 · Live Management"))
                return CFIPClean77ConfigurationSection.LiveManagement;
            if (value.Contains("11 · Filters"))
                return CFIPClean77ConfigurationSection.Filters;
            if (value.Contains("12 · ALERTS"))
                return CFIPClean77ConfigurationSection.Alerts;
            if (value.Contains("13 · AUTO TRADING"))
                return CFIPClean77ConfigurationSection.Automation;
            if (value.Contains("24 · SMART EXECUTION"))
                return CFIPClean77ConfigurationSection.SmartExecution;
            if (value.Contains("14 · DISPLAY"))
                return CFIPClean77ConfigurationSection.Display;
            if (value.Contains("15 · CONTROL"))
                return CFIPClean77ConfigurationSection.Control;
            if (value.Contains("20 · Confluence"))
                return CFIPClean77ConfigurationSection.ConfluenceExtensions;
            if (value.Contains("21 · Complete Intelligence"))
                return CFIPClean77ConfigurationSection.CompleteIntelligence;
            if (value.Contains("23 · Structural Execution"))
                return CFIPClean77ConfigurationSection.StructuralExecution;
            if (value.Contains("22 · Safety"))
                return CFIPClean77ConfigurationSection.SafetyPrecision;
            if (value.Contains("15 · INTELLIGENCE — EARLY"))
                return CFIPClean77ConfigurationSection.EarlyIntelligence;
            if (value.Contains("16 · Accuracy"))
                return CFIPClean77ConfigurationSection.Accuracy;
            if (value.Contains("17 · Smart Engine"))
                return CFIPClean77ConfigurationSection.SmartEngine;

            return CFIPClean77ConfigurationSection.Unknown;
        }
    }

    public sealed class CFIPClean77RuntimeAuthority
    {
        public bool AutoTradingEnabled { get; private set; }
        public bool AutomaticOrdersEnabled { get; private set; }

        public void InitializeFromConfiguration(
            CFIPClean77ConfigSnapshot configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException("configuration");

            AutoTradingEnabled =
                configuration.Get(
                    "EnableAutoTrading",
                    false);

            AutomaticOrdersEnabled =
                configuration.Get(
                    "EnableAutomaticOrders",
                    false);
        }

        public void SetAutoTradingEnabled(bool enabled)
        {
            AutoTradingEnabled = enabled;
        }

        public void SetAutomaticOrdersEnabled(bool enabled)
        {
            AutomaticOrdersEnabled = enabled;
        }
    }

    // ------------------------------------------------------------------------
    // Outcome contract
    // ------------------------------------------------------------------------

    public sealed class CFIPClean77OutcomeEvent
    {
        public CFIPClean77TradeIdentity TradeIdentity { get; private set; }
        public double EntryPrice { get; private set; }
        public double ExitPrice { get; private set; }
        public double ResultAmount { get; private set; }
        public double ResultR { get; private set; }
        public double MaxFavorableExcursion { get; private set; }
        public double MaxAdverseExcursion { get; private set; }
        public CFIPClean77TargetStage HighestTargetStageReached { get; private set; }
        public string ExitReason { get; private set; }
        public DateTime EntryUtc { get; private set; }
        public DateTime ExitUtc { get; private set; }

        public CFIPClean77OutcomeEvent(
            CFIPClean77TradeIdentity tradeIdentity,
            double entryPrice,
            double exitPrice,
            double resultAmount,
            double resultR,
            double maxFavorableExcursion,
            double maxAdverseExcursion,
            CFIPClean77TargetStage highestTargetStageReached,
            string exitReason,
            DateTime entryUtc,
            DateTime exitUtc)
        {
            TradeIdentity =
                tradeIdentity ??
                throw new ArgumentNullException("tradeIdentity");
            EntryPrice = entryPrice;
            ExitPrice = exitPrice;
            ResultAmount = resultAmount;
            ResultR = resultR;
            MaxFavorableExcursion =
                Math.Max(0, maxFavorableExcursion);
            MaxAdverseExcursion =
                Math.Max(0, maxAdverseExcursion);
            HighestTargetStageReached =
                highestTargetStageReached;
            ExitReason = exitReason ?? string.Empty;
            EntryUtc = entryUtc;
            ExitUtc = exitUtc;
        }
    }

    // ------------------------------------------------------------------------
    // Presentation boundary
    // ------------------------------------------------------------------------

    public sealed class CFIPClean77PresentationState
    {
        public DateTime GeneratedUtc { get; private set; }
        public CFIPClean77Direction Direction { get; private set; }
        public CFIPClean77LifecycleState LifecycleState { get; private set; }
        public string Readiness { get; private set; }
        public string ExecutionStatus { get; private set; }
        public string BrokerStatus { get; private set; }
        public string AlertStatus { get; private set; }

        public CFIPClean77PresentationState(
            DateTime generatedUtc,
            CFIPClean77Direction direction,
            CFIPClean77LifecycleState lifecycleState,
            string readiness,
            string executionStatus,
            string brokerStatus,
            string alertStatus)
        {
            GeneratedUtc = generatedUtc;
            Direction = direction;
            LifecycleState = lifecycleState;
            Readiness = readiness ?? string.Empty;
            ExecutionStatus = executionStatus ?? string.Empty;
            BrokerStatus = brokerStatus ?? string.Empty;
            AlertStatus = alertStatus ?? string.Empty;
        }
    }

    // ------------------------------------------------------------------------
    // Service contracts
    //
    // The interfaces are deliberately broker/UI agnostic. Later phases provide
    // cTrader adapters behind them.
    // ------------------------------------------------------------------------

    public interface ICFIPClean77DecisionEngine
    {
        CFIPClean77DecisionSnapshot Evaluate(
            CFIPClean77RuntimeSnapshot runtime,
            CFIPClean77MtfSnapshot mtf,
            CFIPClean77MarketModel market,
            CFIPClean77ConfigSnapshot configuration);
    }

    public interface ICFIPClean77TradePlanBuilder
    {
        CFIPClean77TradePlan Build(
            CFIPClean77DecisionSnapshot decision,
            CFIPClean77MarketModel market,
            CFIPClean77RuntimeSnapshot runtime,
            CFIPClean77ConfigSnapshot configuration);
    }

    public interface ICFIPClean77ExecutionPlanner
    {
        CFIPClean77ExecutionIntent CreateIntent(
            CFIPClean77TradePlan plan,
            CFIPClean77RuntimeSnapshot runtime,
            CFIPClean77ConfigSnapshot configuration);
    }

    public interface ICFIPClean77BrokerGateway
    {
        CFIPClean77ExecutionResult Execute(
            CFIPClean77ExecutionIntent intent);

        CFIPClean77ExecutionResult ModifyProtection(
            string brokerPositionId,
            double? stopLoss,
            double? takeProfit);

        CFIPClean77ExecutionResult ClosePosition(
            string brokerPositionId);

        CFIPClean77ExecutionResult CancelPendingOrder(
            string brokerOrderId);
    }

    public interface ICFIPClean77BrokerStateReader
    {
        CFIPClean77BrokerStateSnapshot ReadManagedState(
            string symbol,
            string strategyId);
    }

    public interface ICFIPClean77OutcomeRecorder
    {
        void Record(
            CFIPClean77OutcomeEvent outcome);
    }

    public interface ICFIPClean77PresentationProjector
    {
        CFIPClean77PresentationState Project(
            CFIPClean77RuntimeSnapshot runtime,
            CFIPClean77DecisionSnapshot decision,
            CFIPClean77TradePlan plan,
            CFIPClean77BrokerStateSnapshot broker,
            CFIPClean77LifecycleState lifecycle);
    }

    // ------------------------------------------------------------------------
    // Phase-4 engine shell
    // ------------------------------------------------------------------------

    internal sealed class CFIPClean77EngineState
    {
        public CFIPClean77RuntimeSnapshot Runtime { get; private set; }
        public CFIPClean77MtfSnapshot Mtf { get; private set; }
        public CFIPClean77MarketModel Market { get; private set; }
        public CFIPClean77DecisionSnapshot Decision { get; private set; }
        public CFIPClean77TradePlan Plan { get; private set; }
        public CFIPClean77ExecutionIntent Intent { get; private set; }
        public CFIPClean77ExecutionResult Execution { get; private set; }
        public CFIPClean77BrokerStateSnapshot Broker { get; private set; }
        public CFIPClean77PresentationState Presentation { get; private set; }

        public void SetRuntime(CFIPClean77RuntimeSnapshot value)
        {
            Runtime = value;
        }

        public void SetMtf(CFIPClean77MtfSnapshot value)
        {
            Mtf = value;
        }

        public void SetMarket(CFIPClean77MarketModel value)
        {
            Market = value;
        }

        // The cycle state is a downstream data carrier. Later phases extend
        // this with immutable cycle snapshots and explicit orchestration.
        public void ResetCycleOutputs()
        {
            Mtf = null;
            Market = null;
            Decision = null;
            Plan = null;
            Intent = null;
            Execution = null;
            Broker = null;
            Presentation = null;
        }
    }

    public enum CFIPClean77PanelCorner
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }

    public enum CFIPClean77SizingMode
    {
        RiskPercentEquity = 0,
        FixedLots = 1
    }

    public enum CFIPClean77PendingOrderMode
    {
        Adaptive = 0,
        ContinuationStop = 1,
        ReversalLimit = 2,
        Both = 3
    }

    public enum CFIPClean77TargetStage
    {
        TP1 = 0,
        TP2 = 1,
        TP3 = 2,
        TP4 = 3
    }

    // ------------------------------------------------------------------------
    // cTrader host adapter — no broker mutation and no UI authority in Phase 1.
    // ------------------------------------------------------------------------

    [Indicator(
        IsOverlay = true,
        TimeZone = TimeZones.UTC,
        AccessRights = AccessRights.None)]
    public class CFIP_MTF_LiveEntryEngine_Clean_v77 : Indicator
    {
        private Bars _m1Bars;
        private Bars _m5Bars;
        private Bars _m15Bars;
        private Bars _m30Bars;
        private Bars _h1Bars;
        private Bars _h4Bars;
        private Bars _d1Bars;
        private Bars _w1Bars;

        // ====================================================================
        // Phase 2 — complete v73 parameter surface carried forward unchanged.
        // These parameters are configuration inputs only. Business logic will
        // migrate to CFIPClean77ConfigSnapshot in subsequent phases.
        // No parameter is silently dropped during the architectural migration.
        // ====================================================================
        #region Migrated Configuration Surface (512 parameters)

[Parameter("Minimum Confidence", Group = "01 · Decision", DefaultValue = 72, MinValue = 50, MaxValue = 99)]
        public int MinimumConfidence { get; set; }

[Parameter("Minimum Edge", Group = "01 · Decision", DefaultValue = 15, MinValue = 0, MaxValue = 50)]
        public int MinimumEdge { get; set; }

[Parameter("Minimum Smart Quality", Group = "01 · Decision", DefaultValue = 70, MinValue = 40, MaxValue = 95)]
        public int MinimumSmartQuality { get; set; }

[Parameter("Minimum Structural Confirmations", Group = "01 · Decision", DefaultValue = 4, MinValue = 1, MaxValue = 8)]
        public int MinimumStructuralConfirmations { get; set; }

[Parameter("Minimum Independent Evidence", Group = "01 · Decision", DefaultValue = 4, MinValue = 2, MaxValue = 8)]
        public int MinimumIndependentEvidence { get; set; }

[Parameter("Minimum Timeframe Agreement", Group = "01 · Decision", DefaultValue = 72, MinValue = 50, MaxValue = 100)]
        public int MinimumTimeframeAgreement { get; set; }

[Parameter("Require Higher TF Agreement", Group = "01 · Decision", DefaultValue = true)]
        public bool RequireHigherTfAgreement { get; set; }

[Parameter("Require Core Agreement", Group = "01 · Decision", DefaultValue = true)]
        public bool RequireCoreAgreement { get; set; }

[Parameter("Require Structural Confirmation", Group = "01 · Decision", DefaultValue = true)]
        public bool RequireStructuralConfirmation { get; set; }

[Parameter("Use Advanced Confluence", Group = "01 · Decision", DefaultValue = true)]
        public bool UseAdvancedConfluence { get; set; }

[Parameter("Allow Strong Trigger Override", Group = "01 · Decision", DefaultValue = true)]
        public bool AllowStrongTriggerOverride { get; set; }

[Parameter("M1 Trigger", Group = "02 · MTF", DefaultValue = false)]
        public bool UseM1Trigger { get; set; }

[Parameter("M5 Confirmation", Group = "02 · MTF", DefaultValue = true)]
        public bool UseM5Confirmation { get; set; }

[Parameter("M5 Weight", Group = "02 · MTF", DefaultValue = 7, MinValue = 1, MaxValue = 20)]
        public int M5Weight { get; set; }

[Parameter("M15 Weight", Group = "02 · MTF", DefaultValue = 8, MinValue = 1, MaxValue = 20)]
        public int M15Weight { get; set; }

[Parameter("M30 Weight", Group = "02 · MTF", DefaultValue = 5, MinValue = 0, MaxValue = 20)]
        public int M30Weight { get; set; }

[Parameter("H1 Weight", Group = "02 · MTF", DefaultValue = 3, MinValue = 0, MaxValue = 20)]
        public int H1Weight { get; set; }

[Parameter("H4 Weight", Group = "02 · MTF", DefaultValue = 2, MinValue = 0, MaxValue = 20)]
        public int H4Weight { get; set; }

[Parameter("D1 Weight", Group = "02 · MTF", DefaultValue = 2, MinValue = 0, MaxValue = 20)]
        public int D1Weight { get; set; }

[Parameter("W1 Weight", Group = "02 · MTF", DefaultValue = 3, MinValue = 0, MaxValue = 20)]
        public int W1Weight { get; set; }

[Parameter("Structure Lookback", Group = "03 · Structure", DefaultValue = 40, MinValue = 15, MaxValue = 150)]
        public int StructureLookback { get; set; }

[Parameter("Swing Strength", Group = "03 · Structure", DefaultValue = 2, MinValue = 1, MaxValue = 5)]
        public int SwingStrength { get; set; }

[Parameter("Structure Break ATR", Group = "03 · Structure", DefaultValue = 0.05, MinValue = 0, MaxValue = 0.5)]
        public double StructureBreakAtr { get; set; }

[Parameter("Use Internal Structure", Group = "03 · Structure", DefaultValue = true)]
        public bool UseInternalStructure { get; set; }

[Parameter("Use MSS / CHOCH", Group = "03 · Structure", DefaultValue = true)]
        public bool UseMssChoch { get; set; }

[Parameter("Use FVG", Group = "04 · Zones", DefaultValue = true)]
        public bool UseFvg { get; set; }

[Parameter("FVG Lookback", Group = "04 · Zones", DefaultValue = 24, MinValue = 5, MaxValue = 80)]
        public int FvgLookback { get; set; }

[Parameter("Minimum FVG ATR", Group = "04 · Zones", DefaultValue = 0.08, MinValue = 0.01, MaxValue = 1)]
        public double MinimumFvgAtr { get; set; }

[Parameter("Use Order Block", Group = "04 · Zones", DefaultValue = true)]
        public bool UseOrderBlock { get; set; }

[Parameter("OB Lookback", Group = "04 · Zones", DefaultValue = 30, MinValue = 5, MaxValue = 100)]
        public int ObLookback { get; set; }

[Parameter("OB Displacement ATR", Group = "04 · Zones", DefaultValue = 0.60, MinValue = 0.1, MaxValue = 3)]
        public double ObDisplacementAtr { get; set; }

[Parameter("OB Use Body For Zone", Group = "04 · Zones", DefaultValue = false)]
        public bool ObUseBodyForZone { get; set; }

[Parameter("OB Break By Wicks", Group = "04 · Zones", DefaultValue = false)]
        public bool ObBreakByWicks { get; set; }

[Parameter("OB Structure Lookback", Group = "04 · Zones", DefaultValue = 8, MinValue = 3, MaxValue = 40)]
        public int ObStructureLookback { get; set; }

[Parameter("OB Impulse Bars", Group = "04 · Zones", DefaultValue = 4, MinValue = 2, MaxValue = 8)]
        public int ObImpulseBars { get; set; }

[Parameter("OB Minimum Quality", Group = "04 · Zones", DefaultValue = 68, MinValue = 50, MaxValue = 100)]
        public int ObMinimumQuality { get; set; }

[Parameter("Require FVG Retest", Group = "04 · Zones", DefaultValue = true)]
        public bool RequireFvgRetest { get; set; }

[Parameter("Use 2-Bar Imbalance FVG", Group = "04 · Zones", DefaultValue = true)]
        public bool UseTwoBarImbalanceFvg { get; set; }

[Parameter("Require OB Displacement", Group = "04 · Zones", DefaultValue = true)]
        public bool RequireObDisplacement { get; set; }

[Parameter("Zone Proximity ATR", Group = "04 · Zones", DefaultValue = 0.25, MinValue = 0.05, MaxValue = 1)]
        public double ZoneProximityAtr { get; set; }

[Parameter("Maximum Zone Age", Group = "04 · Zones", DefaultValue = 40, MinValue = 5, MaxValue = 200)]
        public int MaximumZoneAgeBars { get; set; }

[Parameter("Liquidity Lookback", Group = "05 · Liquidity", DefaultValue = 40, MinValue = 10, MaxValue = 150)]
        public int LiquidityLookback { get; set; }

[Parameter("Use Equal High / Low", Group = "05 · Liquidity", DefaultValue = true)]
        public bool UseEqualHighLow { get; set; }

[Parameter("Equal Level Tolerance ATR", Group = "05 · Liquidity", DefaultValue = 0.12, MinValue = 0.02, MaxValue = 0.5)]
        public double EqualLevelToleranceAtr { get; set; }

[Parameter("Use Liquidity Sweep", Group = "05 · Liquidity", DefaultValue = true)]
        public bool UseLiquiditySweep { get; set; }

[Parameter("Liquidity Sweep Minimum Depth ATR", Group = "05 · Liquidity", DefaultValue = 0.05, MinValue = 0, MaxValue = 1.0, Step = 0.01)]
        public double LiquiditySweepMinimumDepthAtr { get; set; }

[Parameter("Use Displacement", Group = "05 · Liquidity", DefaultValue = true)]
        public bool UseDisplacement { get; set; }

[Parameter("Displacement ATR", Group = "05 · Liquidity", DefaultValue = 0.80, MinValue = 0.2, MaxValue = 3)]
        public double DisplacementAtr { get; set; }

[Parameter("Use Premium / Discount", Group = "05 · Liquidity", DefaultValue = true)]
        public bool UsePremiumDiscount { get; set; }

[Parameter("Use Daily Weekly Liquidity", Group = "05 · Liquidity", DefaultValue = true)]
        public bool UseDailyWeeklyLiquidity { get; set; }

[Parameter("Use Daily Pivots", Group = "05 · Liquidity", DefaultValue = true)]
        public bool UseDailyPivots { get; set; }

[Parameter("Daily Pivot Weight", Group = "05 · Liquidity", DefaultValue = 18, MinValue = 1, MaxValue = 50)]
        public int DailyPivotWeight { get; set; }

[Parameter("Fast EMA", Group = "06 · Indicators", DefaultValue = 9, MinValue = 2, MaxValue = 50)]
        public int FastEma { get; set; }

[Parameter("Slow EMA", Group = "06 · Indicators", DefaultValue = 21, MinValue = 3, MaxValue = 100)]
        public int SlowEma { get; set; }

[Parameter("RSI Period", Group = "06 · Indicators", DefaultValue = 14, MinValue = 2, MaxValue = 50)]
        public int RsiPeriod { get; set; }

[Parameter("ADX Period", Group = "06 · Indicators", DefaultValue = 14, MinValue = 3, MaxValue = 50)]
        public int AdxPeriod { get; set; }

[Parameter("ADX Minimum", Group = "06 · Indicators", DefaultValue = 16, MinValue = 5, MaxValue = 50)]
        public double AdxMinimum { get; set; }

[Parameter("ATR Period", Group = "06 · Indicators", DefaultValue = 14, MinValue = 3, MaxValue = 50)]
        public int AtrPeriod { get; set; }

[Parameter("Use EMA Slope", Group = "06 · Indicators", DefaultValue = true)]
        public bool UseEmaSlope { get; set; }

[Parameter("Avoid RSI Exhaustion", Group = "06 · Indicators", DefaultValue = true)]
        public bool AvoidRsiExhaustion { get; set; }

[Parameter("Minimum Trigger Body ATR", Group = "07 · Entry Precision", DefaultValue = 0.12, MinValue = 0.02, MaxValue = 0.8)]
        public double MinimumTriggerBodyAtr { get; set; }

[Parameter("Minimum Close Location", Group = "07 · Entry Precision", DefaultValue = 0.65, MinValue = 0.5, MaxValue = 0.95)]
        public double MinimumCloseLocation { get; set; }

[Parameter("Maximum Trigger Range ATR", Group = "07 · Entry Precision", DefaultValue = 2.5, MinValue = 0.8, MaxValue = 5)]
        public double MaximumTriggerRangeAtr { get; set; }

[Parameter("Require Stable M5 Direction", Group = "07 · Entry Precision", DefaultValue = true)]
        public bool RequireStableM5Direction { get; set; }

[Parameter("Stable M5 Bars", Group = "07 · Entry Precision", DefaultValue = 2, MinValue = 1, MaxValue = 5)]
        public int StableM5Bars { get; set; }

[Parameter("Require Stable M15 Direction", Group = "07 · Entry Precision", DefaultValue = true)]
        public bool RequireStableM15Direction { get; set; }

[Parameter("Stable M15 Bars", Group = "07 · Entry Precision", DefaultValue = 2, MinValue = 1, MaxValue = 5)]
        public int StableM15Bars { get; set; }

[Parameter("Entry Buffer ATR", Group = "07 · Entry Precision", DefaultValue = 0.05, MinValue = 0, MaxValue = 0.5)]
        public double EntryBufferAtr { get; set; }

[Parameter("Maximum Entry Extension ATR", Group = "07 · Entry Precision", DefaultValue = 0.75, MinValue = 0.1, MaxValue = 2.5)]
        public double MaximumEntryExtensionAtr { get; set; }

[Parameter("Minimum Retest Quality", Group = "07 · Entry Precision", DefaultValue = 76, MinValue = 40, MaxValue = 100)]
        public int MinimumRetestQuality { get; set; }

[Parameter("Require Retest Quality", Group = "07 · Entry Precision", DefaultValue = true)]
        public bool RequireRetestQuality { get; set; }

[Parameter("Supply / Demand Weight", Group = "08 · Smart Weights", DefaultValue = 125, MinValue = 50, MaxValue = 200)]
        public int SupplyDemandWeight { get; set; }

[Parameter("FVG Weight", Group = "08 · Smart Weights", DefaultValue = 120, MinValue = 50, MaxValue = 200)]
        public int FvgWeight { get; set; }

[Parameter("Order Block Weight", Group = "08 · Smart Weights", DefaultValue = 125, MinValue = 50, MaxValue = 200)]
        public int OrderBlockWeight { get; set; }

[Parameter("Liquidity Pool Weight", Group = "08 · Smart Weights", DefaultValue = 120, MinValue = 50, MaxValue = 200)]
        public int LiquidityPoolWeight { get; set; }

[Parameter("Equal High / Low Weight", Group = "08 · Smart Weights", DefaultValue = 115, MinValue = 50, MaxValue = 200)]
        public int EqualHighLowWeight { get; set; }

[Parameter("Swing Structure Weight", Group = "08 · Smart Weights", DefaultValue = 105, MinValue = 50, MaxValue = 200)]
        public int SwingStructureWeight { get; set; }

[Parameter("MTF Cluster Weight", Group = "08 · Smart Weights", DefaultValue = 130, MinValue = 50, MaxValue = 200)]
        public int MtfClusterWeight { get; set; }

[Parameter("Previous Day Weight", Group = "08 · Smart Weights", DefaultValue = 115, MinValue = 50, MaxValue = 200)]
        public int PreviousDayWeight { get; set; }

[Parameter("Previous Week Weight", Group = "08 · Smart Weights", DefaultValue = 130, MinValue = 50, MaxValue = 200)]
        public int PreviousWeekWeight { get; set; }

[Parameter("Session Weight", Group = "08 · Smart Weights", DefaultValue = 110, MinValue = 50, MaxValue = 200)]
        public int SessionWeight { get; set; }

[Parameter("HTF Structure Weight", Group = "08 · Smart Weights", DefaultValue = 118, MinValue = 50, MaxValue = 200)]
        public int HtfStructureWeight { get; set; }

[Parameter("Minimum SL ATR", Group = "09 · Risk & Targets", DefaultValue = 0.55, MinValue = 0.1, MaxValue = 5)]
        public double MinimumSlAtr { get; set; }

[Parameter("Maximum SL ATR", Group = "09 · Risk & Targets", DefaultValue = 1.80, MinValue = 0.5, MaxValue = 10)]
        public double MaximumSlAtr { get; set; }

[Parameter("Fallback SL ATR Multiplier", Group = "09 · Risk & Targets", DefaultValue = 1.00, MinValue = 0.1, MaxValue = 5)]
        public double FallbackSlAtr { get; set; }

[Parameter("TP1 Minimum RR", Group = "09 · Risk & Targets", DefaultValue = 2.00, MinValue = 0.5, MaxValue = 10)]
        public double Tp1MinimumRR { get; set; }

[Parameter("TP2 Minimum RR", Group = "09 · Risk & Targets", DefaultValue = 3.20, MinValue = 0.8, MaxValue = 20)]
        public double Tp2MinimumRR { get; set; }

[Parameter("TP3 Minimum RR", Group = "09 · Risk & Targets", DefaultValue = 4.80, MinValue = 1, MaxValue = 30)]
        public double Tp3MinimumRR { get; set; }

[Parameter("TP4 Minimum RR", Group = "09 · Risk & Targets", DefaultValue = 6.50, MinValue = 1.5, MaxValue = 40)]
        public double Tp4MinimumRR { get; set; }

[Parameter("Minimum TP Spacing ATR", Group = "09 · Risk & Targets", DefaultValue = 0.40, MinValue = 0.05, MaxValue = 3)]
        public double MinimumTpSpacingAtr { get; set; }

[Parameter("Target Clearance ATR", Group = "09 · Risk & Targets", DefaultValue = 0.10, MinValue = 0.02, MaxValue = 1)]
        public double TargetClearanceAtr { get; set; }

[Parameter("Reject Target Obstacle", Group = "09 · Risk & Targets", DefaultValue = true)]
        public bool RejectTargetObstacle { get; set; }

[Parameter("Maximum Target Extension ATR", Group = "09 · Risk & Targets", DefaultValue = 4.0, MinValue = 1, MaxValue = 20)]
        public double MaximumTargetExtensionAtr { get; set; }

[Parameter("Use HTF Structure For Stop", Group = "09 · Risk & Targets", DefaultValue = true)]
        public bool UseHtfStructureForStop { get; set; }

[Parameter("Require Structural Stop", Group = "09 · Risk & Targets", DefaultValue = true)]
        public bool RequireStructuralStop { get; set; }

[Parameter("Adaptive Structural RR", Group = "09 · Risk & Targets", DefaultValue = true)]
        public bool AdaptiveStructuralRR { get; set; }

[Parameter("Enable Live Exit Management", Group = "10 · Live Management", DefaultValue = true)]
        public bool EnableLiveExitManagement { get; set; }

[Parameter("Move SL To Break Even", Group = "10 · Live Management", DefaultValue = true)]
        public bool MoveSlToBreakEven { get; set; }

[Parameter("Break Even Trigger RR", Group = "10 · Live Management", DefaultValue = 0.90, MinValue = 0.2, MaxValue = 10)]
        public double BreakEvenTriggerRR { get; set; }

[Parameter("Break Even Buffer Pips", Group = "10 · Live Management", DefaultValue = 0.5, MinValue = 0, MaxValue = 20)]
        public double BreakEvenBufferPips { get; set; }

[Parameter("Use Spread Aware Break Even", Group = "10 · Live Management", DefaultValue = true)]
        public bool UseSpreadAwareBreakEven { get; set; }

[Parameter("Risk Free Lock Pips", Group = "10 · Live Management", DefaultValue = 0.5, MinValue = 0, MaxValue = 10)]
        public double RiskFreeLockPips { get; set; }

[Parameter("Smart Trail Momentum Bonus ATR", Group = "10 · Live Management", DefaultValue = 0.10, MinValue = 0, MaxValue = 0.50, Step = 0.01)]
        public double SmartTrailMomentumBonusAtr { get; set; }

[Parameter("Smart Trail Tighten At RR", Group = "10 · Live Management", DefaultValue = 1.80, MinValue = 0.5, MaxValue = 10)]
        public double SmartTrailTightenAtRR { get; set; }

[Parameter("Enable Structural SL Repricing", Group = "10 · Live Management", DefaultValue = true)]
        public bool EnableStructuralSlRepricing { get; set; }

[Parameter("SL Reprice Start RR", Group = "10 · Live Management", DefaultValue = 1.00, MinValue = 0.5, MaxValue = 10)]
        public double SlRepriceStartRR { get; set; }

[Parameter("SL Reprice Breathing ATR", Group = "10 · Live Management", DefaultValue = 0.85, MinValue = 0.2, MaxValue = 5)]
        public double SlRepriceBreathingAtr { get; set; }

[Parameter("SL Reprice Step ATR", Group = "10 · Live Management", DefaultValue = 0.08, MinValue = 0.01, MaxValue = 1)]
        public double SlRepriceStepAtr { get; set; }

[Parameter("Update Unhit Targets", Group = "10 · Live Management", DefaultValue = true)]
        public bool UpdateUnhitTargets { get; set; }

[Parameter("Target Update Trigger RR", Group = "10 · Live Management", DefaultValue = 1.20, MinValue = 0.5, MaxValue = 10)]
        public double TargetUpdateTriggerRR { get; set; }

[Parameter("Enable Profit Exhaustion Protection", Group = "10 · Live Management", DefaultValue = true)]
        public bool EnableProfitExhaustionProtection { get; set; }

[Parameter("Exhaustion Minimum Peak RR", Group = "10 · Live Management", DefaultValue = 1.50, MinValue = 0.8, MaxValue = 10)]
        public double ExhaustionMinimumPeakRR { get; set; }

[Parameter("Exhaustion Retracement Percent", Group = "10 · Live Management", DefaultValue = 35, MinValue = 15, MaxValue = 80)]
        public double ExhaustionRetracementPercent { get; set; }

[Parameter("Exhaustion Pressure Threshold", Group = "10 · Live Management", DefaultValue = 78, MinValue = 55, MaxValue = 100)]
        public int ExhaustionPressureThreshold { get; set; }

[Parameter("Exhaustion Minimum Opposite Evidence", Group = "10 · Live Management", DefaultValue = 3, MinValue = 1, MaxValue = 8)]
        public int ExhaustionMinimumOppositeEvidence { get; set; }

[Parameter("Use Session Filter", Group = "11 · Filters", DefaultValue = false)]
        public bool UseSessionFilter { get; set; }

[Parameter("Session Start UTC", Group = "11 · Filters", DefaultValue = 6, MinValue = 0, MaxValue = 23)]
        public int SessionStartUtc { get; set; }

[Parameter("Session End UTC", Group = "11 · Filters", DefaultValue = 20, MinValue = 0, MaxValue = 23)]
        public int SessionEndUtc { get; set; }

[Parameter("Enable End Of Day Alert", Group = "11 · Filters", DefaultValue = true)]
        public bool EnableEndOfDayAlert { get; set; }

[Parameter("End Of Day Alert Minutes Before", Group = "11 · Filters", DefaultValue = 30, MinValue = 5, MaxValue = 180)]
        public int EndOfDayAlertMinutesBefore { get; set; }

[Parameter("Enable End Of Day Auto Close", Group = "11 · Filters", DefaultValue = true)]
        public bool EnableEndOfDayAutoClose { get; set; }

[Parameter("Use Spread Filter", Group = "11 · Filters", DefaultValue = true)]
        public bool UseSpreadFilter { get; set; }

[Parameter("Maximum Spread ATR", Group = "11 · Filters", DefaultValue = 0.15, MinValue = 0.01, MaxValue = 1)]
        public double MaximumSpreadAtr { get; set; }

[Parameter("Cooldown M5 Bars", Group = "11 · Filters", DefaultValue = 3, MinValue = 0, MaxValue = 50)]
        public int CooldownM5Bars { get; set; }

[Parameter("Avoid Friday Late Entry", Group = "11 · Filters", DefaultValue = false)]
        public bool AvoidFridayLateEntry { get; set; }

[Parameter("Friday Cutoff UTC", Group = "11 · Filters", DefaultValue = 18, MinValue = 0, MaxValue = 23)]
        public int FridayCutoffUtc { get; set; }

[Parameter("Use Volatility Guard", Group = "11 · Filters", DefaultValue = true)]
        public bool UseVolatilityGuard { get; set; }

[Parameter("Event Shock Range ATR", Group = "11 · Filters", DefaultValue = 2.20, MinValue = 1.2, MaxValue = 5)]
        public double EventShockRangeAtr { get; set; }

[Parameter("Event Shock ATR Expansion", Group = "11 · Filters", DefaultValue = 1.55, MinValue = 1.1, MaxValue = 3)]
        public double EventShockAtrExpansion { get; set; }

[Parameter("News Blackout UTC", Group = "11 · Filters", DefaultValue = "")]
        public string NewsBlackoutUtc { get; set; }

[Parameter("Enable Sound Alerts", Group = "12 · ALERTS — CORE", DefaultValue = true)]
        public bool EnableSoundAlerts { get; set; }

[Parameter("Show Popup Alerts", Group = "12 · ALERTS — CORE", DefaultValue = false)]
        public bool ShowPopupAlerts { get; set; }

[Parameter("Popup Critical Only", Group = "12 · ALERTS — CORE", DefaultValue = true)]
        public bool PopupCriticalOnly { get; set; }

[Parameter("Popup Duration Seconds", Group = "12 · ALERTS — CORE", DefaultValue = 6, MinValue = 1, MaxValue = 60)]
        public int PopupDurationSeconds { get; set; }

[Parameter("Popup Font Size", Group = "12 · ALERTS — CORE", DefaultValue = 11, MinValue = 8, MaxValue = 22)]
        public int PopupFontSize { get; set; }

[Parameter("Show Entry Restriction Popup", Group = "12 · ALERTS — CORE", DefaultValue = true)]
        public bool ShowEntryRestrictionPopup { get; set; }

[Parameter("Alert On News / Event Guard", Group = "12 · ALERTS — CORE", DefaultValue = true)]
        public bool AlertOnNewsEventGuard { get; set; }

[Parameter("Popup Margin", Group = "12 · ALERTS — CORE", DefaultValue = 10, MinValue = 0, MaxValue = 50)]
        public int PopupMargin { get; set; }

[Parameter("Popup Border Alpha", Group = "12 · ALERTS — CORE", DefaultValue = 235, MinValue = 0, MaxValue = 255)]
        public int PopupBorderAlpha { get; set; }

[Parameter("Popup Bold", Group = "12 · ALERTS — CORE", DefaultValue = false)]
        public bool PopupBold { get; set; }

[Parameter("Popup Font Family", Group = "12 · ALERTS — CORE", DefaultValue = "Arial")]
        public string PopupFontFamily { get; set; }

[Parameter("Alert On Confirmed Signal", Group = "12 · ALERTS — CORE", DefaultValue = true)]
        public bool AlertOnConfirmedSignal { get; set; }

[Parameter("Alert On Reaction", Group = "12 · ALERTS — CORE", DefaultValue = true)]
        public bool AlertOnReaction { get; set; }

[Parameter("Alert On Early Watch", Group = "12 · ALERTS — CORE", DefaultValue = true)]
        public bool AlertOnEarlyWatch { get; set; }

[Parameter("Alert On Level Hit", Group = "12 · ALERTS — CORE", DefaultValue = true)]
        public bool AlertOnLevelHit { get; set; }

[Parameter("Alert On Invalidated", Group = "12 · ALERTS — CORE", DefaultValue = true)]
        public bool AlertOnInvalidated { get; set; }

[Parameter("Alert Cooldown Seconds", Group = "12 · ALERTS — CORE", DefaultValue = 8, MinValue = 1, MaxValue = 60)]
        public int AlertCooldownSeconds { get; set; }

[Parameter("Suppress Duplicate Alerts", Group = "12 · ALERTS — CORE", DefaultValue = true)]
        public bool SuppressDuplicateAlerts { get; set; }

[Parameter("Enable Email Alerts", Group = "12 · ALERTS — CORE", DefaultValue = false)]
        public bool EnableEmailAlerts { get; set; }

[Parameter("Sender Email", Group = "12 · ALERTS — CORE", DefaultValue = "")]
        public string SenderEmail { get; set; }

[Parameter("Receiver Email", Group = "12 · ALERTS — CORE", DefaultValue = "")]
        public string ReceiverEmail { get; set; }

[Parameter("Enable Auto Trading", Group = "13 · AUTO TRADING", DefaultValue = false)]
        public bool EnableAutoTrading { get; set; }

[Parameter("Enable Automatic Orders", Group = "13 · AUTO TRADING", DefaultValue = false)]
        public bool EnableAutomaticOrders { get; set; }

[Parameter("Pending Order Mode", Group = "13 · AUTO TRADING", DefaultValue = CFIPClean77PendingOrderMode.Adaptive)]
        public CFIPClean77PendingOrderMode PendingOrderMode { get; set; }

[Parameter("Pending Order Expiry Minutes", Group = "13 · AUTO TRADING", DefaultValue = 120, MinValue = 15, MaxValue = 1440)]
        public int PendingOrderExpiryMinutes { get; set; }

[Parameter("Pending Entry Buffer ATR", Group = "13 · AUTO TRADING", DefaultValue = 0.10, MinValue = 0.02, MaxValue = 1.00, Step = 0.01)]
        public double PendingEntryBufferAtr { get; set; }

[Parameter("Pending Minimum Confidence", Group = "13 · AUTO TRADING", DefaultValue = 84, MinValue = 50, MaxValue = 99)]
        public int PendingMinimumConfidence { get; set; }

[Parameter("Pending Minimum Smart Quality", Group = "13 · AUTO TRADING", DefaultValue = 78, MinValue = 50, MaxValue = 95)]
        public int PendingMinimumSmartQuality { get; set; }

[Parameter("Pending Minimum Trend Quality", Group = "13 · AUTO TRADING", DefaultValue = 72, MinValue = 40, MaxValue = 100)]
        public int PendingMinimumTrendQuality { get; set; }

[Parameter("Pending Auto Cleanup", Group = "13 · AUTO TRADING", DefaultValue = true)]
        public bool PendingAutoCleanup { get; set; }

[Parameter("Auto Trading Reminder", Group = "13 · AUTO TRADING", DefaultValue = true)]
        public bool AutoTradingReminder { get; set; }

[Parameter("Reversal Close Minimum Evidence", Group = "13 · AUTO TRADING", DefaultValue = 5, MinValue = 2, MaxValue = 10)]
        public int ReversalCloseMinimumEvidence { get; set; }

[Parameter("Reversal Close Minimum MTF", Group = "13 · AUTO TRADING", DefaultValue = 70, MinValue = 50, MaxValue = 100)]
        public int ReversalCloseMinimumMtf { get; set; }

[Parameter("Confirmed Signals Only", Group = "13 · AUTO TRADING", DefaultValue = true)]
        public bool ConfirmedSignalsOnly { get; set; }

[Parameter("Sizing Mode", Group = "13 · AUTO TRADING", DefaultValue = CFIPClean77SizingMode.RiskPercentEquity)]
        public CFIPClean77SizingMode SizingMode { get; set; }

[Parameter("Risk % Equity", Group = "13 · AUTO TRADING", DefaultValue = 0.50, MinValue = 0.05, MaxValue = 5)]
        public double RiskPercentEquity { get; set; }

[Parameter("Fixed Lots", Group = "13 · AUTO TRADING", DefaultValue = 0.01, MinValue = 0.001, MaxValue = 100, Step = 0.001)]
        public double FixedLots { get; set; }

[Parameter("Minimum Auto Confidence", Group = "13 · AUTO TRADING", DefaultValue = 86, MinValue = 50, MaxValue = 99)]
        public int MinimumAutoConfidence { get; set; }

[Parameter("Minimum Auto Smart Quality", Group = "13 · AUTO TRADING", DefaultValue = 80, MinValue = 50, MaxValue = 95)]
        public int MinimumAutoSmartQuality { get; set; }

[Parameter("Minimum Auto Level Quality", Group = "13 · AUTO TRADING", DefaultValue = 72, MinValue = 40, MaxValue = 100)]
        public int MinimumAutoLevelQuality { get; set; }

[Parameter("Auto TP Stage", Group = "13 · AUTO TRADING", DefaultValue = CFIPClean77TargetStage.TP1)]
        public CFIPClean77TargetStage AutoTpStage { get; set; }

[Parameter("Enable Dynamic TP Advance", Group = "13 · AUTO TRADING", DefaultValue = true)]
        public bool EnableDynamicTpAdvance { get; set; }

[Parameter("TP Advance Proximity Percent", Group = "13 · AUTO TRADING", DefaultValue = 72, MinValue = 50, MaxValue = 98)]
        public double TpAdvanceProximityPercent { get; set; }

[Parameter("Enable Partial Take Profit", Group = "13 · AUTO TRADING", DefaultValue = false)]
        public bool EnablePartialTakeProfit { get; set; }

[Parameter("Partial Close At TP1 Percent", Group = "13 · AUTO TRADING", DefaultValue = 33, MinValue = 0, MaxValue = 90, Step = 1)]
        public double PartialCloseTp1Percent { get; set; }

[Parameter("Partial Close At TP2 Percent", Group = "13 · AUTO TRADING", DefaultValue = 33, MinValue = 0, MaxValue = 90, Step = 1)]
        public double PartialCloseTp2Percent { get; set; }

[Parameter("Move To Break Even After Partial", Group = "13 · AUTO TRADING", DefaultValue = true)]
        public bool MoveToBreakEvenAfterPartial { get; set; }

[Parameter("Enable Reversal Protection Close", Group = "13 · AUTO TRADING", DefaultValue = true)]
        public bool EnableReversalProtectionClose { get; set; }

[Parameter("Reversal Protection Minimum Quality", Group = "13 · AUTO TRADING", DefaultValue = 82, MinValue = 50, MaxValue = 100)]
        public int ReversalProtectionMinimumQuality { get; set; }

[Parameter("Maximum Open Positions", Group = "13 · AUTO TRADING", DefaultValue = 1, MinValue = 1, MaxValue = 20)]
        public int MaximumOpenPositions { get; set; }

[Parameter("Enable Daily Loss Limit", Group = "13 · AUTO TRADING", DefaultValue = true)]
        public bool EnableDailyLossLimit { get; set; }

[Parameter("Maximum Daily Loss Percent", Group = "13 · AUTO TRADING", DefaultValue = 3.0, MinValue = 0.5, MaxValue = 20, Step = 0.5)]
        public double MaximumDailyLossPercent { get; set; }

[Parameter("Use Market Hours Guard", Group = "13 · AUTO TRADING", DefaultValue = true)]
        public bool UseMarketHoursGuard { get; set; }

[Parameter("Use Auto Margin Guard", Group = "13 · AUTO TRADING", DefaultValue = true)]
        public bool UseAutoMarginGuard { get; set; }

[Parameter("Max Auto Margin Usage %", Group = "13 · AUTO TRADING", DefaultValue = 80, MinValue = 10, MaxValue = 100)]
        public double MaxAutoMarginUsagePercent { get; set; }

[Parameter("Margin Buffer %", Group = "13 · AUTO TRADING", DefaultValue = 10, MinValue = 0, MaxValue = 40)]
        public double MarginBufferPercent { get; set; }

[Parameter("Auto Trade Label", Group = "13 · AUTO TRADING", DefaultValue = "CFIP-SMART-CLEAN66")]
        public string AutoTradeLabel { get; set; }

[Parameter("Smart Broker Protection", Group = "13 · AUTO TRADING", DefaultValue = true)]
        public bool AutoBrokerProtection { get; set; }

[Parameter("One Order Per Signal", Group = "13 · AUTO TRADING", DefaultValue = true)]
        public bool OneOrderPerSignal { get; set; }

[Parameter("Enable Market Suitability Guard", Group = "24 · SMART EXECUTION", DefaultValue = true)]
        public bool EnableMarketSuitabilityGuard { get; set; }

[Parameter("Minimum Market Suitability", Group = "24 · SMART EXECUTION", DefaultValue = 68, MinValue = 50, MaxValue = 95)]
        public int MinimumMarketSuitability { get; set; }

[Parameter("Hard Market Suitability Gate", Group = "24 · SMART EXECUTION", DefaultValue = true)]
        public bool HardMarketSuitabilityGate { get; set; }

[Parameter("Require Session Suitability", Group = "24 · SMART EXECUTION", DefaultValue = false)]
        public bool RequireSessionSuitability { get; set; }

[Parameter("Use Smart Risk Scaling", Group = "24 · SMART EXECUTION", DefaultValue = true)]
        public bool UseSmartRiskScaling { get; set; }

[Parameter("Minimum Smart Risk Multiplier", Group = "24 · SMART EXECUTION", DefaultValue = 0.55, MinValue = 0.25, MaxValue = 1.0, Step = 0.05)]
        public double MinimumSmartRiskMultiplier { get; set; }

[Parameter("Full Risk Confidence Threshold", Group = "24 · SMART EXECUTION", DefaultValue = 94, MinValue = 70, MaxValue = 99)]
        public int FullRiskConfidenceThreshold { get; set; }

[Parameter("Full Risk Suitability Threshold", Group = "24 · SMART EXECUTION", DefaultValue = 88, MinValue = 60, MaxValue = 100)]
        public int FullRiskSuitabilityThreshold { get; set; }

[Parameter("Penalize Choppy Regime Risk", Group = "24 · SMART EXECUTION", DefaultValue = true)]
        public bool PenalizeChoppyRegimeRisk { get; set; }

[Parameter("Suitability Recalculation Seconds", Group = "24 · SMART EXECUTION", DefaultValue = 2, MinValue = 1, MaxValue = 10)]
        public int SuitabilityRecalculationSeconds { get; set; }

[Parameter("Show Level Lines", Group = "14 · DISPLAY — CORE", DefaultValue = true)]
        public bool ShowLevelLines { get; set; }

[Parameter("Full Width Level Lines", Group = "14 · DISPLAY — CORE", DefaultValue = true)]
        public bool FullWidthLevelLines { get; set; }

[Parameter("Level Line Thickness", Group = "14 · DISPLAY — CORE", DefaultValue = 1, MinValue = 1, MaxValue = 3)]
        public int LevelLineThickness { get; set; }

[Parameter("Show Entry", Group = "14 · DISPLAY — CORE", DefaultValue = true)]
        public bool ShowEntry { get; set; }

[Parameter("Show Trigger", Group = "14 · DISPLAY — CORE", DefaultValue = true)]
        public bool ShowTrigger { get; set; }

[Parameter("Show SL", Group = "14 · DISPLAY — CORE", DefaultValue = true)]
        public bool ShowSL { get; set; }

[Parameter("Show TP1", Group = "14 · DISPLAY — CORE", DefaultValue = true)]
        public bool ShowTP1 { get; set; }

[Parameter("Show TP2", Group = "14 · DISPLAY — CORE", DefaultValue = true)]
        public bool ShowTP2 { get; set; }

[Parameter("Show TP3", Group = "14 · DISPLAY — CORE", DefaultValue = true)]
        public bool ShowTP3 { get; set; }

[Parameter("Show TP4", Group = "14 · DISPLAY — CORE", DefaultValue = true)]
        public bool ShowTP4 { get; set; }

[Parameter("Show Signal Arrow", Group = "14 · DISPLAY — CORE", DefaultValue = true)]
        public bool ShowSignalArrow { get; set; }

[Parameter("Show Early Watch", Group = "14 · DISPLAY — CORE", DefaultValue = true)]
        public bool ShowEarlyWatch { get; set; }

[Parameter("Show Historical Signals", Group = "14 · DISPLAY — CORE", DefaultValue = false)]
        public bool ShowHistoricalSignals { get; set; }

[Parameter("Historical Signal Limit", Group = "14 · DISPLAY — CORE", DefaultValue = 10, MinValue = 1, MaxValue = 50)]
        public int HistoricalSignalLimit { get; set; }

[Parameter("Show Unified Panel", Group = "14 · DISPLAY — CORE", DefaultValue = true)]
        public bool ShowUnifiedPanel { get; set; }

[Parameter("Show Panel Background", Group = "14 · DISPLAY — CORE", DefaultValue = true)]
        public bool ShowPanelBackground { get; set; }

[Parameter("Panel Position", Group = "14 · DISPLAY — CORE", DefaultValue = CFIPClean77PanelCorner.BottomLeft)]
        public CFIPClean77PanelCorner PanelPosition { get; set; }

[Parameter("Panel Width", Group = "14 · DISPLAY — CORE", DefaultValue = 430, MinValue = 220, MaxValue = 700)]
        public int PanelWidth { get; set; }

[Parameter("Panel Font Size", Group = "14 · DISPLAY — CORE", DefaultValue = 11, MinValue = 8, MaxValue = 20)]
        public int PanelFontSize { get; set; }

[Parameter("Panel Font Family", Group = "14 · DISPLAY — CORE", DefaultValue = "Arial")]
        public string PanelFontFamily { get; set; }

[Parameter("Panel Bold", Group = "14 · DISPLAY — CORE", DefaultValue = false)]
        public bool PanelBold { get; set; }

[Parameter("Panel Background", Group = "14 · DISPLAY — CORE", DefaultValue = "Black")]
        public Color PanelBackground { get; set; }

[Parameter("Panel Background Alpha", Group = "14 · DISPLAY — CORE", DefaultValue = 145, MinValue = 0, MaxValue = 255)]
        public int PanelBackgroundAlpha { get; set; }

[Parameter("Panel Border", Group = "14 · DISPLAY — CORE", DefaultValue = "#3A4656")]
        public Color PanelBorder { get; set; }

[Parameter("Panel Border Alpha", Group = "14 · DISPLAY — CORE", DefaultValue = 230, MinValue = 0, MaxValue = 255)]
        public int PanelBorderAlpha { get; set; }

[Parameter("Panel Border Thickness", Group = "14 · DISPLAY — CORE", DefaultValue = 1, MinValue = 0, MaxValue = 4)]
        public int PanelBorderThickness { get; set; }

[Parameter("Panel Corner Radius", Group = "14 · DISPLAY — CORE", DefaultValue = 5, MinValue = 0, MaxValue = 20)]
        public int PanelCornerRadius { get; set; }

[Parameter("Panel Padding", Group = "14 · DISPLAY — CORE", DefaultValue = 9, MinValue = 0, MaxValue = 30)]
        public int PanelPadding { get; set; }

[Parameter("Panel Margin", Group = "14 · DISPLAY — CORE", DefaultValue = 8, MinValue = 0, MaxValue = 30)]
        public int PanelMargin { get; set; }

[Parameter("Panel Row Gap", Group = "14 · DISPLAY — CORE", DefaultValue = 1, MinValue = 0, MaxValue = 6)]
        public int PanelRowGap { get; set; }

[Parameter("Panel Max Height", Group = "14 · DISPLAY — CORE", DefaultValue = 650, MinValue = 260, MaxValue = 1200)]
        public int PanelMaxHeight { get; set; }

[Parameter("Panel Row Padding", Group = "14 · DISPLAY — CORE", DefaultValue = 3, MinValue = 0, MaxValue = 12)]
        public int PanelRowPadding { get; set; }

[Parameter("Panel Button Gap", Group = "14 · DISPLAY — CORE", DefaultValue = 4, MinValue = 0, MaxValue = 16)]
        public int PanelButtonGap { get; set; }

[Parameter("Panel Accent Color", Group = "14 · DISPLAY — CORE", DefaultValue = "#4A90E2")]
        public Color PanelAccentColor { get; set; }

[Parameter("Panel Section Color", Group = "14 · DISPLAY — CORE", DefaultValue = "#8FA3B8")]
        public Color PanelSectionColor { get; set; }

[Parameter("Panel Secondary Text Color", Group = "14 · DISPLAY — CORE", DefaultValue = "#C5CBD3")]
        public Color PanelSecondaryTextColor { get; set; }

[Parameter("Panel Muted Text Color", Group = "14 · DISPLAY — CORE", DefaultValue = "#8A95A5")]
        public Color PanelMutedTextColor { get; set; }

[Parameter("Panel Warning Color", Group = "14 · DISPLAY — CORE", DefaultValue = "Orange")]
        public Color PanelWarningColor { get; set; }

[Parameter("Panel Text Color", Group = "14 · DISPLAY — CORE", DefaultValue = "White")]
        public Color PanelTextColor { get; set; }

[Parameter("Entry Line Color", Group = "14 · DISPLAY — CORE", DefaultValue = "White")]
        public Color EntryLineColor { get; set; }

[Parameter("Trigger Line Color", Group = "14 · DISPLAY — CORE", DefaultValue = "Orange")]
        public Color TriggerLineColor { get; set; }

[Parameter("SL Line Color", Group = "14 · DISPLAY — CORE", DefaultValue = "Red")]
        public Color SlLineColor { get; set; }

[Parameter("TP1 Line Color", Group = "14 · DISPLAY — CORE", DefaultValue = "Lime")]
        public Color TpLineColor { get; set; }

[Parameter("TP2 Line Color", Group = "14 · DISPLAY — CORE", DefaultValue = "SpringGreen")]
        public Color Tp2LineColor { get; set; }

[Parameter("TP3 Line Color", Group = "14 · DISPLAY — CORE", DefaultValue = "Turquoise")]
        public Color Tp3LineColor { get; set; }

[Parameter("TP4 Line Color", Group = "14 · DISPLAY — CORE", DefaultValue = "Gold")]
        public Color Tp4LineColor { get; set; }

[Parameter("BUY Arrow Color", Group = "14 · DISPLAY — CORE", DefaultValue = "Lime")]
        public Color BuyArrowColor { get; set; }

[Parameter("SELL Arrow Color", Group = "14 · DISPLAY — CORE", DefaultValue = "Red")]
        public Color SellArrowColor { get; set; }

[Parameter("Live Trigger Score", Group = "15 · CONTROL — ADVANCED", DefaultValue = 4, MinValue = 1, MaxValue = 6)]
        public int LiveTriggerScore { get; set; }

[Parameter("Precision Trigger Score", Group = "15 · CONTROL — ADVANCED", DefaultValue = 5, MinValue = 2, MaxValue = 6)]
        public int PrecisionTriggerScore { get; set; }

[Parameter("Allow Strong M5 Trigger Override", Group = "15 · CONTROL — ADVANCED", DefaultValue = true)]
        public bool AllowStrongM5TriggerOverride { get; set; }

[Parameter("M5 Only Confirmed Trigger", Group = "15 · CONTROL — ADVANCED", DefaultValue = true)]
        public bool M5OnlyConfirmedTrigger { get; set; }

[Parameter("Allow M15 Neutral Pullback", Group = "15 · CONTROL — ADVANCED", DefaultValue = true)]
        public bool AllowM15NeutralPullback { get; set; }

[Parameter("Higher TF Penalty", Group = "15 · CONTROL — ADVANCED", DefaultValue = 7, MinValue = 0, MaxValue = 20)]
        public int HigherTfPenalty { get; set; }

[Parameter("Use Zone Confluence", Group = "15 · CONTROL — ADVANCED", DefaultValue = true)]
        public bool UseZoneConfluence { get; set; }

[Parameter("Use Higher TF Liquidity Targets", Group = "15 · CONTROL — ADVANCED", DefaultValue = true)]
        public bool UseHigherTfLiquidityTargets { get; set; }

[Parameter("Minimum HTF Target RR", Group = "15 · CONTROL — ADVANCED", DefaultValue = 2.50, MinValue = 1, MaxValue = 20)]
        public double MinimumHtfTargetRR { get; set; }

[Parameter("Structural TP RR Step", Group = "15 · CONTROL — ADVANCED", DefaultValue = 0.50, MinValue = 0.10, MaxValue = 2.0, Step = 0.05)]
        public double StructuralTpRrStep { get; set; }

[Parameter("Minimum Trade RR", Group = "15 · CONTROL — ADVANCED", DefaultValue = 2.00, MinValue = 0.5, MaxValue = 20)]
        public double MinimumTradeRR { get; set; }

[Parameter("Use RR Filter", Group = "15 · CONTROL — ADVANCED", DefaultValue = true)]
        public bool UseRRFilter { get; set; }

[Parameter("Avoid Late Entry", Group = "15 · CONTROL — ADVANCED", DefaultValue = true)]
        public bool AvoidLateEntry { get; set; }

[Parameter("Use Precision Execution Model", Group = "15 · CONTROL — ADVANCED", DefaultValue = true)]
        public bool UsePrecisionExecutionModel { get; set; }

[Parameter("Stop Buffer ATR", Group = "15 · CONTROL — ADVANCED", DefaultValue = 0.10, MinValue = 0.01, MaxValue = 1.0)]
        public double StopBufferAtr { get; set; }

[Parameter("Require HTF Targets", Group = "15 · CONTROL — ADVANCED", DefaultValue = false)]
        public bool RequireHtfTargets { get; set; }

[Parameter("Maximum Structural Stop ATR", Group = "15 · CONTROL — ADVANCED", DefaultValue = 2.25, MinValue = 0.5, MaxValue = 10)]
        public double MaximumStructuralStopAtr { get; set; }

[Parameter("Target Obstacle Lookback Bars", Group = "15 · CONTROL — ADVANCED", DefaultValue = 8, MinValue = 3, MaxValue = 50)]
        public int TargetObstacleLookbackBars { get; set; }

[Parameter("Allow Direct Displacement Override", Group = "15 · CONTROL — ADVANCED", DefaultValue = true)]
        public bool AllowDirectDisplacementOverride { get; set; }

[Parameter("Direct Displacement Override Score", Group = "15 · CONTROL — ADVANCED", DefaultValue = 6, MinValue = 3, MaxValue = 6)]
        public int DirectDisplacementOverrideScore { get; set; }

[Parameter("Maximum Setup Age Bars", Group = "15 · CONTROL — ADVANCED", DefaultValue = 8, MinValue = 1, MaxValue = 50)]
        public int MaximumSetupAgeBars { get; set; }

[Parameter("Allow Synthetic Target Fallback", Group = "15 · CONTROL — ADVANCED", DefaultValue = true)]
        public bool AllowSyntheticTargetFallback { get; set; }

[Parameter("HTF Stop Buffer ATR", Group = "15 · CONTROL — ADVANCED", DefaultValue = 0.15, MinValue = 0.01, MaxValue = 1.0)]
        public double HtfStopBufferAtr { get; set; }

[Parameter("Block New Signal While Active", Group = "15 · CONTROL — ADVANCED", DefaultValue = true)]
        public bool BlockNewSignalWhileActive { get; set; }

[Parameter("Cooldown Bars", Group = "15 · CONTROL — ADVANCED", DefaultValue = 3, MinValue = 0, MaxValue = 50)]
        public int CooldownBars { get; set; }

[Parameter("Use News Event Guard", Group = "15 · CONTROL — ADVANCED", DefaultValue = true)]
        public bool UseNewsEventGuard { get; set; }

[Parameter("Use Volatility Event Guard", Group = "15 · CONTROL — ADVANCED", DefaultValue = true)]
        public bool UseVolatilityEventGuard { get; set; }

[Parameter("Event Guard Cooldown Bars", Group = "15 · CONTROL — ADVANCED", DefaultValue = 3, MinValue = 0, MaxValue = 50)]
        public int EventGuardCooldownBars { get; set; }

[Parameter("Use Regime No-Trade Guard", Group = "15 · CONTROL — ADVANCED", DefaultValue = true)]
        public bool UseRegimeNoTradeGuard { get; set; }

[Parameter("Show Reaction Arrow", Group = "15 · CONTROL — ADVANCED", DefaultValue = true)]
        public bool ShowReactionArrow { get; set; }

[Parameter("Show Historical Arrows", Group = "15 · CONTROL — ADVANCED", DefaultValue = true)]
        public bool ShowHistoricalArrows { get; set; }

[Parameter("Enable Dynamic Structural Stop Alias", Group = "15 · CONTROL — ADVANCED", DefaultValue = true)]
        public bool EnableDynamicSlTrail { get; set; }

[Parameter("Structural Stop Breathing ATR", Group = "15 · CONTROL — ADVANCED", DefaultValue = 0.85, MinValue = 0.20, MaxValue = 5)]
        public double TrailDistanceAtr { get; set; }

[Parameter("Structural Stop Step ATR", Group = "15 · CONTROL — ADVANCED", DefaultValue = 0.08, MinValue = 0.01, MaxValue = 1)]
        public double TrailStepAtr { get; set; }

[Parameter("Target Update Step ATR", Group = "15 · CONTROL — ADVANCED", DefaultValue = 0.20, MinValue = 0.02, MaxValue = 2)]
        public double TargetUpdateStepAtr { get; set; }

[Parameter("Use Swing Structure In Structural Stop", Group = "15 · CONTROL — ADVANCED", DefaultValue = true)]
        public bool UseSwingStructureInTrail { get; set; }

[Parameter("Smart Minimum Independent Evidence", Group = "15 · CONTROL — ADVANCED", DefaultValue = 4, MinValue = 2, MaxValue = 8)]
        public int SmartMinimumIndependentEvidence { get; set; }

[Parameter("Smart Stop Zone Bonus", Group = "15 · CONTROL — ADVANCED", DefaultValue = 10, MinValue = 0, MaxValue = 30)]
        public int SmartStopZoneBonus { get; set; }

[Parameter("Smart Liquidity Pool Bonus", Group = "15 · CONTROL — ADVANCED", DefaultValue = 12, MinValue = 0, MaxValue = 30)]
        public int SmartLiquidityPoolBonus { get; set; }

[Parameter("Smart Trail Minimum RR", Group = "15 · CONTROL — ADVANCED", DefaultValue = 1.00, MinValue = 0.5, MaxValue = 10)]
        public double SmartTrailMinimumRR { get; set; }

[Parameter("Smart Use Closed-Bar Decision (Safety-Enforced)", Group = "15 · CONTROL — ADVANCED", DefaultValue = true)]
        public bool SmartUseClosedBarDecision { get; set; }

[Parameter("Smart Target Nearest Bias", Group = "15 · CONTROL — ADVANCED", DefaultValue = 0.65, MinValue = 0.20, MaxValue = 1.0, Step = 0.05)]
        public double SmartTargetNearestBias { get; set; }

[Parameter("Require Smart Consensus", Group = "15 · CONTROL — ADVANCED", DefaultValue = true)]
        public bool RequireSmartConsensus { get; set; }

[Parameter("Smart Strong Setup Quality", Group = "15 · CONTROL — ADVANCED", DefaultValue = 82, MinValue = 60, MaxValue = 99)]
        public int SmartStrongSetupQuality { get; set; }

[Parameter("Smart Strong Setup Edge", Group = "15 · CONTROL — ADVANCED", DefaultValue = 10, MinValue = 4, MaxValue = 30)]
        public int SmartStrongSetupEdge { get; set; }

[Parameter("Smart Flip Confirmation Bars", Group = "15 · CONTROL — ADVANCED", DefaultValue = 2, MinValue = 1, MaxValue = 5)]
        public int SmartFlipConfirmationBars { get; set; }

[Parameter("Allow Smart Soft Gate", Group = "15 · CONTROL — ADVANCED", DefaultValue = true)]
        public bool AllowSmartSoftGate { get; set; }

[Parameter("Enable Fast Reversal Intelligence", Group = "15 · CONTROL — ADVANCED", DefaultValue = true)]
        public bool EnableFastReversalIntelligence { get; set; }

[Parameter("Fast Reversal Minimum Quality", Group = "15 · CONTROL — ADVANCED", DefaultValue = 74, MinValue = 50, MaxValue = 95)]
        public int FastReversalMinimumQuality { get; set; }

[Parameter("Fast Reversal Lookback Bars", Group = "15 · CONTROL — ADVANCED", DefaultValue = 6, MinValue = 3, MaxValue = 15)]
        public int FastReversalLookbackBars { get; set; }

[Parameter("Fast Reversal Minimum Zone Quality", Group = "15 · CONTROL — ADVANCED", DefaultValue = 60, MinValue = 40, MaxValue = 90)]
        public int FastReversalMinimumZoneQuality { get; set; }

[Parameter("Allow Fast M5 Reversal Before M15", Group = "15 · CONTROL — ADVANCED", DefaultValue = true)]
        public bool AllowFastM5ReversalBeforeM15 { get; set; }

[Parameter("Retest Lookback Bars", Group = "15 · CONTROL — ADVANCED", DefaultValue = 8, MinValue = 3, MaxValue = 30)]
        public int RetestLookbackBars { get; set; }

[Parameter("Retest Max Bars After Displacement", Group = "15 · CONTROL — ADVANCED", DefaultValue = 6, MinValue = 1, MaxValue = 20)]
        public int RetestMaxBarsAfterDisplacement { get; set; }

[Parameter("Retest Zone Tolerance ATR", Group = "15 · CONTROL — ADVANCED", DefaultValue = 0.25, MinValue = 0.05, MaxValue = 1)]
        public double RetestZoneToleranceAtr { get; set; }

[Parameter("Retest Rejection Body ATR", Group = "15 · CONTROL — ADVANCED", DefaultValue = 0.12, MinValue = 0.02, MaxValue = 1)]
        public double RetestRejectionBodyAtr { get; set; }

[Parameter("Require Retest Close Confirmation", Group = "15 · CONTROL — ADVANCED", DefaultValue = true)]
        public bool RequireRetestCloseConfirmation { get; set; }

[Parameter("Use Extended Liquidity Map", Group = "15 · CONTROL — ADVANCED", DefaultValue = true)]
        public bool UseExtendedLiquidityMap { get; set; }

[Parameter("Use Session Liquidity Targets", Group = "15 · CONTROL — ADVANCED", DefaultValue = true)]
        public bool UseSessionLiquidityTargets { get; set; }

[Parameter("Liquidity Target Minimum Score", Group = "15 · CONTROL — ADVANCED", DefaultValue = 72, MinValue = 40, MaxValue = 100)]
        public int LiquidityTargetMinimumScore { get; set; }

[Parameter("Target Obstacle Buffer ATR", Group = "15 · CONTROL — ADVANCED", DefaultValue = 0.10, MinValue = 0.01, MaxValue = 1)]
        public double TargetObstacleBufferAtr { get; set; }

[Parameter("Require Obstacle Free TP1", Group = "15 · CONTROL — ADVANCED", DefaultValue = true)]
        public bool RequireObstacleFreeTp1 { get; set; }

[Parameter("Use Volume Expansion", Group = "20 · Confluence Extensions", DefaultValue = false)]
        public bool UseVolumeExpansion { get; set; }

[Parameter("Volume Expansion Ratio", Group = "20 · Confluence Extensions", DefaultValue = 1.15, MinValue = 1.0, MaxValue = 3.0, Step = 0.05)]
        public double VolumeExpansionRatio { get; set; }

[Parameter("Use MACD Bias", Group = "20 · Confluence Extensions", DefaultValue = false)]
        public bool UseMacdBias { get; set; }

[Parameter("MACD Fast Period", Group = "20 · Confluence Extensions", DefaultValue = 12, MinValue = 2, MaxValue = 50)]
        public int MacdFastPeriod { get; set; }

[Parameter("MACD Slow Period", Group = "20 · Confluence Extensions", DefaultValue = 26, MinValue = 3, MaxValue = 100)]
        public int MacdSlowPeriod { get; set; }

[Parameter("Use VWAP Bias", Group = "20 · Confluence Extensions", DefaultValue = false)]
        public bool UseVwapBias { get; set; }

[Parameter("VWAP Lookback Bars", Group = "20 · Confluence Extensions", DefaultValue = 48, MinValue = 10, MaxValue = 200)]
        public int VwapLookbackBars { get; set; }

[Parameter("Use Healthy Volatility", Group = "20 · Confluence Extensions", DefaultValue = false)]
        public bool UseHealthyVolatility { get; set; }

[Parameter("Healthy ATR Minimum Ratio", Group = "20 · Confluence Extensions", DefaultValue = 0.85, MinValue = 0.50, MaxValue = 1.50, Step = 0.05)]
        public double HealthyAtrMinimumRatio { get; set; }

[Parameter("Healthy ATR Maximum Ratio", Group = "20 · Confluence Extensions", DefaultValue = 1.80, MinValue = 1.0, MaxValue = 3.0, Step = 0.05)]
        public double HealthyAtrMaximumRatio { get; set; }

[Parameter("Early Setup Confidence", Group = "21 · Complete Intelligence", DefaultValue = 52, MinValue = 40, MaxValue = 95)]
        public int EarlySetupConfidence { get; set; }

[Parameter("Enable Live Reaction", Group = "21 · Complete Intelligence", DefaultValue = true)]
        public bool EnableLiveReaction { get; set; }

[Parameter("Live Reaction Watch Threshold", Group = "21 · Complete Intelligence", DefaultValue = 56, MinValue = 40, MaxValue = 95)]
        public int LiveReactionWatchThreshold { get; set; }

[Parameter("Live Reaction Threshold", Group = "21 · Complete Intelligence", DefaultValue = 68, MinValue = 40, MaxValue = 95)]
        public int LiveReactionThreshold { get; set; }

[Parameter("Live Reaction Strong Threshold", Group = "21 · Complete Intelligence", DefaultValue = 82, MinValue = 50, MaxValue = 99)]
        public int LiveReactionStrongThreshold { get; set; }

[Parameter("Minimum Live Reaction Evidence", Group = "21 · Complete Intelligence", DefaultValue = 3, MinValue = 1, MaxValue = 8)]
        public int MinimumLiveReactionEvidence { get; set; }

[Parameter("Use Volume Expansion Evidence", Group = "21 · Complete Intelligence", DefaultValue = true)]
        public bool UseVolumeExpansionEvidence { get; set; }

[Parameter("Use MACD Evidence", Group = "21 · Complete Intelligence", DefaultValue = true)]
        public bool UseMacdEvidence { get; set; }

[Parameter("Use VWAP Evidence", Group = "21 · Complete Intelligence", DefaultValue = true)]
        public bool UseVwapEvidence { get; set; }

[Parameter("Use Healthy Volatility Evidence", Group = "21 · Complete Intelligence", DefaultValue = true)]
        public bool UseHealthyVolatilityEvidence { get; set; }

[Parameter("Minimum Smart Direction Share", Group = "21 · Complete Intelligence", DefaultValue = 57, MinValue = 50, MaxValue = 95)]
        public int MinimumSmartDirectionShare { get; set; }

[Parameter("Adaptive Smart Thresholds", Group = "21 · Complete Intelligence", DefaultValue = true)]
        public bool AdaptiveSmartThresholds { get; set; }

[Parameter("Smart Regime Buffer", Group = "21 · Complete Intelligence", DefaultValue = 6, MinValue = 0, MaxValue = 15)]
        public int SmartRegimeBuffer { get; set; }

[Parameter("Smart Score Temperature", Group = "21 · Complete Intelligence", DefaultValue = 12.0, MinValue = 1.0, MaxValue = 50.0, Step = 0.5)]
        public double SmartScoreTemperature { get; set; }

[Parameter("Smart Consensus Threshold", Group = "21 · Complete Intelligence", DefaultValue = 57, MinValue = 50, MaxValue = 95)]
        public int SmartConsensusThreshold { get; set; }

[Parameter("Adaptive Regime Weighting", Group = "21 · Complete Intelligence", DefaultValue = true)]
        public bool AdaptiveRegimeWeighting { get; set; }

[Parameter("Use Multi TF Level Map", Group = "21 · Complete Intelligence", DefaultValue = true)]
        public bool UseMultiTfLevelMap { get; set; }

[Parameter("Smart Level Cluster ATR", Group = "21 · Complete Intelligence", DefaultValue = 0.10, MinValue = 0.02, MaxValue = 1.0, Step = 0.01)]
        public double SmartLevelClusterAtr { get; set; }

[Parameter("Smart Target Quality", Group = "21 · Complete Intelligence", DefaultValue = 58, MinValue = 40, MaxValue = 100)]
        public int SmartTargetQuality { get; set; }

[Parameter("Smart Stop Quality", Group = "21 · Complete Intelligence", DefaultValue = 55, MinValue = 40, MaxValue = 100)]
        public int SmartStopQuality { get; set; }

[Parameter("Smart Exit Pressure Threshold", Group = "21 · Complete Intelligence", DefaultValue = 65, MinValue = 40, MaxValue = 95)]
        public int SmartExitPressureThreshold { get; set; }

[Parameter("Smart Alert Cooldown Seconds", Group = "21 · Complete Intelligence", DefaultValue = 8, MinValue = 1, MaxValue = 60)]
        public int SmartAlertCooldownSeconds { get; set; }

[Parameter("Smart Weekly Context", Group = "21 · Complete Intelligence", DefaultValue = true)]
        public bool SmartWeeklyContext { get; set; }

[Parameter("Block Same-Bar Reentry After Exit", Group = "21 · Complete Intelligence", DefaultValue = true)]
        public bool BlockSameBarReentryAfterExit { get; set; }

[Parameter("Allow Execution Frame Stop Fallback", Group = "21 · Complete Intelligence", DefaultValue = true)]
        public bool AllowExecutionFrameStopFallback { get; set; }

[Parameter("Use Historical Choppiness Guard", Group = "21 · Complete Intelligence", DefaultValue = true)]
        public bool UseHistoricalChoppinessGuard { get; set; }

[Parameter("Alert On News Event", Group = "21 · Complete Intelligence", DefaultValue = true)]
        public bool AlertOnNewsEvent { get; set; }

[Parameter("Alert On Session Block", Group = "21 · Complete Intelligence", DefaultValue = false)]
        public bool AlertOnSessionBlock { get; set; }

[Parameter("Alert On Spread Block", Group = "21 · Complete Intelligence", DefaultValue = false)]
        public bool AlertOnSpreadBlock { get; set; }

[Parameter("Alert On Friday Block", Group = "21 · Complete Intelligence", DefaultValue = false)]
        public bool AlertOnFridayBlock { get; set; }

[Parameter("Alert On Regime No Trade", Group = "21 · Complete Intelligence", DefaultValue = false)]
        public bool AlertOnRegimeNoTrade { get; set; }

[Parameter("Alert On Cooldown Block", Group = "21 · Complete Intelligence", DefaultValue = false)]
        public bool AlertOnCooldownBlock { get; set; }

[Parameter("Alert On Exit Plan Update", Group = "21 · Complete Intelligence", DefaultValue = true)]
        public bool AlertOnExitPlanUpdate { get; set; }

[Parameter("Show Engine Status", Group = "21 · Complete Intelligence", DefaultValue = true)]
        public bool ShowEngineStatus { get; set; }

[Parameter("Panel State Hold Seconds", Group = "21 · Complete Intelligence", DefaultValue = 2, MinValue = 0, MaxValue = 10)]
        public int PanelStateHoldSeconds { get; set; }

[Parameter("Show Level Prices In Unified Panel", Group = "21 · Complete Intelligence", DefaultValue = true)]
        public bool ShowLevelPricesInUnifiedPanel { get; set; }

[Parameter("Show Trade Plan Panel", Group = "21 · Complete Intelligence", DefaultValue = true)]
        public bool ShowTradePlanPanel { get; set; }

// Compatibility parameter retained for preset visibility only.
        // The clean architecture has no manual BUY/SELL/order-placement authority.
        [Parameter("Show Trade Action Buttons", Group = "13 · AUTO TRADING", DefaultValue = true)]
        public bool ShowTradeActionButtons { get; set; }

[Parameter("Always Show Safety Buttons", Group = "13 · AUTO TRADING", DefaultValue = true)]
        public bool AlwaysShowSafetyButtons { get; set; }

[Parameter("Action Button Margin", Group = "13 · AUTO TRADING", DefaultValue = 2, MinValue = 0, MaxValue = 20)]
        public int ActionButtonMargin { get; set; }

[Parameter("Use Authoritative Signal State", Group = "21 · Complete Intelligence", DefaultValue = true)]
        public bool UseAuthoritativeSignalState { get; set; }

[Parameter("Require Plan Integrity", Group = "21 · Complete Intelligence", DefaultValue = true)]
        public bool RequirePlanIntegrity { get; set; }

[Parameter("Maximum Spread / Stop Risk Ratio", Group = "21 · Complete Intelligence", DefaultValue = 0.18, MinValue = 0.02, MaxValue = 0.50, Step = 0.01)]
        public double MaximumSpreadToStopRiskRatio { get; set; }

[Parameter("Minimum Smart Target Quality For TP1", Group = "21 · Complete Intelligence", DefaultValue = 55, MinValue = 40, MaxValue = 95)]
        public int MinimumSmartTargetQualityForTp1 { get; set; }

[Parameter("Prediction Color", Group = "21 · Complete Intelligence", DefaultValue = "#4A90E2")]
        public Color PredictionColor { get; set; }

[Parameter("Strong BUY Arrow Color", Group = "21 · Complete Intelligence", DefaultValue = "Lime")]
        public Color StrongBuyArrowColor { get; set; }

[Parameter("Strong SELL Arrow Color", Group = "21 · Complete Intelligence", DefaultValue = "Red")]
        public Color StrongSellArrowColor { get; set; }

[Parameter("Confirmed BUY Arrow Color", Group = "21 · Complete Intelligence", DefaultValue = "Lime")]
        public Color ConfirmedBuyArrowColor { get; set; }

[Parameter("Confirmed SELL Arrow Color", Group = "21 · Complete Intelligence", DefaultValue = "Red")]
        public Color ConfirmedSellArrowColor { get; set; }

[Parameter("Caution BUY Arrow Color", Group = "21 · Complete Intelligence", DefaultValue = "#9AA7B4")]
        public Color CautionBuyArrowColor { get; set; }

[Parameter("Caution SELL Arrow Color", Group = "21 · Complete Intelligence", DefaultValue = "#9AA7B4")]
        public Color CautionSellArrowColor { get; set; }

[Parameter("Blocked / Reaction Arrow Color", Group = "21 · Complete Intelligence", DefaultValue = "#9AA7B4")]
        public Color BlockedReactionArrowColor { get; set; }

[Parameter("Fallback TP1 RR", Group = "21 · Complete Intelligence", DefaultValue = 2.0, MinValue = 0.5, MaxValue = 10, Step = 0.05)]
        public double FallbackTp1RR { get; set; }

[Parameter("Fallback TP2 RR", Group = "21 · Complete Intelligence", DefaultValue = 3.2, MinValue = 0.8, MaxValue = 20, Step = 0.05)]
        public double FallbackTp2RR { get; set; }

[Parameter("Fallback TP3 RR", Group = "21 · Complete Intelligence", DefaultValue = 4.8, MinValue = 1.0, MaxValue = 30, Step = 0.05)]
        public double FallbackTp3RR { get; set; }

[Parameter("Fallback TP4 RR", Group = "21 · Complete Intelligence", DefaultValue = 6.5, MinValue = 1.5, MaxValue = 40, Step = 0.05)]
        public double FallbackTp4RR { get; set; }

[Parameter("Require Precision Entry", Group = "23 · Structural Execution", DefaultValue = true)]
        public bool RequirePrecisionEntry { get; set; }

[Parameter("Execution Zone ATR", Group = "23 · Structural Execution", DefaultValue = 0.35, MinValue = 0.05, MaxValue = 1.50, Step = 0.01)]
        public double ExecutionZoneAtr { get; set; }

[Parameter("Minimum Entry Quality", Group = "23 · Structural Execution", DefaultValue = 72, MinValue = 40, MaxValue = 100)]
        public int MinimumEntryQuality { get; set; }

[Parameter("Maximum Entry Distance ATR", Group = "23 · Structural Execution", DefaultValue = 0.45, MinValue = 0.05, MaxValue = 2.0, Step = 0.01)]
        public double MaximumEntryDistanceAtr { get; set; }

[Parameter("Allow Precision Breakout Entry", Group = "23 · Structural Execution", DefaultValue = true)]
        public bool AllowPrecisionBreakoutEntry { get; set; }

[Parameter("Precision Breakout Buffer ATR", Group = "23 · Structural Execution", DefaultValue = 0.08, MinValue = 0.01, MaxValue = 0.50, Step = 0.01)]
        public double PrecisionBreakoutBufferAtr { get; set; }

[Parameter("Minimum Targets For Plan", Group = "23 · Structural Execution", DefaultValue = 1, MinValue = 1, MaxValue = 4)]
        public int MinimumTargetsForPlan { get; set; }

[Parameter("Require HTF Reward For TP1", Group = "23 · Structural Execution", DefaultValue = false)]
        public bool RequireHtfRewardForTp1 { get; set; }

[Parameter("Require HTF Reward For TP2+", Group = "23 · Structural Execution", DefaultValue = true)]
        public bool RequireHtfRewardForTp2Plus { get; set; }

[Parameter("Minimum HTF Reward Quality", Group = "23 · Structural Execution", DefaultValue = 68, MinValue = 40, MaxValue = 100)]
        public int MinimumHtfRewardQuality { get; set; }

[Parameter("Maximum Reward RR", Group = "23 · Structural Execution", DefaultValue = 12.0, MinValue = 2.0, MaxValue = 40, Step = 0.10)]
        public double MaximumRewardRR { get; set; }

[Parameter("HTF Reward Bonus", Group = "23 · Structural Execution", DefaultValue = 22, MinValue = 0, MaxValue = 40)]
        public int HtfRewardBonus { get; set; }

[Parameter("Liquidity Reward Bonus", Group = "23 · Structural Execution", DefaultValue = 14, MinValue = 0, MaxValue = 40)]
        public int LiquidityRewardBonus { get; set; }

[Parameter("Zone Reward Bonus", Group = "23 · Structural Execution", DefaultValue = 10, MinValue = 0, MaxValue = 30)]
        public int ZoneRewardBonus { get; set; }

[Parameter("Preferred Stop Risk ATR", Group = "23 · Structural Execution", DefaultValue = 1.00, MinValue = 0.25, MaxValue = 3.0, Step = 0.05)]
        public double PreferredStopRiskAtr { get; set; }

[Parameter("Stop Risk Balance Weight", Group = "23 · Structural Execution", DefaultValue = 18, MinValue = 0, MaxValue = 40)]
        public int StopRiskBalanceWeight { get; set; }

[Parameter("Minimum Structural Stop Quality", Group = "23 · Structural Execution", DefaultValue = 65, MinValue = 40, MaxValue = 100)]
        public int MinimumStructuralStopQuality { get; set; }

[Parameter("Structural Stop Management Only", Group = "23 · Structural Execution", DefaultValue = true)]
        public bool StructuralStopManagementOnly { get; set; }

[Parameter("Structural Target Updates Only", Group = "23 · Structural Execution", DefaultValue = true)]
        public bool StructuralTargetUpdatesOnly { get; set; }

[Parameter("Use M5 Structure For Initial Stop", Group = "22 · Safety & Precision", DefaultValue = true)]
        public bool UseM5StructureForStop { get; set; }

[Parameter("Use Zone Mitigation Guard", Group = "22 · Safety & Precision", DefaultValue = true)]
        public bool UseZoneMitigationGuard { get; set; }

[Parameter("Require Order Block Retest", Group = "22 · Safety & Precision", DefaultValue = false)]
        public bool RequireObRetest { get; set; }

[Parameter("FVG Invalidate On Full Fill (Safety-Enforced)", Group = "22 · Safety & Precision", DefaultValue = true)]
        public bool FvgInvalidateOnFullFill { get; set; }

[Parameter("FVG Partial Mitigation", Group = "22 · Safety & Precision", DefaultValue = true)]
        public bool EnableFvgPartialMitigation { get; set; }

[Parameter("FVG Break By Wicks", Group = "22 · Safety & Precision", DefaultValue = false)]
        public bool FvgBreakByWicks { get; set; }

[Parameter("Include Spread In Risk Sizing", Group = "13 · AUTO TRADING", DefaultValue = true)]
        public bool IncludeSpreadInRiskSizing { get; set; }

[Parameter("Use Semantic Alert Sounds", Group = "22 · Safety & Precision", DefaultValue = true)]
        public bool UseSemanticAlertSounds { get; set; }

[Parameter("Managed Actions Only", Group = "13 · AUTO TRADING", DefaultValue = true)]
        public bool ManagedActionsOnly { get; set; }

[Parameter("Show Spread Diagnostics", Group = "22 · Safety & Precision", DefaultValue = true)]
        public bool ShowSpreadDiagnostics { get; set; }

[Parameter("Enable Early Prediction", Group = "15 · INTELLIGENCE — EARLY", DefaultValue = true)]
        public bool EnableEarlyPrediction { get; set; }

[Parameter("Minimum Early Confidence", Group = "15 · INTELLIGENCE — EARLY", DefaultValue = 56, MinValue = 40, MaxValue = 95)]
        public int MinimumEarlyConfidence { get; set; }

[Parameter("Prediction Lookahead Bars", Group = "15 · INTELLIGENCE — EARLY", DefaultValue = 10, MinValue = 2, MaxValue = 30)]
        public int PredictionLookaheadBars { get; set; }

[Parameter("Show Prediction Zone", Group = "15 · INTELLIGENCE — EARLY", DefaultValue = true)]
        public bool ShowPredictionZone { get; set; }

[Parameter("Show Prediction Targets", Group = "15 · INTELLIGENCE — EARLY", DefaultValue = true)]
        public bool ShowPredictionTargets { get; set; }

[Parameter("Use Liquidity Forecast", Group = "15 · INTELLIGENCE — EARLY", DefaultValue = true)]
        public bool UseLiquidityForecast { get; set; }

[Parameter("Alert On Early Setup", Group = "15 · INTELLIGENCE — EARLY", DefaultValue = true)]
        public bool AlertOnEarlySetup { get; set; }

[Parameter("Alert On BOS", Group = "15 · INTELLIGENCE — EARLY", DefaultValue = true)]
        public bool AlertOnBos { get; set; }

[Parameter("Alert On MSS / CHOCH", Group = "15 · INTELLIGENCE — EARLY", DefaultValue = true)]
        public bool AlertOnMssChoch { get; set; }

[Parameter("Alert On Liquidity Sweep", Group = "15 · INTELLIGENCE — EARLY", DefaultValue = true)]
        public bool AlertOnLiquiditySweep { get; set; }

[Parameter("Enable Outcome Telemetry", Group = "15 · INTELLIGENCE — EARLY", DefaultValue = true)]
        public bool EnableOutcomeTelemetry { get; set; }

[Parameter("Outcome Maximum M5 Bars", Group = "15 · INTELLIGENCE — EARLY", DefaultValue = 72, MinValue = 10, MaxValue = 500)]
        public int OutcomeMaximumM5Bars { get; set; }

[Parameter("Enable Confidence Calibration", Group = "15 · INTELLIGENCE — EARLY", DefaultValue = true)]
        public bool EnableConfidenceCalibration { get; set; }

[Parameter("Use Empirical Calibration", Group = "15 · INTELLIGENCE — EARLY", DefaultValue = true)]
        public bool UseEmpiricalCalibration { get; set; }

[Parameter("Calibration Directional Minimum Samples", Group = "15 · INTELLIGENCE — EARLY", DefaultValue = 6, MinValue = 2, MaxValue = 250)]
        public int CalibrationDirectionalMinimumSamples { get; set; }

[Parameter("Calibration Minimum Samples", Group = "15 · INTELLIGENCE — EARLY", DefaultValue = 5, MinValue = 1, MaxValue = 100)]
        public int CalibrationMinimumSamples { get; set; }

[Parameter("Calibration Max Confidence Adjustment", Group = "15 · INTELLIGENCE — EARLY", DefaultValue = 8, MinValue = 0, MaxValue = 20)]
        public int CalibrationMaxConfidenceAdjustment { get; set; }

[Parameter("Show Outcome Diagnostics", Group = "15 · INTELLIGENCE — EARLY", DefaultValue = true)]
        public bool ShowOutcomeDiagnostics { get; set; }

[Parameter("Use Smart Entry Quality Filter", Group = "16 · Accuracy", DefaultValue = true)]
        public bool UseSmartEntryQualityFilter { get; set; }

[Parameter("Smart Quality Threshold", Group = "16 · Accuracy", DefaultValue = 70, MinValue = 40, MaxValue = 95)]
        public int SmartQualityThreshold { get; set; }

[Parameter("Require Fresh M5 Trigger", Group = "16 · Accuracy", DefaultValue = true)]
        public bool RequireFreshM5Trigger { get; set; }

[Parameter("Minimum Fresh Trigger Evidence", Group = "16 · Accuracy", DefaultValue = 3, MinValue = 1, MaxValue = 8)]
        public int MinimumFreshTriggerEvidence { get; set; }

[Parameter("Use False Signal Guard", Group = "16 · Accuracy", DefaultValue = true)]
        public bool UseFalseSignalGuard { get; set; }

[Parameter("False Signal Adverse R", Group = "16 · Accuracy", DefaultValue = 1.10, MinValue = 0.25, MaxValue = 5)]
        public double FalseSignalAdverseR { get; set; }

[Parameter("False Signal Watch Bars", Group = "16 · Accuracy", DefaultValue = 3, MinValue = 1, MaxValue = 12)]
        public int FalseSignalWatchBars { get; set; }

[Parameter("Invalidate On False Signal", Group = "16 · Accuracy", DefaultValue = true)]
        public bool InvalidateOnFalseSignal { get; set; }

[Parameter("Enable Setup Invalidation", Group = "16 · Accuracy", DefaultValue = true)]
        public bool EnableSetupInvalidation { get; set; }

[Parameter("Invalidation Structure ATR", Group = "16 · Accuracy", DefaultValue = 0.10, MinValue = 0.02, MaxValue = 0.50)]
        public double InvalidationStructureAtr { get; set; }

[Parameter("Invalidation Zone Close ATR", Group = "16 · Accuracy", DefaultValue = 0.10, MinValue = 0.02, MaxValue = 0.75)]
        public double InvalidationZoneCloseAtr { get; set; }

[Parameter("Invalidation Max Adverse R", Group = "16 · Accuracy", DefaultValue = 0.75, MinValue = 0.30, MaxValue = 2.00, Step = 0.05)]
        public double InvalidationMaxAdverseR { get; set; }

[Parameter("Require MTF Flip For Invalidation", Group = "16 · Accuracy", DefaultValue = true)]
        public bool RequireMtfFlipForInvalidation { get; set; }

[Parameter("Allow Reversal Against Stale HTF", Group = "16 · Accuracy", DefaultValue = true)]
        public bool AllowReversalAgainstStaleHtf { get; set; }

[Parameter("Enable Live Structural Reversal", Group = "16 · Accuracy", DefaultValue = true)]
        public bool EnableLiveStructuralReversal { get; set; }

[Parameter("Live Reversal Minimum Confidence", Group = "16 · Accuracy", DefaultValue = 68, MinValue = 50, MaxValue = 95)]
        public int LiveReversalMinimumConfidence { get; set; }

[Parameter("Live Reversal Minimum Evidence", Group = "16 · Accuracy", DefaultValue = 3, MinValue = 2, MaxValue = 8)]
        public int LiveReversalMinimumEvidence { get; set; }

[Parameter("Live Reversal Structural Score", Group = "16 · Accuracy", DefaultValue = 72, MinValue = 50, MaxValue = 100)]
        public int LiveReversalStructuralScore { get; set; }

[Parameter("Require Reversal Force", Group = "16 · Accuracy", DefaultValue = true)]
        public bool RequireReversalForce { get; set; }

[Parameter("Opposite Signal Cooldown M5", Group = "16 · Accuracy", DefaultValue = 5, MinValue = 0, MaxValue = 50)]
        public int OppositeSignalCooldownM5 { get; set; }

[Parameter("Prevent Rapid Direction Flip", Group = "16 · Accuracy", DefaultValue = true)]
        public bool PreventRapidDirectionFlip { get; set; }

[Parameter("Require M15 Reversal For Opposite", Group = "16 · Accuracy", DefaultValue = true)]
        public bool RequireM15ReversalForOpposite { get; set; }

[Parameter("Allow Opposite While Active", Group = "16 · Accuracy", DefaultValue = false)]
        public bool AllowOppositeWhileActive { get; set; }

[Parameter("Minimum Opposite M5 Structure", Group = "16 · Accuracy", DefaultValue = 2, MinValue = 1, MaxValue = 6)]
        public int MinimumOppositeM5Structure { get; set; }

[Parameter("Exit Reentry Cooldown M5", Group = "16 · Accuracy", DefaultValue = 3, MinValue = 0, MaxValue = 50)]
        public int ExitReentryCooldownM5 { get; set; }

[Parameter("Use Structural Sequence Gate", Group = "16 · Accuracy", DefaultValue = true)]
        public bool UseStructuralSequenceGate { get; set; }

[Parameter("Minimum Structural Sequence", Group = "16 · Accuracy", DefaultValue = 2, MinValue = 1, MaxValue = 5)]
        public int MinimumStructuralSequence { get; set; }

[Parameter("Require Entry Location Confluence", Group = "16 · Accuracy", DefaultValue = true)]
        public bool RequireEntryLocationConfluence { get; set; }

[Parameter("Minimum Entry Location Quality", Group = "16 · Accuracy", DefaultValue = 64, MinValue = 40, MaxValue = 95)]
        public int MinimumEntryLocationQuality { get; set; }

[Parameter("Use Proxy Expected Value Gate", Group = "16 · Accuracy", DefaultValue = true)]
        public bool UseProxyExpectedValueGate { get; set; }

[Parameter("Minimum Proxy Expected Value", Group = "16 · Accuracy", DefaultValue = 0.20, MinValue = -1, MaxValue = 2, Step = 0.05)]
        public double MinimumProxyExpectedValue { get; set; }

[Parameter("Enable Smart Decision Engine", Group = "17 · Smart Engine", DefaultValue = true)]
        public bool EnableSmartDecisionEngine { get; set; }

[Parameter("Smart Minimum Timeframe Agreement", Group = "17 · Smart Engine", DefaultValue = 72, MinValue = 50, MaxValue = 95)]
        public int SmartMinimumTimeframeAgreement { get; set; }

[Parameter("Smart Target Minimum RR", Group = "17 · Smart Engine", DefaultValue = 1.50, MinValue = 0.5, MaxValue = 8, Step = 0.05)]
        public double SmartTargetMinimumRR { get; set; }

[Parameter("Smart Target Max Candidates", Group = "17 · Smart Engine", DefaultValue = 32, MinValue = 8, MaxValue = 64)]
        public int SmartTargetMaxCandidates { get; set; }

[Parameter("Smart Regime Quality Floor", Group = "17 · Smart Engine", DefaultValue = 55, MinValue = 30, MaxValue = 90)]
        public int SmartRegimeQualityFloor { get; set; }

[Parameter("No Trade Minimum Smart Quality", Group = "17 · Smart Engine", DefaultValue = 55, MinValue = 30, MaxValue = 90)]
        public int NoTradeMinimumSmartQuality { get; set; }

[Parameter("Block Compression Regime", Group = "17 · Smart Engine", DefaultValue = true)]
        public bool BlockCompressionRegime { get; set; }

[Parameter("Block Weak Range Transition", Group = "17 · Smart Engine", DefaultValue = true)]
        public bool BlockWeakRangeTransition { get; set; }

[Parameter("Alert On Live Reaction", Group = "12 · ALERTS — ADVANCED", DefaultValue = true)]
        public bool AlertOnLiveReaction { get; set; }

[Parameter("Alert On Smart Decision", Group = "12 · ALERTS — ADVANCED", DefaultValue = true)]
        public bool AlertOnSmartDecision { get; set; }

[Parameter("Enable Level Hit Alerts", Group = "12 · ALERTS — ADVANCED", DefaultValue = true)]
        public bool EnableLevelHitAlerts { get; set; }

[Parameter("Alert On TP1", Group = "12 · ALERTS — ADVANCED", DefaultValue = true)]
        public bool AlertOnTp1 { get; set; }

[Parameter("Alert On TP2", Group = "12 · ALERTS — ADVANCED", DefaultValue = true)]
        public bool AlertOnTp2 { get; set; }

[Parameter("Alert On TP3", Group = "12 · ALERTS — ADVANCED", DefaultValue = true)]
        public bool AlertOnTp3 { get; set; }

[Parameter("Alert On TP4", Group = "12 · ALERTS — ADVANCED", DefaultValue = true)]
        public bool AlertOnTp4 { get; set; }

[Parameter("Alert On SL", Group = "12 · ALERTS — ADVANCED", DefaultValue = true)]
        public bool AlertOnSl { get; set; }

[Parameter("Alert On False Signal Risk", Group = "12 · ALERTS — ADVANCED", DefaultValue = true)]
        public bool AlertOnFalseSignalRisk { get; set; }

[Parameter("Alert On Entry Restriction", Group = "12 · ALERTS — ADVANCED", DefaultValue = false)]
        public bool AlertOnEntryRestriction { get; set; }

[Parameter("Alert On High Confidence Entry", Group = "12 · ALERTS — ADVANCED", DefaultValue = true)]
        public bool AlertOnHighConfidenceEntry { get; set; }

[Parameter("High Confidence Threshold", Group = "12 · ALERTS — ADVANCED", DefaultValue = 82, MinValue = 70, MaxValue = 99)]
        public int HighConfidenceThreshold { get; set; }

[Parameter("Alert Sound Type", Group = "12 · ALERTS — ADVANCED", DefaultValue = SoundType.PositiveNotification)]
        public cAlgo.API.SoundType AlertSoundType { get; set; }

        [Parameter("Sound File Path", Group = "12 · ALERTS — ADVANCED", DefaultValue = "")]
        public string SoundFilePath { get; set; }

[Parameter("Popup Position", Group = "12 · ALERTS — ADVANCED", DefaultValue = CFIPClean77PanelCorner.TopRight)]
        public CFIPClean77PanelCorner PopupPosition { get; set; }

[Parameter("Popup Width", Group = "12 · ALERTS — ADVANCED", DefaultValue = 430, MinValue = 220, MaxValue = 700)]
        public int PopupWidth { get; set; }

[Parameter("Keep Popup Until Next Alert", Group = "12 · ALERTS — ADVANCED", DefaultValue = false)]
        public bool KeepPopupUntilNextAlert { get; set; }

[Parameter("Show Popup Close Button", Group = "12 · ALERTS — ADVANCED", DefaultValue = true)]
        public bool ShowPopupCloseButton { get; set; }

[Parameter("Popup Background", Group = "12 · ALERTS — ADVANCED", DefaultValue = "Black")]
        public Color PopupBackgroundColor { get; set; }

[Parameter("Popup Background Alpha", Group = "12 · ALERTS — ADVANCED", DefaultValue = 235, MinValue = 0, MaxValue = 255)]
        public int PopupBackgroundAlpha { get; set; }

[Parameter("Popup Border", Group = "12 · ALERTS — ADVANCED", DefaultValue = "#3A4656")]
        public Color PopupBorderColor { get; set; }

[Parameter("Popup Border Thickness", Group = "12 · ALERTS — ADVANCED", DefaultValue = 1, MinValue = 0, MaxValue = 4)]
        public int PopupBorderThickness { get; set; }

[Parameter("Popup Corner Radius", Group = "12 · ALERTS — ADVANCED", DefaultValue = 5, MinValue = 0, MaxValue = 20)]
        public int PopupCornerRadius { get; set; }

[Parameter("Popup Padding", Group = "12 · ALERTS — ADVANCED", DefaultValue = 8, MinValue = 0, MaxValue = 30)]
        public int PopupPadding { get; set; }

[Parameter("Popup Text Color", Group = "12 · ALERTS — ADVANCED", DefaultValue = "White")]
        public Color PopupTextColor { get; set; }

[Parameter("Line Length Bars", Group = "14 · DISPLAY — ADVANCED", DefaultValue = 40, MinValue = 5, MaxValue = 300)]
        public int LineLengthBars { get; set; }

[Parameter("Line Forward Bars", Group = "14 · DISPLAY — ADVANCED", DefaultValue = 10, MinValue = 1, MaxValue = 100)]
        public int LineForwardBars { get; set; }

[Parameter("Show Signal Labels", Group = "14 · DISPLAY — ADVANCED", DefaultValue = true)]
        public bool ShowSignalLabels { get; set; }

[Parameter("Show Level Price Labels", Group = "14 · DISPLAY — ADVANCED", DefaultValue = true)]
        public bool ShowLevelPriceLabels { get; set; }

[Parameter("Show Context Event Marker", Group = "14 · DISPLAY — ADVANCED", DefaultValue = true)]
        public bool ShowContextEventMarker { get; set; }

[Parameter("Label Left Offset Bars", Group = "14 · DISPLAY — ADVANCED", DefaultValue = 2, MinValue = 1, MaxValue = 10)]
        public int LabelLeftOffsetBars { get; set; }

[Parameter("Show Prediction Objects", Group = "14 · DISPLAY — ADVANCED", DefaultValue = true)]
        public bool ShowPredictionObjects { get; set; }

[Parameter("Arrow Offset ATR", Group = "14 · DISPLAY — ADVANCED", DefaultValue = 0.18, MinValue = 0.02, MaxValue = 1)]
        public double ArrowOffsetAtr { get; set; }

[Parameter("Minimum Arrow Offset Pips", Group = "14 · DISPLAY — ADVANCED", DefaultValue = 2.0, MinValue = 0.5, MaxValue = 20)]
        public double MinimumArrowOffsetPips { get; set; }

[Parameter("Show Early Arrow", Group = "14 · DISPLAY — ADVANCED", DefaultValue = true)]
        public bool ShowEarlyArrow { get; set; }

[Parameter("Show Panel Toggle Button", Group = "14 · DISPLAY — PANEL", DefaultValue = true)]
        public bool ShowPanelToggleButton { get; set; }

[Parameter("Panel Toggle Width", Group = "14 · DISPLAY — PANEL", DefaultValue = 26, MinValue = 22, MaxValue = 40)]
        public int PanelToggleWidth { get; set; }

[Parameter("Panel Toggle Height", Group = "14 · DISPLAY — PANEL", DefaultValue = 26, MinValue = 22, MaxValue = 40)]
        public int PanelToggleHeight { get; set; }

[Parameter("Action Button Width", Group = "13 · AUTO TRADING", DefaultValue = 150, MinValue = 100, MaxValue = 240)]
        public int ActionButtonWidth { get; set; }

[Parameter("Action Button Height", Group = "13 · AUTO TRADING", DefaultValue = 25, MinValue = 20, MaxValue = 50)]
        public int ActionButtonHeight { get; set; }

[Parameter("Auto Protect Broker Positions", Group = "13 · AUTO TRADING", DefaultValue = false)]
        public bool AutoProtectBrokerPositions { get; set; }

[Parameter("Managed Position Label", Group = "13 · AUTO TRADING", DefaultValue = "")]
        public string ManagedPositionLabel { get; set; }

[Parameter("Sync Smart Broker Take Profit", Group = "13 · AUTO TRADING", DefaultValue = true)]
        public bool SyncBrokerTakeProfit { get; set; }

[Parameter("Prevent Broker TP Backward Move", Group = "13 · AUTO TRADING", DefaultValue = true)]
        public bool PreventBrokerTpBackwardMove { get; set; }

[Parameter("Broker Modify Cooldown ms", Group = "13 · AUTO TRADING", DefaultValue = 750, MinValue = 100, MaxValue = 5000, Step = 50)]
        public int BrokerModifyCooldownMs { get; set; }

[Parameter("Enable Aggressive Auto Entry", Group = "13 · AUTO TRADING", DefaultValue = false)]
        public bool EnableAggressiveAutoEntry { get; set; }

[Parameter("Aggressive Minimum Confidence", Group = "13 · AUTO TRADING", DefaultValue = 88, MinValue = 50, MaxValue = 99)]
        public int AggressiveMinimumConfidence { get; set; }

[Parameter("Aggressive Minimum Evidence", Group = "13 · AUTO TRADING", DefaultValue = 4, MinValue = 1, MaxValue = 8)]
        public int AggressiveMinimumEvidence { get; set; }

[Parameter("Aggressive Minimum Smart Quality", Group = "13 · AUTO TRADING", DefaultValue = 78, MinValue = 50, MaxValue = 95)]
        public int AggressiveMinimumSmartQuality { get; set; }

[Parameter("Aggressive Risk % Equity", Group = "13 · AUTO TRADING", DefaultValue = 0.25, MinValue = 0.05, MaxValue = 5)]
        public double AggressiveRiskPercentEquity { get; set; }

[Parameter("Aggressive TP Stage", Group = "13 · AUTO TRADING", DefaultValue = CFIPClean77TargetStage.TP1)]
        public CFIPClean77TargetStage AggressiveTpStage { get; set; }

[Parameter("Aggressive Require Smart Agreement", Group = "13 · AUTO TRADING", DefaultValue = true)]
        public bool AggressiveRequireSmartAgreement { get; set; }

        #endregion

        private CFIPClean77ConfigSnapshot _configuration;
        private CFIPClean77EngineState _state;
        private CFIPClean77LifecycleManager _lifecycle;
        private CFIPClean77RuntimeAuthority _runtimeAuthority;
        private CFIPClean77MarketModelBuilder _marketModelBuilder;
        private DateTime _lastMarketReferenceUtc = DateTime.MinValue;

        public CFIPClean77ConfigSnapshot Configuration
        {
            get { return _configuration; }
        }

        public CFIPClean77RuntimeAuthority RuntimeAuthority
        {
            get { return _runtimeAuthority; }
        }

        public CFIPClean77LifecycleState LifecycleState
        {
            get { return _lifecycle.State; }
        }

        protected override void Initialize()
        {
            _m1Bars = MarketData.GetBars(TimeFrame.Minute);
            _m5Bars = MarketData.GetBars(TimeFrame.Minute5);
            _m15Bars = MarketData.GetBars(TimeFrame.Minute15);
            _m30Bars = MarketData.GetBars(TimeFrame.Minute30);
            _h1Bars = MarketData.GetBars(TimeFrame.Hour);
            _h4Bars = MarketData.GetBars(TimeFrame.Hour4);
            _d1Bars = MarketData.GetBars(TimeFrame.Daily);
            _w1Bars = MarketData.GetBars(TimeFrame.Weekly);

            _configuration =
                CFIPClean77ConfigSnapshot.Build(
                    this,
                    "CFIP-PRO-v77");

            _runtimeAuthority =
                new CFIPClean77RuntimeAuthority();

            _runtimeAuthority.InitializeFromConfiguration(
                _configuration);

            _marketModelBuilder =
                new CFIPClean77MarketModelBuilder(
                    Indicators);

            _state = new CFIPClean77EngineState();
            _lifecycle = new CFIPClean77LifecycleManager();
        }

        public override void Calculate(int index)
        {
            DateTime serverUtc =
                TimeInUtc;

            DateTime userLocalTime =
                serverUtc +
                Application.UserTimeOffset;

            _state.ResetCycleOutputs();

            _state.SetRuntime(
                new CFIPClean77RuntimeSnapshot(
                    serverUtc,
                    SymbolName,
                    Math.Max(0, Symbol.Bid),
                    Math.Max(0, Symbol.Ask),
                    Symbol.PipSize > 0
                        ? Math.Max(
                            0,
                            (Symbol.Ask - Symbol.Bid) /
                            Symbol.PipSize)
                        : 0,
                    Server.IsConnected,
                    0,
                    0,
                    new CFIPClean77BrokerConstraints(
                        0,
                        0,
                        0,
                        0),
                    0,
                    0));

            CFIPClean77MtfSnapshot mtf =
                CFIPClean77MtfSnapshotBuilder.Build(
                    serverUtc,
                    userLocalTime,
                    Bars,
                    TimeFrame.ToString(),
                    _m1Bars,
                    _m5Bars,
                    _m15Bars,
                    _m30Bars,
                    _h1Bars,
                    _h4Bars,
                    _d1Bars,
                    _w1Bars,
                    30);

            _state.SetMtf(mtf);

            if (mtf.IsReferenceValid &&
                (
                    mtf.ReferenceUtc !=
                    _lastMarketReferenceUtc ||
                    !mtf.IsPrimaryDecisionReady
                ))
            {
                CFIPClean77MarketModel market =
                    _marketModelBuilder.Build(
                        _state.Runtime,
                        mtf,
                        _configuration,
                        Bars,
                        _m1Bars,
                        _m5Bars,
                        _m15Bars,
                        _m30Bars,
                        _h1Bars,
                        _h4Bars,
                        _d1Bars,
                        _w1Bars);

                _state.SetMarket(market);
                _lastMarketReferenceUtc =
                    mtf.ReferenceUtc;
            }
            else if (!mtf.IsReferenceValid)
            {
                _state.SetMarket(null);
                _lastMarketReferenceUtc =
                    DateTime.MinValue;
            }
        }

    }
}
