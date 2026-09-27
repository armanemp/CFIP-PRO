// ============================================================================
// CFIP-PRO cTrader — v74 Clean Architecture Foundation
// Phase: 1 · Architecture foundation and canonical contracts
//
// This version intentionally does NOT copy the v73 monolith.
// v73 remains the frozen behavioral/reference baseline.
// v74 establishes the type/ownership boundaries that later phases will fill.
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

namespace cAlgo
{
    // ------------------------------------------------------------------------
    // Canonical scalar semantics
    // ------------------------------------------------------------------------

    public enum CFIPClean74Direction
    {
        Sell = -1,
        Wait = 0,
        Buy = 1
    }

    public static class CFIPClean74DirectionRules
    {
        public static bool IsDirectional(CFIPClean74Direction direction)
        {
            return direction != CFIPClean74Direction.Wait;
        }

        public static CFIPClean74Direction Opposite(CFIPClean74Direction direction)
        {
            if (direction == CFIPClean74Direction.Buy)
                return CFIPClean74Direction.Sell;

            if (direction == CFIPClean74Direction.Sell)
                return CFIPClean74Direction.Buy;

            return CFIPClean74Direction.Wait;
        }

        public static bool IsProtectiveMove(
            CFIPClean74Direction direction,
            double currentStop,
            double candidateStop)
        {
            if (!IsDirectional(direction) ||
                currentStop <= 0 ||
                candidateStop <= 0)
                return false;

            return direction == CFIPClean74Direction.Buy
                ? candidateStop > currentStop
                : candidateStop < currentStop;
        }
    }

    public enum CFIPClean74DecisionPolicyMode
    {
        Confirmed = 0,
        Soft = 1,
        Aggressive = 2,
        Pending = 3
    }

    public enum CFIPClean74ExecutionKind
    {
        None = 0,
        Market = 1,
        Stop = 2,
        Limit = 3
    }

    public enum CFIPClean74EntryMode
    {
        None = 0,
        RetestMarket = 1,
        BreakoutMarket = 2,
        ContinuationStop = 3,
        ReversalLimit = 4
    }

    public enum CFIPClean74LifecycleState
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

    public enum CFIPClean74BlockReason
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

    public enum CFIPClean74FallbackKind
    {
        None = 0,
        Structural = 1,
        HigherTimeframe = 2,
        ExecutionFrame = 3,
        Atr = 4,
        Synthetic = 5
    }

    public enum CFIPClean74TargetStage
    {
        TP1 = 1,
        TP2 = 2,
        TP3 = 3,
        TP4 = 4
    }

    public enum CFIPClean74ProtectionState
    {
        Unknown = 0,
        Unprotected = 1,
        PartiallyProtected = 2,
        FullyProtected = 3,
        RecoveryRequired = 4
    }

    public enum CFIPClean74BrokerObjectKind
    {
        None = 0,
        Position = 1,
        PendingOrder = 2
    }

    public enum CFIPClean74TargetState
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

    public sealed class CFIPClean74Provenance
    {
        public string Source { get; private set; }
        public string Rule { get; private set; }
        public CFIPClean74FallbackKind Fallback { get; private set; }
        public string Detail { get; private set; }

        public CFIPClean74Provenance(
            string source,
            string rule,
            CFIPClean74FallbackKind fallback,
            string detail)
        {
            Source = source ?? string.Empty;
            Rule = rule ?? string.Empty;
            Fallback = fallback;
            Detail = detail ?? string.Empty;
        }

        public static CFIPClean74Provenance Direct(
            string source,
            string rule)
        {
            return new CFIPClean74Provenance(
                source,
                rule,
                CFIPClean74FallbackKind.None,
                string.Empty);
        }

        public static CFIPClean74Provenance FallbackFrom(
            string source,
            string rule,
            CFIPClean74FallbackKind fallback,
            string detail)
        {
            return new CFIPClean74Provenance(
                source,
                rule,
                fallback,
                detail);
        }
    }

    // ------------------------------------------------------------------------
    // Price / zone primitives
    // ------------------------------------------------------------------------

    public sealed class CFIPClean74PriceLevel
    {
        public double Price { get; private set; }
        public string Name { get; private set; }
        public CFIPClean74Provenance Provenance { get; private set; }

        public CFIPClean74PriceLevel(
            double price,
            string name,
            CFIPClean74Provenance provenance)
        {
            if (price <= 0)
                throw new ArgumentOutOfRangeException("price");

            Price = price;
            Name = name ?? string.Empty;
            Provenance = provenance ??
                         CFIPClean74Provenance.Direct(
                             "UNKNOWN",
                             "UNSPECIFIED");
        }
    }

    public sealed class CFIPClean74PriceZone
    {
        public double Lower { get; private set; }
        public double Upper { get; private set; }
        public string Name { get; private set; }
        public CFIPClean74Provenance Provenance { get; private set; }

