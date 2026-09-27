// ============================================================================
// CFIP-PRO cTrader — v78 Clean Architecture Foundation
// Phase: 4 · Market model, indicators, regime and confluence
//
// This version intentionally does NOT copy the v73 monolith.
// v73 remains the frozen behavioral/reference baseline.
// v78 now extends the type/ownership boundaries with the first normalized market model.
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

    public enum CFIPClean78Direction
    {
        Sell = -1,
        Wait = 0,
        Buy = 1
    }

    public static class CFIPClean78DirectionRules
    {
        public static bool IsDirectional(CFIPClean78Direction direction)
        {
            return direction != CFIPClean78Direction.Wait;
        }

        public static CFIPClean78Direction Opposite(CFIPClean78Direction direction)
        {
            if (direction == CFIPClean78Direction.Buy)
                return CFIPClean78Direction.Sell;

            if (direction == CFIPClean78Direction.Sell)
                return CFIPClean78Direction.Buy;

            return CFIPClean78Direction.Wait;
        }

        public static bool IsProtectiveMove(
            CFIPClean78Direction direction,
            double currentStop,
            double candidateStop)
        {
            if (!IsDirectional(direction) ||
                currentStop <= 0 ||
                candidateStop <= 0)
                return false;

            return direction == CFIPClean78Direction.Buy
                ? candidateStop > currentStop
                : candidateStop < currentStop;
        }
    }

    public enum CFIPClean78DecisionPolicyMode
    {
        Confirmed = 0,
        Soft = 1,
        Aggressive = 2,
        Pending = 3
    }

    public enum CFIPClean78ExecutionKind
    {
        None = 0,
        Market = 1,
        Stop = 2,
        Limit = 3
    }

    public enum CFIPClean78EntryMode
    {
        None = 0,
        RetestMarket = 1,
        BreakoutMarket = 2,
        ContinuationStop = 3,
        ReversalLimit = 4
    }

    public enum CFIPClean78LifecycleState
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

    public enum CFIPClean78BlockReason
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

    public enum CFIPClean78FallbackKind
    {
        None = 0,
        Structural = 1,
        HigherTimeframe = 2,
        ExecutionFrame = 3,
        Atr = 4,
        Synthetic = 5
    }

    public enum CFIPClean78TargetStage
    {
        TP1 = 1,
        TP2 = 2,
        TP3 = 3,
        TP4 = 4
    }

    public enum CFIPClean78ProtectionState
    {
        Unknown = 0,
        Unprotected = 1,
        PartiallyProtected = 2,
        FullyProtected = 3,
        RecoveryRequired = 4
    }

    public enum CFIPClean78BrokerObjectKind
    {
        None = 0,
        Position = 1,
        PendingOrder = 2
    }

    public enum CFIPClean78TargetState
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

    public sealed class CFIPClean78Provenance
    {
        public string Source { get; private set; }
        public string Rule { get; private set; }
        public CFIPClean78FallbackKind Fallback { get; private set; }
        public string Detail { get; private set; }

        public CFIPClean78Provenance(
            string source,
            string rule,
            CFIPClean78FallbackKind fallback,
            string detail)
        {
            Source = source ?? string.Empty;
            Rule = rule ?? string.Empty;
            Fallback = fallback;
            Detail = detail ?? string.Empty;
        }

        public static CFIPClean78Provenance Direct(
            string source,
            string rule)
        {
            return new CFIPClean78Provenance(
                source,
                rule,
                CFIPClean78FallbackKind.None,
                string.Empty);
        }

        public static CFIPClean78Provenance FallbackFrom(
            string source,
            string rule,
            CFIPClean78FallbackKind fallback,
            string detail)
        {
            return new CFIPClean78Provenance(
                source,
                rule,
                fallback,
                detail);
        }
    }

    // ------------------------------------------------------------------------
    // Price / zone primitives
    // ------------------------------------------------------------------------

    public sealed class CFIPClean78PriceLevel
    {
        public double Price { get; private set; }
        public string Name { get; private set; }
        public CFIPClean78Provenance Provenance { get; private set; }

        public CFIPClean78PriceLevel(
            double price,
            string name,
            CFIPClean78Provenance provenance)
        {
            if (price <= 0)
                throw new ArgumentOutOfRangeException("price");

            Price = price;
            Name = name ?? string.Empty;
            Provenance = provenance ??
                         CFIPClean78Provenance.Direct(
                             "UNKNOWN",
                             "UNSPECIFIED");
        }
    }

    public sealed class CFIPClean78PriceZone
    {
        public double Lower { get; private set; }
        public double Upper { get; private set; }
        public string Name { get; private set; }
        public CFIPClean78Provenance Provenance { get; private set; }

        public CFIPClean78PriceZone(
            double lower,
            double upper,
            string name,
            CFIPClean78Provenance provenance)
        {
            if (lower <= 0 || upper <= 0 || upper < lower)
                throw new ArgumentException("Invalid price zone.");

            Lower = lower;
            Upper = upper;
            Name = name ?? string.Empty;
            Provenance = provenance ??
                         CFIPClean78Provenance.Direct(
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

    public sealed class CFIPClean78EntryModel
    {
        public CFIPClean78Direction Direction { get; private set; }

        // Preferred price inside the structural execution area.
        public CFIPClean78PriceLevel IdealEntry { get; private set; }

        // Allowed structural retest region.
        public CFIPClean78PriceZone EntryZone { get; private set; }

        // Structural activation threshold for breakout/continuation.
        public CFIPClean78PriceLevel Trigger { get; private set; }

        // Exact strategy/broker protection boundary.
        public CFIPClean78PriceLevel Invalidation { get; private set; }

        public CFIPClean78EntryModel(
            CFIPClean78Direction direction,
            CFIPClean78PriceLevel idealEntry,
            CFIPClean78PriceZone entryZone,
            CFIPClean78PriceLevel trigger,
            CFIPClean78PriceLevel invalidation)
        {
            if (!CFIPClean78DirectionRules.IsDirectional(direction))
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

    public sealed class CFIPClean78TargetLevel
    {
        public CFIPClean78TargetStage Stage { get; private set; }
        public CFIPClean78PriceLevel Level { get; private set; }
        public CFIPClean78TargetState State { get; private set; }
        public int Quality { get; private set; }

        public CFIPClean78TargetLevel(
            CFIPClean78TargetStage stage,
            CFIPClean78PriceLevel level,
            int quality,
            CFIPClean78TargetState state)
        {
            Stage = stage;
            Level = level;
            Quality = Math.Max(0, Math.Min(100, quality));
            State = state;
        }
    }

    public sealed class CFIPClean78TargetLadder
    {
        private readonly ReadOnlyCollection<CFIPClean78TargetLevel> _levels;

        public IReadOnlyList<CFIPClean78TargetLevel> Levels
        {
            get { return _levels; }
        }

        public CFIPClean78TargetLadder(
            IList<CFIPClean78TargetLevel> levels)
        {
            if (levels == null)
                throw new ArgumentNullException("levels");

            var copy =
                new List<CFIPClean78TargetLevel>(
                    levels);

            copy.Sort(
                delegate (
                    CFIPClean78TargetLevel left,
                    CFIPClean78TargetLevel right)
                {
                    return left.Stage.CompareTo(right.Stage);
                });

            _levels =
                new ReadOnlyCollection<CFIPClean78TargetLevel>(
                    copy);
        }

        public bool ValidateForDirection(
            CFIPClean78Direction direction)
        {
            if (!CFIPClean78DirectionRules.IsDirectional(direction) ||
                _levels.Count == 0)
                return false;

            for (int i = 1; i < _levels.Count; i++)
            {
                double previous =
                    _levels[i - 1].Level.Price;

                double current =
                    _levels[i].Level.Price;

                if (direction == CFIPClean78Direction.Buy &&
                    current <= previous)
                    return false;

                if (direction == CFIPClean78Direction.Sell &&
                    current >= previous)
                    return false;
            }

            return true;
        }

        public CFIPClean78TargetLevel Find(
            CFIPClean78TargetStage stage)
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

    public sealed class CFIPClean78BrokerConstraints
    {
        public double MinVolumeInUnits { get; private set; }
        public double VolumeStepInUnits { get; private set; }
        public double MinStopDistancePips { get; private set; }
        public double MinTakeProfitDistancePips { get; private set; }

        public CFIPClean78BrokerConstraints(
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

    public sealed class CFIPClean78RuntimeSnapshot
    {
        public DateTime ServerUtc { get; private set; }
        public string Symbol { get; private set; }
        public double Bid { get; private set; }
        public double Ask { get; private set; }
        public double SpreadPips { get; private set; }
        public bool SymbolTradingEnabled { get; private set; }
        public double Equity { get; private set; }
        public double FreeMargin { get; private set; }
        public CFIPClean78BrokerConstraints BrokerConstraints { get; private set; }
        public int ManagedPositionCount { get; private set; }
        public int ManagedPendingOrderCount { get; private set; }

        public CFIPClean78RuntimeSnapshot(
            DateTime serverUtc,
            string symbol,
            double bid,
            double ask,
            double spreadPips,
            bool symbolTradingEnabled,
            double equity,
            double freeMargin,
            CFIPClean78BrokerConstraints brokerConstraints,
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

    public enum CFIPClean78MtfDataStatus
    {
        Ready = 0,
        PrimaryHistoryInsufficient = 1,
        MissingTimeframeData = 2,
        InvalidReference = 3,
        StaleReference = 4
    }

    public sealed class CFIPClean78MtfBarSnapshot
    {
        public string Timeframe { get; private set; }
        public int ClosedIndex { get; private set; }
        public DateTime BarOpenUtc { get; private set; }
        public DateTime NextBarOpenUtc { get; private set; }
        public bool IsAvailable { get; private set; }
        public bool IsFullyClosedAtReference { get; private set; }
        public bool HasMinimumHistory { get; private set; }

        public CFIPClean78MtfBarSnapshot(
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

        public static CFIPClean78MtfBarSnapshot Missing(
            string timeframe)
        {
            return new CFIPClean78MtfBarSnapshot(
                timeframe,
                -1,
                DateTime.MinValue,
                DateTime.MinValue,
                false,
                false,
                false);
        }
    }

    public sealed class CFIPClean78MtfSnapshot
    {
        public DateTime ServerUtc { get; private set; }
        public DateTime ReferenceUtc { get; private set; }
        public DateTime UserLocalTime { get; private set; }
        public TimeSpan ReferenceAge { get; private set; }
        public string ChartTimeframe { get; private set; }

        public CFIPClean78MtfBarSnapshot Chart { get; private set; }
        public CFIPClean78MtfBarSnapshot M1 { get; private set; }
        public CFIPClean78MtfBarSnapshot M5 { get; private set; }
        public CFIPClean78MtfBarSnapshot M15 { get; private set; }
        public CFIPClean78MtfBarSnapshot M30 { get; private set; }
        public CFIPClean78MtfBarSnapshot H1 { get; private set; }
        public CFIPClean78MtfBarSnapshot H4 { get; private set; }
        public CFIPClean78MtfBarSnapshot D1 { get; private set; }
        public CFIPClean78MtfBarSnapshot W1 { get; private set; }

        public CFIPClean78MtfDataStatus DataStatus { get; private set; }

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

        public CFIPClean78MtfSnapshot(
            DateTime serverUtc,
            DateTime referenceUtc,
            DateTime userLocalTime,
            TimeSpan referenceAge,
            string chartTimeframe,
            CFIPClean78MtfBarSnapshot chart,
            CFIPClean78MtfBarSnapshot m1,
            CFIPClean78MtfBarSnapshot m5,
            CFIPClean78MtfBarSnapshot m15,
            CFIPClean78MtfBarSnapshot m30,
            CFIPClean78MtfBarSnapshot h1,
            CFIPClean78MtfBarSnapshot h4,
            CFIPClean78MtfBarSnapshot d1,
            CFIPClean78MtfBarSnapshot w1,
            CFIPClean78MtfDataStatus dataStatus)
        {
            ServerUtc = serverUtc;
            ReferenceUtc = referenceUtc;
            UserLocalTime = userLocalTime;
            ReferenceAge =
                referenceAge < TimeSpan.Zero
                    ? TimeSpan.Zero
                    : referenceAge;
            ChartTimeframe = chartTimeframe ?? string.Empty;
            Chart = chart ?? CFIPClean78MtfBarSnapshot.Missing("CHART");
            M1 = m1 ?? CFIPClean78MtfBarSnapshot.Missing("M1");
            M5 = m5 ?? CFIPClean78MtfBarSnapshot.Missing("M5");
            M15 = m15 ?? CFIPClean78MtfBarSnapshot.Missing("M15");
            M30 = m30 ?? CFIPClean78MtfBarSnapshot.Missing("M30");
            H1 = h1 ?? CFIPClean78MtfBarSnapshot.Missing("H1");
            H4 = h4 ?? CFIPClean78MtfBarSnapshot.Missing("H4");
            D1 = d1 ?? CFIPClean78MtfBarSnapshot.Missing("D1");
            W1 = w1 ?? CFIPClean78MtfBarSnapshot.Missing("W1");
            DataStatus = dataStatus;
        }
    }

    public static class CFIPClean78MtfSnapshotBuilder
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

        public static CFIPClean78MtfSnapshot Build(
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
                return new CFIPClean78MtfSnapshot(
                    serverUtc,
                    DateTime.MinValue,
                    userLocalTime,
                    TimeSpan.Zero,
                    chartTimeframe,
                    CFIPClean78MtfBarSnapshot.Missing(
                        "CHART"),
                    CFIPClean78MtfBarSnapshot.Missing("M1"),
                    CFIPClean78MtfBarSnapshot.Missing("M5"),
                    CFIPClean78MtfBarSnapshot.Missing("M15"),
                    CFIPClean78MtfBarSnapshot.Missing("M30"),
                    CFIPClean78MtfBarSnapshot.Missing("H1"),
                    CFIPClean78MtfBarSnapshot.Missing("H4"),
                    CFIPClean78MtfBarSnapshot.Missing("D1"),
                    CFIPClean78MtfBarSnapshot.Missing("W1"),
                    CFIPClean78MtfDataStatus.InvalidReference);
            }

            TimeSpan age =
                serverUtc >= reference
                    ? serverUtc - reference
                    : TimeSpan.Zero;

            CFIPClean78MtfBarSnapshot chart =
                ResolveClosedBar(
                    chartBars,
                    reference,
                    chartTimeframe,
                    minimumHistory);

            CFIPClean78MtfBarSnapshot m1 =
                ResolveClosedBar(
                    m1Bars,
                    reference,
                    "M1",
                    minimumHistory);

            CFIPClean78MtfBarSnapshot m5 =
                ResolveClosedBar(
                    m5Bars,
                    reference,
                    "M5",
                    minimumHistory);

            CFIPClean78MtfBarSnapshot m15 =
                ResolveClosedBar(
                    m15Bars,
                    reference,
                    "M15",
                    minimumHistory);

            CFIPClean78MtfBarSnapshot m30 =
                ResolveClosedBar(
                    m30Bars,
                    reference,
                    "M30",
                    minimumHistory);

            CFIPClean78MtfBarSnapshot h1 =
                ResolveClosedBar(
                    h1Bars,
                    reference,
                    "H1",
                    minimumHistory);

            CFIPClean78MtfBarSnapshot h4 =
                ResolveClosedBar(
                    h4Bars,
                    reference,
                    "H4",
                    minimumHistory);

            CFIPClean78MtfBarSnapshot d1 =
                ResolveClosedBar(
                    d1Bars,
                    reference,
                    "D1",
                    minimumHistory);

            CFIPClean78MtfBarSnapshot w1 =
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

            CFIPClean78MtfDataStatus status;

            if (age > TimeSpan.FromMinutes(10))
                status =
                    CFIPClean78MtfDataStatus.StaleReference;
            else if (!primaryHistory)
                status =
                    CFIPClean78MtfDataStatus.PrimaryHistoryInsufficient;
            else if (!m1.IsAvailable ||
                     !m5.IsAvailable ||
                     !m15.IsAvailable ||
                     !m30.IsAvailable ||
                     !h1.IsAvailable ||
                     !h4.IsAvailable)
                status =
                    CFIPClean78MtfDataStatus.MissingTimeframeData;
            else
                status =
                    CFIPClean78MtfDataStatus.Ready;

            return new CFIPClean78MtfSnapshot(
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
            CFIPClean78MtfBarSnapshot m5,
            CFIPClean78MtfBarSnapshot m15,
            CFIPClean78MtfBarSnapshot m30,
            CFIPClean78MtfBarSnapshot h1,
            CFIPClean78MtfBarSnapshot h4,
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

        public static CFIPClean78MtfBarSnapshot ResolveClosedBar(
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
                    CFIPClean78MtfBarSnapshot.Missing(
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
                    new CFIPClean78MtfBarSnapshot(
                        timeframe,
                        i,
                        open,
                        nextOpen,
                        true,
                        true,
                        minimum);
            }

            return
                CFIPClean78MtfBarSnapshot.Missing(
                    timeframe);
        }

        public static bool IsCoherent(
            CFIPClean78MtfSnapshot snapshot)
        {
            if (snapshot == null ||
                !snapshot.IsReferenceValid ||
                !snapshot.IsReferenceFresh)
                return false;

            CFIPClean78MtfBarSnapshot[] items =
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
                CFIPClean78MtfBarSnapshot item =
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

    public enum CFIPClean78Regime
    {
        Unknown = 0,
        Trend = 1,
        Expansion = 2,
        Compression = 3,
        Range = 4,
        Transition = 5
    }

    public enum CFIPClean78MarketFeature
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

    public sealed class CFIPClean78FeatureEvidence
    {
        public CFIPClean78MarketFeature Feature { get; private set; }
        public CFIPClean78Direction Direction { get; private set; }
        public double Value { get; private set; }
        public int Weight { get; private set; }
        public bool Triggered { get; private set; }
        public bool CountsAsEvidence { get; private set; }
        public bool CountsAsGate { get; private set; }
        public CFIPClean78Provenance Provenance { get; private set; }

        public CFIPClean78FeatureEvidence(
            CFIPClean78MarketFeature feature,
            CFIPClean78Direction direction,
            double value,
            int weight,
            bool triggered,
            bool countsAsEvidence,
            bool countsAsGate,
            CFIPClean78Provenance provenance)
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
                CFIPClean78Provenance.Direct(
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

    public sealed class CFIPClean78MarketFrame
    {
        private readonly ReadOnlyCollection<CFIPClean78FeatureEvidence> _features;

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
        public CFIPClean78Direction BiasDirection { get; private set; }
        public int BiasStrength { get; private set; }
        public int MarketQuality { get; private set; }
        public int BullScore { get; private set; }
        public int BearScore { get; private set; }
        public int BullScoreNormalized { get; private set; }
        public int BearScoreNormalized { get; private set; }
        public int IndependentEvidence { get; private set; }
        public int EnabledEvidenceFeatures { get; private set; }

        public CFIPClean78Regime Regime { get; private set; }
        public int RegimeQuality { get; private set; }
        public bool Choppy { get; private set; }
        public bool HealthyVolatility { get; private set; }
        public bool DataValid { get; private set; }

        public IReadOnlyList<CFIPClean78FeatureEvidence> Features
        {
            get { return _features; }
        }

        public int DirectionScore
        {
            get
            {
                return
                    BiasDirection == CFIPClean78Direction.Buy
                        ? BiasStrength
                        : BiasDirection == CFIPClean78Direction.Sell
                            ? -BiasStrength
                            : 0;
            }
        }

        public CFIPClean78MarketFrame(
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
            CFIPClean78Direction biasDirection,
            int biasStrength,
            int marketQuality,
            int bullScore,
            int bearScore,
            int bullScoreNormalized,
            int bearScoreNormalized,
            int independentEvidence,
            int enabledEvidenceFeatures,
            CFIPClean78Regime regime,
            int regimeQuality,
            bool choppy,
            bool healthyVolatility,
            bool dataValid,
            IList<CFIPClean78FeatureEvidence> features)
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
            MarketQuality =
                Math.Max(0,
                    Math.Min(100, marketQuality));
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
                new ReadOnlyCollection<CFIPClean78FeatureEvidence>(
                    new List<CFIPClean78FeatureEvidence>(
                        features ??
                        new List<CFIPClean78FeatureEvidence>()));
        }
    }

    public sealed class CFIPClean78MarketModel
    {
        private readonly ReadOnlyCollection<CFIPClean78MarketFrame> _frames;

        public DateTime ReferenceUtc { get; private set; }
        public bool IsCoherent { get; private set; }
        public bool IsPrimaryReady { get; private set; }
        public string DataStatus { get; private set; }

        public IReadOnlyList<CFIPClean78MarketFrame> Frames
        {
            get { return _frames; }
        }

        public CFIPClean78MarketFrame M5
        {
            get { return FindFrame("M5"); }
        }

        public CFIPClean78MarketFrame M15
        {
            get { return FindFrame("M15"); }
        }

        public CFIPClean78MarketFrame FindFrame(string timeframe)
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

        public CFIPClean78MarketModel(
            DateTime referenceUtc,
            bool isCoherent,
            bool isPrimaryReady,
            string dataStatus,
            IList<CFIPClean78MarketFrame> frames)
        {
            ReferenceUtc = referenceUtc;
            IsCoherent = isCoherent;
            IsPrimaryReady = isPrimaryReady;
            DataStatus = dataStatus ?? string.Empty;
            _frames =
                new ReadOnlyCollection<CFIPClean78MarketFrame>(
                    new List<CFIPClean78MarketFrame>(
                        frames ??
                        new List<CFIPClean78MarketFrame>()));
        }
    }

    public sealed class CFIPClean78NativeIndicatorSet
    {
        public Bars Bars { get; private set; }
        public ExponentialMovingAverage Fast { get; private set; }
        public ExponentialMovingAverage Slow { get; private set; }
        public AverageTrueRange Atr { get; private set; }
        public RelativeStrengthIndex Rsi { get; private set; }
        public DirectionalMovementSystem Dms { get; private set; }
        public ExponentialMovingAverage MacdFast { get; private set; }
        public ExponentialMovingAverage MacdSlow { get; private set; }

        public CFIPClean78NativeIndicatorSet(
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

    public sealed class CFIPClean78NativeIndicatorCatalog
    {
        private readonly IIndicatorsAccessor _indicators;
        private readonly List<CFIPClean78NativeIndicatorSet> _sets =
            new List<CFIPClean78NativeIndicatorSet>();

        public CFIPClean78NativeIndicatorCatalog(
            IIndicatorsAccessor indicators)
        {
            _indicators =
                indicators ??
                throw new ArgumentNullException("indicators");
        }

        public CFIPClean78NativeIndicatorSet GetOrCreate(
            Bars bars,
            CFIPClean78ConfigSnapshot configuration)
        {
            if (bars == null)
                return null;

            for (int i = 0; i < _sets.Count; i++)
            {
                if (ReferenceEquals(_sets[i].Bars, bars))
                    return _sets[i];
            }

            int fastPeriod = Math.Max(2, configuration.Get("FastEma", 21));
            int slowPeriod = Math.Max(fastPeriod + 1, configuration.Get("SlowEma", 55));
            int atrPeriod = Math.Max(2, configuration.Get("AtrPeriod", 14));
            int rsiPeriod = Math.Max(2, configuration.Get("RsiPeriod", 14));
            int adxPeriod = Math.Max(2, configuration.Get("AdxPeriod", 14));
            int macdFastPeriod = Math.Max(2, configuration.Get("MacdFastPeriod", 12));
            int macdSlowPeriod = Math.Max(macdFastPeriod + 1, configuration.Get("MacdSlowPeriod", 26));

            try
            {
                var set = new CFIPClean78NativeIndicatorSet(
                    bars,
                    _indicators.ExponentialMovingAverage(
                        bars.ClosePrices,
                        fastPeriod),
                    _indicators.ExponentialMovingAverage(
                        bars.ClosePrices,
                        slowPeriod),
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

    public sealed class CFIPClean78MarketModelBuilder
    {
        private const int MinimumClosedIndex = 30;
        private readonly CFIPClean78NativeIndicatorCatalog _catalog;

        public CFIPClean78MarketModelBuilder(IIndicatorsAccessor indicators)
        {
            _catalog = new CFIPClean78NativeIndicatorCatalog(indicators);
        }

        public CFIPClean78MarketModel Build(
            CFIPClean78RuntimeSnapshot runtime,
            CFIPClean78MtfSnapshot mtf,
            CFIPClean78ConfigSnapshot configuration,
            Bars chartBars,
            Bars m1Bars,
            Bars m5Bars,
            Bars m15Bars,
            Bars m30Bars,
            Bars h1Bars,
            Bars h4Bars,
            Bars d1Bars,
            Bars w1Bars)
        {
            if (mtf == null)
                throw new ArgumentNullException("mtf");

            var frames = new List<CFIPClean78MarketFrame>();

            if (!mtf.IsReferenceValid)
                return new CFIPClean78MarketModel(
                    DateTime.MinValue,
                    false,
                    false,
                    mtf.DataStatus.ToString(),
                    frames);

            bool coherent =
                CFIPClean78MtfSnapshotBuilder.IsCoherent(mtf);

            AddFrame(frames, "CHART", chartBars, mtf.Chart, configuration);
            AddFrame(frames, "M1", m1Bars, mtf.M1, configuration);
            AddFrame(frames, "M5", m5Bars, mtf.M5, configuration);
            AddFrame(frames, "M15", m15Bars, mtf.M15, configuration);
            AddFrame(frames, "M30", m30Bars, mtf.M30, configuration);
            AddFrame(frames, "H1", h1Bars, mtf.H1, configuration);
            AddFrame(frames, "H4", h4Bars, mtf.H4, configuration);
            AddFrame(frames, "D1", d1Bars, mtf.D1, configuration);
            AddFrame(frames, "W1", w1Bars, mtf.W1, configuration);

            return new CFIPClean78MarketModel(
                mtf.ReferenceUtc,
                coherent,
                mtf.IsPrimaryDecisionReady,
                mtf.DataStatus.ToString(),
                frames);
        }

        private void AddFrame(
            IList<CFIPClean78MarketFrame> frames,
            string timeframe,
            Bars bars,
            CFIPClean78MtfBarSnapshot snapshot,
            CFIPClean78ConfigSnapshot configuration)
        {
            if (frames == null ||
                snapshot == null ||
                !snapshot.IsAvailable ||
                !snapshot.IsFullyClosedAtReference ||
                snapshot.ClosedIndex < MinimumClosedIndex ||
                bars == null)
                return;

            CFIPClean78MarketFrame frame =
                BuildFrame(
                    timeframe,
                    bars,
                    snapshot,
                    configuration);

            if (frame != null)
                frames.Add(frame);
        }

        private CFIPClean78MarketFrame BuildFrame(
            string timeframe,
            Bars bars,
            CFIPClean78MtfBarSnapshot snapshot,
            CFIPClean78ConfigSnapshot configuration)
        {
            int index = snapshot.ClosedIndex;

            if (index < MinimumClosedIndex ||
                index >= bars.Count - 1)
                return null;

            CFIPClean78NativeIndicatorSet native =
                _catalog.GetOrCreate(
                    bars,
                    configuration);

            if (native == null)
                return null;

            double atr =
                Read(
                    native.Atr == null
                        ? null
                        : native.Atr.Result,
                    index);

            double previousAtr =
                Read(
                    native.Atr == null
                        ? null
                        : native.Atr.Result,
                    Math.Max(5, index - 10));

            if (atr <= 0 || previousAtr <= 0)
                return null;

            double open = bars.OpenPrices[index];
            double high = bars.HighPrices[index];
            double low = bars.LowPrices[index];
            double close = bars.ClosePrices[index];

            double fast =
                Read(
                    native.Fast == null
                        ? null
                        : native.Fast.Result,
                    index);

            double slow =
                Read(
                    native.Slow == null
                        ? null
                        : native.Slow.Result,
                    index);

            double previousFast =
                Read(
                    native.Fast == null
                        ? null
                        : native.Fast.Result,
                    Math.Max(0, index - 2));

            double rsi =
                Read(
                    native.Rsi == null
                        ? null
                        : native.Rsi.Result,
                    index,
                    50);

            double adx =
                Read(
                    native.Dms == null
                        ? null
                        : native.Dms.ADX,
                    index);

            double dmiPlus =
                Read(
                    native.Dms == null
                        ? null
                        : native.Dms.DIPlus,
                    index);

            double dmiMinus =
                Read(
                    native.Dms == null
                        ? null
                        : native.Dms.DIMinus,
                    index);

            double dmiBias = NormalizeDmi(dmiPlus, dmiMinus);
            double emaSpread = fast - slow;
            double emaSpreadAtr = emaSpread / atr;
            double emaSlopeAtr = (fast - previousFast) / atr;

            double momentumAtr =
                (close -
                 bars.ClosePrices[Math.Max(0, index - 2)]) /
                atr;

            double macdHistogram =
                Read(
                    native.MacdFast == null
                        ? null
                        : native.MacdFast.Result,
                    index) -
                Read(
                    native.MacdSlow == null
                        ? null
                        : native.MacdSlow.Result,
                    index);

            double previousMacd =
                Read(
                    native.MacdFast == null
                        ? null
                        : native.MacdFast.Result,
                    Math.Max(0, index - 2)) -
                Read(
                    native.MacdSlow == null
                        ? null
                        : native.MacdSlow.Result,
                    Math.Max(0, index - 2));

            double vwap =
                RollingVwap(
                    bars,
                    index,
                    Math.Max(
                        10,
                        configuration.Get(
                            "VwapLookbackBars",
                            48)));

            double averageVolume =
                AverageTickVolume(
                    bars,
                    index,
                    20);

            double volumeRatio =
                averageVolume > 0
                    ? Math.Max(
                        0,
                        bars.TickVolumes[index]) /
                      averageVolume
                    : 0;

            double range =
                Math.Max(
                    0.0000001,
                    high - low);

            double body =
                Math.Abs(
                    close - open);

            double bodyAtr = body / atr;
            double rangeAtr = range / atr;
            double atrRatio = atr / previousAtr;

            int adxMinimum =
                Math.Max(
                    0,
                    configuration.Get(
                        "AdxMinimum",
                        20));

            bool useSlope =
                configuration.Get(
                    "UseEmaSlope",
                    true);

            bool trendBull = fast > slow && close > fast;
            bool trendBear = fast < slow && close < fast;
            bool momentumBull = momentumAtr > 0.15;
            bool momentumBear = momentumAtr < -0.15;
            bool rsiBull = rsi > 50;
            bool rsiBear = rsi < 50;
            bool dmiBull = adx >= adxMinimum && dmiBias > 0;
            bool dmiBear = adx >= adxMinimum && dmiBias < 0;
            bool slopeBull = useSlope && emaSlopeAtr > 0;
            bool slopeBear = useSlope && emaSlopeAtr < 0;

            bool rejectionBull =
                IsBullishRejection(open, high, low, close);
            bool rejectionBear =
                IsBearishRejection(open, high, low, close);

            bool volumeEnabled =
                configuration.Get(
                    "UseVolumeExpansion",
                    false);

            double volumeThreshold =
                Math.Max(
                    1.0,
                    configuration.Get(
                        "VolumeExpansionRatio",
                        1.15));

            bool volumeBull =
                volumeEnabled &&
                close > open &&
                volumeRatio >= volumeThreshold;

            bool volumeBear =
                volumeEnabled &&
                close < open &&
                volumeRatio >= volumeThreshold;

            bool macdEnabled =
                configuration.Get(
                    "UseMacdBias",
                    false);

            bool macdBull =
                macdEnabled &&
                macdHistogram > 0 &&
                macdHistogram >= previousMacd;

            bool macdBear =
                macdEnabled &&
                macdHistogram < 0 &&
                macdHistogram <= previousMacd;

            bool vwapEnabled =
                configuration.Get(
                    "UseVwapBias",
                    false);

            bool vwapBull =
                vwapEnabled &&
                close > vwap;

            bool vwapBear =
                vwapEnabled &&
                close < vwap;

            bool healthyEnabled =
                configuration.Get(
                    "UseHealthyVolatility",
                    false);

            double minHealthyRatio =
                Math.Max(
                    0.50,
                    configuration.Get(
                        "HealthyAtrMinimumRatio",
                        0.85));

            double maxHealthyRatio =
                Math.Max(
                    minHealthyRatio,
                    configuration.Get(
                        "HealthyAtrMaximumRatio",
                        1.80));

            double minBodyAtr =
                Math.Max(
                    0,
                    configuration.Get(
                        "MinimumTriggerBodyAtr",
                        0.35));

            bool healthyVolatility =
                healthyEnabled &&
                atrRatio >= minHealthyRatio &&
                atrRatio <= maxHealthyRatio &&
                bodyAtr >= minBodyAtr;

            bool qualityVolatility =
                healthyEnabled
                    ? healthyVolatility
                    : atrRatio >= 0.50 &&
                      atrRatio <= 2.50;

            bool volumeEvidence =
                configuration.Get(
                    "UseVolumeExpansionEvidence",
                    true);

            bool macdEvidence =
                configuration.Get(
                    "UseMacdEvidence",
                    true);

            bool vwapEvidence =
                configuration.Get(
                    "UseVwapEvidence",
                    true);

            bool healthyEvidence =
                configuration.Get(
                    "UseHealthyVolatilityEvidence",
                    true);

            var features =
                new List<CFIPClean78FeatureEvidence>();

            int bullScore = 0;
            int bearScore = 0;
            int independentEvidence = 0;
            int enabledFeatures = 0;

            AddFeature(features, CFIPClean78MarketFeature.Trend,
                trendBull, trendBear, 10, true, true, true,
                ref bullScore, ref bearScore,
                ref independentEvidence, ref enabledFeatures);

            AddFeature(features, CFIPClean78MarketFeature.Momentum,
                momentumBull, momentumBear, 8, true, true, true,
                ref bullScore, ref bearScore,
                ref independentEvidence, ref enabledFeatures);

            AddFeature(features, CFIPClean78MarketFeature.Rsi,
                rsiBull, rsiBear, 3, true, false, true,
                ref bullScore, ref bearScore,
                ref independentEvidence, ref enabledFeatures);

            AddFeature(features, CFIPClean78MarketFeature.Dmi,
                dmiBull, dmiBear, 4,
                adx >= adxMinimum,
                true, true,
                ref bullScore, ref bearScore,
                ref independentEvidence, ref enabledFeatures);

            AddFeature(features, CFIPClean78MarketFeature.EmaSlope,
                slopeBull, slopeBear, 3,
                useSlope, false, useSlope,
                ref bullScore, ref bearScore,
                ref independentEvidence, ref enabledFeatures);

            AddFeature(features, CFIPClean78MarketFeature.Rejection,
                rejectionBull, rejectionBear, 6, true, false, true,
                ref bullScore, ref bearScore,
                ref independentEvidence, ref enabledFeatures);

            AddFeature(features, CFIPClean78MarketFeature.VolumeExpansion,
                volumeBull, volumeBear, 3,
                volumeEvidence, false, volumeEnabled,
                ref bullScore, ref bearScore,
                ref independentEvidence, ref enabledFeatures);

            AddFeature(features, CFIPClean78MarketFeature.MacdBias,
                macdBull, macdBear, 3,
                macdEvidence, false, macdEnabled,
                ref bullScore, ref bearScore,
                ref independentEvidence, ref enabledFeatures);

            AddFeature(features, CFIPClean78MarketFeature.VwapBias,
                vwapBull, vwapBear, 2,
                vwapEvidence, false, vwapEnabled,
                ref bullScore, ref bearScore,
                ref independentEvidence, ref enabledFeatures);

            AddFeature(features, CFIPClean78MarketFeature.HealthyVolatility,
                healthyVolatility && close > open,
                healthyVolatility && close < open,
                2,
                healthyEvidence, false, healthyEnabled,
                ref bullScore, ref bearScore,
                ref independentEvidence, ref enabledFeatures);

            int possibleScore =
                SumEnabledWeights(
                    useSlope,
                    volumeEvidence && volumeEnabled,
                    macdEvidence && macdEnabled,
                    vwapEvidence && vwapEnabled,
                    healthyEvidence && healthyEnabled);

            bool avoidRsiExhaustion =
                configuration.Get(
                    "AvoidRsiExhaustion",
                    true);

            if (avoidRsiExhaustion)
            {
                if (rsi >= 75)
                    bullScore =
                        Math.Max(
                            0,
                            bullScore - 5);

                if (rsi <= 25)
                    bearScore =
                        Math.Max(
                            0,
                            bearScore - 5);
            }

            int bullNormalized =
                NormalizeScore(bullScore, possibleScore);

            int bearNormalized =
                NormalizeScore(bearScore, possibleScore);

            CFIPClean78Direction bias =
                ResolveBias(
                    bullNormalized,
                    bearNormalized);

            int biasStrength =
                bias == CFIPClean78Direction.Buy
                    ? bullNormalized
                    : bias == CFIPClean78Direction.Sell
                        ? bearNormalized
                        : 0;

            CFIPClean78Regime regime =
                DetectRegime(
                    atr,
                    previousAtr,
                    adx,
                    adxMinimum,
                    Math.Abs(
                        emaSpread));

            bool choppy =
                configuration.Get(
                    "UseHistoricalChoppinessGuard",
                    true) &&
                adx < adxMinimum &&
                Math.Abs(emaSpread) < atr * 0.35;

            int regimeQuality =
                CalculateRegimeQuality(
                    regime,
                    adx,
                    atrRatio,
                    emaSpreadAtr);

            int marketQuality =
                CalculateMarketQuality(
                    bullNormalized,
                    bearNormalized,
                    adx,
                    independentEvidence,
                    choppy,
                    regimeQuality,
                    qualityVolatility);

            return new CFIPClean78MarketFrame(
                timeframe,
                snapshot.BarOpenUtc,
                open,
                high,
                low,
                close,
                atr,
                atrRatio,
                rsi,
                adx,
                dmiBias,
                fast,
                slow,
                emaSpreadAtr,
                emaSlopeAtr,
                momentumAtr,
                macdHistogram,
                vwap,
                volumeRatio,
                bodyAtr,
                rangeAtr,
                bias,
                biasStrength,
                marketQuality,
                bullScore,
                bearScore,
                bullNormalized,
                bearNormalized,
                independentEvidence,
                enabledFeatures,
                regime,
                regimeQuality,
                choppy,
                qualityVolatility,
                true,
                features);
        }

        private static void AddFeature(
            IList<CFIPClean78FeatureEvidence> features,
            CFIPClean78MarketFeature feature,
            bool bull,
            bool bear,
            int weight,
            bool countsAsEvidence,
            bool countsAsGate,
            bool enabled,
            ref int bullScore,
            ref int bearScore,
            ref int independentEvidence,
            ref int enabledFeatures)
        {
            if (!enabled)
                return;

            enabledFeatures++;

            CFIPClean78Direction direction =
                bull && !bear
                    ? CFIPClean78Direction.Buy
                    : bear && !bull
                        ? CFIPClean78Direction.Sell
                        : CFIPClean78Direction.Wait;

            features.Add(
                new CFIPClean78FeatureEvidence(
                    feature,
                    direction,
                    direction == CFIPClean78Direction.Wait ? 0 : 1,
                    weight,
                    direction != CFIPClean78Direction.Wait,
                    countsAsEvidence,
                    countsAsGate,
                    CFIPClean78Provenance.Direct(
                        "MARKET_MODEL",
                        feature.ToString())));

            if (direction == CFIPClean78Direction.Buy)
            {
                bullScore += weight;
                if (countsAsEvidence)
                    independentEvidence++;
            }
            else if (direction == CFIPClean78Direction.Sell)
            {
                bearScore += weight;
                if (countsAsEvidence)
                    independentEvidence++;
            }
        }

        private static int SumEnabledWeights(
            bool slope,
            bool volume,
            bool macd,
            bool vwap,
            bool healthy)
        {
            int weight = 10 + 8 + 3 + 4 + 6;

            if (slope) weight += 3;
            if (volume) weight += 3;
            if (macd) weight += 3;
            if (vwap) weight += 2;
            if (healthy) weight += 2;

            return Math.Max(1, weight);
        }

        private static int NormalizeScore(int score, int maximum)
        {
            return ClampInt(
                (int)Math.Round(
                    100.0 *
                    Math.Max(0, score) /
                    Math.Max(1, maximum)),
                0,
                100);
        }

        private static CFIPClean78Direction ResolveBias(int bull, int bear)
        {
            if (bull >= 55 && bull >= bear + 12)
                return CFIPClean78Direction.Buy;

            if (bear >= 55 && bear >= bull + 12)
                return CFIPClean78Direction.Sell;

            return CFIPClean78Direction.Wait;
        }

        private static CFIPClean78Regime DetectRegime(
            double atr,
            double previousAtr,
            double adx,
            int adxMinimum,
            double emaSpread)
        {
            if (atr <= 0 || previousAtr <= 0)
                return CFIPClean78Regime.Unknown;

            double ratio = atr / previousAtr;

            if (ratio >= 1.30)
                return CFIPClean78Regime.Expansion;

            if (ratio <= 0.80)
                return CFIPClean78Regime.Compression;

            if (adx < adxMinimum)
                return CFIPClean78Regime.Range;

            if (emaSpread <= atr * 0.10)
                return CFIPClean78Regime.Transition;

            return CFIPClean78Regime.Trend;
        }

        private static int CalculateRegimeQuality(
            CFIPClean78Regime regime,
            double adx,
            double atrRatio,
            double emaSpreadAtr)
        {
            double adxQuality = Math.Min(100, adx * 1.5);

            double volatilityQuality =
                Math.Max(
                    0,
                    100 -
                    Math.Abs(
                        Math.Log(
                            Math.Max(
                                0.01,
                                atrRatio)) *
                        70));

            double separationQuality =
                Math.Min(
                    100,
                    Math.Abs(
                        emaSpreadAtr) *
                    100);

            double bonus =
                regime ==
                    CFIPClean78Regime.Trend ||
                regime ==
                    CFIPClean78Regime.Expansion
                    ? 100
                    : regime ==
                        CFIPClean78Regime.Transition
                        ? 55
                        : 35;

            return ClampInt(
                (int)Math.Round(
                    adxQuality * 0.45 +
                    volatilityQuality * 0.25 +
                    separationQuality * 0.15 +
                    bonus * 0.15),
                0,
                100);
        }

        private static int CalculateMarketQuality(
            int bull,
            int bear,
            double adx,
            int evidence,
            bool choppy,
            int regimeQuality,
            bool healthyVolatility)
        {
            double dominance = Math.Max(bull, bear);
            double evidenceQuality = Math.Min(100, evidence * 12.5);

            double quality =
                dominance * 0.45 +
                Math.Min(100, adx * 1.5) * 0.15 +
                evidenceQuality * 0.20 +
                regimeQuality * 0.10 +
                (healthyVolatility ? 100 : 60) * 0.05 +
                (choppy ? 0 : 100) * 0.05;

            return ClampInt(
                (int)Math.Round(quality),
                0,
                100);
        }

        private static bool IsBullishRejection(
            double open,
            double high,
            double low,
            double close)
        {
            double range = Math.Max(0.0000001, high - low);
            double body = Math.Abs(close - open);
            double wick = Math.Min(open, close) - low;

            return wick > body * 1.25 &&
                   wick / range > 0.20;
        }

        private static bool IsBearishRejection(
            double open,
            double high,
            double low,
            double close)
        {
            double range = Math.Max(0.0000001, high - low);
            double body = Math.Abs(close - open);
            double wick = high - Math.Max(open, close);

            return wick > body * 1.25 &&
                   wick / range > 0.20;
        }

        private static double RollingVwap(
            Bars bars,
            int index,
            int lookback)
        {
            int first =
                Math.Max(
                    0,
                    index -
                    Math.Max(
                        10,
                        lookback - 1));

            double priceVolume = 0;
            double volume = 0;

            for (int i = first; i <= index; i++)
            {
                double typical =
                    (bars.HighPrices[i] +
                     bars.LowPrices[i] +
                     bars.ClosePrices[i]) /
                    3.0;

                double v =
                    Math.Max(
                        1.0,
                        bars.TickVolumes[i]);

                priceVolume += typical * v;
                volume += v;
            }

            return volume > 0
                ? priceVolume / volume
                : bars.ClosePrices[index];
        }

        private static double AverageTickVolume(
            Bars bars,
            int index,
            int lookback)
        {
            int first =
                Math.Max(
                    0,
                    index -
                    Math.Max(
                        1,
                        lookback));

            double total = 0;
            int count = 0;

            for (int i = first; i < index; i++)
            {
                total += Math.Max(0, bars.TickVolumes[i]);
                count++;
            }

            return count > 0
                ? total / count
                : 0;
        }

        private static double NormalizeDmi(
            double plus,
            double minus)
        {
            double total =
                Math.Max(0, plus) +
                Math.Max(0, minus);

            if (total <= 0)
                return 0;

            return Math.Max(
                -1,
                Math.Min(
                    1,
                    (plus - minus) /
                    total));
        }

        private static double Read(
            IndicatorDataSeries series,
            int index,
            double fallback = 0)
        {
            if (series == null ||
                index < 0 ||
                index >= series.Count)
                return fallback;

            double value = series[index];

            return
                double.IsNaN(value) ||
                double.IsInfinity(value)
                    ? fallback
                    : value;
        }

        private static int ClampInt(
            int value,
            int min,
            int max)
        {
            return Math.Max(
                min,
                Math.Min(
                    max,
                    value));
        }
    }

    public sealed class CFIPClean78StructureEvent
    {
        public string EventType { get; private set; }
        public CFIPClean78Direction Direction { get; private set; }
        public string Timeframe { get; private set; }
        public DateTime TimeUtc { get; private set; }
        public double Price { get; private set; }
        public int Quality { get; private set; }
        public CFIPClean78Provenance Provenance { get; private set; }

        public CFIPClean78StructureEvent(
            string eventType,
            CFIPClean78Direction direction,
            string timeframe,
            DateTime timeUtc,
            double price,
            int quality,
            CFIPClean78Provenance provenance)
        {
            EventType = eventType ?? string.Empty;
            Direction = direction;
            Timeframe = timeframe ?? string.Empty;
            TimeUtc = timeUtc;
            Price = price;
            Quality = Math.Max(0, Math.Min(100, quality));
            Provenance =
                provenance ??
                CFIPClean78Provenance.Direct(
                    "UNKNOWN",
                    "UNSPECIFIED");
        }
    }

    public sealed class CFIPClean78ZoneSnapshot
    {
        public string Id { get; private set; }
        public string Type { get; private set; }
        public CFIPClean78Direction Direction { get; private set; }
        public string Timeframe { get; private set; }
        public double OriginalLower { get; private set; }
        public double OriginalUpper { get; private set; }
        public double CurrentLower { get; private set; }
        public double CurrentUpper { get; private set; }
        public DateTime CreatedUtc { get; private set; }
        public bool Mitigated { get; private set; }
        public bool Retested { get; private set; }
        public bool Invalidated { get; private set; }
        public int Quality { get; private set; }
        public CFIPClean78Provenance Provenance { get; private set; }

        public CFIPClean78ZoneSnapshot(
            string id,
            string type,
            CFIPClean78Direction direction,
            string timeframe,
            double originalLower,
            double originalUpper,
            double currentLower,
            double currentUpper,
            DateTime createdUtc,
            bool mitigated,
            bool retested,
            bool invalidated,
            int quality,
            CFIPClean78Provenance provenance)
        {
            Id = id ?? string.Empty;
            Type = type ?? string.Empty;
            Direction = direction;
            Timeframe = timeframe ?? string.Empty;
            OriginalLower = originalLower;
            OriginalUpper = originalUpper;
            CurrentLower = currentLower;
            CurrentUpper = currentUpper;
            CreatedUtc = createdUtc;
            Mitigated = mitigated;
            Retested = retested;
            Invalidated = invalidated;
            Quality = Math.Max(0, Math.Min(100, quality));
            Provenance =
                provenance ??
                CFIPClean78Provenance.Direct(
                    "UNKNOWN",
                    "UNSPECIFIED");
        }
    }

    public sealed class CFIPClean78LiquiditySnapshot
    {
        public string Id { get; private set; }
        public string Type { get; private set; }
        public CFIPClean78Direction SweepDirection { get; private set; }
        public string Timeframe { get; private set; }
        public double Price { get; private set; }
        public double Distance { get; private set; }
        public bool Swept { get; private set; }
        public int Quality { get; private set; }
        public CFIPClean78Provenance Provenance { get; private set; }

        public CFIPClean78LiquiditySnapshot(
            string id,
            string type,
            CFIPClean78Direction sweepDirection,
            string timeframe,
            double price,
            double distance,
            bool swept,
            int quality,
            CFIPClean78Provenance provenance)
        {
            Id = id ?? string.Empty;
            Type = type ?? string.Empty;
            SweepDirection = sweepDirection;
            Timeframe = timeframe ?? string.Empty;
            Price = price;
            Distance = Math.Max(0, distance);
            Swept = swept;
            Quality = Math.Max(0, Math.Min(100, quality));
            Provenance =
                provenance ??
                CFIPClean78Provenance.Direct(
                    "UNKNOWN",
                    "UNSPECIFIED");
        }
    }

    public enum CFIPClean78StructureEventKind
    {
        SwingHigh, SwingLow, BreakOfStructure, MarketStructureShift,
        ChangeOfCharacter, Displacement, LiquiditySweep
    }

    public enum CFIPClean78ZoneKind { FairValueGap, OrderBlock }

    public enum CFIPClean78ZoneLifecycle
    {
        Active, Retested, PartiallyMitigated, Consumed, Invalidated, Expired
    }

    public enum CFIPClean78LiquidityKind
    {
        EqualHigh, EqualLow, SwingHigh, SwingLow,
        PriorDayHigh, PriorDayLow, PriorWeekHigh, PriorWeekLow,
        SessionHigh, SessionLow, DailyPivot, DailyR1, DailyR2, DailyS1, DailyS2,
        Forecast
    }

    public enum CFIPClean78LiquiditySide { Neutral, Above, Below }

    public sealed class CFIPClean78StructureEventRecord
    {
        public string Id { get; private set; }
        public CFIPClean78StructureEventKind Kind { get; private set; }
        public CFIPClean78Direction Direction { get; private set; }
        public string Timeframe { get; private set; }
        public int BarIndex { get; private set; }
        public DateTime TimeUtc { get; private set; }
        public double Price { get; private set; }
        public double StrengthAtr { get; private set; }
        public int Quality { get; private set; }
        public CFIPClean78Provenance Provenance { get; private set; }

        public CFIPClean78StructureEventRecord(
            string id, CFIPClean78StructureEventKind kind,
            CFIPClean78Direction direction, string timeframe,
            int barIndex, DateTime timeUtc, double price,
            double strengthAtr, int quality,
            CFIPClean78Provenance provenance)
        {
            Id = id ?? string.Empty;
            Kind = kind; Direction = direction; Timeframe = timeframe ?? string.Empty;
            BarIndex = barIndex; TimeUtc = timeUtc; Price = price;
            StrengthAtr = Math.Max(0, strengthAtr);
            Quality = Math.Max(0, Math.Min(100, quality));
            Provenance = provenance ?? CFIPClean78Provenance.Direct("STRUCTURE", kind.ToString());
        }
    }

    public sealed class CFIPClean78ZoneRecord
    {
        public string Id { get; private set; }
        public CFIPClean78ZoneKind Kind { get; private set; }
        public CFIPClean78Direction Direction { get; private set; }
        public string Timeframe { get; private set; }
        public int CreatedIndex { get; private set; }
        public DateTime CreatedUtc { get; private set; }
        public double OriginalLower { get; private set; }
        public double OriginalUpper { get; private set; }
        public double CurrentLower { get; private set; }
        public double CurrentUpper { get; private set; }
        public int AgeBars { get; private set; }
        public bool Retested { get; private set; }
        public bool Mitigated { get; private set; }
        public bool Invalidated { get; private set; }
        public bool Consumed { get; private set; }
        public bool ExecutionEligible { get; private set; }
        public int Quality { get; private set; }
        public double DisplacementAtr { get; private set; }
        public bool LiquidityConfluence { get; private set; }
        public bool FvgConfluence { get; private set; }
        public CFIPClean78ZoneLifecycle Lifecycle { get; private set; }
        public CFIPClean78Provenance Provenance { get; private set; }

        public CFIPClean78ZoneRecord(
            string id, CFIPClean78ZoneKind kind,
            CFIPClean78Direction direction, string timeframe,
            int createdIndex, DateTime createdUtc,
            double originalLower, double originalUpper,
            double currentLower, double currentUpper,
            int ageBars, bool retested, bool mitigated,
            bool invalidated, bool consumed, bool executionEligible,
            int quality, double displacementAtr,
            bool liquidityConfluence, bool fvgConfluence,
            CFIPClean78ZoneLifecycle lifecycle,
            CFIPClean78Provenance provenance)
        {
            Id = id ?? string.Empty; Kind = kind; Direction = direction;
            Timeframe = timeframe ?? string.Empty; CreatedIndex = createdIndex;
            CreatedUtc = createdUtc; OriginalLower = originalLower; OriginalUpper = originalUpper;
            CurrentLower = currentLower; CurrentUpper = currentUpper;
            AgeBars = Math.Max(0, ageBars); Retested = retested; Mitigated = mitigated;
            Invalidated = invalidated; Consumed = consumed; ExecutionEligible = executionEligible;
            Quality = Math.Max(0, Math.Min(100, quality));
            DisplacementAtr = Math.Max(0, displacementAtr);
            LiquidityConfluence = liquidityConfluence; FvgConfluence = fvgConfluence;
            Lifecycle = lifecycle;
            Provenance = provenance ?? CFIPClean78Provenance.Direct("ZONE", kind.ToString());
        }

        public bool Contains(double price)
        {
            return price >= CurrentLower && price <= CurrentUpper;
        }
    }

    public sealed class CFIPClean78LiquidityRecord
    {
        public string Id { get; private set; }
        public CFIPClean78LiquidityKind Kind { get; private set; }
        public CFIPClean78LiquiditySide Side { get; private set; }
        public CFIPClean78Direction SweepDirection { get; private set; }
        public string Timeframe { get; private set; }
        public int BarIndex { get; private set; }
        public DateTime TimeUtc { get; private set; }
        public double Price { get; private set; }
        public double Tolerance { get; private set; }
        public double Distance { get; private set; }
        public double Penetration { get; private set; }
        public bool Swept { get; private set; }
        public bool ForecastCandidate { get; private set; }
        public int Quality { get; private set; }
        public CFIPClean78Provenance Provenance { get; private set; }

        public CFIPClean78LiquidityRecord(
            string id, CFIPClean78LiquidityKind kind,
            CFIPClean78LiquiditySide side, CFIPClean78Direction sweepDirection,
            string timeframe, int barIndex, DateTime timeUtc,
            double price, double tolerance, double distance, double penetration,
            bool swept, bool forecastCandidate, int quality,
            CFIPClean78Provenance provenance)
        {
            Id = id ?? string.Empty; Kind = kind; Side = side; SweepDirection = sweepDirection;
            Timeframe = timeframe ?? string.Empty; BarIndex = barIndex; TimeUtc = timeUtc;
            Price = price; Tolerance = Math.Max(0, tolerance); Distance = Math.Max(0, distance);
            Penetration = Math.Max(0, penetration); Swept = swept;
            ForecastCandidate = forecastCandidate;
            Quality = Math.Max(0, Math.Min(100, quality));
            Provenance = provenance ?? CFIPClean78Provenance.Direct("LIQUIDITY", kind.ToString());
        }
    }

    public sealed class CFIPClean78PremiumDiscountState
    {
        public bool Available { get; private set; }
        public double RangeLow { get; private set; }
        public double RangeHigh { get; private set; }
        public double Midpoint { get { return Available ? (RangeLow + RangeHigh) / 2.0 : 0; } }
        public double ValueRatio { get; private set; }
        public bool IsDiscount { get { return Available && ValueRatio < 0.5; } }
        public bool IsPremium { get { return Available && ValueRatio > 0.5; } }

        public CFIPClean78PremiumDiscountState(
            bool available, double rangeLow, double rangeHigh, double valueRatio)
        {
            Available = available; RangeLow = rangeLow; RangeHigh = rangeHigh;
            ValueRatio = Math.Max(0, Math.Min(1, valueRatio));
        }
    }

    public sealed class CFIPClean78StructureSnapshot
    {
        private readonly ReadOnlyCollection<CFIPClean78StructureEventRecord> _events;
        private readonly ReadOnlyCollection<CFIPClean78ZoneRecord> _zones;
        private readonly ReadOnlyCollection<CFIPClean78LiquidityRecord> _liquidity;

        public DateTime ReferenceUtc { get; private set; }
        public bool IsCoherent { get; private set; }
        public bool IsPrimaryReady { get; private set; }
        public CFIPClean78Direction CurrentStructureDirection { get; private set; }
        public int StructureQuality { get; private set; }
        public CFIPClean78PremiumDiscountState PremiumDiscount { get; private set; }
        public IReadOnlyList<CFIPClean78StructureEventRecord> Events { get { return _events; } }
        public IReadOnlyList<CFIPClean78ZoneRecord> Zones { get { return _zones; } }
        public IReadOnlyList<CFIPClean78LiquidityRecord> Liquidity { get { return _liquidity; } }

        public CFIPClean78StructureSnapshot(
            DateTime referenceUtc, bool isCoherent, bool isPrimaryReady,
            CFIPClean78Direction direction, int quality,
            IList<CFIPClean78StructureEventRecord> events,
            IList<CFIPClean78ZoneRecord> zones,
            IList<CFIPClean78LiquidityRecord> liquidity,
            CFIPClean78PremiumDiscountState premiumDiscount)
        {
            ReferenceUtc = referenceUtc; IsCoherent = isCoherent; IsPrimaryReady = isPrimaryReady;
            CurrentStructureDirection = direction;
            StructureQuality = Math.Max(0, Math.Min(100, quality));
            _events = new ReadOnlyCollection<CFIPClean78StructureEventRecord>(
                new List<CFIPClean78StructureEventRecord>(events ?? new List<CFIPClean78StructureEventRecord>()));
            _zones = new ReadOnlyCollection<CFIPClean78ZoneRecord>(
                new List<CFIPClean78ZoneRecord>(zones ?? new List<CFIPClean78ZoneRecord>()));
            _liquidity = new ReadOnlyCollection<CFIPClean78LiquidityRecord>(
                new List<CFIPClean78LiquidityRecord>(liquidity ?? new List<CFIPClean78LiquidityRecord>()));
            PremiumDiscount = premiumDiscount ?? new CFIPClean78PremiumDiscountState(false, 0, 0, 0);
        }

        public bool HasEvent(CFIPClean78StructureEventKind kind, CFIPClean78Direction direction)
        {
            for (int i = 0; i < _events.Count; i++)
                if (_events[i].Kind == kind && _events[i].Direction == direction) return true;
            return false;
        }

        public CFIPClean78ZoneRecord FindNearestZone(
            CFIPClean78Direction direction, CFIPClean78ZoneKind kind,
            double price, bool executionEligibleOnly)
        {
            CFIPClean78ZoneRecord best = null; double bestDistance = double.MaxValue;
            for (int i = 0; i < _zones.Count; i++)
            {
                var z = _zones[i];
                if (z.Direction != direction || z.Kind != kind ||
                    z.Consumed || z.Invalidated || z.Lifecycle == CFIPClean78ZoneLifecycle.Expired)
                    continue;
                if (executionEligibleOnly && !z.ExecutionEligible) continue;

                double distance =
                    price <= z.CurrentLower ? z.CurrentLower - price :
                    price >= z.CurrentUpper ? price - z.CurrentUpper : 0;

                if (distance < bestDistance) { bestDistance = distance; best = z; }
            }
            return best;
        }

        public CFIPClean78LiquidityRecord FindNearestLiquidity(
            CFIPClean78LiquiditySide side, double price, bool unsweptOnly)
        {
            CFIPClean78LiquidityRecord best = null; double bestDistance = double.MaxValue;
            for (int i = 0; i < _liquidity.Count; i++)
            {
                var x = _liquidity[i];
                if (x.Side != side || (unsweptOnly && x.Swept)) continue;
                double distance = Math.Abs(x.Price - price);
                if (distance < bestDistance) { bestDistance = distance; best = x; }
            }
            return best;
        }
    }

    public sealed class CFIPClean78StructureLedgerBuilder
    {
        private const int MinimumIndex = 35;

        public CFIPClean78StructureSnapshot Build(
            CFIPClean78MtfSnapshot mtf,
            CFIPClean78MarketModel market,
            CFIPClean78ConfigSnapshot configuration,
            Bars m5Bars, Bars m15Bars, Bars m30Bars, Bars h1Bars, Bars h4Bars,
            Bars d1Bars, Bars w1Bars)
        {
            var events = new List<CFIPClean78StructureEventRecord>();
            var zones = new List<CFIPClean78ZoneRecord>();
            var liquidity = new List<CFIPClean78LiquidityRecord>();

            if (mtf == null || market == null ||
                !mtf.IsPrimaryDecisionReady)
            {
                return new CFIPClean78StructureSnapshot(
                    mtf == null ? DateTime.MinValue : mtf.ReferenceUtc,
                    false, false, CFIPClean78Direction.Wait, 0,
                    events, zones, liquidity,
                    new CFIPClean78PremiumDiscountState(false, 0, 0, 0));
            }

            BuildTimeframe(events, zones, liquidity, "M5", m5Bars, mtf.M5, market.M5, configuration, true);
            BuildTimeframe(events, zones, liquidity, "M15", m15Bars, mtf.M15, market.M15, configuration, false);
            BuildTimeframe(events, zones, liquidity, "M30", m30Bars, mtf.M30, market.FindFrame("M30"), configuration, false);
            BuildTimeframe(events, zones, liquidity, "H1", h1Bars, mtf.H1, market.FindFrame("H1"), configuration, false);
            BuildTimeframe(events, zones, liquidity, "H4", h4Bars, mtf.H4, market.FindFrame("H4"), configuration, false);

            AddPriorDayWeekLiquidity(liquidity, mtf, d1Bars, w1Bars, configuration);
            AddSessionLiquidity(liquidity, mtf, m5Bars, configuration);
            AddDailyPivots(liquidity, mtf, d1Bars, configuration);
            MarkForecastLiquidity(liquidity, market.M5);
            MarkConfluence(zones, liquidity, configuration);

            var direction = ResolveDirection(events);
            int quality = CalculateQuality(events, zones, liquidity, direction);
            var pd = BuildPremiumDiscount(m5Bars, mtf.M5.ClosedIndex, configuration);

            return new CFIPClean78StructureSnapshot(
                mtf.ReferenceUtc,
                CFIPClean78MtfSnapshotBuilder.IsCoherent(mtf),
                mtf.IsPrimaryDecisionReady,
                direction,
                quality,
                events, zones, liquidity, pd);
        }

        private void BuildTimeframe(
            IList<CFIPClean78StructureEventRecord> events,
            IList<CFIPClean78ZoneRecord> zones,
            IList<CFIPClean78LiquidityRecord> liquidity,
            string timeframe, Bars bars, CFIPClean78MtfBarSnapshot snapshot,
            CFIPClean78MarketFrame frame, CFIPClean78ConfigSnapshot cfg, bool primary)
        {
            if (bars == null || snapshot == null || frame == null ||
                !snapshot.IsAvailable || !snapshot.IsFullyClosedAtReference ||
                snapshot.ClosedIndex < MinimumIndex || !frame.DataValid)
                return;

            int index = snapshot.ClosedIndex;
            double atr = frame.Atr;
            if (atr <= 0) return;

            int strength = Math.Max(1, cfg.Get("SwingStrength", 3));
            int lookback = Math.Max(20, cfg.Get("StructureLookback", 60));
            double breakAtr = Math.Max(0, cfg.Get("StructureBreakAtr", 0.02));

            int swingHigh = FindLatestSwingHigh(bars, index, strength, lookback);
            int swingLow = FindLatestSwingLow(bars, index, strength, lookback);

            if (cfg.Get("UseInternalStructure", true))
            {
                if (swingHigh >= 0)
                    AddEvent(events, CFIPClean78StructureEventKind.SwingHigh,
                        CFIPClean78Direction.Sell, timeframe, bars, swingHigh,
                        bars.HighPrices[swingHigh], 0, 68, "confirmed swing high");

                if (swingLow >= 0)
                    AddEvent(events, CFIPClean78StructureEventKind.SwingLow,
                        CFIPClean78Direction.Buy, timeframe, bars, swingLow,
                        bars.LowPrices[swingLow], 0, 68, "confirmed swing low");

                if (swingHigh >= 0 &&
                    bars.ClosePrices[index] > bars.HighPrices[swingHigh] + atr * breakAtr)
                {
                    double strengthAtr =
                        (bars.ClosePrices[index] - bars.HighPrices[swingHigh]) / atr;
                    AddEvent(events, CFIPClean78StructureEventKind.BreakOfStructure,
                        CFIPClean78Direction.Buy, timeframe, bars, index,
                        bars.ClosePrices[index], strengthAtr,
                        Clamp(70 + (int)Math.Round(Math.Min(20, strengthAtr * 8))),
                        "close beyond latest swing high");

                    if (IsBearishPreBreak(bars, index, swingHigh))
                        AddEvent(events, CFIPClean78StructureEventKind.MarketStructureShift,
                            CFIPClean78Direction.Buy, timeframe, bars, index,
                            bars.ClosePrices[index], strengthAtr, 82,
                            "bullish break after bearish sequence");

                    if (IsCharacterBreakBull(bars, index, strength, lookback, atr, breakAtr))
                        AddEvent(events, CFIPClean78StructureEventKind.ChangeOfCharacter,
                            CFIPClean78Direction.Buy, timeframe, bars, index,
                            bars.ClosePrices[index], strengthAtr, 78,
                            "bullish change of character");
                }

                if (swingLow >= 0 &&
                    bars.ClosePrices[index] < bars.LowPrices[swingLow] - atr * breakAtr)
                {
                    double strengthAtr =
                        (bars.LowPrices[swingLow] - bars.ClosePrices[index]) / atr;
                    AddEvent(events, CFIPClean78StructureEventKind.BreakOfStructure,
                        CFIPClean78Direction.Sell, timeframe, bars, index,
                        bars.ClosePrices[index], strengthAtr,
                        Clamp(70 + (int)Math.Round(Math.Min(20, strengthAtr * 8))),
                        "close beyond latest swing low");

                    if (IsBullishPreBreak(bars, index, swingLow))
                        AddEvent(events, CFIPClean78StructureEventKind.MarketStructureShift,
                            CFIPClean78Direction.Sell, timeframe, bars, index,
                            bars.ClosePrices[index], strengthAtr, 82,
                            "bearish break after bullish sequence");

                    if (IsCharacterBreakBear(bars, index, strength, lookback, atr, breakAtr))
                        AddEvent(events, CFIPClean78StructureEventKind.ChangeOfCharacter,
                            CFIPClean78Direction.Sell, timeframe, bars, index,
                            bars.ClosePrices[index], strengthAtr, 78,
                            "bearish change of character");
                }
            }

            if (cfg.Get("UseDisplacement", true))
            {
                double bodyAtr =
                    Math.Abs(bars.ClosePrices[index] - bars.OpenPrices[index]) / atr;
                double threshold = Math.Max(0, cfg.Get("DisplacementAtr", 0.80));
                CFIPClean78Direction d =
                    bars.ClosePrices[index] > bars.OpenPrices[index]
                        ? CFIPClean78Direction.Buy
                        : bars.ClosePrices[index] < bars.OpenPrices[index]
                            ? CFIPClean78Direction.Sell
                            : CFIPClean78Direction.Wait;

                if (d != CFIPClean78Direction.Wait && bodyAtr >= threshold)
                    AddEvent(events, CFIPClean78StructureEventKind.Displacement,
                        d, timeframe, bars, index, bars.ClosePrices[index], bodyAtr,
                        Clamp(70 + (int)Math.Round(Math.Min(25, bodyAtr * 10))),
                        "closed body exceeds displacement ATR threshold");
            }

            if (cfg.Get("UseFvg", true))
                BuildFvgZones(zones, bars, index, timeframe, atr, cfg);

            if (cfg.Get("UseOrderBlock", true))
                BuildObZones(zones, bars, index, timeframe, atr, cfg);

            if (cfg.Get("UseEqualHighLow", true))
                BuildEqualLiquidity(liquidity, bars, index, timeframe, atr, cfg);

            if (cfg.Get("UseLiquiditySweep", true))
                BuildSweepLiquidity(liquidity, events, bars, index, timeframe, atr, cfg);

            if (primary && cfg.Get("UseExtendedLiquidityMap", true))
                AddSwingLiquidity(liquidity, bars, index, timeframe, atr, cfg);
        }

        private void BuildFvgZones(
            IList<CFIPClean78ZoneRecord> zones, Bars bars, int index,
            string timeframe, double atr, CFIPClean78ConfigSnapshot cfg)
        {
            int lookback = Math.Max(3, cfg.Get("FvgLookback", 80));
            double minGap = Math.Max(0, cfg.Get("MinimumFvgAtr", 0.05));
            int maxAge = Math.Max(1, cfg.Get("MaximumZoneAgeBars", 120));
            int start = Math.Max(2, index - lookback);
            int count = 0;

            for (int i = index; i >= start && count < 24; i--)
            {
                double bullGap = bars.LowPrices[i] - bars.HighPrices[i - 2];
                if (bullGap >= atr * minGap)
                {
                    zones.Add(BuildFvg(
                        bars, i, index, timeframe, CFIPClean78Direction.Buy,
                        bars.HighPrices[i - 2], bars.LowPrices[i], bullGap, atr, maxAge, cfg));
                    count++;
                }

                double bearGap = bars.LowPrices[i - 2] - bars.HighPrices[i];
                if (bearGap >= atr * minGap && count < 24)
                {
                    zones.Add(BuildFvg(
                        bars, i, index, timeframe, CFIPClean78Direction.Sell,
                        bars.HighPrices[i], bars.LowPrices[i - 2], bearGap, atr, maxAge, cfg));
                    count++;
                }
            }
        }

        private CFIPClean78ZoneRecord BuildFvg(
            Bars bars, int created, int current, string timeframe,
            CFIPClean78Direction direction, double lower, double upper,
            double gap, double atr, int maxAge, CFIPClean78ConfigSnapshot cfg)
        {
            bool retested = false, partial = false, consumed = false;
            double currentLower = lower, currentUpper = upper;
            bool breakByWicks = cfg.Get("FvgBreakByWicks", true);

            for (int i = created + 1; i <= current; i++)
            {
                bool overlap =
                    bars.HighPrices[i] >= lower &&
                    bars.LowPrices[i] <= upper;

                if (!overlap) continue;
                retested = true;

                if (direction == CFIPClean78Direction.Buy)
                {
                    if (bars.LowPrices[i] > lower) partial = true;
                    currentLower = Math.Max(lower, Math.Min(upper, bars.LowPrices[i]));
                    if ((breakByWicks && bars.LowPrices[i] <= lower) ||
                        (!breakByWicks && bars.ClosePrices[i] <= lower))
                    { consumed = true; break; }
                }
                else
                {
                    if (bars.HighPrices[i] < upper) partial = true;
                    currentUpper = Math.Min(upper, Math.Max(lower, bars.HighPrices[i]));
                    if ((breakByWicks && bars.HighPrices[i] >= upper) ||
                        (!breakByWicks && bars.ClosePrices[i] >= upper))
                    { consumed = true; break; }
                }
            }

            int age = Math.Max(0, current - created);
            bool expired = age > maxAge;
            bool eligible = !consumed && !expired && currentUpper > currentLower;

            if (consumed) { currentLower = 0; currentUpper = 0; }

            var lifecycle =
                consumed ? CFIPClean78ZoneLifecycle.Consumed :
                expired ? CFIPClean78ZoneLifecycle.Expired :
                partial ? CFIPClean78ZoneLifecycle.PartiallyMitigated :
                retested ? CFIPClean78ZoneLifecycle.Retested :
                CFIPClean78ZoneLifecycle.Active;

            int quality = Clamp(
                55 +
                (int)Math.Round(Math.Min(20, gap / Math.Max(0.0000001, atr) * 12)) +
                (retested ? 8 : 0) -
                (partial ? 6 : 0) -
                (expired ? 25 : 0));

            return new CFIPClean78ZoneRecord(
                "CFIP78|" + timeframe + "|FVG|" + direction + "|" + created,
                CFIPClean78ZoneKind.FairValueGap, direction, timeframe, created,
                bars.OpenTimes[created], lower, upper,
                eligible ? currentLower : 0,
                eligible ? currentUpper : 0,
                age, retested, partial, consumed, consumed, eligible,
                quality, gap / Math.Max(0.0000001, atr),
                false, false, lifecycle,
                CFIPClean78Provenance.Direct(timeframe, "three-candle imbalance FVG"));
        }

        private void BuildObZones(
            IList<CFIPClean78ZoneRecord> zones, Bars bars, int index,
            string timeframe, double atr, CFIPClean78ConfigSnapshot cfg)
        {
            int lookback = Math.Max(5, cfg.Get("ObStructureLookback", 60));
            double threshold = Math.Max(0, cfg.Get("ObDisplacementAtr", 0.80));
            bool require = cfg.Get("RequireObDisplacement", true);
            bool bodyOnly = cfg.Get("ObUseBodyForZone", true);
            int maxAge = Math.Max(1, cfg.Get("MaximumZoneAgeBars", 120));
            int start = Math.Max(2, index - lookback);
            int count = 0;

            for (int impulse = index; impulse >= start && count < 16; impulse--)
            {
                double bodyAtr =
                    Math.Abs(bars.ClosePrices[impulse] - bars.OpenPrices[impulse]) / Math.Max(0.0000001, atr);

                if (require && bodyAtr < threshold) continue;

                CFIPClean78Direction direction =
                    bars.ClosePrices[impulse] > bars.OpenPrices[impulse]
                        ? CFIPClean78Direction.Buy
                        : bars.ClosePrices[impulse] < bars.OpenPrices[impulse]
                            ? CFIPClean78Direction.Sell
                            : CFIPClean78Direction.Wait;

                if (direction == CFIPClean78Direction.Wait) continue;

                int source = -1;
                for (int j = impulse - 1; j >= start; j--)
                {
                    bool opposite =
                        direction == CFIPClean78Direction.Buy
                            ? bars.ClosePrices[j] < bars.OpenPrices[j]
                            : bars.ClosePrices[j] > bars.OpenPrices[j];
                    if (opposite) { source = j; break; }
                }

                if (source < 0) continue;

                double bodyLow = Math.Min(bars.OpenPrices[source], bars.ClosePrices[source]);
                double bodyHigh = Math.Max(bars.OpenPrices[source], bars.ClosePrices[source]);
                double low = bodyOnly ? bodyLow : bars.LowPrices[source];
                double high = bodyOnly ? bodyHigh : bars.HighPrices[source];

                bool retested = false, consumed = false;
                for (int k = source + 1; k <= index; k++)
                {
                    bool overlap =
                        bars.HighPrices[k] >= low &&
                        bars.LowPrices[k] <= high;

                    if (overlap) retested = true;

                    bool breach =
                        direction == CFIPClean78Direction.Buy
                            ? bars.LowPrices[k] <= low
                            : bars.HighPrices[k] >= high;

                    if (breach) { consumed = true; break; }
                }

                int age = Math.Max(0, index - source);
                bool expired = age > maxAge;
                bool eligible = !consumed && !expired && high > low;
                var lifecycle =
                    consumed ? CFIPClean78ZoneLifecycle.Consumed :
                    expired ? CFIPClean78ZoneLifecycle.Expired :
                    retested ? CFIPClean78ZoneLifecycle.Retested :
                    CFIPClean78ZoneLifecycle.Active;

                int quality = Clamp(
                    60 + (int)Math.Round(Math.Min(25, bodyAtr * 12)) +
                    (retested ? 5 : 0) - (expired ? 20 : 0));

                zones.Add(new CFIPClean78ZoneRecord(
                    "CFIP78|" + timeframe + "|OB|" + direction + "|" + source,
                    CFIPClean78ZoneKind.OrderBlock, direction, timeframe, source,
                    bars.OpenTimes[source], low, high,
                    eligible ? low : 0, eligible ? high : 0,
                    age, retested, retested && !consumed,
                    consumed || expired, consumed, eligible,
                    quality, bodyAtr, false, false, lifecycle,
                    CFIPClean78Provenance.Direct(timeframe, "opposite candle before displacement")));
                count++;
            }
        }

        private void BuildEqualLiquidity(
            IList<CFIPClean78LiquidityRecord> liquidity,
            Bars bars, int index, string timeframe,
            double atr, CFIPClean78ConfigSnapshot cfg)
        {
            int lookback = Math.Max(10, cfg.Get("LiquidityLookback", 40));
            int strength = Math.Max(1, cfg.Get("SwingStrength", 3));
            double tolerance =
                atr * Math.Max(0, cfg.Get("EqualLevelToleranceAtr", 0.08));

            int high = FindLatestSwingHigh(bars, index, strength, lookback);
            int low = FindLatestSwingLow(bars, index, strength, lookback);

            if (high >= 0)
            {
                for (int i = high - strength; i >= Math.Max(1, index - lookback); i--)
                    if (IsSwingHigh(bars, i, strength) &&
                        Math.Abs(bars.HighPrices[high] - bars.HighPrices[i]) <= tolerance)
                    {
                        AddLiquidity(
                            liquidity, CFIPClean78LiquidityKind.EqualHigh,
                            CFIPClean78LiquiditySide.Above,
                            CFIPClean78Direction.Wait, timeframe, bars, i,
                            (bars.HighPrices[high] + bars.HighPrices[i]) / 2.0,
                            tolerance, 78, false, "equal highs pool");
                        break;
                    }
            }

            if (low >= 0)
            {
                for (int i = low - strength; i >= Math.Max(1, index - lookback); i--)
                    if (IsSwingLow(bars, i, strength) &&
                        Math.Abs(bars.LowPrices[low] - bars.LowPrices[i]) <= tolerance)
                    {
                        AddLiquidity(
                            liquidity, CFIPClean78LiquidityKind.EqualLow,
                            CFIPClean78LiquiditySide.Below,
                            CFIPClean78Direction.Wait, timeframe, bars, i,
                            (bars.LowPrices[low] + bars.LowPrices[i]) / 2.0,
                            tolerance, 78, false, "equal lows pool");
                        break;
                    }
            }
        }

        private void BuildSweepLiquidity(
            IList<CFIPClean78LiquidityRecord> liquidity,
            IList<CFIPClean78StructureEventRecord> events,
            Bars bars, int index, string timeframe,
            double atr, CFIPClean78ConfigSnapshot cfg)
        {
            int lookback = Math.Max(10, cfg.Get("LiquidityLookback", 40));
            double minDepth = Math.Max(0, atr * cfg.Get("LiquiditySweepMinimumDepthAtr", 0.05));
            double priorLow = Lowest(bars, Math.Max(0, index - lookback), index - 1);
            double priorHigh = Highest(bars, Math.Max(0, index - lookback), index - 1);

            double lowPen = Math.Max(0, priorLow - bars.LowPrices[index]);
            if (lowPen >= minDepth && bars.ClosePrices[index] > priorLow)
            {
                AddLiquidity(liquidity, CFIPClean78LiquidityKind.SwingLow,
                    CFIPClean78LiquiditySide.Below, CFIPClean78Direction.Buy,
                    timeframe, bars, index, priorLow, minDepth, 90, true,
                    "bullish liquidity sweep");

                AddEvent(events, CFIPClean78StructureEventKind.LiquiditySweep,
                    CFIPClean78Direction.Buy, timeframe, bars, index, priorLow,
                    lowPen / Math.Max(0.0000001, atr), 90,
                    "low penetration and recovery");
            }

            double highPen = Math.Max(0, bars.HighPrices[index] - priorHigh);
            if (highPen >= minDepth && bars.ClosePrices[index] < priorHigh)
            {
                AddLiquidity(liquidity, CFIPClean78LiquidityKind.SwingHigh,
                    CFIPClean78LiquiditySide.Above, CFIPClean78Direction.Sell,
                    timeframe, bars, index, priorHigh, minDepth, 90, true,
                    "bearish liquidity sweep");

                AddEvent(events, CFIPClean78StructureEventKind.LiquiditySweep,
                    CFIPClean78Direction.Sell, timeframe, bars, index, priorHigh,
                    highPen / Math.Max(0.0000001, atr), 90,
                    "high penetration and recovery");
            }
        }

        private void AddSwingLiquidity(
            IList<CFIPClean78LiquidityRecord> liquidity,
            Bars bars, int index, string timeframe, double atr,
            CFIPClean78ConfigSnapshot cfg)
        {
            int lookback = Math.Max(10, cfg.Get("LiquidityLookback", 40));
            int strength = Math.Max(1, cfg.Get("SwingStrength", 3));
            int high = FindLatestSwingHigh(bars, index, strength, lookback);
            int low = FindLatestSwingLow(bars, index, strength, lookback);

            if (high >= 0)
                AddLiquidity(liquidity, CFIPClean78LiquidityKind.SwingHigh,
                    CFIPClean78LiquiditySide.Above, CFIPClean78Direction.Wait,
                    timeframe, bars, high, bars.HighPrices[high], atr * 0.02, 72,
                    false, "latest confirmed swing high");

            if (low >= 0)
                AddLiquidity(liquidity, CFIPClean78LiquidityKind.SwingLow,
                    CFIPClean78LiquiditySide.Below, CFIPClean78Direction.Wait,
                    timeframe, bars, low, bars.LowPrices[low], atr * 0.02, 72,
                    false, "latest confirmed swing low");
        }

        private void AddPriorDayWeekLiquidity(
            IList<CFIPClean78LiquidityRecord> liquidity,
            CFIPClean78MtfSnapshot mtf, Bars d1Bars, Bars w1Bars,
            CFIPClean78ConfigSnapshot cfg)
        {
            if (!cfg.Get("UseDailyWeeklyLiquidity", true)) return;

            if (d1Bars != null && mtf.D1.IsAvailable && mtf.D1.ClosedIndex > 0)
            {
                int p = mtf.D1.ClosedIndex - 1;
                AddLiquidity(liquidity, CFIPClean78LiquidityKind.PriorDayHigh,
                    CFIPClean78LiquiditySide.Above, CFIPClean78Direction.Wait,
                    "D1", d1Bars, p, d1Bars.HighPrices[p], 0, 82, false, "prior day high");
                AddLiquidity(liquidity, CFIPClean78LiquidityKind.PriorDayLow,
                    CFIPClean78LiquiditySide.Below, CFIPClean78Direction.Wait,
                    "D1", d1Bars, p, d1Bars.LowPrices[p], 0, 82, false, "prior day low");
            }

            if (w1Bars != null && mtf.W1.IsAvailable && mtf.W1.ClosedIndex > 0)
            {
                int p = mtf.W1.ClosedIndex - 1;
                AddLiquidity(liquidity, CFIPClean78LiquidityKind.PriorWeekHigh,
                    CFIPClean78LiquiditySide.Above, CFIPClean78Direction.Wait,
                    "W1", w1Bars, p, w1Bars.HighPrices[p], 0, 86, false, "prior week high");
                AddLiquidity(liquidity, CFIPClean78LiquidityKind.PriorWeekLow,
                    CFIPClean78LiquiditySide.Below, CFIPClean78Direction.Wait,
                    "W1", w1Bars, p, w1Bars.LowPrices[p], 0, 86, false, "prior week low");
            }
        }

        private void AddSessionLiquidity(
            IList<CFIPClean78LiquidityRecord> liquidity,
            CFIPClean78MtfSnapshot mtf, Bars bars,
            CFIPClean78ConfigSnapshot cfg)
        {
            if (!cfg.Get("UseSessionLiquidityTargets", true) ||
                bars == null || !mtf.M5.IsAvailable) return;

            int startHour = Math.Max(0, Math.Min(23, cfg.Get("SessionStartUtc", 6)));
            int endHour = Math.Max(0, Math.Min(23, cfg.Get("SessionEndUtc", 20)));

            DateTime sessionStart = new DateTime(
                mtf.ReferenceUtc.Year, mtf.ReferenceUtc.Month, mtf.ReferenceUtc.Day,
                startHour, 0, 0, DateTimeKind.Utc);

            DateTime sessionEnd = new DateTime(
                mtf.ReferenceUtc.Year, mtf.ReferenceUtc.Month, mtf.ReferenceUtc.Day,
                endHour, 0, 0, DateTimeKind.Utc);

            if (endHour <= startHour)
            {
                if (mtf.ReferenceUtc < sessionStart) sessionStart = sessionStart.AddDays(-1);
                sessionEnd = sessionStart.AddDays(1);
                sessionEnd = new DateTime(
                    sessionEnd.Year, sessionEnd.Month, sessionEnd.Day,
                    endHour, 0, 0, DateTimeKind.Utc);
            }

            double high = double.MinValue, low = double.MaxValue;
            int highIndex = -1, lowIndex = -1;
            int first = Math.Max(0, mtf.M5.ClosedIndex - 700);

            for (int i = first; i <= mtf.M5.ClosedIndex; i++)
            {
                DateTime t = bars.OpenTimes[i];
                if (t < sessionStart || t >= sessionEnd) continue;

                if (bars.HighPrices[i] > high) { high = bars.HighPrices[i]; highIndex = i; }
                if (bars.LowPrices[i] < low) { low = bars.LowPrices[i]; lowIndex = i; }
            }

            if (highIndex >= 0)
                AddLiquidity(liquidity, CFIPClean78LiquidityKind.SessionHigh,
                    CFIPClean78LiquiditySide.Above, CFIPClean78Direction.Wait,
                    "M5", bars, highIndex, high, 0, 74, false, "configured session high");

            if (lowIndex >= 0)
                AddLiquidity(liquidity, CFIPClean78LiquidityKind.SessionLow,
                    CFIPClean78LiquiditySide.Below, CFIPClean78Direction.Wait,
                    "M5", bars, lowIndex, low, 0, 74, false, "configured session low");
        }

        private void AddDailyPivots(
            IList<CFIPClean78LiquidityRecord> liquidity,
            CFIPClean78MtfSnapshot mtf, Bars d1Bars,
            CFIPClean78ConfigSnapshot cfg)
        {
            if (!cfg.Get("UseDailyPivots", true) ||
                d1Bars == null || !mtf.D1.IsAvailable ||
                mtf.D1.ClosedIndex <= 0) return;

            int p = mtf.D1.ClosedIndex - 1;
            double h = d1Bars.HighPrices[p], l = d1Bars.LowPrices[p], c = d1Bars.ClosePrices[p];
            double pivot = (h + l + c) / 3.0, range = h - l;

            AddLiquidity(liquidity, CFIPClean78LiquidityKind.DailyPivot,
                CFIPClean78LiquiditySide.Neutral, CFIPClean78Direction.Wait,
                "D1", d1Bars, p, pivot, 0, 62, false, "daily pivot");

            AddLiquidity(liquidity, CFIPClean78LiquidityKind.DailyR1,
                CFIPClean78LiquiditySide.Above, CFIPClean78Direction.Wait,
                "D1", d1Bars, p, 2 * pivot - l, 0, 64, false, "daily R1");

            AddLiquidity(liquidity, CFIPClean78LiquidityKind.DailyR2,
                CFIPClean78LiquiditySide.Above, CFIPClean78Direction.Wait,
                "D1", d1Bars, p, pivot + range, 0, 66, false, "daily R2");

            AddLiquidity(liquidity, CFIPClean78LiquidityKind.DailyS1,
                CFIPClean78LiquiditySide.Below, CFIPClean78Direction.Wait,
                "D1", d1Bars, p, 2 * pivot - h, 0, 64, false, "daily S1");

            AddLiquidity(liquidity, CFIPClean78LiquidityKind.DailyS2,
                CFIPClean78LiquiditySide.Below, CFIPClean78Direction.Wait,
                "D1", d1Bars, p, pivot - range, 0, 66, false, "daily S2");
        }

        private void MarkForecastLiquidity(
            IList<CFIPClean78LiquidityRecord> liquidity,
            CFIPClean78MarketFrame m5)
        {
            if (m5 == null ||
                !m5.DataValid)
                return;

            double price = m5.Close;

            for (int i = 0; i < liquidity.Count; i++)
            {
                CFIPClean78LiquidityRecord item = liquidity[i];

                bool ahead =
                    (item.Side == CFIPClean78LiquiditySide.Above &&
                     item.Price > price) ||
                    (item.Side == CFIPClean78LiquiditySide.Below &&
                     item.Price < price);

                if (!ahead ||
                    item.Swept ||
                    item.ForecastCandidate)
                    continue;

                liquidity[i] =
                    new CFIPClean78LiquidityRecord(
                        item.Id,
                        CFIPClean78LiquidityKind.Forecast,
                        item.Side,
                        item.SweepDirection,
                        item.Timeframe,
                        item.BarIndex,
                        item.TimeUtc,
                        item.Price,
                        item.Tolerance,
                        item.Distance,
                        item.Penetration,
                        item.Swept,
                        true,
                        item.Quality,
                        item.Provenance);
            }
        }

        private void MarkConfluence(
            IList<CFIPClean78ZoneRecord> zones,
            IList<CFIPClean78LiquidityRecord> liquidity,
            CFIPClean78ConfigSnapshot cfg)
        {
            bool enabled = cfg.Get("UseZoneConfluence", true);

            for (int i = 0; i < zones.Count; i++)
            {
                var z = zones[i];
                bool fvg = z.Kind == CFIPClean78ZoneKind.OrderBlock &&
                           HasOverlap(zones, z, CFIPClean78ZoneKind.FairValueGap);
                bool liq = enabled && HasNearbyLiquidity(liquidity, z);

                int q = Clamp(z.Quality +
                              (fvg ? 8 : 0) +
                              (liq ? cfg.Get("SmartLiquidityPoolBonus", 12) : 0));

                zones[i] = new CFIPClean78ZoneRecord(
                    z.Id, z.Kind, z.Direction, z.Timeframe, z.CreatedIndex, z.CreatedUtc,
                    z.OriginalLower, z.OriginalUpper, z.CurrentLower, z.CurrentUpper,
                    z.AgeBars, z.Retested, z.Mitigated, z.Invalidated, z.Consumed, z.ExecutionEligible,
                    q, z.DisplacementAtr, liq, fvg, z.Lifecycle, z.Provenance);
            }
        }

        private bool HasOverlap(
            IList<CFIPClean78ZoneRecord> zones, CFIPClean78ZoneRecord source,
            CFIPClean78ZoneKind kind)
        {
            for (int i = 0; i < zones.Count; i++)
            {
                var z = zones[i];
                if (z.Id == source.Id || z.Kind != kind ||
                    z.Direction != source.Direction || z.Consumed || z.Invalidated)
                    continue;
                if (z.CurrentUpper >= source.CurrentLower &&
                    z.CurrentLower <= source.CurrentUpper)
                    return true;
            }
            return false;
        }

        private bool HasNearbyLiquidity(
            IList<CFIPClean78LiquidityRecord> liquidity,
            CFIPClean78ZoneRecord zone)
        {
            double width = Math.Max(0, zone.CurrentUpper - zone.CurrentLower);
            for (int i = 0; i < liquidity.Count; i++)
            {
                if (liquidity[i].Price >= zone.CurrentLower - width &&
                    liquidity[i].Price <= zone.CurrentUpper + width)
                    return true;
            }
            return false;
        }

        private CFIPClean78Direction ResolveDirection(
            IList<CFIPClean78StructureEventRecord> events)
        {
            CFIPClean78StructureEventRecord bull = null, bear = null;
            for (int i = 0; i < events.Count; i++)
            {
                var e = events[i];
                if (!IsDirectional(e.Kind)) continue;

                if (e.Direction == CFIPClean78Direction.Buy &&
                    (bull == null || e.TimeUtc > bull.TimeUtc)) bull = e;

                if (e.Direction == CFIPClean78Direction.Sell &&
                    (bear == null || e.TimeUtc > bear.TimeUtc)) bear = e;
            }

            if (bull == null && bear == null) return CFIPClean78Direction.Wait;
            if (bear == null) return CFIPClean78Direction.Buy;
            if (bull == null) return CFIPClean78Direction.Sell;
            return bull.TimeUtc > bear.TimeUtc
                ? CFIPClean78Direction.Buy
                : CFIPClean78Direction.Sell;
        }

        private bool IsDirectional(CFIPClean78StructureEventKind kind)
        {
            return kind == CFIPClean78StructureEventKind.BreakOfStructure ||
                   kind == CFIPClean78StructureEventKind.MarketStructureShift ||
                   kind == CFIPClean78StructureEventKind.ChangeOfCharacter ||
                   kind == CFIPClean78StructureEventKind.Displacement ||
                   kind == CFIPClean78StructureEventKind.LiquiditySweep;
        }

        private int CalculateQuality(
            IList<CFIPClean78StructureEventRecord> events,
            IList<CFIPClean78ZoneRecord> zones,
            IList<CFIPClean78LiquidityRecord> liquidity,
            CFIPClean78Direction direction)
        {
            int eventScore = 0, eventCount = 0, zoneScore = 0, zoneCount = 0, swept = 0;

            for (int i = 0; i < events.Count; i++)
                if (events[i].Direction == direction && IsDirectional(events[i].Kind))
                { eventScore += events[i].Quality; eventCount++; }

            for (int i = 0; i < zones.Count; i++)
                if (zones[i].Direction == direction && zones[i].ExecutionEligible)
                { zoneScore += zones[i].Quality; zoneCount++; }

            for (int i = 0; i < liquidity.Count; i++)
                if (liquidity[i].Swept) swept += liquidity[i].Quality;

            double e = eventCount > 0 ? (double)eventScore / eventCount : 0;
            double z = zoneCount > 0 ? (double)zoneScore / zoneCount : 0;

            return Clamp((int)Math.Round(
                e * 0.55 + Math.Min(100, z) * 0.30 + Math.Min(100, swept) * 0.15));
        }

        private CFIPClean78PremiumDiscountState BuildPremiumDiscount(
            Bars bars, int index, CFIPClean78ConfigSnapshot cfg)
        {
            if (!cfg.Get("UsePremiumDiscount", true) || bars == null || index <= 5)
                return new CFIPClean78PremiumDiscountState(false, 0, 0, 0);

            int lookback = Math.Max(10, cfg.Get("LiquidityLookback", 40));
            double high = Highest(bars, Math.Max(0, index - lookback), index);
            double low = Lowest(bars, Math.Max(0, index - lookback), index);
            double range = high - low;
            if (range <= 0) return new CFIPClean78PremiumDiscountState(false, 0, 0, 0);

            return new CFIPClean78PremiumDiscountState(
                true, low, high, (bars.ClosePrices[index] - low) / range);
        }

        private bool IsBearishPreBreak(Bars bars, int index, int referenceHigh)
        {
            if (index < 3) return false;
            double level = referenceHigh >= 0 ? bars.HighPrices[referenceHigh] :
                           Highest(bars, Math.Max(0, index - 8), index - 1);
            return bars.ClosePrices[index - 1] > level;
        }

        private bool IsBullishPreBreak(Bars bars, int index, int referenceLow)
        {
            if (index < 3) return false;
            double level = referenceLow >= 0 ? bars.LowPrices[referenceLow] :
                           Lowest(bars, Math.Max(0, index - 8), index - 1);
            return bars.ClosePrices[index - 1] < level;
        }

        private bool IsCharacterBreakBull(
            Bars bars, int index, int strength, int lookback, double atr, double breakAtr)
        {
            int h = FindLatestSwingHigh(bars, index - 1, strength, lookback);
            return h >= 0 &&
                   bars.ClosePrices[index] > bars.HighPrices[h] + atr * breakAtr &&
                   bars.ClosePrices[index - 1] <= bars.HighPrices[h];
        }

        private bool IsCharacterBreakBear(
            Bars bars, int index, int strength, int lookback, double atr, double breakAtr)
        {
            int l = FindLatestSwingLow(bars, index - 1, strength, lookback);
            return l >= 0 &&
                   bars.ClosePrices[index] < bars.LowPrices[l] - atr * breakAtr &&
                   bars.ClosePrices[index - 1] >= bars.LowPrices[l];
        }

        private int FindLatestSwingHigh(Bars bars, int index, int strength, int lookback)
        {
            int start = Math.Max(strength, index - Math.Max(strength * 2, lookback));
            int end = Math.Min(bars.Count - strength - 1, index - strength);
            for (int i = end; i >= start; i--)
                if (IsSwingHigh(bars, i, strength)) return i;
            return -1;
        }

        private int FindLatestSwingLow(Bars bars, int index, int strength, int lookback)
        {
            int start = Math.Max(strength, index - Math.Max(strength * 2, lookback));
            int end = Math.Min(bars.Count - strength - 1, index - strength);
            for (int i = end; i >= start; i--)
                if (IsSwingLow(bars, i, strength)) return i;
            return -1;
        }

        private bool IsSwingHigh(Bars bars, int index, int strength)
        {
            if (bars == null || index - strength < 0 || index + strength >= bars.Count) return false;
            double level = bars.HighPrices[index];
            for (int i = 1; i <= strength; i++)
                if (bars.HighPrices[index - i] > level ||
                    bars.HighPrices[index + i] > level) return false;
            return true;
        }

        private bool IsSwingLow(Bars bars, int index, int strength)
        {
            if (bars == null || index - strength < 0 || index + strength >= bars.Count) return false;
            double level = bars.LowPrices[index];
            for (int i = 1; i <= strength; i++)
                if (bars.LowPrices[index - i] < level ||
                    bars.LowPrices[index + i] < level) return false;
            return true;
        }

        private double Highest(Bars bars, int start, int end)
        {
            if (bars == null || bars.Count == 0 || start > end) return 0;
            double value = double.MinValue;
            for (int i = Math.Max(0, start); i <= Math.Min(bars.Count - 1, end); i++)
                value = Math.Max(value, bars.HighPrices[i]);
            return value == double.MinValue ? 0 : value;
        }

        private double Lowest(Bars bars, int start, int end)
        {
            if (bars == null || bars.Count == 0 || start > end) return 0;
            double value = double.MaxValue;
            for (int i = Math.Max(0, start); i <= Math.Min(bars.Count - 1, end); i++)
                value = Math.Min(value, bars.LowPrices[i]);
            return value == double.MaxValue ? 0 : value;
        }

        private void AddEvent(
            IList<CFIPClean78StructureEventRecord> events,
            CFIPClean78StructureEventKind kind, CFIPClean78Direction direction,
            string timeframe, Bars bars, int index, double price,
            double strengthAtr, int quality, string detail)
        {
            events.Add(new CFIPClean78StructureEventRecord(
                "CFIP78|" + timeframe + "|" + kind + "|" + index,
                kind, direction, timeframe, index, bars.OpenTimes[index], price,
                strengthAtr, quality,
                CFIPClean78Provenance.Direct("STRUCTURE", detail)));
        }

        private void AddLiquidity(
            IList<CFIPClean78LiquidityRecord> list,
            CFIPClean78LiquidityKind kind, CFIPClean78LiquiditySide side,
            CFIPClean78Direction sweepDirection, string timeframe, Bars bars,
            int index, double price, double tolerance, int quality,
            bool swept, string detail)
        {
            int safe = Math.Max(0, Math.Min(index, bars.Count - 1));
            double reference = bars.ClosePrices[safe];
            list.Add(new CFIPClean78LiquidityRecord(
                "CFIP78|" + timeframe + "|" + kind + "|" + index,
                kind, side, sweepDirection, timeframe, safe, bars.OpenTimes[safe],
                price, tolerance, Math.Abs(price - reference),
                swept ? tolerance : 0, swept, false, quality,
                CFIPClean78Provenance.Direct(timeframe, detail)));
        }

        private int Clamp(int value)
        {
            return Math.Max(0, Math.Min(100, value));
        }
    }

    // ------------------------------------------------------------------------
    // Decision
    // ------------------------------------------------------------------------

    public sealed class CFIPClean78DecisionSnapshot
    {
        private readonly ReadOnlyCollection<CFIPClean78BlockReason> _blockReasons;

        public CFIPClean78Direction Direction { get; private set; }
        public int Confidence { get; private set; }
        public int Quality { get; private set; }
        public int Edge { get; private set; }
        public int MtfAgreement { get; private set; }
        public int IndependentEvidence { get; private set; }
        public int StructuralConfirmations { get; private set; }
        public string Regime { get; private set; }
        public int RegimeQuality { get; private set; }
        public bool EntryEligible { get; private set; }
        public CFIPClean78DecisionPolicyMode PolicyMode { get; private set; }
        public IReadOnlyList<CFIPClean78BlockReason> BlockReasons
        {
            get { return _blockReasons; }
        }
        public CFIPClean78Provenance Provenance { get; private set; }

        public CFIPClean78DecisionSnapshot(
            CFIPClean78Direction direction,
            int confidence,
            int quality,
            int edge,
            int mtfAgreement,
            int independentEvidence,
            int structuralConfirmations,
            string regime,
            int regimeQuality,
            bool entryEligible,
            CFIPClean78DecisionPolicyMode policyMode,
            IList<CFIPClean78BlockReason> blockReasons,
            CFIPClean78Provenance provenance)
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
                new ReadOnlyCollection<CFIPClean78BlockReason>(
                    new List<CFIPClean78BlockReason>(
                        blockReasons ??
                        new List<CFIPClean78BlockReason>()));
            Provenance =
                provenance ??
                CFIPClean78Provenance.Direct(
                    "UNKNOWN",
                    "UNSPECIFIED");
        }
    }

    // ------------------------------------------------------------------------
    // Trade identity / idempotency
    // ------------------------------------------------------------------------

    public sealed class CFIPClean78TradeIdentity
    {
        public string StrategyId { get; private set; }
        public string Symbol { get; private set; }
        public string PlanId { get; private set; }
        public string SignalId { get; private set; }

        public CFIPClean78TradeIdentity(
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

    public sealed class CFIPClean78ExecutionIdentity
    {
        public string IntentId { get; private set; }
        public string IdempotencyKey { get; private set; }
        public DateTime CreatedUtc { get; private set; }

        public CFIPClean78ExecutionIdentity(
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

    public sealed class CFIPClean78TradePlan
    {
        public CFIPClean78TradeIdentity Identity { get; private set; }
        public CFIPClean78Direction Direction { get; private set; }
        public CFIPClean78EntryMode EntryMode { get; private set; }
        public CFIPClean78EntryModel Entry { get; private set; }
        public CFIPClean78PriceLevel StructuralStop { get; private set; }
        public CFIPClean78TargetLadder TargetLadder { get; private set; }
        public double RiskRewardToTp1 { get; private set; }
        public double RiskRewardToFinalTarget { get; private set; }
        public int LevelQuality { get; private set; }
        public bool IsValid { get; private set; }
        public DateTime CreatedUtc { get; private set; }
        public int ReferenceBarIndex { get; private set; }
        public CFIPClean78Provenance Provenance { get; private set; }

        public CFIPClean78TradePlan(
            CFIPClean78TradeIdentity identity,
            CFIPClean78Direction direction,
            CFIPClean78EntryMode entryMode,
            CFIPClean78EntryModel entry,
            CFIPClean78PriceLevel structuralStop,
            CFIPClean78TargetLadder targetLadder,
            double riskRewardToTp1,
            double riskRewardToFinalTarget,
            int levelQuality,
            bool isValid,
            DateTime createdUtc,
            int referenceBarIndex,
            CFIPClean78Provenance provenance)
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
                CFIPClean78Provenance.Direct(
                    "UNKNOWN",
                    "UNSPECIFIED");
        }
    }

    // ------------------------------------------------------------------------
    // Execution intent / result
    // ------------------------------------------------------------------------

    public sealed class CFIPClean78RiskRequest
    {
        public double RiskPercentEquity { get; private set; }
        public double RequestedVolumeInUnits { get; private set; }
        public double MaxRiskAmount { get; private set; }

        public CFIPClean78RiskRequest(
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

    public sealed class CFIPClean78ExecutionIntent
    {
        public CFIPClean78ExecutionIdentity Identity { get; private set; }
        public CFIPClean78TradeIdentity TradeIdentity { get; private set; }
        public CFIPClean78Direction Direction { get; private set; }
        public CFIPClean78ExecutionKind Kind { get; private set; }
        public CFIPClean78EntryMode EntryMode { get; private set; }

        // Exact value requested from the broker.
        public CFIPClean78PriceLevel RequestedEntry { get; private set; }

        // Structural activation threshold, when applicable.
        public CFIPClean78PriceLevel Trigger { get; private set; }

        // Protection requested at execution time.
        public CFIPClean78PriceLevel StopLoss { get; private set; }

        // This is the requested active broker target, not the whole ladder.
        public CFIPClean78PriceLevel EffectiveTarget { get; private set; }

        public double VolumeInUnits { get; private set; }
        public CFIPClean78RiskRequest Risk { get; private set; }
        public CFIPClean78DecisionPolicyMode PolicyMode { get; private set; }
        public DateTime CreatedUtc { get; private set; }
        public DateTime? ExpiryUtc { get; private set; }

        public CFIPClean78ExecutionIntent(
            CFIPClean78ExecutionIdentity identity,
            CFIPClean78TradeIdentity tradeIdentity,
            CFIPClean78Direction direction,
            CFIPClean78ExecutionKind kind,
            CFIPClean78EntryMode entryMode,
            CFIPClean78PriceLevel requestedEntry,
            CFIPClean78PriceLevel trigger,
            CFIPClean78PriceLevel stopLoss,
            CFIPClean78PriceLevel effectiveTarget,
            double volumeInUnits,
            CFIPClean78RiskRequest risk,
            CFIPClean78DecisionPolicyMode policyMode,
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

    public sealed class CFIPClean78ExecutionResult
    {
        public bool Accepted { get; private set; }
        public string BrokerOrderId { get; private set; }
        public string BrokerPositionId { get; private set; }
        public CFIPClean78PriceLevel ActualFill { get; private set; }
        public double RequestedVsActualDelta { get; private set; }
        public string BrokerError { get; private set; }
        public CFIPClean78ProtectionState ProtectionState { get; private set; }
        public bool ReconciliationRequired { get; private set; }

        public CFIPClean78ExecutionResult(
            bool accepted,
            string brokerOrderId,
            string brokerPositionId,
            CFIPClean78PriceLevel actualFill,
            double requestedVsActualDelta,
            string brokerError,
            CFIPClean78ProtectionState protectionState,
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

    public sealed class CFIPClean78BrokerPositionSnapshot
    {
        public string BrokerPositionId { get; private set; }
        public string Label { get; private set; }
        public string Symbol { get; private set; }
        public CFIPClean78Direction Direction { get; private set; }
        public double EntryPrice { get; private set; }
        public double VolumeInUnits { get; private set; }
        public double? StopLoss { get; private set; }
        public double? TakeProfit { get; private set; }
        public double NetProfit { get; private set; }
        public bool IsOpen { get; private set; }

        public CFIPClean78BrokerPositionSnapshot(
            string brokerPositionId,
            string label,
            string symbol,
            CFIPClean78Direction direction,
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

    public sealed class CFIPClean78BrokerPendingOrderSnapshot
    {
        public string BrokerOrderId { get; private set; }
        public string Label { get; private set; }
        public string Symbol { get; private set; }
        public CFIPClean78Direction Direction { get; private set; }
        public CFIPClean78ExecutionKind Kind { get; private set; }
        public double RequestedEntry { get; private set; }
        public double VolumeInUnits { get; private set; }
        public double? StopLoss { get; private set; }
        public double? TakeProfit { get; private set; }
        public bool IsActive { get; private set; }

        public CFIPClean78BrokerPendingOrderSnapshot(
            string brokerOrderId,
            string label,
            string symbol,
            CFIPClean78Direction direction,
            CFIPClean78ExecutionKind kind,
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

    public sealed class CFIPClean78BrokerStateSnapshot
    {
        private readonly ReadOnlyCollection<CFIPClean78BrokerPositionSnapshot> _positions;
        private readonly ReadOnlyCollection<CFIPClean78BrokerPendingOrderSnapshot> _pendingOrders;

        public IReadOnlyList<CFIPClean78BrokerPositionSnapshot> Positions
        {
            get { return _positions; }
        }

        public IReadOnlyList<CFIPClean78BrokerPendingOrderSnapshot> PendingOrders
        {
            get { return _pendingOrders; }
        }

        public CFIPClean78BrokerStateSnapshot(
            IList<CFIPClean78BrokerPositionSnapshot> positions,
            IList<CFIPClean78BrokerPendingOrderSnapshot> pendingOrders)
        {
            _positions =
                new ReadOnlyCollection<CFIPClean78BrokerPositionSnapshot>(
                    new List<CFIPClean78BrokerPositionSnapshot>(
                        positions ??
                        new List<CFIPClean78BrokerPositionSnapshot>()));

            _pendingOrders =
                new ReadOnlyCollection<CFIPClean78BrokerPendingOrderSnapshot>(
                    new List<CFIPClean78BrokerPendingOrderSnapshot>(
                        pendingOrders ??
                        new List<CFIPClean78BrokerPendingOrderSnapshot>()));
        }
    }

    // ------------------------------------------------------------------------
    // Lifecycle authority
    // ------------------------------------------------------------------------

    public sealed class CFIPClean78LifecycleTransition
    {
        public CFIPClean78LifecycleState From { get; private set; }
        public CFIPClean78LifecycleState To { get; private set; }
        public DateTime TimeUtc { get; private set; }
        public string Reason { get; private set; }

        public CFIPClean78LifecycleTransition(
            CFIPClean78LifecycleState from,
            CFIPClean78LifecycleState to,
            DateTime timeUtc,
            string reason)
        {
            From = from;
            To = to;
            TimeUtc = timeUtc;
            Reason = reason ?? string.Empty;
        }
    }

    public sealed class CFIPClean78LifecycleManager
    {
        private CFIPClean78LifecycleState _state;

        public CFIPClean78LifecycleState State
        {
            get { return _state; }
        }

        public CFIPClean78LifecycleManager()
        {
            _state = CFIPClean78LifecycleState.Flat;
        }

        public bool TryTransition(
            CFIPClean78LifecycleState target,
            DateTime timeUtc,
            string reason)
        {
            if (!IsAllowed(_state, target))
                return false;

            _state = target;
            return true;
        }

        private static bool IsAllowed(
            CFIPClean78LifecycleState from,
            CFIPClean78LifecycleState to)
        {
            if (from == to)
                return true;

            switch (from)
            {
                case CFIPClean78LifecycleState.Flat:
                    return
                        to == CFIPClean78LifecycleState.SignalDetected ||
                        to == CFIPClean78LifecycleState.Closed;

                case CFIPClean78LifecycleState.SignalDetected:
                    return
                        to == CFIPClean78LifecycleState.PlanReady ||
                        to == CFIPClean78LifecycleState.Rejected ||
                        to == CFIPClean78LifecycleState.Flat;

                case CFIPClean78LifecycleState.PlanReady:
                    return
                        to == CFIPClean78LifecycleState.ExecutionReady ||
                        to == CFIPClean78LifecycleState.Rejected ||
                        to == CFIPClean78LifecycleState.Flat;

                case CFIPClean78LifecycleState.ExecutionReady:
                    return
                        to == CFIPClean78LifecycleState.PendingOrder ||
                        to == CFIPClean78LifecycleState.LivePosition ||
                        to == CFIPClean78LifecycleState.Rejected ||
                        to == CFIPClean78LifecycleState.Error;

                case CFIPClean78LifecycleState.PendingOrder:
                    return
                        to == CFIPClean78LifecycleState.LivePosition ||
                        to == CFIPClean78LifecycleState.RecoveryRequired ||
                        to == CFIPClean78LifecycleState.Flat ||
                        to == CFIPClean78LifecycleState.Error;

                case CFIPClean78LifecycleState.LivePosition:
                    return
                        to == CFIPClean78LifecycleState.ExitRequested ||
                        to == CFIPClean78LifecycleState.RecoveryRequired ||
                        to == CFIPClean78LifecycleState.Closed ||
                        to == CFIPClean78LifecycleState.Error;

                case CFIPClean78LifecycleState.ExitRequested:
                    return
                        to == CFIPClean78LifecycleState.Closed ||
                        to == CFIPClean78LifecycleState.RecoveryRequired ||
                        to == CFIPClean78LifecycleState.Error;

                case CFIPClean78LifecycleState.RecoveryRequired:
                    return
                        to == CFIPClean78LifecycleState.LivePosition ||
                        to == CFIPClean78LifecycleState.Closed ||
                        to == CFIPClean78LifecycleState.Error ||
                        to == CFIPClean78LifecycleState.Flat;

                case CFIPClean78LifecycleState.Closed:
                    return
                        to == CFIPClean78LifecycleState.SignalDetected ||
                        to == CFIPClean78LifecycleState.Flat;

                case CFIPClean78LifecycleState.Rejected:
                    return
                        to == CFIPClean78LifecycleState.Flat ||
                        to == CFIPClean78LifecycleState.SignalDetected;

                case CFIPClean78LifecycleState.Error:
                    return
                        to == CFIPClean78LifecycleState.RecoveryRequired ||
                        to == CFIPClean78LifecycleState.Flat;

                default:
                    return false;
            }
        }
    }

    // ------------------------------------------------------------------------
    // Configuration / runtime separation
    // ------------------------------------------------------------------------

    public sealed class CFIPClean78ExecutionEnvelope
    {
        public double MaxEntryChaseAtr { get; private set; }
        public double MaxBrokerSlippagePips { get; private set; }
        public double MaxBreakoutFillDeviationPips { get; private set; }
        public double MaxPlanRebaseDistancePips { get; private set; }

        public CFIPClean78ExecutionEnvelope(
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

    public enum CFIPClean78ConfigurationSection
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

    public sealed class CFIPClean78ConfigurationSectionSnapshot
    {
        private readonly ReadOnlyDictionary<string, object> _values;

        public CFIPClean78ConfigurationSection Section { get; private set; }
        public IReadOnlyDictionary<string, object> Values { get { return _values; } }

        public CFIPClean78ConfigurationSectionSnapshot(
            CFIPClean78ConfigurationSection section,
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

    public sealed class CFIPClean78ConfigSnapshot
    {
        private readonly ReadOnlyDictionary<string, object> _values;
        private readonly ReadOnlyDictionary<string, CFIPClean78ConfigurationSectionSnapshot> _sections;

        public string StrategyId { get; private set; }
        public int ParameterCount { get { return _values.Count; } }
        public bool ManualTradeEntryControlsSupported { get { return false; } }

        public IReadOnlyDictionary<string, object> Values { get { return _values; } }
        public IReadOnlyDictionary<string, CFIPClean78ConfigurationSectionSnapshot> Sections
        {
            get { return _sections; }
        }

        private CFIPClean78ConfigSnapshot(
            string strategyId,
            IDictionary<string, object> values,
            IDictionary<string, CFIPClean78ConfigurationSectionSnapshot> sections)
        {
            StrategyId = strategyId ?? string.Empty;
            _values = new ReadOnlyDictionary<string, object>(
                new Dictionary<string, object>(values));

            _sections =
                new ReadOnlyDictionary<string, CFIPClean78ConfigurationSectionSnapshot>(
                    new Dictionary<string, CFIPClean78ConfigurationSectionSnapshot>(sections));
        }

        public static CFIPClean78ConfigSnapshot Build(
            object parameterSource,
            string strategyId)
        {
            if (parameterSource == null)
                throw new ArgumentNullException("parameterSource");

            var values =
                new Dictionary<string, object>(StringComparer.Ordinal);

            var buckets =
                new Dictionary<CFIPClean78ConfigurationSection, IDictionary<string, object>>();

            foreach (CFIPClean78ConfigurationSection section in Enum.GetValues(
                typeof(CFIPClean78ConfigurationSection)))
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

                CFIPClean78ConfigurationSection section =
                    ResolveSection(group);

                buckets[section][property.Name] = value;
            }

            var sections =
                new Dictionary<string, CFIPClean78ConfigurationSectionSnapshot>(
                    StringComparer.Ordinal);

            foreach (var pair in buckets)
            {
                sections[pair.Key.ToString()] =
                    new CFIPClean78ConfigurationSectionSnapshot(
                        pair.Key,
                        pair.Value);
            }

            return new CFIPClean78ConfigSnapshot(
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

        public static CFIPClean78ConfigurationSection ResolveSection(
            string group)
        {
            string value = group ?? string.Empty;

            if (value.Contains("01 · Decision"))
                return CFIPClean78ConfigurationSection.Decision;
            if (value.Contains("02 · MTF"))
                return CFIPClean78ConfigurationSection.Mtf;
            if (value.Contains("03 · Structure"))
                return CFIPClean78ConfigurationSection.Structure;
            if (value.Contains("04 · Zones"))
                return CFIPClean78ConfigurationSection.Zones;
            if (value.Contains("05 · Liquidity"))
                return CFIPClean78ConfigurationSection.Liquidity;
            if (value.Contains("06 · Indicators"))
                return CFIPClean78ConfigurationSection.Indicators;
            if (value.Contains("07 · Entry"))
                return CFIPClean78ConfigurationSection.Entry;
            if (value.Contains("08 · Smart Weights"))
                return CFIPClean78ConfigurationSection.SmartWeights;
            if (value.Contains("09 · Risk & Targets"))
                return CFIPClean78ConfigurationSection.RiskTargets;
            if (value.Contains("10 · Live Management"))
                return CFIPClean78ConfigurationSection.LiveManagement;
            if (value.Contains("11 · Filters"))
                return CFIPClean78ConfigurationSection.Filters;
            if (value.Contains("12 · ALERTS"))
                return CFIPClean78ConfigurationSection.Alerts;
            if (value.Contains("13 · AUTO TRADING"))
                return CFIPClean78ConfigurationSection.Automation;
            if (value.Contains("24 · SMART EXECUTION"))
                return CFIPClean78ConfigurationSection.SmartExecution;
            if (value.Contains("14 · DISPLAY"))
                return CFIPClean78ConfigurationSection.Display;
            if (value.Contains("15 · CONTROL"))
                return CFIPClean78ConfigurationSection.Control;
            if (value.Contains("20 · Confluence"))
                return CFIPClean78ConfigurationSection.ConfluenceExtensions;
            if (value.Contains("21 · Complete Intelligence"))
                return CFIPClean78ConfigurationSection.CompleteIntelligence;
            if (value.Contains("23 · Structural Execution"))
                return CFIPClean78ConfigurationSection.StructuralExecution;
            if (value.Contains("22 · Safety"))
                return CFIPClean78ConfigurationSection.SafetyPrecision;
            if (value.Contains("15 · INTELLIGENCE — EARLY"))
                return CFIPClean78ConfigurationSection.EarlyIntelligence;
            if (value.Contains("16 · Accuracy"))
                return CFIPClean78ConfigurationSection.Accuracy;
            if (value.Contains("17 · Smart Engine"))
                return CFIPClean78ConfigurationSection.SmartEngine;

            return CFIPClean78ConfigurationSection.Unknown;
        }
    }

    public sealed class CFIPClean78RuntimeAuthority
    {
        public bool AutoTradingEnabled { get; private set; }
        public bool AutomaticOrdersEnabled { get; private set; }

        public void InitializeFromConfiguration(
            CFIPClean78ConfigSnapshot configuration)
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

    public sealed class CFIPClean78OutcomeEvent
    {
        public CFIPClean78TradeIdentity TradeIdentity { get; private set; }
        public double EntryPrice { get; private set; }
        public double ExitPrice { get; private set; }
        public double ResultAmount { get; private set; }
        public double ResultR { get; private set; }
        public double MaxFavorableExcursion { get; private set; }
        public double MaxAdverseExcursion { get; private set; }
        public CFIPClean78TargetStage HighestTargetStageReached { get; private set; }
        public string ExitReason { get; private set; }
        public DateTime EntryUtc { get; private set; }
        public DateTime ExitUtc { get; private set; }

        public CFIPClean78OutcomeEvent(
            CFIPClean78TradeIdentity tradeIdentity,
            double entryPrice,
            double exitPrice,
            double resultAmount,
            double resultR,
            double maxFavorableExcursion,
            double maxAdverseExcursion,
            CFIPClean78TargetStage highestTargetStageReached,
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

    public sealed class CFIPClean78PresentationState
    {
        public DateTime GeneratedUtc { get; private set; }
        public CFIPClean78Direction Direction { get; private set; }
        public CFIPClean78LifecycleState LifecycleState { get; private set; }
        public string Readiness { get; private set; }
        public string ExecutionStatus { get; private set; }
        public string BrokerStatus { get; private set; }
        public string AlertStatus { get; private set; }

        public CFIPClean78PresentationState(
            DateTime generatedUtc,
            CFIPClean78Direction direction,
            CFIPClean78LifecycleState lifecycleState,
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

    public interface ICFIPClean78DecisionEngine
    {
        CFIPClean78DecisionSnapshot Evaluate(
            CFIPClean78RuntimeSnapshot runtime,
            CFIPClean78MtfSnapshot mtf,
            CFIPClean78MarketModel market,
            CFIPClean78ConfigSnapshot configuration);
    }

    public interface ICFIPClean78TradePlanBuilder
    {
        CFIPClean78TradePlan Build(
            CFIPClean78DecisionSnapshot decision,
            CFIPClean78MarketModel market,
            CFIPClean78RuntimeSnapshot runtime,
            CFIPClean78ConfigSnapshot configuration);
    }

    public interface ICFIPClean78ExecutionPlanner
    {
        CFIPClean78ExecutionIntent CreateIntent(
            CFIPClean78TradePlan plan,
            CFIPClean78RuntimeSnapshot runtime,
            CFIPClean78ConfigSnapshot configuration);
    }

    public interface ICFIPClean78BrokerGateway
    {
        CFIPClean78ExecutionResult Execute(
            CFIPClean78ExecutionIntent intent);

        CFIPClean78ExecutionResult ModifyProtection(
            string brokerPositionId,
            double? stopLoss,
            double? takeProfit);

        CFIPClean78ExecutionResult ClosePosition(
            string brokerPositionId);

        CFIPClean78ExecutionResult CancelPendingOrder(
            string brokerOrderId);
    }

    public interface ICFIPClean78BrokerStateReader
    {
        CFIPClean78BrokerStateSnapshot ReadManagedState(
            string symbol,
            string strategyId);
    }

    public interface ICFIPClean78OutcomeRecorder
    {
        void Record(
            CFIPClean78OutcomeEvent outcome);
    }

    public interface ICFIPClean78PresentationProjector
    {
        CFIPClean78PresentationState Project(
            CFIPClean78RuntimeSnapshot runtime,
            CFIPClean78DecisionSnapshot decision,
            CFIPClean78TradePlan plan,
            CFIPClean78BrokerStateSnapshot broker,
            CFIPClean78LifecycleState lifecycle);
    }

    // ------------------------------------------------------------------------
    // Phase-4 engine shell
    // ------------------------------------------------------------------------

    internal sealed class CFIPClean78EngineState
    {
        public CFIPClean78RuntimeSnapshot Runtime { get; private set; }
        public CFIPClean78MtfSnapshot Mtf { get; private set; }
        public CFIPClean78MarketModel Market { get; private set; }
        public CFIPClean78StructureSnapshot Structure { get; private set; }
        public CFIPClean78DecisionSnapshot Decision { get; private set; }
        public CFIPClean78TradePlan Plan { get; private set; }
        public CFIPClean78ExecutionIntent Intent { get; private set; }
        public CFIPClean78ExecutionResult Execution { get; private set; }
        public CFIPClean78BrokerStateSnapshot Broker { get; private set; }
        public CFIPClean78PresentationState Presentation { get; private set; }

        public void SetRuntime(CFIPClean78RuntimeSnapshot value)
        {
            Runtime = value;
        }

        public void SetMtf(CFIPClean78MtfSnapshot value)
        {
            Mtf = value;
        }

        public void SetMarket(CFIPClean78MarketModel value)
        {
            Market = value;
        }

        public void SetStructure(CFIPClean78StructureSnapshot value)
        {
            Structure = value;
        }

        // The cycle state is a downstream data carrier. Later phases extend
        // this with immutable cycle snapshots and explicit orchestration.
        public void ResetCycleOutputs()
        {
            Mtf = null;
            Market = null;
            Structure = null;
            Decision = null;
            Plan = null;
            Intent = null;
            Execution = null;
            Broker = null;
            Presentation = null;
        }
    }

    public enum CFIPClean78PanelCorner
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }

    public enum CFIPClean78SizingMode
    {
        RiskPercentEquity = 0,
        FixedLots = 1
    }

    public enum CFIPClean78PendingOrderMode
    {
        Adaptive = 0,
        ContinuationStop = 1,
        ReversalLimit = 2,
        Both = 3
    }

    // ------------------------------------------------------------------------
    // cTrader host adapter — no broker mutation and no UI authority in Phase 1.
    // ------------------------------------------------------------------------

    [Indicator(
        IsOverlay = true,
        TimeZone = TimeZones.UTC,
        AccessRights = AccessRights.None)]
    public class CFIP_MTF_LiveEntryEngine_Clean_v78 : Indicator
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
        // migrate to CFIPClean78ConfigSnapshot in subsequent phases.
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

[Parameter("Pending Order Mode", Group = "13 · AUTO TRADING", DefaultValue = CFIPClean78PendingOrderMode.Adaptive)]
        public CFIPClean78PendingOrderMode PendingOrderMode { get; set; }

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

[Parameter("Sizing Mode", Group = "13 · AUTO TRADING", DefaultValue = CFIPClean78SizingMode.RiskPercentEquity)]
        public CFIPClean78SizingMode SizingMode { get; set; }

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

[Parameter("Auto TP Stage", Group = "13 · AUTO TRADING", DefaultValue = CFIPClean78TargetStage.TP1)]
        public CFIPClean78TargetStage AutoTpStage { get; set; }

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

[Parameter("Panel Position", Group = "14 · DISPLAY — CORE", DefaultValue = CFIPClean78PanelCorner.BottomLeft)]
        public CFIPClean78PanelCorner PanelPosition { get; set; }

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

[Parameter("Popup Position", Group = "12 · ALERTS — ADVANCED", DefaultValue = CFIPClean78PanelCorner.TopRight)]
        public CFIPClean78PanelCorner PopupPosition { get; set; }

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

[Parameter("Aggressive TP Stage", Group = "13 · AUTO TRADING", DefaultValue = CFIPClean78TargetStage.TP1)]
        public CFIPClean78TargetStage AggressiveTpStage { get; set; }

[Parameter("Aggressive Require Smart Agreement", Group = "13 · AUTO TRADING", DefaultValue = true)]
        public bool AggressiveRequireSmartAgreement { get; set; }

        #endregion

        private CFIPClean78ConfigSnapshot _configuration;
        private CFIPClean78EngineState _state;
        private CFIPClean78LifecycleManager _lifecycle;
        private CFIPClean78RuntimeAuthority _runtimeAuthority;
        private CFIPClean78MarketModelBuilder _marketModelBuilder;
        private CFIPClean78StructureLedgerBuilder _structureBuilder;
        private DateTime _lastMarketReferenceUtc = DateTime.MinValue;

        public CFIPClean78ConfigSnapshot Configuration
        {
            get { return _configuration; }
        }

        public CFIPClean78RuntimeAuthority RuntimeAuthority
        {
            get { return _runtimeAuthority; }
        }

        public CFIPClean78LifecycleState LifecycleState
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
                CFIPClean78ConfigSnapshot.Build(
                    this,
                    "CFIP-PRO-v78");

            _runtimeAuthority =
                new CFIPClean78RuntimeAuthority();

            _runtimeAuthority.InitializeFromConfiguration(
                _configuration);

            _marketModelBuilder =
                new CFIPClean78MarketModelBuilder(
                    Indicators);

            _structureBuilder =
                new CFIPClean78StructureLedgerBuilder();

            _state = new CFIPClean78EngineState();
            _lifecycle = new CFIPClean78LifecycleManager();
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
                new CFIPClean78RuntimeSnapshot(
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
                    new CFIPClean78BrokerConstraints(
                        0,
                        0,
                        0,
                        0),
                    0,
                    0));

            CFIPClean78MtfSnapshot mtf =
                CFIPClean78MtfSnapshotBuilder.Build(
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
                CFIPClean78MarketModel market =
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

                CFIPClean78StructureSnapshot structure =
                    _structureBuilder.Build(
                        mtf,
                        market,
                        _configuration,
                        _m5Bars,
                        _m15Bars,
                        _m30Bars,
                        _h1Bars,
                        _h4Bars,
                        _d1Bars,
                        _w1Bars);

                _state.SetStructure(structure);

                _lastMarketReferenceUtc =
                    mtf.ReferenceUtc;
            }
            else if (!mtf.IsReferenceValid)
            {
                _state.SetMarket(null);
                _state.SetStructure(null);
                _lastMarketReferenceUtc =
                    DateTime.MinValue;
            }
        }

    }
}
