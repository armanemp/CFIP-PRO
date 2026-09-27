// ============================================================================
// CFIP-PRO cTrader — v83 Clean Architecture Foundation
// Phase: 9 · Unified Execution Policy / Broker Gateway
//
// v83 carries the clean Phase-7/8 contracts forward and adds the unified
// pre-broker execution authority. It does NOT copy the v73 monolith.
// v73 remains the frozen behavioral/reference baseline.
    // v83 parent/reference: v79 Phase-6 Decision engine.
// v83 extends the type/ownership boundaries through the normalized execution
// pipeline while preserving the Phase-6 Decision and Phase-7 Entry authorities.
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

    public enum CFIPClean83Direction
    {
        Sell = -1,
        Wait = 0,
        Buy = 1
    }

    public static class CFIPClean83DirectionRules
    {
        public static bool IsDirectional(CFIPClean83Direction direction)
        {
            return direction != CFIPClean83Direction.Wait;
        }

        public static CFIPClean83Direction Opposite(CFIPClean83Direction direction)
        {
            if (direction == CFIPClean83Direction.Buy)
                return CFIPClean83Direction.Sell;

            if (direction == CFIPClean83Direction.Sell)
                return CFIPClean83Direction.Buy;

            return CFIPClean83Direction.Wait;
        }

        public static bool IsProtectiveMove(
            CFIPClean83Direction direction,
            double currentStop,
            double candidateStop)
        {
            if (!IsDirectional(direction) ||
                currentStop <= 0 ||
                candidateStop <= 0)
                return false;

            return direction == CFIPClean83Direction.Buy
                ? candidateStop > currentStop
                : candidateStop < currentStop;
        }
    }

    public enum CFIPClean83DecisionPolicyMode
    {
        Confirmed = 0,
        Soft = 1,
        Aggressive = 2,
        Pending = 3
    }

    public enum CFIPClean83ExecutionKind
    {
        None = 0,
        Market = 1,
        Stop = 2,
        Limit = 3
    }

    public enum CFIPClean83EntryMode
    {
        None = 0,
        RetestMarket = 1,
        BreakoutMarket = 2,
        ContinuationStop = 3,
        ReversalLimit = 4
    }

    public enum CFIPClean83LifecycleState
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

    public enum CFIPClean83BlockReason
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

    public enum CFIPClean83FallbackKind
    {
        None = 0,
        Structural = 1,
        HigherTimeframe = 2,
        ExecutionFrame = 3,
        Atr = 4,
        Synthetic = 5
    }

    public enum CFIPClean83TargetStage
    {
        TP1 = 1,
        TP2 = 2,
        TP3 = 3,
        TP4 = 4
    }

    public enum CFIPClean83ProtectionState
    {
        Unknown = 0,
        Unprotected = 1,
        PartiallyProtected = 2,
        FullyProtected = 3,
        RecoveryRequired = 4
    }

    public enum CFIPClean83BrokerObjectKind
    {
        None = 0,
        Position = 1,
        PendingOrder = 2
    }

    public enum CFIPClean83TargetState
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

    public sealed class CFIPClean83Provenance
    {
        public string Source { get; private set; }
        public string Rule { get; private set; }
        public CFIPClean83FallbackKind Fallback { get; private set; }
        public string Detail { get; private set; }

        public CFIPClean83Provenance(
            string source,
            string rule,
            CFIPClean83FallbackKind fallback,
            string detail)
        {
            Source = source ?? string.Empty;
            Rule = rule ?? string.Empty;
            Fallback = fallback;
            Detail = detail ?? string.Empty;
        }

        public static CFIPClean83Provenance Direct(
            string source,
            string rule)
        {
            return new CFIPClean83Provenance(
                source,
                rule,
                CFIPClean83FallbackKind.None,
                string.Empty);
        }

        public static CFIPClean83Provenance FallbackFrom(
            string source,
            string rule,
            CFIPClean83FallbackKind fallback,
            string detail)
        {
            return new CFIPClean83Provenance(
                source,
                rule,
                fallback,
                detail);
        }
    }

    // ------------------------------------------------------------------------
    // Price / zone primitives
    // ------------------------------------------------------------------------

    public sealed class CFIPClean83PriceLevel
    {
        public double Price { get; private set; }
        public string Name { get; private set; }
        public CFIPClean83Provenance Provenance { get; private set; }

        public CFIPClean83PriceLevel(
            double price,
            string name,
            CFIPClean83Provenance provenance)
        {
            if (price <= 0)
                throw new ArgumentOutOfRangeException("price");

            Price = price;
            Name = name ?? string.Empty;
            Provenance = provenance ??
                         CFIPClean83Provenance.Direct(
                             "UNKNOWN",
                             "UNSPECIFIED");
        }
    }

    public sealed class CFIPClean83PriceZone
    {
        public double Lower { get; private set; }
        public double Upper { get; private set; }
        public string Name { get; private set; }
        public CFIPClean83Provenance Provenance { get; private set; }

        public CFIPClean83PriceZone(
            double lower,
            double upper,
            string name,
            CFIPClean83Provenance provenance)
        {
            if (lower <= 0 || upper <= 0 || upper < lower)
                throw new ArgumentException("Invalid price zone.");

            Lower = lower;
            Upper = upper;
            Name = name ?? string.Empty;
            Provenance = provenance ??
                         CFIPClean83Provenance.Direct(
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

    public sealed class CFIPClean83EntryModel
    {
        public CFIPClean83Direction Direction { get; private set; }

        // Preferred price inside the structural execution area.
        public CFIPClean83PriceLevel IdealEntry { get; private set; }

        // Allowed structural retest region.
        public CFIPClean83PriceZone EntryZone { get; private set; }

        // Structural activation threshold for breakout/continuation.
        public CFIPClean83PriceLevel Trigger { get; private set; }

        // Exact strategy/broker protection boundary.
        public CFIPClean83PriceLevel Invalidation { get; private set; }

        public CFIPClean83EntryModel(
            CFIPClean83Direction direction,
            CFIPClean83PriceLevel idealEntry,
            CFIPClean83PriceZone entryZone,
            CFIPClean83PriceLevel trigger,
            CFIPClean83PriceLevel invalidation)
        {
            if (!CFIPClean83DirectionRules.IsDirectional(direction))
                throw new ArgumentException("A directional entry model is required.");

            if (idealEntry == null)
                throw new ArgumentNullException("idealEntry");

            if (entryZone == null)
                throw new ArgumentNullException("entryZone");

            if (invalidation == null)
                throw new ArgumentNullException("invalidation");

            // Retest entries do not require an activation threshold.
            // Breakout/continuation entries must provide Trigger.
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

    public sealed class CFIPClean83TargetLevel
    {
        public CFIPClean83TargetStage Stage { get; private set; }
        public CFIPClean83PriceLevel Level { get; private set; }
        public CFIPClean83TargetState State { get; private set; }
        public int Quality { get; private set; }

        public CFIPClean83TargetLevel(
            CFIPClean83TargetStage stage,
            CFIPClean83PriceLevel level,
            int quality,
            CFIPClean83TargetState state)
        {
            Stage = stage;
            Level = level;
            Quality = Math.Max(0, Math.Min(100, quality));
            State = state;
        }
    }

    public sealed class CFIPClean83TargetLadder
    {
        private readonly ReadOnlyCollection<CFIPClean83TargetLevel> _levels;

        public IReadOnlyList<CFIPClean83TargetLevel> Levels
        {
            get { return _levels; }
        }

        public CFIPClean83TargetLadder(
            IList<CFIPClean83TargetLevel> levels)
        {
            if (levels == null)
                throw new ArgumentNullException("levels");

            var copy =
                new List<CFIPClean83TargetLevel>(
                    levels);

            copy.Sort(
                delegate (
                    CFIPClean83TargetLevel left,
                    CFIPClean83TargetLevel right)
                {
                    return left.Stage.CompareTo(right.Stage);
                });

            _levels =
                new ReadOnlyCollection<CFIPClean83TargetLevel>(
                    copy);
        }

        public bool ValidateForDirection(
            CFIPClean83Direction direction)
        {
            if (!CFIPClean83DirectionRules.IsDirectional(direction) ||
                _levels.Count == 0)
                return false;

            for (int i = 1; i < _levels.Count; i++)
            {
                double previous =
                    _levels[i - 1].Level.Price;

                double current =
                    _levels[i].Level.Price;

                if (direction == CFIPClean83Direction.Buy &&
                    current <= previous)
                    return false;

                if (direction == CFIPClean83Direction.Sell &&
                    current >= previous)
                    return false;
            }

            return true;
        }

        public CFIPClean83TargetLevel Find(
            CFIPClean83TargetStage stage)
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

    public sealed class CFIPClean83BrokerConstraints
    {
        public double MinVolumeInUnits { get; private set; }
        public double VolumeStepInUnits { get; private set; }
        public double MinStopDistancePips { get; private set; }
        public double MinTakeProfitDistancePips { get; private set; }

        public CFIPClean83BrokerConstraints(
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

    public sealed class CFIPClean83RuntimeSnapshot
    {
        public DateTime ServerUtc { get; private set; }
        public string Symbol { get; private set; }
        public double Bid { get; private set; }
        public double Ask { get; private set; }
        public double PipSize { get; private set; }
        public double SpreadPips { get; private set; }
        public bool SymbolTradingEnabled { get; private set; }
        public double Equity { get; private set; }
        public double FreeMargin { get; private set; }
        public double Balance { get; private set; }
        public double Margin { get; private set; }
        public double MarginLevel { get; private set; }
        public double DailyRealizedNetProfit { get; private set; }
        public DateTime TradingDayStartUtc { get; private set; }
        public CFIPClean83BrokerConstraints BrokerConstraints { get; private set; }
        public int ManagedPositionCount { get; private set; }
        public int ManagedPendingOrderCount { get; private set; }

        public CFIPClean83RuntimeSnapshot(
            DateTime serverUtc,
            string symbol,
            double bid,
            double ask,
            double pipSize,
            double spreadPips,
            bool symbolTradingEnabled,
            double equity,
            double freeMargin,
            double balance,
            double margin,
            double marginLevel,
            double dailyRealizedNetProfit,
            DateTime tradingDayStartUtc,
            CFIPClean83BrokerConstraints brokerConstraints,
            int managedPositionCount,
            int managedPendingOrderCount)
        {
            if (bid < 0 || ask < 0)
                throw new ArgumentOutOfRangeException("bid");

            ServerUtc = serverUtc;
            Symbol = symbol ?? string.Empty;
            Bid = bid;
            Ask = ask;
            PipSize = Math.Max(0, pipSize);
            SpreadPips = Math.Max(0, spreadPips);
            SymbolTradingEnabled = symbolTradingEnabled;
            Equity = Math.Max(0, equity);
            FreeMargin = Math.Max(0, freeMargin);
            Balance = Math.Max(0, balance);
            Margin = Math.Max(0, margin);
            MarginLevel = Math.Max(0, marginLevel);
            DailyRealizedNetProfit = dailyRealizedNetProfit;
            TradingDayStartUtc = tradingDayStartUtc;
            BrokerConstraints =
                brokerConstraints ??
                throw new ArgumentNullException("brokerConstraints");
            ManagedPositionCount =
                Math.Max(0, managedPositionCount);
            ManagedPendingOrderCount =
                Math.Max(0, managedPendingOrderCount);
        }
    }

    public enum CFIPClean83MtfDataStatus
    {
        Ready = 0,
        PrimaryHistoryInsufficient = 1,
        MissingTimeframeData = 2,
        InvalidReference = 3,
        StaleReference = 4
    }

    public sealed class CFIPClean83MtfBarSnapshot
    {
        public string Timeframe { get; private set; }
        public int ClosedIndex { get; private set; }
        public DateTime BarOpenUtc { get; private set; }
        public DateTime NextBarOpenUtc { get; private set; }
        public bool IsAvailable { get; private set; }
        public bool IsFullyClosedAtReference { get; private set; }
        public bool HasMinimumHistory { get; private set; }

        public CFIPClean83MtfBarSnapshot(
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

        public static CFIPClean83MtfBarSnapshot Missing(
            string timeframe)
        {
            return new CFIPClean83MtfBarSnapshot(
                timeframe,
                -1,
                DateTime.MinValue,
                DateTime.MinValue,
                false,
                false,
                false);
        }
    }

    public sealed class CFIPClean83MtfSnapshot
    {
        public DateTime ServerUtc { get; private set; }
        public DateTime ReferenceUtc { get; private set; }
        public DateTime UserLocalTime { get; private set; }
        public TimeSpan ReferenceAge { get; private set; }
        public string ChartTimeframe { get; private set; }

        public CFIPClean83MtfBarSnapshot Chart { get; private set; }
        public CFIPClean83MtfBarSnapshot M1 { get; private set; }
        public CFIPClean83MtfBarSnapshot M5 { get; private set; }
        public CFIPClean83MtfBarSnapshot M15 { get; private set; }
        public CFIPClean83MtfBarSnapshot M30 { get; private set; }
        public CFIPClean83MtfBarSnapshot H1 { get; private set; }
        public CFIPClean83MtfBarSnapshot H4 { get; private set; }
        public CFIPClean83MtfBarSnapshot D1 { get; private set; }
        public CFIPClean83MtfBarSnapshot W1 { get; private set; }

        public CFIPClean83MtfDataStatus DataStatus { get; private set; }

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

        public CFIPClean83MtfSnapshot(
            DateTime serverUtc,
            DateTime referenceUtc,
            DateTime userLocalTime,
            TimeSpan referenceAge,
            string chartTimeframe,
            CFIPClean83MtfBarSnapshot chart,
            CFIPClean83MtfBarSnapshot m1,
            CFIPClean83MtfBarSnapshot m5,
            CFIPClean83MtfBarSnapshot m15,
            CFIPClean83MtfBarSnapshot m30,
            CFIPClean83MtfBarSnapshot h1,
            CFIPClean83MtfBarSnapshot h4,
            CFIPClean83MtfBarSnapshot d1,
            CFIPClean83MtfBarSnapshot w1,
            CFIPClean83MtfDataStatus dataStatus)
        {
            ServerUtc = serverUtc;
            ReferenceUtc = referenceUtc;
            UserLocalTime = userLocalTime;
            ReferenceAge =
                referenceAge < TimeSpan.Zero
                    ? TimeSpan.Zero
                    : referenceAge;
            ChartTimeframe = chartTimeframe ?? string.Empty;
            Chart = chart ?? CFIPClean83MtfBarSnapshot.Missing("CHART");
            M1 = m1 ?? CFIPClean83MtfBarSnapshot.Missing("M1");
            M5 = m5 ?? CFIPClean83MtfBarSnapshot.Missing("M5");
            M15 = m15 ?? CFIPClean83MtfBarSnapshot.Missing("M15");
            M30 = m30 ?? CFIPClean83MtfBarSnapshot.Missing("M30");
            H1 = h1 ?? CFIPClean83MtfBarSnapshot.Missing("H1");
            H4 = h4 ?? CFIPClean83MtfBarSnapshot.Missing("H4");
            D1 = d1 ?? CFIPClean83MtfBarSnapshot.Missing("D1");
            W1 = w1 ?? CFIPClean83MtfBarSnapshot.Missing("W1");
            DataStatus = dataStatus;
        }
    }

    public static class CFIPClean83MtfSnapshotBuilder
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

        public static CFIPClean83MtfSnapshot Build(
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
                return new CFIPClean83MtfSnapshot(
                    serverUtc,
                    DateTime.MinValue,
                    userLocalTime,
                    TimeSpan.Zero,
                    chartTimeframe,
                    CFIPClean83MtfBarSnapshot.Missing(
                        "CHART"),
                    CFIPClean83MtfBarSnapshot.Missing("M1"),
                    CFIPClean83MtfBarSnapshot.Missing("M5"),
                    CFIPClean83MtfBarSnapshot.Missing("M15"),
                    CFIPClean83MtfBarSnapshot.Missing("M30"),
                    CFIPClean83MtfBarSnapshot.Missing("H1"),
                    CFIPClean83MtfBarSnapshot.Missing("H4"),
                    CFIPClean83MtfBarSnapshot.Missing("D1"),
                    CFIPClean83MtfBarSnapshot.Missing("W1"),
                    CFIPClean83MtfDataStatus.InvalidReference);
            }

            TimeSpan age =
                serverUtc >= reference
                    ? serverUtc - reference
                    : TimeSpan.Zero;

            CFIPClean83MtfBarSnapshot chart =
                ResolveClosedBar(
                    chartBars,
                    reference,
                    chartTimeframe,
                    minimumHistory);

            CFIPClean83MtfBarSnapshot m1 =
                ResolveClosedBar(
                    m1Bars,
                    reference,
                    "M1",
                    minimumHistory);

            CFIPClean83MtfBarSnapshot m5 =
                ResolveClosedBar(
                    m5Bars,
                    reference,
                    "M5",
                    minimumHistory);

            CFIPClean83MtfBarSnapshot m15 =
                ResolveClosedBar(
                    m15Bars,
                    reference,
                    "M15",
                    minimumHistory);

            CFIPClean83MtfBarSnapshot m30 =
                ResolveClosedBar(
                    m30Bars,
                    reference,
                    "M30",
                    minimumHistory);

            CFIPClean83MtfBarSnapshot h1 =
                ResolveClosedBar(
                    h1Bars,
                    reference,
                    "H1",
                    minimumHistory);

            CFIPClean83MtfBarSnapshot h4 =
                ResolveClosedBar(
                    h4Bars,
                    reference,
                    "H4",
                    minimumHistory);

            CFIPClean83MtfBarSnapshot d1 =
                ResolveClosedBar(
                    d1Bars,
                    reference,
                    "D1",
                    minimumHistory);

            CFIPClean83MtfBarSnapshot w1 =
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

            CFIPClean83MtfDataStatus status;

            if (age > TimeSpan.FromMinutes(10))
                status =
                    CFIPClean83MtfDataStatus.StaleReference;
            else if (!primaryHistory)
                status =
                    CFIPClean83MtfDataStatus.PrimaryHistoryInsufficient;
            else if (!m1.IsAvailable ||
                     !m5.IsAvailable ||
                     !m15.IsAvailable ||
                     !m30.IsAvailable ||
                     !h1.IsAvailable ||
                     !h4.IsAvailable)
                status =
                    CFIPClean83MtfDataStatus.MissingTimeframeData;
            else
                status =
                    CFIPClean83MtfDataStatus.Ready;

            return new CFIPClean83MtfSnapshot(
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
            CFIPClean83MtfBarSnapshot m5,
            CFIPClean83MtfBarSnapshot m15,
            CFIPClean83MtfBarSnapshot m30,
            CFIPClean83MtfBarSnapshot h1,
            CFIPClean83MtfBarSnapshot h4,
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

        public static CFIPClean83MtfBarSnapshot ResolveClosedBar(
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
                    CFIPClean83MtfBarSnapshot.Missing(
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
                    new CFIPClean83MtfBarSnapshot(
                        timeframe,
                        i,
                        open,
                        nextOpen,
                        true,
                        true,
                        minimum);
            }

            return
                CFIPClean83MtfBarSnapshot.Missing(
                    timeframe);
        }

        public static bool IsCoherent(
            CFIPClean83MtfSnapshot snapshot)
        {
            if (snapshot == null ||
                !snapshot.IsReferenceValid ||
                !snapshot.IsReferenceFresh)
                return false;

            CFIPClean83MtfBarSnapshot[] items =
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
                CFIPClean83MtfBarSnapshot item =
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

    public enum CFIPClean83Regime
    {
        Unknown = 0,
        Trend = 1,
        Expansion = 2,
        Compression = 3,
        Range = 4,
        Transition = 5
    }

    public enum CFIPClean83MarketFeature
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

    public sealed class CFIPClean83FeatureEvidence
    {
        public CFIPClean83MarketFeature Feature { get; private set; }
        public CFIPClean83Direction Direction { get; private set; }
        public double Value { get; private set; }
        public int Weight { get; private set; }
        public bool Triggered { get; private set; }
        public bool CountsAsEvidence { get; private set; }
        public bool CountsAsGate { get; private set; }
        public CFIPClean83Provenance Provenance { get; private set; }

        public CFIPClean83FeatureEvidence(
            CFIPClean83MarketFeature feature,
            CFIPClean83Direction direction,
            double value,
            int weight,
            bool triggered,
            bool countsAsEvidence,
            bool countsAsGate,
            CFIPClean83Provenance provenance)
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
                CFIPClean83Provenance.Direct(
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

    public sealed class CFIPClean83MarketFrame
    {
        private readonly ReadOnlyCollection<CFIPClean83FeatureEvidence> _features;

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
        public CFIPClean83Direction BiasDirection { get; private set; }
        public int BiasStrength { get; private set; }
        public int MarketQuality { get; private set; }
        public int BullScore { get; private set; }
        public int BearScore { get; private set; }
        public int BullScoreNormalized { get; private set; }
        public int BearScoreNormalized { get; private set; }
        public int IndependentEvidence { get; private set; }
        public int EnabledEvidenceFeatures { get; private set; }

        public CFIPClean83Regime Regime { get; private set; }
        public int RegimeQuality { get; private set; }
        public bool Choppy { get; private set; }
        public bool HealthyVolatility { get; private set; }
        public bool DataValid { get; private set; }

        public IReadOnlyList<CFIPClean83FeatureEvidence> Features
        {
            get { return _features; }
        }

        public int DirectionScore
        {
            get
            {
                return
                    BiasDirection == CFIPClean83Direction.Buy
                        ? BiasStrength
                        : BiasDirection == CFIPClean83Direction.Sell
                            ? -BiasStrength
                            : 0;
            }
        }

        public CFIPClean83MarketFrame(
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
            CFIPClean83Direction biasDirection,
            int biasStrength,
            int marketQuality,
            int bullScore,
            int bearScore,
            int bullScoreNormalized,
            int bearScoreNormalized,
            int independentEvidence,
            int enabledEvidenceFeatures,
            CFIPClean83Regime regime,
            int regimeQuality,
            bool choppy,
            bool healthyVolatility,
            bool dataValid,
            IList<CFIPClean83FeatureEvidence> features)
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
                new ReadOnlyCollection<CFIPClean83FeatureEvidence>(
                    new List<CFIPClean83FeatureEvidence>(
                        features ??
                        new List<CFIPClean83FeatureEvidence>()));
        }
    }

    public sealed class CFIPClean83MarketModel
    {
        private readonly ReadOnlyCollection<CFIPClean83MarketFrame> _frames;

        public DateTime ReferenceUtc { get; private set; }
        public bool IsCoherent { get; private set; }
        public bool IsPrimaryReady { get; private set; }
        public string DataStatus { get; private set; }

        public IReadOnlyList<CFIPClean83MarketFrame> Frames
        {
            get { return _frames; }
        }

        public CFIPClean83MarketFrame M5
        {
            get { return FindFrame("M5"); }
        }

        public CFIPClean83MarketFrame M15
        {
            get { return FindFrame("M15"); }
        }

        public CFIPClean83MarketFrame FindFrame(string timeframe)
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

        public CFIPClean83MarketModel(
            DateTime referenceUtc,
            bool isCoherent,
            bool isPrimaryReady,
            string dataStatus,
            IList<CFIPClean83MarketFrame> frames)
        {
            ReferenceUtc = referenceUtc;
            IsCoherent = isCoherent;
            IsPrimaryReady = isPrimaryReady;
            DataStatus = dataStatus ?? string.Empty;
            _frames =
                new ReadOnlyCollection<CFIPClean83MarketFrame>(
                    new List<CFIPClean83MarketFrame>(
                        frames ??
                        new List<CFIPClean83MarketFrame>()));
        }
    }

    public sealed class CFIPClean83NativeIndicatorSet
    {
        public Bars Bars { get; private set; }
        public ExponentialMovingAverage Fast { get; private set; }
        public ExponentialMovingAverage Slow { get; private set; }
        public AverageTrueRange Atr { get; private set; }
        public RelativeStrengthIndex Rsi { get; private set; }
        public DirectionalMovementSystem Dms { get; private set; }
        public ExponentialMovingAverage MacdFast { get; private set; }
        public ExponentialMovingAverage MacdSlow { get; private set; }

        public CFIPClean83NativeIndicatorSet(
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

    public sealed class CFIPClean83NativeIndicatorCatalog
    {
        private readonly IIndicatorsAccessor _indicators;
        private readonly List<CFIPClean83NativeIndicatorSet> _sets =
            new List<CFIPClean83NativeIndicatorSet>();

        public CFIPClean83NativeIndicatorCatalog(
            IIndicatorsAccessor indicators)
        {
            _indicators =
                indicators ??
                throw new ArgumentNullException("indicators");
        }

        public CFIPClean83NativeIndicatorSet GetOrCreate(
            Bars bars,
            CFIPClean83ConfigSnapshot configuration)
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
                var set = new CFIPClean83NativeIndicatorSet(
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

    public sealed class CFIPClean83MarketModelBuilder
    {
        private const int MinimumClosedIndex = 30;
        private readonly CFIPClean83NativeIndicatorCatalog _catalog;

        public CFIPClean83MarketModelBuilder(IIndicatorsAccessor indicators)
        {
            _catalog = new CFIPClean83NativeIndicatorCatalog(indicators);
        }

        public CFIPClean83MarketModel Build(
            CFIPClean83RuntimeSnapshot runtime,
            CFIPClean83MtfSnapshot mtf,
            CFIPClean83ConfigSnapshot configuration,
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

            var frames = new List<CFIPClean83MarketFrame>();

            if (!mtf.IsReferenceValid)
                return new CFIPClean83MarketModel(
                    DateTime.MinValue,
                    false,
                    false,
                    mtf.DataStatus.ToString(),
                    frames);

            bool coherent =
                CFIPClean83MtfSnapshotBuilder.IsCoherent(mtf);

            AddFrame(frames, "CHART", chartBars, mtf.Chart, configuration);
            AddFrame(frames, "M1", m1Bars, mtf.M1, configuration);
            AddFrame(frames, "M5", m5Bars, mtf.M5, configuration);
            AddFrame(frames, "M15", m15Bars, mtf.M15, configuration);
            AddFrame(frames, "M30", m30Bars, mtf.M30, configuration);
            AddFrame(frames, "H1", h1Bars, mtf.H1, configuration);
            AddFrame(frames, "H4", h4Bars, mtf.H4, configuration);
            AddFrame(frames, "D1", d1Bars, mtf.D1, configuration);
            AddFrame(frames, "W1", w1Bars, mtf.W1, configuration);

            return new CFIPClean83MarketModel(
                mtf.ReferenceUtc,
                coherent,
                mtf.IsPrimaryDecisionReady,
                mtf.DataStatus.ToString(),
                frames);
        }

        private void AddFrame(
            IList<CFIPClean83MarketFrame> frames,
            string timeframe,
            Bars bars,
            CFIPClean83MtfBarSnapshot snapshot,
            CFIPClean83ConfigSnapshot configuration)
        {
            if (frames == null ||
                snapshot == null ||
                !snapshot.IsAvailable ||
                !snapshot.IsFullyClosedAtReference ||
                snapshot.ClosedIndex < MinimumClosedIndex ||
                bars == null)
                return;

            CFIPClean83MarketFrame frame =
                BuildFrame(
                    timeframe,
                    bars,
                    snapshot,
                    configuration);

            if (frame != null)
                frames.Add(frame);
        }

        private CFIPClean83MarketFrame BuildFrame(
            string timeframe,
            Bars bars,
            CFIPClean83MtfBarSnapshot snapshot,
            CFIPClean83ConfigSnapshot configuration)
        {
            int index = snapshot.ClosedIndex;

            if (index < MinimumClosedIndex ||
                index >= bars.Count - 1)
                return null;

            CFIPClean83NativeIndicatorSet native =
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
                new List<CFIPClean83FeatureEvidence>();

            int bullScore = 0;
            int bearScore = 0;
            int independentEvidence = 0;
            int enabledFeatures = 0;

            AddFeature(features, CFIPClean83MarketFeature.Trend,
                trendBull, trendBear, 10, true, true, true,
                ref bullScore, ref bearScore,
                ref independentEvidence, ref enabledFeatures);

            AddFeature(features, CFIPClean83MarketFeature.Momentum,
                momentumBull, momentumBear, 8, true, true, true,
                ref bullScore, ref bearScore,
                ref independentEvidence, ref enabledFeatures);

            AddFeature(features, CFIPClean83MarketFeature.Rsi,
                rsiBull, rsiBear, 3, true, false, true,
                ref bullScore, ref bearScore,
                ref independentEvidence, ref enabledFeatures);

            AddFeature(features, CFIPClean83MarketFeature.Dmi,
                dmiBull, dmiBear, 4,
                adx >= adxMinimum,
                true, true,
                ref bullScore, ref bearScore,
                ref independentEvidence, ref enabledFeatures);

            AddFeature(features, CFIPClean83MarketFeature.EmaSlope,
                slopeBull, slopeBear, 3,
                useSlope, false, useSlope,
                ref bullScore, ref bearScore,
                ref independentEvidence, ref enabledFeatures);

            AddFeature(features, CFIPClean83MarketFeature.Rejection,
                rejectionBull, rejectionBear, 6, true, false, true,
                ref bullScore, ref bearScore,
                ref independentEvidence, ref enabledFeatures);

            AddFeature(features, CFIPClean83MarketFeature.VolumeExpansion,
                volumeBull, volumeBear, 3,
                volumeEvidence, false, volumeEnabled,
                ref bullScore, ref bearScore,
                ref independentEvidence, ref enabledFeatures);

            AddFeature(features, CFIPClean83MarketFeature.MacdBias,
                macdBull, macdBear, 3,
                macdEvidence, false, macdEnabled,
                ref bullScore, ref bearScore,
                ref independentEvidence, ref enabledFeatures);

            AddFeature(features, CFIPClean83MarketFeature.VwapBias,
                vwapBull, vwapBear, 2,
                vwapEvidence, false, vwapEnabled,
                ref bullScore, ref bearScore,
                ref independentEvidence, ref enabledFeatures);

            AddFeature(features, CFIPClean83MarketFeature.HealthyVolatility,
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

            CFIPClean83Direction bias =
                ResolveBias(
                    bullNormalized,
                    bearNormalized);

            int biasStrength =
                bias == CFIPClean83Direction.Buy
                    ? bullNormalized
                    : bias == CFIPClean83Direction.Sell
                        ? bearNormalized
                        : 0;

            CFIPClean83Regime regime =
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

            return new CFIPClean83MarketFrame(
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
            IList<CFIPClean83FeatureEvidence> features,
            CFIPClean83MarketFeature feature,
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

            CFIPClean83Direction direction =
                bull && !bear
                    ? CFIPClean83Direction.Buy
                    : bear && !bull
                        ? CFIPClean83Direction.Sell
                        : CFIPClean83Direction.Wait;

            features.Add(
                new CFIPClean83FeatureEvidence(
                    feature,
                    direction,
                    direction == CFIPClean83Direction.Wait ? 0 : 1,
                    weight,
                    direction != CFIPClean83Direction.Wait,
                    countsAsEvidence,
                    countsAsGate,
                    CFIPClean83Provenance.Direct(
                        "MARKET_MODEL",
                        feature.ToString())));

            if (direction == CFIPClean83Direction.Buy)
            {
                bullScore += weight;
                if (countsAsEvidence)
                    independentEvidence++;
            }
            else if (direction == CFIPClean83Direction.Sell)
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

        private static CFIPClean83Direction ResolveBias(int bull, int bear)
        {
            if (bull >= 55 && bull >= bear + 12)
                return CFIPClean83Direction.Buy;

            if (bear >= 55 && bear >= bull + 12)
                return CFIPClean83Direction.Sell;

            return CFIPClean83Direction.Wait;
        }

        private static CFIPClean83Regime DetectRegime(
            double atr,
            double previousAtr,
            double adx,
            int adxMinimum,
            double emaSpread)
        {
            if (atr <= 0 || previousAtr <= 0)
                return CFIPClean83Regime.Unknown;

            double ratio = atr / previousAtr;

            if (ratio >= 1.30)
                return CFIPClean83Regime.Expansion;

            if (ratio <= 0.80)
                return CFIPClean83Regime.Compression;

            if (adx < adxMinimum)
                return CFIPClean83Regime.Range;

            if (emaSpread <= atr * 0.10)
                return CFIPClean83Regime.Transition;

            return CFIPClean83Regime.Trend;
        }

        private static int CalculateRegimeQuality(
            CFIPClean83Regime regime,
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
                    CFIPClean83Regime.Trend ||
                regime ==
                    CFIPClean83Regime.Expansion
                    ? 100
                    : regime ==
                        CFIPClean83Regime.Transition
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




    public enum CFIPClean83StructureEventKind
    {
        SwingHigh, SwingLow, BreakOfStructure, MarketStructureShift,
        ChangeOfCharacter, Displacement, LiquiditySweep
    }

    public enum CFIPClean83ZoneKind { FairValueGap, OrderBlock }

    public enum CFIPClean83ZoneLifecycle
    {
        Active, Retested, PartiallyMitigated, Consumed, Invalidated, Expired
    }

    public enum CFIPClean83LiquidityKind
    {
        EqualHigh, EqualLow, SwingHigh, SwingLow,
        PriorDayHigh, PriorDayLow, PriorWeekHigh, PriorWeekLow,
        SessionHigh, SessionLow, DailyPivot, DailyR1, DailyR2, DailyS1, DailyS2,
        Forecast
    }

    public enum CFIPClean83LiquiditySide { Neutral, Above, Below }

    public sealed class CFIPClean83StructureEventRecord
    {
        public string Id { get; private set; }
        public CFIPClean83StructureEventKind Kind { get; private set; }
        public CFIPClean83Direction Direction { get; private set; }
        public string Timeframe { get; private set; }
        public int BarIndex { get; private set; }
        public DateTime TimeUtc { get; private set; }
        public double Price { get; private set; }
        public double StrengthAtr { get; private set; }
        public int Quality { get; private set; }
        public CFIPClean83Provenance Provenance { get; private set; }

        public CFIPClean83StructureEventRecord(
            string id, CFIPClean83StructureEventKind kind,
            CFIPClean83Direction direction, string timeframe,
            int barIndex, DateTime timeUtc, double price,
            double strengthAtr, int quality,
            CFIPClean83Provenance provenance)
        {
            Id = id ?? string.Empty;
            Kind = kind; Direction = direction; Timeframe = timeframe ?? string.Empty;
            BarIndex = barIndex; TimeUtc = timeUtc; Price = price;
            StrengthAtr = Math.Max(0, strengthAtr);
            Quality = Math.Max(0, Math.Min(100, quality));
            Provenance = provenance ?? CFIPClean83Provenance.Direct("STRUCTURE", kind.ToString());
        }
    }

    public sealed class CFIPClean83ZoneRecord
    {
        public string Id { get; private set; }
        public CFIPClean83ZoneKind Kind { get; private set; }
        public CFIPClean83Direction Direction { get; private set; }
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
        public CFIPClean83ZoneLifecycle Lifecycle { get; private set; }
        public CFIPClean83Provenance Provenance { get; private set; }

        public CFIPClean83ZoneRecord(
            string id, CFIPClean83ZoneKind kind,
            CFIPClean83Direction direction, string timeframe,
            int createdIndex, DateTime createdUtc,
            double originalLower, double originalUpper,
            double currentLower, double currentUpper,
            int ageBars, bool retested, bool mitigated,
            bool invalidated, bool consumed, bool executionEligible,
            int quality, double displacementAtr,
            bool liquidityConfluence, bool fvgConfluence,
            CFIPClean83ZoneLifecycle lifecycle,
            CFIPClean83Provenance provenance)
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
            Provenance = provenance ?? CFIPClean83Provenance.Direct("ZONE", kind.ToString());
        }

        public bool Contains(double price)
        {
            return price >= CurrentLower && price <= CurrentUpper;
        }
    }

    public sealed class CFIPClean83LiquidityRecord
    {
        public string Id { get; private set; }
        public CFIPClean83LiquidityKind Kind { get; private set; }
        public CFIPClean83LiquiditySide Side { get; private set; }
        public CFIPClean83Direction SweepDirection { get; private set; }
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
        public CFIPClean83Provenance Provenance { get; private set; }

        public CFIPClean83LiquidityRecord(
            string id, CFIPClean83LiquidityKind kind,
            CFIPClean83LiquiditySide side, CFIPClean83Direction sweepDirection,
            string timeframe, int barIndex, DateTime timeUtc,
            double price, double tolerance, double distance, double penetration,
            bool swept, bool forecastCandidate, int quality,
            CFIPClean83Provenance provenance)
        {
            Id = id ?? string.Empty; Kind = kind; Side = side; SweepDirection = sweepDirection;
            Timeframe = timeframe ?? string.Empty; BarIndex = barIndex; TimeUtc = timeUtc;
            Price = price; Tolerance = Math.Max(0, tolerance); Distance = Math.Max(0, distance);
            Penetration = Math.Max(0, penetration); Swept = swept;
            ForecastCandidate = forecastCandidate;
            Quality = Math.Max(0, Math.Min(100, quality));
            Provenance = provenance ?? CFIPClean83Provenance.Direct("LIQUIDITY", kind.ToString());
        }
    }

    public sealed class CFIPClean83PremiumDiscountState
    {
        public bool Available { get; private set; }
        public double RangeLow { get; private set; }
        public double RangeHigh { get; private set; }
        public double Midpoint { get { return Available ? (RangeLow + RangeHigh) / 2.0 : 0; } }
        public double ValueRatio { get; private set; }
        public bool IsDiscount { get { return Available && ValueRatio < 0.5; } }
        public bool IsPremium { get { return Available && ValueRatio > 0.5; } }

        public CFIPClean83PremiumDiscountState(
            bool available, double rangeLow, double rangeHigh, double valueRatio)
        {
            Available = available; RangeLow = rangeLow; RangeHigh = rangeHigh;
            ValueRatio = Math.Max(0, Math.Min(1, valueRatio));
        }
    }

    public sealed class CFIPClean83StructureSnapshot
    {
        private readonly ReadOnlyCollection<CFIPClean83StructureEventRecord> _events;
        private readonly ReadOnlyCollection<CFIPClean83ZoneRecord> _zones;
        private readonly ReadOnlyCollection<CFIPClean83LiquidityRecord> _liquidity;

        public DateTime ReferenceUtc { get; private set; }
        public bool IsCoherent { get; private set; }
        public bool IsPrimaryReady { get; private set; }
        public CFIPClean83Direction CurrentStructureDirection { get; private set; }
        public int StructureQuality { get; private set; }
        public CFIPClean83PremiumDiscountState PremiumDiscount { get; private set; }
        public IReadOnlyList<CFIPClean83StructureEventRecord> Events { get { return _events; } }
        public IReadOnlyList<CFIPClean83ZoneRecord> Zones { get { return _zones; } }
        public IReadOnlyList<CFIPClean83LiquidityRecord> Liquidity { get { return _liquidity; } }

        public CFIPClean83StructureSnapshot(
            DateTime referenceUtc, bool isCoherent, bool isPrimaryReady,
            CFIPClean83Direction direction, int quality,
            IList<CFIPClean83StructureEventRecord> events,
            IList<CFIPClean83ZoneRecord> zones,
            IList<CFIPClean83LiquidityRecord> liquidity,
            CFIPClean83PremiumDiscountState premiumDiscount)
        {
            ReferenceUtc = referenceUtc; IsCoherent = isCoherent; IsPrimaryReady = isPrimaryReady;
            CurrentStructureDirection = direction;
            StructureQuality = Math.Max(0, Math.Min(100, quality));
            _events = new ReadOnlyCollection<CFIPClean83StructureEventRecord>(
                new List<CFIPClean83StructureEventRecord>(events ?? new List<CFIPClean83StructureEventRecord>()));
            _zones = new ReadOnlyCollection<CFIPClean83ZoneRecord>(
                new List<CFIPClean83ZoneRecord>(zones ?? new List<CFIPClean83ZoneRecord>()));
            _liquidity = new ReadOnlyCollection<CFIPClean83LiquidityRecord>(
                new List<CFIPClean83LiquidityRecord>(liquidity ?? new List<CFIPClean83LiquidityRecord>()));
            PremiumDiscount = premiumDiscount ?? new CFIPClean83PremiumDiscountState(false, 0, 0, 0);
        }

        public bool HasEvent(CFIPClean83StructureEventKind kind, CFIPClean83Direction direction)
        {
            for (int i = 0; i < _events.Count; i++)
                if (_events[i].Kind == kind && _events[i].Direction == direction) return true;
            return false;
        }

        public CFIPClean83ZoneRecord FindNearestZone(
            CFIPClean83Direction direction, CFIPClean83ZoneKind kind,
            double price, bool executionEligibleOnly)
        {
            CFIPClean83ZoneRecord best = null; double bestDistance = double.MaxValue;
            for (int i = 0; i < _zones.Count; i++)
            {
                var z = _zones[i];
                if (z.Direction != direction || z.Kind != kind ||
                    z.Consumed || z.Invalidated || z.Lifecycle == CFIPClean83ZoneLifecycle.Expired)
                    continue;
                if (executionEligibleOnly && !z.ExecutionEligible) continue;

                double distance =
                    price <= z.CurrentLower ? z.CurrentLower - price :
                    price >= z.CurrentUpper ? price - z.CurrentUpper : 0;

                if (distance < bestDistance) { bestDistance = distance; best = z; }
            }
            return best;
        }

        public CFIPClean83LiquidityRecord FindNearestLiquidity(
            CFIPClean83LiquiditySide side, double price, bool unsweptOnly)
        {
            CFIPClean83LiquidityRecord best = null; double bestDistance = double.MaxValue;
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

    public sealed class CFIPClean83StructureLedgerBuilder
    {
        private const int MinimumIndex = 35;

        public CFIPClean83StructureSnapshot Build(
            CFIPClean83MtfSnapshot mtf,
            CFIPClean83MarketModel market,
            CFIPClean83ConfigSnapshot configuration,
            Bars m5Bars, Bars m15Bars, Bars m30Bars, Bars h1Bars, Bars h4Bars,
            Bars d1Bars, Bars w1Bars)
        {
            var events = new List<CFIPClean83StructureEventRecord>();
            var zones = new List<CFIPClean83ZoneRecord>();
            var liquidity = new List<CFIPClean83LiquidityRecord>();

            if (mtf == null || market == null ||
                !mtf.IsPrimaryDecisionReady)
            {
                return new CFIPClean83StructureSnapshot(
                    mtf == null ? DateTime.MinValue : mtf.ReferenceUtc,
                    false, false, CFIPClean83Direction.Wait, 0,
                    events, zones, liquidity,
                    new CFIPClean83PremiumDiscountState(false, 0, 0, 0));
            }

            BuildTimeframe(events, zones, liquidity, "M5", m5Bars, mtf.M5, market.M5, configuration, true);
            BuildTimeframe(events, zones, liquidity, "M15", m15Bars, mtf.M15, market.M15, configuration, false);
            BuildTimeframe(events, zones, liquidity, "M30", m30Bars, mtf.M30, market.FindFrame("M30"), configuration, false);
            BuildTimeframe(events, zones, liquidity, "H1", h1Bars, mtf.H1, market.FindFrame("H1"), configuration, false);
            BuildTimeframe(events, zones, liquidity, "H4", h4Bars, mtf.H4, market.FindFrame("H4"), configuration, false);

            AddPriorDayWeekLiquidity(liquidity, mtf, d1Bars, w1Bars, configuration);
            AddSessionLiquidity(liquidity, mtf, m5Bars, configuration);
            AddDailyPivots(liquidity, mtf, d1Bars, configuration);
            MarkForecastLiquidity(
                liquidity,
                market.M5,
                configuration);
            MarkConfluence(zones, liquidity, configuration);

            var direction = ResolveDirection(events);
            int quality = CalculateQuality(events, zones, liquidity, direction);
            var pd = BuildPremiumDiscount(m5Bars, mtf.M5.ClosedIndex, configuration);

            return new CFIPClean83StructureSnapshot(
                mtf.ReferenceUtc,
                CFIPClean83MtfSnapshotBuilder.IsCoherent(mtf),
                mtf.IsPrimaryDecisionReady,
                direction,
                quality,
                events, zones, liquidity, pd);
        }

        private void BuildTimeframe(
            IList<CFIPClean83StructureEventRecord> events,
            IList<CFIPClean83ZoneRecord> zones,
            IList<CFIPClean83LiquidityRecord> liquidity,
            string timeframe, Bars bars, CFIPClean83MtfBarSnapshot snapshot,
            CFIPClean83MarketFrame frame, CFIPClean83ConfigSnapshot cfg, bool primary)
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
                    AddEvent(events, CFIPClean83StructureEventKind.SwingHigh,
                        CFIPClean83Direction.Sell, timeframe, bars, swingHigh,
                        bars.HighPrices[swingHigh], 0, 68, "confirmed swing high");

                if (swingLow >= 0)
                    AddEvent(events, CFIPClean83StructureEventKind.SwingLow,
                        CFIPClean83Direction.Buy, timeframe, bars, swingLow,
                        bars.LowPrices[swingLow], 0, 68, "confirmed swing low");

                if (swingHigh >= 0 &&
                    bars.ClosePrices[index] > bars.HighPrices[swingHigh] + atr * breakAtr)
                {
                    double strengthAtr =
                        (bars.ClosePrices[index] - bars.HighPrices[swingHigh]) / atr;
                    AddEvent(events, CFIPClean83StructureEventKind.BreakOfStructure,
                        CFIPClean83Direction.Buy, timeframe, bars, index,
                        bars.ClosePrices[index], strengthAtr,
                        Clamp(70 + (int)Math.Round(Math.Min(20, strengthAtr * 8))),
                        "close beyond latest swing high");

                    if (IsBearishPreBreak(bars, index, swingHigh))
                        AddEvent(events, CFIPClean83StructureEventKind.MarketStructureShift,
                            CFIPClean83Direction.Buy, timeframe, bars, index,
                            bars.ClosePrices[index], strengthAtr, 82,
                            "bullish break after bearish sequence");

                    if (IsCharacterBreakBull(bars, index, strength, lookback, atr, breakAtr))
                        AddEvent(events, CFIPClean83StructureEventKind.ChangeOfCharacter,
                            CFIPClean83Direction.Buy, timeframe, bars, index,
                            bars.ClosePrices[index], strengthAtr, 78,
                            "bullish change of character");
                }

                if (swingLow >= 0 &&
                    bars.ClosePrices[index] < bars.LowPrices[swingLow] - atr * breakAtr)
                {
                    double strengthAtr =
                        (bars.LowPrices[swingLow] - bars.ClosePrices[index]) / atr;
                    AddEvent(events, CFIPClean83StructureEventKind.BreakOfStructure,
                        CFIPClean83Direction.Sell, timeframe, bars, index,
                        bars.ClosePrices[index], strengthAtr,
                        Clamp(70 + (int)Math.Round(Math.Min(20, strengthAtr * 8))),
                        "close beyond latest swing low");

                    if (IsBullishPreBreak(bars, index, swingLow))
                        AddEvent(events, CFIPClean83StructureEventKind.MarketStructureShift,
                            CFIPClean83Direction.Sell, timeframe, bars, index,
                            bars.ClosePrices[index], strengthAtr, 82,
                            "bearish break after bullish sequence");

                    if (IsCharacterBreakBear(bars, index, strength, lookback, atr, breakAtr))
                        AddEvent(events, CFIPClean83StructureEventKind.ChangeOfCharacter,
                            CFIPClean83Direction.Sell, timeframe, bars, index,
                            bars.ClosePrices[index], strengthAtr, 78,
                            "bearish change of character");
                }
            }

            if (cfg.Get("UseDisplacement", true))
            {
                double bodyAtr =
                    Math.Abs(bars.ClosePrices[index] - bars.OpenPrices[index]) / atr;
                double threshold = Math.Max(0, cfg.Get("DisplacementAtr", 0.80));
                CFIPClean83Direction d =
                    bars.ClosePrices[index] > bars.OpenPrices[index]
                        ? CFIPClean83Direction.Buy
                        : bars.ClosePrices[index] < bars.OpenPrices[index]
                            ? CFIPClean83Direction.Sell
                            : CFIPClean83Direction.Wait;

                if (d != CFIPClean83Direction.Wait && bodyAtr >= threshold)
                    AddEvent(events, CFIPClean83StructureEventKind.Displacement,
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
            IList<CFIPClean83ZoneRecord> zones, Bars bars, int index,
            string timeframe, double atr, CFIPClean83ConfigSnapshot cfg)
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
                        bars, i, index, timeframe, CFIPClean83Direction.Buy,
                        bars.HighPrices[i - 2], bars.LowPrices[i],
                        bullGap, atr, maxAge, cfg, "three-candle imbalance FVG"));
                    count++;
                }

                double bearGap = bars.LowPrices[i - 2] - bars.HighPrices[i];
                if (bearGap >= atr * minGap && count < 24)
                {
                    zones.Add(BuildFvg(
                        bars, i, index, timeframe, CFIPClean83Direction.Sell,
                        bars.HighPrices[i], bars.LowPrices[i - 2],
                        bearGap, atr, maxAge, cfg, "three-candle imbalance FVG"));
                    count++;
                }

                if (cfg.Get("UseTwoBarImbalanceFvg", false) &&
                    count < 24 &&
                    i >= 1)
                {
                    double twoBarBull =
                        bars.LowPrices[i] -
                        bars.HighPrices[i - 1];

                    if (twoBarBull >= atr * minGap)
                    {
                        zones.Add(BuildFvg(
                            bars, i, index, timeframe, CFIPClean83Direction.Buy,
                            bars.HighPrices[i - 1], bars.LowPrices[i],
                            twoBarBull, atr, maxAge, cfg,
                            "two-bar imbalance FVG"));
                        count++;
                    }

                    double twoBarBear =
                        bars.LowPrices[i - 1] -
                        bars.HighPrices[i];

                    if (twoBarBear >= atr * minGap && count < 24)
                    {
                        zones.Add(BuildFvg(
                            bars, i, index, timeframe, CFIPClean83Direction.Sell,
                            bars.HighPrices[i], bars.LowPrices[i - 1],
                            twoBarBear, atr, maxAge, cfg,
                            "two-bar imbalance FVG"));
                        count++;
                    }
                }
            }
        }

        private CFIPClean83ZoneRecord BuildFvg(
            Bars bars, int created, int current, string timeframe,
            CFIPClean83Direction direction, double lower, double upper,
            double gap, double atr, int maxAge,
            CFIPClean83ConfigSnapshot cfg,
            string rule)
        {
            bool retested = false, partial = false, consumed = false;
            bool partialEnabled =
                cfg.Get(
                    "EnableFvgPartialMitigation",
                    true);
            double currentLower = lower, currentUpper = upper;
            bool breakByWicks = cfg.Get("FvgBreakByWicks", true);

            for (int i = created + 1; i <= current; i++)
            {
                bool overlap =
                    bars.HighPrices[i] >= lower &&
                    bars.LowPrices[i] <= upper;

                if (!overlap) continue;
                retested = true;

                if (direction == CFIPClean83Direction.Buy)
                {
                    if (partialEnabled &&
                        bars.LowPrices[i] > lower)
                    {
                        partial = true;
                        currentLower =
                            Math.Max(
                                lower,
                                Math.Min(
                                    upper,
                                    bars.LowPrices[i]));
                    }

                    if ((breakByWicks && bars.LowPrices[i] <= lower) ||
                        (!breakByWicks && bars.ClosePrices[i] <= lower))
                    {
                        // Full consumption is a safety invariant; it cannot
                        // be disabled by a presentation/configuration toggle.
                        consumed = true;
                        break;
                    }
                }
                else
                {
                    if (partialEnabled &&
                        bars.HighPrices[i] < upper)
                    {
                        partial = true;
                        currentUpper =
                            Math.Min(
                                upper,
                                Math.Max(
                                    lower,
                                    bars.HighPrices[i]));
                    }

                    if ((breakByWicks && bars.HighPrices[i] >= upper) ||
                        (!breakByWicks && bars.ClosePrices[i] >= upper))
                    {
                        consumed = true;
                        break;
                    }
                }
            }

            int age = Math.Max(0, current - created);
            bool expired = age > maxAge;
            bool eligible = !consumed && !expired && currentUpper > currentLower;

            if (consumed) { currentLower = 0; currentUpper = 0; }

            var lifecycle =
                consumed ? CFIPClean83ZoneLifecycle.Consumed :
                expired ? CFIPClean83ZoneLifecycle.Expired :
                partial ? CFIPClean83ZoneLifecycle.PartiallyMitigated :
                retested ? CFIPClean83ZoneLifecycle.Retested :
                CFIPClean83ZoneLifecycle.Active;

            int quality = Clamp(
                55 +
                (int)Math.Round(Math.Min(20, gap / Math.Max(0.0000001, atr) * 12)) +
                (retested ? 8 : 0) -
                (partial ? 6 : 0) -
                (expired ? 25 : 0));

            return new CFIPClean83ZoneRecord(
                "CFIP79|" + timeframe + "|FVG|" + direction + "|" + created,
                CFIPClean83ZoneKind.FairValueGap, direction, timeframe, created,
                bars.OpenTimes[created], lower, upper,
                eligible ? currentLower : 0,
                eligible ? currentUpper : 0,
                age, retested, partial, consumed, consumed, eligible,
                quality, gap / Math.Max(0.0000001, atr),
                false, false, lifecycle,
                CFIPClean83Provenance.Direct(timeframe, rule ?? "FVG"));
        }

        private void BuildObZones(
            IList<CFIPClean83ZoneRecord> zones, Bars bars, int index,
            string timeframe, double atr, CFIPClean83ConfigSnapshot cfg)
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

                CFIPClean83Direction direction =
                    bars.ClosePrices[impulse] > bars.OpenPrices[impulse]
                        ? CFIPClean83Direction.Buy
                        : bars.ClosePrices[impulse] < bars.OpenPrices[impulse]
                            ? CFIPClean83Direction.Sell
                            : CFIPClean83Direction.Wait;

                if (direction == CFIPClean83Direction.Wait) continue;

                int source = -1;
                for (int j = impulse - 1; j >= start; j--)
                {
                    bool opposite =
                        direction == CFIPClean83Direction.Buy
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
                        direction == CFIPClean83Direction.Buy
                            ? bars.LowPrices[k] <= low
                            : bars.HighPrices[k] >= high;

                    if (breach) { consumed = true; break; }
                }

                int age = Math.Max(0, index - source);
                bool expired = age > maxAge;
                bool eligible = !consumed && !expired && high > low;
                var lifecycle =
                    consumed ? CFIPClean83ZoneLifecycle.Consumed :
                    expired ? CFIPClean83ZoneLifecycle.Expired :
                    retested ? CFIPClean83ZoneLifecycle.Retested :
                    CFIPClean83ZoneLifecycle.Active;

                int quality = Clamp(
                    60 + (int)Math.Round(Math.Min(25, bodyAtr * 12)) +
                    (retested ? 5 : 0) - (expired ? 20 : 0));

                zones.Add(new CFIPClean83ZoneRecord(
                    "CFIP79|" + timeframe + "|OB|" + direction + "|" + source,
                    CFIPClean83ZoneKind.OrderBlock, direction, timeframe, source,
                    bars.OpenTimes[source], low, high,
                    eligible ? low : 0, eligible ? high : 0,
                    age, retested, retested && !consumed,
                    consumed || expired, consumed, eligible,
                    quality, bodyAtr, false, false, lifecycle,
                    CFIPClean83Provenance.Direct(timeframe, "opposite candle before displacement")));
                count++;
            }
        }

        private void BuildEqualLiquidity(
            IList<CFIPClean83LiquidityRecord> liquidity,
            Bars bars, int index, string timeframe,
            double atr, CFIPClean83ConfigSnapshot cfg)
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
                            liquidity, CFIPClean83LiquidityKind.EqualHigh,
                            CFIPClean83LiquiditySide.Above,
                            CFIPClean83Direction.Wait, timeframe, bars, i,
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
                            liquidity, CFIPClean83LiquidityKind.EqualLow,
                            CFIPClean83LiquiditySide.Below,
                            CFIPClean83Direction.Wait, timeframe, bars, i,
                            (bars.LowPrices[low] + bars.LowPrices[i]) / 2.0,
                            tolerance, 78, false, "equal lows pool");
                        break;
                    }
            }
        }

        private void BuildSweepLiquidity(
            IList<CFIPClean83LiquidityRecord> liquidity,
            IList<CFIPClean83StructureEventRecord> events,
            Bars bars, int index, string timeframe,
            double atr, CFIPClean83ConfigSnapshot cfg)
        {
            int lookback = Math.Max(10, cfg.Get("LiquidityLookback", 40));
            double minDepth = Math.Max(0, atr * cfg.Get("LiquiditySweepMinimumDepthAtr", 0.05));
            double priorLow = Lowest(bars, Math.Max(0, index - lookback), index - 1);
            double priorHigh = Highest(bars, Math.Max(0, index - lookback), index - 1);

            double lowPen = Math.Max(0, priorLow - bars.LowPrices[index]);
            if (lowPen >= minDepth && bars.ClosePrices[index] > priorLow)
            {
                AddLiquidity(liquidity, CFIPClean83LiquidityKind.SwingLow,
                    CFIPClean83LiquiditySide.Below, CFIPClean83Direction.Buy,
                    timeframe, bars, index, priorLow, minDepth, 90, true,
                    "bullish liquidity sweep");

                AddEvent(events, CFIPClean83StructureEventKind.LiquiditySweep,
                    CFIPClean83Direction.Buy, timeframe, bars, index, priorLow,
                    lowPen / Math.Max(0.0000001, atr), 90,
                    "low penetration and recovery");
            }

            double highPen = Math.Max(0, bars.HighPrices[index] - priorHigh);
            if (highPen >= minDepth && bars.ClosePrices[index] < priorHigh)
            {
                AddLiquidity(liquidity, CFIPClean83LiquidityKind.SwingHigh,
                    CFIPClean83LiquiditySide.Above, CFIPClean83Direction.Sell,
                    timeframe, bars, index, priorHigh, minDepth, 90, true,
                    "bearish liquidity sweep");

                AddEvent(events, CFIPClean83StructureEventKind.LiquiditySweep,
                    CFIPClean83Direction.Sell, timeframe, bars, index, priorHigh,
                    highPen / Math.Max(0.0000001, atr), 90,
                    "high penetration and recovery");
            }
        }

        private void AddSwingLiquidity(
            IList<CFIPClean83LiquidityRecord> liquidity,
            Bars bars, int index, string timeframe, double atr,
            CFIPClean83ConfigSnapshot cfg)
        {
            int lookback = Math.Max(10, cfg.Get("LiquidityLookback", 40));
            int strength = Math.Max(1, cfg.Get("SwingStrength", 3));
            int high = FindLatestSwingHigh(bars, index, strength, lookback);
            int low = FindLatestSwingLow(bars, index, strength, lookback);

            if (high >= 0)
                AddLiquidity(liquidity, CFIPClean83LiquidityKind.SwingHigh,
                    CFIPClean83LiquiditySide.Above, CFIPClean83Direction.Wait,
                    timeframe, bars, high, bars.HighPrices[high], atr * 0.02, 72,
                    false, "latest confirmed swing high");

            if (low >= 0)
                AddLiquidity(liquidity, CFIPClean83LiquidityKind.SwingLow,
                    CFIPClean83LiquiditySide.Below, CFIPClean83Direction.Wait,
                    timeframe, bars, low, bars.LowPrices[low], atr * 0.02, 72,
                    false, "latest confirmed swing low");
        }

        private void AddPriorDayWeekLiquidity(
            IList<CFIPClean83LiquidityRecord> liquidity,
            CFIPClean83MtfSnapshot mtf, Bars d1Bars, Bars w1Bars,
            CFIPClean83ConfigSnapshot cfg)
        {
            if (!cfg.Get("UseDailyWeeklyLiquidity", true)) return;

            if (d1Bars != null && mtf.D1.IsAvailable && mtf.D1.ClosedIndex > 0)
            {
                int p = mtf.D1.ClosedIndex - 1;
                AddLiquidity(liquidity, CFIPClean83LiquidityKind.PriorDayHigh,
                    CFIPClean83LiquiditySide.Above, CFIPClean83Direction.Wait,
                    "D1", d1Bars, p, d1Bars.HighPrices[p], 0, 82, false, "prior day high");
                AddLiquidity(liquidity, CFIPClean83LiquidityKind.PriorDayLow,
                    CFIPClean83LiquiditySide.Below, CFIPClean83Direction.Wait,
                    "D1", d1Bars, p, d1Bars.LowPrices[p], 0, 82, false, "prior day low");
            }

            if (w1Bars != null && mtf.W1.IsAvailable && mtf.W1.ClosedIndex > 0)
            {
                int p = mtf.W1.ClosedIndex - 1;
                AddLiquidity(liquidity, CFIPClean83LiquidityKind.PriorWeekHigh,
                    CFIPClean83LiquiditySide.Above, CFIPClean83Direction.Wait,
                    "W1", w1Bars, p, w1Bars.HighPrices[p], 0, 86, false, "prior week high");
                AddLiquidity(liquidity, CFIPClean83LiquidityKind.PriorWeekLow,
                    CFIPClean83LiquiditySide.Below, CFIPClean83Direction.Wait,
                    "W1", w1Bars, p, w1Bars.LowPrices[p], 0, 86, false, "prior week low");
            }
        }

        private void AddSessionLiquidity(
            IList<CFIPClean83LiquidityRecord> liquidity,
            CFIPClean83MtfSnapshot mtf, Bars bars,
            CFIPClean83ConfigSnapshot cfg)
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
                AddLiquidity(liquidity, CFIPClean83LiquidityKind.SessionHigh,
                    CFIPClean83LiquiditySide.Above, CFIPClean83Direction.Wait,
                    "M5", bars, highIndex, high, 0, 74, false, "configured session high");

            if (lowIndex >= 0)
                AddLiquidity(liquidity, CFIPClean83LiquidityKind.SessionLow,
                    CFIPClean83LiquiditySide.Below, CFIPClean83Direction.Wait,
                    "M5", bars, lowIndex, low, 0, 74, false, "configured session low");
        }

        private void AddDailyPivots(
            IList<CFIPClean83LiquidityRecord> liquidity,
            CFIPClean83MtfSnapshot mtf, Bars d1Bars,
            CFIPClean83ConfigSnapshot cfg)
        {
            if (!cfg.Get("UseDailyPivots", true) ||
                d1Bars == null || !mtf.D1.IsAvailable ||
                mtf.D1.ClosedIndex <= 0) return;

            int p = mtf.D1.ClosedIndex - 1;
            double h = d1Bars.HighPrices[p], l = d1Bars.LowPrices[p], c = d1Bars.ClosePrices[p];
            double pivot = (h + l + c) / 3.0, range = h - l;

            AddLiquidity(liquidity, CFIPClean83LiquidityKind.DailyPivot,
                CFIPClean83LiquiditySide.Neutral, CFIPClean83Direction.Wait,
                "D1", d1Bars, p, pivot, 0, 62, false, "daily pivot");

            AddLiquidity(liquidity, CFIPClean83LiquidityKind.DailyR1,
                CFIPClean83LiquiditySide.Above, CFIPClean83Direction.Wait,
                "D1", d1Bars, p, 2 * pivot - l, 0, 64, false, "daily R1");

            AddLiquidity(liquidity, CFIPClean83LiquidityKind.DailyR2,
                CFIPClean83LiquiditySide.Above, CFIPClean83Direction.Wait,
                "D1", d1Bars, p, pivot + range, 0, 66, false, "daily R2");

            AddLiquidity(liquidity, CFIPClean83LiquidityKind.DailyS1,
                CFIPClean83LiquiditySide.Below, CFIPClean83Direction.Wait,
                "D1", d1Bars, p, 2 * pivot - h, 0, 64, false, "daily S1");

            AddLiquidity(liquidity, CFIPClean83LiquidityKind.DailyS2,
                CFIPClean83LiquiditySide.Below, CFIPClean83Direction.Wait,
                "D1", d1Bars, p, pivot - range, 0, 66, false, "daily S2");
        }

        private void MarkForecastLiquidity(
            IList<CFIPClean83LiquidityRecord> liquidity,
            CFIPClean83MarketFrame m5,
            CFIPClean83ConfigSnapshot cfg)
        {
            if (!cfg.Get("UseLiquidityForecast", true) ||
                m5 == null ||
                !m5.DataValid)
                return;

            double price = m5.Close;

            for (int i = 0; i < liquidity.Count; i++)
            {
                CFIPClean83LiquidityRecord item = liquidity[i];

                bool ahead =
                    (item.Side == CFIPClean83LiquiditySide.Above &&
                     item.Price > price) ||
                    (item.Side == CFIPClean83LiquiditySide.Below &&
                     item.Price < price);

                if (!ahead ||
                    item.Swept ||
                    item.ForecastCandidate)
                    continue;

                liquidity[i] =
                    new CFIPClean83LiquidityRecord(
                        item.Id,
                        item.Kind,
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
            IList<CFIPClean83ZoneRecord> zones,
            IList<CFIPClean83LiquidityRecord> liquidity,
            CFIPClean83ConfigSnapshot cfg)
        {
            bool enabled = cfg.Get("UseZoneConfluence", true);

            for (int i = 0; i < zones.Count; i++)
            {
                var z = zones[i];
                bool fvg = z.Kind == CFIPClean83ZoneKind.OrderBlock &&
                           HasOverlap(zones, z, CFIPClean83ZoneKind.FairValueGap);
                bool liq = enabled && HasNearbyLiquidity(liquidity, z);

                // Keep base zone quality independent. Confluence is carried
                // explicitly by typed flags and consumed once by Decision quality.
                int q = Clamp(z.Quality);

                zones[i] = new CFIPClean83ZoneRecord(
                    z.Id, z.Kind, z.Direction, z.Timeframe, z.CreatedIndex, z.CreatedUtc,
                    z.OriginalLower, z.OriginalUpper, z.CurrentLower, z.CurrentUpper,
                    z.AgeBars, z.Retested, z.Mitigated, z.Invalidated, z.Consumed, z.ExecutionEligible,
                    q, z.DisplacementAtr, liq, fvg, z.Lifecycle, z.Provenance);
            }
        }

        private bool HasOverlap(
            IList<CFIPClean83ZoneRecord> zones, CFIPClean83ZoneRecord source,
            CFIPClean83ZoneKind kind)
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
            IList<CFIPClean83LiquidityRecord> liquidity,
            CFIPClean83ZoneRecord zone)
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

        private CFIPClean83Direction ResolveDirection(
            IList<CFIPClean83StructureEventRecord> events)
        {
            CFIPClean83StructureEventRecord bull = null, bear = null;
            for (int i = 0; i < events.Count; i++)
            {
                var e = events[i];
                if (!IsDirectional(e.Kind)) continue;

                if (e.Direction == CFIPClean83Direction.Buy &&
                    (bull == null || e.TimeUtc > bull.TimeUtc)) bull = e;

                if (e.Direction == CFIPClean83Direction.Sell &&
                    (bear == null || e.TimeUtc > bear.TimeUtc)) bear = e;
            }

            if (bull == null && bear == null) return CFIPClean83Direction.Wait;
            if (bear == null) return CFIPClean83Direction.Buy;
            if (bull == null) return CFIPClean83Direction.Sell;
            return bull.TimeUtc > bear.TimeUtc
                ? CFIPClean83Direction.Buy
                : CFIPClean83Direction.Sell;
        }

        private bool IsDirectional(CFIPClean83StructureEventKind kind)
        {
            return kind == CFIPClean83StructureEventKind.BreakOfStructure ||
                   kind == CFIPClean83StructureEventKind.MarketStructureShift ||
                   kind == CFIPClean83StructureEventKind.ChangeOfCharacter ||
                   kind == CFIPClean83StructureEventKind.Displacement ||
                   kind == CFIPClean83StructureEventKind.LiquiditySweep;
        }

        private int CalculateQuality(
            IList<CFIPClean83StructureEventRecord> events,
            IList<CFIPClean83ZoneRecord> zones,
            IList<CFIPClean83LiquidityRecord> liquidity,
            CFIPClean83Direction direction)
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

        private CFIPClean83PremiumDiscountState BuildPremiumDiscount(
            Bars bars, int index, CFIPClean83ConfigSnapshot cfg)
        {
            if (!cfg.Get("UsePremiumDiscount", true) || bars == null || index <= 5)
                return new CFIPClean83PremiumDiscountState(false, 0, 0, 0);

            int lookback = Math.Max(10, cfg.Get("LiquidityLookback", 40));
            double high = Highest(bars, Math.Max(0, index - lookback), index);
            double low = Lowest(bars, Math.Max(0, index - lookback), index);
            double range = high - low;
            if (range <= 0) return new CFIPClean83PremiumDiscountState(false, 0, 0, 0);

            return new CFIPClean83PremiumDiscountState(
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
            IList<CFIPClean83StructureEventRecord> events,
            CFIPClean83StructureEventKind kind, CFIPClean83Direction direction,
            string timeframe, Bars bars, int index, double price,
            double strengthAtr, int quality, string detail)
        {
            events.Add(new CFIPClean83StructureEventRecord(
                "CFIP79|" + timeframe + "|" + kind + "|" + index,
                kind, direction, timeframe, index, bars.OpenTimes[index], price,
                strengthAtr, quality,
                CFIPClean83Provenance.Direct("STRUCTURE", detail)));
        }

        private void AddLiquidity(
            IList<CFIPClean83LiquidityRecord> list,
            CFIPClean83LiquidityKind kind, CFIPClean83LiquiditySide side,
            CFIPClean83Direction sweepDirection, string timeframe, Bars bars,
            int index, double price, double tolerance, int quality,
            bool swept, string detail)
        {
            int safe = Math.Max(0, Math.Min(index, bars.Count - 1));
            double reference = bars.ClosePrices[safe];
            list.Add(new CFIPClean83LiquidityRecord(
                "CFIP79|" + timeframe + "|" + kind + "|" + index,
                kind, side, sweepDirection, timeframe, safe, bars.OpenTimes[safe],
                price, tolerance, Math.Abs(price - reference),
                swept ? tolerance : 0, swept, false, quality,
                CFIPClean83Provenance.Direct(timeframe, detail)));
        }

        private int Clamp(int value)
        {
            return Math.Max(0, Math.Min(100, value));
        }
    }

    // ------------------------------------------------------------------------
    // Decision
    // ------------------------------------------------------------------------

    public sealed class CFIPClean83DecisionSnapshot
    {
        private readonly ReadOnlyCollection<CFIPClean83BlockReason> _blockReasons;

        public CFIPClean83Direction Direction { get; private set; }
        public int Confidence { get; private set; }
        public int Quality { get; private set; }
        public int Edge { get; private set; }
        public int MtfAgreement { get; private set; }
        public int IndependentEvidence { get; private set; }
        public int StructuralConfirmations { get; private set; }
        public string Regime { get; private set; }
        public int RegimeQuality { get; private set; }

        // Decision-level eligibility. Entry/Trigger eligibility belongs to Phase 7.
        public bool DecisionEligible { get; private set; }

        public CFIPClean83DecisionPolicyMode PolicyMode { get; private set; }
        public IReadOnlyList<CFIPClean83BlockReason> BlockReasons
        {
            get { return _blockReasons; }
        }
        public CFIPClean83Provenance Provenance { get; private set; }

        public CFIPClean83DecisionSnapshot(
            CFIPClean83Direction direction,
            int confidence,
            int quality,
            int edge,
            int mtfAgreement,
            int independentEvidence,
            int structuralConfirmations,
            string regime,
            int regimeQuality,
            bool decisionEligible,
            CFIPClean83DecisionPolicyMode policyMode,
            IList<CFIPClean83BlockReason> blockReasons,
            CFIPClean83Provenance provenance)
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
            DecisionEligible = decisionEligible;
            PolicyMode = policyMode;

            var normalizedBlocks =
                new List<CFIPClean83BlockReason>(
                    blockReasons ??
                    new List<CFIPClean83BlockReason>());

            if (DecisionEligible &&
                (Direction == CFIPClean83Direction.Wait ||
                 normalizedBlocks.Count > 0))
                throw new ArgumentException(
                    "Eligible decision cannot be WAIT or blocked.",
                    "decisionEligible");

            if (Direction == CFIPClean83Direction.Wait &&
                !normalizedBlocks.Contains(CFIPClean83BlockReason.NoDirection))
                throw new ArgumentException(
                    "WAIT decision requires a NoDirection block.",
                    "decision");

            if (Direction != CFIPClean83Direction.Wait &&
                !DecisionEligible &&
                normalizedBlocks.Count == 0)
                throw new ArgumentException(
                    "Blocked directional decision requires at least one block.",
                    "decisionEligible");

            if (!DecisionEligible &&
                (PolicyMode == CFIPClean83DecisionPolicyMode.Confirmed ||
                 PolicyMode == CFIPClean83DecisionPolicyMode.Aggressive))
                throw new ArgumentException(
                    "Confirmed/Aggressive policy requires decision eligibility.",
                    "policyMode");

            if (DecisionEligible &&
                PolicyMode != CFIPClean83DecisionPolicyMode.Confirmed &&
                PolicyMode != CFIPClean83DecisionPolicyMode.Aggressive)
                throw new ArgumentException(
                    "Eligible decision requires Confirmed or Aggressive policy.",
                    "policyMode");

            if (PolicyMode == CFIPClean83DecisionPolicyMode.Pending &&
                (Direction == CFIPClean83Direction.Wait ||
                 normalizedBlocks.Count == 0))
                throw new ArgumentException(
                    "Pending policy requires a directional blocked setup.",
                    "policyMode");

            _blockReasons =
                new ReadOnlyCollection<CFIPClean83BlockReason>(
                    normalizedBlocks);
            Provenance =
                provenance ??
                CFIPClean83Provenance.Direct(
                    "UNKNOWN",
                    "UNSPECIFIED");
        }
    }


    // ------------------------------------------------------------------------
    // Phase 6 — authoritative Decision Engine
    // ------------------------------------------------------------------------

    public sealed class CFIPClean83DecisionEngine : ICFIPClean83DecisionEngine
    {
        private sealed class FeatureAggregate
        {
            public bool Seen;
            public double BullScore;
            public double BearScore;
        }

        private sealed class ZoneAggregate
        {
            public int Quality;
            public bool Retested;
            public bool LiquidityConfluence;
            public bool FvgConfluence;
        }

        private sealed class LiquidityAggregate
        {
            public int Quality;
        }

        private sealed class Evidence
        {
            public double Bull;
            public double Bear;
            public int Independent;
            public int Structural;
            public int BullStructuralConfirmations;
            public int BearStructuralConfirmations;
            public int BullStructure;
            public int BearStructure;
            public int BullZone;
            public int BearZone;
            public int BullLiquidity;
            public int BearLiquidity;
            public int BullConfluence;
            public int BearConfluence;
            public int BullRetest;
            public int BearRetest;
        }

        public CFIPClean83DecisionSnapshot Evaluate(
            CFIPClean83RuntimeSnapshot runtime,
            CFIPClean83MtfSnapshot mtf,
            CFIPClean83MarketModel market,
            CFIPClean83StructureSnapshot structure,
            CFIPClean83ConfigSnapshot configuration)
        {
            var blocks = new List<CFIPClean83BlockReason>();

            if (runtime == null || mtf == null || market == null ||
                structure == null || configuration == null)
                return Blocked(
                    CFIPClean83BlockReason.DataIncomplete,
                    "MISSING_INPUT");

            if (!mtf.IsPrimaryDecisionReady ||
                !CFIPClean83MtfSnapshotBuilder.IsCoherent(mtf) ||
                !market.IsCoherent || !market.IsPrimaryReady ||
                !structure.IsCoherent || !structure.IsPrimaryReady)
                return Blocked(
                    CFIPClean83BlockReason.DataIncomplete,
                    "SNAPSHOT_NOT_READY");

            CFIPClean83MarketFrame m5 = market.FindFrame("M5");
            string regime =
                m5 == null
                    ? "UNKNOWN"
                    : m5.Regime.ToString();

            int adaptiveQualityThreshold;
            int adaptiveShareThreshold;
            int adaptiveEdgeThreshold;

            GetAdaptiveSmartThresholds(
                regime,
                configuration,
                out adaptiveQualityThreshold,
                out adaptiveShareThreshold,
                out adaptiveEdgeThreshold);

            Evidence e = CollectEvidence(
                market,
                structure,
                configuration);

            int bullShare;
            int bearShare;

            CalculateDirectionalShares(
                e.Bull,
                e.Bear,
                configuration.Get("SmartScoreTemperature", 12.0),
                out bullShare,
                out bearShare);

            int strongestShare = Math.Max(bullShare, bearShare);

            CFIPClean83Direction direction =
                e.Bull <= 0 &&
                e.Bear <= 0
                    ? CFIPClean83Direction.Wait
                    : strongestShare < adaptiveShareThreshold
                        ? CFIPClean83Direction.Wait
                        : bullShare > bearShare
                            ? CFIPClean83Direction.Buy
                            : bearShare > bullShare
                                ? CFIPClean83Direction.Sell
                                : CFIPClean83Direction.Wait;

            int edge = Math.Abs(bullShare - bearShare);

            int mtfAgreement =
                CalculateMtfAgreement(
                    direction,
                    market,
                    configuration);

            int marketQuality = m5 == null ? 0 : m5.MarketQuality;
            int regimeQuality = m5 == null ? 0 : m5.RegimeQuality;

            // Structural confirmations belong to the selected direction only.
            // Opposing confirmations must never satisfy the selected direction's gate.
            e.Structural =
                direction == CFIPClean83Direction.Buy
                    ? e.BullStructuralConfirmations
                    : direction == CFIPClean83Direction.Sell
                        ? e.BearStructuralConfirmations
                        : 0;

            int structureQuality =
                direction == CFIPClean83Direction.Buy
                    ? e.BullStructure
                    : direction == CFIPClean83Direction.Sell
                        ? e.BearStructure : 0;

            int zoneQuality =
                direction == CFIPClean83Direction.Buy
                    ? e.BullZone
                    : direction == CFIPClean83Direction.Sell
                        ? e.BearZone : 0;

            int liquidityQuality =
                direction == CFIPClean83Direction.Buy
                    ? e.BullLiquidity
                    : direction == CFIPClean83Direction.Sell
                        ? e.BearLiquidity : 0;

            int confluenceQuality =
                configuration.Get("UseAdvancedConfluence", true)
                    ? direction == CFIPClean83Direction.Buy
                        ? e.BullConfluence
                        : direction == CFIPClean83Direction.Sell
                            ? e.BearConfluence : 0
                    : 0;

            int retestQuality =
                direction == CFIPClean83Direction.Buy
                    ? e.BullRetest
                    : direction == CFIPClean83Direction.Sell
                        ? e.BearRetest : 0;

            // Weighted domains intentionally sum to 1.00. Structure, zone,
            // liquidity and retest are not allowed to independently recreate
            // the same directional indicator score.
            int quality = Clamp((int)Math.Round(
                marketQuality * 0.28 +
                mtfAgreement * 0.18 +
                structureQuality * 0.18 +
                zoneQuality * 0.12 +
                liquidityQuality * 0.07 +
                confluenceQuality * 0.05 +
                retestQuality * 0.06 +
                regimeQuality * 0.06), 0, 100);

            int confidence = Clamp(
                (int)Math.Round(quality * 0.70 + (50 + edge) * 0.30),
                0, 100);

            confidence = ApplyHigherTimeframePenalty(
                confidence,
                direction,
                market,
                configuration);

            if (direction == CFIPClean83Direction.Wait)
            {
                // A neutral directional consensus is the root decision block.
                // Direction-dependent gates are not meaningful until a direction
                // exists, so they must not pollute the exact block-reason ledger.
                blocks.Add(CFIPClean83BlockReason.NoDirection);
            }
            else
            {
                if (confidence < configuration.Get("MinimumConfidence", 72))
                    blocks.Add(CFIPClean83BlockReason.ConfidenceTooLow);
    
                if (edge < adaptiveEdgeThreshold)
                    blocks.Add(CFIPClean83BlockReason.EvidenceInsufficient);
    
                if (quality < adaptiveQualityThreshold)
                    blocks.Add(CFIPClean83BlockReason.PolicyBlocked);
    
                if (mtfAgreement < configuration.Get("MinimumTimeframeAgreement", 72) &&
                    configuration.Get("RequireHigherTfAgreement", true))
                    blocks.Add(CFIPClean83BlockReason.MtfDisagreement);
    
                int requiredEvidence = Math.Max(
                    configuration.Get("MinimumIndependentEvidence", 4),
                    configuration.Get("EnableSmartDecisionEngine", true)
                        ? configuration.Get("SmartMinimumIndependentEvidence", 4) : 0);
    
                if (e.Independent < requiredEvidence)
                    blocks.Add(CFIPClean83BlockReason.EvidenceInsufficient);
    
                if (configuration.Get("RequireStructuralConfirmation", true) &&
                    e.Structural < configuration.Get("MinimumStructuralConfirmations", 4))
                    blocks.Add(CFIPClean83BlockReason.StructureInvalid);
    
                if (configuration.Get("RequireCoreAgreement", true) &&
                    !CoreAgreement(direction, market, configuration))
                    blocks.Add(CFIPClean83BlockReason.MtfDisagreement);
    
                if (configuration.Get("EnableSmartDecisionEngine", true))
                {
                    if (configuration.Get("RequireSmartConsensus", true) &&
                        strongestShare <
                        Math.Max(
                            configuration.Get("SmartConsensusThreshold", 57),
                            adaptiveShareThreshold))
                    {
                        bool soft =
                            configuration.Get("AllowSmartSoftGate", true) &&
                            quality >= configuration.Get("SmartStrongSetupQuality", 82) &&
                            edge >= configuration.Get("SmartStrongSetupEdge", 10) &&
                            e.Independent >= requiredEvidence + 1;
    
                        if (!soft)
                            blocks.Add(CFIPClean83BlockReason.PolicyBlocked);
                    }
    
                    if (mtfAgreement <
                        configuration.Get("SmartMinimumTimeframeAgreement", 72))
                        blocks.Add(CFIPClean83BlockReason.MtfDisagreement);
                }
    
                // Legacy name retained for preset parity. In Phase 6 this is
                // only a Decision-quality policy floor; actual Entry/Trigger eligibility
                // remains exclusively owned by Phase 7.
                if (configuration.Get("UseSmartEntryQualityFilter", true) &&
                    quality < Math.Max(
                        configuration.Get("SmartQualityThreshold", 70),
                        Math.Max(
                            adaptiveQualityThreshold,
                            configuration.Get("EnableSmartDecisionEngine", true)
                                ? SmartMinimumConsensusFloor(configuration)
                                : 0)))
                    blocks.Add(CFIPClean83BlockReason.PolicyBlocked);
    
                if (configuration.Get("UseRegimeNoTradeGuard", true) &&
                    RegimeBlocked(regime, regimeQuality, configuration))
                    blocks.Add(CFIPClean83BlockReason.VolatilityBlocked);
    
                if (configuration.Get("UseHistoricalChoppinessGuard", true) &&
                    m5 != null && m5.Choppy &&
                    market.M15 != null && market.M15.Choppy &&
                    quality < Math.Max(
                        configuration.Get("SmartRegimeQualityFloor", 55) + 5,
                        configuration.Get("NoTradeMinimumSmartQuality", 55) + 5))
                    blocks.Add(CFIPClean83BlockReason.VolatilityBlocked);
    
    
            }

            blocks = Distinct(blocks);

            bool eligible =
                direction != CFIPClean83Direction.Wait &&
                blocks.Count == 0;

            CFIPClean83DecisionPolicyMode policy =
                ResolvePolicy(
                    eligible,
                    direction,
                    confidence,
                    quality,
                    e.Independent,
                    blocks,
                    configuration);

            return new CFIPClean83DecisionSnapshot(
                direction, confidence, quality, edge, mtfAgreement,
                e.Independent, e.Structural, regime, regimeQuality,
                eligible, policy, blocks,
                CFIPClean83Provenance.Direct(
                    "DECISION_ENGINE",
                    eligible ? "DECISION_ELIGIBLE" : "DECISION_BLOCKED"));
        }

        private Evidence CollectEvidence(
            CFIPClean83MarketModel market,
            CFIPClean83StructureSnapshot structure,
            CFIPClean83ConfigSnapshot configuration)
        {
            var e = new Evidence();

            // Directional market evidence is deduplicated by feature family
            // across timeframes. MTF agreement is a separate semantic domain.
            AddMarketEvidence(
                e,
                market.FindFrame("M5"),
                market.FindFrame("M15"),
                market.FindFrame("M30"),
                market.FindFrame("H1"),
                market.FindFrame("H4"),
                market.FindFrame("D1"),
                market.FindFrame("W1"));

            bool bullM5Break = false;
            bool bullM5Displacement = false;
            bool bullM15Break = false;
            bool bullH1Break = false;
            bool bullH4Break = false;

            bool bearM5Break = false;
            bool bearM5Displacement = false;
            bool bearM15Break = false;
            bool bearH1Break = false;
            bool bearH4Break = false;

            int bullDisplacement = 0;
            int bearDisplacement = 0;

            for (int i = 0; i < structure.Events.Count; i++)
            {
                CFIPClean83StructureEventRecord x = structure.Events[i];
                if (x == null || x.Direction == CFIPClean83Direction.Wait)
                    continue;

                bool isStructureBreak =
                    x.Kind == CFIPClean83StructureEventKind.BreakOfStructure ||
                    x.Kind == CFIPClean83StructureEventKind.MarketStructureShift ||
                    x.Kind == CFIPClean83StructureEventKind.ChangeOfCharacter;

                bool isM5 = string.Equals(
                    x.Timeframe, "M5", StringComparison.OrdinalIgnoreCase);
                bool isM15 = string.Equals(
                    x.Timeframe, "M15", StringComparison.OrdinalIgnoreCase);
                bool isH1 = string.Equals(
                    x.Timeframe, "H1", StringComparison.OrdinalIgnoreCase);
                bool isH4 = string.Equals(
                    x.Timeframe, "H4", StringComparison.OrdinalIgnoreCase);

                if (x.Direction == CFIPClean83Direction.Buy)
                {
                    if (isStructureBreak)
                    {
                        e.BullStructure = Math.Max(e.BullStructure, x.Quality);

                        if (isM5) bullM5Break = true;
                        else if (isM15) bullM15Break = true;
                        else if (isH1) bullH1Break = true;
                        else if (isH4) bullH4Break = true;
                    }
                    else if (x.Kind == CFIPClean83StructureEventKind.Displacement)
                    {
                        bullDisplacement = Math.Max(bullDisplacement, x.Quality);
                        if (isM5) bullM5Displacement = true;
                    }
                }
                else
                {
                    if (isStructureBreak)
                    {
                        e.BearStructure = Math.Max(e.BearStructure, x.Quality);

                        if (isM5) bearM5Break = true;
                        else if (isM15) bearM15Break = true;
                        else if (isH1) bearH1Break = true;
                        else if (isH4) bearH4Break = true;
                    }
                    else if (x.Kind == CFIPClean83StructureEventKind.Displacement)
                    {
                        bearDisplacement = Math.Max(bearDisplacement, x.Quality);
                        if (isM5) bearM5Displacement = true;
                    }
                }
            }

            // Preserve the v73 confirmation concept without treating BOS/MSS/CHOCH
            // as three independent confirmations. A structural-break family counts
            // once per timeframe; M5 displacement remains a distinct confirmation.
            e.BullStructuralConfirmations =
                (bullM5Break ? 1 : 0) +
                (bullM5Displacement ? 1 : 0) +
                (bullM15Break ? 1 : 0) +
                (bullH1Break ? 1 : 0) +
                (bullH4Break ? 1 : 0);

            e.BearStructuralConfirmations =
                (bearM5Break ? 1 : 0) +
                (bearM5Displacement ? 1 : 0) +
                (bearM15Break ? 1 : 0) +
                (bearH1Break ? 1 : 0) +
                (bearH4Break ? 1 : 0);

            e.Bull += e.BullStructuralConfirmations > 0
                ? e.BullStructure * 0.10
                : 0;

            e.Bear += e.BearStructuralConfirmations > 0
                ? e.BearStructure * 0.10
                : 0;

            if (bullDisplacement > 0)
                e.Bull += bullDisplacement * 0.10;

            if (bearDisplacement > 0)
                e.Bear += bearDisplacement * 0.10;

            // Zones are deduplicated by zone family (FVG/OB) per direction.
            // Multiple timeframes or repeated instances of the same family cannot
            // manufacture directional confidence through record count.
            var bullZones =
                new Dictionary<CFIPClean83ZoneKind, ZoneAggregate>();
            var bearZones =
                new Dictionary<CFIPClean83ZoneKind, ZoneAggregate>();

            for (int i = 0; i < structure.Zones.Count; i++)
            {
                CFIPClean83ZoneRecord z = structure.Zones[i];
                if (z == null || z.Direction == CFIPClean83Direction.Wait ||
                    z.Invalidated || z.Consumed || !z.ExecutionEligible)
                    continue;

                Dictionary<CFIPClean83ZoneKind, ZoneAggregate> target =
                    z.Direction == CFIPClean83Direction.Buy
                        ? bullZones
                        : bearZones;

                ZoneAggregate aggregate;
                if (!target.TryGetValue(z.Kind, out aggregate))
                {
                    aggregate = new ZoneAggregate();
                    target.Add(z.Kind, aggregate);
                }

                if (z.Quality > aggregate.Quality)
                {
                    aggregate.Quality = z.Quality;
                    aggregate.Retested = z.Retested;
                    aggregate.LiquidityConfluence = z.LiquidityConfluence;
                    aggregate.FvgConfluence = z.FvgConfluence;
                }
                else if (z.Quality == aggregate.Quality)
                {
                    aggregate.Retested =
                        aggregate.Retested || z.Retested;
                    aggregate.LiquidityConfluence =
                        aggregate.LiquidityConfluence ||
                        z.LiquidityConfluence;
                    aggregate.FvgConfluence =
                        aggregate.FvgConfluence ||
                        z.FvgConfluence;
                }
            }

            foreach (KeyValuePair<CFIPClean83ZoneKind, ZoneAggregate> pair in bullZones)
            {
                ZoneAggregate aggregate = pair.Value;
                if (aggregate == null || aggregate.Quality <= 0)
                    continue;

                e.Bull += aggregate.Quality * 0.08;
                e.BullZone = Math.Max(e.BullZone, aggregate.Quality);
                if (aggregate.Retested)
                    e.BullRetest = Math.Max(e.BullRetest, aggregate.Quality);
                if (aggregate.LiquidityConfluence || aggregate.FvgConfluence)
                    e.BullConfluence = Math.Max(
                        e.BullConfluence,
                        aggregate.Quality);
            }

            foreach (KeyValuePair<CFIPClean83ZoneKind, ZoneAggregate> pair in bearZones)
            {
                ZoneAggregate aggregate = pair.Value;
                if (aggregate == null || aggregate.Quality <= 0)
                    continue;

                e.Bear += aggregate.Quality * 0.08;
                e.BearZone = Math.Max(e.BearZone, aggregate.Quality);
                if (aggregate.Retested)
                    e.BearRetest = Math.Max(e.BearRetest, aggregate.Quality);
                if (aggregate.LiquidityConfluence || aggregate.FvgConfluence)
                    e.BearConfluence = Math.Max(
                        e.BearConfluence,
                        aggregate.Quality);
            }

            // Liquidity sweeps are deduplicated by liquidity-pool family. This
            // preserves distinct pools (PDH, PDL, equal highs, swings, etc.) while
            // preventing repeated timeframe records from dominating the score.
            var bullLiquidity =
                new Dictionary<CFIPClean83LiquidityKind, LiquidityAggregate>();
            var bearLiquidity =
                new Dictionary<CFIPClean83LiquidityKind, LiquidityAggregate>();

            for (int i = 0; i < structure.Liquidity.Count; i++)
            {
                CFIPClean83LiquidityRecord l = structure.Liquidity[i];
                if (l == null || !l.Swept ||
                    l.SweepDirection == CFIPClean83Direction.Wait)
                    continue;

                Dictionary<CFIPClean83LiquidityKind, LiquidityAggregate> target =
                    l.SweepDirection == CFIPClean83Direction.Buy
                        ? bullLiquidity
                        : bearLiquidity;

                LiquidityAggregate aggregate;
                if (!target.TryGetValue(l.Kind, out aggregate))
                {
                    aggregate = new LiquidityAggregate();
                    target.Add(l.Kind, aggregate);
                }

                aggregate.Quality =
                    Math.Max(
                        aggregate.Quality,
                        l.Quality);
            }

            foreach (KeyValuePair<CFIPClean83LiquidityKind, LiquidityAggregate> pair in bullLiquidity)
            {
                LiquidityAggregate aggregate = pair.Value;
                if (aggregate == null || aggregate.Quality <= 0)
                    continue;

                e.Bull += aggregate.Quality * 0.06;
                e.BullLiquidity = Math.Max(
                    e.BullLiquidity,
                    aggregate.Quality);
            }

            foreach (KeyValuePair<CFIPClean83LiquidityKind, LiquidityAggregate> pair in bearLiquidity)
            {
                LiquidityAggregate aggregate = pair.Value;
                if (aggregate == null || aggregate.Quality <= 0)
                    continue;

                e.Bear += aggregate.Quality * 0.06;
                e.BearLiquidity = Math.Max(
                    e.BearLiquidity,
                    aggregate.Quality);
            }

            if (configuration.Get("UsePremiumDiscount", true) &&
                structure.PremiumDiscount != null &&
                structure.PremiumDiscount.Available)
            {
                // Preserve the v73 location bias: discount supports BUY,
                // premium supports SELL. This affects directional score only;
                // it is not an independent-evidence count.
                if (structure.PremiumDiscount.IsDiscount)
                    e.Bull += 6.0;
                else if (structure.PremiumDiscount.IsPremium)
                    e.Bear += 6.0;
            }

            // Confluence is a quality modifier, not another directional evidence
            // source. It is consumed only by the weighted quality domain.
            return e;
        }

        private void AddMarketEvidence(
            Evidence e,
            params CFIPClean83MarketFrame[] frames)
        {
            if (e == null || frames == null)
                return;

            var families =
                new Dictionary<CFIPClean83MarketFeature, FeatureAggregate>();

            for (int i = 0; i < frames.Length; i++)
            {
                CFIPClean83MarketFrame frame = frames[i];
                if (frame == null || !frame.DataValid)
                    continue;

                for (int j = 0; j < frame.Features.Count; j++)
                {
                    CFIPClean83FeatureEvidence feature = frame.Features[j];
                    if (feature == null ||
                        !feature.Triggered ||
                        !feature.CountsAsEvidence ||
                        feature.Direction == CFIPClean83Direction.Wait)
                        continue;

                    FeatureAggregate aggregate;
                    if (!families.TryGetValue(feature.Feature, out aggregate))
                    {
                        aggregate = new FeatureAggregate();
                        families.Add(feature.Feature, aggregate);
                    }

                    aggregate.Seen = true;

                    // Feature.Value is normalized to [0,1], while Weight is
                    // the market-model semantic weight. Keep that weighting
                    // and independently retain the strongest BUY and SELL
                    // observation for the feature family across timeframes.
                    double score = feature.Value * Math.Max(0, feature.Weight);

                    if (feature.Direction == CFIPClean83Direction.Buy)
                        aggregate.BullScore = Math.Max(
                            aggregate.BullScore,
                            score);
                    else if (feature.Direction == CFIPClean83Direction.Sell)
                        aggregate.BearScore = Math.Max(
                            aggregate.BearScore,
                            score);
                }
            }

            foreach (KeyValuePair<CFIPClean83MarketFeature, FeatureAggregate> pair in families)
            {
                FeatureAggregate aggregate = pair.Value;
                if (aggregate == null || !aggregate.Seen)
                    continue;

                // A market feature family contributes once. Keep the stronger
                // directional observation; an exact tie is neutral rather than
                // inheriting the answer from timeframe iteration order.
                if (aggregate.BullScore > aggregate.BearScore &&
                    aggregate.BullScore > 0)
                {
                    e.Bull += aggregate.BullScore;
                    e.Independent++;
                }
                else if (aggregate.BearScore > aggregate.BullScore &&
                         aggregate.BearScore > 0)
                {
                    e.Bear += aggregate.BearScore;
                    e.Independent++;
                }
            }
        }

        private int ApplyHigherTimeframePenalty(
            int confidence,
            CFIPClean83Direction direction,
            CFIPClean83MarketModel market,
            CFIPClean83ConfigSnapshot cfg)
        {
            if (direction == CFIPClean83Direction.Wait ||
                market == null)
                return confidence;

            int basePenalty = Math.Max(
                0,
                cfg.Get("HigherTfPenalty", 7));

            if (basePenalty == 0)
                return confidence;

            bool h1Against = IsAgainst(
                market.FindFrame("H1"),
                direction);

            bool h4Against = IsAgainst(
                market.FindFrame("H4"),
                direction);

            bool d1Against = IsAgainst(
                market.FindFrame("D1"),
                direction);

            if (!h1Against && !h4Against && !d1Against)
                return confidence;

            int penalty =
                basePenalty +
                (d1Against
                    ? basePenalty / 2
                    : 0);

            return Clamp(
                confidence - penalty,
                0,
                100);
        }

        private bool IsAgainst(
            CFIPClean83MarketFrame frame,
            CFIPClean83Direction direction)
        {
            return frame != null &&
                   frame.DataValid &&
                   frame.BiasDirection != CFIPClean83Direction.Wait &&
                   frame.BiasDirection != direction;
        }

        private void CalculateDirectionalShares(
            double bull,
            double bear,
            double temperature,
            out int bullShare,
            out int bearShare)
        {
            double safeTemperature = Math.Max(1.0, temperature);
            double centered = (bull - bear) / safeTemperature;

            double expBull = Math.Exp(
                Clamp(
                    centered,
                    -12,
                    12));

            double expBear = Math.Exp(
                Clamp(
                    -centered,
                    -12,
                    12));

            double total =
                Math.Max(
                    1e-9,
                    expBull + expBear);

            bullShare = Clamp(
                (int)Math.Round(
                    100.0 * expBull / total),
                0,
                100);

            bearShare = 100 - bullShare;
        }

        private void GetAdaptiveSmartThresholds(
            string regime,
            CFIPClean83ConfigSnapshot cfg,
            out int qualityThreshold,
            out int shareThreshold,
            out int edgeThreshold)
        {
            qualityThreshold = Math.Max(
                40,
                Math.Min(
                    95,
                    cfg.Get("MinimumSmartQuality", 70)));

            shareThreshold = Math.Max(
                50,
                Math.Min(
                    90,
                    cfg.Get("MinimumSmartDirectionShare", 57)));

            edgeThreshold = Math.Max(
                4,
                Math.Min(
                    30,
                    cfg.Get("MinimumEdge", 15)));

            if (!cfg.Get("AdaptiveSmartThresholds", true))
                return;

            int buffer = Math.Max(
                0,
                cfg.Get("SmartRegimeBuffer", 6));

            switch (regime ?? "UNKNOWN")
            {
                case "TREND":
                case "Trend":
                case "EXPANSION":
                case "Expansion":
                    qualityThreshold -= buffer;
                    shareThreshold -= Math.Max(1, buffer / 3);
                    edgeThreshold -= Math.Max(1, buffer / 3);
                    break;

                case "REVERSAL":
                case "Reversal":
                    qualityThreshold -= Math.Max(1, buffer / 2);
                    break;

                case "RANGE":
                case "Range":
                    qualityThreshold += Math.Max(1, buffer / 2);
                    shareThreshold += Math.Max(1, buffer / 3);
                    edgeThreshold += Math.Max(1, buffer / 3);
                    break;

                case "TRANSITION":
                case "Transition":
                    qualityThreshold += Math.Max(1, buffer / 2);
                    shareThreshold += Math.Max(1, buffer / 3);
                    edgeThreshold += Math.Max(1, buffer / 3);
                    break;

                case "COMPRESSION":
                case "Compression":
                    qualityThreshold += buffer;
                    shareThreshold += Math.Max(1, buffer / 2);
                    edgeThreshold += Math.Max(1, buffer / 2);
                    break;
            }

            qualityThreshold = Math.Max(
                40,
                Math.Min(
                    95,
                    qualityThreshold));

            shareThreshold = Math.Max(
                50,
                Math.Min(
                    90,
                    shareThreshold));

            edgeThreshold = Math.Max(
                4,
                Math.Min(
                    30,
                    edgeThreshold));
        }

        private int SmartMinimumConsensusFloor(
            CFIPClean83ConfigSnapshot cfg)
        {
            return Math.Max(
                40,
                cfg.Get("SmartConsensusThreshold", 57) - 12);
        }

        private int CalculateMtfAgreement(
            CFIPClean83Direction direction,
            CFIPClean83MarketModel market,
            CFIPClean83ConfigSnapshot cfg)
        {
            if (direction == CFIPClean83Direction.Wait) return 0;
            double total = 0, aligned = 0;

            AddMtf(market.FindFrame("M5"), cfg.Get("M5Weight", 7), direction, ref total, ref aligned);
            AddMtf(market.FindFrame("M15"), cfg.Get("M15Weight", 8), direction, ref total, ref aligned);
            AddMtf(market.FindFrame("M30"), cfg.Get("M30Weight", 5), direction, ref total, ref aligned);
            AddMtf(market.FindFrame("H1"), cfg.Get("H1Weight", 3), direction, ref total, ref aligned);
            AddMtf(market.FindFrame("H4"), cfg.Get("H4Weight", 2), direction, ref total, ref aligned);
            AddMtf(market.FindFrame("D1"), cfg.Get("D1Weight", 2), direction, ref total, ref aligned);
            if (cfg.Get("SmartWeeklyContext", true))
                AddMtf(market.FindFrame("W1"), cfg.Get("W1Weight", 3), direction, ref total, ref aligned);

            return total <= 0 ? 0 : Clamp((int)Math.Round(100.0 * aligned / total), 0, 100);
        }

        private void AddMtf(
            CFIPClean83MarketFrame frame, int weight,
            CFIPClean83Direction direction,
            ref double total, ref double aligned)
        {
            if (frame == null || !frame.DataValid ||
                frame.BiasDirection == CFIPClean83Direction.Wait || weight <= 0)
                return;

            total += weight;
            if (frame.BiasDirection == direction) aligned += weight;
        }

        private bool CoreAgreement(
            CFIPClean83Direction direction,
            CFIPClean83MarketModel market,
            CFIPClean83ConfigSnapshot cfg)
        {
            if (direction == CFIPClean83Direction.Wait) return false;
            CFIPClean83MarketFrame m5 = market.FindFrame("M5");
            CFIPClean83MarketFrame m15 = market.FindFrame("M15");
            if (m5 == null || m15 == null || m5.BiasDirection != direction)
                return false;

            return m15.BiasDirection == direction ||
                (cfg.Get("AllowM15NeutralPullback", true) &&
                 m15.BiasDirection == CFIPClean83Direction.Wait);
        }

        private bool RegimeBlocked(
            string regime, int quality, CFIPClean83ConfigSnapshot cfg)
        {
            if (string.Equals(regime, "Compression", StringComparison.OrdinalIgnoreCase) &&
                cfg.Get("BlockCompressionRegime", true))
                return true;

            bool weak =
                (string.Equals(regime, "Range", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(regime, "Transition", StringComparison.OrdinalIgnoreCase)) &&
                quality < cfg.Get("SmartRegimeQualityFloor", 55);

            return weak && cfg.Get("BlockWeakRangeTransition", true);
        }

        private CFIPClean83DecisionPolicyMode ResolvePolicy(
            bool eligible,
            CFIPClean83Direction direction,
            int confidence,
            int quality,
            int evidence,
            IList<CFIPClean83BlockReason> blocks,
            CFIPClean83ConfigSnapshot cfg)
        {
            if (eligible &&
                cfg.Get("EnableAggressiveAutoEntry", false) &&
                confidence >= cfg.Get("AggressiveMinimumConfidence", 88) &&
                evidence >= cfg.Get("AggressiveMinimumEvidence", 4) &&
                quality >= cfg.Get("AggressiveMinimumSmartQuality", 78))
                return CFIPClean83DecisionPolicyMode.Aggressive;

            if (eligible)
                return CFIPClean83DecisionPolicyMode.Confirmed;

            // Phase 6 owns only policy state. Trigger/retest execution remains
            // Phase 7. Pending therefore means "directional setup exists but
            // one or more policy gates are not yet satisfied".
            if (direction != CFIPClean83Direction.Wait &&
                cfg.Get("EnableSmartDecisionEngine", true) &&
                blocks != null &&
                blocks.Count > 0 &&
                quality >= cfg.Get("NoTradeMinimumSmartQuality", 55))
                return CFIPClean83DecisionPolicyMode.Pending;

            return CFIPClean83DecisionPolicyMode.Soft;
        }

        private static List<CFIPClean83BlockReason> Distinct(
            IList<CFIPClean83BlockReason> source)
        {
            var result = new List<CFIPClean83BlockReason>();
            if (source == null) return result;

            for (int i = 0; i < source.Count; i++)
            {
                if (source[i] == CFIPClean83BlockReason.None) continue;
                bool found = false;
                for (int j = 0; j < result.Count; j++)
                    if (result[j] == source[i]) { found = true; break; }
                if (!found) result.Add(source[i]);
            }
            return result;
        }

        private static CFIPClean83DecisionSnapshot Blocked(
            CFIPClean83BlockReason reason, string rule)
        {
            var blocks = new List<CFIPClean83BlockReason>();
            blocks.Add(reason);

            return new CFIPClean83DecisionSnapshot(
                CFIPClean83Direction.Wait,
                0, 0, 0, 0, 0, 0, "UNKNOWN", 0,
                false,
                CFIPClean83DecisionPolicyMode.Soft,
                blocks,
                CFIPClean83Provenance.Direct("DECISION_ENGINE", rule));
        }

        private static int Clamp(int value, int min, int max)
        {
            return Math.Max(min, Math.Min(max, value));
        }
    }


    // ------------------------------------------------------------------------
    // Phase 7 — canonical Entry / Trigger / Retest / Breakout engine
    // ------------------------------------------------------------------------

    public enum CFIPClean83EntryTriggerState
    {
        Blocked = 0,
        WaitingRetest = 1,
        WaitingBreakout = 2,
        TriggerReached = 3,
        Ready = 4,
        Invalidated = 5,
        Expired = 6
    }

    public sealed class CFIPClean83EntrySnapshot
    {
        private readonly ReadOnlyCollection<CFIPClean83BlockReason> _blockReasons;

        // This is the exact Phase-6 snapshot received by Entry.
        public CFIPClean83DecisionSnapshot Decision { get; private set; }

        public CFIPClean83Direction Direction { get; private set; }
        public CFIPClean83EntryMode Mode { get; private set; }
        public CFIPClean83EntryTriggerState State { get; private set; }
        public CFIPClean83EntryModel Model { get; private set; }
        public CFIPClean83PriceLevel RequestedEntry { get; private set; }

        public bool TriggerReached { get; private set; }
        public bool Eligible { get; private set; }

        public int EntryQuality { get; private set; }
        public DateTime ReferenceUtc { get; private set; }
        public DateTime CreatedUtc { get; private set; }
        public DateTime? ExpiresUtc { get; private set; }

        public IReadOnlyList<CFIPClean83BlockReason> BlockReasons
        {
            get { return _blockReasons; }
        }

        public CFIPClean83Provenance Provenance { get; private set; }

        public CFIPClean83EntrySnapshot(
            CFIPClean83DecisionSnapshot decision,
            CFIPClean83Direction direction,
            CFIPClean83EntryMode mode,
            CFIPClean83EntryTriggerState state,
            CFIPClean83EntryModel model,
            CFIPClean83PriceLevel requestedEntry,
            bool triggerReached,
            bool eligible,
            int entryQuality,
            DateTime referenceUtc,
            DateTime createdUtc,
            DateTime? expiresUtc,
            IList<CFIPClean83BlockReason> blockReasons,
            CFIPClean83Provenance provenance)
        {
            Decision =
                decision ??
                throw new ArgumentNullException("decision");

            if (direction != decision.Direction)
                throw new ArgumentException(
                    "Entry direction must equal the authoritative Decision direction.",
                    "direction");

            if (eligible &&
                (!decision.DecisionEligible ||
                 state != CFIPClean83EntryTriggerState.Ready ||
                 model == null ||
                 requestedEntry == null ||
                 direction == CFIPClean83Direction.Wait))
                throw new ArgumentException(
                    "Eligible Entry requires a directional ready Decision-backed model.",
                    "eligible");

            if (eligible &&
                blockReasons != null &&
                blockReasons.Count > 0)
                throw new ArgumentException(
                    "Eligible Entry cannot contain block reasons.",
                    "blockReasons");

            if (!eligible &&
                (blockReasons == null || blockReasons.Count == 0))
                throw new ArgumentException(
                    "Blocked Entry requires at least one block reason.",
                    "blockReasons");

            if (triggerReached &&
                model == null)
                throw new ArgumentException(
                    "A reached Trigger requires an EntryModel.",
                    "triggerReached");

            Direction = direction;
            Mode = mode;
            State = state;
            Model = model;
            RequestedEntry = requestedEntry;
            TriggerReached = triggerReached;
            Eligible = eligible;
            EntryQuality = Math.Max(0, Math.Min(100, entryQuality));
            ReferenceUtc = referenceUtc;
            CreatedUtc = createdUtc;
            ExpiresUtc = expiresUtc;

            _blockReasons =
                new ReadOnlyCollection<CFIPClean83BlockReason>(
                    new List<CFIPClean83BlockReason>(
                        blockReasons ??
                        new List<CFIPClean83BlockReason>()));

            Provenance =
                provenance ??
                CFIPClean83Provenance.Direct(
                    "UNKNOWN",
                    "UNSPECIFIED");
        }
    }

    public interface ICFIPClean83EntryTriggerEngine
    {
        CFIPClean83EntrySnapshot Evaluate(
            CFIPClean83DecisionSnapshot decision,
            CFIPClean83RuntimeSnapshot runtime,
            CFIPClean83MtfSnapshot mtf,
            CFIPClean83MarketModel market,
            CFIPClean83StructureSnapshot structure,
            CFIPClean83ConfigSnapshot configuration);
    }

    public sealed class CFIPClean83EntryTriggerEngine :
        ICFIPClean83EntryTriggerEngine
    {
        private sealed class ZoneCandidate
        {
            public CFIPClean83ZoneRecord Zone;
            public int Quality;
            public double Distance;
        }

        public CFIPClean83EntrySnapshot Evaluate(
            CFIPClean83DecisionSnapshot decision,
            CFIPClean83RuntimeSnapshot runtime,
            CFIPClean83MtfSnapshot mtf,
            CFIPClean83MarketModel market,
            CFIPClean83StructureSnapshot structure,
            CFIPClean83ConfigSnapshot configuration)
        {
            if (decision == null)
                throw new ArgumentNullException("decision");

            if (runtime == null ||
                mtf == null ||
                market == null ||
                structure == null ||
                configuration == null)
                return Blocked(
                    decision,
                    CFIPClean83BlockReason.DataIncomplete,
                    CFIPClean83EntryTriggerState.Blocked,
                    "MISSING_ENTRY_INPUT");

            // Final direction is NEVER recomputed here.
            CFIPClean83Direction direction = decision.Direction;

            if (!decision.DecisionEligible ||
                direction == CFIPClean83Direction.Wait)
                return Blocked(
                    decision,
                    FirstDecisionBlock(
                        decision.BlockReasons,
                        CFIPClean83BlockReason.PolicyBlocked),
                    CFIPClean83EntryTriggerState.Blocked,
                    "DECISION_NOT_ENTRY_ELIGIBLE");

            CFIPClean83MarketFrame m5 = market.FindFrame("M5");
            CFIPClean83MarketFrame m1 = market.FindFrame("M1");

            if (m5 == null ||
                !m5.DataValid ||
                m5.Atr <= 0)
                return Blocked(
                    decision,
                    CFIPClean83BlockReason.DataIncomplete,
                    CFIPClean83EntryTriggerState.Blocked,
                    "M5_ENTRY_FRAME_UNAVAILABLE");

            double executablePrice =
                direction == CFIPClean83Direction.Buy
                    ? runtime.Ask
                    : runtime.Bid;

            if (executablePrice <= 0)
                return Blocked(
                    decision,
                    CFIPClean83BlockReason.DataIncomplete,
                    CFIPClean83EntryTriggerState.Blocked,
                    "EXECUTABLE_PRICE_INVALID");

            var blocks = new List<CFIPClean83BlockReason>();

            if (configuration.Get("UseSpreadFilter", true) &&
                Math.Abs(runtime.Ask - runtime.Bid) >
                m5.Atr *
                Math.Max(
                    0,
                    configuration.Get(
                        "MaximumSpreadAtr",
                        0.20)))
                blocks.Add(CFIPClean83BlockReason.SpreadBlocked);

            if (configuration.Get("UseM5Confirmation", true) &&
                m5.BiasDirection != direction)
                blocks.Add(CFIPClean83BlockReason.MtfDisagreement);

            if (configuration.Get("UseM1Trigger", false) &&
                (m1 == null ||
                 !m1.DataValid ||
                 m1.BiasDirection != direction))
                blocks.Add(CFIPClean83BlockReason.MtfDisagreement);

            if (blocks.Count > 0)
                return Blocked(
                    decision,
                    FirstBlock(
                        blocks,
                        CFIPClean83BlockReason.EntryInvalid),
                    CFIPClean83EntryTriggerState.Blocked,
                    "ENTRY_PRECONDITIONS",
                    blocks);

            ZoneCandidate retest;
            DateTime? retestExpiry;

            if (TryFindRetestZone(
                direction,
                executablePrice,
                m5,
                structure,
                configuration,
                out retest,
                out retestExpiry))
            {
                if (configuration.Get("AvoidLateEntry", true) &&
                    retestExpiry.HasValue &&
                    runtime.ServerUtc > retestExpiry.Value)
                    return Blocked(
                        decision,
                        CFIPClean83BlockReason.EntryInvalid,
                        CFIPClean83EntryTriggerState.Expired,
                        "RETEST_SETUP_EXPIRED");

                double idealPrice =
                    ZoneMidpoint(retest.Zone);

                double invalidation =
                    direction == CFIPClean83Direction.Buy
                        ? retest.Zone.CurrentLower -
                          m5.Atr *
                          Math.Max(
                              0,
                              configuration.Get(
                                  "InvalidationZoneCloseAtr",
                                  0.10))
                        : retest.Zone.CurrentUpper +
                          m5.Atr *
                          Math.Max(
                              0,
                              configuration.Get(
                                  "InvalidationZoneCloseAtr",
                                  0.10));

                var idealEntry =
                    new CFIPClean83PriceLevel(
                        idealPrice,
                        "IDEAL_ENTRY",
                        CFIPClean83Provenance.Direct(
                            "ENTRY_ENGINE",
                            "RETEST_ZONE_MIDPOINT"));

                var zone =
                    new CFIPClean83PriceZone(
                        retest.Zone.CurrentLower,
                        retest.Zone.CurrentUpper,
                        retest.Zone.Kind.ToString(),
                        CFIPClean83Provenance.Direct(
                            "ENTRY_ENGINE",
                            "RETEST_ZONE"));

                var invalidationLevel =
                    new CFIPClean83PriceLevel(
                        invalidation,
                        "INVALIDATION",
                        CFIPClean83Provenance.Direct(
                            "ENTRY_ENGINE",
                            "RETEST_INVALIDATION"));

                if (configuration.Get(
                        "EnableSetupInvalidation",
                        true) &&
                    IsInvalidated(
                        direction,
                        executablePrice,
                        invalidation))
                    return new CFIPClean83EntrySnapshot(
                        decision,
                        direction,
                        CFIPClean83EntryMode.RetestMarket,
                        CFIPClean83EntryTriggerState.Invalidated,
                        new CFIPClean83EntryModel(
                            direction,
                            idealEntry,
                            zone,
                            null,
                            invalidationLevel),
                        null,
                        false,
                        false,
                        retest.Quality,
                        mtf.ReferenceUtc,
                        runtime.ServerUtc,
                        retestExpiry,
                        new List<CFIPClean83BlockReason>
                        {
                            CFIPClean83BlockReason.EntryInvalid
                        },
                        CFIPClean83Provenance.Direct(
                            "ENTRY_ENGINE",
                            "RETEST_INVALIDATED"));

                int minimumEntryQuality =
                    configuration.Get(
                        "MinimumEntryQuality",
                        64);

                if (configuration.Get("RequirePrecisionEntry", false) &&
                    retest.Quality < minimumEntryQuality)
                    return Blocked(
                        decision,
                        CFIPClean83BlockReason.EntryInvalid,
                        CFIPClean83EntryTriggerState.WaitingRetest,
                        "RETEST_PRECISION_QUALITY");

                if (!WithinMaximumEntryDistance(
                    executablePrice,
                    idealPrice,
                    m5.Atr,
                    configuration))
                    return Blocked(
                        decision,
                        CFIPClean83BlockReason.EntryInvalid,
                        CFIPClean83EntryTriggerState.WaitingRetest,
                        "RETEST_ENTRY_DISTANCE");

                var model =
                    new CFIPClean83EntryModel(
                        direction,
                        idealEntry,
                        zone,
                        null,
                        invalidationLevel);

                var requestedEntry =
                    new CFIPClean83PriceLevel(
                        executablePrice,
                        "REQUESTED_ENTRY",
                        CFIPClean83Provenance.Direct(
                            "ENTRY_ENGINE",
                            "RETEST_MARKET"));

                return new CFIPClean83EntrySnapshot(
                    decision,
                    direction,
                    CFIPClean83EntryMode.RetestMarket,
                    CFIPClean83EntryTriggerState.Ready,
                    model,
                    requestedEntry,
                    false,
                    true,
                    retest.Quality,
                    mtf.ReferenceUtc,
                    runtime.ServerUtc,
                    retestExpiry,
                    new List<CFIPClean83BlockReason>(),
                    CFIPClean83Provenance.Direct(
                        "ENTRY_ENGINE",
                        "RETEST_READY"));
            }

            if (!configuration.Get(
                    "AllowPrecisionBreakoutEntry",
                    true))
                return Blocked(
                    decision,
                    CFIPClean83BlockReason.EntryInvalid,
                    CFIPClean83EntryTriggerState.WaitingRetest,
                    "RETEST_REQUIRED");

            ZoneCandidate breakoutZone =
                FindBestExecutionZone(
                    direction,
                    executablePrice,
                    structure);

            CFIPClean83StructureEventRecord triggerEvent;

            if (breakoutZone == null ||
                !TryFindFreshBreakoutEvent(
                    direction,
                    mtf.ReferenceUtc,
                    m5,
                    structure,
                    configuration,
                    out triggerEvent))
                return Blocked(
                    decision,
                    CFIPClean83BlockReason.EntryInvalid,
                    CFIPClean83EntryTriggerState.WaitingBreakout,
                    "BREAKOUT_TRIGGER_UNAVAILABLE");

            double bufferAtr =
                Math.Max(
                    0,
                    configuration.Get(
                        "PrecisionBreakoutBufferAtr",
                        configuration.Get(
                            "EntryBufferAtr",
                            0.05)));

            double breakoutAnchor =
                direction == CFIPClean83Direction.Buy
                    ? Math.Max(
                        triggerEvent.Price,
                        breakoutZone.Zone.CurrentUpper)
                    : Math.Min(
                        triggerEvent.Price,
                        breakoutZone.Zone.CurrentLower);

            double triggerPrice =
                direction == CFIPClean83Direction.Buy
                    ? breakoutAnchor + m5.Atr * bufferAtr
                    : breakoutAnchor - m5.Atr * bufferAtr;

            double triggerTolerance =
                m5.Atr *
                Math.Max(
                    0,
                    configuration.Get(
                        "EntryBufferAtr",
                        0.05));

            bool triggerReached =
                direction == CFIPClean83Direction.Buy
                    ? executablePrice >= triggerPrice - triggerTolerance
                    : executablePrice <= triggerPrice + triggerTolerance;

            double invalidation =
                direction == CFIPClean83Direction.Buy
                    ? Math.Min(
                        breakoutZone.Zone.CurrentLower,
                        triggerEvent.Price) -
                      m5.Atr *
                      Math.Max(
                          0,
                          configuration.Get(
                              "InvalidationStructureAtr",
                              0.10))
                    : Math.Max(
                        breakoutZone.Zone.CurrentUpper,
                        triggerEvent.Price) +
                      m5.Atr *
                      Math.Max(
                          0,
                          configuration.Get(
                              "InvalidationStructureAtr",
                              0.10));

            var breakoutModel =
                new CFIPClean83EntryModel(
                    direction,
                    new CFIPClean83PriceLevel(
                        ZoneMidpoint(breakoutZone.Zone),
                        "IDEAL_ENTRY",
                        CFIPClean83Provenance.Direct(
                            "ENTRY_ENGINE",
                            "BREAKOUT_ZONE_MIDPOINT")),
                    new CFIPClean83PriceZone(
                        breakoutZone.Zone.CurrentLower,
                        breakoutZone.Zone.CurrentUpper,
                        breakoutZone.Zone.Kind.ToString(),
                        CFIPClean83Provenance.Direct(
                            "ENTRY_ENGINE",
                            "BREAKOUT_ZONE")),
                    new CFIPClean83PriceLevel(
                        triggerPrice,
                        "TRIGGER",
                        CFIPClean83Provenance.Direct(
                            "ENTRY_ENGINE",
                            "BREAKOUT_STRUCTURAL_EVENT")),
                    new CFIPClean83PriceLevel(
                        invalidation,
                        "INVALIDATION",
                        CFIPClean83Provenance.Direct(
                            "ENTRY_ENGINE",
                            "BREAKOUT_INVALIDATION")));

            DateTime expiry =
                ExpiryFor(
                    triggerEvent.TimeUtc,
                    m5,
                    configuration);

            if (configuration.Get("RequirePrecisionEntry", false) &&
                breakoutZone.Quality <
                configuration.Get(
                    "MinimumEntryQuality",
                    64))
                return new CFIPClean83EntrySnapshot(
                    decision,
                    direction,
                    CFIPClean83EntryMode.BreakoutMarket,
                    CFIPClean83EntryTriggerState.WaitingBreakout,
                    breakoutModel,
                    null,
                    false,
                    false,
                    breakoutZone.Quality,
                    mtf.ReferenceUtc,
                    runtime.ServerUtc,
                    expiry,
                    new List<CFIPClean83BlockReason>
                    {
                        CFIPClean83BlockReason.EntryInvalid
                    },
                    CFIPClean83Provenance.Direct(
                        "ENTRY_ENGINE",
                        "BREAKOUT_PRECISION_QUALITY"));

            if (configuration.Get("AvoidLateEntry", true) &&
                runtime.ServerUtc > expiry)
                return new CFIPClean83EntrySnapshot(
                    decision,
                    direction,
                    CFIPClean83EntryMode.BreakoutMarket,
                    CFIPClean83EntryTriggerState.Expired,
                    breakoutModel,
                    null,
                    false,
                    false,
                    breakoutZone.Quality,
                    mtf.ReferenceUtc,
                    runtime.ServerUtc,
                    expiry,
                    new List<CFIPClean83BlockReason>
                    {
                        CFIPClean83BlockReason.EntryInvalid
                    },
                    CFIPClean83Provenance.Direct(
                        "ENTRY_ENGINE",
                        "BREAKOUT_SETUP_EXPIRED"));

            if (configuration.Get("EnableSetupInvalidation", true) &&
                IsInvalidated(
                    direction,
                    executablePrice,
                    invalidation))
                return new CFIPClean83EntrySnapshot(
                    decision,
                    direction,
                    CFIPClean83EntryMode.BreakoutMarket,
                    CFIPClean83EntryTriggerState.Invalidated,
                    breakoutModel,
                    null,
                    false,
                    false,
                    breakoutZone.Quality,
                    mtf.ReferenceUtc,
                    runtime.ServerUtc,
                    expiry,
                    new List<CFIPClean83BlockReason>
                    {
                        CFIPClean83BlockReason.EntryInvalid
                    },
                    CFIPClean83Provenance.Direct(
                        "ENTRY_ENGINE",
                        "BREAKOUT_INVALIDATED"));

            if (!triggerReached)
            {
                CFIPClean83EntryMode waitingMode =
                    ResolvePendingEntryMode(
                        triggerEvent.Kind,
                        configuration);

                return new CFIPClean83EntrySnapshot(
                    decision,
                    direction,
                    waitingMode,
                    CFIPClean83EntryTriggerState.WaitingBreakout,
                    breakoutModel,
                    null,
                    false,
                    false,
                    breakoutZone.Quality,
                    mtf.ReferenceUtc,
                    runtime.ServerUtc,
                    expiry,
                    new List<CFIPClean83BlockReason>
                    {
                        CFIPClean83BlockReason.EntryInvalid
                    },
                    CFIPClean83Provenance.Direct(
                        "ENTRY_ENGINE",
                        "TRIGGER_WAIT"));
            }

            if (!WithinMaximumTriggerDistance(
                executablePrice,
                triggerPrice,
                m5.Atr,
                configuration))
                return new CFIPClean83EntrySnapshot(
                    decision,
                    direction,
                    CFIPClean83EntryMode.BreakoutMarket,
                    CFIPClean83EntryTriggerState.WaitingBreakout,
                    breakoutModel,
                    null,
                    true,
                    false,
                    breakoutZone.Quality,
                    mtf.ReferenceUtc,
                    runtime.ServerUtc,
                    expiry,
                    new List<CFIPClean83BlockReason>
                    {
                        CFIPClean83BlockReason.EntryInvalid
                    },
                    CFIPClean83Provenance.Direct(
                        "ENTRY_ENGINE",
                        "LATE_BREAKOUT_ENTRY"));

            var requested =
                new CFIPClean83PriceLevel(
                    executablePrice,
                    "REQUESTED_ENTRY",
                    CFIPClean83Provenance.Direct(
                        "ENTRY_ENGINE",
                        "BREAKOUT_MARKET_AFTER_TRIGGER"));

            return new CFIPClean83EntrySnapshot(
                decision,
                direction,
                CFIPClean83EntryMode.BreakoutMarket,
                CFIPClean83EntryTriggerState.Ready,
                breakoutModel,
                requested,
                true,
                true,
                breakoutZone.Quality,
                mtf.ReferenceUtc,
                runtime.ServerUtc,
                expiry,
                new List<CFIPClean83BlockReason>(),
                CFIPClean83Provenance.Direct(
                    "ENTRY_ENGINE",
                    "BREAKOUT_TRIGGER_REACHED"));
        }

        private bool TryFindRetestZone(
            CFIPClean83Direction direction,
            double executablePrice,
            CFIPClean83MarketFrame m5,
            CFIPClean83StructureSnapshot structure,
            CFIPClean83ConfigSnapshot cfg,
            out ZoneCandidate candidate,
            out DateTime? expiresUtc)
        {
            candidate = null;
            expiresUtc = null;

            double tolerance =
                m5.Atr *
                Math.Max(
                    0,
                    cfg.Get(
                        "RetestZoneToleranceAtr",
                        0.12));

            int minimumQuality =
                cfg.Get(
                    "MinimumRetestQuality",
                    cfg.Get(
                        "MinimumEntryQuality",
                        64));

            for (int i = 0; i < structure.Zones.Count; i++)
            {
                CFIPClean83ZoneRecord zone =
                    structure.Zones[i];

                if (!IsUsableZone(zone, direction))
                    continue;

                if (m5.ClosedBarTimeUtc < zone.CreatedUtc)
                    continue;

                if (zone.AgeBars >
                    Math.Max(
                        1,
                        cfg.Get(
                            "RetestMaxBarsAfterDisplacement",
                            12)))
                    continue;

                double lower =
                    zone.CurrentLower - tolerance;
                double upper =
                    zone.CurrentUpper + tolerance;

                bool priceInsideExpandedZone =
                    executablePrice >= lower &&
                    executablePrice <= upper;

                if (zone.Kind ==
                        CFIPClean83ZoneKind.FairValueGap &&
                    cfg.Get(
                        "RequireFvgRetest",
                        false) &&
                    !zone.Contains(executablePrice))
                    continue;

                if (zone.Kind ==
                        CFIPClean83ZoneKind.OrderBlock &&
                    cfg.Get(
                        "RequireObRetest",
                        false) &&
                    !zone.Contains(executablePrice))
                    continue;

                bool touched =
                    priceInsideExpandedZone ||
                    (
                        direction == CFIPClean83Direction.Buy
                            ? m5.Low <= upper
                            : m5.High >= lower);

                if (!touched)
                    continue;

                bool closeConfirmed =
                    direction == CFIPClean83Direction.Buy
                        ? m5.Close >= ZoneMidpoint(zone)
                        : m5.Close <= ZoneMidpoint(zone);

                if (cfg.Get(
                        "RequireRetestCloseConfirmation",
                        true) &&
                    (!touched || !closeConfirmed))
                    continue;

                double bodyAtr =
                    Math.Abs(m5.Close - m5.Open) /
                    m5.Atr;

                if (bodyAtr <
                    Math.Max(
                        0,
                        cfg.Get(
                            "RetestRejectionBodyAtr",
                            0.15)))
                    continue;

                int quality = zone.Quality;

                if (zone.LiquidityConfluence)
                    quality += 6;

                if (zone.FvgConfluence)
                    quality += 6;

                if (zone.Retested ||
                    zone.Lifecycle ==
                    CFIPClean83ZoneLifecycle.Retested ||
                    zone.Lifecycle ==
                    CFIPClean83ZoneLifecycle.PartiallyMitigated)
                    quality += 6;

                quality =
                    Math.Max(
                        0,
                        Math.Min(
                            100,
                            quality));

                if (cfg.Get(
                        "RequireRetestQuality",
                        true) &&
                    quality < minimumQuality)
                    continue;

                double distance =
                    Math.Abs(
                        executablePrice -
                        ZoneMidpoint(zone));

                if (candidate == null ||
                    quality > candidate.Quality ||
                    (quality == candidate.Quality &&
                     distance < candidate.Distance))
                {
                    candidate =
                        new ZoneCandidate
                        {
                            Zone = zone,
                            Quality = quality,
                            Distance = distance
                        };

                    expiresUtc =
                        zone.CreatedUtc +
                        TimeSpan.FromMinutes(
                            5 *
                            Math.Max(
                                1,
                                cfg.Get(
                                    "RetestMaxBarsAfterDisplacement",
                                    12)));
                }
            }

            return candidate != null;
        }

        private ZoneCandidate FindBestExecutionZone(
            CFIPClean83Direction direction,
            double price,
            CFIPClean83StructureSnapshot structure)
        {
            ZoneCandidate best = null;

            for (int i = 0; i < structure.Zones.Count; i++)
            {
                CFIPClean83ZoneRecord zone =
                    structure.Zones[i];

                if (!IsUsableZone(zone, direction))
                    continue;

                double distance =
                    price <= zone.CurrentLower
                        ? zone.CurrentLower - price
                        : price >= zone.CurrentUpper
                            ? price - zone.CurrentUpper
                            : 0;

                var candidate =
                    new ZoneCandidate
                    {
                        Zone = zone,
                        Quality = zone.Quality,
                        Distance = distance
                    };

                if (best == null ||
                    candidate.Quality > best.Quality ||
                    (candidate.Quality == best.Quality &&
                     candidate.Distance < best.Distance))
                    best = candidate;
            }

            return best;
        }

        private bool TryFindFreshBreakoutEvent(
            CFIPClean83Direction direction,
            DateTime referenceUtc,
            CFIPClean83MarketFrame m5,
            CFIPClean83StructureSnapshot structure,
            CFIPClean83ConfigSnapshot cfg,
            out CFIPClean83StructureEventRecord latest)
        {
            latest = null;

            DateTime cutoff =
                referenceUtc -
                TimeSpan.FromMinutes(
                    5 *
                    Math.Max(
                        1,
                        cfg.Get(
                            "RetestLookbackBars",
                            8)));

            bool structuralBreakPresent = false;
            bool displacementPresent = false;
            bool liquiditySweepPresent = false;

            for (int i = 0; i < structure.Events.Count; i++)
            {
                CFIPClean83StructureEventRecord item =
                    structure.Events[i];

                if (item.Direction != direction ||
                    item.TimeUtc < cutoff ||
                    item.TimeUtc > referenceUtc ||
                    !string.Equals(
                        item.Timeframe,
                        "M5",
                        StringComparison.OrdinalIgnoreCase))
                    continue;

                if (IsTriggerEvent(item.Kind))
                {
                    if (latest == null ||
                        item.TimeUtc > latest.TimeUtc)
                        latest = item;
                }

                if (item.Kind ==
                        CFIPClean83StructureEventKind.BreakOfStructure ||
                    item.Kind ==
                        CFIPClean83StructureEventKind.MarketStructureShift ||
                    item.Kind ==
                        CFIPClean83StructureEventKind.ChangeOfCharacter)
                    structuralBreakPresent = true;

                if (item.Kind ==
                        CFIPClean83StructureEventKind.Displacement)
                    displacementPresent = true;

                if (item.Kind ==
                        CFIPClean83StructureEventKind.LiquiditySweep)
                    liquiditySweepPresent = true;
            }

            if (latest == null)
                return false;

            if (!cfg.Get("RequireFreshM5Trigger", true))
                return true;

            int triggerEvidence = 0;

            // BOS/MSS/CHOCH are one structural-break family.
            if (structuralBreakPresent)
                triggerEvidence += 2;

            if (displacementPresent)
                triggerEvidence += 1;

            if (liquiditySweepPresent)
                triggerEvidence += 1;

            double candleRange =
                Math.Max(0, m5.High - m5.Low);

            double candleBody =
                Math.Abs(m5.Close - m5.Open);

            double bodyAtr =
                candleRange <= 0
                    ? 0
                    : candleBody / Math.Max(m5.Atr, 1e-12);

            double rangeAtr =
                candleRange <= 0
                    ? 0
                    : candleRange / Math.Max(m5.Atr, 1e-12);

            double closeLocation =
                candleRange <= 0
                    ? 0.5
                    : direction == CFIPClean83Direction.Buy
                        ? (m5.Close - m5.Low) / candleRange
                        : (m5.High - m5.Close) / candleRange;

            if (bodyAtr >=
                Math.Max(
                    0,
                    cfg.Get(
                        "MinimumTriggerBodyAtr",
                        0.12)))
                triggerEvidence += 1;

            if (closeLocation >=
                Math.Max(
                    0.50,
                    Math.Min(
                        0.95,
                        cfg.Get(
                            "MinimumCloseLocation",
                            0.65))))
                triggerEvidence += 1;

            double maximumRange =
                Math.Max(
                    0,
                    cfg.Get(
                        "MaximumTriggerRangeAtr",
                        2.5));

            if (maximumRange > 0 &&
                rangeAtr > maximumRange)
                return false;

            int required =
                Math.Max(
                    1,
                    cfg.Get(
                        "MinimumFreshTriggerEvidence",
                        3));

            return triggerEvidence >= required;
        }

        private double ZoneMidpoint(
            CFIPClean83ZoneRecord zone)
        {
            if (zone == null)
                return 0;

            return
                (zone.CurrentLower + zone.CurrentUpper) /
                2.0;
        }

        private bool IsTriggerEvent(
            CFIPClean83StructureEventKind kind)
        {
            return
                kind ==
                    CFIPClean83StructureEventKind.BreakOfStructure ||
                kind ==
                    CFIPClean83StructureEventKind.MarketStructureShift ||
                kind ==
                    CFIPClean83StructureEventKind.ChangeOfCharacter ||
                kind ==
                    CFIPClean83StructureEventKind.Displacement;
        }

        private bool IsUsableZone(
            CFIPClean83ZoneRecord zone,
            CFIPClean83Direction direction)
        {
            if (zone == null ||
                zone.Direction != direction ||
                !zone.ExecutionEligible ||
                zone.Consumed ||
                zone.Invalidated ||
                zone.Lifecycle ==
                    CFIPClean83ZoneLifecycle.Invalidated ||
                zone.Lifecycle ==
                    CFIPClean83ZoneLifecycle.Expired)
                return false;

            return
                zone.CurrentLower > 0 &&
                zone.CurrentUpper >= zone.CurrentLower;
        }

        private CFIPClean83EntryMode ResolvePendingEntryMode(
            CFIPClean83StructureEventKind kind,
            CFIPClean83ConfigSnapshot cfg)
        {
            if (kind ==
                    CFIPClean83StructureEventKind.MarketStructureShift ||
                kind ==
                    CFIPClean83StructureEventKind.ChangeOfCharacter)
                return CFIPClean83EntryMode.ReversalLimit;

            CFIPClean83PendingOrderMode pendingMode =
                cfg.Get(
                    "PendingOrderMode",
                    CFIPClean83PendingOrderMode.Adaptive);

            if (pendingMode ==
                    CFIPClean83PendingOrderMode.ContinuationStop ||
                pendingMode ==
                    CFIPClean83PendingOrderMode.Both ||
                pendingMode ==
                    CFIPClean83PendingOrderMode.Adaptive)
                return CFIPClean83EntryMode.ContinuationStop;

            return CFIPClean83EntryMode.BreakoutMarket;
        }

        private bool WithinMaximumEntryDistance(
            double executablePrice,
            double idealPrice,
            double atr,
            CFIPClean83ConfigSnapshot cfg)
        {
            if (atr <= 0)
                return false;

            double max =
                Math.Max(
                    0,
                    cfg.Get(
                        "MaximumEntryDistanceAtr",
                        0.60));

            if (max <= 0)
                return true;

            return
                Math.Abs(executablePrice - idealPrice) /
                atr <= max;
        }

        private bool WithinMaximumTriggerDistance(
            double executablePrice,
            double triggerPrice,
            double atr,
            CFIPClean83ConfigSnapshot cfg)
        {
            if (atr <= 0)
                return false;

            double max =
                Math.Max(
                    0,
                    Math.Min(
                        cfg.Get(
                            "MaximumEntryExtensionAtr",
                            0.60),
                        cfg.Get(
                            "MaximumEntryDistanceAtr",
                            0.60)));

            return
                max <= 0 ||
                Math.Abs(executablePrice - triggerPrice) /
                atr <= max;
        }

        private bool IsInvalidated(
            CFIPClean83Direction direction,
            double price,
            double invalidation)
        {
            return direction == CFIPClean83Direction.Buy
                ? price <= invalidation
                : price >= invalidation;
        }

        private DateTime ExpiryFor(
            DateTime sourceUtc,
            CFIPClean83MarketFrame m5,
            CFIPClean83ConfigSnapshot cfg)
        {
            return
                sourceUtc +
                TimeSpan.FromMinutes(
                    5 *
                    Math.Max(
                        1,
                        cfg.Get(
                            "RetestLookbackBars",
                            8)));
        }

        private CFIPClean83EntrySnapshot Blocked(
            CFIPClean83DecisionSnapshot decision,
            CFIPClean83BlockReason reason,
            CFIPClean83EntryTriggerState state,
            string rule)
        {
            return Blocked(
                decision,
                reason,
                state,
                rule,
                null);
        }

        private CFIPClean83EntrySnapshot Blocked(
            CFIPClean83DecisionSnapshot decision,
            CFIPClean83BlockReason primaryReason,
            CFIPClean83EntryTriggerState state,
            string rule,
            IList<CFIPClean83BlockReason> additional)
        {
            var blocks =
                new List<CFIPClean83BlockReason>();

            if (additional != null)
            {
                for (int i = 0; i < additional.Count; i++)
                    if (!blocks.Contains(additional[i]))
                        blocks.Add(additional[i]);
            }

            if (!blocks.Contains(primaryReason))
                blocks.Add(primaryReason);

            return new CFIPClean83EntrySnapshot(
                decision,
                decision.Direction,
                CFIPClean83EntryMode.None,
                state,
                null,
                null,
                false,
                false,
                0,
                DateTime.MinValue,
                DateTime.MinValue,
                null,
                blocks,
                CFIPClean83Provenance.Direct(
                    "ENTRY_ENGINE",
                    rule));
        }

        private CFIPClean83BlockReason FirstDecisionBlock(
            IReadOnlyList<CFIPClean83BlockReason> blocks,
            CFIPClean83BlockReason fallback)
        {
            if (blocks != null)
            {
                for (int i = 0; i < blocks.Count; i++)
                    if (blocks[i] !=
                        CFIPClean83BlockReason.None)
                        return blocks[i];
            }

            return fallback;
        }

        private CFIPClean83BlockReason FirstBlock(
            IList<CFIPClean83BlockReason> blocks,
            CFIPClean83BlockReason fallback)
        {
            if (blocks != null)
            {
                for (int i = 0; i < blocks.Count; i++)
                    if (blocks[i] !=
                        CFIPClean83BlockReason.None)
                        return blocks[i];
            }

            return fallback;
        }
    }

    // ------------------------------------------------------------------------
    // Trade identity / idempotency
    // ------------------------------------------------------------------------

    public sealed class CFIPClean83TradeIdentity
    {
        public string StrategyId { get; private set; }
        public string Symbol { get; private set; }
        public string PlanId { get; private set; }
        public string SignalId { get; private set; }

        public CFIPClean83TradeIdentity(
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

    public sealed class CFIPClean83ExecutionIdentity
    {
        public string IntentId { get; private set; }
        public string IdempotencyKey { get; private set; }
        public DateTime CreatedUtc { get; private set; }

        public CFIPClean83ExecutionIdentity(
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

    public sealed class CFIPClean83TradePlan
    {
        public CFIPClean83TradeIdentity Identity { get; private set; }
        public CFIPClean83Direction Direction { get; private set; }
        public CFIPClean83EntryMode EntryMode { get; private set; }
        public CFIPClean83EntryModel Entry { get; private set; }
        public CFIPClean83PriceLevel ExecutionAnchor { get; private set; }
        public CFIPClean83PriceLevel StructuralStop { get; private set; }
        public CFIPClean83TargetLadder TargetLadder { get; private set; }
        public double RiskRewardToTp1 { get; private set; }
        public double RiskRewardToFinalTarget { get; private set; }
        public int LevelQuality { get; private set; }
        public bool IsValid { get; private set; }
        public DateTime CreatedUtc { get; private set; }
        public int ReferenceBarIndex { get; private set; }
        public CFIPClean83Provenance Provenance { get; private set; }

        public CFIPClean83TradePlan(
            CFIPClean83TradeIdentity identity,
            CFIPClean83Direction direction,
            CFIPClean83EntryMode entryMode,
            CFIPClean83EntryModel entry,
            CFIPClean83PriceLevel executionAnchor,
            CFIPClean83PriceLevel structuralStop,
            CFIPClean83TargetLadder targetLadder,
            double riskRewardToTp1,
            double riskRewardToFinalTarget,
            int levelQuality,
            bool isValid,
            DateTime createdUtc,
            int referenceBarIndex,
            CFIPClean83Provenance provenance)
        {
            Identity =
                identity ??
                throw new ArgumentNullException("identity");
            Direction = direction;
            EntryMode = entryMode;
            Entry =
                entry ??
                throw new ArgumentNullException("entry");
            ExecutionAnchor =
                executionAnchor ??
                throw new ArgumentNullException("executionAnchor");
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

            if (IsValid)
            {
                if (!CFIPClean83DirectionRules.IsDirectional(Direction))
                    throw new ArgumentException(
                        "A valid TradePlan must be directional.",
                        "direction");

                if (Entry.Direction != Direction)
                    throw new ArgumentException(
                        "TradePlan direction must match Entry direction.",
                        "entry");

                if (!CFIPClean83DirectionRules.IsProtectivePrice(
                    Direction,
                    ExecutionAnchor.Price,
                    StructuralStop.Price))
                    throw new ArgumentException(
                        "TradePlan structural stop must protect the selected direction.",
                        "structuralStop");

                if (!TargetLadder.ValidateForDirection(Direction) ||
                    TargetLadder.Find(CFIPClean83TargetStage.TP1) == null ||
                    RiskRewardToTp1 <= 0)
                    throw new ArgumentException(
                        "A valid TradePlan requires an ordered TP ladder with TP1 and positive RR.",
                        "targetLadder");
            }

            CreatedUtc = createdUtc;
            ReferenceBarIndex = referenceBarIndex;
            Provenance =
                provenance ??
                CFIPClean83Provenance.Direct(
                    "UNKNOWN",
                    "UNSPECIFIED");
        }
    }

    
// ------------------------------------------------------------------------
// Phase 8 — authoritative Risk / Stop / Target engine
// ------------------------------------------------------------------------

public sealed class CFIPClean83TradePlanBuilder :
    ICFIPClean83TradePlanBuilder
{
    private sealed class StopCandidate
    {
        public double Price;
        public int Quality;
        public double Distance;
        public bool Structural;
        public CFIPClean83Provenance Provenance;
    }

    private sealed class TargetCandidate
    {
        public double Price;
        public int Quality;
        public bool Htf;
        public CFIPClean83Provenance Provenance;
    }

    public CFIPClean83TradePlan Build(
        CFIPClean83DecisionSnapshot decision,
        CFIPClean83EntrySnapshot entry,
        CFIPClean83MarketModel market,
        CFIPClean83StructureSnapshot structure,
        CFIPClean83MtfSnapshot mtf,
        CFIPClean83RuntimeSnapshot runtime,
        CFIPClean83ConfigSnapshot configuration)
    {
        if (decision == null)
            throw new ArgumentNullException("decision");
        if (entry == null)
            throw new ArgumentNullException("entry");
        if (market == null || structure == null || mtf == null ||
            runtime == null || configuration == null)
            return InvalidPlan(
                decision,
                entry,
                mtf,
                runtime,
                "PHASE8_INPUT_INCOMPLETE");

        if (!decision.DecisionEligible ||
            entry.Model == null ||
            !CFIPClean83DirectionRules.IsDirectional(decision.Direction))
            return InvalidPlan(
                decision,
                entry,
                mtf,
                runtime,
                "DECISION_OR_ENTRY_MODEL_INVALID");

        bool marketEntryReady =
            entry.Eligible &&
            entry.State == CFIPClean83EntryTriggerState.Ready &&
            (entry.Mode == CFIPClean83EntryMode.RetestMarket ||
             entry.Mode == CFIPClean83EntryMode.BreakoutMarket);

        bool pendingProposal =
            !entry.Eligible &&
            entry.State == CFIPClean83EntryTriggerState.WaitingBreakout &&
            (entry.Mode == CFIPClean83EntryMode.ContinuationStop ||
             entry.Mode == CFIPClean83EntryMode.ReversalLimit) &&
            (entry.Mode == CFIPClean83EntryMode.ReversalLimit ||
             entry.Model.Trigger != null);

        if (!marketEntryReady && !pendingProposal)
            return InvalidPlan(
                decision,
                entry,
                mtf,
                runtime,
                "ENTRY_NOT_PLAN_READY");

        if (entry.Decision != decision ||
            entry.Direction != decision.Direction)
            return InvalidPlan(
                decision,
                entry,
                mtf,
                runtime,
                "DECISION_SNAPSHOT_MISMATCH");

        CFIPClean83MarketFrame m5 = market.FindFrame("M5");
        if (m5 == null || !m5.DataValid || m5.Atr <= 0)
            return InvalidPlan(
                decision,
                entry,
                mtf,
                runtime,
                "M5_RISK_FRAME_UNAVAILABLE");

        double entryPrice = ResolvePlanEntryPrice(entry);

        if (entryPrice <= 0)
            return InvalidPlan(
                decision,
                entry,
                mtf,
                runtime,
                "ENTRY_PRICE_INVALID");

        double atr = m5.Atr;
        double minimumRiskAtr =
            Math.Max(
                0.1,
                configuration.Get(
                    "MinimumSlAtr",
                    0.55));
        double maximumRiskAtr =
            Math.Max(
                minimumRiskAtr,
                configuration.Get(
                    "MaximumSlAtr",
                    1.80));
        double stopBufferAtr =
            Math.Max(
                0.0,
                configuration.Get(
                    "StopBufferAtr",
                    0.10));
        bool requireStructuralStop =
            configuration.Get(
                "RequireStructuralStop",
                true);
        bool useHtfStop =
            configuration.Get(
                "UseHtfStructureForStop",
                true);

        StopCandidate stop =
            FindStructuralStop(
                decision.Direction,
                entryPrice,
                atr,
                entry,
                structure,
                minimumRiskAtr,
                maximumRiskAtr,
                stopBufferAtr,
                useHtfStop);

        bool structuralStop =
            stop != null && stop.Structural;

        if (stop == null ||
            !IsProtectiveStop(
                decision.Direction,
                entryPrice,
                stop.Price))
        {
            if (requireStructuralStop)
                return InvalidPlan(
                    decision,
                    entry,
                    mtf,
                    runtime,
                    "STRUCTURAL_STOP_UNAVAILABLE");

            double fallbackAtr =
                Math.Max(
                    minimumRiskAtr,
                    configuration.Get(
                        "FallbackSlAtr",
                        1.00));

            double fallbackPrice =
                decision.Direction == CFIPClean83Direction.Buy
                    ? entryPrice - fallbackAtr * atr
                    : entryPrice + fallbackAtr * atr;

            stop = new StopCandidate
            {
                Price = fallbackPrice,
                Quality = 50,
                Distance = Math.Abs(entryPrice - fallbackPrice),
                Structural = false,
                Provenance =
                    CFIPClean83Provenance.FallbackFrom(
                        "RISK",
                        "ATR_STOP",
                        CFIPClean83FallbackKind.Atr,
                        "No valid structural stop was available.")
            };
            structuralStop = false;
        }

        double riskDistance =
            Math.Abs(entryPrice - stop.Price);

        if (riskDistance < minimumRiskAtr * atr)
        {
            double minimumDistance = minimumRiskAtr * atr;
            stop.Price =
                decision.Direction == CFIPClean83Direction.Buy
                    ? entryPrice - minimumDistance
                    : entryPrice + minimumDistance;
            stop.Distance = minimumDistance;
            stop.Provenance =
                stop.Structural
                    ? CFIPClean83Provenance.Direct(
                        "RISK",
                        "STRUCTURAL_STOP_EXPANDED_TO_MINIMUM_RISK")
                    : stop.Provenance;
            riskDistance = minimumDistance;
        }

        if (riskDistance > maximumRiskAtr * atr)
        {
            if (requireStructuralStop && structuralStop)
                return InvalidPlan(
                    decision,
                    entry,
                    mtf,
                    runtime,
                    "STRUCTURAL_STOP_EXCEEDS_MAX_RISK");

            double fallbackAtr =
                Math.Max(
                    minimumRiskAtr,
                    configuration.Get(
                        "FallbackSlAtr",
                        1.00));
            double boundedAtr =
                Math.Min(
                    maximumRiskAtr,
                    Math.Max(
                        minimumRiskAtr,
                        fallbackAtr));

            double fallbackPrice =
                decision.Direction == CFIPClean83Direction.Buy
                    ? entryPrice - boundedAtr * atr
                    : entryPrice + boundedAtr * atr;

            stop = new StopCandidate
            {
                Price = fallbackPrice,
                Quality = 50,
                Distance = Math.Abs(entryPrice - fallbackPrice),
                Structural = false,
                Provenance =
                    CFIPClean83Provenance.FallbackFrom(
                        "RISK",
                        "ATR_STOP_RISK_CAP",
                        CFIPClean83FallbackKind.Atr,
                        "Structural stop exceeded configured maximum risk.")
            };
            structuralStop = false;
            riskDistance = stop.Distance;
        }

        var ladder =
            BuildTargetLadder(
                decision,
                entryPrice,
                stop.Price,
                atr,
                structure,
                configuration);

        if (ladder == null ||
            ladder.Levels.Count == 0 ||
            !ladder.ValidateForDirection(decision.Direction))
            return InvalidPlan(
                decision,
                entry,
                mtf,
                runtime,
                "TARGET_LADDER_INVALID");

        if (configuration.Get("RequireHtfTargets", false) &&
            !HasHtfTarget(ladder))
            return InvalidPlan(
                decision,
                entry,
                mtf,
                runtime,
                "HTF_TARGET_REQUIRED");

        double tp1 =
            ladder.Find(CFIPClean83TargetStage.TP1).Level.Price;
        CFIPClean83TargetLevel finalTarget =
            ladder.Levels[ladder.Levels.Count - 1];

        double rr1 =
            riskDistance > 0
                ? Math.Abs(tp1 - entryPrice) / riskDistance
                : 0;
        double rrFinal =
            riskDistance > 0
                ? Math.Abs(finalTarget.Level.Price - entryPrice) /
                  riskDistance
                : 0;

        int levelQuality =
            Math.Max(
                0,
                Math.Min(
                    100,
                    (stop.Quality + finalTarget.Quality) / 2));

        var identity =
            new CFIPClean83TradeIdentity(
                configuration.StrategyId,
                runtime.Symbol,
                BuildPlanId(
                    mtf.ReferenceUtc,
                    decision.Direction,
                    entryPrice,
                    stop.Price),
                BuildSignalId(
                    mtf.ReferenceUtc,
                    decision.Direction));

        return new CFIPClean83TradePlan(
            identity,
            decision.Direction,
            entry.Mode,
            entry.Model,
            new CFIPClean83PriceLevel(
                entryPrice,
                "EXECUTION_ANCHOR",
                CFIPClean83Provenance.Direct(
                    "PHASE8",
                    "PLAN_EXECUTION_ANCHOR")),
            new CFIPClean83PriceLevel(
                stop.Price,
                structuralStop
                    ? "StructuralStop"
                    : "FallbackStop",
                stop.Provenance),
            ladder,
            rr1,
            rrFinal,
            levelQuality,
            rr1 > 0,
            runtime.ServerUtc,
            mtf.M5 != null ? mtf.M5.ClosedIndex : -1,
            CFIPClean83Provenance.Direct(
                "PHASE8",
                structuralStop
                    ? "STRUCTURAL_RISK_TARGET_PLAN"
                    : "EXPLICIT_STOP_FALLBACK_TARGET_PLAN"));
    }

    private double ResolvePlanEntryPrice(
        CFIPClean83EntrySnapshot entry)
    {
        if (entry == null || entry.Model == null)
            return 0;

        if (entry.Mode == CFIPClean83EntryMode.ContinuationStop &&
            entry.Model.Trigger != null)
            return entry.Model.Trigger.Price;

        if (entry.Mode == CFIPClean83EntryMode.ReversalLimit)
            return entry.Model.IdealEntry.Price;

        if (entry.RequestedEntry != null)
            return entry.RequestedEntry.Price;

        return entry.Model.IdealEntry.Price;
    }

    private StopCandidate FindStructuralStop(
        CFIPClean83Direction direction,
        double entryPrice,
        double atr,
        CFIPClean83EntrySnapshot entry,
        CFIPClean83StructureSnapshot structure,
        double minimumRiskAtr,
        double maximumRiskAtr,
        double stopBufferAtr,
        bool useHtfStop)
    {
        var candidates = new List<StopCandidate>();

        if (entry.Model != null &&
            entry.Model.Invalidation != null)
        {
            double p = entry.Model.Invalidation.Price;
            if (IsProtectiveStop(direction, entryPrice, p))
                candidates.Add(
                    new StopCandidate
                    {
                        Price = p,
                        Quality = 92,
                        Distance = Math.Abs(entryPrice - p),
                        Structural = true,
                        Provenance =
                            CFIPClean83Provenance.Direct(
                                "ENTRY",
                                "ENTRY_INVALIDATION")
                    });
        }

        for (int i = 0; i < structure.Zones.Count; i++)
        {
            CFIPClean83ZoneRecord z = structure.Zones[i];

            if (z.Direction != direction ||
                z.Invalidated ||
                z.Consumed ||
                z.Lifecycle == CFIPClean83ZoneLifecycle.Expired ||
                !string.Equals(
                    z.Timeframe,
                    "M5",
                    StringComparison.OrdinalIgnoreCase))
                continue;

            double p =
                direction == CFIPClean83Direction.Buy
                    ? z.CurrentLower - stopBufferAtr * atr
                    : z.CurrentUpper + stopBufferAtr * atr;

            if (!IsProtectiveStop(direction, entryPrice, p))
                continue;

            candidates.Add(
                new StopCandidate
                {
                    Price = p,
                    Quality = Math.Max(60, z.Quality),
                    Distance = Math.Abs(entryPrice - p),
                    Structural = true,
                    Provenance =
                        CFIPClean83Provenance.Direct(
                            "STRUCTURE",
                            z.Kind == CFIPClean83ZoneKind.FairValueGap
                                ? "M5_FVG_STOP"
                                : "M5_OB_STOP")
                });
        }

        for (int i = 0; i < structure.Events.Count; i++)
        {
            CFIPClean83StructureEventRecord e = structure.Events[i];

            bool swing =
                direction == CFIPClean83Direction.Buy
                    ? e.Kind == CFIPClean83StructureEventKind.SwingLow
                    : e.Kind == CFIPClean83StructureEventKind.SwingHigh;

            bool htf =
                e.Timeframe == "H1" ||
                e.Timeframe == "H4" ||
                e.Timeframe == "D1";

            if (!swing ||
                !IsProtectiveStop(direction, entryPrice, e.Price) ||
                (!htf && e.Timeframe != "M5") ||
                (htf && !useHtfStop))
                continue;

            int quality =
                Math.Max(
                    50,
                    Math.Min(
                        100,
                        e.Quality +
                        (htf ? 8 : 0)));

            candidates.Add(
                new StopCandidate
                {
                    Price = e.Price -
                        (direction == CFIPClean83Direction.Buy
                            ? stopBufferAtr * atr
                            : -stopBufferAtr * atr),
                    Quality = quality,
                    Distance = Math.Abs(
                        entryPrice -
                        (e.Price -
                            (direction == CFIPClean83Direction.Buy
                                ? stopBufferAtr * atr
                                : -stopBufferAtr * atr))),
                    Structural = true,
                    Provenance =
                        CFIPClean83Provenance.Direct(
                            e.Timeframe,
                            htf
                                ? "HTF_SWING_STOP"
                                : "M5_SWING_STOP")
                });
        }

        StopCandidate best = null;
        double maximumDistance = maximumRiskAtr * atr;

        for (int i = 0; i < candidates.Count; i++)
        {
            StopCandidate c = candidates[i];

            if (c.Distance > maximumDistance)
                continue;

            if (best == null ||
                c.Distance > best.Distance ||
                (Math.Abs(c.Distance - best.Distance) < 0.0000001 &&
                 c.Quality > best.Quality))
                best = c;
        }

        if (best != null)
            return best;

        // Return the closest valid structural candidate so the caller can
        // explicitly decide whether it must be rejected or replaced by a
        // configured fallback.
        for (int i = 0; i < candidates.Count; i++)
        {
            StopCandidate c = candidates[i];
            if (!IsProtectiveStop(direction, entryPrice, c.Price))
                continue;

            if (best == null ||
                c.Quality > best.Quality)
                best = c;
        }

        return best;
    }

    private CFIPClean83TargetLadder BuildTargetLadder(
        CFIPClean83DecisionSnapshot decision,
        double entryPrice,
        double stopPrice,
        double atr,
        CFIPClean83StructureSnapshot structure,
        CFIPClean83ConfigSnapshot configuration)
    {
        double risk = Math.Abs(entryPrice - stopPrice);
        if (risk <= 0 || atr <= 0)
            return new CFIPClean83TargetLadder(
                new List<CFIPClean83TargetLevel>());

        double[] rr =
        {
            configuration.Get("Tp1MinimumRR", 2.00),
            configuration.Get("Tp2MinimumRR", 3.20),
            configuration.Get("Tp3MinimumRR", 4.80),
            configuration.Get("Tp4MinimumRR", 6.50)
        };

        if (configuration.Get("AdaptiveStructuralRR", true))
        {
            double qualityFactor =
                1.0 +
                Math.Max(
                    0,
                    decision.Quality - 80) /
                200.0;

            for (int i = 0; i < rr.Length; i++)
                rr[i] *= qualityFactor;
        }

        double spacing =
            Math.Max(
                0.05,
                configuration.Get(
                    "MinimumTpSpacingAtr",
                    0.40)) * atr;
        double clearance =
            Math.Max(
                0.02,
                configuration.Get(
                    "TargetClearanceAtr",
                    0.10)) * atr;
        double maxExtension =
            Math.Max(
                1.0,
                configuration.Get(
                    "MaximumTargetExtensionAtr",
                    4.0)) * atr;
        bool rejectObstacle =
            configuration.Get(
                "RejectTargetObstacle",
                true);
        bool allowSynthetic =
            configuration.Get(
                "AllowSyntheticTargetFallback",
                true);

        var levels = new List<CFIPClean83TargetLevel>();
        double previous = entryPrice;
        bool htfTargetUsed = false;

        for (int stageIndex = 0; stageIndex < rr.Length; stageIndex++)
        {
            double minimumDistance = rr[stageIndex] * risk;
            if (minimumDistance > maxExtension)
                break;

            TargetCandidate candidate =
                FindLiquidityTarget(
                    decision.Direction,
                    entryPrice,
                    previous,
                    minimumDistance,
                    maxExtension,
                    spacing,
                    clearance,
                    structure,
                    rejectObstacle);

            if (candidate == null && allowSynthetic)
            {
                double syntheticDistance =
                    Math.Max(
                        minimumDistance,
                        Math.Abs(previous - entryPrice) + spacing);

                if (syntheticDistance <= maxExtension)
                {
                    double p =
                        decision.Direction == CFIPClean83Direction.Buy
                            ? entryPrice + syntheticDistance
                            : entryPrice - syntheticDistance;

                    if (!rejectObstacle ||
                        !HasObstacle(
                            decision.Direction,
                            entryPrice,
                            p,
                            clearance,
                            structure))
                    {
                        candidate =
                            new TargetCandidate
                            {
                                Price = p,
                                Quality = 55,
                                Htf = false,
                                Provenance =
                                    CFIPClean83Provenance.FallbackFrom(
                                        "TARGET",
                                        "SYNTHETIC_RR_TARGET",
                                        CFIPClean83FallbackKind.Synthetic,
                                        "No suitable structural/liquidity target met the configured constraints.")
                            };
                    }
                }
            }

            if (candidate == null)
                break;

            CFIPClean83TargetStage stage =
                (CFIPClean83TargetStage)(stageIndex + 1);

            levels.Add(
                new CFIPClean83TargetLevel(
                    stage,
                    new CFIPClean83PriceLevel(
                        candidate.Price,
                        "TP" + (stageIndex + 1),
                        candidate.Provenance),
                    candidate.Quality,
                    CFIPClean83TargetState.Proposed));

            htfTargetUsed =
                htfTargetUsed || candidate.Htf;
            previous = candidate.Price;
        }

        // TP1 is mandatory. Higher stages are only published when their
        // configured RR/extension/obstacle constraints can be satisfied.
        if (levels.Count == 0)
            return new CFIPClean83TargetLadder(
                new List<CFIPClean83TargetLevel>());

        return new CFIPClean83TargetLadder(levels);
    }

    private TargetCandidate FindLiquidityTarget(
        CFIPClean83Direction direction,
        double entryPrice,
        double previousTarget,
        double minimumDistance,
        double maximumDistance,
        double spacing,
        double clearance,
        CFIPClean83StructureSnapshot structure,
        bool rejectObstacle)
    {
        TargetCandidate best = null;

        for (int i = 0; i < structure.Liquidity.Count; i++)
        {
            CFIPClean83LiquidityRecord x = structure.Liquidity[i];

            bool directional =
                direction == CFIPClean83Direction.Buy
                    ? x.Price > entryPrice
                    : x.Price < entryPrice;

            if (!directional || x.Swept)
                continue;

            double distance = Math.Abs(x.Price - entryPrice);
            if (distance < minimumDistance ||
                distance > maximumDistance)
                continue;

            bool progressesBeyondPrevious =
                direction == CFIPClean83Direction.Buy
                    ? x.Price > previousTarget + spacing
                    : x.Price < previousTarget - spacing;

            if (!progressesBeyondPrevious)
                continue;

            if (Math.Abs(x.Price - entryPrice) < clearance)
                continue;

            if (rejectObstacle &&
                HasObstacle(
                    direction,
                    entryPrice,
                    x.Price,
                    clearance,
                    structure))
                continue;

            bool htf =
                x.Timeframe == "H1" ||
                x.Timeframe == "H4" ||
                x.Timeframe == "D1" ||
                x.Timeframe == "W1";

            int quality =
                Math.Max(
                    40,
                    Math.Min(
                        100,
                        x.Quality +
                        (htf ? 6 : 0)));

            if (best == null ||
                quality > best.Quality ||
                (quality == best.Quality &&
                 distance < Math.Abs(best.Price - entryPrice)))
            {
                best =
                    new TargetCandidate
                    {
                        Price = x.Price,
                        Quality = quality,
                        Htf = htf,
                        Provenance =
                            CFIPClean83Provenance.Direct(
                                x.Timeframe,
                                "LIQUIDITY_TARGET_" +
                                x.Kind.ToString().ToUpperInvariant())
                    };
            }
        }

        return best;
    }

    private bool HasObstacle(
        CFIPClean83Direction direction,
        double entryPrice,
        double targetPrice,
        double clearance,
        CFIPClean83StructureSnapshot structure)
    {
        double lower =
            Math.Min(entryPrice, targetPrice) + clearance;
        double upper =
            Math.Max(entryPrice, targetPrice) - clearance;

        if (upper <= lower)
            return false;

        CFIPClean83Direction opposing =
            direction == CFIPClean83Direction.Buy
                ? CFIPClean83Direction.Sell
                : CFIPClean83Direction.Buy;

        for (int i = 0; i < structure.Zones.Count; i++)
        {
            CFIPClean83ZoneRecord z = structure.Zones[i];

            if (z.Direction != opposing ||
                z.Invalidated ||
                z.Consumed ||
                z.Lifecycle == CFIPClean83ZoneLifecycle.Expired)
                continue;

            if (z.CurrentUpper >= lower &&
                z.CurrentLower <= upper)
                return true;
        }

        return false;
    }

    private bool HasHtfTarget(
        CFIPClean83TargetLadder ladder)
    {
        for (int i = 0; i < ladder.Levels.Count; i++)
        {
            CFIPClean83Provenance p =
                ladder.Levels[i].Level.Provenance;

            if (p != null &&
                (p.Source == "H1" ||
                 p.Source == "H4" ||
                 p.Source == "D1" ||
                 p.Source == "W1"))
                return true;
        }

        return false;
    }

    private bool IsProtectiveStop(
        CFIPClean83Direction direction,
        double entryPrice,
        double stopPrice)
    {
        return
            direction == CFIPClean83Direction.Buy
                ? stopPrice < entryPrice
                : direction == CFIPClean83Direction.Sell &&
                  stopPrice > entryPrice;
    }

    private string BuildPlanId(
        DateTime referenceUtc,
        CFIPClean83Direction direction,
        double entryPrice,
        double stopPrice)
    {
        return
            "CFIP81|PLAN|" +
            referenceUtc.Ticks.ToString() +
            "|" +
            direction.ToString() +
            "|" +
            entryPrice.ToString("R") +
            "|" +
            stopPrice.ToString("R");
    }

    private string BuildSignalId(
        DateTime referenceUtc,
        CFIPClean83Direction direction)
    {
        return
            "CFIP81|SIGNAL|" +
            referenceUtc.Ticks.ToString() +
            "|" +
            direction.ToString();
    }

    private CFIPClean83TradePlan InvalidPlan(
        CFIPClean83DecisionSnapshot decision,
        CFIPClean83EntrySnapshot entry,
        CFIPClean83MtfSnapshot mtf,
        CFIPClean83RuntimeSnapshot runtime,
        string reason)
    {
        DateTime now =
            runtime != null
                ? runtime.ServerUtc
                : DateTime.UtcNow;

        DateTime reference =
            mtf != null
                ? mtf.ReferenceUtc
                : DateTime.MinValue;

        CFIPClean83Direction direction =
            decision != null
                ? decision.Direction
                : CFIPClean83Direction.Wait;

        CFIPClean83EntryModel entryModel =
            entry != null && entry.Model != null
                ? entry.Model
                : new CFIPClean83EntryModel(
                    direction == CFIPClean83Direction.Wait
                        ? CFIPClean83Direction.Buy
                        : direction,
                    new CFIPClean83PriceLevel(
                        runtime != null && runtime.Bid > 0
                            ? runtime.Bid
                            : 1,
                        "InvalidPlanEntry",
                        CFIPClean83Provenance.Direct(
                            "PHASE8",
                            "INVALID_PLAN")),
                    new CFIPClean83PriceZone(
                        runtime != null && runtime.Bid > 0
                            ? runtime.Bid
                            : 1,
                        runtime != null && runtime.Bid > 0
                            ? runtime.Bid
                            : 1,
                        "InvalidPlanZone",
                        CFIPClean83Provenance.Direct(
                            "PHASE8",
                            "INVALID_PLAN")),
                    null,
                    new CFIPClean83PriceLevel(
                        runtime != null && runtime.Bid > 0
                            ? runtime.Bid
                            : 1,
                        "InvalidPlanInvalidation",
                        CFIPClean83Provenance.Direct(
                            "PHASE8",
                            "INVALID_PLAN")));

        double safeEntry =
            entryModel.IdealEntry.Price;
        var identity =
            new CFIPClean83TradeIdentity(
                "CFIP-PRO-v83",
                runtime != null ? runtime.Symbol : string.Empty,
                "INVALID|" + reference.Ticks.ToString(),
                "INVALID|" + reason);

        var emptyLadder =
            new CFIPClean83TargetLadder(
                new List<CFIPClean83TargetLevel>());

        return new CFIPClean83TradePlan(
            identity,
            direction,
            entry != null
                ? entry.Mode
                : CFIPClean83EntryMode.None,
            entryModel,
            new CFIPClean83PriceLevel(
                safeEntry,
                "InvalidPlanExecutionAnchor",
                CFIPClean83Provenance.Direct(
                    "PHASE8",
                    "INVALID_PLAN")),
            new CFIPClean83PriceLevel(
                safeEntry,
                "InvalidPlanStop",
                CFIPClean83Provenance.FallbackFrom(
                    "PHASE8",
                    reason,
                    CFIPClean83FallbackKind.None,
                    "Plan is explicitly invalid and must not reach execution.")),
            emptyLadder,
            0,
            0,
            0,
            false,
            now,
            mtf != null && mtf.M5 != null
                ? mtf.M5.ClosedIndex
                : -1,
            CFIPClean83Provenance.Direct(
                "PHASE8",
                reason));
    }
}

// ------------------------------------------------------------------------
    // Execution intent / result
    // ------------------------------------------------------------------------

    public sealed class CFIPClean83RiskRequest
    {
        public double RiskPercentEquity { get; private set; }
        public double RequestedVolumeInUnits { get; private set; }
        public double MaxRiskAmount { get; private set; }

        public CFIPClean83RiskRequest(
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

    public sealed class CFIPClean83ExecutionIntent
    {
        public CFIPClean83ExecutionIdentity Identity { get; private set; }
        public CFIPClean83TradeIdentity TradeIdentity { get; private set; }
        public CFIPClean83Direction Direction { get; private set; }
        public CFIPClean83ExecutionKind Kind { get; private set; }
        public CFIPClean83EntryMode EntryMode { get; private set; }

        // Exact value requested from the broker.
        public CFIPClean83PriceLevel RequestedEntry { get; private set; }

        // Structural activation threshold, when applicable.
        public CFIPClean83PriceLevel Trigger { get; private set; }

        // Protection requested at execution time.
        public CFIPClean83PriceLevel StopLoss { get; private set; }

        // This is the requested active broker target, not the whole ladder.
        public CFIPClean83PriceLevel EffectiveTarget { get; private set; }

        public double VolumeInUnits { get; private set; }
        public CFIPClean83RiskRequest Risk { get; private set; }
        public CFIPClean83DecisionPolicyMode PolicyMode { get; private set; }
        public DateTime CreatedUtc { get; private set; }
        public DateTime? ExpiryUtc { get; private set; }

        public CFIPClean83ExecutionIntent(
            CFIPClean83ExecutionIdentity identity,
            CFIPClean83TradeIdentity tradeIdentity,
            CFIPClean83Direction direction,
            CFIPClean83ExecutionKind kind,
            CFIPClean83EntryMode entryMode,
            CFIPClean83PriceLevel requestedEntry,
            CFIPClean83PriceLevel trigger,
            CFIPClean83PriceLevel stopLoss,
            CFIPClean83PriceLevel effectiveTarget,
            double volumeInUnits,
            CFIPClean83RiskRequest risk,
            CFIPClean83DecisionPolicyMode policyMode,
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

    public sealed class CFIPClean83ExecutionResult
    {
        public bool Accepted { get; private set; }
        public string BrokerOrderId { get; private set; }
        public string BrokerPositionId { get; private set; }
        public CFIPClean83PriceLevel ActualFill { get; private set; }
        public double RequestedVsActualDelta { get; private set; }
        public string BrokerError { get; private set; }
        public CFIPClean83ProtectionState ProtectionState { get; private set; }
        public bool ReconciliationRequired { get; private set; }
        public string StatusDetail { get; private set; }

        public CFIPClean83ExecutionResult(
            bool accepted,
            string brokerOrderId,
            string brokerPositionId,
            CFIPClean83PriceLevel actualFill,
            double requestedVsActualDelta,
            string brokerError,
            CFIPClean83ProtectionState protectionState,
            bool reconciliationRequired,
            string statusDetail = "")
        {
            Accepted = accepted;
            BrokerOrderId = brokerOrderId ?? string.Empty;
            BrokerPositionId = brokerPositionId ?? string.Empty;
            ActualFill = actualFill;
            RequestedVsActualDelta = requestedVsActualDelta;
            BrokerError = brokerError ?? string.Empty;
            ProtectionState = protectionState;
            ReconciliationRequired = reconciliationRequired;
            StatusDetail = statusDetail ?? string.Empty;
        }
    }

    // ------------------------------------------------------------------------
    // Broker state
    // ------------------------------------------------------------------------

    public sealed class CFIPClean83BrokerPositionSnapshot
    {
        public string BrokerPositionId { get; private set; }
        public string Label { get; private set; }
        public string Symbol { get; private set; }
        public CFIPClean83Direction Direction { get; private set; }
        public double EntryPrice { get; private set; }
        public double VolumeInUnits { get; private set; }
        public double? StopLoss { get; private set; }
        public double? TakeProfit { get; private set; }
        public double NetProfit { get; private set; }
        public bool IsOpen { get; private set; }

        public CFIPClean83BrokerPositionSnapshot(
            string brokerPositionId,
            string label,
            string symbol,
            CFIPClean83Direction direction,
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

    public sealed class CFIPClean83BrokerPendingOrderSnapshot
    {
        public string BrokerOrderId { get; private set; }
        public string Label { get; private set; }
        public string Symbol { get; private set; }
        public CFIPClean83Direction Direction { get; private set; }
        public CFIPClean83ExecutionKind Kind { get; private set; }
        public double RequestedEntry { get; private set; }
        public double VolumeInUnits { get; private set; }
        public double? StopLoss { get; private set; }
        public double? TakeProfit { get; private set; }
        public bool IsActive { get; private set; }

        public CFIPClean83BrokerPendingOrderSnapshot(
            string brokerOrderId,
            string label,
            string symbol,
            CFIPClean83Direction direction,
            CFIPClean83ExecutionKind kind,
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

    public sealed class CFIPClean83BrokerStateSnapshot
    {
        private readonly ReadOnlyCollection<CFIPClean83BrokerPositionSnapshot> _positions;
        private readonly ReadOnlyCollection<CFIPClean83BrokerPendingOrderSnapshot> _pendingOrders;

        public IReadOnlyList<CFIPClean83BrokerPositionSnapshot> Positions
        {
            get { return _positions; }
        }

        public IReadOnlyList<CFIPClean83BrokerPendingOrderSnapshot> PendingOrders
        {
            get { return _pendingOrders; }
        }

        public CFIPClean83BrokerStateSnapshot(
            IList<CFIPClean83BrokerPositionSnapshot> positions,
            IList<CFIPClean83BrokerPendingOrderSnapshot> pendingOrders)
        {
            _positions =
                new ReadOnlyCollection<CFIPClean83BrokerPositionSnapshot>(
                    new List<CFIPClean83BrokerPositionSnapshot>(
                        positions ??
                        new List<CFIPClean83BrokerPositionSnapshot>()));

            _pendingOrders =
                new ReadOnlyCollection<CFIPClean83BrokerPendingOrderSnapshot>(
                    new List<CFIPClean83BrokerPendingOrderSnapshot>(
                        pendingOrders ??
                        new List<CFIPClean83BrokerPendingOrderSnapshot>()));
        }
    }

    // ------------------------------------------------------------------------
    // Lifecycle authority
    // ------------------------------------------------------------------------

    public sealed class CFIPClean83LifecycleTransition
    {
        public CFIPClean83LifecycleState From { get; private set; }
        public CFIPClean83LifecycleState To { get; private set; }
        public DateTime TimeUtc { get; private set; }
        public string Reason { get; private set; }

        public CFIPClean83LifecycleTransition(
            CFIPClean83LifecycleState from,
            CFIPClean83LifecycleState to,
            DateTime timeUtc,
            string reason)
        {
            From = from;
            To = to;
            TimeUtc = timeUtc;
            Reason = reason ?? string.Empty;
        }
    }

    public sealed class CFIPClean83LifecycleManager
    {
        private CFIPClean83LifecycleState _state;

        public CFIPClean83LifecycleState State
        {
            get { return _state; }
        }

        public CFIPClean83LifecycleManager()
        {
            _state = CFIPClean83LifecycleState.Flat;
        }

        public bool TryTransition(
            CFIPClean83LifecycleState target,
            DateTime timeUtc,
            string reason)
        {
            if (!IsAllowed(_state, target))
                return false;

            _state = target;
            return true;
        }

        private static bool IsAllowed(
            CFIPClean83LifecycleState from,
            CFIPClean83LifecycleState to)
        {
            if (from == to)
                return true;

            switch (from)
            {
                case CFIPClean83LifecycleState.Flat:
                    return
                        to == CFIPClean83LifecycleState.SignalDetected ||
                        to == CFIPClean83LifecycleState.Closed;

                case CFIPClean83LifecycleState.SignalDetected:
                    return
                        to == CFIPClean83LifecycleState.PlanReady ||
                        to == CFIPClean83LifecycleState.Rejected ||
                        to == CFIPClean83LifecycleState.Flat;

                case CFIPClean83LifecycleState.PlanReady:
                    return
                        to == CFIPClean83LifecycleState.ExecutionReady ||
                        to == CFIPClean83LifecycleState.Rejected ||
                        to == CFIPClean83LifecycleState.Flat;

                case CFIPClean83LifecycleState.ExecutionReady:
                    return
                        to == CFIPClean83LifecycleState.PendingOrder ||
                        to == CFIPClean83LifecycleState.LivePosition ||
                        to == CFIPClean83LifecycleState.Rejected ||
                        to == CFIPClean83LifecycleState.Error;

                case CFIPClean83LifecycleState.PendingOrder:
                    return
                        to == CFIPClean83LifecycleState.LivePosition ||
                        to == CFIPClean83LifecycleState.RecoveryRequired ||
                        to == CFIPClean83LifecycleState.Flat ||
                        to == CFIPClean83LifecycleState.Error;

                case CFIPClean83LifecycleState.LivePosition:
                    return
                        to == CFIPClean83LifecycleState.ExitRequested ||
                        to == CFIPClean83LifecycleState.RecoveryRequired ||
                        to == CFIPClean83LifecycleState.Closed ||
                        to == CFIPClean83LifecycleState.Error;

                case CFIPClean83LifecycleState.ExitRequested:
                    return
                        to == CFIPClean83LifecycleState.Closed ||
                        to == CFIPClean83LifecycleState.RecoveryRequired ||
                        to == CFIPClean83LifecycleState.Error;

                case CFIPClean83LifecycleState.RecoveryRequired:
                    return
                        to == CFIPClean83LifecycleState.LivePosition ||
                        to == CFIPClean83LifecycleState.Closed ||
                        to == CFIPClean83LifecycleState.Error ||
                        to == CFIPClean83LifecycleState.Flat;

                case CFIPClean83LifecycleState.Closed:
                    return
                        to == CFIPClean83LifecycleState.SignalDetected ||
                        to == CFIPClean83LifecycleState.Flat;

                case CFIPClean83LifecycleState.Rejected:
                    return
                        to == CFIPClean83LifecycleState.Flat ||
                        to == CFIPClean83LifecycleState.SignalDetected;

                case CFIPClean83LifecycleState.Error:
                    return
                        to == CFIPClean83LifecycleState.RecoveryRequired ||
                        to == CFIPClean83LifecycleState.Flat;

                default:
                    return false;
            }
        }
    }

    // ------------------------------------------------------------------------
    // Configuration / runtime separation
    // ------------------------------------------------------------------------

    public sealed class CFIPClean83ExecutionEnvelope
    {
        public double MaxEntryChaseAtr { get; private set; }
        public double MaxBrokerSlippagePips { get; private set; }
        public double MaxBreakoutFillDeviationPips { get; private set; }
        public double MaxPlanRebaseDistancePips { get; private set; }

        public CFIPClean83ExecutionEnvelope(
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

    public enum CFIPClean83ConfigurationSection
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

    public sealed class CFIPClean83ConfigurationSectionSnapshot
    {
        private readonly ReadOnlyDictionary<string, object> _values;

        public CFIPClean83ConfigurationSection Section { get; private set; }
        public IReadOnlyDictionary<string, object> Values { get { return _values; } }

        public CFIPClean83ConfigurationSectionSnapshot(
            CFIPClean83ConfigurationSection section,
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

    public sealed class CFIPClean83ConfigSnapshot
    {
        private readonly ReadOnlyDictionary<string, object> _values;
        private readonly ReadOnlyDictionary<string, CFIPClean83ConfigurationSectionSnapshot> _sections;

        public string StrategyId { get; private set; }
        public int ParameterCount { get { return _values.Count; } }
        public bool ManualTradeEntryControlsSupported { get { return false; } }

        public IReadOnlyDictionary<string, object> Values { get { return _values; } }
        public IReadOnlyDictionary<string, CFIPClean83ConfigurationSectionSnapshot> Sections
        {
            get { return _sections; }
        }

        private CFIPClean83ConfigSnapshot(
            string strategyId,
            IDictionary<string, object> values,
            IDictionary<string, CFIPClean83ConfigurationSectionSnapshot> sections)
        {
            StrategyId = strategyId ?? string.Empty;
            _values = new ReadOnlyDictionary<string, object>(
                new Dictionary<string, object>(values));

            _sections =
                new ReadOnlyDictionary<string, CFIPClean83ConfigurationSectionSnapshot>(
                    new Dictionary<string, CFIPClean83ConfigurationSectionSnapshot>(sections));
        }

        public static CFIPClean83ConfigSnapshot Build(
            object parameterSource,
            string strategyId)
        {
            if (parameterSource == null)
                throw new ArgumentNullException("parameterSource");

            var values =
                new Dictionary<string, object>(StringComparer.Ordinal);

            var buckets =
                new Dictionary<CFIPClean83ConfigurationSection, IDictionary<string, object>>();

            foreach (CFIPClean83ConfigurationSection section in Enum.GetValues(
                typeof(CFIPClean83ConfigurationSection)))
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

                CFIPClean83ConfigurationSection section =
                    ResolveSection(group);

                buckets[section][property.Name] = value;
            }

            var sections =
                new Dictionary<string, CFIPClean83ConfigurationSectionSnapshot>(
                    StringComparer.Ordinal);

            foreach (var pair in buckets)
            {
                sections[pair.Key.ToString()] =
                    new CFIPClean83ConfigurationSectionSnapshot(
                        pair.Key,
                        pair.Value);
            }

            return new CFIPClean83ConfigSnapshot(
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

        public static CFIPClean83ConfigurationSection ResolveSection(
            string group)
        {
            string value = group ?? string.Empty;

            if (value.Contains("01 · Decision"))
                return CFIPClean83ConfigurationSection.Decision;
            if (value.Contains("02 · MTF"))
                return CFIPClean83ConfigurationSection.Mtf;
            if (value.Contains("03 · Structure"))
                return CFIPClean83ConfigurationSection.Structure;
            if (value.Contains("04 · Zones"))
                return CFIPClean83ConfigurationSection.Zones;
            if (value.Contains("05 · Liquidity"))
                return CFIPClean83ConfigurationSection.Liquidity;
            if (value.Contains("06 · Indicators"))
                return CFIPClean83ConfigurationSection.Indicators;
            if (value.Contains("07 · Entry"))
                return CFIPClean83ConfigurationSection.Entry;
            if (value.Contains("08 · Smart Weights"))
                return CFIPClean83ConfigurationSection.SmartWeights;
            if (value.Contains("09 · Risk & Targets"))
                return CFIPClean83ConfigurationSection.RiskTargets;
            if (value.Contains("10 · Live Management"))
                return CFIPClean83ConfigurationSection.LiveManagement;
            if (value.Contains("11 · Filters"))
                return CFIPClean83ConfigurationSection.Filters;
            if (value.Contains("12 · ALERTS"))
                return CFIPClean83ConfigurationSection.Alerts;
            if (value.Contains("13 · AUTO TRADING"))
                return CFIPClean83ConfigurationSection.Automation;
            if (value.Contains("24 · SMART EXECUTION"))
                return CFIPClean83ConfigurationSection.SmartExecution;
            if (value.Contains("14 · DISPLAY"))
                return CFIPClean83ConfigurationSection.Display;
            if (value.Contains("15 · CONTROL"))
                return CFIPClean83ConfigurationSection.Control;
            if (value.Contains("20 · Confluence"))
                return CFIPClean83ConfigurationSection.ConfluenceExtensions;
            if (value.Contains("21 · Complete Intelligence"))
                return CFIPClean83ConfigurationSection.CompleteIntelligence;
            if (value.Contains("23 · Structural Execution"))
                return CFIPClean83ConfigurationSection.StructuralExecution;
            if (value.Contains("22 · Safety"))
                return CFIPClean83ConfigurationSection.SafetyPrecision;
            if (value.Contains("15 · INTELLIGENCE — EARLY"))
                return CFIPClean83ConfigurationSection.EarlyIntelligence;
            if (value.Contains("16 · Accuracy"))
                return CFIPClean83ConfigurationSection.Accuracy;
            if (value.Contains("17 · Smart Engine"))
                return CFIPClean83ConfigurationSection.SmartEngine;

            return CFIPClean83ConfigurationSection.Unknown;
        }
    }

    public sealed class CFIPClean83RuntimeAuthority
    {
        public bool AutoTradingEnabled { get; private set; }
        public bool AutomaticOrdersEnabled { get; private set; }

        public void InitializeFromConfiguration(
            CFIPClean83ConfigSnapshot configuration)
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

    public sealed class CFIPClean83OutcomeEvent
    {
        public CFIPClean83TradeIdentity TradeIdentity { get; private set; }
        public double EntryPrice { get; private set; }
        public double ExitPrice { get; private set; }
        public double ResultAmount { get; private set; }
        public double ResultR { get; private set; }
        public double MaxFavorableExcursion { get; private set; }
        public double MaxAdverseExcursion { get; private set; }
        public CFIPClean83TargetStage HighestTargetStageReached { get; private set; }
        public string ExitReason { get; private set; }
        public DateTime EntryUtc { get; private set; }
        public DateTime ExitUtc { get; private set; }

        public CFIPClean83OutcomeEvent(
            CFIPClean83TradeIdentity tradeIdentity,
            double entryPrice,
            double exitPrice,
            double resultAmount,
            double resultR,
            double maxFavorableExcursion,
            double maxAdverseExcursion,
            CFIPClean83TargetStage highestTargetStageReached,
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
    // Phase 9 — unified execution policy and broker adapter
    // ------------------------------------------------------------------------

    public sealed class CFIPClean83ExecutionReadiness
    {
        private readonly ReadOnlyCollection<CFIPClean83BlockReason> _blockReasons;

        public bool Eligible { get; private set; }
        public CFIPClean83ExecutionKind Kind { get; private set; }
        public CFIPClean83EntryMode EntryMode { get; private set; }
        public CFIPClean83Direction Direction { get; private set; }
        public double RequestedPrice { get; private set; }
        public double VolumeInUnits { get; private set; }
        public double RiskAmount { get; private set; }
        public double EstimatedMargin { get; private set; }
        public IReadOnlyList<CFIPClean83BlockReason> BlockReasons { get { return _blockReasons; } }

        public CFIPClean83ExecutionReadiness(
            bool eligible,
            CFIPClean83ExecutionKind kind,
            CFIPClean83EntryMode entryMode,
            CFIPClean83Direction direction,
            double requestedPrice,
            double volumeInUnits,
            double riskAmount,
            double estimatedMargin,
            IList<CFIPClean83BlockReason> blockReasons)
        {
            Eligible = eligible;
            Kind = kind;
            EntryMode = entryMode;
            Direction = direction;
            RequestedPrice = Math.Max(0, requestedPrice);
            VolumeInUnits = Math.Max(0, volumeInUnits);
            RiskAmount = Math.Max(0, riskAmount);
            EstimatedMargin = Math.Max(0, estimatedMargin);
            _blockReasons =
                new ReadOnlyCollection<CFIPClean83BlockReason>(
                    new List<CFIPClean83BlockReason>(
                        blockReasons ??
                        new List<CFIPClean83BlockReason>()));
        }
    }

    public interface ICFIPClean83ExecutionPolicy
    {
        CFIPClean83ExecutionReadiness Evaluate(
            CFIPClean83DecisionSnapshot decision,
            CFIPClean83EntrySnapshot entry,
            CFIPClean83TradePlan plan,
            CFIPClean83RuntimeSnapshot runtime,
            CFIPClean83ConfigSnapshot configuration,
            double volumeInUnits,
            double riskAmount,
            double estimatedMargin);
    }

    public sealed class CFIPClean83ExecutionPolicy : ICFIPClean83ExecutionPolicy
    {
        public CFIPClean83ExecutionReadiness Evaluate(
            CFIPClean83DecisionSnapshot decision,
            CFIPClean83EntrySnapshot entry,
            CFIPClean83TradePlan plan,
            CFIPClean83RuntimeSnapshot runtime,
            CFIPClean83ConfigSnapshot configuration,
            double volumeInUnits,
            double riskAmount,
            double estimatedMargin)
        {
            var blocks = new List<CFIPClean83BlockReason>();

            if (decision == null || entry == null || plan == null ||
                runtime == null || configuration == null)
                return Blocked(blocks, CFIPClean83BlockReason.DataIncomplete);

            bool market =
                entry.Mode == CFIPClean83EntryMode.RetestMarket ||
                entry.Mode == CFIPClean83EntryMode.BreakoutMarket;

            bool pending =
                entry.Mode == CFIPClean83EntryMode.ContinuationStop ||
                entry.Mode == CFIPClean83EntryMode.ReversalLimit;

            if (!decision.DecisionEligible ||
                decision.Direction == CFIPClean83Direction.Wait ||
                !plan.IsValid)
                blocks.Add(CFIPClean83BlockReason.PolicyBlocked);

            if (entry.Decision != decision ||
                entry.Direction != decision.Direction ||
                entry.Mode != plan.EntryMode)
                blocks.Add(CFIPClean83BlockReason.EntryInvalid);

            CFIPClean83ExecutionKind kind =
                market
                    ? CFIPClean83ExecutionKind.Market
                    : entry.Mode == CFIPClean83EntryMode.ContinuationStop
                        ? CFIPClean83ExecutionKind.Stop
                        : entry.Mode == CFIPClean83EntryMode.ReversalLimit
                            ? CFIPClean83ExecutionKind.Limit
                            : CFIPClean83ExecutionKind.None;

            if (market)
            {
                if (!entry.Eligible ||
                    entry.State != CFIPClean83EntryTriggerState.Ready)
                    blocks.Add(CFIPClean83BlockReason.EntryInvalid);

                if (!configuration.Get("EnableAutoTrading", false))
                    blocks.Add(CFIPClean83BlockReason.PolicyBlocked);
            }
            else if (pending)
            {
                if (entry.State != CFIPClean83EntryTriggerState.WaitingBreakout ||
                    entry.Model == null)
                    blocks.Add(CFIPClean83BlockReason.EntryInvalid);

                if (!configuration.Get("EnableAutomaticOrders", false))
                    blocks.Add(CFIPClean83BlockReason.PolicyBlocked);
            }
            else
            {
                blocks.Add(CFIPClean83BlockReason.EntryInvalid);
            }

            if (configuration.Get("ConfirmedSignalsOnly", true) &&
                decision.PolicyMode != CFIPClean83DecisionPolicyMode.Confirmed)
                blocks.Add(CFIPClean83BlockReason.PolicyBlocked);

            if (decision.Confidence <
                configuration.Get("MinimumAutoConfidence", 86))
                blocks.Add(CFIPClean83BlockReason.ConfidenceTooLow);

            if (decision.Quality <
                configuration.Get("MinimumAutoSmartQuality", 80))
                blocks.Add(CFIPClean83BlockReason.EvidenceInsufficient);

            if (plan.LevelQuality <
                configuration.Get("MinimumAutoLevelQuality", 72))
                blocks.Add(CFIPClean83BlockReason.EntryInvalid);

            double requestedPrice =
                ResolveRequestedPrice(entry);

            CFIPClean83TargetLevel target =
                plan.TargetLadder.Find(
                    configuration.Get(
                        "AutoTpStage",
                        CFIPClean83TargetStage.TP1));

            if (target == null)
                target =
                    plan.TargetLadder.Find(
                        CFIPClean83TargetStage.TP1);

            if (runtime.PipSize <= 0 ||
                requestedPrice <= 0 ||
                target == null)
            {
                blocks.Add(CFIPClean83BlockReason.BrokerConstraintsBlocked);
            }
            else
            {
                double stopDistancePips =
                    Math.Abs(
                        requestedPrice -
                        plan.StructuralStop.Price) /
                    runtime.PipSize;
                double targetDistancePips =
                    Math.Abs(
                        target.Level.Price -
                        requestedPrice) /
                    runtime.PipSize;

                if (runtime.BrokerConstraints.MinStopDistancePips > 0 &&
                    stopDistancePips + 0.000001 <
                    runtime.BrokerConstraints.MinStopDistancePips)
                    blocks.Add(
                        CFIPClean83BlockReason.BrokerConstraintsBlocked);

                if (runtime.BrokerConstraints.MinTakeProfitDistancePips > 0 &&
                    targetDistancePips + 0.000001 <
                    runtime.BrokerConstraints.MinTakeProfitDistancePips)
                    blocks.Add(
                        CFIPClean83BlockReason.BrokerConstraintsBlocked);
            }

            if (runtime.BrokerConstraints.MinVolumeInUnits > 0 &&
                volumeInUnits + 0.000001 <
                runtime.BrokerConstraints.MinVolumeInUnits)
                blocks.Add(CFIPClean83BlockReason.BrokerConstraintsBlocked);

            if (runtime.BrokerConstraints.VolumeStepInUnits > 0 &&
                volumeInUnits > 0)
            {
                double steps =
                    volumeInUnits /
                    runtime.BrokerConstraints.VolumeStepInUnits;
                double nearest =
                    Math.Round(steps);

                if (Math.Abs(steps - nearest) > 0.000001)
                    blocks.Add(
                        CFIPClean83BlockReason.BrokerConstraintsBlocked);
            }

            if (!runtime.SymbolTradingEnabled)
                blocks.Add(CFIPClean83BlockReason.BrokerUnavailable);

            if (configuration.Get("UseSessionFilter", false) &&
                !IsWithinConfiguredSession(
                    runtime.ServerUtc,
                    configuration))
                blocks.Add(CFIPClean83BlockReason.SessionBlocked);

            if (configuration.Get("AvoidFridayLateEntry", false) &&
                runtime.ServerUtc.DayOfWeek == DayOfWeek.Friday &&
                runtime.ServerUtc.Hour >=
                configuration.Get("FridayCutoffUtc", 18))
                blocks.Add(CFIPClean83BlockReason.SessionBlocked);

            int exposureLimit =
                Math.Max(1, configuration.Get("MaximumOpenPositions", 1));

            if (runtime.ManagedPositionCount >= exposureLimit ||
                (pending && runtime.ManagedPendingOrderCount >= exposureLimit))
                blocks.Add(CFIPClean83BlockReason.ExistingExposureBlocked);

            if (configuration.Get("EnableDailyLossLimit", true))
            {
                double baseline =
                    runtime.Balance -
                    runtime.DailyRealizedNetProfit;

                double limit =
                    Math.Max(0, baseline) *
                    Math.Max(
                        0,
                        configuration.Get(
                            "MaximumDailyLossPercent",
                            3.0)) /
                    100.0;

                if (runtime.DailyRealizedNetProfit < -limit)
                    blocks.Add(CFIPClean83BlockReason.DailyLossBlocked);
            }

            if (configuration.Get("UseAutoMarginGuard", true))
            {
                double maxUsage =
                    Math.Max(
                        0,
                        configuration.Get(
                            "MaxAutoMarginUsagePercent",
                            80));
                double buffer =
                    Math.Max(
                        0,
                        configuration.Get(
                            "MarginBufferPercent",
                            10));

                double allowedMargin =
                    runtime.Equity *
                    Math.Max(0, maxUsage - buffer) /
                    100.0;

                double projectedMargin =
                    runtime.Margin +
                    Math.Max(0, estimatedMargin);

                if (runtime.Equity <= 0 ||
                    runtime.FreeMargin <= 0 ||
                    estimatedMargin < 0 ||
                    estimatedMargin > runtime.FreeMargin ||
                    projectedMargin > allowedMargin)
                    blocks.Add(CFIPClean83BlockReason.RiskInvalid);
            }

            double riskPercent =
                Math.Max(
                    0,
                    configuration.Get(
                        "RiskPercentEquity",
                        0.50));
            double riskBudget =
                runtime.Equity * riskPercent / 100.0;

            if (volumeInUnits <= 0 ||
                riskAmount <= 0 ||
                (riskBudget > 0 &&
                 riskAmount > riskBudget * 1.01) ||
                estimatedMargin < 0)
                blocks.Add(CFIPClean83BlockReason.RiskInvalid);

            return new CFIPClean83ExecutionReadiness(
                blocks.Count == 0,
                kind,
                entry.Mode,
                decision.Direction,
                ResolveRequestedPrice(entry),
                volumeInUnits,
                riskAmount,
                estimatedMargin,
                blocks);
        }

        private bool IsWithinConfiguredSession(
            DateTime utc,
            CFIPClean83ConfigSnapshot configuration)
        {
            int start =
                Math.Max(
                    0,
                    Math.Min(
                        23,
                        configuration.Get(
                            "SessionStartUtc",
                            6)));
            int end =
                Math.Max(
                    0,
                    Math.Min(
                        23,
                        configuration.Get(
                            "SessionEndUtc",
                            20)));

            int hour = utc.Hour;

            if (start == end)
                return true;

            return start < end
                ? hour >= start && hour < end
                : hour >= start || hour < end;
        }

        private double ResolveRequestedPrice(CFIPClean83EntrySnapshot entry)
        {
            if (entry == null || entry.Model == null)
                return 0;

            if (entry.Mode == CFIPClean83EntryMode.ContinuationStop &&
                entry.Model.Trigger != null)
                return entry.Model.Trigger.Price;

            if (entry.Mode == CFIPClean83EntryMode.ReversalLimit)
                return entry.Model.IdealEntry.Price;

            return entry.RequestedEntry != null
                ? entry.RequestedEntry.Price
                : entry.Model.IdealEntry.Price;
        }

        private CFIPClean83ExecutionReadiness Blocked(
            IList<CFIPClean83BlockReason> blocks,
            CFIPClean83BlockReason reason)
        {
            if (!blocks.Contains(reason))
                blocks.Add(reason);

            return new CFIPClean83ExecutionReadiness(
                false,
                CFIPClean83ExecutionKind.None,
                CFIPClean83EntryMode.None,
                CFIPClean83Direction.Wait,
                0,
                0,
                0,
                0,
                blocks);
        }
    }

    public sealed class CFIPClean83ExecutionPlanner : ICFIPClean83ExecutionPlanner
    {
        public CFIPClean83ExecutionIntent CreateIntent(
            CFIPClean83TradePlan plan,
            CFIPClean83EntrySnapshot entry,
            CFIPClean83RuntimeSnapshot runtime,
            CFIPClean83ConfigSnapshot configuration,
            CFIPClean83ExecutionReadiness readiness)
        {
            if (plan == null || entry == null || runtime == null ||
                configuration == null || readiness == null || !readiness.Eligible)
                throw new ArgumentException(
                    "Execution intent requires eligible execution state.");

            if (entry.Mode != plan.EntryMode ||
                entry.Direction != plan.Direction ||
                readiness.Kind == CFIPClean83ExecutionKind.None)
                throw new ArgumentException(
                    "Execution intent must match the authoritative TradePlan.");

            var requested =
                new CFIPClean83PriceLevel(
                    readiness.RequestedPrice,
                    "EXECUTION_REQUESTED_ENTRY",
                    CFIPClean83Provenance.Direct(
                        "EXECUTION",
                        readiness.Kind.ToString().ToUpperInvariant()));

            CFIPClean83TargetStage requestedStage =
                configuration.Get(
                    "AutoTpStage",
                    CFIPClean83TargetStage.TP1);

            CFIPClean83TargetLevel target =
                plan.TargetLadder.Find(requestedStage);

            if (target == null)
                throw new ArgumentException(
                    "Requested execution target stage is unavailable.",
                    "AutoTpStage");

            string seed =
                plan.Identity.PlanId +
                "|" +
                readiness.Kind.ToString();

            return new CFIPClean83ExecutionIntent(
                new CFIPClean83ExecutionIdentity(
                    seed,
                    "CFIP82|EXEC|" + seed,
                    runtime.ServerUtc),
                plan.Identity,
                plan.Direction,
                readiness.Kind,
                entry.Mode,
                requested,
                entry.Model.Trigger,
                plan.StructuralStop,
                target.Level,
                readiness.VolumeInUnits,
                new CFIPClean83RiskRequest(
                    configuration.Get("RiskPercentEquity", 0.50),
                    readiness.VolumeInUnits,
                    readiness.RiskAmount),
                entry.Decision.PolicyMode,
                runtime.ServerUtc,
                readiness.Kind == CFIPClean83ExecutionKind.Market
                    ? null
                    : runtime.ServerUtc +
                      TimeSpan.FromMinutes(
                          Math.Max(
                              15,
                              configuration.Get(
                                  "PendingOrderExpiryMinutes",
                                  120))));
        }
    }

    public sealed class CFIPClean83CTraderBrokerGateway : ICFIPClean83BrokerGateway
    {
        private readonly CFIP_MTF_LiveEntryEngine_Clean_v83 _host;

        public CFIPClean83CTraderBrokerGateway(
            CFIP_MTF_LiveEntryEngine_Clean_v83 host)
        {
            _host = host ?? throw new ArgumentNullException("host");
        }

        public CFIPClean83ExecutionResult Execute(
            CFIPClean83ExecutionIntent intent)
        {
            if (intent == null)
                return Failure("NULL_INTENT");

            try
            {
                TradeType type =
                    intent.Direction == CFIPClean83Direction.Buy
                        ? TradeType.Buy
                        : TradeType.Sell;

                double requested = intent.RequestedEntry.Price;
                double stop = intent.StopLoss.Price;
                double target = intent.EffectiveTarget.Price;
                double pip = _host.Symbol.PipSize;

                if (requested <= 0 || stop <= 0 || target <= 0 ||
                    intent.VolumeInUnits <= 0 || pip <= 0)
                    return Failure("BROKER_ARGUMENT_INVALID");

                double stopPips = Math.Abs(requested - stop) / pip;
                double targetPips = Math.Abs(target - requested) / pip;

                if (stopPips <= 0 || targetPips <= 0)
                    return Failure("PROTECTION_DISTANCE_INVALID");

                string label =
                    _host.Configuration.Get(
                        "AutoTradeLabel",
                        "CFIP-SMART-CLEAN82");

                string comment =
                    intent.TradeIdentity.SignalId +
                    "|" +
                    intent.TradeIdentity.PlanId;

                TradeResult result;

                if (intent.Kind == CFIPClean83ExecutionKind.Market)
                    result = _host.ExecuteMarketOrder(
                        type,
                        _host.SymbolName,
                        intent.VolumeInUnits,
                        label,
                        stopPips,
                        targetPips,
                        comment);
                else if (intent.Kind == CFIPClean83ExecutionKind.Stop)
                    result = _host.PlaceStopOrder(
                        type,
                        _host.SymbolName,
                        intent.VolumeInUnits,
                        requested,
                        label,
                        stopPips,
                        targetPips,
                        ProtectionType.Relative,
                        intent.ExpiryUtc,
                        comment);
                else if (intent.Kind == CFIPClean83ExecutionKind.Limit)
                    result = _host.PlaceLimitOrder(
                        type,
                        _host.SymbolName,
                        intent.VolumeInUnits,
                        requested,
                        label,
                        stopPips,
                        targetPips,
                        ProtectionType.Relative,
                        intent.ExpiryUtc,
                        comment);
                else
                    return Failure("EXECUTION_KIND_INVALID");

                if (!result.IsSuccessful)
                    return Failure(
                        result.Error != null
                            ? result.Error.ToString()
                            : "BROKER_REJECTED");

                if (intent.Kind == CFIPClean83ExecutionKind.Market)
                {
                    if (result.Position == null)
                        return Failure("POSITION_MISSING_AFTER_EXECUTION");

                    bool protectedPosition =
                        result.Position.StopLoss.HasValue &&
                        result.Position.TakeProfit.HasValue;

                    double actual =
                        result.Position.EntryPrice;

                    return new CFIPClean83ExecutionResult(
                        true,
                        string.Empty,
                        result.Position.Id.ToString(),
                        new CFIPClean83PriceLevel(
                            actual,
                            "ACTUAL_FILL",
                            CFIPClean83Provenance.Direct(
                                "BROKER",
                                "MARKET_FILL")),
                        Math.Abs(actual - requested),
                        string.Empty,
                        protectedPosition
                            ? CFIPClean83ProtectionState.FullyProtected
                            : CFIPClean83ProtectionState.RecoveryRequired,
                        !protectedPosition,
                        "MARKET_EXECUTED");
                }

                if (result.PendingOrder == null)
                    return Failure("PENDING_ORDER_MISSING_AFTER_ACCEPT");

                bool protectedPending =
                    result.PendingOrder.StopLoss.HasValue &&
                    result.PendingOrder.TakeProfit.HasValue;

                return new CFIPClean83ExecutionResult(
                    true,
                    result.PendingOrder.Id.ToString(),
                    string.Empty,
                    null,
                    0,
                    string.Empty,
                    protectedPending
                        ? CFIPClean83ProtectionState.FullyProtected
                        : CFIPClean83ProtectionState.RecoveryRequired,
                    !protectedPending,
                    "PENDING_ORDER_ACCEPTED");
            }
            catch (Exception ex)
            {
                return Failure(ex.Message);
            }
        }

        public CFIPClean83ExecutionResult ModifyProtection(
            string brokerPositionId,
            double? stopLoss,
            double? takeProfit)
        {
            try
            {
                int id;
                if (!int.TryParse(brokerPositionId, out id))
                    return Failure("POSITION_ID_INVALID");

                Position position = _host.Positions.FindById(id);
                if (position == null)
                    return Failure("POSITION_NOT_FOUND");

                TradeResult result =
                    _host.ModifyPosition(position, stopLoss, takeProfit);

                return result.IsSuccessful
                    ? new CFIPClean83ExecutionResult(
                        true,
                        string.Empty,
                        brokerPositionId,
                        null,
                        0,
                        string.Empty,
                        stopLoss.HasValue && takeProfit.HasValue
                            ? CFIPClean83ProtectionState.FullyProtected
                            : CFIPClean83ProtectionState.PartiallyProtected,
                        false,
                        "PROTECTION_MODIFIED")
                    : Failure(
                        result.Error != null
                            ? result.Error.ToString()
                            : "PROTECTION_MODIFICATION_REJECTED");
            }
            catch (Exception ex)
            {
                return Failure(ex.Message);
            }
        }

        public CFIPClean83ExecutionResult ClosePosition(string brokerPositionId)
        {
            try
            {
                int id;
                if (!int.TryParse(brokerPositionId, out id))
                    return Failure("POSITION_ID_INVALID");

                Position position = _host.Positions.FindById(id);
                if (position == null)
                    return Failure("POSITION_NOT_FOUND");

                TradeResult result = _host.ClosePosition(position);

                return result.IsSuccessful
                    ? Success("POSITION_CLOSE_ACCEPTED", brokerPositionId, string.Empty)
                    : Failure(
                        result.Error != null
                            ? result.Error.ToString()
                            : "POSITION_CLOSE_REJECTED");
            }
            catch (Exception ex)
            {
                return Failure(ex.Message);
            }
        }

        public CFIPClean83ExecutionResult ModifyPendingProtection(
            string brokerOrderId,
            double? stopLoss,
            double? takeProfit)
        {
            try
            {
                int id;
                if (!int.TryParse(brokerOrderId, out id))
                    return Failure("ORDER_ID_INVALID");

                PendingOrder order = null;
                foreach (var item in _host.PendingOrders)
                {
                    if (item.Id == id)
                    {
                        order = item;
                        break;
                    }
                }

                if (order == null)
                    return Failure("PENDING_ORDER_NOT_FOUND");

                if (stopLoss.HasValue)
                {
                    TradeResult stopResult =
                        order.ModifyStopLossPrice(stopLoss.Value);

                    if (!stopResult.IsSuccessful)
                        return Failure(
                            stopResult.Error != null
                                ? stopResult.Error.ToString()
                                : "PENDING_STOP_MODIFICATION_REJECTED");
                }

                if (takeProfit.HasValue)
                {
                    TradeResult targetResult =
                        order.ModifyTakeProfitPrice(takeProfit.Value);

                    if (!targetResult.IsSuccessful)
                        return Failure(
                            targetResult.Error != null
                                ? targetResult.Error.ToString()
                                : "PENDING_TARGET_MODIFICATION_REJECTED");
                }

                return Success(
                    "PENDING_PROTECTION_MODIFIED",
                    brokerOrderId,
                    string.Empty);
            }
            catch (Exception ex)
            {
                return Failure(ex.Message);
            }
        }

        public CFIPClean83ExecutionResult CancelPendingOrder(string brokerOrderId)
        {
            try
            {
                int id;
                if (!int.TryParse(brokerOrderId, out id))
                    return Failure("ORDER_ID_INVALID");

                PendingOrder found = null;
                foreach (var order in _host.PendingOrders)
                {
                    if (order.Id == id)
                    {
                        found = order;
                        break;
                    }
                }

                if (found == null)
                    return Failure("PENDING_ORDER_NOT_FOUND");

                TradeResult result = _host.CancelPendingOrder(found);

                return result.IsSuccessful
                    ? Success("PENDING_CANCEL_ACCEPTED", brokerOrderId, string.Empty)
                    : Failure(
                        result.Error != null
                            ? result.Error.ToString()
                            : "PENDING_CANCEL_REJECTED");
            }
            catch (Exception ex)
            {
                return Failure(ex.Message);
            }
        }

        private CFIPClean83ExecutionResult Success(
            string detail,
            string orderId,
            string positionId)
        {
            return new CFIPClean83ExecutionResult(
                true,
                orderId,
                positionId,
                null,
                0,
                string.Empty,
                CFIPClean83ProtectionState.Unknown,
                false,
                detail);
        }

        private CFIPClean83ExecutionResult Failure(string error)
        {
            return new CFIPClean83ExecutionResult(
                false,
                string.Empty,
                string.Empty,
                null,
                0,
                error,
                CFIPClean83ProtectionState.Unknown,
                false,
                "EXECUTION_FAILED");
        }
    }

    public sealed class CFIPClean83CTraderBrokerStateReader : ICFIPClean83BrokerStateReader
    {
        private readonly CFIP_MTF_LiveEntryEngine_Clean_v83 _host;

        public CFIPClean83CTraderBrokerStateReader(
            CFIP_MTF_LiveEntryEngine_Clean_v83 host)
        {
            _host = host ?? throw new ArgumentNullException("host");
        }

        public CFIPClean83BrokerStateSnapshot ReadManagedState(
            string symbol,
            string strategyId)
        {
            string label =
                _host.Configuration.Get(
                    "AutoTradeLabel",
                    "CFIP-SMART-CLEAN82");

            var positions = new List<CFIPClean83BrokerPositionSnapshot>();
            foreach (var position in _host.Positions)
            {
                if (!string.Equals(position.Label, label, StringComparison.Ordinal) ||
                    !string.Equals(position.SymbolName, symbol, StringComparison.Ordinal))
                    continue;

                positions.Add(
                    new CFIPClean83BrokerPositionSnapshot(
                        position.Id.ToString(),
                        position.Label,
                        position.SymbolName,
                        position.TradeType == TradeType.Buy
                            ? CFIPClean83Direction.Buy
                            : CFIPClean83Direction.Sell,
                        position.EntryPrice,
                        position.VolumeInUnits,
                        position.StopLoss,
                        position.TakeProfit,
                        position.NetProfit,
                        true));
            }

            var orders = new List<CFIPClean83BrokerPendingOrderSnapshot>();
            foreach (var order in _host.PendingOrders)
            {
                if (!string.Equals(order.Label, label, StringComparison.Ordinal) ||
                    !string.Equals(order.SymbolName, symbol, StringComparison.Ordinal))
                    continue;

                var kind =
                    order.OrderType == PendingOrderType.Stop
                        ? CFIPClean83ExecutionKind.Stop
                        : order.OrderType == PendingOrderType.Limit
                            ? CFIPClean83ExecutionKind.Limit
                            : CFIPClean83ExecutionKind.None;

                orders.Add(
                    new CFIPClean83BrokerPendingOrderSnapshot(
                        order.Id.ToString(),
                        order.Label,
                        order.SymbolName,
                        order.TradeType == TradeType.Buy
                            ? CFIPClean83Direction.Buy
                            : CFIPClean83Direction.Sell,
                        kind,
                        order.TargetPrice,
                        order.VolumeInUnits,
                        order.StopLoss,
                        order.TakeProfit,
                        true));
            }

            return new CFIPClean83BrokerStateSnapshot(
                positions,
                orders);
        }
    }


    // ------------------------------------------------------------------------
    // Phase 10 — first-class Pending Order lifecycle
    // ------------------------------------------------------------------------

    public enum CFIPClean83PendingOrderLifecycleState
    {
        Unknown = 0,
        Pending = 1,
        ProtectionRecoveryRequired = 2,
        Filled = 3,
        Cancelled = 4,
        Reconciled = 5
    }

    public enum CFIPClean83PendingOrderActionKind
    {
        None = 0,
        Cancel = 1,
        RestoreProtection = 2
    }

    public sealed class CFIPClean83PendingOrderRecord
    {
        public string BrokerOrderId { get; private set; }
        public string SignalId { get; private set; }
        public string PlanId { get; private set; }
        public CFIPClean83Direction Direction { get; private set; }
        public CFIPClean83ExecutionKind Kind { get; private set; }
        public double TargetPrice { get; private set; }
        public double? ExpectedStopLoss { get; private set; }
        public double? ExpectedTakeProfit { get; private set; }
        public DateTime CreatedUtc { get; private set; }
        public DateTime? ExpectedExpiryUtc { get; private set; }
        public DateTime? FilledUtc { get; private set; }
        public DateTime? CancelledUtc { get; private set; }
        public CFIPClean83PendingOrderLifecycleState State { get; private set; }

        public CFIPClean83PendingOrderRecord(
            string brokerOrderId,
            string signalId,
            string planId,
            CFIPClean83Direction direction,
            CFIPClean83ExecutionKind kind,
            double targetPrice,
            double? expectedStopLoss,
            double? expectedTakeProfit,
            DateTime createdUtc,
            DateTime? expectedExpiryUtc)
        {
            BrokerOrderId = brokerOrderId ?? string.Empty;
            SignalId = signalId ?? string.Empty;
            PlanId = planId ?? string.Empty;
            Direction = direction;
            Kind = kind;
            TargetPrice = targetPrice;
            ExpectedStopLoss = expectedStopLoss;
            ExpectedTakeProfit = expectedTakeProfit;
            CreatedUtc = createdUtc;
            ExpectedExpiryUtc = expectedExpiryUtc;
            State = CFIPClean83PendingOrderLifecycleState.Pending;
        }

        public void MarkProtectionRecoveryRequired()
        {
            State =
                CFIPClean83PendingOrderLifecycleState.ProtectionRecoveryRequired;
        }

        public void MarkFilled(DateTime utc)
        {
            FilledUtc = utc;
            State = CFIPClean83PendingOrderLifecycleState.Filled;
        }

        public void MarkCancelled(DateTime utc)
        {
            CancelledUtc = utc;
            State = CFIPClean83PendingOrderLifecycleState.Cancelled;
        }

        public void MarkReconciled()
        {
            State = CFIPClean83PendingOrderLifecycleState.Reconciled;
        }
    }

    public sealed class CFIPClean83PendingOrderAction
    {
        public string BrokerOrderId { get; private set; }
        public CFIPClean83PendingOrderActionKind Kind { get; private set; }
        public double? StopLoss { get; private set; }
        public double? TakeProfit { get; private set; }
        public string Reason { get; private set; }

        public CFIPClean83PendingOrderAction(
            string brokerOrderId,
            CFIPClean83PendingOrderActionKind kind,
            double? stopLoss,
            double? takeProfit,
            string reason)
        {
            BrokerOrderId = brokerOrderId ?? string.Empty;
            Kind = kind;
            StopLoss = stopLoss;
            TakeProfit = takeProfit;
            Reason = reason ?? string.Empty;
        }
    }

    public sealed class CFIPClean83PendingOrderLifecycleManager
    {
        private readonly string _managedLabel;
        private readonly Dictionary<string, CFIPClean83PendingOrderRecord> _records =
            new Dictionary<string, CFIPClean83PendingOrderRecord>(
                StringComparer.Ordinal);
        private readonly List<CFIPClean83PendingOrderAction> _actions =
            new List<CFIPClean83PendingOrderAction>();
        private readonly HashSet<string> _actionKeys =
            new HashSet<string>(StringComparer.Ordinal);

        public CFIPClean83PendingOrderLifecycleManager(
            string managedLabel)
        {
            _managedLabel = managedLabel ?? string.Empty;
        }

        public IReadOnlyList<CFIPClean83PendingOrderRecord> Records
        {
            get
            {
                return new ReadOnlyCollection<CFIPClean83PendingOrderRecord>(
                    new List<CFIPClean83PendingOrderRecord>(
                        _records.Values));
            }
        }

        public void RegisterExisting(
            PendingOrder order,
            DateTime utc)
        {
            if (!IsManaged(order))
                return;

            string key = order.Id.ToString();
            if (_records.ContainsKey(key))
                return;

            string signalId;
            string planId;
            ParseIdentity(
                order.Comment,
                out signalId,
                out planId);

            _records[key] =
                CreateRecord(
                    order,
                    utc,
                    signalId,
                    planId,
                    null);
        }

        public void RegisterSubmitted(
            PendingOrder order,
            CFIPClean83ExecutionIntent intent,
            DateTime utc)
        {
            if (order == null || intent == null)
                return;

            var record =
                new CFIPClean83PendingOrderRecord(
                    order.Id.ToString(),
                    intent.TradeIdentity != null
                        ? intent.TradeIdentity.SignalId
                        : string.Empty,
                    intent.TradeIdentity != null
                        ? intent.TradeIdentity.PlanId
                        : string.Empty,
                    intent.Direction,
                    intent.Kind,
                    order.TargetPrice,
                    intent.StopLoss != null
                        ? (double?)intent.StopLoss.Price
                        : null,
                    intent.EffectiveTarget != null
                        ? (double?)intent.EffectiveTarget.Price
                        : null,
                    utc,
                    intent.ExpiryUtc);

            _records[record.BrokerOrderId] = record;
            QueueProtectionRecoveryIfRequired(order, record);
        }

        public void HandleModified(PendingOrder order)
        {
            if (!IsManaged(order))
                return;

            CFIPClean83PendingOrderRecord record;
            if (!_records.TryGetValue(
                    order.Id.ToString(),
                    out record))
            {
                RegisterExisting(order, DateTime.MinValue);
                return;
            }

            if (!order.StopLoss.HasValue ||
                !order.TakeProfit.HasValue)
                QueueProtectionRecoveryIfRequired(
                    order,
                    record);
        }

        public void HandleFilled(
            PendingOrder order,
            Position position,
            DateTime utc)
        {
            if (order == null || position == null)
                return;

            CFIPClean83PendingOrderRecord record;
            if (!_records.TryGetValue(
                    order.Id.ToString(),
                    out record))
            {
                RegisterExisting(order, DateTime.MinValue);
                _records.TryGetValue(
                    order.Id.ToString(),
                    out record);
            }

            if (record != null)
            {
                record.MarkFilled(utc);

                if (!position.StopLoss.HasValue ||
                    !position.TakeProfit.HasValue)
                {
                    QueueProtectionRecoveryForRecord(record);
                }
            }
        }

        public void HandleCancelled(
            PendingOrder order,
            DateTime utc)
        {
            if (order == null)
                return;

            CFIPClean83PendingOrderRecord record;
            if (_records.TryGetValue(
                    order.Id.ToString(),
                    out record))
                record.MarkCancelled(utc);
        }

        public void Evaluate(
            DateTime utc,
            bool dailyLossBreached)
        {
            var snapshot =
                new List<CFIPClean83PendingOrderRecord>(
                    _records.Values);

            for (int i = 0; i < snapshot.Count; i++)
            {
                CFIPClean83PendingOrderRecord record =
                    snapshot[i];

                if (record.State ==
                    CFIPClean83PendingOrderLifecycleState.Filled ||
                    record.State ==
                    CFIPClean83PendingOrderLifecycleState.Cancelled ||
                    record.State ==
                    CFIPClean83PendingOrderLifecycleState.Reconciled)
                    continue;

                if (dailyLossBreached)
                {
                    Queue(
                        new CFIPClean83PendingOrderAction(
                            record.BrokerOrderId,
                            CFIPClean83PendingOrderActionKind.Cancel,
                            null,
                            null,
                            "DAILY_LOSS_LIMIT"));
                    continue;
                }

                if (record.ExpectedExpiryUtc.HasValue &&
                    record.ExpectedExpiryUtc.Value <= utc)
                {
                    Queue(
                        new CFIPClean83PendingOrderAction(
                            record.BrokerOrderId,
                            CFIPClean83PendingOrderActionKind.Cancel,
                            null,
                            null,
                            "EXPECTED_EXPIRY"));
                }
            }
        }

        public IReadOnlyList<CFIPClean83PendingOrderAction> DrainActions()
        {
            var result =
                new ReadOnlyCollection<CFIPClean83PendingOrderAction>(
                    new List<CFIPClean83PendingOrderAction>(
                        _actions));
            _actions.Clear();
            _actionKeys.Clear();
            return result;
        }

        private void QueueProtectionRecoveryForRecord(
            CFIPClean83PendingOrderRecord record)
        {
            if (record == null)
                return;

            if (!record.ExpectedStopLoss.HasValue &&
                !record.ExpectedTakeProfit.HasValue)
            {
                record.MarkProtectionRecoveryRequired();
                return;
            }

            Queue(
                new CFIPClean83PendingOrderAction(
                    record.BrokerOrderId,
                    CFIPClean83PendingOrderActionKind.RestoreProtection,
                    record.ExpectedStopLoss,
                    record.ExpectedTakeProfit,
                    "PENDING_OR_FILLED_PROTECTION_MISSING"));

            record.MarkProtectionRecoveryRequired();
        }

        private void QueueProtectionRecoveryIfRequired(
            PendingOrder order,
            CFIPClean83PendingOrderRecord record)
        {
            if (order.StopLoss.HasValue &&
                order.TakeProfit.HasValue)
                return;

            if (!record.ExpectedStopLoss.HasValue &&
                !record.ExpectedTakeProfit.HasValue)
            {
                record.MarkProtectionRecoveryRequired();
                return;
            }

            QueueProtectionRecoveryForRecord(record);
        }

        private void Queue(CFIPClean83PendingOrderAction action)
        {
            string key =
                action.BrokerOrderId +
                "|" +
                action.Kind.ToString();

            if (_actionKeys.Add(key))
                _actions.Add(action);
        }

        private CFIPClean83PendingOrderRecord CreateRecord(
            PendingOrder order,
            DateTime utc,
            string signalId,
            string planId,
            CFIPClean83ExecutionIntent intent)
        {
            return
                new CFIPClean83PendingOrderRecord(
                    order.Id.ToString(),
                    signalId,
                    planId,
                    order.TradeType == TradeType.Buy
                        ? CFIPClean83Direction.Buy
                        : CFIPClean83Direction.Sell,
                    order.OrderType == PendingOrderType.Stop
                        ? CFIPClean83ExecutionKind.Stop
                        : CFIPClean83ExecutionKind.Limit,
                    order.TargetPrice,
                    order.StopLoss,
                    order.TakeProfit,
                    utc == DateTime.MinValue
                        ? order.SubmittedTime
                        : utc,
                    intent != null
                        ? intent.ExpiryUtc
                        : order.ExpirationTime);
        }

        private void ParseIdentity(
            string comment,
            out string signalId,
            out string planId)
        {
            signalId = string.Empty;
            planId = string.Empty;

            if (string.IsNullOrWhiteSpace(comment))
                return;

            const string signalMarker = "CFIP83|SIGNAL|";
            const string planMarker = "CFIP83|PLAN|";

            int signalStart =
                comment.IndexOf(
                    signalMarker,
                    StringComparison.Ordinal);
            int planStart =
                comment.IndexOf(
                    planMarker,
                    StringComparison.Ordinal);

            if (signalStart >= 0)
            {
                int signalEnd =
                    planStart > signalStart
                        ? planStart - 1
                        : comment.Length;

                signalId =
                    comment.Substring(
                        signalStart,
                        Math.Max(
                            0,
                            signalEnd - signalStart));
            }

            if (planStart >= 0)
                planId =
                    comment.Substring(planStart);
        }

        private bool IsManaged(PendingOrder order)
        {
            return
                order != null &&
                string.Equals(
                    order.Label,
                    _managedLabel,
                    StringComparison.Ordinal) &&
                order.Comment != null &&
                order.Comment.IndexOf(
                    "CFIP83|",
                    StringComparison.Ordinal) >= 0;
        }
    }

    // ------------------------------------------------------------------------
    // Presentation boundary
    // ------------------------------------------------------------------------

    public sealed class CFIPClean83PresentationState
    {
        public DateTime GeneratedUtc { get; private set; }
        public CFIPClean83Direction Direction { get; private set; }
        public CFIPClean83LifecycleState LifecycleState { get; private set; }
        public string Readiness { get; private set; }
        public string ExecutionStatus { get; private set; }
        public string BrokerStatus { get; private set; }
        public string AlertStatus { get; private set; }

        public CFIPClean83PresentationState(
            DateTime generatedUtc,
            CFIPClean83Direction direction,
            CFIPClean83LifecycleState lifecycleState,
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

    public interface ICFIPClean83DecisionEngine
    {
        CFIPClean83DecisionSnapshot Evaluate(
            CFIPClean83RuntimeSnapshot runtime,
            CFIPClean83MtfSnapshot mtf,
            CFIPClean83MarketModel market,
            CFIPClean83StructureSnapshot structure,
            CFIPClean83ConfigSnapshot configuration);
    }

    public interface ICFIPClean83TradePlanBuilder
    {
        CFIPClean83TradePlan Build(
            CFIPClean83DecisionSnapshot decision,
            CFIPClean83EntrySnapshot entry,
            CFIPClean83MarketModel market,
            CFIPClean83StructureSnapshot structure,
            CFIPClean83MtfSnapshot mtf,
            CFIPClean83RuntimeSnapshot runtime,
            CFIPClean83ConfigSnapshot configuration);
    }

    public interface ICFIPClean83ExecutionPlanner
    {
        CFIPClean83ExecutionIntent CreateIntent(
            CFIPClean83TradePlan plan,
            CFIPClean83EntrySnapshot entry,
            CFIPClean83RuntimeSnapshot runtime,
            CFIPClean83ConfigSnapshot configuration,
            CFIPClean83ExecutionReadiness readiness);
    }

    public interface ICFIPClean83BrokerGateway
    {
        CFIPClean83ExecutionResult Execute(
            CFIPClean83ExecutionIntent intent);

        CFIPClean83ExecutionResult ModifyProtection(
            string brokerPositionId,
            double? stopLoss,
            double? takeProfit);

        CFIPClean83ExecutionResult ClosePosition(
            string brokerPositionId);

        CFIPClean83ExecutionResult CancelPendingOrder(
            string brokerOrderId);

        CFIPClean83ExecutionResult ModifyPendingProtection(
            string brokerOrderId,
            double? stopLoss,
            double? takeProfit);
    }

    public interface ICFIPClean83BrokerStateReader
    {
        CFIPClean83BrokerStateSnapshot ReadManagedState(
            string symbol,
            string strategyId);
    }

    public interface ICFIPClean83OutcomeRecorder
    {
        void Record(
            CFIPClean83OutcomeEvent outcome);
    }

    public interface ICFIPClean83PresentationProjector
    {
        CFIPClean83PresentationState Project(
            CFIPClean83RuntimeSnapshot runtime,
            CFIPClean83DecisionSnapshot decision,
            CFIPClean83TradePlan plan,
            CFIPClean83BrokerStateSnapshot broker,
            CFIPClean83LifecycleState lifecycle);
    }

    // ------------------------------------------------------------------------
    // Phase-4 engine shell
    // ------------------------------------------------------------------------

    internal sealed class CFIPClean83EngineState
    {
        public CFIPClean83RuntimeSnapshot Runtime { get; private set; }
        public CFIPClean83MtfSnapshot Mtf { get; private set; }
        public CFIPClean83MarketModel Market { get; private set; }
        public CFIPClean83StructureSnapshot Structure { get; private set; }
        public CFIPClean83DecisionSnapshot Decision { get; private set; }
        public CFIPClean83EntrySnapshot Entry { get; private set; }
        public CFIPClean83TradePlan Plan { get; private set; }
        public CFIPClean83ExecutionIntent Intent { get; private set; }
        public CFIPClean83ExecutionResult Execution { get; private set; }
        public CFIPClean83BrokerStateSnapshot Broker { get; private set; }
        public CFIPClean83PresentationState Presentation { get; private set; }

        public void SetRuntime(CFIPClean83RuntimeSnapshot value)
        {
            Runtime = value;
        }

        public void SetMtf(CFIPClean83MtfSnapshot value)
        {
            Mtf = value;
        }

        public void SetMarket(CFIPClean83MarketModel value)
        {
            Market = value;
        }

        public void SetStructure(CFIPClean83StructureSnapshot value)
        {
            Structure = value;
        }

        public void SetDecision(CFIPClean83DecisionSnapshot value)
        {
            Decision = value;
        }

        public void SetEntry(CFIPClean83EntrySnapshot value)
        {
            Entry = value;
        }

        public void SetPlan(CFIPClean83TradePlan value)
        {
            Plan = value;
        }

        // The cycle state is a downstream data carrier. Later phases extend
        // this with immutable cycle snapshots and explicit orchestration.
        public void ResetCycleOutputs()
        {
            // MTF is rebuilt on every Calculate cycle. Market and Structure
            // snapshots are intentionally retained until the closed-bar
            // reference changes, because their builders are reference-gated.
            Decision = null;
            Entry = null;
            Plan = null;
            Intent = null;
            Execution = null;
            Broker = null;
            Presentation = null;
        }
    }

    public enum CFIPClean83PanelCorner
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }

    public enum CFIPClean83SizingMode
    {
        RiskPercentEquity = 0,
        FixedLots = 1
    }

    public enum CFIPClean83PendingOrderMode
    {
        Adaptive = 0,
        ContinuationStop = 1,
        ReversalLimit = 2,
        Both = 3
    }

    // ------------------------------------------------------------------------
    // cTrader host adapter — Phase 7 wires Decision -> Entry only; no broker/UI authority.
    // ------------------------------------------------------------------------

    [Indicator(
        IsOverlay = true,
        TimeZone = TimeZones.UTC,
        AccessRights = AccessRights.None)]
    public class CFIP_MTF_LiveEntryEngine_Clean_v83 : Indicator
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
        // migrate to CFIPClean83ConfigSnapshot in subsequent phases.
        // No parameter is silently dropped during the architectural migration.
        // ====================================================================
        #region Migrated Configuration Surface (513 parameters)

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

// Compatibility parameter retained for preset parity. The clean Decision
        // engine does not interpret Trigger/Entry state; Phase 7 owns it.
[Parameter("Allow Strong Trigger Override", Group = "01 · Decision", DefaultValue = true)]
        public bool AllowStrongTriggerOverride { get; set; }

// Compatibility parameters retained for preset parity. M1 trigger and M5
        // execution confirmation belong to the Phase 7 Entry/Trigger owner.
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

[Parameter("Pending Order Mode", Group = "13 · AUTO TRADING", DefaultValue = CFIPClean83PendingOrderMode.Adaptive)]
        public CFIPClean83PendingOrderMode PendingOrderMode { get; set; }

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

[Parameter("Sizing Mode", Group = "13 · AUTO TRADING", DefaultValue = CFIPClean83SizingMode.RiskPercentEquity)]
        public CFIPClean83SizingMode SizingMode { get; set; }

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

[Parameter("Auto TP Stage", Group = "13 · AUTO TRADING", DefaultValue = CFIPClean83TargetStage.TP1)]
        public CFIPClean83TargetStage AutoTpStage { get; set; }

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

[Parameter("Panel Position", Group = "14 · DISPLAY — CORE", DefaultValue = CFIPClean83PanelCorner.BottomLeft)]
        public CFIPClean83PanelCorner PanelPosition { get; set; }

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

// Compatibility parameter retained for preset parity. v83 replaces the old
        // additive regime-weighting layer with deduplicated evidence plus
        // regime-adaptive quality/share/edge policy.
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

[Parameter("Popup Position", Group = "12 · ALERTS — ADVANCED", DefaultValue = CFIPClean83PanelCorner.TopRight)]
        public CFIPClean83PanelCorner PopupPosition { get; set; }

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

[Parameter("Aggressive TP Stage", Group = "13 · AUTO TRADING", DefaultValue = CFIPClean83TargetStage.TP1)]
        public CFIPClean83TargetStage AggressiveTpStage { get; set; }

[Parameter("Aggressive Require Smart Agreement", Group = "13 · AUTO TRADING", DefaultValue = true)]
        public bool AggressiveRequireSmartAgreement { get; set; }

        #endregion

        private CFIPClean83ConfigSnapshot _configuration;
        private CFIPClean83EngineState _state;
        private CFIPClean83LifecycleManager _lifecycle;
        private CFIPClean83RuntimeAuthority _runtimeAuthority;
        private CFIPClean83MarketModelBuilder _marketModelBuilder;
        private CFIPClean83StructureLedgerBuilder _structureBuilder;
        private CFIPClean83DecisionEngine _decisionEngine;
        private ICFIPClean83EntryTriggerEngine _entryTriggerEngine;
        private ICFIPClean83TradePlanBuilder _tradePlanBuilder;
        private DateTime _lastMarketReferenceUtc = DateTime.MinValue;

        public CFIPClean83ConfigSnapshot Configuration
        {
            get { return _configuration; }
        }

        public CFIPClean83RuntimeAuthority RuntimeAuthority
        {
            get { return _runtimeAuthority; }
        }

        public CFIPClean83LifecycleState LifecycleState
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
                CFIPClean83ConfigSnapshot.Build(
                    this,
                    "CFIP-PRO-v83");

            _runtimeAuthority =
                new CFIPClean83RuntimeAuthority();

            _runtimeAuthority.InitializeFromConfiguration(
                _configuration);

            _marketModelBuilder =
                new CFIPClean83MarketModelBuilder(
                    Indicators);

            _structureBuilder =
                new CFIPClean83StructureLedgerBuilder();

            _decisionEngine =
                new CFIPClean83DecisionEngine();

            _entryTriggerEngine =
                new CFIPClean83EntryTriggerEngine();

            _tradePlanBuilder =
                new CFIPClean83TradePlanBuilder();

            _executionPolicy =
                new CFIPClean83ExecutionPolicy();

            _executionPlanner =
                new CFIPClean83ExecutionPlanner();

            _brokerGateway =
                new CFIPClean83CTraderBrokerGateway(this);

            _brokerStateReader =
                new CFIPClean83CTraderBrokerStateReader(this);

            _pendingOrderLifecycle =
                new CFIPClean83PendingOrderLifecycleManager(
                    _configuration.Get(
                        "AutoTradeLabel",
                        "CFIP-SMART-CLEAN83"));

            PendingOrders.Created += PendingOrders_Created;
            PendingOrders.Modified += PendingOrders_Modified;
            PendingOrders.Filled += PendingOrders_Filled;
            PendingOrders.Cancelled += PendingOrders_Cancelled;

            foreach (var order in PendingOrders)
                _pendingOrderLifecycle.RegisterExisting(
                    order,
                    DateTime.MinValue);

            _state = new CFIPClean83EngineState();
            _lifecycle = new CFIPClean83LifecycleManager();
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
                new CFIPClean83RuntimeSnapshot(
                    serverUtc,
                    SymbolName,
                    Math.Max(0, Symbol.Bid),
                    Math.Max(0, Symbol.Ask),
                    Math.Max(0, Symbol.PipSize),
                    Symbol.PipSize > 0
                        ? Math.Max(
                            0,
                            (Symbol.Ask - Symbol.Bid) /
                            Symbol.PipSize)
                        : 0,
                    Server.IsConnected &&
                    Symbol.IsTradingEnabled,
                    Math.Max(0, Account.Equity),
                    Math.Max(0, Account.FreeMargin),
                    Math.Max(0, Account.Balance),
                    Math.Max(0, Account.Margin),
                    Account.MarginLevel.HasValue
                        ? Math.Max(0, Account.MarginLevel.Value)
                        : 0,
                    CalculateDailyRealizedNetProfit(serverUtc.Date),
                    serverUtc.Date,
                    new CFIPClean83BrokerConstraints(
                        Math.Max(0, Symbol.VolumeInUnitsMin),
                        Math.Max(0, Symbol.VolumeInUnitsStep),
                        ConvertMinimumDistanceToPips(
                            Symbol.MinStopLossDistance),
                        ConvertMinimumDistanceToPips(
                            Symbol.MinTakeProfitDistance)),
                    CountManagedPositions(),
                    CountManagedPendingOrders()));

            CFIPClean83MtfSnapshot mtf =
                CFIPClean83MtfSnapshotBuilder.Build(
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
                CFIPClean83MarketModel market =
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

                CFIPClean83StructureSnapshot structure =
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

            if (mtf.IsPrimaryDecisionReady &&
                _state.Market != null &&
                _state.Structure != null)
            {
                _state.SetDecision(
                    _decisionEngine.Evaluate(
                        _state.Runtime,
                        mtf,
                        _state.Market,
                        _state.Structure,
                        _configuration));

                _state.SetEntry(
                    _entryTriggerEngine.Evaluate(
                        _state.Decision,
                        _state.Runtime,
                        mtf,
                        _state.Market,
                        _state.Structure,
                        _configuration));

                _state.SetPlan(
                    _tradePlanBuilder.Build(
                        _state.Decision,
                        _state.Entry,
                        _state.Market,
                        _state.Structure,
                        mtf,
                        _state.Runtime,
                        _configuration));

                TryExecuteCurrentCycle();
            }

            ReconcileBrokerState();
            ProcessPendingOrderLifecycle();
        }

        private void TryExecuteCurrentCycle()
        {
            if (_state.Plan == null ||
                !_state.Plan.IsValid ||
                _state.Decision == null ||
                _state.Entry == null ||
                IsExecutionDuplicate(_state.Plan))
                return;

            double volume =
                CalculateNormalizedVolume(_state.Plan);

            double riskAmount =
                CalculateRiskAmount(_state.Plan, volume);

            TradeType tradeType =
                _state.Plan.Direction == CFIPClean83Direction.Buy
                    ? TradeType.Buy
                    : TradeType.Sell;

            double estimatedMargin =
                volume > 0
                    ? Symbol.GetEstimatedMargin(
                        tradeType,
                        volume)
                    : 0;

            var readiness =
                _executionPolicy.Evaluate(
                    _state.Decision,
                    _state.Entry,
                    _state.Plan,
                    _state.Runtime,
                    _configuration,
                    volume,
                    riskAmount,
                    estimatedMargin);

            if (!readiness.Eligible)
                return;

            _lifecycle.TryTransition(
                CFIPClean83LifecycleState.SignalDetected,
                _state.Runtime.ServerUtc,
                "DECISION_PLAN_READY");
            _lifecycle.TryTransition(
                CFIPClean83LifecycleState.PlanReady,
                _state.Runtime.ServerUtc,
                "TRADE_PLAN_VALID");
            _lifecycle.TryTransition(
                CFIPClean83LifecycleState.ExecutionReady,
                _state.Runtime.ServerUtc,
                "EXECUTION_POLICY_ACCEPTED");

            var intent =
                _executionPlanner.CreateIntent(
                    _state.Plan,
                    _state.Entry,
                    _state.Runtime,
                    _configuration,
                    readiness);

            _state.Intent = intent;

            var result =
                _brokerGateway.Execute(intent);

            _state.Execution = result;

            if (!result.Accepted)
            {
                _lifecycle.TryTransition(
                    CFIPClean83LifecycleState.Rejected,
                    _state.Runtime.ServerUtc,
                    "BROKER_EXECUTION_REJECTED");
                return;
            }

            _submittedExecutionKeys.Add(
                intent.Identity.IdempotencyKey);

            if (intent.Kind != CFIPClean83ExecutionKind.Market &&
                result.BrokerOrderId.Length > 0 &&
                _pendingOrderLifecycle != null)
            {
                foreach (var order in PendingOrders)
                {
                    if (order.Id.ToString() == result.BrokerOrderId)
                    {
                        _pendingOrderLifecycle.RegisterSubmitted(
                            order,
                            intent,
                            _state.Runtime.ServerUtc);
                        break;
                    }
                }
            }

            if (intent.Kind == CFIPClean83ExecutionKind.Market)
            {
                _lifecycle.TryTransition(
                    CFIPClean83LifecycleState.LivePosition,
                    _state.Runtime.ServerUtc,
                    "BROKER_MARKET_ACCEPTED");
            }
            else
            {
                _lifecycle.TryTransition(
                    CFIPClean83LifecycleState.PendingOrder,
                    _state.Runtime.ServerUtc,
                    "BROKER_PENDING_ACCEPTED");
            }

            if (result.ReconciliationRequired &&
                result.BrokerPositionId.Length > 0)
            {
                CFIPClean83ExecutionResult protectionResult =
                    _brokerGateway.ModifyProtection(
                        result.BrokerPositionId,
                        intent.StopLoss.Price,
                        intent.EffectiveTarget.Price);

                if (!protectionResult.Accepted)
                {
                    _state.Execution = protectionResult;
                    _lifecycle.TryTransition(
                        CFIPClean83LifecycleState.RecoveryRequired,
                        _state.Runtime.ServerUtc,
                        "BROKER_PROTECTION_RECOVERY_FAILED");
                }
                else
                {
                    _state.Execution = protectionResult;
                }
            }
        }

        private bool IsExecutionDuplicate(CFIPClean83TradePlan plan)
        {
            string label =
                _configuration.Get(
                    "AutoTradeLabel",
                    "CFIP-SMART-CLEAN82");
            string signal =
                plan.Identity.SignalId ?? string.Empty;

            foreach (var key in new[]
            {
                "CFIP82|EXEC|" + plan.Identity.PlanId + "|" + CFIPClean83ExecutionKind.Market,
                "CFIP82|EXEC|" + plan.Identity.PlanId + "|" + CFIPClean83ExecutionKind.Stop,
                "CFIP82|EXEC|" + plan.Identity.PlanId + "|" + CFIPClean83ExecutionKind.Limit
            })
            {
                if (_submittedExecutionKeys.Contains(key))
                    return true;
            }

            foreach (var position in Positions)
            {
                if (string.Equals(position.Label, label, StringComparison.Ordinal) &&
                    string.Equals(position.SymbolName, SymbolName, StringComparison.Ordinal) &&
                    (position.Comment ?? string.Empty).IndexOf(
                        signal,
                        StringComparison.Ordinal) >= 0)
                    return true;
            }

            foreach (var order in PendingOrders)
            {
                if (string.Equals(order.Label, label, StringComparison.Ordinal) &&
                    string.Equals(order.SymbolName, SymbolName, StringComparison.Ordinal) &&
                    (order.Comment ?? string.Empty).IndexOf(
                        signal,
                        StringComparison.Ordinal) >= 0)
                    return true;
            }

            if (_configuration.Get("OneOrderPerSignal", true))
            {
                HistoricalTrade[] trades =
                    History.FindAll(
                        label,
                        SymbolName);

                if (trades != null)
                {
                    for (int i = 0; i < trades.Length; i++)
                    {
                        if ((trades[i].Comment ?? string.Empty).IndexOf(
                                signal,
                                StringComparison.Ordinal) >= 0)
                            return true;
                    }
                }
            }

            return false;
        }

        private double CalculateNormalizedVolume(CFIPClean83TradePlan plan)
        {
            if (plan == null || Symbol.PipSize <= 0)
                return 0;

            double stopPips =
                Math.Abs(
                    plan.ExecutionAnchor.Price -
                    plan.StructuralStop.Price) /
                Symbol.PipSize;

            if (stopPips <= 0)
                return 0;

            double raw;

            if (_configuration.Get(
                    "SizingMode",
                    CFIPClean83SizingMode.RiskPercentEquity) ==
                CFIPClean83SizingMode.FixedLots)
            {
                raw =
                    Symbol.QuantityToVolumeInUnits(
                        Math.Max(
                            0.001,
                            _configuration.Get(
                                "FixedLots",
                                0.01)));
            }
            else
            {
                raw =
                    Symbol.VolumeForProportionalRisk(
                        ProportionalAmountType.Equity,
                        Math.Max(
                            0.01,
                            _configuration.Get(
                                "RiskPercentEquity",
                                0.50)),
                        stopPips,
                        RoundingMode.Down);
            }

            if (raw <= 0)
                return 0;

            raw =
                Symbol.NormalizeVolumeInUnits(
                    raw,
                    RoundingMode.Down);

            if (raw < Symbol.VolumeInUnitsMin)
                return 0;

            return Math.Min(
                raw,
                Symbol.NormalizeVolumeInUnits(
                    Symbol.VolumeInUnitsMax,
                    RoundingMode.Down));
        }

        private double CalculateRiskAmount(
            CFIPClean83TradePlan plan,
            double volume)
        {
            if (plan == null || volume <= 0 || Symbol.PipSize <= 0)
                return 0;

            double stopPips =
                Math.Abs(
                    plan.ExecutionAnchor.Price -
                    plan.StructuralStop.Price) /
                Symbol.PipSize;

            return stopPips > 0
                ? Symbol.AmountRisked(volume, stopPips)
                : 0;
        }

        private double ConvertMinimumDistanceToPips(double rawDistance)
        {
            if (rawDistance <= 0 || Symbol.PipSize <= 0)
                return 0;

            if (Symbol.MinDistanceType ==
                SymbolMinDistanceType.Pips)
                return rawDistance;

            double reference =
                Math.Max(Symbol.Ask, Symbol.Bid);

            return reference > 0
                ? reference * rawDistance / 100.0 / Symbol.PipSize
                : 0;
        }

        private int CountManagedPositions()
        {
            if (_configuration == null)
                return 0;

            string label =
                _configuration.Get(
                    "AutoTradeLabel",
                    "CFIP-SMART-CLEAN82");

            int count = 0;
            foreach (var position in Positions)
                if (string.Equals(position.Label, label, StringComparison.Ordinal) &&
                    string.Equals(position.SymbolName, SymbolName, StringComparison.Ordinal))
                    count++;
            return count;
        }

        private int CountManagedPendingOrders()
        {
            if (_configuration == null)
                return 0;

            string label =
                _configuration.Get(
                    "AutoTradeLabel",
                    "CFIP-SMART-CLEAN82");

            int count = 0;
            foreach (var order in PendingOrders)
                if (string.Equals(order.Label, label, StringComparison.Ordinal) &&
                    string.Equals(order.SymbolName, SymbolName, StringComparison.Ordinal))
                    count++;
            return count;
        }

        private double CalculateDailyRealizedNetProfit(DateTime dayStartUtc)
        {
            if (_configuration == null)
                return 0;

            string label =
                _configuration.Get(
                    "AutoTradeLabel",
                    "CFIP-SMART-CLEAN82");

            double total = 0;
            HistoricalTrade[] trades =
                History.FindAll(
                    label,
                    SymbolName);

            if (trades == null)
                return 0;

            for (int i = 0; i < trades.Length; i++)
            {
                HistoricalTrade trade = trades[i];

                if (trade.ClosingTime >= dayStartUtc &&
                    trade.ClosingTime < dayStartUtc.AddDays(1))
                    total += trade.NetProfit;
            }

            return total;
        }

        private void PendingOrders_Created(
            PendingOrderCreatedEventArgs args)
        {
            if (_pendingOrderLifecycle != null &&
                args != null &&
                args.PendingOrder != null)
                _pendingOrderLifecycle.RegisterExisting(
                    args.PendingOrder,
                    TimeInUtc);
        }

        private void PendingOrders_Modified(
            PendingOrderModifiedEventArgs args)
        {
            if (_pendingOrderLifecycle != null &&
                args != null &&
                args.PendingOrder != null)
                _pendingOrderLifecycle.HandleModified(
                    args.PendingOrder);
        }

        private void PendingOrders_Filled(
            PendingOrderFilledEventArgs args)
        {
            if (_pendingOrderLifecycle != null &&
                args != null &&
                args.PendingOrder != null &&
                args.Position != null)
            {
                _pendingOrderLifecycle.HandleFilled(
                    args.PendingOrder,
                    args.Position,
                    TimeInUtc);

                _lifecycle.TryTransition(
                    CFIPClean83LifecycleState.LivePosition,
                    TimeInUtc,
                    "PENDING_FILLED_TO_POSITION");
            }
        }

        private void PendingOrders_Cancelled(
            PendingOrderCancelledEventArgs args)
        {
            if (_pendingOrderLifecycle != null &&
                args != null &&
                args.PendingOrder != null)
            {
                _pendingOrderLifecycle.HandleCancelled(
                    args.PendingOrder,
                    TimeInUtc);

                ReconcileBrokerState();
            }
        }

        private void ProcessPendingOrderLifecycle()
        {
            if (_pendingOrderLifecycle == null ||
                _state == null ||
                _state.Runtime == null)
                return;

            _pendingOrderLifecycle.Evaluate(
                _state.Runtime.ServerUtc,
                IsDailyLossLimitBreached());

            IReadOnlyList<CFIPClean83PendingOrderAction> actions =
                _pendingOrderLifecycle.DrainActions();

            for (int i = 0; i < actions.Count; i++)
            {
                CFIPClean83PendingOrderAction action =
                    actions[i];

                CFIPClean83ExecutionResult result;

                if (action.Kind ==
                    CFIPClean83PendingOrderActionKind.Cancel)
                {
                    result =
                        _brokerGateway.CancelPendingOrder(
                            action.BrokerOrderId);
                }
                else if (action.Kind ==
                         CFIPClean83PendingOrderActionKind.RestoreProtection)
                {
                    result =
                        _brokerGateway.ModifyPendingProtection(
                            action.BrokerOrderId,
                            action.StopLoss,
                            action.TakeProfit);
                }
                else
                {
                    continue;
                }

                if (!result.Accepted)
                {
                    _lifecycle.TryTransition(
                        CFIPClean83LifecycleState.RecoveryRequired,
                        _state.Runtime.ServerUtc,
                        "PENDING_ACTION_FAILED_" +
                        action.Reason);
                }
            }
        }

        private bool IsDailyLossLimitBreached()
        {
            if (_configuration == null ||
                _state == null ||
                _state.Runtime == null ||
                !_configuration.Get(
                    "EnableDailyLossLimit",
                    true))
                return false;

            double baseline =
                _state.Runtime.Balance -
                _state.Runtime.DailyRealizedNetProfit;

            double limit =
                Math.Max(
                    0,
                    baseline) *
                Math.Max(
                    0,
                    _configuration.Get(
                        "MaximumDailyLossPercent",
                        3.0)) /
                100.0;

            return
                _state.Runtime.DailyRealizedNetProfit < -limit;
        }

        private void ReconcileBrokerState()
        {
            if (_brokerStateReader == null)
                return;

            _state.Broker =
                _brokerStateReader.ReadManagedState(
                    SymbolName,
                    _configuration.StrategyId);

            if (_state.Broker.Positions.Count > 0)
            {
                _lifecycle.TryTransition(
                    CFIPClean83LifecycleState.LivePosition,
                    _state.Runtime.ServerUtc,
                    "BROKER_POSITION_PRESENT");
            }
            else if (_state.Broker.PendingOrders.Count > 0)
            {
                _lifecycle.TryTransition(
                    CFIPClean83LifecycleState.PendingOrder,
                    _state.Runtime.ServerUtc,
                    "BROKER_PENDING_PRESENT");
            }
            else if (_lifecycle.State ==
                     CFIPClean83LifecycleState.LivePosition ||
                     _lifecycle.State ==
                     CFIPClean83LifecycleState.PendingOrder ||
                     _lifecycle.State ==
                     CFIPClean83LifecycleState.ExitRequested)
            {
                _lifecycle.TryTransition(
                    CFIPClean83LifecycleState.Closed,
                    _state.Runtime.ServerUtc,
                    "BROKER_OBJECTS_ABSENT");
            }
        }

    }
}