        public CFIPClean74PriceZone(
            double lower,
            double upper,
            string name,
            CFIPClean74Provenance provenance)
        {
            if (lower <= 0 || upper <= 0 || upper < lower)
                throw new ArgumentException("Invalid price zone.");

            Lower = lower;
            Upper = upper;
            Name = name ?? string.Empty;
            Provenance = provenance ??
                         CFIPClean74Provenance.Direct(
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

    public sealed class CFIPClean74EntryModel
    {
        public CFIPClean74Direction Direction { get; private set; }

        // Preferred price inside the structural execution area.
        public CFIPClean74PriceLevel IdealEntry { get; private set; }

        // Allowed structural retest region.
        public CFIPClean74PriceZone EntryZone { get; private set; }

        // Structural activation threshold for breakout/continuation.
        public CFIPClean74PriceLevel Trigger { get; private set; }

        // Exact strategy/broker protection boundary.
        public CFIPClean74PriceLevel Invalidation { get; private set; }

        public CFIPClean74EntryModel(
            CFIPClean74Direction direction,
            CFIPClean74PriceLevel idealEntry,
            CFIPClean74PriceZone entryZone,
            CFIPClean74PriceLevel trigger,
            CFIPClean74PriceLevel invalidation)
        {
            if (!CFIPClean74DirectionRules.IsDirectional(direction))
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

    public sealed class CFIPClean74TargetLevel
    {
        public CFIPClean74TargetStage Stage { get; private set; }
        public CFIPClean74PriceLevel Level { get; private set; }
        public CFIPClean74TargetState State { get; private set; }
        public int Quality { get; private set; }

        public CFIPClean74TargetLevel(
            CFIPClean74TargetStage stage,
            CFIPClean74PriceLevel level,
            int quality,
            CFIPClean74TargetState state)
        {
            Stage = stage;
            Level = level;
            Quality = Math.Max(0, Math.Min(100, quality));
            State = state;
        }
    }

    public sealed class CFIPClean74TargetLadder
    {
        private readonly ReadOnlyCollection<CFIPClean74TargetLevel> _levels;

        public IReadOnlyList<CFIPClean74TargetLevel> Levels
        {
            get { return _levels; }
        }

        public CFIPClean74TargetLadder(
            IList<CFIPClean74TargetLevel> levels)
        {
            if (levels == null)
                throw new ArgumentNullException("levels");

            var copy =
                new List<CFIPClean74TargetLevel>(
                    levels);

            copy.Sort(
                delegate (
                    CFIPClean74TargetLevel left,
                    CFIPClean74TargetLevel right)
                {
                    return left.Stage.CompareTo(right.Stage);
                });

            _levels =
                new ReadOnlyCollection<CFIPClean74TargetLevel>(
                    copy);
        }

        public bool ValidateForDirection(
            CFIPClean74Direction direction)
        {
            if (!CFIPClean74DirectionRules.IsDirectional(direction) ||
                _levels.Count == 0)
                return false;

            for (int i = 1; i < _levels.Count; i++)
            {
                double previous =
                    _levels[i - 1].Level.Price;

                double current =
                    _levels[i].Level.Price;

                if (direction == CFIPClean74Direction.Buy &&
                    current <= previous)
                    return false;

                if (direction == CFIPClean74Direction.Sell &&
                    current >= previous)
                    return false;
            }

            return true;
        }

        public CFIPClean74TargetLevel Find(
            CFIPClean74TargetStage stage)
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

    public sealed class CFIPClean74BrokerConstraints
    {
        public double MinVolumeInUnits { get; private set; }
        public double VolumeStepInUnits { get; private set; }
        public double MinStopDistancePips { get; private set; }
        public double MinTakeProfitDistancePips { get; private set; }

        public CFIPClean74BrokerConstraints(
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

    public sealed class CFIPClean74RuntimeSnapshot
    {
        public DateTime ServerUtc { get; private set; }
        public string Symbol { get; private set; }
        public double Bid { get; private set; }
        public double Ask { get; private set; }
        public double SpreadPips { get; private set; }
        public bool SymbolTradingEnabled { get; private set; }
        public double Equity { get; private set; }
        public double FreeMargin { get; private set; }
        public CFIPClean74BrokerConstraints BrokerConstraints { get; private set; }
        public int ManagedPositionCount { get; private set; }
        public int ManagedPendingOrderCount { get; private set; }

        public CFIPClean74RuntimeSnapshot(
            DateTime serverUtc,
            string symbol,
            double bid,
            double ask,
            double spreadPips,
            bool symbolTradingEnabled,
            double equity,
            double freeMargin,
            CFIPClean74BrokerConstraints brokerConstraints,
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

    public sealed class CFIPClean74MtfSnapshot
    {
        public DateTime ReferenceUtc { get; private set; }
        public int M1ClosedIndex { get; private set; }
        public int M5ClosedIndex { get; private set; }
        public int M15ClosedIndex { get; private set; }
        public int M30ClosedIndex { get; private set; }
        public int H1ClosedIndex { get; private set; }
        public int H4ClosedIndex { get; private set; }
        public int D1ClosedIndex { get; private set; }
        public int W1ClosedIndex { get; private set; }
        public bool IsComplete { get; private set; }
        public string DataStatus { get; private set; }

        public CFIPClean74MtfSnapshot(
            DateTime referenceUtc,
            int m1ClosedIndex,
            int m5ClosedIndex,
            int m15ClosedIndex,
            int m30ClosedIndex,
            int h1ClosedIndex,
            int h4ClosedIndex,
            int d1ClosedIndex,
            int w1ClosedIndex,
            bool isComplete,
            string dataStatus)
        {
            ReferenceUtc = referenceUtc;
            M1ClosedIndex = m1ClosedIndex;
            M5ClosedIndex = m5ClosedIndex;
            M15ClosedIndex = m15ClosedIndex;
            M30ClosedIndex = m30ClosedIndex;
            H1ClosedIndex = h1ClosedIndex;
            H4ClosedIndex = h4ClosedIndex;
            D1ClosedIndex = d1ClosedIndex;
            W1ClosedIndex = w1ClosedIndex;
            IsComplete = isComplete;
            DataStatus = dataStatus ?? string.Empty;
        }
    }

    // ------------------------------------------------------------------------
    // Market model
    // ------------------------------------------------------------------------

    public sealed class CFIPClean74MarketFrame
    {
        public string Timeframe { get; private set; }
        public DateTime ClosedBarTimeUtc { get; private set; }
        public double Open { get; private set; }
        public double High { get; private set; }
        public double Low { get; private set; }
        public double Close { get; private set; }
        public double Atr { get; private set; }
        public double Rsi { get; private set; }
        public double Adx { get; private set; }
        public double EmaFast { get; private set; }
        public double EmaSlow { get; private set; }
        public int DirectionScore { get; private set; }

        public CFIPClean74MarketFrame(
            string timeframe,
            DateTime closedBarTimeUtc,
            double open,
            double high,
            double low,
            double close,
            double atr,
            double rsi,
            double adx,
            double emaFast,
            double emaSlow,
            int directionScore)
        {
            Timeframe = timeframe ?? string.Empty;
            ClosedBarTimeUtc = closedBarTimeUtc;
            Open = open;
            High = high;
            Low = low;
            Close = close;
            Atr = Math.Max(0, atr);
            Rsi = Math.Max(0, Math.Min(100, rsi));
            Adx = Math.Max(0, Math.Min(100, adx));
            EmaFast = emaFast;
            EmaSlow = emaSlow;
            DirectionScore =
                Math.Max(-100, Math.Min(100, directionScore));
        }
    }

    public sealed class CFIPClean74StructureEvent
    {
        public string EventType { get; private set; }
        public CFIPClean74Direction Direction { get; private set; }
        public string Timeframe { get; private set; }
        public DateTime TimeUtc { get; private set; }
        public double Price { get; private set; }
        public int Quality { get; private set; }
        public CFIPClean74Provenance Provenance { get; private set; }

        public CFIPClean74StructureEvent(
            string eventType,
            CFIPClean74Direction direction,
            string timeframe,
            DateTime timeUtc,
            double price,
            int quality,
            CFIPClean74Provenance provenance)
        {
            EventType = eventType ?? string.Empty;
            Direction = direction;
            Timeframe = timeframe ?? string.Empty;
            TimeUtc = timeUtc;
            Price = price;
            Quality = Math.Max(0, Math.Min(100, quality));
            Provenance =
                provenance ??
                CFIPClean74Provenance.Direct(
                    "UNKNOWN",
                    "UNSPECIFIED");
        }
    }

    public sealed class CFIPClean74ZoneSnapshot
    {
        public string Id { get; private set; }
        public string Type { get; private set; }
        public CFIPClean74Direction Direction { get; private set; }
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
        public CFIPClean74Provenance Provenance { get; private set; }

        public CFIPClean74ZoneSnapshot(
            string id,
            string type,
            CFIPClean74Direction direction,
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
            CFIPClean74Provenance provenance)
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
                CFIPClean74Provenance.Direct(
                    "UNKNOWN",
                    "UNSPECIFIED");
        }
    }

    public sealed class CFIPClean74LiquiditySnapshot
    {
        public string Id { get; private set; }
        public string Type { get; private set; }
        public CFIPClean74Direction SweepDirection { get; private set; }
        public string Timeframe { get; private set; }
        public double Price { get; private set; }
        public double Distance { get; private set; }
        public bool Swept { get; private set; }
        public int Quality { get; private set; }
        public CFIPClean74Provenance Provenance { get; private set; }

        public CFIPClean74LiquiditySnapshot(
            string id,
            string type,
            CFIPClean74Direction sweepDirection,
            string timeframe,
            double price,
            double distance,
            bool swept,
            int quality,
            CFIPClean74Provenance provenance)
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
                CFIPClean74Provenance.Direct(
                    "UNKNOWN",
                    "UNSPECIFIED");
        }
    }

    public sealed class CFIPClean74MarketModel
    {
        private readonly ReadOnlyCollection<CFIPClean74MarketFrame> _frames;
        private readonly ReadOnlyCollection<CFIPClean74StructureEvent> _structure;
        private readonly ReadOnlyCollection<CFIPClean74ZoneSnapshot> _zones;
        private readonly ReadOnlyCollection<CFIPClean74LiquiditySnapshot> _liquidity;

        public IReadOnlyList<CFIPClean74MarketFrame> Frames
        {
            get { return _frames; }
        }

        public IReadOnlyList<CFIPClean74StructureEvent> Structure
        {
            get { return _structure; }
        }

        public IReadOnlyList<CFIPClean74ZoneSnapshot> Zones
        {
            get { return _zones; }
        }

        public IReadOnlyList<CFIPClean74LiquiditySnapshot> Liquidity
        {
            get { return _liquidity; }
        }

        public CFIPClean74MarketModel(
            IList<CFIPClean74MarketFrame> frames,
            IList<CFIPClean74StructureEvent> structure,
            IList<CFIPClean74ZoneSnapshot> zones,
            IList<CFIPClean74LiquiditySnapshot> liquidity)
        {
            _frames =
                new ReadOnlyCollection<CFIPClean74MarketFrame>(
                    new List<CFIPClean74MarketFrame>(frames ?? new List<CFIPClean74MarketFrame>()));

            _structure =
                new ReadOnlyCollection<CFIPClean74StructureEvent>(
                    new List<CFIPClean74StructureEvent>(structure ?? new List<CFIPClean74StructureEvent>()));

            _zones =
                new ReadOnlyCollection<CFIPClean74ZoneSnapshot>(
                    new List<CFIPClean74ZoneSnapshot>(zones ?? new List<CFIPClean74ZoneSnapshot>()));

            _liquidity =
                new ReadOnlyCollection<CFIPClean74LiquiditySnapshot>(
                    new List<CFIPClean74LiquiditySnapshot>(liquidity ?? new List<CFIPClean74LiquiditySnapshot>()));
        }
    }

    // ------------------------------------------------------------------------
    // Decision
    // ------------------------------------------------------------------------

    public sealed class CFIPClean74DecisionSnapshot
    {
        private readonly ReadOnlyCollection<CFIPClean74BlockReason> _blockReasons;

        public CFIPClean74Direction Direction { get; private set; }
        public int Confidence { get; private set; }
        public int Quality { get; private set; }
        public int Edge { get; private set; }
        public int MtfAgreement { get; private set; }
        public int IndependentEvidence { get; private set; }
        public int StructuralConfirmations { get; private set; }
        public string Regime { get; private set; }
        public int RegimeQuality { get; private set; }
        public bool EntryEligible { get; private set; }
        public CFIPClean74DecisionPolicyMode PolicyMode { get; private set; }
        public IReadOnlyList<CFIPClean74BlockReason> BlockReasons
        {
            get { return _blockReasons; }
        }
        public CFIPClean74Provenance Provenance { get; private set; }

        public CFIPClean74DecisionSnapshot(
            CFIPClean74Direction direction,
            int confidence,
            int quality,
            int edge,
            int mtfAgreement,
            int independentEvidence,
            int structuralConfirmations,
            string regime,
            int regimeQuality,
            bool entryEligible,
            CFIPClean74DecisionPolicyMode policyMode,
            IList<CFIPClean74BlockReason> blockReasons,
            CFIPClean74Provenance provenance)
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
                new ReadOnlyCollection<CFIPClean74BlockReason>(
                    new List<CFIPClean74BlockReason>(
                        blockReasons ??
                        new List<CFIPClean74BlockReason>()));
            Provenance =
                provenance ??
                CFIPClean74Provenance.Direct(
                    "UNKNOWN",
                    "UNSPECIFIED");
        }
    }

    // ------------------------------------------------------------------------
    // Trade identity / idempotency
    // ------------------------------------------------------------------------

    public sealed class CFIPClean74TradeIdentity
    {
        public string StrategyId { get; private set; }
        public string Symbol { get; private set; }
        public string PlanId { get; private set; }
        public string SignalId { get; private set; }

        public CFIPClean74TradeIdentity(
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

    public sealed class CFIPClean74ExecutionIdentity
    {
        public string IntentId { get; private set; }
        public string IdempotencyKey { get; private set; }
        public DateTime CreatedUtc { get; private set; }

        public CFIPClean74ExecutionIdentity(
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

    public sealed class CFIPClean74TradePlan
    {
        public CFIPClean74TradeIdentity Identity { get; private set; }
        public CFIPClean74Direction Direction { get; private set; }
        public CFIPClean74EntryMode EntryMode { get; private set; }
        public CFIPClean74EntryModel Entry { get; private set; }
        public CFIPClean74PriceLevel StructuralStop { get; private set; }
        public CFIPClean74TargetLadder TargetLadder { get; private set; }
        public double RiskRewardToTp1 { get; private set; }
        public double RiskRewardToFinalTarget { get; private set; }
        public int LevelQuality { get; private set; }
        public bool IsValid { get; private set; }
        public DateTime CreatedUtc { get; private set; }
        public int ReferenceBarIndex { get; private set; }
        public CFIPClean74Provenance Provenance { get; private set; }

        public CFIPClean74TradePlan(
            CFIPClean74TradeIdentity identity,
            CFIPClean74Direction direction,
            CFIPClean74EntryMode entryMode,
            CFIPClean74EntryModel entry,
            CFIPClean74PriceLevel structuralStop,
            CFIPClean74TargetLadder targetLadder,
            double riskRewardToTp1,
            double riskRewardToFinalTarget,
            int levelQuality,
            bool isValid,
            DateTime createdUtc,
            int referenceBarIndex,
            CFIPClean74Provenance provenance)
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
                CFIPClean74Provenance.Direct(
                    "UNKNOWN",
                    "UNSPECIFIED");
        }
    }

    // ------------------------------------------------------------------------
    // Execution intent / result
    // ------------------------------------------------------------------------

    public sealed class CFIPClean74RiskRequest
    {
        public double RiskPercentEquity { get; private set; }
        public double RequestedVolumeInUnits { get; private set; }
        public double MaxRiskAmount { get; private set; }

        public CFIPClean74RiskRequest(
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

    public sealed class CFIPClean74ExecutionIntent
    {
        public CFIPClean74ExecutionIdentity Identity { get; private set; }
        public CFIPClean74TradeIdentity TradeIdentity { get; private set; }
        public CFIPClean74Direction Direction { get; private set; }
        public CFIPClean74ExecutionKind Kind { get; private set; }
        public CFIPClean74EntryMode EntryMode { get; private set; }

        // Exact value requested from the broker.
        public CFIPClean74PriceLevel RequestedEntry { get; private set; }

        // Structural activation threshold, when applicable.
        public CFIPClean74PriceLevel Trigger { get; private set; }

        // Protection requested at execution time.
        public CFIPClean74PriceLevel StopLoss { get; private set; }

        // This is the requested active broker target, not the whole ladder.
        public CFIPClean74PriceLevel EffectiveTarget { get; private set; }

        public double VolumeInUnits { get; private set; }
        public CFIPClean74RiskRequest Risk { get; private set; }
        public CFIPClean74DecisionPolicyMode PolicyMode { get; private set; }
        public DateTime CreatedUtc { get; private set; }
        public DateTime? ExpiryUtc { get; private set; }

        public CFIPClean74ExecutionIntent(
            CFIPClean74ExecutionIdentity identity,
            CFIPClean74TradeIdentity tradeIdentity,
            CFIPClean74Direction direction,
            CFIPClean74ExecutionKind kind,
            CFIPClean74EntryMode entryMode,
            CFIPClean74PriceLevel requestedEntry,
            CFIPClean74PriceLevel trigger,
            CFIPClean74PriceLevel stopLoss,
            CFIPClean74PriceLevel effectiveTarget,
            double volumeInUnits,
            CFIPClean74RiskRequest risk,
            CFIPClean74DecisionPolicyMode policyMode,
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

    public sealed class CFIPClean74ExecutionResult
    {
        public bool Accepted { get; private set; }
        public string BrokerOrderId { get; private set; }
        public string BrokerPositionId { get; private set; }
        public CFIPClean74PriceLevel ActualFill { get; private set; }
        public double RequestedVsActualDelta { get; private set; }
        public string BrokerError { get; private set; }
        public CFIPClean74ProtectionState ProtectionState { get; private set; }
        public bool ReconciliationRequired { get; private set; }

        public CFIPClean74ExecutionResult(
            bool accepted,
            string brokerOrderId,
            string brokerPositionId,
            CFIPClean74PriceLevel actualFill,
            double requestedVsActualDelta,
            string brokerError,
            CFIPClean74ProtectionState protectionState,
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

    public sealed class CFIPClean74BrokerPositionSnapshot
    {
        public string BrokerPositionId { get; private set; }
        public string Label { get; private set; }
        public string Symbol { get; private set; }
        public CFIPClean74Direction Direction { get; private set; }
        public double EntryPrice { get; private set; }
        public double VolumeInUnits { get; private set; }
        public double? StopLoss { get; private set; }
        public double? TakeProfit { get; private set; }
        public double NetProfit { get; private set; }
        public bool IsOpen { get; private set; }

        public CFIPClean74BrokerPositionSnapshot(
            string brokerPositionId,
            string label,
            string symbol,
            CFIPClean74Direction direction,
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

    public sealed class CFIPClean74BrokerPendingOrderSnapshot
    {
        public string BrokerOrderId { get; private set; }
        public string Label { get; private set; }
        public string Symbol { get; private set; }
        public CFIPClean74Direction Direction { get; private set; }
        public CFIPClean74ExecutionKind Kind { get; private set; }
        public double RequestedEntry { get; private set; }
        public double VolumeInUnits { get; private set; }
        public double? StopLoss { get; private set; }
        public double? TakeProfit { get; private set; }
        public bool IsActive { get; private set; }

        public CFIPClean74BrokerPendingOrderSnapshot(
            string brokerOrderId,
            string label,
            string symbol,
            CFIPClean74Direction direction,
            CFIPClean74ExecutionKind kind,
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

    public sealed class CFIPClean74BrokerStateSnapshot
    {
        private readonly ReadOnlyCollection<CFIPClean74BrokerPositionSnapshot> _positions;
        private readonly ReadOnlyCollection<CFIPClean74BrokerPendingOrderSnapshot> _pendingOrders;

        public IReadOnlyList<CFIPClean74BrokerPositionSnapshot> Positions
        {
            get { return _positions; }
        }

        public IReadOnlyList<CFIPClean74BrokerPendingOrderSnapshot> PendingOrders
        {
            get { return _pendingOrders; }
        }

        public CFIPClean74BrokerStateSnapshot(
            IList<CFIPClean74BrokerPositionSnapshot> positions,
            IList<CFIPClean74BrokerPendingOrderSnapshot> pendingOrders)
        {
            _positions =
                new ReadOnlyCollection<CFIPClean74BrokerPositionSnapshot>(
                    new List<CFIPClean74BrokerPositionSnapshot>(
                        positions ??
                        new List<CFIPClean74BrokerPositionSnapshot>()));

            _pendingOrders =
                new ReadOnlyCollection<CFIPClean74BrokerPendingOrderSnapshot>(
                    new List<CFIPClean74BrokerPendingOrderSnapshot>(
                        pendingOrders ??
                        new List<CFIPClean74BrokerPendingOrderSnapshot>()));
        }
    }

    // ------------------------------------------------------------------------
    // Lifecycle authority
    // ------------------------------------------------------------------------

    public sealed class CFIPClean74LifecycleTransition
    {
        public CFIPClean74LifecycleState From { get; private set; }
        public CFIPClean74LifecycleState To { get; private set; }
        public DateTime TimeUtc { get; private set; }
        public string Reason { get; private set; }

        public CFIPClean74LifecycleTransition(
            CFIPClean74LifecycleState from,
            CFIPClean74LifecycleState to,
            DateTime timeUtc,
            string reason)
        {
            From = from;
            To = to;
            TimeUtc = timeUtc;
            Reason = reason ?? string.Empty;
        }
    }

    public sealed class CFIPClean74LifecycleManager
    {
        private CFIPClean74LifecycleState _state;

        public CFIPClean74LifecycleState State
        {
            get { return _state; }
        }

        public CFIPClean74LifecycleManager()
        {
            _state = CFIPClean74LifecycleState.Flat;
        }

        public bool TryTransition(
            CFIPClean74LifecycleState target,
            DateTime timeUtc,
            string reason)
        {
            if (!IsAllowed(_state, target))
                return false;

            _state = target;
            return true;
        }

        private static bool IsAllowed(
            CFIPClean74LifecycleState from,
            CFIPClean74LifecycleState to)
        {
            if (from == to)
                return true;

            switch (from)
            {
                case CFIPClean74LifecycleState.Flat:
                    return
                        to == CFIPClean74LifecycleState.SignalDetected ||
                        to == CFIPClean74LifecycleState.Closed;

                case CFIPClean74LifecycleState.SignalDetected:
                    return
                        to == CFIPClean74LifecycleState.PlanReady ||
                        to == CFIPClean74LifecycleState.Rejected ||
                        to == CFIPClean74LifecycleState.Flat;

                case CFIPClean74LifecycleState.PlanReady:
                    return
                        to == CFIPClean74LifecycleState.ExecutionReady ||
                        to == CFIPClean74LifecycleState.Rejected ||
                        to == CFIPClean74LifecycleState.Flat;

                case CFIPClean74LifecycleState.ExecutionReady:
                    return
                        to == CFIPClean74LifecycleState.PendingOrder ||
                        to == CFIPClean74LifecycleState.LivePosition ||
                        to == CFIPClean74LifecycleState.Rejected ||
                        to == CFIPClean74LifecycleState.Error;

                case CFIPClean74LifecycleState.PendingOrder:
                    return
                        to == CFIPClean74LifecycleState.LivePosition ||
                        to == CFIPClean74LifecycleState.RecoveryRequired ||
                        to == CFIPClean74LifecycleState.Flat ||
                        to == CFIPClean74LifecycleState.Error;

                case CFIPClean74LifecycleState.LivePosition:
                    return
                        to == CFIPClean74LifecycleState.ExitRequested ||
                        to == CFIPClean74LifecycleState.RecoveryRequired ||
                        to == CFIPClean74LifecycleState.Closed ||
                        to == CFIPClean74LifecycleState.Error;

                case CFIPClean74LifecycleState.ExitRequested:
                    return
                        to == CFIPClean74LifecycleState.Closed ||
                        to == CFIPClean74LifecycleState.RecoveryRequired ||
                        to == CFIPClean74LifecycleState.Error;

                case CFIPClean74LifecycleState.RecoveryRequired:
                    return
                        to == CFIPClean74LifecycleState.LivePosition ||
                        to == CFIPClean74LifecycleState.Closed ||
                        to == CFIPClean74LifecycleState.Error ||
                        to == CFIPClean74LifecycleState.Flat;

                case CFIPClean74LifecycleState.Closed:
                    return
                        to == CFIPClean74LifecycleState.SignalDetected ||
                        to == CFIPClean74LifecycleState.Flat;

                case CFIPClean74LifecycleState.Rejected:
                    return
                        to == CFIPClean74LifecycleState.Flat ||
                        to == CFIPClean74LifecycleState.SignalDetected;

                case CFIPClean74LifecycleState.Error:
                    return
                        to == CFIPClean74LifecycleState.RecoveryRequired ||
                        to == CFIPClean74LifecycleState.Flat;

                default:
                    return false;
            }
        }
    }

    // ------------------------------------------------------------------------
    // Configuration / runtime separation
    // ------------------------------------------------------------------------

    public sealed class CFIPClean74ExecutionEnvelope
    {
        public double MaxEntryChaseAtr { get; private set; }
        public double MaxBrokerSlippagePips { get; private set; }
        public double MaxBreakoutFillDeviationPips { get; private set; }
        public double MaxPlanRebaseDistancePips { get; private set; }

        public CFIPClean74ExecutionEnvelope(
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

    public sealed class CFIPClean74ConfigSnapshot
    {
        public string StrategyId { get; private set; }
        public bool AutoTradingEnabled { get; private set; }
        public bool AutomaticOrdersEnabled { get; private set; }
        public bool ManageBrokerProtection { get; private set; }
        public int MinimumConfidence { get; private set; }
        public int MinimumQuality { get; private set; }
        public double MinimumTradeRr { get; private set; }
        public CFIPClean74ExecutionEnvelope ExecutionEnvelope { get; private set; }

        public CFIPClean74ConfigSnapshot(
            string strategyId,
            bool autoTradingEnabled,
            bool automaticOrdersEnabled,
            bool manageBrokerProtection,
            int minimumConfidence,
            int minimumQuality,
            double minimumTradeRr,
            CFIPClean74ExecutionEnvelope executionEnvelope)
        {
            StrategyId = strategyId ?? string.Empty;
            AutoTradingEnabled = autoTradingEnabled;
            AutomaticOrdersEnabled = automaticOrdersEnabled;
            ManageBrokerProtection = manageBrokerProtection;
            MinimumConfidence =
                Math.Max(0, Math.Min(100, minimumConfidence));
            MinimumQuality =
                Math.Max(0, Math.Min(100, minimumQuality));
            MinimumTradeRr = Math.Max(0, minimumTradeRr);
            ExecutionEnvelope =
                executionEnvelope ??
                throw new ArgumentNullException("executionEnvelope");
        }

        public static CFIPClean74ConfigSnapshot Default(string strategyId)
        {
            return new CFIPClean74ConfigSnapshot(
                strategyId,
                false,
                false,
                true,
                72,
                70,
                2.0,
                new CFIPClean74ExecutionEnvelope(
                    0.45,
                    2.0,
                    8.0,
                    12.0));
        }
    }

    // ------------------------------------------------------------------------
    // Outcome contract
    // ------------------------------------------------------------------------

    public sealed class CFIPClean74OutcomeEvent
    {
        public CFIPClean74TradeIdentity TradeIdentity { get; private set; }
        public double EntryPrice { get; private set; }
        public double ExitPrice { get; private set; }
        public double ResultAmount { get; private set; }
        public double ResultR { get; private set; }
        public double MaxFavorableExcursion { get; private set; }
        public double MaxAdverseExcursion { get; private set; }
        public CFIPClean74TargetStage HighestTargetStageReached { get; private set; }
        public string ExitReason { get; private set; }
        public DateTime EntryUtc { get; private set; }
        public DateTime ExitUtc { get; private set; }

        public CFIPClean74OutcomeEvent(
            CFIPClean74TradeIdentity tradeIdentity,
            double entryPrice,
            double exitPrice,
            double resultAmount,
            double resultR,
            double maxFavorableExcursion,
            double maxAdverseExcursion,
            CFIPClean74TargetStage highestTargetStageReached,
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

    public sealed class CFIPClean74PresentationState
    {
        public DateTime GeneratedUtc { get; private set; }
        public CFIPClean74Direction Direction { get; private set; }
        public CFIPClean74LifecycleState LifecycleState { get; private set; }
        public string Readiness { get; private set; }
        public string ExecutionStatus { get; private set; }
        public string BrokerStatus { get; private set; }
        public string AlertStatus { get; private set; }

        public CFIPClean74PresentationState(
            DateTime generatedUtc,
            CFIPClean74Direction direction,
            CFIPClean74LifecycleState lifecycleState,
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

    public interface ICFIPClean74DecisionEngine
    {
        CFIPClean74DecisionSnapshot Evaluate(
            CFIPClean74RuntimeSnapshot runtime,
            CFIPClean74MtfSnapshot mtf,
            CFIPClean74MarketModel market,
            CFIPClean74ConfigSnapshot configuration);
    }

    public interface ICFIPClean74TradePlanBuilder
    {
        CFIPClean74TradePlan Build(
            CFIPClean74DecisionSnapshot decision,
            CFIPClean74MarketModel market,
            CFIPClean74RuntimeSnapshot runtime,
            CFIPClean74ConfigSnapshot configuration);
    }

    public interface ICFIPClean74ExecutionPlanner
    {
        CFIPClean74ExecutionIntent CreateIntent(
            CFIPClean74TradePlan plan,
            CFIPClean74RuntimeSnapshot runtime,
            CFIPClean74ConfigSnapshot configuration);
    }

    public interface ICFIPClean74BrokerGateway
    {
        CFIPClean74ExecutionResult Execute(
            CFIPClean74ExecutionIntent intent);

        CFIPClean74ExecutionResult ModifyProtection(
            string brokerPositionId,
            double? stopLoss,
            double? takeProfit);

        CFIPClean74ExecutionResult ClosePosition(
            string brokerPositionId);

        CFIPClean74ExecutionResult CancelPendingOrder(
            string brokerOrderId);
    }

    public interface ICFIPClean74BrokerStateReader
    {
        CFIPClean74BrokerStateSnapshot ReadManagedState(
            string symbol,
            string strategyId);
    }

    public interface ICFIPClean74OutcomeRecorder
    {
        void Record(
            CFIPClean74OutcomeEvent outcome);
    }

    public interface ICFIPClean74PresentationProjector
    {
        CFIPClean74PresentationState Project(
            CFIPClean74RuntimeSnapshot runtime,
            CFIPClean74DecisionSnapshot decision,
            CFIPClean74TradePlan plan,
            CFIPClean74BrokerStateSnapshot broker,
            CFIPClean74LifecycleState lifecycle);
    }

    // ------------------------------------------------------------------------
    // Phase-1 engine shell
    // ------------------------------------------------------------------------

    internal sealed class CFIPClean74EngineState
    {
        public CFIPClean74RuntimeSnapshot Runtime { get; private set; }
        public CFIPClean74MtfSnapshot Mtf { get; private set; }
        public CFIPClean74MarketModel Market { get; private set; }
        public CFIPClean74DecisionSnapshot Decision { get; private set; }
        public CFIPClean74TradePlan Plan { get; private set; }
        public CFIPClean74ExecutionIntent Intent { get; private set; }
        public CFIPClean74ExecutionResult Execution { get; private set; }
        public CFIPClean74BrokerStateSnapshot Broker { get; private set; }
        public CFIPClean74PresentationState Presentation { get; private set; }

        public void SetRuntime(CFIPClean74RuntimeSnapshot value)
        {
            Runtime = value;
        }

        // Phase 1 intentionally exposes only data slots. Later phases replace
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

    // ------------------------------------------------------------------------
    // cTrader host adapter — no broker mutation and no UI authority in Phase 1.
    // ------------------------------------------------------------------------

    [Indicator(
        IsOverlay = true,
        TimeZone = TimeZones.UTC,
        AccessRights = AccessRights.None)]
    public class CFIP_MTF_LiveEntryEngine_Clean_v74 : Indicator
    {
        private CFIPClean74ConfigSnapshot _configuration;
        private CFIPClean74EngineState _state;
        private CFIPClean74LifecycleManager _lifecycle;

        public CFIPClean74ConfigSnapshot Configuration
        {
            get { return _configuration; }
        }

        public CFIPClean74LifecycleState LifecycleState
        {
            get { return _lifecycle.State; }
        }

        protected override void Initialize()
        {
            _configuration =
                CFIPClean74ConfigSnapshot.Default(
                    "CFIP-PRO-v74");

            _state = new CFIPClean74EngineState();
            _lifecycle = new CFIPClean74LifecycleManager();
        }

        public override void Calculate(int index)
        {
            // Phase 1 only establishes the runtime boundary.
            // MTF resolution, analysis, execution and rendering are intentionally
            // introduced in their dedicated later phases.
            double bid =
                Math.Max(0, Symbol.Bid);

            double ask =
                Math.Max(0, Symbol.Ask);

            double spreadPips =
                Symbol.PipSize > 0
                    ? Math.Max(
                        0,
                        (ask - bid) / Symbol.PipSize)
                    : 0;

            _state.ResetCycleOutputs();

            _state.SetRuntime(
                new CFIPClean74RuntimeSnapshot(
                    TimeInUtc,
                    SymbolName,
                    bid,
                    ask,
                    spreadPips,
                    true,
                    0,
                    0,
                    new CFIPClean74BrokerConstraints(
                        0,
                        0,
                        0,
                        0),
                    0,
                    0));
        }
    }
}
