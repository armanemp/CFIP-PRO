
// ============================================================================
// CFIP-PRO · Clean Unified MTF Live Entry Engine v70
// ============================================================================
// Structural precision execution release.
// v66: unified execution-plan coordination, runtime toggle synchronization,
// actual-entry structural repricing, and chart/broker execution consistency.
//
// Decision and execution are separate:
//   Direction -> execution zone -> precise entry -> structural stop
//   -> HTF/liquidity reward ladder -> validated RR envelope.
//
// SL/TP are not fixed-distance or simple trailing levels. Structural changes
// on closed bars can reprice protection/targets; tick movement alone cannot
// invent a new structural target or stop.
// ============================================================================
// v49 changes:
//  - FIX (stage/RR alignment): SelectTargets() used to append only the
//    stages that found a qualifying level, so a missing TP1 candidate
//    silently shifted the TP2/TP3/TP4-grade level into the TP1 slot in
//    BuildPlan, SelectStructuralAutoTarget, and the live/early-prediction
//    target refresh. Now every stage keeps its own fixed slot (null when
//    empty), so TP1..TP4 always reflect the RR tier they were meant to.
//  - Hardened all three SelectTargets() consumers against the now-possible
//    null slots (no behavior change when every stage has a candidate).
//  - Panel hide/show: confirmed the existing small square toggle button
//    (top-left of the panel header, "-"/"+" ) is wired correctly end to
//    end; left as-is since it already matches the requested behavior.
// ============================================================================
// v50 changes (Phase 1 of the multi-phase review — see chat for the full
// phase plan):
//  - Hide/show toggle MOVED from the panel header to the bottom button row,
//    left of CLOSE/CANCEL, per request. It stays visible even when "Show
//    Trade Action Buttons" is off (was previously at risk of being hidden
//    along with that row before this fix), and its width is now reserved
//    before Close/Cancel split the remaining space, so nothing overlaps.
//  - Signal accuracy: added a D1 (daily) opposition check to the
//    higher-timeframe confidence penalty. Previously only H1/H4 disagreement
//    with the decided direction reduced confidence; a setup could pass with
//    H1/H4 aligned while fighting the daily trend outright. D1 opposition
//    now applies the same penalty plus an extra half-penalty, since it is
//    the most consequential disagreement of the three.
// ============================================================================
// v51 changes (continuing the phase plan — trigger timeframe & reward
// tuned for the user's stated M5-trigger, HTF-reward, day-trading setup):
//  - "M1 Trigger" now defaults OFF. The real trigger check already runs on
//    M5 closes (M5OnlyConfirmedTrigger); M1 only added noise-prone bias and
//    a veto. Trigger timeframe is now cleanly M5 by default. Still there
//    as an opt-in for anyone who wants it.
//  - Reward tuned upward for the "use HTF for bigger reward" request:
//    HTF Reward Bonus 18 -> 22 (HTF-confirmed levels now win target
//    selection more often), Maximum Reward RR ceiling 10.0 -> 12.0 (lets
//    a genuinely strong HTF target pay out further when structure
//    supports it). TP1-4 minimum-RR floors and the ATR-based target
//    distance cap were left untouched on purpose — those are what keep
//    targets realistically reachable within a trading day; only the
//    ceiling and the HTF preference moved.
// ============================================================================
// v52 changes (Phase 2 of the plan — MTF structure & zones):
//  - Added an alert-only (never auto-close) end-of-day warning for day
//    traders: when a managed position is still open inside
//    "End Of Day Alert Minutes Before" of "Session End UTC", it fires once
//    per day through the existing sound/popup/email channels asking the
//    user to review/close manually. New parameters: "Enable End Of Day
//    Alert" (default on) and "End Of Day Alert Minutes Before" (default 30).
//  - FIX (CFIP-BUG-FVG-RESET): BuildManagedFvgZone's mitigation-tracking
//    loop, when FvgInvalidateOnFullFill was OFF, reset a fully-swept FVG
//    back to its ORIGINAL pristine size instead of recognizing it as
//    consumed — so a gap price had already traded straight through could
//    be reported as a fresh, full-size zone to live entry/SL/TP selection.
//    It now collapses to zero width on a full breach in both modes, so the
//    existing post-loop guard excludes it correctly either way. Verified
//    the equivalent Order Block zone builder did not have this bug.
// ============================================================================
// v53 changes (Phase 2 continued — liquidity & order-block review):
//  - Verified FindEqualHigh/FindEqualLow (bucketed equal-high/low detection)
//    are correct and symmetric — no changes needed.
//  - ENHANCEMENT: BullLiquiditySweep/BearLiquiditySweep used to count ANY
//    penetration beyond the prior extreme as a valid sweep, even a
//    single-tick wick indistinguishable from noise or spread. Added a
//    minimum penetration depth (new "Liquidity Sweep Minimum Depth ATR"
//    parameter, default 0.05x ATR, floored by the live spread) so only
//    genuine stop-hunt wicks count — fewer false liquidity-sweep signals.
//  - Reviewed Order Block quality scoring (displacement, structure break,
//    liquidity sweep, FVG confluence, remaining ratio, body/impulse ratio,
//    age decay) — the composite weighting is sound, no bug found.
// ============================================================================
// v54 changes (Phase 3 — entry precision & execution model):
//  - Reviewed BuildExecutionModel (M5/M15 FVG+OB confluence zone, ideal
//    entry, trigger/invalidation, inside-zone vs breakout logic),
//    PremiumDiscountBias, and TriggerReadyWithoutPrecisionGate in depth —
//    all structurally sound, no bugs found.
//  - ENHANCEMENT: added an M30-alignment quality bonus to the precision
//    entry score, mirroring the existing M15 bonus (M30 direction was
//    already computed elsewhere in the engine but never checked here) —
//    a precision entry can no longer score high confluence while fighting
//    the M30 trend outright.
// ============================================================================
// v55 changes (Phase 4 — risk engine):
//  - Reviewed CalculateProtectedStop (break-even, structural swing trail,
//    smart-exit tightening, final monotonic BetterStop guard),
//    SpreadAllowed, and CalculateVolume/VolumeForFixedRisk position sizing
//    (including optional spread-inclusive risk sizing) — all sound, no
//    bugs found.
//  - ENHANCEMENT: added a daily loss circuit-breaker. New parameters
//    "Enable Daily Loss Limit" (default on) and "Maximum Daily Loss
//    Percent" (default 3.0%). Once today's equity drawdown from the day's
//    starting equity reaches that percentage, new auto-trade entries
//    (normal and aggressive) are blocked for the rest of the day and a
//    one-time alert fires — open positions are never touched, consistent
//    with the alert-only/no-auto-close preference already in place.
// ============================================================================
// v56 changes (Phase 5 — reward engine):
//  - Reviewed BuildTargetLevels (swing/opposing-FVG/opposing-OB/supply-
//    demand/liquidity/HTF/previous-period/smart-extra target aggregation)
//    and the TP-hit alert/telemetry path — all sound.
//  - Found the real limiter on reward: SyncBrokerTakeProfit always kept the
//    live broker take-profit fixed at one configured stage (Auto TP Stage,
//    TP2 by default) for the entire life of the trade, so a move with real
//    structural room for TP3/TP4 always closed early at TP2.
//  - ENHANCEMENT: added "Enable Dynamic TP Advance" (default off — opt-in
//    since it changes live trade-management behavior) + "TP Advance
//    Proximity Percent" (default 80). When enabled, once price has covered
//    that percentage of the distance to the current effective stage AND a
//    further stage has a genuinely farther valid price, the live target
//    ratchets forward to that stage instead of closing there. Only moves
//    forward, resets to the base stage on every new trade, and never
//    touches position size — no partial closes were added.
// ============================================================================
// v57 changes (Phase 6 — alerts & auto trading):
//  - FIX (CFIP-BUG-ALERT-DEDUP): SendUnifiedAlert's duplicate-suppression
//    compared every incoming key against a single shared "last alert key",
//    so any two DIFFERENT alert kinds firing back-to-back (e.g. a TP1 alert
//    then a smart/reaction alert) reset that tracker and silently defeated
//    the per-key cooldown — the same alert could then re-fire immediately
//    instead of waiting out its cooldown. Cooldowns are now tracked per key
//    in a dictionary, with light housekeeping (entries older than 24h are
//    pruned once the table passes 500 keys) so it can't grow unbounded over
//    a long-running session.
//  - Reviewed PassesAutoTradeSafetyGuards (market hours, margin), and
//    confirmed TryAggressiveAutoTrade runs through the same safety guards,
//    IsAutoPlanValid, and (as of v55) DailyLossLimitHit as the normal auto-
//    trade path — no weaker gate found on the aggressive side.
// ============================================================================
// v58 changes (auto-trading, heavily extended per request):
//  - MAJOR ENHANCEMENT: real multi-stage partial take-profit (scale-out).
//    New parameters "Enable Partial Take Profit" (default off — changes
//    live trade management), "Partial Close At TP1 Percent" (default 33),
//    "Partial Close At TP2 Percent" (default 33), and "Move To Break Even
//    After Partial" (default on). When enabled, the moment price actually
//    reaches TP1/TP2 on a normal auto-trade, that percentage of the
//    ORIGINAL fill size is genuinely closed (via ClosePosition's partial
//    overload) — locking in real, realized profit — while the remainder
//    keeps running toward the live broker target (TP2/TP3/TP4, and can
//    still ratchet forward under Dynamic TP Advance from v56). After a
//    partial fires, the stop is moved to break-even (only if that's
//    actually an improvement — never loosens it). Volume math is rounded
//    to the broker's step and never leaves an unbrokerable dust remainder
//    (closes the rest outright instead). This only applies to the normal
//    auto-trade path (which tracks a Plan with Tp1-4) — the aggressive
//    auto-entry path has no Plan/TP ladder to scale out of, so it is
//    unaffected and keeps its existing single-target behavior.
// ============================================================================
// v64 changes (Phase 4 · executable trading path + UI state authority):
//  - Auto Trade now attempts to create an eligible plan and continue in the
//    same calculation cycle instead of requiring a later tick/bar for the
//    execution path.
//  - Quick controls use native ToggleButton Checked/Unchecked events and an
//    explicit re-entrancy guard, keeping UI state and runtime parameter state
//    synchronized without switching themselves off during a permission prompt.
//  - Added execution diagnostics to the panel so blocked/rejected execution
//    exposes its current reason rather than appearing silently inactive.
//  - Margin safety uses the same reserved-margin calculation in volume sizing
//    and final execution validation.
//  - Structural SL validation now respects cTrader's symbol minimum SL distance.
//  - Line labels are positioned above the line with bottom alignment, while
//    staying left-anchored so the text does not sit across the line.
// ============================================================================

// v70 changes (Phase 1 · lifecycle/runtime hardening):
//  - Preserves v69 behavior while separating runtime quick-control state from
//    public cTrader configuration parameters.
//  - Live telemetry timeout can no longer terminate a still-open managed
//    position.
//  - Partial take-profit mutations now require broker success before their
//    hit-state is consumed, allowing transient failures to retry.
//  - Adds explicit telemetry-timeout state without taking ownership away from
//    the broker lifecycle.
// ============================================================================

// v66 changes (Phase 5 · runtime/UI authority + full execution/chart sync):
//  - Added independent runtime execution flags. The cTrader parameters remain
//    configuration inputs; quick controls now change runtime authority and
//    therefore immediately gate actual auto-trade/pending execution.
//  - Quick controls use the native ToggleButton Click event and checked value;
//    programmatic synchronization is guarded to prevent feedback loops.
//  - Execution/lifecycle operations now complete before chart/panel rendering,
//    eliminating same-cycle stale trigger/SL/TP/pending visuals.
//  - Auto-trading and automatic-order status are isolated; pending-order
//    failures no longer overwrite the general auto-trading state.
//  - Panel geometry uses one exact quick-control row height, exact margins and
//    effective panel width; close/cancel visibility follows safety-button rules.
//  - Level labels obey the same visibility settings as their corresponding
//    lines.
//  - Active broker TP is marked separately only when it differs from the
//    structural TP ladder; the panel reports its current TP stage.
//  - Broker stop/target state is synchronized after protection/lifecycle work
//    so the final visual state is consistent with actual broker state.
// ============================================================================

// v64 changes (Phase 3 · execution synchronization + chart precision):
//  - Trigger remains internal for breakout/continuation activation and pending
//    Stop placement, but is hidden from live-position drawings because the
//    position is already active; it remains visible for pre-entry/pending use.
//  - All chart line styles are forced to LineStyle.Solid. Configurable style
//    parameters were removed to keep the visual contract deterministic.
//  - Level labels are anchored near the left edge and right-aligned so the
//    complete label text appears to the LEFT of its line.
//  - Normal market-entry plans are rebased to the actual broker fill price;
//    structural SL and the full TP ladder are re-evaluated from that fill.
//  - Reversal protection now uses IsManagedPosition(), including custom
//    ManagedPositionLabel ownership.
//  - Added profit-exhaustion protection after meaningful profit and confirmed
//    adverse structural/reaction retracement; generic broker trailing remains off.
// ============================================================================

// v62 changes (Phase 2 · smart execution + runtime integrity):
//  - Fixed both CS0120 errors: Parameter DefaultValue uses static enum constants.
//  - Added Market Suitability scoring for session, spread, volatility, trend/MTF,
//    ADX, chop, daily pivot, event/news and evidence context.
//  - Auto Trading and Automatic Orders remain independent and both use the
//    same execution-quality suitability gate.
//  - Risk sizing scales downward from configured risk; margin protection remains
//    authoritative and generic cTrader trailing remains explicitly disabled.
//  - Close/Cancel controls use the same managed identity as the execution engine
//    and do not erase state when the broker rejects/retains the object.
//  - Managed Position Label is honored consistently.
//  - Suitability runtime recalculation is throttled for responsiveness/performance.
//  - v61 remains unchanged; v62 is the independent continuation.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;

namespace cAlgo
{
    public enum CFIPClean70PanelCorner
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }

    public enum CFIPClean70SizingMode
    {
        RiskPercentEquity = 0,
        FixedLots = 1
    }

    public enum CFIPClean70TargetStage
    {
        TP1 = 0,
        TP2 = 1,
        TP3 = 2,
        TP4 = 3
    }

    public enum CFIPClean70PendingOrderMode
    {
        Adaptive = 0,
        ContinuationStop = 1,
        ReversalLimit = 2,
        Both = 3
    }

    public enum CFIPClean70ExecutionMode
    {
        None = 0,
        WaitingForTrigger = 1,
        RetestMarket = 2,
        BreakoutMarket = 3,
        ContinuationStop = 4,
        ReversalLimit = 5
    }

    public enum CFIPClean70LifecycleState
    {
        Flat = 0,
        Signal = 1,
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

    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class CFIP_MTF_LiveEntryEngine_Clean_v70 : Indicator
    {
        #region Parameters · Decision
        // ============================================================

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

        #endregion

        #region Parameters · MTF
        // ============================================================

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

        #endregion

        #region Parameters · Structure
        // ============================================================

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

        #endregion

        #region Parameters · Zones
        // ============================================================

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

        #endregion

        #region Parameters · Liquidity
        // ============================================================

        [Parameter("Liquidity Lookback", Group = "05 · Liquidity", DefaultValue = 40, MinValue = 10, MaxValue = 150)]
        public int LiquidityLookback { get; set; }

        [Parameter("Use Equal High / Low", Group = "05 · Liquidity", DefaultValue = true)]
        public bool UseEqualHighLow { get; set; }

        [Parameter("Equal Level Tolerance ATR", Group = "05 · Liquidity", DefaultValue = 0.12, MinValue = 0.02, MaxValue = 0.5)]
        public double EqualLevelToleranceAtr { get; set; }

        [Parameter("Use Liquidity Sweep", Group = "05 · Liquidity", DefaultValue = true)]
        public bool UseLiquiditySweep { get; set; }

        // ENHANCEMENT (signal accuracy): BullLiquiditySweep/BearLiquiditySweep
        // used to accept ANY penetration beyond the prior extreme, even a
        // single tick — indistinguishable from ordinary noise or spread.
        // This sets a minimum penetration depth (in ATR) for a wick to count
        // as a genuine stop-hunt/liquidity grab.
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

        #endregion

        #region Parameters · Indicators
        // ============================================================

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

        #endregion

        #region Parameters · Entry Precision
        // ============================================================

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

        #endregion

        #region Parameters · Smart Weights
        // ============================================================

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

        #endregion

        #region Parameters · Risk & Targets
        // ============================================================

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

        #endregion

        #region Parameters · Live Management
        // ============================================================

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

        #endregion

        #region Parameters · Filters
        // ============================================================

        [Parameter("Use Session Filter", Group = "11 · Filters", DefaultValue = false)]
        public bool UseSessionFilter { get; set; }

        [Parameter("Session Start UTC", Group = "11 · Filters", DefaultValue = 6, MinValue = 0, MaxValue = 23)]
        public int SessionStartUtc { get; set; }

        [Parameter("Session End UTC", Group = "11 · Filters", DefaultValue = 20, MinValue = 0, MaxValue = 23)]
        public int SessionEndUtc { get; set; }

        // ENHANCEMENT (day trading): a one-shot alert (sound/popup/email —
        // whichever channels are already enabled below) as the trading day
        // approaches its close, so a day trader gets a heads-up to review or
        // close open positions manually. This is deliberately alert-only —
        // no position is ever closed automatically. Uses the existing
        // "Session End UTC" above as the day's close reference so there is
        // no duplicate time setting to keep in sync.
        [Parameter("Enable End Of Day Alert", Group = "11 · Filters", DefaultValue = true)]
        public bool EnableEndOfDayAlert { get; set; }

        [Parameter("End Of Day Alert Minutes Before", Group = "11 · Filters", DefaultValue = 30, MinValue = 5, MaxValue = 180)]
        public int EndOfDayAlertMinutesBefore { get; set; }

        // ENHANCEMENT: actually closes managed positions at the configured
        // day-close (in addition to, not instead of, the warning above).
        // Defaults ON per explicit request; turn off to keep alert-only
        // behavior.
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

        #endregion

        #region Parameters · Alerts
        // ============================================================

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

        #endregion

        #region Parameters · Auto Trading
        // ============================================================

        [Parameter("Enable Auto Trading", Group = "13 · AUTO TRADING", DefaultValue = false)]
        public bool EnableAutoTrading { get; set; }

        [Parameter("Enable Automatic Orders", Group = "13 · AUTO TRADING", DefaultValue = false)]
        public bool EnableAutomaticOrders { get; set; }

        [Parameter("Pending Order Mode", Group = "13 · AUTO TRADING", DefaultValue = CFIPClean70PendingOrderMode.Adaptive)]
        public CFIPClean70PendingOrderMode PendingOrderMode { get; set; }

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

        [Parameter("Sizing Mode", Group = "13 · AUTO TRADING", DefaultValue = CFIPClean70SizingMode.RiskPercentEquity)]
        public CFIPClean70SizingMode SizingMode { get; set; }

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

        [Parameter("Auto TP Stage", Group = "13 · AUTO TRADING", DefaultValue = CFIPClean70TargetStage.TP1)]
        public CFIPClean70TargetStage AutoTpStage { get; set; }

        // ENHANCEMENT (reward engine, Phase 5): the broker take-profit was
        // always synced to one fixed stage (TP2 by default) for the whole
        // life of the trade, so a move that reached TP2 but had genuine
        // structural room for TP3/TP4 closed early every time. When
        // enabled, once price has covered "TP Advance Proximity Percent" of
        // the distance to the current effective stage AND a further stage
        // has a genuinely farther valid price, the live target ratchets
        // forward to that stage instead of closing there. It only ever
        // moves forward (never back to an earlier, closer stage) and
        // starts back at "Auto TP Stage" on every new trade.
        [Parameter("Enable Dynamic TP Advance", Group = "13 · AUTO TRADING", DefaultValue = true)]
        public bool EnableDynamicTpAdvance { get; set; }

        [Parameter("TP Advance Proximity Percent", Group = "13 · AUTO TRADING", DefaultValue = 72, MinValue = 50, MaxValue = 98)]
        public double TpAdvanceProximityPercent { get; set; }

        // ENHANCEMENT (advanced auto trading): real multi-stage scale-out.
        // When enabled, a slice of the ORIGINAL position volume is closed
        // the moment price actually reaches TP1 / TP2 — locking in real,
        // realized profit instead of only ever waiting for one final exit.
        // The remainder keeps running toward whatever the live broker
        // target currently is (TP2/TP3/TP4, and it can still ratchet
        // forward under Dynamic TP Advance above). Off by default since it
        // changes how a live trade is managed; percentages are of the
        // ORIGINAL fill size, not of what's left.
        [Parameter("Enable Partial Take Profit", Group = "13 · AUTO TRADING", DefaultValue = false)]
        public bool EnablePartialTakeProfit { get; set; }

        [Parameter("Partial Close At TP1 Percent", Group = "13 · AUTO TRADING", DefaultValue = 33, MinValue = 0, MaxValue = 90, Step = 1)]
        public double PartialCloseTp1Percent { get; set; }

        [Parameter("Partial Close At TP2 Percent", Group = "13 · AUTO TRADING", DefaultValue = 33, MinValue = 0, MaxValue = 90, Step = 1)]
        public double PartialCloseTp2Percent { get; set; }

        [Parameter("Move To Break Even After Partial", Group = "13 · AUTO TRADING", DefaultValue = true)]
        public bool MoveToBreakEvenAfterPartial { get; set; }

        // ENHANCEMENT (protect profit on a decisive reversal): closes an
        // open, currently-profitable managed position outright the moment
        // the engine's own decision score flips to a DECISIVE opposite
        // signal (quality above the threshold below) — meant to rescue
        // real gains from an actual trend reversal, not to react to normal
        // drawdown/noise (it only ever fires while net profit is positive).
        // Defaults on since the user asked for this specifically.
        [Parameter("Enable Reversal Protection Close", Group = "13 · AUTO TRADING", DefaultValue = true)]
        public bool EnableReversalProtectionClose { get; set; }

        [Parameter("Reversal Protection Minimum Quality", Group = "13 · AUTO TRADING", DefaultValue = 82, MinValue = 50, MaxValue = 100)]
        public int ReversalProtectionMinimumQuality { get; set; }

        [Parameter("Maximum Open Positions", Group = "13 · AUTO TRADING", DefaultValue = 1, MinValue = 1, MaxValue = 20)]
        public int MaximumOpenPositions { get; set; }

        // ENHANCEMENT (risk engine, Phase 4): a daily loss circuit-breaker.
        // Once today's account equity drawdown from the day's starting
        // equity reaches this percentage, new auto-trade entries are
        // blocked (both normal and aggressive) for the rest of the trading
        // day and a one-time alert fires. This never touches positions
        // already open — consistent with alert-only, no-auto-close — it
        // only stops opening new risk after a bad day.
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

        #region Parameters · Smart Execution
        // ============================================================

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

        #endregion

        #endregion

        #region Parameters · Display
        // ============================================================

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

        [Parameter("Panel Position", Group = "14 · DISPLAY — CORE", DefaultValue = CFIPClean70PanelCorner.BottomLeft)]
        public CFIPClean70PanelCorner PanelPosition { get; set; }

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

        // ENHANCEMENT (chart clarity): TP2-4 used to share the exact same
        // color as TP1, so on a chart with several targets stacked above
        // each other it wasn't obvious which line was which without reading
        // the label text. Distinct, deliberately-graduated colors (closer =
        // green, farther/bigger-reward = gold) make the ladder readable at
        // a glance while keeping the SL(red)/Entry(white) scheme untouched.
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

        [Parameter("Smart Use Closed-Bar Decision", Group = "15 · CONTROL — ADVANCED", DefaultValue = true)]
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

        // ============================================================
        #endregion

        #region Parameters · Advanced Control
        // ============================================================

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

        // ============================================================
        // 22 · COMPLETE INTELLIGENCE
        // ============================================================

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

        // ============================================================
        #endregion

        #region Structural Execution Controls

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

        #endregion

        #region Safety and Precision Controls

        [Parameter("Use M5 Structure For Initial Stop", Group = "22 · Safety & Precision", DefaultValue = true)]
        public bool UseM5StructureForStop { get; set; }

        [Parameter("Use Zone Mitigation Guard", Group = "22 · Safety & Precision", DefaultValue = true)]
        public bool UseZoneMitigationGuard { get; set; }

        [Parameter("Require Order Block Retest", Group = "22 · Safety & Precision", DefaultValue = false)]
        public bool RequireObRetest { get; set; }

        [Parameter("FVG Invalidate On Full Fill", Group = "22 · Safety & Precision", DefaultValue = true)]
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

        #endregion

        #region Runtime Models
        // ============================================================

        private sealed class Frame
        {
            public Bars Bars;
            public int Index;
            public double Atr;
            public double Rsi;
            public double Adx;
            public double EmaFast;
            public double EmaSlow;
            public int Direction;
            public int BullScore;
            public int BearScore;
            public int Evidence;
            public int Quality;
            public bool StructureBull;
            public bool StructureBear;
            public bool MssBull;
            public bool MssBear;
            public bool ChochBull;
            public bool ChochBear;
            public bool DisplacementBull;
            public bool DisplacementBear;
            public bool LiquidityBull;
            public bool LiquidityBear;
            public bool FvgBull;
            public bool FvgBear;
            public bool ObBull;
            public bool ObBear;
            public bool TrendBull;
            public bool TrendBear;
            public bool MomentumBull;
            public bool MomentumBear;
            public bool RejectionBull;
            public bool RejectionBear;
            public bool EqualHigh;
            public bool EqualLow;
            public bool VolumeBull;
            public bool VolumeBear;
            public bool MacdBull;
            public bool MacdBear;
            public bool VwapBull;
            public bool VwapBear;
            public bool VolatilityBull;
            public bool VolatilityBear;
            public bool Choppy;
        }

        private sealed class Level
        {
            public double Price;
            public double Score;
            public string Kind;
            public string Timeframe;
            public int Age;
            public int Hits;
        }

        private sealed class Zone
        {
            public double Low;
            public double High;
            public int Direction;
            public string Kind;
            public int Age;
            public int Quality;
        }

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

        [Parameter("Popup Position", Group = "12 · ALERTS — ADVANCED", DefaultValue = CFIPClean70PanelCorner.TopRight)]
        public CFIPClean70PanelCorner PopupPosition { get; set; }

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

        [Parameter("Aggressive TP Stage", Group = "13 · AUTO TRADING", DefaultValue = CFIPClean70TargetStage.TP1)]
        public CFIPClean70TargetStage AggressiveTpStage { get; set; }

        [Parameter("Aggressive Require Smart Agreement", Group = "13 · AUTO TRADING", DefaultValue = true)]
        public bool AggressiveRequireSmartAgreement { get; set; }

        private sealed class ExecutionModel
        {
            public int Direction;
            public CFIPClean70ExecutionMode Mode;
            public double IdealEntry;
            public double ActualEntry;
            public double ZoneLow;
            public double ZoneHigh;
            public double Trigger;
            public double Invalidation;
            public int Quality;
            public bool Ready;
            public string Source;
        }

        private sealed class Prediction
        {
            public int Direction;
            public CFIPClean70ExecutionMode Mode;
            public int Confidence;
            public double Entry;
            public double StopLoss;
            public double ZoneLow;
            public double ZoneHigh;
            public double Trigger;
            public double Target;
            public double Target1;
            public double Target2;
            public double Target3;
            public double Target4;
            public string Reason;
        }

        private sealed class Decision
        {
            public int Direction;
            public int Confidence;
            public int Edge;
            public int SmartQuality;
            public int TimeframeAgreement;
            public int IndependentEvidence;
            public int StructuralConfirmations;
            public int RetestQuality;
            public int BuyShare;
            public int SellShare;
            public string Regime;
            public int RegimeQuality;
            public bool TriggerReady;
            public bool EntryAllowed;
            public string BlockReason;
            public string Reason;
        }

        private sealed class Plan
        {
            public int Direction;
            public CFIPClean70ExecutionMode EntryMode;
            public double Entry;
            public double IdealEntry;
            public double EntryZoneLow;
            public double EntryZoneHigh;
            public double EntryTrigger;
            public double EntryInvalidation;
            public int EntryQuality;
            public string EntrySource;
            public double Stop;
            public double Tp1;
            public double Tp2;
            public double Tp3;
            public double Tp4;
            public double Risk;
            public double Tp1RR;
            public double Tp2RR;
            public double Tp3RR;
            public double Tp4RR;
            public int StopQuality;
            public int Tp1Quality;
            public int Tp2Quality;
            public int Tp3Quality;
            public int Tp4Quality;
            public string StopSource;
            public string Tp1Source;
            public string Tp2Source;
            public string Tp3Source;
            public string Tp4Source;
            public int HtfTargetCount;
            public int CreatedM5;
            public double OriginalVolume;
            public bool IsLivePosition;
            public long PositionId;
        }

        private sealed class Native
        {
            public Bars Bars;
            public ExponentialMovingAverage Fast;
            public ExponentialMovingAverage Slow;
            public AverageTrueRange Atr;
            public RelativeStrengthIndex Rsi;
            public DirectionalMovementSystem Dms;
            public ExponentialMovingAverage MacdFast;
            public ExponentialMovingAverage MacdSlow;
        }

        // ============================================================
        #endregion

        #region State
        // ============================================================

        private Bars _m1Bars;
        private Bars _m5Bars;
        private Bars _m15Bars;
        private Bars _m30Bars;
        private Bars _h1Bars;
        private Bars _h4Bars;
        private Bars _d1Bars;
        private Bars _w1Bars;

        private Frame _m1Frame;
        private Frame _m5Frame;
        private Frame _m15Frame;
        private Frame _m30Frame;
        private Frame _h1Frame;
        private Frame _h4Frame;
        private Frame _d1Frame;
        private Frame _w1Frame;

        private readonly List<Native> _native = new List<Native>();
        private readonly HashSet<string> _historicalDrawn = new HashSet<string>();
        private readonly HashSet<string> _outcomeDrawn = new HashSet<string>();
        private int _outcomeSequence;

        private Plan _plan;
        private Decision _decision;
        private Decision _reaction;
        private Prediction _prediction;
        private ExecutionModel _executionModel;

        private int _lastStructuralStopUpdateM5 = -1;
        private int _lastTargetRepriceM5 = -1;

        private int _lastEvaluatedM5 = -1;
        private int _lastSignalM5 = -1;
        private int _lastConfirmedM5 = -1;
        private int _lastConfirmedDirection = 0;
        private int _lastExitM5 = -1;
        private bool _outcomeRegistered;
        private int _lastAutoM5 = -1;
        private int _lastEarlyAlertM5 = -1;
        private int _lastHighConfidenceM5 = -1;
        private int _tp1Hit;
        private int _tp2Hit;
        private int _tp3Hit;
        private int _tp4Hit;
        private bool _slHit;
        private double _peakPrice;
        private double _lastMarket;
        private int _wins;
        private int _losses;

        private string _status = "INITIALIZING";
        private string _autoTradingState = "OFF";
        private string _autoTradingReason = "DISABLED";
        private string _autoExecutionBlockReason = "NOT EVALUATED";
        private string _autoOrdersBlockReason = "NOT EVALUATED";
        private DateTime _lastAutoTradeAttemptUtc = DateTime.MinValue;
        private DateTime _lastAutoOrderAttemptUtc = DateTime.MinValue;
        private bool _executionToggleSyncing;
        private bool _autoTradingEnabledRuntime;
        private bool _automaticOrdersEnabledRuntime;
        private bool _executionRuntimeInitialized;
        private bool _lastConfiguredAutoTrading;
        private bool _lastConfiguredAutomaticOrders;
        private bool _outcomeTelemetryTimedOut;

        private CFIPClean70LifecycleState _lifecycleState =
            CFIPClean70LifecycleState.Flat;

        private string _lifecycleReason =
            "INITIALIZING";

        private bool _brokerProtectionRecoveryRequired;

        // Public cTrader parameters are configuration inputs. These private
        // flags are the single runtime authority used by execution, panel
        // state and quick controls so UI state cannot become execution state
        // by accident.
        private bool AutoTradingEnabled =>
            _autoTradingEnabledRuntime;

        private bool AutomaticOrdersEnabled =>
            _automaticOrdersEnabledRuntime;

        // Active broker protection is kept separate from the structural plan
        // so chart/panel presentation can distinguish what the engine wants
        // from what the broker actually holds.
        private double _activeBrokerStop;
        private double _activeBrokerTarget;

        // FIX (CFIP-BUG-ALERT-DEDUP): the cooldown used to compare against a
        // single shared "last alert key", so any two different alert kinds
        // firing back-to-back (e.g. a TP1 alert then a smart/reaction alert)
        // reset that tracker and silently defeated the per-key cooldown for
        // everything else — the same key could then re-fire immediately.
        // Cooldowns are now tracked per key.
        private readonly Dictionary<string, DateTime> _alertCooldowns =
            new Dictionary<string, DateTime>();


        private string _lastRestrictionMessage = "";

        private DateTime _lastEndOfDayAlertDate = DateTime.MinValue;

        private DateTime _lastEndOfDayCloseDate = DateTime.MinValue;

        private DateTime _dailyLossBaselineDate = DateTime.MinValue;

        private double _dailyStartEquity = 0;

        private bool _dailyLossLimitAlerted = false;
        private DateTime _lastRestrictionAlertUtc = DateTime.MinValue;
        private int _lastPendingSignalM5 = -1;
        private int _lastPendingCleanupM5 = -1;
        private int _lastAutoTradingReminderM5 = -1;
        private int _lastVisualDirection = 0;
        private string _lastAlertMessage = "";
        private int _lastAlertDirection;
        private bool _lastAlertCritical;
        private DateTime _lastAlertUtc = DateTime.MinValue;
        private int _authoritativeDirection;
        private string _authoritativeState = "WAITING";

        private Border _panel;
        private StackPanel _panelStack;
        private StackPanel _panelHeaderStack;
        private TextBlock _panelHeaderTitle;
        private StackPanel _panelRowsStack;
        private ScrollViewer _panelScroll;
        private readonly List<TextBlock> _panelRows =
            new List<TextBlock>();
        private StackPanel _buttonStack;
        private Button _closeButton;
        private Button _cancelButton;
        private Button _panelRestoreButton;
        private StackPanel _quickExecutionStack;
        private ToggleButton _autoTradingQuickToggle;
        private ToggleButton _automaticOrdersQuickToggle;

        private Border _popup;
        private TextBlock _popupText;
        private DateTime _popupUntilUtc = DateTime.MinValue;

        private const string P = "CFIP_CLEAN70_";
        private const string H = "CFIP_CLEAN70_H_";

        private int _lastContextM5 = -1;
        private int _lastInvalidationAlertM5 = -1;
        private bool _panelHidden;
        private Button _panelToggleButton;
        private string _panelStableHeader = "";
        private DateTime _panelStableHeaderSinceUtc = DateTime.MinValue;
        private Button _popupCloseButton;

        private readonly Dictionary<int, int> _directionSamples =
            new Dictionary<int, int>();

        private readonly Dictionary<int, int> _directionWins =
            new Dictionary<int, int>();

        private DateTime _lastBrokerModifyUtc = DateTime.MinValue;
        private int _lastRestrictionM5 = -1;
        private int _lastSmartDecisionAlertM5 = -1;
        private int _lastHistoricalHostBar = -1;
        private int _lastAutoPlanAttemptM5 = -1;
        private DateTime _lastTradingPermissionRequestUtc = DateTime.MinValue;

        private int _marketSuitabilityM5 = -1;
        private int _marketSuitabilityDirection = 0;
        private int _marketSuitabilityScore = 0;
        private string _marketSuitabilityState = "UNKNOWN";
        private string _marketSuitabilityReason = "NOT EVALUATED";
        private DateTime _lastMarketSuitabilityUtc = DateTime.MinValue;

        // ============================================================
        #endregion

        #region Lifecycle
        // ============================================================

        private void InitializeExecutionRuntimeState()
        {
            _autoTradingEnabledRuntime =
                EnableAutoTrading;

            _automaticOrdersEnabledRuntime =
                EnableAutomaticOrders;

            _lastConfiguredAutoTrading =
                EnableAutoTrading;

            _lastConfiguredAutomaticOrders =
                EnableAutomaticOrders;

            _outcomeTelemetryTimedOut =
                false;

            _executionRuntimeInitialized =
                true;

            _autoExecutionBlockReason =
                _autoTradingEnabledRuntime
                    ? "NOT EVALUATED"
                    : "DISABLED";

            _autoOrdersBlockReason =
                _automaticOrdersEnabledRuntime
                    ? "NOT EVALUATED"
                    : "DISABLED";
        }

        private void EnsureExecutionRuntimeState()
        {
            if (!_executionRuntimeInitialized)
            {
                InitializeExecutionRuntimeState();
                return;
            }

            // Parameters are configuration inputs. Runtime quick controls own
            // the live execution state. Only an actual external/configuration
            // change in a parameter is allowed to alter that runtime state.
            if (EnableAutoTrading != _lastConfiguredAutoTrading)
            {
                _lastConfiguredAutoTrading =
                    EnableAutoTrading;

                _autoTradingEnabledRuntime =
                    EnableAutoTrading;

                _autoExecutionBlockReason =
                    EnableAutoTrading
                        ? "AWAITING EXECUTION"
                        : "DISABLED";
            }

            if (EnableAutomaticOrders !=
                _lastConfiguredAutomaticOrders)
            {
                _lastConfiguredAutomaticOrders =
                    EnableAutomaticOrders;

                _automaticOrdersEnabledRuntime =
                    EnableAutomaticOrders;

                _autoOrdersBlockReason =
                    EnableAutomaticOrders
                        ? "AWAITING ORDER SETUP"
                        : "DISABLED";
            }
        }

        protected override void Initialize()
        {
            _native.Clear();
            _historicalDrawn.Clear();
            _outcomeDrawn.Clear();

            _m1Bars = MarketData.GetBars(TimeFrame.Minute);
            _m5Bars = MarketData.GetBars(TimeFrame.Minute5);
            _m15Bars = MarketData.GetBars(TimeFrame.Minute15);
            _m30Bars = MarketData.GetBars(TimeFrame.Minute30);
            _h1Bars = MarketData.GetBars(TimeFrame.Hour);
            _h4Bars = MarketData.GetBars(TimeFrame.Hour4);
            _d1Bars = MarketData.GetBars(TimeFrame.Daily);
            _w1Bars = MarketData.GetBars(TimeFrame.Weekly);

            RegisterAllNative();

            try
            {
                Positions.Opened += OnPositionOpened;
                Positions.Closed += OnPositionClosed;
                Positions.Modified += OnPositionModified;
                PendingOrders.Created += OnPendingOrderCreated;
                PendingOrders.Modified += OnPendingOrderModified;
                PendingOrders.Filled += OnPendingOrderFilled;
                PendingOrders.Cancelled += OnPendingOrderCancelled;
            }
            catch (Exception ex)
            {
                Print("CFIP CLEAN70 trading event hookup failed: {0}", ex.Message);
            }

            InitializeExecutionRuntimeState();

            SetLifecycleState(
                CFIPClean70LifecycleState.Flat,
                "READY");

            CreatePanel();

            SetAutoTradingState(
                AutoTradingEnabled
                    ? "ARMED"
                    : "OFF",
                AutoTradingEnabled
                    ? "INITIALIZING"
                    : "DISABLED");

            _status = "READY";
        }

        protected override void OnDestroy()
        {
            try
            {
                Positions.Opened -= OnPositionOpened;
                Positions.Closed -= OnPositionClosed;
                Positions.Modified -= OnPositionModified;
                PendingOrders.Created -= OnPendingOrderCreated;
                PendingOrders.Modified -= OnPendingOrderModified;
                PendingOrders.Filled -= OnPendingOrderFilled;
                PendingOrders.Cancelled -= OnPendingOrderCancelled;
            }
            catch
            {
            }

            RemoveAllChartObjects();
            RemovePanel();
            RemovePopup();
            base.OnDestroy();
        }

        public override void Calculate(int index)
        {
            if (!IsLastBar ||
                Bars == null)
                return;

            RemoveExpiredPopup();

            if (!HasEnoughData())
            {
                _status = "BUILDING DATA";
                RenderPanel();
                return;
            }

            int closedM5 =
                Math.Max(
                    1,
                    _m5Bars.Count - 2);

            if (closedM5 < 30)
            {
                _status =
                    "WAITING FOR CLOSED M5";
                RenderPanel();
                return;
            }

            DateTime reference =
                _m5Bars.OpenTimes[
                    _m5Bars.Count - 1];

            bool newClosedBar =
                closedM5 !=
                _lastEvaluatedM5;

            // Expensive MTF/structure calculation runs exactly once per newly
            // closed M5 bar. Live price management remains tick responsive.
            if (newClosedBar)
            {
                int m1Index =
                    ClosedIndex(
                        _m1Bars,
                        reference);

                int m15Index =
                    ClosedIndex(
                        _m15Bars,
                        reference);

                int m30Index =
                    ClosedIndex(
                        _m30Bars,
                        reference);

                int h1Index =
                    ClosedIndex(
                        _h1Bars,
                        reference);

                int h4Index =
                    ClosedIndex(
                        _h4Bars,
                        reference);

                int d1Index =
                    ClosedIndex(
                        _d1Bars,
                        reference);

                int w1Index =
                    ClosedIndex(
                        _w1Bars,
                        reference);

                if (m15Index < 30 ||
                    m30Index < 30 ||
                    h1Index < 30 ||
                    h4Index < 30)
                {
                    _status =
                        "WAITING FOR MTF DATA";
                    RenderPanel();
                    return;
                }

                _m1Frame =
                    m1Index >= 30
                        ? AnalyzeFrame(
                            _m1Bars,
                            m1Index)
                        : null;

                _m5Frame =
                    AnalyzeFrame(
                        _m5Bars,
                        closedM5);

                _m15Frame =
                    AnalyzeFrame(
                        _m15Bars,
                        m15Index);

                _m30Frame =
                    AnalyzeFrame(
                        _m30Bars,
                        m30Index);

                _h1Frame =
                    AnalyzeFrame(
                        _h1Bars,
                        h1Index);

                _h4Frame =
                    AnalyzeFrame(
                        _h4Bars,
                        h4Index);

                _d1Frame =
                    d1Index >= 10
                        ? AnalyzeFrame(
                            _d1Bars,
                            d1Index)
                        : null;

                _w1Frame =
                    w1Index >= 10
                        ? AnalyzeFrame(
                            _w1Bars,
                            w1Index)
                        : null;

                int decisionChartIndex =
                    SmartUseClosedBarDecision
                        ? MapM5ToChart(
                            closedM5,
                            index)
                        : index;

                _decision =
                    BuildDecision(
                        decisionChartIndex,
                        SmartUseClosedBarDecision
                            ? closedM5
                            : Math.Max(
                                1,
                                _m5Bars.Count - 1),
                        reference);

                RefreshMarketSuitability(
                    closedM5,
                    _decision == null
                        ? 0
                        : _decision.Direction,
                    true);

                _prediction =
                    BuildEarlyPrediction(
                        closedM5);

                RenderPredictionObjects(
                    _prediction,
                    closedM5);

                EmitContextAlerts(
                    closedM5);

                if (_decision != null)
                {
                    if (AlertOnHighConfidenceEntry &&
                        _decision.Confidence >=
                        HighConfidenceThreshold &&
                        _lastHighConfidenceM5 !=
                        closedM5)
                    {
                        SendUnifiedAlert(
                            "HIGH|" +
                            closedM5,
                            "CFIP CLEAN70 HIGH CONFIDENCE | " +
                            (_decision.Direction == 1
                                ? "BUY"
                                : "SELL") +
                            " | CONF " +
                            _decision.Confidence,
                            _decision.Direction,
                            true);

                        _lastHighConfidenceM5 =
                            closedM5;
                    }

                    if (!_decision.EntryAllowed &&
                        RestrictionAlertEnabled(
                            _decision.BlockReason) &&
                        !string.IsNullOrWhiteSpace(
                            _decision.BlockReason))
                    {
                        string restrictionMessage =
                            _decision.BlockReason.Trim();

                        bool restrictionChanged =
                            !string.Equals(
                                _lastRestrictionMessage,
                                restrictionMessage,
                                StringComparison.OrdinalIgnoreCase);

                        if (restrictionChanged)
                        {
                            SendUnifiedAlert(
                                "RESTRICT|" +
                                restrictionMessage,
                                "CFIP CLEAN70 ENTRY BLOCKED | " +
                                restrictionMessage,
                                _decision.Direction,
                                false);

                            _lastRestrictionMessage =
                                restrictionMessage;

                            _lastRestrictionAlertUtc =
                                DateTime.UtcNow;

                            _lastRestrictionM5 =
                                closedM5;
                        }
                    }
                    else if (_decision.EntryAllowed)
                    {
                        _lastRestrictionMessage = "";
                        _lastRestrictionAlertUtc =
                            DateTime.MinValue;
                        _lastRestrictionM5 = -1;
                    }

                    if (AlertOnSmartDecision &&
                        _decision.EntryAllowed &&
                        _decision.SmartQuality >=
                        SmartStrongSetupQuality &&
                        _decision.Edge >=
                        SmartStrongSetupEdge &&
                        _lastSmartDecisionAlertM5 !=
                        closedM5)
                    {
                        SendUnifiedAlert(
                            "SMART|" +
                            closedM5,
                            "CFIP CLEAN70 SMART DECISION | " +
                            (_decision.Direction == 1
                                ? "BUY"
                                : "SELL") +
                            " | Q " +
                            _decision.SmartQuality +
                            " | CONF " +
                            _decision.Confidence,
                            _decision.Direction,
                            true);

                        _lastSmartDecisionAlertM5 =
                            closedM5;
                    }
                }

                bool allowUnconfirmedAutoPlan =
                    AutoTradingEnabled &&
                    !ConfirmedSignalsOnly;

                ReconcilePreTradePlanDirection(closedM5);

                EnsureSignalPlan(
                    closedM5,
                    allowUnconfirmedAutoPlan);

                _lastEvaluatedM5 =
                    closedM5;

                if (ShowHistoricalSignals)
                {
                    int hostBar =
                        Math.Max(
                            0,
                            Math.Min(
                                Bars.Count - 1,
                                index));

                    if (_lastHistoricalHostBar !=
                        hostBar)
                    {
                        RenderHistoricalSignals();
                        _lastHistoricalHostBar =
                            hostBar;
                    }
                }
                else
                {
                    RemoveHistoricalObjects();
                    _lastHistoricalHostBar =
                        -1;
                }
            }

            // Tick-level path: reaction and active plan management remain live.
            _reaction =
                BuildReaction();

            RecoverManagedLivePlan(closedM5);

            if (!AutoTradingEnabled &&
                AutoTradingReminder)
                CheckAutoTradingDisabledReminder(closedM5);

            SyncQuickExecutionControls();

            if (AutoTradingEnabled &&
                _plan == null &&
                _decision != null &&
                _decision.Direction != 0 &&
                _lastAutoPlanAttemptM5 !=
                closedM5)
            {
                _lastAutoPlanAttemptM5 =
                    closedM5;

                EnsureSignalPlan(
                    closedM5,
                    !ConfirmedSignalsOnly);
            }

            if (_decision != null &&
                _decision.Direction != 0 &&
                _plan == null)
            {
                _executionModel =
                    BuildExecutionModel(
                        closedM5,
                        _decision.Direction);
            }
            else if (_plan == null)
            {
                _executionModel = null;
            }

            SynchronizeLiveBrokerState();

            EvaluateActivePlan(
                closedM5);

            // Complete all execution, broker-protection and lifecycle state
            // changes before rendering. This prevents the chart/panel from
            // displaying the state from before a same-cycle execution event.
            TryAutoTrade(
                closedM5);

            TryAggressiveAutoTrade(
                closedM5);

            TrySmartPendingOrders(
                closedM5);

            ProtectBrokerPositions(
                closedM5);

            MonitorOutcome(
                closedM5);

            CheckEndOfDayAlert(
                DateTime.UtcNow);

            CheckReversalProtection();

            SynchronizeLiveBrokerState();

            if (_plan != null)
                RenderPlan();
            else
                RenderWatchAndReaction(
                    index,
                    closedM5);

            RenderManagedPendingOrder();
            RenderPanel();
        }

        private bool HasEnoughData()
        {
            return _m5Bars != null &&
                   _m15Bars != null &&
                   _m30Bars != null &&
                   _h1Bars != null &&
                   _h4Bars != null &&
                   _m5Bars.Count >= 100 &&
                   _m15Bars.Count >= 100 &&
                   _m30Bars.Count >= 80 &&
                   _h1Bars.Count >= 80 &&
                   _h4Bars.Count >= 60;
        }

        // ============================================================
        #endregion

        #region Native Indicators
        // ============================================================

        private void RegisterAllNative()
        {
            RegisterNative(_m1Bars);
            RegisterNative(_m5Bars);
            RegisterNative(_m15Bars);
            RegisterNative(_m30Bars);
            RegisterNative(_h1Bars);
            RegisterNative(_h4Bars);
            RegisterNative(_d1Bars);
            RegisterNative(_w1Bars);
        }

        private Native RegisterNative(Bars bars)
        {
            if (bars == null)
                return null;

            Native existing =
                _native.FirstOrDefault(x => ReferenceEquals(x.Bars, bars));

            if (existing != null)
                return existing;

            Native set = new Native();
            set.Bars = bars;

            try
            {
                set.Fast =
                    Indicators.ExponentialMovingAverage(
                        bars.ClosePrices,
                        Math.Max(2, FastEma));

                set.Slow =
                    Indicators.ExponentialMovingAverage(
                        bars.ClosePrices,
                        Math.Max(3, SlowEma));

                set.Atr =
                    Indicators.AverageTrueRange(
                        bars,
                        Math.Max(2, AtrPeriod),
                        MovingAverageType.WilderSmoothing);

                set.Rsi =
                    Indicators.RelativeStrengthIndex(
                        bars.ClosePrices,
                        Math.Max(2, RsiPeriod));

                set.Dms =
                    Indicators.DirectionalMovementSystem(
                        bars,
                        Math.Max(2, AdxPeriod),
                        MovingAverageType.WilderSmoothing);

                int macdFastPeriod =
                    Math.Max(
                        2,
                        MacdFastPeriod);

                int macdSlowPeriod =
                    Math.Max(
                        macdFastPeriod + 1,
                        MacdSlowPeriod);

                set.MacdFast =
                    Indicators.ExponentialMovingAverage(
                        bars.ClosePrices,
                        macdFastPeriod);

                set.MacdSlow =
                    Indicators.ExponentialMovingAverage(
                        bars.ClosePrices,
                        macdSlowPeriod);
            }
            catch (Exception ex)
            {
                Print(
                    "CFIP CLEAN70 indicator initialization failed: {0}",
                    ex.Message);
            }

            _native.Add(set);
            return set;
        }

        private Native GetNative(Bars bars)
        {
            if (bars == null)
                return null;

            Native set =
                _native.FirstOrDefault(
                    x => ReferenceEquals(x.Bars, bars));

            return set ?? RegisterNative(bars);
        }

        private double Ema(Bars bars, int index, bool fast)
        {
            if (bars == null || index < 0 || index >= bars.Count)
                return 0;

            Native set = GetNative(bars);
            if (set == null)
                return 0;

            ExponentialMovingAverage ema =
                fast ? set.Fast : set.Slow;

            if (ema == null || index >= ema.Result.Count)
                return 0;

            return SafePositive(ema.Result[index]);
        }

        private double Atr(Bars bars, int index)
        {
            if (bars == null || index < 0 || index >= bars.Count)
                return 0;

            Native set = GetNative(bars);

            if (set == null ||
                set.Atr == null ||
                index >= set.Atr.Result.Count)
                return 0;

            return SafePositive(set.Atr.Result[index]);
        }

        private double Rsi(Bars bars, int index)
        {
            if (bars == null || index < 0 || index >= bars.Count)
                return 50;

            Native set = GetNative(bars);

            if (set == null ||
                set.Rsi == null ||
                index >= set.Rsi.Result.Count)
                return 50;

            double value = set.Rsi.Result[index];

            return
                double.IsNaN(value) ||
                double.IsInfinity(value)
                    ? 50
                    : Clamp(value, 0, 100);
        }

        private double Adx(Bars bars, int index)
        {
            if (bars == null || index < 0 || index >= bars.Count)
                return 0;

            Native set = GetNative(bars);

            if (set == null ||
                set.Dms == null ||
                index >= set.Dms.ADX.Count)
                return 0;

            double value = set.Dms.ADX[index];

            return
                double.IsNaN(value) ||
                double.IsInfinity(value)
                    ? 0
                    : Clamp(value, 0, 100);
        }

        private double DmiBias(Bars bars, int index)
        {
            if (bars == null || index < 0 || index >= bars.Count)
                return 0;

            Native set = GetNative(bars);

            if (set == null ||
                set.Dms == null ||
                index >= set.Dms.DIPlus.Count ||
                index >= set.Dms.DIMinus.Count)
                return 0;

            double plus = set.Dms.DIPlus[index];
            double minus = set.Dms.DIMinus[index];

            if (double.IsNaN(plus) ||
                double.IsInfinity(plus) ||
                double.IsNaN(minus) ||
                double.IsInfinity(minus))
                return 0;

            double total = plus + minus;

            return
                total <= 0
                    ? 0
                    : Clamp(
                        (plus - minus) / total,
                        -1,
                        1);
        }

        // ============================================================
        #endregion

        #region Frame Analysis
        // ============================================================

        private Frame AnalyzeFrame(
            Bars bars,
            int index)
        {
            Frame f =
                new Frame
                {
                    Bars = bars,
                    Index = index
                };

            if (bars == null ||
                index < 30 ||
                index >= bars.Count)
                return f;

            f.Atr = Atr(bars, index);
            f.Rsi = Rsi(bars, index);
            f.Adx = Adx(bars, index);
            f.EmaFast = Ema(bars, index, true);
            f.EmaSlow = Ema(bars, index, false);

            f.StructureBull =
                UseInternalStructure &&
                BullStructure(
                    bars,
                    index,
                    f.Atr);

            f.StructureBear =
                UseInternalStructure &&
                BearStructure(
                    bars,
                    index,
                    f.Atr);

            f.MssBull =
                UseMssChoch &&
                BullMss(
                    bars,
                    index,
                    f.Atr);

            f.MssBear =
                UseMssChoch &&
                BearMss(
                    bars,
                    index,
                    f.Atr);

            f.ChochBull =
                UseMssChoch &&
                BullChoch(
                    bars,
                    index);

            f.ChochBear =
                UseMssChoch &&
                BearChoch(
                    bars,
                    index);

            f.DisplacementBull =
                UseDisplacement &&
                BullDisplacement(
                    bars,
                    index,
                    f.Atr);

            f.DisplacementBear =
                UseDisplacement &&
                BearDisplacement(
                    bars,
                    index,
                    f.Atr);

            f.LiquidityBull =
                UseLiquiditySweep &&
                BullLiquiditySweep(
                    bars,
                    index,
                    f.Atr);

            f.LiquidityBear =
                UseLiquiditySweep &&
                BearLiquiditySweep(
                    bars,
                    index,
                    f.Atr);

            f.FvgBull =
                UseFvg &&
                FindNearestFvg(
                    bars,
                    index,
                    1,
                    f.Atr) != null;

            f.FvgBear =
                UseFvg &&
                FindNearestFvg(
                    bars,
                    index,
                    -1,
                    f.Atr) != null;

            f.ObBull =
                UseOrderBlock &&
                FindNearestOrderBlock(
                    bars,
                    index,
                    1,
                    f.Atr) != null;

            f.ObBear =
                UseOrderBlock &&
                FindNearestOrderBlock(
                    bars,
                    index,
                    -1,
                    f.Atr) != null;

            f.TrendBull =
                f.EmaFast > f.EmaSlow &&
                bars.ClosePrices[index] >
                f.EmaFast;

            f.TrendBear =
                f.EmaFast < f.EmaSlow &&
                bars.ClosePrices[index] <
                f.EmaFast;

            f.MomentumBull =
                Momentum(
                    bars,
                    index,
                    1,
                    f.Atr);

            f.MomentumBear =
                Momentum(
                    bars,
                    index,
                    -1,
                    f.Atr);

            f.RejectionBull =
                Rejection(
                    bars,
                    index,
                    1);

            f.RejectionBear =
                Rejection(
                    bars,
                    index,
                    -1);

            f.VolumeBull =
                HasVolumeExpansion(
                    bars,
                    index,
                    1);

            f.VolumeBear =
                HasVolumeExpansion(
                    bars,
                    index,
                    -1);

            f.MacdBull =
                HasMacdBias(
                    bars,
                    index,
                    1);

            f.MacdBear =
                HasMacdBias(
                    bars,
                    index,
                    -1);

            f.VwapBull =
                HasVwapBias(
                    bars,
                    index,
                    1);

            f.VwapBear =
                HasVwapBias(
                    bars,
                    index,
                    -1);

            f.VolatilityBull =
                HasHealthyVolatility(
                    bars,
                    index,
                    1);

            f.VolatilityBear =
                HasHealthyVolatility(
                    bars,
                    index,
                    -1);

            f.Choppy =
                UseHistoricalChoppinessGuard &&
                f.Adx < AdxMinimum &&
                Math.Abs(
                    f.EmaFast -
                    f.EmaSlow) <
                f.Atr * 0.35;

            f.EqualHigh =
                UseEqualHighLow &&
                FindEqualHigh(
                    bars,
                    index,
                    bars.ClosePrices[index],
                    f.Atr) > 0;

            f.EqualLow =
                UseEqualHighLow &&
                FindEqualLow(
                    bars,
                    index,
                    bars.ClosePrices[index],
                    f.Atr) > 0;

            int bull = 0;
            int bear = 0;
            int evidence = 0;

            AddScore(f.StructureBull, 16, ref bull, ref evidence);
            AddScore(f.StructureBear, 16, ref bear, ref evidence);
            AddScore(f.MssBull, 12, ref bull, ref evidence);
            AddScore(f.MssBear, 12, ref bear, ref evidence);
            AddScore(f.ChochBull, 9, ref bull, ref evidence);
            AddScore(f.ChochBear, 9, ref bear, ref evidence);
            AddScore(f.DisplacementBull, 10, ref bull, ref evidence);
            AddScore(f.DisplacementBear, 10, ref bear, ref evidence);
            AddScore(f.LiquidityBull, 10, ref bull, ref evidence);
            AddScore(f.LiquidityBear, 10, ref bear, ref evidence);
            AddScore(f.FvgBull, 8, ref bull, ref evidence);
            AddScore(f.FvgBear, 8, ref bear, ref evidence);
            AddScore(f.ObBull, 9, ref bull, ref evidence);
            AddScore(f.ObBear, 9, ref bear, ref evidence);
            AddScore(f.TrendBull, 10, ref bull, ref evidence);
            AddScore(f.TrendBear, 10, ref bear, ref evidence);
            AddScore(f.MomentumBull, 8, ref bull, ref evidence);
            AddScore(f.MomentumBear, 8, ref bear, ref evidence);
            AddScore(f.RejectionBull, 6, ref bull, ref evidence);
            AddScore(f.RejectionBear, 6, ref bear, ref evidence);
            AddScore(
                f.VolumeBull,
                3,
                ref bull,
                ref evidence,
                UseVolumeExpansionEvidence);
            AddScore(
                f.VolumeBear,
                3,
                ref bear,
                ref evidence,
                UseVolumeExpansionEvidence);
            AddScore(
                f.MacdBull,
                3,
                ref bull,
                ref evidence,
                UseMacdEvidence);
            AddScore(
                f.MacdBear,
                3,
                ref bear,
                ref evidence,
                UseMacdEvidence);
            AddScore(
                f.VwapBull,
                2,
                ref bull,
                ref evidence,
                UseVwapEvidence);
            AddScore(
                f.VwapBear,
                2,
                ref bear,
                ref evidence,
                UseVwapEvidence);
            AddScore(
                f.VolatilityBull,
                2,
                ref bull,
                ref evidence,
                UseHealthyVolatilityEvidence);
            AddScore(
                f.VolatilityBear,
                2,
                ref bear,
                ref evidence,
                UseHealthyVolatilityEvidence);
            AddScore(f.EqualLow, 5, ref bull, ref evidence);
            AddScore(f.EqualHigh, 5, ref bear, ref evidence);

            if (f.Rsi > 50)
                bull += 3;
            else if (f.Rsi < 50)
                bear += 3;

            if (f.Adx >= AdxMinimum)
            {
                double dmi =
                    DmiBias(
                        bars,
                        index);

                if (dmi > 0)
                    bull += 4;
                else if (dmi < 0)
                    bear += 4;
            }

            if (UseEmaSlope &&
                index > 2)
            {
                double previous =
                    Ema(
                        bars,
                        index - 2,
                        true);

                if (f.EmaFast > previous)
                    bull += 3;
                else if (f.EmaFast < previous)
                    bear += 3;
            }

            if (AvoidRsiExhaustion)
            {
                if (f.Rsi >= 75)
                    bull = Math.Max(
                        0,
                        bull - 5);

                if (f.Rsi <= 25)
                    bear = Math.Max(
                        0,
                        bear - 5);
            }

            f.BullScore = bull;
            f.BearScore = bear;
            f.Evidence = evidence;

            if (bull >= 35 &&
                bull >= bear + 8)
                f.Direction = 1;
            else if (bear >= 35 &&
                     bear >= bull + 8)
                f.Direction = -1;

            double total =
                Math.Max(
                    1,
                    bull + bear);

            double strongest =
                100.0 *
                Math.Max(
                    bull,
                    bear) /
                total;

            f.Quality =
                ClampInt(
                    (int)Math.Round(
                        strongest * 0.50 +
                        Math.Min(
                            100,
                            f.Adx * 1.5) * 0.15 +
                        Math.Min(
                            100,
                            evidence * 5) * 0.25 +
                        (f.Choppy ? 0 : 10) * 0.10),
                    0,
                    100);

            return f;
        }

        private void AddScore(
            bool condition,
            int score,
            ref int total,
            ref int evidence)
        {
            if (!condition)
                return;

            total += score;
            evidence++;
        }

        private void AddScore(
            bool condition,
            int score,
            ref int total,
            ref int evidence,
            bool countAsEvidence)
        {
            if (!condition)
                return;

            total += score;

            if (countAsEvidence)
                evidence++;
        }

        private bool HasVolumeExpansion(
            Bars bars,
            int index,
            int direction)
        {
            if (!UseVolumeExpansion ||
                bars == null ||
                index < 25)
                return false;

            double average = 0;
            int count = 0;
            int first =
                Math.Max(
                    0,
                    index - 20);

            for (int i = first;
                 i < index;
                 i++)
            {
                average +=
                    Math.Max(
                        0,
                        bars.TickVolumes[i]);

                count++;
            }

            if (count == 0 ||
                average <= 0)
                return false;

            average /= count;

            bool directional =
                direction == 1
                    ? bars.ClosePrices[index] >
                      bars.OpenPrices[index]
                    : bars.ClosePrices[index] <
                      bars.OpenPrices[index];

            return
                directional &&
                bars.TickVolumes[index] >=
                average *
                Math.Max(
                    1.0,
                    VolumeExpansionRatio);
        }

        private bool HasMacdBias(
            Bars bars,
            int index,
            int direction)
        {
            if (!UseMacdBias ||
                bars == null ||
                index < 35)
                return false;

            Native set =
                GetNative(bars);

            if (set == null ||
                set.MacdFast == null ||
                set.MacdSlow == null ||
                index >= set.MacdFast.Result.Count ||
                index >= set.MacdSlow.Result.Count)
                return false;

            double histogram =
                set.MacdFast.Result[index] -
                set.MacdSlow.Result[index];

            int previousIndex =
                Math.Max(
                    0,
                    index - 2);

            double previous =
                set.MacdFast.Result[previousIndex] -
                set.MacdSlow.Result[previousIndex];

            if (double.IsNaN(histogram) ||
                double.IsInfinity(histogram) ||
                double.IsNaN(previous) ||
                double.IsInfinity(previous))
                return false;

            return
                direction == 1
                    ? histogram > 0 &&
                      histogram >= previous
                    : histogram < 0 &&
                      histogram <= previous;
        }

        private bool HasVwapBias(
            Bars bars,
            int index,
            int direction)
        {
            if (!UseVwapBias ||
                bars == null ||
                index < 20)
                return false;

            int first =
                Math.Max(
                    0,
                    index -
                    Math.Max(
                        10,
                        VwapLookbackBars - 1));

            double priceVolume = 0;
            double volume = 0;

            for (int i = first;
                 i <= index;
                 i++)
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

                priceVolume +=
                    typical *
                    v;

                volume +=
                    v;
            }

            if (volume <= 0)
                return false;

            double vwap =
                priceVolume /
                volume;

            return
                direction == 1
                    ? bars.ClosePrices[index] >
                      vwap
                    : bars.ClosePrices[index] <
                      vwap;
        }

        private bool HasHealthyVolatility(
            Bars bars,
            int index,
            int direction)
        {
            if (!UseHealthyVolatility ||
                bars == null ||
                index < 30)
                return false;

            double atr =
                Atr(
                    bars,
                    index);

            double oldAtr =
                Atr(
                    bars,
                    Math.Max(
                        5,
                        index - 10));

            if (atr <= 0 ||
                oldAtr <= 0)
                return false;

            double body =
                Math.Abs(
                    bars.ClosePrices[index] -
                    bars.OpenPrices[index]);

            bool directional =
                direction == 1
                    ? bars.ClosePrices[index] >
                      bars.OpenPrices[index]
                    : bars.ClosePrices[index] <
                      bars.OpenPrices[index];

            double minRatio =
                Math.Max(
                    0.50,
                    HealthyAtrMinimumRatio);

            double maxRatio =
                Math.Max(
                    minRatio,
                    HealthyAtrMaximumRatio);

            return
                directional &&
                body >=
                atr *
                MinimumTriggerBodyAtr &&
                atr >=
                oldAtr *
                minRatio &&
                atr <=
                oldAtr *
                maxRatio;
        }

        // ============================================================
        // DECISION
        // ============================================================

        private Decision BuildDecision(
            int chartIndex,
            int closedM5,
            DateTime reference)
        {
            Decision d =
                new Decision();

            double buy = 0;
            double sell = 0;
            int evidence = 0;

            AddFrame(_m5Frame, M5Weight, ref buy, ref sell, ref evidence);
            AddFrame(_m15Frame, M15Weight, ref buy, ref sell, ref evidence);
            AddFrame(_m30Frame, M30Weight, ref buy, ref sell, ref evidence);
            AddFrame(_h1Frame, H1Weight, ref buy, ref sell, ref evidence);
            AddFrame(_h4Frame, H4Weight, ref buy, ref sell, ref evidence);
            AddFrame(_d1Frame, D1Weight, ref buy, ref sell, ref evidence);

            if (SmartWeeklyContext)
                AddFrame(_w1Frame, W1Weight, ref buy, ref sell, ref evidence);

            if (UseAdvancedConfluence)
            {
                buy +=
                    LiveBias(
                        chartIndex,
                        1);

                sell +=
                    LiveBias(
                        chartIndex,
                        -1);
            }

            if (UsePremiumDiscount)
            {
                int pd =
                    PremiumDiscountBias(
                        _m5Bars,
                        closedM5);

                if (pd == 1)
                    buy += 6;
                else if (pd == -1)
                    sell += 6;
            }

            if (UseM1Trigger &&
                _m1Frame != null)
            {
                if (_m1Frame.Direction == 1)
                    buy += 3;
                else if (_m1Frame.Direction == -1)
                    sell += 3;
            }

            string regime =
                DetectRegime(
                    _m5Bars,
                    closedM5);

            if (AdaptiveRegimeWeighting &&
                _m5Frame != null)
            {
                if (regime == "EXPANSION")
                {
                    if (_m5Frame.DisplacementBull)
                        buy += 4;

                    if (_m5Frame.DisplacementBear)
                        sell += 4;

                    if (_m5Frame.VolumeBull)
                        buy += 2;

                    if (_m5Frame.VolumeBear)
                        sell += 2;
                }
                else if (regime == "RANGE" ||
                         regime == "TRANSITION")
                {
                    if (_m5Frame.LiquidityBull)
                        buy += 3;

                    if (_m5Frame.LiquidityBear)
                        sell += 3;

                    if (_m5Frame.FvgBull)
                        buy += 2;

                    if (_m5Frame.FvgBear)
                        sell += 2;

                    if (_m5Frame.ObBull)
                        buy += 2;

                    if (_m5Frame.ObBear)
                        sell += 2;
                }
                else if (regime == "COMPRESSION")
                {
                    buy *= 0.95;
                    sell *= 0.95;
                }
            }

            if (UseHistoricalChoppinessGuard &&
                _m5Frame != null &&
                _m15Frame != null &&
                _m5Frame.Choppy &&
                _m15Frame.Choppy)
            {
                buy *= 0.90;
                sell *= 0.90;
            }

            double temperature =
                Math.Max(
                    1.0,
                    SmartScoreTemperature);

            double centered =
                (buy - sell) /
                temperature;

            double expBuy =
                Math.Exp(
                    Clamp(
                        centered,
                        -12,
                        12));

            double expSell =
                Math.Exp(
                    Clamp(
                        -centered,
                        -12,
                        12));

            double total =
                Math.Max(
                    1e-9,
                    expBuy + expSell);

            int buyShare =
                ClampInt(
                    (int)Math.Round(
                        100.0 *
                        expBuy /
                        total),
                    0,
                    100);

            int sellShare =
                100 -
                buyShare;

            d.BuyShare = buyShare;
            d.SellShare = sellShare;

            int strongestShare =
                Math.Max(
                    buyShare,
                    sellShare);

            d.Direction =
                strongestShare >=
                Math.Max(
                    50,
                    MinimumSmartDirectionShare)
                    ? (buyShare >= sellShare
                        ? 1
                        : -1)
                    : 0;

            d.Edge =
                Math.Abs(
                    buyShare -
                    sellShare);

            d.TimeframeAgreement =
                TimeframeAgreement(
                    d.Direction);

            d.IndependentEvidence =
                IndependentEvidence(
                    d.Direction);

            d.StructuralConfirmations =
                StructuralConfirmations(
                    d.Direction);

            d.Regime = regime;

            d.RegimeQuality =
                RegimeQuality(
                    regime,
                    _m5Bars,
                    closedM5);

            d.SmartQuality =
                ClampInt(
                    (int)Math.Round(
                        strongestShare * 0.28 +
                        d.TimeframeAgreement * 0.23 +
                        Math.Min(
                            100,
                            d.IndependentEvidence * 10) * 0.20 +
                        Math.Min(
                            100,
                            d.StructuralConfirmations * 16) * 0.17 +
                        d.RegimeQuality * 0.12),
                    0,
                    100);

            if (d.Direction == 0)
            {
                d.Confidence =
                    strongestShare;

                d.TriggerReady = false;
                d.EntryAllowed = false;
                d.BlockReason =
                    "SMART CONSENSUS";

                d.Reason =
                    "NEUTRAL | BUY " +
                    buyShare +
                    " | SELL " +
                    sellShare;

                return d;
            }

            d.Confidence =
                CalibratedConfidence(
                    ClampInt(
                        (int)Math.Round(
                            strongestShare * 0.45 +
                            d.TimeframeAgreement * 0.25 +
                            d.SmartQuality * 0.30),
                        0,
                        100),
                    d.Direction);

            if (HigherTfPenalty > 0)
            {
                bool h1Against =
                    _h1Frame != null &&
                    _h1Frame.Direction != 0 &&
                    _h1Frame.Direction !=
                    d.Direction;

                bool h4Against =
                    _h4Frame != null &&
                    _h4Frame.Direction != 0 &&
                    _h4Frame.Direction !=
                    d.Direction;

                // ENHANCEMENT (signal accuracy): a D1-opposed trade is the
                // single riskiest disagreement a professional would flag —
                // daily structure was previously never checked here, so a
                // setup could pass with H1/H4 aligned while fighting the
                // daily trend outright. D1 opposition now applies the same
                // penalty, plus an extra half-penalty since it is the more
                // consequential disagreement of the three.
                bool d1Against =
                    _d1Frame != null &&
                    _d1Frame.Direction != 0 &&
                    _d1Frame.Direction !=
                    d.Direction;

                if (h1Against ||
                    h4Against ||
                    d1Against)
                {
                    int penalty =
                        HigherTfPenalty +
                        (d1Against
                            ? HigherTfPenalty / 2
                            : 0);

                    d.Confidence =
                        ClampInt(
                            d.Confidence -
                            penalty,
                            0,
                            100);
                }
            }

            d.RetestQuality =
                RetestQuality(
                    _m5Bars,
                    closedM5,
                    d.Direction);

            d.TriggerReady =
                EntryTriggerReady(
                    _m5Bars,
                    closedM5,
                    d.Direction);

            d.EntryAllowed =
                PassesDecisionFilters(
                    chartIndex,
                    closedM5,
                    reference,
                    d,
                    out d.BlockReason);

            d.Reason =
                BuildReason(
                    d,
                    buyShare,
                    sellShare);

            return d;
        }

        private void AddFrame(
            Frame frame,
            double weight,
            ref double buy,
            ref double sell,
            ref int evidence)
        {
            if (frame == null || frame.Quality <= 0 || weight <= 0)
                return;

            double scale =
                weight / 10.0;

            buy += frame.BullScore * scale;
            sell += frame.BearScore * scale;

            if (frame.Direction != 0)
                evidence++;
        }

        private int TimeframeAgreement(int direction)
        {
            Frame[] frames =
            {
                _m5Frame,
                _m15Frame,
                _m30Frame,
                _h1Frame,
                _h4Frame,
                _d1Frame,
                _w1Frame
            };

            int total = 0;
            int aligned = 0;

            for (int i = 0; i < frames.Length; i++)
            {
                if (frames[i] == null ||
                    frames[i].Quality <= 0)
                    continue;

                total++;

                if (frames[i].Direction == direction)
                    aligned++;
            }

            return
                total == 0
                    ? 0
                    : ClampInt(
                        (int)Math.Round(
                            100.0 * aligned / total),
                        0,
                        100);
        }

        private int IndependentEvidence(
            int direction)
        {
            if (_m5Frame == null ||
                direction == 0)
                return 0;

            int count = 0;

            if (direction == 1)
            {
                if (_m5Frame.StructureBull) count++;
                if (_m5Frame.LiquidityBull) count++;
                if (_m5Frame.FvgBull) count++;
                if (_m5Frame.ObBull) count++;
                if (_m5Frame.DisplacementBull) count++;
                if (_m5Frame.MomentumBull) count++;
                if (_m5Frame.VolumeBull) count++;
                if (_m5Frame.MacdBull) count++;
                if (_m5Frame.VwapBull) count++;
                if (_m5Frame.VolatilityBull) count++;
            }
            else
            {
                if (_m5Frame.StructureBear) count++;
                if (_m5Frame.LiquidityBear) count++;
                if (_m5Frame.FvgBear) count++;
                if (_m5Frame.ObBear) count++;
                if (_m5Frame.DisplacementBear) count++;
                if (_m5Frame.MomentumBear) count++;
                if (_m5Frame.VolumeBear) count++;
                if (_m5Frame.MacdBear) count++;
                if (_m5Frame.VwapBear) count++;
                if (_m5Frame.VolatilityBear) count++;
            }

            return count;
        }

        private int StructuralConfirmations(
            int direction)
        {
            if (_m5Frame == null ||
                direction == 0)
                return 0;

            int count = 0;

            if (direction == 1)
            {
                if (_m5Frame.StructureBull) count++;
                if (_m5Frame.MssBull ||
                    _m5Frame.ChochBull) count++;
                if (_m5Frame.DisplacementBull) count++;
                if (_m15Frame != null &&
                    _m15Frame.StructureBull) count++;
                if (_h1Frame != null &&
                    _h1Frame.StructureBull) count++;
                if (_h4Frame != null &&
                    _h4Frame.StructureBull) count++;
            }
            else
            {
                if (_m5Frame.StructureBear) count++;
                if (_m5Frame.MssBear ||
                    _m5Frame.ChochBear) count++;
                if (_m5Frame.DisplacementBear) count++;
                if (_m15Frame != null &&
                    _m15Frame.StructureBear) count++;
                if (_h1Frame != null &&
                    _h1Frame.StructureBear) count++;
                if (_h4Frame != null &&
                    _h4Frame.StructureBear) count++;
            }

            return count;
        }

        private bool CanAcceptConfirmedDirection(
            int direction,
            int closedM5)
        {
            if (direction == 0)
                return false;

            if (!PreventRapidDirectionFlip)
                return true;

            if (_lastConfirmedDirection == 0 ||
                _lastConfirmedM5 < 0)
                return true;

            int elapsed =
                closedM5 -
                _lastConfirmedM5;

            if (elapsed < 0)
                return false;

            if (direction ==
                _lastConfirmedDirection)
            {
                if (_plan != null)
                    return false;

                if (_lastExitM5 >= 0 &&
                    closedM5 -
                    _lastExitM5 <
                    Math.Max(
                        0,
                        ExitReentryCooldownM5))
                    return false;

                return true;
            }

            if (elapsed <
                Math.Max(
                    1,
                    OppositeSignalCooldownM5))
                return false;

            if (_plan != null &&
                !AllowOppositeWhileActive)
                return false;

            if (RequireM15ReversalForOpposite)
            {
                if (_m15Frame == null ||
                    _m15Frame.Direction !=
                    direction)
                    return false;

                bool structural =
                    direction == 1
                        ? (_m15Frame.MssBull ||
                           _m15Frame.ChochBull)
                        : (_m15Frame.MssBear ||
                           _m15Frame.ChochBear);

                bool force =
                    direction == 1
                        ? (_m15Frame.DisplacementBull &&
                           _m15Frame.LiquidityBull)
                        : (_m15Frame.DisplacementBear &&
                           _m15Frame.LiquidityBear);

                if (!(RequireReversalForce
                        ? structural && force
                        : structural || force))
                    return false;

                int m15Index =
                    ClosedIndex(
                        _m15Bars,
                        _m5Bars.OpenTimes[closedM5]);

                if (m15Index >= 0 &&
                    !StableDirection(
                        _m15Bars,
                        m15Index,
                        direction,
                        Math.Max(
                            1,
                            SmartFlipConfirmationBars)))
                    return false;
            }

            int m5Evidence = 0;

            if (_m5Frame != null)
            {
                if (direction == 1)
                {
                    if (_m5Frame.MssBull) m5Evidence++;
                    if (_m5Frame.ChochBull) m5Evidence++;
                    if (_m5Frame.DisplacementBull) m5Evidence++;
                    if (_m5Frame.LiquidityBull) m5Evidence++;
                }
                else
                {
                    if (_m5Frame.MssBear) m5Evidence++;
                    if (_m5Frame.ChochBear) m5Evidence++;
                    if (_m5Frame.DisplacementBear) m5Evidence++;
                    if (_m5Frame.LiquidityBear) m5Evidence++;
                }
            }

            return
                m5Evidence >=
                Math.Max(
                    1,
                    MinimumOppositeM5Structure);
        }

        private bool PassesDecisionFilters(
            int chartIndex,
            int closedM5,
            DateTime reference,
            Decision d,
            out string reason)
        {
            reason = "";

            if (d == null ||
                d.Direction == 0)
            {
                reason = "NO DIRECTION";
                return false;
            }

            int adaptiveQualityThreshold;
            int adaptiveShareThreshold;
            int adaptiveEdgeThreshold;

            GetAdaptiveSmartThresholds(
                d.Regime,
                out adaptiveQualityThreshold,
                out adaptiveShareThreshold,
                out adaptiveEdgeThreshold);

            if (d.Confidence < MinimumConfidence)
            {
                reason = "CONFIDENCE";
                return false;
            }

            if (d.Edge < adaptiveEdgeThreshold)
            {
                reason = "EDGE";
                return false;
            }

            if (d.SmartQuality < adaptiveQualityThreshold)
            {
                reason = "SMART QUALITY";
                return false;
            }

            if (RequireHigherTfAgreement &&
                d.TimeframeAgreement <
                MinimumTimeframeAgreement)
            {
                reason = "MTF AGREEMENT";
                return false;
            }

            if (d.IndependentEvidence <
                Math.Max(
                    MinimumIndependentEvidence,
                    EnableSmartDecisionEngine
                        ? SmartMinimumIndependentEvidence
                        : 0))
            {
                reason = "INDEPENDENT EVIDENCE";
                return false;
            }

            if (RequireStructuralConfirmation &&
                d.StructuralConfirmations <
                MinimumStructuralConfirmations)
            {
                reason = "STRUCTURE";
                return false;
            }

            if (RequireCoreAgreement &&
                _m5Frame != null &&
                _m15Frame != null)
            {
                bool aligned =
                    d.Direction == 1
                        ? _m5Frame.Direction == 1 &&
                          (_m15Frame.Direction == 1 ||
                           (_m15Frame.Direction == 0 &&
                            AllowM15NeutralPullback))
                        : _m5Frame.Direction == -1 &&
                          (_m15Frame.Direction == -1 ||
                           (_m15Frame.Direction == 0 &&
                            AllowM15NeutralPullback));

                if (!aligned)
                {
                    reason = "CORE ALIGNMENT";
                    return false;
                }
            }

            if (UseM5Confirmation &&
                (_m5Frame == null ||
                 _m5Frame.Direction !=
                 d.Direction))
            {
                reason = "M5 CONFIRMATION";
                return false;
            }

            if (M5OnlyConfirmedTrigger &&
                !EntryTriggerReady(
                    _m5Bars,
                    closedM5,
                    d.Direction))
            {
                bool directOverride =
                    AllowDirectDisplacementOverride &&
                    d.Confidence >=
                    SmartStrongSetupQuality &&
                    d.Edge >=
                    DirectDisplacementOverrideScore &&
                    _m5Frame != null &&
                    (d.Direction == 1
                        ? _m5Frame.DisplacementBull
                        : _m5Frame.DisplacementBear);

                bool strongOverride =
                    AllowStrongTriggerOverride &&
                    AllowStrongM5TriggerOverride &&
                    d.Confidence >= 85 &&
                    d.Edge >= 20;

                if (!(directOverride ||
                      strongOverride))
                {
                    reason = "M5 TRIGGER";
                    return false;
                }
            }

            // ENHANCEMENT (day-trading / M5 trigger alignment): M1 Trigger
            // now defaults OFF. The actual trigger evaluation (candle
            // pattern score + precision entry model, gated by
            // M5OnlyConfirmedTrigger) already runs entirely on M5 bars —
            // M1 only ever added a +3 confluence bias and this veto. For a
            // stated M5-trigger, day-trading workflow that veto was mostly
            // reacting to 1-minute noise the M5 close hadn't confirmed yet.
            // Still available for anyone who wants the extra M1 filter —
            // just off by default now.
            if (UseM1Trigger &&
                _m1Frame != null &&
                _m1Frame.Direction != 0 &&
                _m1Frame.Direction !=
                d.Direction)
            {
                reason = "M1 MISALIGNMENT";
                return false;
            }

            if (EnableSmartDecisionEngine)
            {
                int strongest =
                    Math.Max(
                        d.BuyShare,
                        d.SellShare);

                if (RequireSmartConsensus &&
                    strongest <
                    Math.Max(
                        SmartConsensusThreshold,
                        adaptiveShareThreshold))
                {
                    bool soft =
                        AllowSmartSoftGate &&
                        d.SmartQuality >=
                        SmartStrongSetupQuality &&
                        d.Edge >=
                        SmartStrongSetupEdge &&
                        d.IndependentEvidence >=
                        SmartMinimumIndependentEvidence + 1;

                    if (!soft)
                    {
                        reason = "SMART CONSENSUS";
                        return false;
                    }
                }

                if (d.TimeframeAgreement <
                    SmartMinimumTimeframeAgreement)
                {
                    reason = "SMART MTF";
                    return false;
                }
            }

            if (UseSmartEntryQualityFilter &&
                d.SmartQuality <
                Math.Max(
                    SmartQualityThreshold,
                    Math.Max(
                        adaptiveQualityThreshold,
                        EnableSmartDecisionEngine
                            ? SmartMinimumConsensusFloor()
                            : 0)))
            {
                reason = "SMART QUALITY";
                return false;
            }

            if (UseStructuralSequenceGate &&
                StructuralSequence(
                    _m5Bars,
                    closedM5,
                    d.Direction) <
                MinimumStructuralSequence)
            {
                reason = "STRUCTURAL SEQUENCE";
                return false;
            }

            if (UseZoneConfluence &&
                RequireEntryLocationConfluence &&
                EntryLocationQuality(
                    _m5Bars,
                    closedM5,
                    d.Direction) <
                MinimumEntryLocationQuality)
            {
                reason = "ENTRY LOCATION";
                return false;
            }

            if (UseProxyExpectedValueGate &&
                ProxyExpectedValue(
                    d.SmartQuality,
                    Math.Max(
                        1.0,
                        SmartTargetMinimumRR)) <
                MinimumProxyExpectedValue)
            {
                reason = "EXPECTED VALUE";
                return false;
            }

            if (UseRegimeNoTradeGuard &&
                NoTradeRegimeBlocked(
                    d.Regime,
                    d.SmartQuality))
            {
                reason = "REGIME NO-TRADE";
                return false;
            }

            if (UseHistoricalChoppinessGuard &&
                _m5Frame != null &&
                _m15Frame != null &&
                _m5Frame.Choppy &&
                _m15Frame.Choppy &&
                d.SmartQuality <
                Math.Max(
                    SmartRegimeQualityFloor + 5,
                    NoTradeMinimumSmartQuality + 5))
            {
                reason = "CHOP";
                return false;
            }

            if (RequireStableM5Direction &&
                !StableDirection(
                    _m5Bars,
                    closedM5,
                    d.Direction,
                    StableM5Bars))
            {
                reason = "M5 STABILITY";
                return false;
            }

            int m15Closed =
                ClosedIndex(
                    _m15Bars,
                    reference);

            if (RequireStableM15Direction &&
                m15Closed >=
                StableM15Bars + 5 &&
                !StableDirection(
                    _m15Bars,
                    m15Closed,
                    d.Direction,
                    StableM15Bars))
            {
                reason = "M15 STABILITY";
                return false;
            }

            if (RequireRetestQuality &&
                d.RetestQuality <
                MinimumRetestQuality)
            {
                bool retestOverride =
                    AllowStrongTriggerOverride &&
                    d.Confidence >= 85 &&
                    d.Edge >= 20 &&
                    d.IndependentEvidence >=
                    MinimumIndependentEvidence + 1;

                if (!retestOverride)
                {
                    reason = "RETEST QUALITY";
                    return false;
                }
            }

            if (!d.TriggerReady)
            {
                reason = "TRIGGER";
                return false;
            }

            if (!SessionAllowed(
                    DateTime.UtcNow))
            {
                reason = "SESSION";
                return false;
            }

            if (!FridayAllowed(
                    DateTime.UtcNow))
            {
                reason = "FRIDAY";
                return false;
            }

            if (!SpreadAllowed(
                    _m5Bars,
                    closedM5))
            {
                reason = "SPREAD";
                return false;
            }

            if ((UseVolatilityGuard ||
                 UseVolatilityEventGuard) &&
                VolatilityBlocked(
                    _m5Bars,
                    closedM5))
            {
                reason = "VOLATILITY GUARD";
                return false;
            }

            if (UseNewsEventGuard &&
                NewsBlocked(
                    DateTime.UtcNow,
                    out reason))
                return false;

            if (!CanAcceptConfirmedDirection(
                    d.Direction,
                    closedM5))
            {
                reason = "DIRECTION FLIP";
                return false;
            }

            if (CooldownBlocked(
                    closedM5))
            {
                reason = "COOLDOWN";
                return false;
            }

            return true;
        }

        private bool RestrictionAlertEnabled(
            string reason)
        {
            if (string.IsNullOrWhiteSpace(
                    reason))
                return AlertOnEntryRestriction;

            switch (reason)
            {
                case "NEWS BLACKOUT":
                    return AlertOnNewsEventGuard ||
                           AlertOnNewsEvent ||
                           AlertOnEntryRestriction;

                case "SESSION":
                    return AlertOnSessionBlock ||
                           AlertOnEntryRestriction;

                case "SPREAD":
                    return AlertOnSpreadBlock ||
                           AlertOnEntryRestriction;

                case "FRIDAY":
                    return AlertOnFridayBlock ||
                           AlertOnEntryRestriction;

                case "REGIME NO-TRADE":
                case "CHOP":
                    return AlertOnRegimeNoTrade ||
                           AlertOnEntryRestriction;

                case "COOLDOWN":
                case "DIRECTION FLIP":
                    return AlertOnCooldownBlock ||
                           AlertOnEntryRestriction;

                default:
                    return AlertOnEntryRestriction;
            }
        }

        private string BuildReason(
            Decision d,
            int buyShare,
            int sellShare)
        {
            return
                (d.Direction == 1
                    ? "BUY"
                    : "SELL") +
                " | CONF " +
                d.Confidence +
                " | EDGE " +
                d.Edge +
                " | SMART " +
                d.SmartQuality +
                " | MTF " +
                d.TimeframeAgreement +
                " | EVID " +
                d.IndependentEvidence +
                " | STRUCT " +
                d.StructuralConfirmations +
                " | RETEST " +
                d.RetestQuality +
                " | REGIME " +
                d.Regime +
                " | " +
                buyShare +
                "/" +
                sellShare +
                (string.IsNullOrWhiteSpace(d.BlockReason)
                    ? ""
                    : " | BLOCK " +
                      d.BlockReason);
        }

        private int PremiumDiscountBias(
            Bars bars,
            int index)
        {
            if (!UsePremiumDiscount ||
                bars == null ||
                index < 10)
                return 0;

            double high =
                Highest(
                    bars,
                    Math.Max(
                        0,
                        index -
                        StructureLookback),
                    index);

            double low =
                Lowest(
                    bars,
                    Math.Max(
                        0,
                        index -
                        StructureLookback),
                    index);

            if (high <= low)
                return 0;

            double midpoint =
                (high + low) * 0.5;

            if (bars.ClosePrices[index] <
                midpoint)
                return 1;

            if (bars.ClosePrices[index] >
                midpoint)
                return -1;

            return 0;
        }

        private double LiveBias(
            int chartIndex,
            int direction)
        {
            if (Bars == null ||
                Bars.Count < 10)
                return 0;

            int i =
                Math.Max(
                    1,
                    Math.Min(
                        chartIndex,
                        Bars.Count - 1));

            double fast =
                Ema(
                    Bars,
                    i,
                    true);

            double slow =
                Ema(
                    Bars,
                    i,
                    false);

            if (direction == 1 &&
                Bars.ClosePrices[i] > fast &&
                fast > slow)
                return 5;

            if (direction == -1 &&
                Bars.ClosePrices[i] < fast &&
                fast < slow)
                return 5;

            return 0;
        }

        // ============================================================
        #endregion

        #region Live Reaction
        // ============================================================

        private Decision BuildReaction()
        {
            if (!EnableLiveReaction ||
                !EnableFastReversalIntelligence ||
                _m5Bars == null ||
                _m5Bars.Count < 10)
                return new Decision();

            int live =
                _m5Bars.Count - 1;

            double atr =
                Atr(
                    _m5Bars,
                    Math.Max(
                        1,
                        live - 1));

            if (atr <= 0)
                return new Decision();

            int buyQuality;
            int buyEvidence;
            int sellQuality;
            int sellEvidence;

            ReversalQuality(
                _m5Bars,
                live,
                1,
                atr,
                out buyQuality,
                out buyEvidence);

            ReversalQuality(
                _m5Bars,
                live,
                -1,
                atr,
                out sellQuality,
                out sellEvidence);

            int watchThreshold =
                Math.Max(
                    50,
                    Math.Max(
                        FastReversalMinimumQuality,
                        LiveReactionWatchThreshold));

            int entryThreshold =
                Math.Max(
                    watchThreshold,
                    LiveReactionThreshold);

            int strongThreshold =
                Math.Max(
                    entryThreshold,
                    LiveReactionStrongThreshold);

            if (buyQuality < watchThreshold &&
                sellQuality < watchThreshold)
                return new Decision();

            Decision d =
                new Decision();

            if (buyQuality >= sellQuality)
            {
                d.Direction = 1;
                d.Confidence = buyQuality;
                d.IndependentEvidence = buyEvidence;
            }
            else
            {
                d.Direction = -1;
                d.Confidence = sellQuality;
                d.IndependentEvidence = sellEvidence;
            }

            d.SmartQuality =
                d.Confidence;

            d.EntryAllowed =
                d.Confidence >=
                entryThreshold &&
                d.IndependentEvidence >=
                Math.Max(
                    2,
                    LiveReversalMinimumEvidence);

            if (!AllowFastM5ReversalBeforeM15 &&
                (_m15Frame == null ||
                 _m15Frame.Direction !=
                 d.Direction))
            {
                d.EntryAllowed = false;
                d.BlockReason =
                    "M15 REVERSAL";
            }

            if (d.EntryAllowed)
            {
                Zone reactionZone =
                    FindNearestOpposingZone(
                        _m5Bars,
                        live,
                        d.Direction,
                        atr);

                if (reactionZone != null &&
                    reactionZone.Quality <
                    FastReversalMinimumZoneQuality)
                {
                    d.EntryAllowed = false;
                    d.BlockReason =
                        "REACTION ZONE";
                }
            }

            d.Reason =
                (d.Confidence >= strongThreshold
                    ? "STRONG "
                    : "") +
                (d.Direction == 1
                    ? "BUY"
                    : "SELL") +
                " REACTION | Q " +
                d.Confidence +
                " | EVID " +
                d.IndependentEvidence;

            return d;
        }

        private void ReversalQuality(
            Bars bars,
            int index,
            int direction,
            double atr,
            out int quality,
            out int evidence)
        {
            quality = 0;
            evidence = 0;

            if (bars == null ||
                index < 6 ||
                atr <= 0)
                return;

            double range =
                Math.Max(
                    Symbol.PipSize,
                    bars.HighPrices[index] -
                    bars.LowPrices[index]);

            double body =
                Math.Abs(
                    bars.ClosePrices[index] -
                    bars.OpenPrices[index]);

            bool directional =
                direction == 1
                    ? bars.ClosePrices[index] >
                      bars.OpenPrices[index]
                    : bars.ClosePrices[index] <
                      bars.OpenPrices[index];

            double closeLocation =
                direction == 1
                    ? (bars.ClosePrices[index] -
                       bars.LowPrices[index]) /
                      range
                    : (bars.HighPrices[index] -
                       bars.ClosePrices[index]) /
                      range;

            bool closeStrong =
                closeLocation >=
                MinimumCloseLocation;

            bool displacement =
                body >=
                atr *
                Math.Max(
                    MinimumTriggerBodyAtr,
                    0.60);

            int lookback =
                Math.Max(
                    2,
                    FastReversalLookbackBars);

            int previous =
                Math.Max(
                    0,
                    index - lookback);

            bool breakMicro =
                direction == 1
                    ? bars.ClosePrices[index] >
                      Highest(
                          bars,
                          previous,
                          index - 1)
                    : bars.ClosePrices[index] <
                      Lowest(
                          bars,
                          previous,
                          index - 1);

            double rsi =
                Rsi(
                    bars,
                    index);

            bool rsiSupport =
                direction == 1
                    ? rsi >= 52
                    : rsi <= 48;

            bool emaSupport =
                direction == 1
                    ? Ema(
                        bars,
                        index,
                        true) >
                      Ema(
                        bars,
                        index,
                        false)
                    : Ema(
                        bars,
                        index,
                        true) <
                      Ema(
                        bars,
                        index,
                        false);

            if (directional)
            {
                quality += 20;
                evidence++;
            }

            if (closeStrong)
            {
                quality += 18;
                evidence++;
            }

            if (displacement)
            {
                quality += 22;
                evidence++;
            }

            if (breakMicro)
            {
                quality += 22;
                evidence++;
            }

            if (rsiSupport)
            {
                quality += 8;
                evidence++;
            }

            if (emaSupport)
            {
                quality += 10;
                evidence++;
            }

            Zone reversalZone =
                FindNearestOpposingZone(
                    bars,
                    index,
                    direction,
                    atr);

            if (reversalZone != null &&
                reversalZone.Quality >=
                FastReversalMinimumZoneQuality)
            {
                quality += 10;
                evidence++;
            }

            quality =
                ClampInt(
                    quality,
                    0,
                    100);
        }

        // ============================================================
        #endregion

        #region Entry Trigger
        // ============================================================

        private bool TriggerReadyWithoutPrecisionGate(
            Bars bars,
            int index,
            int direction)
        {
            if (bars == null ||
                index < 20 ||
                index >= bars.Count)
                return false;

            double atr =
                Atr(
                    bars,
                    index);

            if (atr <= 0)
                return false;

            double range =
                bars.HighPrices[index] -
                bars.LowPrices[index];

            double body =
                Math.Abs(
                    bars.ClosePrices[index] -
                    bars.OpenPrices[index]);

            if (range <= 0 ||
                body < atr * MinimumTriggerBodyAtr ||
                range > atr * MaximumTriggerRangeAtr)
                return false;

            double location =
                direction == 1
                    ? (bars.ClosePrices[index] -
                       bars.LowPrices[index]) /
                      range
                    : (bars.HighPrices[index] -
                       bars.ClosePrices[index]) /
                      range;

            if (location < MinimumCloseLocation)
                return false;

            int trigger =
                direction == 1
                    ? BullTriggerScore(
                        bars,
                        index)
                    : BearTriggerScore(
                        bars,
                        index);

            int requiredTrigger =
                UsePrecisionExecutionModel
                    ? Math.Max(
                        LiveTriggerScore,
                        PrecisionTriggerScore)
                    : LiveTriggerScore;

            bool breakReady =
                direction == 1
                    ? bars.ClosePrices[index] >
                      Highest(
                          bars,
                          Math.Max(
                              0,
                              index - 6),
                          index - 1)
                    : bars.ClosePrices[index] <
                      Lowest(
                          bars,
                          Math.Max(
                              0,
                              index - 6),
                          index - 1);

            double market =
                direction == 1
                    ? Symbol.Ask
                    : Symbol.Bid;

            double buffer =
                atr *
                Math.Max(
                    0,
                    EntryBufferAtr);

            bool bufferPassed =
                direction == 1
                    ? market >=
                      bars.ClosePrices[index] +
                      buffer
                    : market <=
                      bars.ClosePrices[index] -
                      buffer;

            bool extensionOk =
                Math.Abs(
                    market -
                    bars.ClosePrices[index]) <=
                atr *
                MaximumEntryExtensionAtr;

            if (RequireFreshM5Trigger &&
                FreshTriggerEvidence(
                    bars,
                    index,
                    direction) <
                MinimumFreshTriggerEvidence)
            {
                bool overrideOk =
                    AllowDirectDisplacementOverride &&
                    UseDisplacement &&
                    trigger >=
                    ClampInt(
                        DirectDisplacementOverrideScore,
                        1,
                        6) &&
                    (direction == 1
                        ? BullDisplacement(
                            bars,
                            index,
                            atr)
                        : BearDisplacement(
                            bars,
                            index,
                            atr));

                if (!overrideOk)
                    return false;
            }

            return
                ((trigger >=
                  Math.Max(
                      4,
                      requiredTrigger) &&
                  breakReady &&
                  bufferPassed) ||
                 (trigger >= 5 &&
                  breakReady &&
                  extensionOk &&
                  bufferPassed));
        }

                private bool EntryTriggerReady(
            Bars bars,
            int index,
            int direction)
        {
            if (RequirePrecisionEntry &&
                ReferenceEquals(
                    bars,
                    _m5Bars))
            {
                ExecutionModel model =
                    BuildExecutionModel(
                        index,
                        direction);

                if (model != null &&
                    model.Ready)
                    return true;
            }

            return
                TriggerReadyWithoutPrecisionGate(
                    bars,
                    index,
                    direction);
        }

        private int BullTriggerScore(
            Bars bars,
            int index)
        {
            int score = 0;

            double atr = Atr(bars, index);

            double range =
                Math.Max(
                    Symbol.PipSize,
                    bars.HighPrices[index] -
                    bars.LowPrices[index]);

            double body =
                Math.Abs(
                    bars.ClosePrices[index] -
                    bars.OpenPrices[index]);

            if (bars.ClosePrices[index] >
                bars.OpenPrices[index])
                score++;

            if (body >= atr * MinimumTriggerBodyAtr)
                score++;

            if ((bars.ClosePrices[index] -
                 bars.LowPrices[index]) /
                range >= MinimumCloseLocation)
                score++;

            if (bars.ClosePrices[index] >
                Ema(bars, index, true))
                score++;

            if (Rsi(bars, index) >= 50)
                score++;

            if (DmiBias(bars, index) >= 0)
                score++;

            if (UseDisplacement &&
                body >= atr * DisplacementAtr)
                score++;

            return Math.Min(
                6,
                score);
        }

        private int BearTriggerScore(
            Bars bars,
            int index)
        {
            int score = 0;

            double atr = Atr(bars, index);

            double range =
                Math.Max(
                    Symbol.PipSize,
                    bars.HighPrices[index] -
                    bars.LowPrices[index]);

            double body =
                Math.Abs(
                    bars.ClosePrices[index] -
                    bars.OpenPrices[index]);

            if (bars.ClosePrices[index] <
                bars.OpenPrices[index])
                score++;

            if (body >= atr * MinimumTriggerBodyAtr)
                score++;

            if ((bars.HighPrices[index] -
                 bars.ClosePrices[index]) /
                range >= MinimumCloseLocation)
                score++;

            if (bars.ClosePrices[index] <
                Ema(bars, index, true))
                score++;

            if (Rsi(bars, index) <= 50)
                score++;

            if (DmiBias(bars, index) <= 0)
                score++;

            if (UseDisplacement &&
                body >= atr * DisplacementAtr)
                score++;

            return Math.Min(
                6,
                score);
        }

        // ============================================================
        #endregion

        #region Plan Engine
        // ============================================================

        private bool IsTriggerReached(
            int direction,
            double market,
            double trigger)
        {
            if (!IsFinitePositive(market) ||
                !IsFinitePositive(trigger))
                return false;

            double tolerance =
                Math.Max(
                    Symbol.TickSize,
                    Symbol.PipSize * 0.10);

            return direction == 1
                ? market >= trigger - tolerance
                : direction == -1 &&
                  market <= trigger + tolerance;
        }

        private bool IsContinuationExecutionContext(
            int direction)
        {
            if (_decision == null ||
                _decision.Direction != direction ||
                _m5Frame == null ||
                _m15Frame == null)
                return false;

            bool m5Aligned =
                _m5Frame.Direction == direction;

            bool m15Compatible =
                _m15Frame.Direction == direction ||
                (_m15Frame.Direction == 0 &&
                 AllowM15NeutralPullback);

            bool trendRegime =
                string.Equals(
                    _decision.Regime,
                    "TREND",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    _decision.Regime,
                    "EXPANSION",
                    StringComparison.OrdinalIgnoreCase);

            return
                m5Aligned &&
                m15Compatible &&
                (trendRegime ||
                 _decision.StructuralConfirmations >=
                 Math.Max(
                     3,
                     MinimumStructuralConfirmations));
        }

        private string ExecutionModeText(
            CFIPClean70ExecutionMode mode)
        {
            switch (mode)
            {
                case CFIPClean70ExecutionMode.WaitingForTrigger:
                    return "WAIT TRIGGER";
                case CFIPClean70ExecutionMode.RetestMarket:
                    return "RETEST MARKET";
                case CFIPClean70ExecutionMode.BreakoutMarket:
                    return "BREAKOUT MARKET";
                case CFIPClean70ExecutionMode.ContinuationStop:
                    return "CONTINUATION STOP";
                case CFIPClean70ExecutionMode.ReversalLimit:
                    return "REVERSAL LIMIT";
                default:
                    return "NONE";
            }
        }

        private bool IsExecutableMarketEntry(
            Plan plan,
            double market,
            out string reason)
        {
            reason = "OK";

            if (plan == null)
            {
                reason = "NO PLAN";
                return false;
            }

            if (!IsFinitePositive(market))
            {
                reason = "INVALID MARKET";
                return false;
            }

            if (plan.EntryMode ==
                CFIPClean70ExecutionMode.BreakoutMarket)
            {
                if (!IsTriggerReached(
                        plan.Direction,
                        market,
                        plan.EntryTrigger))
                {
                    reason = "WAITING FOR TRIGGER";
                    return false;
                }

                return true;
            }

            if (plan.EntryMode ==
                CFIPClean70ExecutionMode.RetestMarket)
            {
                double tolerance =
                    Math.Max(
                        Symbol.TickSize,
                        Math.Max(
                            Symbol.PipSize,
                            Symbol.Ask - Symbol.Bid));

                if (market <
                        plan.EntryZoneLow - tolerance ||
                    market >
                        plan.EntryZoneHigh + tolerance)
                {
                    reason = "OUTSIDE RETEST ZONE";
                    return false;
                }

                return true;
            }

            reason =
                ExecutionModeText(
                    plan.EntryMode);
            return false;
        }

        private bool IsExecutableFillPrice(
            Plan plan,
            double fillPrice,
            out string reason)
        {
            reason = "OK";

            if (plan == null ||
                !IsFinitePositive(fillPrice))
            {
                reason = "INVALID FILL";
                return false;
            }

            if (plan.EntryMode ==
                CFIPClean70ExecutionMode.BreakoutMarket)
            {
                if (!IsFinitePositive(
                        plan.EntryTrigger))
                {
                    reason = "MISSING BREAKOUT TRIGGER";
                    return false;
                }

                double tolerance =
                    Math.Max(
                        Symbol.TickSize * 2,
                        Math.Max(
                            Symbol.PipSize * 0.5,
                            (Symbol.Ask - Symbol.Bid) * 2));

                bool acceptable =
                    plan.Direction == 1
                        ? fillPrice >=
                          plan.EntryTrigger - tolerance
                        : fillPrice <=
                          plan.EntryTrigger + tolerance;

                if (!acceptable)
                {
                    reason =
                        "BROKER FILL FAR FROM TRIGGER";
                    return false;
                }

                if (!IsTriggerReached(
                        plan.Direction,
                        fillPrice,
                        plan.EntryTrigger))
                {
                    reason =
                        "BREAKOUT FILL • SLIPPAGE ACCEPTED";
                }

                return true;
            }

            if (plan.EntryMode ==
                CFIPClean70ExecutionMode.RetestMarket)
                return IsExecutableMarketEntry(
                    plan,
                    fillPrice,
                    out reason);

            reason =
                "INVALID EXECUTION MODE";
            return false;
        }

        private ExecutionModel BuildExecutionModel(
            int closedM5,
            int direction)
        {
            ExecutionModel model =
                new ExecutionModel
                {
                    Direction = direction,
                    Mode = CFIPClean70ExecutionMode.None,
                    Source = "NONE"
                };

            if (_m5Bars == null ||
                closedM5 < 20 ||
                (direction != 1 && direction != -1))
                return model;

            double atr =
                Atr(
                    _m5Bars,
                    closedM5);

            if (atr <= 0)
                return model;

            double market =
                NormalizePrice(
                    direction == 1
                        ? Symbol.Ask
                        : Symbol.Bid);

            Zone m5Fvg =
                FindNearestFvg(
                    _m5Bars,
                    closedM5,
                    direction,
                    atr);

            Zone m5Ob =
                FindNearestOrderBlock(
                    _m5Bars,
                    closedM5,
                    direction,
                    atr);

            Zone m15Fvg = null;
            Zone m15Ob = null;

            int m15Index =
                ClosedIndex(
                    _m15Bars,
                    _m5Bars.OpenTimes[closedM5]);

            double m15Atr =
                m15Index >= 10
                    ? Atr(_m15Bars, m15Index)
                    : 0;

            if (m15Index >= 10 && m15Atr > 0)
            {
                m15Fvg =
                    FindNearestFvg(
                        _m15Bars,
                        m15Index,
                        direction,
                        m15Atr);

                m15Ob =
                    FindNearestOrderBlock(
                        _m15Bars,
                        m15Index,
                        direction,
                        m15Atr);
            }

            double low = 0;
            double high = 0;
            string source = "NONE";
            int quality = 0;

            if (m5Fvg != null && m5Ob != null)
            {
                double overlapLow =
                    Math.Max(m5Fvg.Low, m5Ob.Low);

                double overlapHigh =
                    Math.Min(m5Fvg.High, m5Ob.High);

                if (overlapHigh >= overlapLow)
                {
                    low = overlapLow;
                    high = overlapHigh;
                    source = "M5 FVG+OB";
                    quality = 92;
                }
            }

            if (quality == 0 && m5Fvg != null)
            {
                low = m5Fvg.Low;
                high = m5Fvg.High;
                source = "M5 FVG";
                quality = 84;
            }

            if (quality == 0 && m5Ob != null)
            {
                low = m5Ob.Low;
                high = m5Ob.High;
                source = "M5 ORDER_BLOCK";
                quality = 86;
            }

            if (quality == 0 &&
                m15Fvg != null &&
                m15Ob != null)
            {
                double overlapLow =
                    Math.Max(m15Fvg.Low, m15Ob.Low);

                double overlapHigh =
                    Math.Min(m15Fvg.High, m15Ob.High);

                if (overlapHigh >= overlapLow)
                {
                    low = overlapLow;
                    high = overlapHigh;
                    source = "M15 FVG+OB";
                    quality = 86;
                }
            }

            if (quality == 0 && m15Fvg != null)
            {
                low = m15Fvg.Low;
                high = m15Fvg.High;
                source = "M15 FVG";
                quality = 78;
            }

            if (quality == 0 && m15Ob != null)
            {
                low = m15Ob.Low;
                high = m15Ob.High;
                source = "M15 ORDER_BLOCK";
                quality = 80;
            }

            if (quality > 0 &&
                m15Fvg != null &&
                m15Ob != null)
            {
                double overlapLow =
                    Math.Max(
                        low,
                        Math.Max(m15Fvg.Low, m15Ob.Low));

                double overlapHigh =
                    Math.Min(
                        high,
                        Math.Min(m15Fvg.High, m15Ob.High));

                if (overlapHigh > overlapLow)
                {
                    low = overlapLow;
                    high = overlapHigh;
                    quality += 5;
                    source += "+MTF";
                }
            }

            if (quality == 0)
            {
                double swing =
                    direction == 1
                        ? FindSwingLowBelow(
                            _m5Bars,
                            closedM5,
                            market)
                        : FindSwingHighAbove(
                            _m5Bars,
                            closedM5,
                            market);

                if (IsFinitePositive(swing))
                {
                    if (direction == 1)
                    {
                        low = swing;
                        high =
                            swing +
                            atr *
                            Math.Max(
                                0.10,
                                ExecutionZoneAtr);
                    }
                    else
                    {
                        low =
                            swing -
                            atr *
                            Math.Max(
                                0.10,
                                ExecutionZoneAtr);
                        high = swing;
                    }

                    source = "M5 SWING";
                    quality = 70;
                }
            }

            if (!IsFinitePositive(low) ||
                !IsFinitePositive(high) ||
                high <= low)
                return model;

            double ideal =
                low +
                (high - low) * 0.50;

            int retest =
                RetestQuality(
                    _m5Bars,
                    closedM5,
                    direction);

            if ((direction == 1 &&
                 PremiumDiscountBias(
                     _m5Bars,
                     closedM5) == 1) ||
                (direction == -1 &&
                 PremiumDiscountBias(
                     _m5Bars,
                     closedM5) == -1))
                quality += 5;

            if (_m15Frame != null &&
                _m15Frame.Direction == direction)
                quality += 5;

            if (_m30Frame != null &&
                _m30Frame.Direction == direction)
                quality += 3;

            if (retest >= MinimumRetestQuality)
                quality += 5;

            quality =
                ClampInt(
                    quality,
                    0,
                    100);

            double tolerance =
                atr *
                Math.Max(
                    0.02,
                    ExecutionZoneAtr);

            double triggerBuffer =
                atr *
                Math.Max(
                    0.01,
                    PrecisionBreakoutBufferAtr);

            model.ZoneLow =
                NormalizePrice(low);

            model.ZoneHigh =
                NormalizePrice(high);

            model.IdealEntry =
                NormalizePrice(ideal);

            model.Trigger =
                NormalizePrice(
                    direction == 1
                        ? high + triggerBuffer
                        : low - triggerBuffer);

            model.Invalidation =
                NormalizePrice(
                    direction == 1
                        ? low -
                          Math.Max(
                              StopBufferAtr,
                              0.05) *
                          atr
                        : high +
                          Math.Max(
                              StopBufferAtr,
                              0.05) *
                          atr);

            bool inside =
                market >= low - tolerance &&
                market <= high + tolerance;

            bool triggerReached =
                IsTriggerReached(
                    direction,
                    market,
                    model.Trigger);

            bool continuation =
                IsContinuationExecutionContext(direction);

            bool retestReady =
                inside &&
                !triggerReached;

            bool qualityReady =
                !RequirePrecisionEntry ||
                quality >=
                Math.Max(
                    40,
                    MinimumEntryQuality);

            if (triggerReached &&
                AllowPrecisionBreakoutEntry)
            {
                model.Mode =
                    CFIPClean70ExecutionMode.BreakoutMarket;

                model.ActualEntry =
                    market;

                model.Ready =
                    qualityReady &&
                    Math.Abs(
                        market -
                        model.Trigger) <=
                    atr *
                    Math.Max(
                        0.10,
                        MaximumEntryExtensionAtr);
            }
            else if (continuation)
            {
                model.Mode =
                    CFIPClean70ExecutionMode.WaitingForTrigger;

                model.ActualEntry = 0;
                model.Ready = false;
            }
            else if (retestReady)
            {
                model.Mode =
                    CFIPClean70ExecutionMode.RetestMarket;

                model.ActualEntry =
                    market;

                model.Ready =
                    qualityReady &&
                    Math.Abs(
                        market -
                        model.IdealEntry) <=
                    atr *
                    Math.Max(
                        0.05,
                        MaximumEntryDistanceAtr) &&
                    (retest >= MinimumRetestQuality ||
                     !RequireRetestQuality);
            }
            else
            {
                model.Mode =
                    CFIPClean70ExecutionMode.None;

                model.ActualEntry = 0;
                model.Ready = false;
            }

            model.Quality = quality;
            model.Source = source;

            if (model.Mode ==
                CFIPClean70ExecutionMode.WaitingForTrigger)
                model.Source += "+WAIT";
            else if (model.Mode ==
                     CFIPClean70ExecutionMode.RetestMarket)
                model.Source += "+RETEST";
            else if (model.Mode ==
                     CFIPClean70ExecutionMode.BreakoutMarket)
                model.Source += "+EXEC";

            return model;
        }

                private Plan BuildPlan(
            int closedM5,
            int direction)
        {
            if (_m5Bars == null ||
                closedM5 < 30 ||
                (direction != 1 && direction != -1))
                return null;

            double atr =
                Atr(
                    _m5Bars,
                    closedM5);

            if (atr <= 0)
                return null;

            ExecutionModel execution =
                BuildExecutionModel(
                    closedM5,
                    direction);

            if (execution == null ||
                !execution.Ready ||
                !IsFinitePositive(
                    execution.ActualEntry))
                return null;

            if (execution.Mode !=
                    CFIPClean70ExecutionMode.BreakoutMarket &&
                execution.Mode !=
                    CFIPClean70ExecutionMode.RetestMarket)
                return null;

            double entry =
                NormalizePrice(
                    execution.ActualEntry);

            if (!IsFinitePositive(entry))
                return null;

            if (RequirePrecisionEntry &&
                execution.Quality <
                Math.Max(
                    40,
                    MinimumEntryQuality))
                return null;

            Plan executionProbe =
                new Plan
                {
                    Direction = direction,
                    EntryMode = execution.Mode,
                    EntryTrigger = execution.Trigger,
                    EntryZoneLow = execution.ZoneLow,
                    EntryZoneHigh = execution.ZoneHigh
                };

            string executionReason;

            if (!IsExecutableMarketEntry(
                    executionProbe,
                    entry,
                    out executionReason))
                return null;



            string stopSource;
            int stopQuality;

            double stop =
                BuildStructuralStop(
                    closedM5,
                    direction,
                    entry,
                    atr,
                    out stopSource,
                    out stopQuality);

            if (!IsFinitePositive(stop))
            {
                if (RequireStructuralStop)
                    return null;

                stop =
                    direction == 1
                        ? entry -
                          atr *
                          FallbackSlAtr
                        : entry +
                          atr *
                          FallbackSlAtr;

                stopSource =
                    "ATR FALLBACK";
                stopQuality = 50;
            }

            if (AvoidLateEntry &&
                Math.Abs(
                    entry -
                    _m5Bars.ClosePrices[
                        closedM5]) >
                atr *
                MaximumEntryExtensionAtr)
                return null;

            double risk =
                Math.Abs(
                    entry -
                    stop);

            double spread =
                Math.Max(
                    0,
                    Symbol.Ask -
                    Symbol.Bid);

            double minimumRisk =
                Math.Max(
                    Math.Max(
                        0.05,
                        MinimumSlAtr) *
                    atr,
                    spread *
                    Math.Max(
                        1.0,
                        MaximumSpreadToStopRiskRatio));

            double maximumRisk =
                Math.Min(
                    Math.Max(
                        MinimumSlAtr,
                        MaximumSlAtr),
                    Math.Max(
                        MinimumSlAtr,
                        MaximumStructuralStopAtr)) *
                atr;

            if (risk < minimumRisk ||
                risk > maximumRisk)
                return null;

            List<Level> candidates =
                BuildTargetLevels(
                    closedM5,
                    direction,
                    entry,
                    atr);

            List<Level> selected =
                SelectTargets(
                    candidates,
                    closedM5,
                    entry,
                    risk,
                    direction,
                    atr);

            // FIX (CFIP-BUG-STAGE-ALIGNMENT): `selected` is now a fixed 4-slot
            // array (possibly containing nulls for stages with no qualifying
            // level), so count the *filled* slots rather than the list length.
            int filledTargetSlots =
                selected.Count(
                    x => x != null);

            if (filledTargetSlots <
                Math.Max(
                    1,
                    MinimumTargetsForPlan))
                return null;

            double tp1 =
                SelectTarget(
                    selected,
                    0,
                    entry,
                    risk,
                    direction,
                    Math.Max(
                        FallbackTp1RR,
                        MinimumRequiredRR()));

            double tp2 =
                SelectTarget(
                    selected,
                    1,
                    entry,
                    risk,
                    direction,
                    Math.Max(
                        FallbackTp2RR,
                        Tp2MinimumRR));

            double tp3 =
                SelectTarget(
                    selected,
                    2,
                    entry,
                    risk,
                    direction,
                    Math.Max(
                        FallbackTp3RR,
                        Tp3MinimumRR));

            double tp4 =
                SelectTarget(
                    selected,
                    3,
                    entry,
                    risk,
                    direction,
                    Math.Max(
                        FallbackTp4RR,
                        Tp4MinimumRR));

            if (!IsValidTarget(
                    direction,
                    entry,
                    tp1))
                return null;

            if (UseRRFilter &&
                Math.Abs(
                    tp1 -
                    entry) /
                Math.Max(
                    Symbol.PipSize,
                    risk) <
                Math.Max(
                    MinimumTradeRR,
                    MinimumRequiredRR()))
                return null;

            if (RequireHtfTargets &&
                !HasAnyHtfTargetLevel(
                    candidates))
                return null;

            if (RequireHtfRewardForTp2Plus)
            {
                if (tp2 > 0 &&
                    !IsHtfSourceForReward(
                        selected,
                        tp2))
                    return null;

                if (tp3 > 0 &&
                    !IsHtfSourceForReward(
                        selected,
                        tp3))
                    return null;

                if (tp4 > 0 &&
                    !IsHtfSourceForReward(
                        selected,
                        tp4))
                    return null;
            }

            if (RequireHtfRewardForTp1 &&
                !IsHtfSourceForReward(
                    selected,
                    tp1))
                return null;

            if (RejectTargetObstacle &&
                RequireObstacleFreeTp1 &&
                HasTargetObstacle(
                    _m5Bars,
                    closedM5,
                    direction,
                    entry,
                    tp1,
                    atr))
                return null;

            Plan p =
                new Plan
                {
                    Direction = direction,
                    EntryMode =
                        execution == null
                            ? CFIPClean70ExecutionMode.None
                            : execution.Mode,
                    Entry = NormalizePrice(entry),
                    IdealEntry =
                        execution == null
                            ? entry
                            : NormalizePrice(
                                execution.IdealEntry),
                    EntryZoneLow =
                        execution == null
                            ? 0
                            : NormalizePrice(
                                execution.ZoneLow),
                    EntryZoneHigh =
                        execution == null
                            ? 0
                            : NormalizePrice(
                                execution.ZoneHigh),
                    EntryTrigger =
                        execution == null
                            ? 0
                            : NormalizePrice(
                                execution.Trigger),
                    EntryInvalidation =
                        execution == null
                            ? 0
                            : NormalizePrice(
                                execution.Invalidation),
                    EntryQuality =
                        execution == null
                            ? 0
                            : execution.Quality,
                    EntrySource =
                        execution == null
                            ? ""
                            : execution.Source,
                    Stop = NormalizePrice(stop),
                    Tp1 = NormalizePrice(tp1),
                    Tp2 =
                        IsValidTarget(
                            direction,
                            entry,
                            tp2)
                            ? NormalizePrice(tp2)
                            : 0,
                    Tp3 =
                        IsValidTarget(
                            direction,
                            entry,
                            tp3)
                            ? NormalizePrice(tp3)
                            : 0,
                    Tp4 =
                        IsValidTarget(
                            direction,
                            entry,
                            tp4)
                            ? NormalizePrice(tp4)
                            : 0,
                    StopSource = stopSource,
                    StopQuality = stopQuality,
                    CreatedM5 = closedM5
                };

            p.Risk =
                Math.Abs(
                    p.Entry -
                    p.Stop);

            p.Tp1RR =
                p.Tp1 > 0
                    ? Math.Abs(
                        p.Tp1 -
                        p.Entry) /
                      p.Risk
                    : 0;

            p.Tp2RR =
                p.Tp2 > 0
                    ? Math.Abs(
                        p.Tp2 -
                        p.Entry) /
                      p.Risk
                    : 0;

            p.Tp3RR =
                p.Tp3 > 0
                    ? Math.Abs(
                        p.Tp3 -
                        p.Entry) /
                      p.Risk
                    : 0;

            p.Tp4RR =
                p.Tp4 > 0
                    ? Math.Abs(
                        p.Tp4 -
                        p.Entry) /
                      p.Risk
                    : 0;

            ApplyTargetMeta(
                candidates,
                p.Tp1,
                atr,
                out p.Tp1Source,
                out p.Tp1Quality);

            ApplyTargetMeta(
                candidates,
                p.Tp2,
                atr,
                out p.Tp2Source,
                out p.Tp2Quality);

            ApplyTargetMeta(
                candidates,
                p.Tp3,
                atr,
                out p.Tp3Source,
                out p.Tp3Quality);

            ApplyTargetMeta(
                candidates,
                p.Tp4,
                atr,
                out p.Tp4Source,
                out p.Tp4Quality);

            p.HtfTargetCount =
                CountHtfTargetsInPlan(
                    p);

            if (RequirePlanIntegrity &&
                !ValidatePlanIntegrity(
                    p,
                    direction,
                    entry,
                    atr,
                    true))
                return null;

            return p;
        }

        private bool IsHtfTimeframe(
            string timeframe)
        {
            if (string.IsNullOrWhiteSpace(
                    timeframe))
                return false;

            return
                timeframe == "M15" ||
                timeframe == "M30" ||
                timeframe == "H1" ||
                timeframe == "H4" ||
                timeframe == "D1" ||
                timeframe == "W1";
        }

        private bool IsHtfSource(
            string source)
        {
            if (string.IsNullOrWhiteSpace(
                    source))
                return false;

            return
                source.IndexOf(
                    "@M15",
                    StringComparison.OrdinalIgnoreCase) >= 0 ||
                source.IndexOf(
                    "@M30",
                    StringComparison.OrdinalIgnoreCase) >= 0 ||
                source.IndexOf(
                    "@H1",
                    StringComparison.OrdinalIgnoreCase) >= 0 ||
                source.IndexOf(
                    "@H4",
                    StringComparison.OrdinalIgnoreCase) >= 0 ||
                source.IndexOf(
                    "@D1",
                    StringComparison.OrdinalIgnoreCase) >= 0 ||
                source.IndexOf(
                    "@W1",
                    StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private bool IsHtfSourceForReward(
            List<Level> selected,
            double target)
        {
            if (selected == null ||
                !IsFinitePositive(target))
                return false;

            for (int i = 0;
                 i < selected.Count;
                 i++)
            {
                if (selected[i] == null)
                    continue;

                if (Math.Abs(
                        selected[i].Price -
                        target) <=
                    Math.Max(
                        Symbol.PipSize * 2,
                        target * 1e-8) &&
                    IsHtfTimeframe(
                        selected[i].Timeframe))
                    return true;
            }

            return false;
        }

        private int CountHtfTargetsInPlan(
            Plan plan)
        {
            if (plan == null)
                return 0;

            int count = 0;

            if (IsHtfSource(plan.Tp1Source))
                count++;

            if (IsHtfSource(plan.Tp2Source))
                count++;

            if (IsHtfSource(plan.Tp3Source))
                count++;

            if (IsHtfSource(plan.Tp4Source))
                count++;

            return count;
        }

                private bool ValidatePlanIntegrity(
            Plan plan,
            int direction,
            double referenceEntry,
            double atr,
            bool checkSpread)
        {
            if (plan == null ||
                (direction != 1 &&
                 direction != -1) ||
                !IsFinitePositive(referenceEntry) ||
                atr <= 0)
                return false;

            if (!IsValidStop(
                    direction,
                    plan.Entry,
                    plan.Stop))
                return false;

            if (!IsValidTarget(
                    direction,
                    plan.Entry,
                    plan.Tp1))
                return false;

            if (plan.Risk <= 0)
                return false;

            if (RequirePrecisionEntry &&
                plan.EntryQuality <
                Math.Max(
                    40,
                    MinimumEntryQuality))
                return false;

            double minimumRR =
                Math.Max(
                    Tp1MinimumRR,
                    MinimumRequiredRR());

            double maximumRR =
                Math.Max(
                    minimumRR,
                    MaximumRewardRR);

            double tp1RR =
                Math.Abs(
                    plan.Tp1 -
                    plan.Entry) /
                plan.Risk;

            if (tp1RR < minimumRR ||
                tp1RR > maximumRR)
                return false;

            if (plan.Tp2 > 0)
            {
                double rr =
                    Math.Abs(
                        plan.Tp2 -
                        plan.Entry) /
                    plan.Risk;

                if (rr <
                        Math.Max(
                            Tp2MinimumRR,
                            tp1RR +
                            Math.Max(
                                0.10,
                                StructuralTpRrStep)) ||
                    rr > maximumRR)
                    return false;

                if (RequireHtfRewardForTp2Plus &&
                    !IsHtfSource(
                        plan.Tp2Source))
                    return false;
            }
            else if (MinimumTargetsForPlan >= 2)
            {
                return false;
            }

            if (plan.Tp3 > 0)
            {
                double rr =
                    Math.Abs(
                        plan.Tp3 -
                        plan.Entry) /
                    plan.Risk;

                double previousRR =
                    plan.Tp2 > 0
                        ? plan.Tp2RR
                        : tp1RR;

                if (rr <
                        Math.Max(
                            Tp3MinimumRR,
                            previousRR +
                            Math.Max(
                                0.10,
                                StructuralTpRrStep)) ||
                    rr > maximumRR)
                    return false;

                if (RequireHtfRewardForTp2Plus &&
                    !IsHtfSource(
                        plan.Tp3Source))
                    return false;
            }

            if (plan.Tp4 > 0)
            {
                double rr =
                    Math.Abs(
                        plan.Tp4 -
                        plan.Entry) /
                    plan.Risk;

                double previousRR =
                    plan.Tp3 > 0
                        ? plan.Tp3RR
                        : plan.Tp2 > 0
                            ? plan.Tp2RR
                            : tp1RR;

                if (rr <
                        Math.Max(
                            Tp4MinimumRR,
                            previousRR +
                            Math.Max(
                                0.10,
                                StructuralTpRrStep)) ||
                    rr > maximumRR)
                    return false;

                if (RequireHtfRewardForTp2Plus &&
                    !IsHtfSource(
                        plan.Tp4Source))
                    return false;
            }

            if (RequireHtfRewardForTp1 &&
                !IsHtfSource(
                    plan.Tp1Source))
                return false;

            if (RequireHtfRewardForTp2Plus &&
                plan.HtfTargetCount <= 0)
                return false;

            if (checkSpread &&
                UseSpreadFilter)
            {
                double spread =
                    Math.Max(
                        0,
                        Symbol.Ask -
                        Symbol.Bid);

                if (spread > 0 &&
                    plan.Risk > 0 &&
                    spread / plan.Risk >
                    Math.Max(
                        0.02,
                        MaximumSpreadToStopRiskRatio))
                    return false;
            }

            if (Math.Abs(
                    plan.Entry -
                    referenceEntry) >
                atr *
                Math.Max(
                    0.10,
                    MaximumEntryExtensionAtr))
                return false;

            if (MinimumSmartTargetQualityForTp1 > 0 &&
                plan.Tp1Quality > 0 &&
                plan.Tp1Quality <
                MinimumSmartTargetQualityForTp1)
                return false;

            if (plan.Tp2 > 0 &&
                !IsProgressiveTarget(
                    direction,
                    plan.Tp1,
                    plan.Tp2))
                return false;

            if (plan.Tp3 > 0 &&
                !IsProgressiveTarget(
                    direction,
                    plan.Tp2 > 0
                        ? plan.Tp2
                        : plan.Tp1,
                    plan.Tp3))
                return false;

            if (plan.Tp4 > 0 &&
                !IsProgressiveTarget(
                    direction,
                    plan.Tp3 > 0
                        ? plan.Tp3
                        : plan.Tp2 > 0
                            ? plan.Tp2
                            : plan.Tp1,
                    plan.Tp4))
                return false;

            return true;
        }

        private bool IsProgressiveTarget(
            int direction,
            double previous,
            double next)
        {
            if (!IsFinitePositive(previous) ||
                !IsFinitePositive(next))
                return false;

            return direction == 1
                ? next > previous
                : direction == -1
                    ? next < previous
                    : false;
        }

        private double MinimumRequiredRR()
        {
            if (!AdaptiveStructuralRR ||
                _decision == null)
                return Tp1MinimumRR;

            double step =
                Math.Max(
                    0.05,
                    StructuralTpRrStep);

            if (_decision.Regime == "EXPANSION")
                return Math.Max(
                    2.10,
                    Tp1MinimumRR + step);

            if (_decision.Regime == "RANGE")
                return Math.Max(
                    1.75,
                    Tp1MinimumRR -
                    step * 0.50);

            return Tp1MinimumRR;
        }

                private double BuildStructuralStop(
            int closedM5,
            int direction,
            double entry,
            double atr,
            out string source,
            out int quality)
        {
            source = "NONE";
            quality = 0;

            if (_m5Bars == null ||
                closedM5 < 20 ||
                atr <= 0 ||
                !IsFinitePositive(entry))
                return 0;

            List<Level> candidates =
                new List<Level>();

            if (UseM5StructureForStop)
            {
                double swing =
                    direction == 1
                        ? FindSwingLowBelow(
                            _m5Bars,
                            closedM5,
                            entry)
                        : FindSwingHighAbove(
                            _m5Bars,
                            closedM5,
                            entry);

                AddLevel(
                    candidates,
                    swing,
                    "M5_SWING_STOP",
                    "M5",
                    0,
                    SwingStructureWeight);
            }

            Zone supportFvg =
                FindNearestFvg(
                    _m5Bars,
                    closedM5,
                    direction,
                    atr);

            if (supportFvg != null)
            {
                AddLevel(
                    candidates,
                    direction == 1
                        ? supportFvg.Low
                        : supportFvg.High,
                    "FVG_STOP",
                    "M5",
                    supportFvg.Age,
                    FvgWeight +
                    SmartStopZoneBonus);
            }

            Zone supportOb =
                FindNearestOrderBlock(
                    _m5Bars,
                    closedM5,
                    direction,
                    atr);

            if (supportOb != null)
            {
                AddLevel(
                    candidates,
                    direction == 1
                        ? supportOb.Low
                        : supportOb.High,
                    "ORDER_BLOCK_STOP",
                    "M5",
                    supportOb.Age,
                    OrderBlockWeight +
                    SmartStopZoneBonus);
            }

            if (UseHtfStructureForStop)
            {
                Bars[] frames =
                {
                    _m15Bars,
                    _m30Bars,
                    _h1Bars,
                    _h4Bars,
                    _d1Bars,
                    _w1Bars
                };

                string[] names =
                {
                    "M15",
                    "M30",
                    "H1",
                    "H4",
                    "D1",
                    "W1"
                };

                int[] weights =
                {
                    M15Weight,
                    M30Weight,
                    H1Weight,
                    H4Weight,
                    D1Weight,
                    W1Weight
                };

                DateTime reference =
                    _m5Bars.OpenTimes[
                        closedM5];

                for (int i = 0;
                     i < frames.Length;
                     i++)
                {
                    if (frames[i] == null)
                        continue;

                    int idx =
                        ClosedIndex(
                            frames[i],
                            reference);

                    if (idx < 10)
                        continue;

                    double frameAtr =
                        Atr(
                            frames[i],
                            idx);

                    if (frameAtr <= 0)
                        frameAtr = atr;

                    double swing =
                        direction == 1
                            ? FindSwingLowBelow(
                                frames[i],
                                idx,
                                entry)
                            : FindSwingHighAbove(
                                frames[i],
                                idx,
                                entry);

                    AddLevel(
                        candidates,
                        swing,
                        "HTF_STRUCTURE_STOP",
                        names[i],
                        0,
                        weights[i] +
                        HtfRewardBonus / 2);

                    Zone fvg =
                        FindNearestFvg(
                            frames[i],
                            idx,
                            direction,
                            frameAtr);

                    if (fvg != null)
                    {
                        AddLevel(
                            candidates,
                            direction == 1
                                ? fvg.Low
                                : fvg.High,
                            "HTF_FVG_STOP",
                            names[i],
                            fvg.Age,
                            FvgWeight +
                            HtfRewardBonus / 2);
                    }

                    Zone ob =
                        FindNearestOrderBlock(
                            frames[i],
                            idx,
                            direction,
                            frameAtr);

                    if (ob != null)
                    {
                        AddLevel(
                            candidates,
                            direction == 1
                                ? ob.Low
                                : ob.High,
                            "HTF_ORDER_BLOCK_STOP",
                            names[i],
                            ob.Age,
                            OrderBlockWeight +
                            HtfRewardBonus / 2);
                    }
                }
            }

            if (candidates.Count == 0)
                return 0;

            double minRiskAtr =
                Math.Max(
                    0.05,
                    MinimumSlAtr);

            double maxRiskAtr =
                Math.Min(
                    Math.Max(
                        minRiskAtr,
                        MaximumSlAtr),
                    Math.Max(
                        minRiskAtr,
                        MaximumStructuralStopAtr));

            double spread =
                Math.Max(
                    0,
                    Symbol.Ask -
                    Symbol.Bid);

            minRiskAtr =
                Math.Max(
                    minRiskAtr,
                    (spread /
                     Math.Max(
                         Symbol.PipSize,
                         atr)) *
                    Math.Max(
                        1.0,
                        MaximumSpreadToStopRiskRatio));

            Level best = null;
            double bestScore =
                double.MinValue;
            int bestQuality = 0;
            string bestSource = "NONE";

            int minimumQuality =
                Math.Max(
                    Math.Max(
                        40,
                        MinimumStructuralStopQuality),
                    SmartStopQuality);

            for (int i = 0;
                 i < candidates.Count;
                 i++)
            {
                Level candidate =
                    candidates[i];

                double frameAtr =
                    atr;

                if (candidate.Timeframe != "M5")
                {
                    Bars frame =
                        candidate.Timeframe == "M15"
                            ? _m15Bars
                            : candidate.Timeframe == "M30"
                                ? _m30Bars
                                : candidate.Timeframe == "H1"
                                    ? _h1Bars
                                    : candidate.Timeframe == "H4"
                                        ? _h4Bars
                                        : candidate.Timeframe == "D1"
                                            ? _d1Bars
                                            : _w1Bars;

                    int idx =
                        ClosedIndex(
                            frame,
                            _m5Bars.OpenTimes[
                                closedM5]);

                    if (idx >= 10)
                    {
                        double localAtr =
                            Atr(
                                frame,
                                idx);

                        if (localAtr > 0)
                            frameAtr =
                                localAtr;
                    }
                }

                double buffer =
                    frameAtr *
                    Math.Max(
                        0.02,
                        candidate.Timeframe == "M5"
                            ? StopBufferAtr
                            : Math.Max(
                                StopBufferAtr,
                                HtfStopBufferAtr));

                double stop =
                    direction == 1
                        ? candidate.Price - buffer
                        : candidate.Price + buffer;

                stop =
                    NormalizePrice(
                        stop);

                if (!IsValidStop(
                        direction,
                        entry,
                        stop))
                    continue;

                double risk =
                    Math.Abs(
                        entry -
                        stop);

                double riskAtr =
                    risk /
                    Math.Max(
                        Symbol.PipSize,
                        atr);

                if (riskAtr < minRiskAtr ||
                    riskAtr > maxRiskAtr)
                    continue;

                double score =
                    candidate.Score;

                if (HasOpposingZonePathObstacle(
                        _m5Bars,
                        closedM5,
                        direction,
                        entry,
                        stop,
                        atr))
                    score -=
                        Math.Min(
                            18,
                            StopRiskBalanceWeight);

                if (IsHtfTimeframe(
                        candidate.Timeframe))
                    score +=
                        HtfRewardBonus *
                        0.50;

                if (candidate.Kind.IndexOf(
                        "FVG",
                        StringComparison.OrdinalIgnoreCase) >= 0 ||
                    candidate.Kind.IndexOf(
                        "ORDER_BLOCK",
                        StringComparison.OrdinalIgnoreCase) >= 0)
                    score +=
                        SmartStopZoneBonus;

                if (candidate.Kind.IndexOf(
                        "LIQUIDITY",
                        StringComparison.OrdinalIgnoreCase) >= 0)
                    score +=
                        SmartLiquidityPoolBonus;

                double preferredRisk =
                    Math.Max(
                        0.25,
                        PreferredStopRiskAtr);

                double riskBalance =
                    Math.Max(
                        0,
                        20.0 -
                        Math.Abs(
                            riskAtr -
                            preferredRisk) *
                        Math.Max(
                            6.0,
                            12.0 *
                            Math.Max(
                                0.25,
                                StopRiskBalanceWeight /
                                18.0)));

                score +=
                    riskBalance *
                    Math.Max(
                        0,
                        StopRiskBalanceWeight) /
                    20.0;

                if (score > bestScore)
                {
                    bestScore =
                        score;
                    best =
                        candidate;
                    bestQuality =
                        ClampInt(
                            (int)Math.Round(
                                score),
                            0,
                            100);
                    bestSource =
                        candidate.Kind +
                        "@" +
                        candidate.Timeframe;
                }
            }

            if (best == null ||
                bestQuality <
                minimumQuality)
                return 0;

            double finalFrameAtr =
                atr;

            if (best.Timeframe != "M5")
            {
                Bars frame =
                    best.Timeframe == "M15"
                        ? _m15Bars
                        : best.Timeframe == "M30"
                            ? _m30Bars
                            : best.Timeframe == "H1"
                                ? _h1Bars
                                : best.Timeframe == "H4"
                                    ? _h4Bars
                                    : best.Timeframe == "D1"
                                        ? _d1Bars
                                        : _w1Bars;

                int idx =
                    ClosedIndex(
                        frame,
                        _m5Bars.OpenTimes[
                            closedM5]);

                if (idx >= 10)
                {
                    double localAtr =
                        Atr(
                            frame,
                            idx);

                    if (localAtr > 0)
                        finalFrameAtr =
                            localAtr;
                }
            }

            double finalBuffer =
                finalFrameAtr *
                Math.Max(
                    0.02,
                    best.Timeframe == "M5"
                        ? StopBufferAtr
                        : Math.Max(
                            StopBufferAtr,
                            HtfStopBufferAtr));

            double selectedStop =
                direction == 1
                    ? best.Price - finalBuffer
                    : best.Price + finalBuffer;

            source = bestSource;
            quality = bestQuality;

            return NormalizePrice(
                selectedStop);
        }

                private List<Level> BuildTargetLevels(
            int closedM5,
            int direction,
            double entry,
            double atr)
        {
            List<Level> levels =
                new List<Level>();

            if (_m5Bars == null ||
                atr <= 0 ||
                !IsFinitePositive(entry))
                return levels;

            double swing =
                direction == 1
                    ? FindSwingHighAbove(
                        _m5Bars,
                        closedM5,
                        entry)
                    : FindSwingLowBelow(
                        _m5Bars,
                        closedM5,
                        entry);

            AddLevel(
                levels,
                swing,
                "SWING_TARGET",
                "M5",
                0,
                SwingStructureWeight);

            int opposingDirection =
                -direction;

            Zone fvg =
                FindNearestFvg(
                    _m5Bars,
                    closedM5,
                    opposingDirection,
                    atr);

            if (fvg != null)
            {
                AddLevel(
                    levels,
                    direction == 1
                        ? fvg.Low
                        : fvg.High,
                    "OPPOSING_FVG",
                    "M5",
                    fvg.Age,
                    FvgWeight +
                    ZoneRewardBonus);
            }

            Zone ob =
                FindNearestOrderBlock(
                    _m5Bars,
                    closedM5,
                    opposingDirection,
                    atr);

            if (ob != null)
            {
                AddLevel(
                    levels,
                    direction == 1
                        ? ob.Low
                        : ob.High,
                    "OPPOSING_ORDER_BLOCK",
                    "M5",
                    ob.Age,
                    OrderBlockWeight +
                    ZoneRewardBonus);
            }

            AddSupplyDemandAndLiquidityLevels(
                levels,
                closedM5,
                direction,
                entry,
                atr);

            if (UseDailyPivots)
            {
                AddDailyPivotLevels(
                    levels,
                    direction,
                    entry,
                    atr,
                    _m5Bars.OpenTimes[closedM5]);
            }

            if (UseMultiTfLevelMap)
            {
                AddHtfTargets(
                    levels,
                    direction,
                    entry,
                    atr,
                    _m5Bars.OpenTimes[
                        closedM5]);

                AddPreviousPeriodLevels(
                    levels,
                    direction,
                    entry,
                    atr,
                    _m5Bars.OpenTimes[
                        closedM5]);
            }

            AddSmartExtraTargetLevels(
                levels,
                closedM5,
                direction,
                entry,
                atr);

            return
                MergeLevels(
                    levels
                        .Where(
                            x =>
                                IsFinitePositive(
                                    x.Price) &&
                                x.Price != entry)
                        .OrderByDescending(
                            x =>
                                x.Score)
                        .Take(
                            Math.Max(
                                1,
                                SmartTargetMaxCandidates))
                        .ToList(),
                    atr);
        }

        private void AddSupplyDemandAndLiquidityLevels(
            List<Level> levels,
            int closedM5,
            int direction,
            double entry,
            double atr)
        {
            if (_m5Bars == null)
                return;

            double swing =
                direction == 1
                    ? FindSwingHighAbove(
                        _m5Bars,
                        closedM5,
                        entry)
                    : FindSwingLowBelow(
                        _m5Bars,
                        closedM5,
                        entry);

            AddLevel(
                levels,
                swing,
                direction == 1
                    ? "SUPPLY_ZONE"
                    : "DEMAND_ZONE",
                "M5",
                0,
                SupplyDemandWeight);

            if (UseEqualHighLow)
            {
                double liquidity =
                    direction == 1
                        ? FindEqualHigh(
                            _m5Bars,
                            closedM5,
                            entry,
                            atr)
                        : FindEqualLow(
                            _m5Bars,
                            closedM5,
                            entry,
                            atr);

                AddLevel(
                    levels,
                    liquidity,
                    "LIQUIDITY_POOL",
                    "M5",
                    0,
                    EqualHighLowWeight);
            }

            if (UseDailyWeeklyLiquidity)
            {
                int d1 =
                    ClosedIndex(
                        _d1Bars,
                        _m5Bars.OpenTimes[
                            closedM5]);

                if (d1 > 0)
                {
                    AddLevel(
                        levels,
                        direction == 1
                            ? _d1Bars.HighPrices[d1 - 1]
                            : _d1Bars.LowPrices[d1 - 1],
                        "LIQUIDITY_POOL",
                        "D1",
                        1,
                        LiquidityPoolWeight);
                }
            }

            if (UseSessionLiquidityTargets)
            {
                double sessionHigh;
                double sessionLow;

                GetSessionRange(
                    _m5Bars,
                    closedM5,
                    SessionStartUtc,
                    SessionEndUtc,
                    out sessionHigh,
                    out sessionLow);

                AddLevel(
                    levels,
                    direction == 1
                        ? sessionHigh
                        : sessionLow,
                    "SESSION",
                    "M5",
                    0,
                    Math.Max(
                        SessionWeight,
                        LiquidityTargetMinimumScore));
            }
        }

        private void GetSessionRange(
            Bars bars,
            int index,
            int startHour,
            int endHour,
            out double high,
            out double low)
        {
            high = 0;
            low = 0;

            if (bars == null || index < 5)
                return;

            DateTime anchor =
                bars.OpenTimes[index];

            DateTime dayStart =
                new DateTime(
                    anchor.Year,
                    anchor.Month,
                    anchor.Day,
                    0,
                    0,
                    0);

            bool overnight =
                startHour > endHour;

            DateTime from;
            DateTime to;

            if (!overnight)
            {
                from =
                    dayStart.AddHours(startHour);

                to =
                    dayStart.AddHours(endHour);
            }
            else if (anchor.Hour < endHour)
            {
                from =
                    dayStart.AddDays(-1)
                        .AddHours(startHour);

                to =
                    dayStart.AddHours(endHour);
            }
            else
            {
                from =
                    dayStart.AddHours(startHour);

                to =
                    dayStart.AddDays(1)
                        .AddHours(endHour);
            }

            int first = -1;
            int last = -1;

            for (int i = index;
                 i >= Math.Max(0, index - 400);
                 i--)
            {
                DateTime t = bars.OpenTimes[i];

                if (t < from)
                    break;

                if (t < to)
                {
                    first = i;
                    if (last < 0)
                        last = i;
                }
            }

            if (first < 0 || last < first)
                return;

            high =
                Highest(
                    bars,
                    first,
                    last);

            low =
                Lowest(
                    bars,
                    first,
                    last);
        }


        private double FindNextLiquidityAbove(
            Bars bars,
            int index,
            double price)
        {
            if (bars == null ||
                index < 10)
                return 0;

            int first =
                Math.Max(
                    2,
                    index -
                    LiquidityLookback);

            double best = 0;

            for (int i = first;
                 i <= index - 2;
                 i++)
            {
                double high =
                    bars.HighPrices[i];

                if (high <= price)
                    continue;

                if (best <= 0 ||
                    high < best)
                    best = high;
            }

            return best;
        }

        private double FindNextLiquidityBelow(
            Bars bars,
            int index,
            double price)
        {
            if (bars == null ||
                index < 10)
                return 0;

            int first =
                Math.Max(
                    2,
                    index -
                    LiquidityLookback);

            double best = 0;

            for (int i = first;
                 i <= index - 2;
                 i++)
            {
                double low =
                    bars.LowPrices[i];

                if (low >= price)
                    continue;

                if (best <= 0 ||
                    low > best)
                    best = low;
            }

            return best;
        }

        private void AddSmartExtraTargetLevels(
            List<Level> levels,
            int closedM5,
            int direction,
            double entry,
            double atr)
        {
            if (!UseExtendedLiquidityMap ||
                _m5Bars == null)
                return;

            double forecast =
                direction == 1
                    ? FindNextLiquidityAbove(
                        _m5Bars,
                        closedM5,
                        entry)
                    : FindNextLiquidityBelow(
                        _m5Bars,
                        closedM5,
                        entry);

            AddLevel(
                levels,
                forecast,
                "LIQUIDITY_FORECAST",
                "M5",
                0,
                Math.Max(
                    LiquidityPoolWeight,
                    LiquidityTargetMinimumScore));

            if (UseSessionLiquidityTargets)
            {
                double high;
                double low;

                GetSessionRange(
                    _m5Bars,
                    closedM5,
                    SessionStartUtc,
                    SessionEndUtc,
                    out high,
                    out low);

                AddLevel(
                    levels,
                    direction == 1
                        ? high
                        : low,
                    "SESSION_FORECAST",
                    "M5",
                    0,
                    Math.Max(
                        SessionWeight,
                        LiquidityTargetMinimumScore));
            }
        }

        private void AddDailyPivotLevels(
            List<Level> levels,
            int direction,
            double entry,
            double atr,
            DateTime reference)
        {
            if (!UseDailyPivots ||
                _d1Bars == null ||
                _d1Bars.Count < 3 ||
                atr <= 0)
                return;

            int index =
                ClosedIndex(
                    _d1Bars,
                    reference);

            if (index <= 0)
                return;

            int previous = index - 1;

            double high = _d1Bars.HighPrices[previous];
            double low = _d1Bars.LowPrices[previous];
            double close = _d1Bars.ClosePrices[previous];

            if (high <= low ||
                !IsFinitePositive(close))
                return;

            double pivot = (high + low + close) / 3.0;
            double range = high - low;

            double r1 = 2.0 * pivot - low;
            double s1 = 2.0 * pivot - high;
            double r2 = pivot + range;
            double s2 = pivot - range;
            double r3 = high + 2.0 * (pivot - low);
            double s3 = low - 2.0 * (high - pivot);

            double weight =
                Math.Max(
                    1,
                    DailyPivotWeight);

            AddLevel(levels, pivot, "PIVOT", "D1", 1, weight);

            if (direction == 1)
            {
                AddLevel(levels, r1, "PIVOT_R1", "D1", 1, weight + 4);
                AddLevel(levels, r2, "PIVOT_R2", "D1", 1, weight + 8);
                AddLevel(levels, r3, "PIVOT_R3", "D1", 1, weight + 10);
            }
            else if (direction == -1)
            {
                AddLevel(levels, s1, "PIVOT_S1", "D1", 1, weight + 4);
                AddLevel(levels, s2, "PIVOT_S2", "D1", 1, weight + 8);
                AddLevel(levels, s3, "PIVOT_S3", "D1", 1, weight + 10);
            }
        }

        private void AddHtfTargets(
            List<Level> levels,
            int direction,
            double entry,
            double atr,
            DateTime reference)
        {
            Bars[] frames =
            {
                _m15Bars,
                _m30Bars,
                _h1Bars,
                _h4Bars,
                _d1Bars,
                _w1Bars
            };

            string[] names =
            {
                "M15",
                "M30",
                "H1",
                "H4",
                "D1",
                "W1"
            };

            int[] weights =
            {
                M15Weight,
                M30Weight,
                H1Weight,
                H4Weight,
                D1Weight,
                W1Weight
            };

            for (int i = 0;
                 i < frames.Length;
                 i++)
            {
                if (frames[i] == null)
                    continue;

                int idx =
                    ClosedIndex(
                        frames[i],
                        reference);

                if (idx < 10)
                    continue;

                double clusterWeight =
                    i < 2
                        ? MtfClusterWeight
                        : HtfStructureWeight;

                double frameAtr =
                    Atr(
                        frames[i],
                        idx);

                if (frameAtr <= 0)
                    frameAtr = atr;

                double swing =
                    direction == 1
                        ? FindSwingHighAbove(
                            frames[i],
                            idx,
                            entry)
                        : FindSwingLowBelow(
                            frames[i],
                            idx,
                            entry);

                AddLevel(
                    levels,
                    swing,
                    "HTF_SWING",
                    names[i],
                    0,
                    weights[i] +
                    clusterWeight / 10.0 +
                    HtfRewardBonus);

                int opposing =
                    -direction;

                Zone fvg =
                    FindNearestFvg(
                        frames[i],
                        idx,
                        opposing,
                        frameAtr);

                if (fvg != null)
                {
                    AddLevel(
                        levels,
                        direction == 1
                            ? fvg.Low
                            : fvg.High,
                        "HTF_FVG",
                        names[i],
                        fvg.Age,
                        FvgWeight +
                        clusterWeight / 10.0 +
                        HtfRewardBonus);
                }

                Zone ob =
                    FindNearestOrderBlock(
                        frames[i],
                        idx,
                        opposing,
                        frameAtr);

                if (ob != null)
                {
                    AddLevel(
                        levels,
                        direction == 1
                            ? ob.Low
                            : ob.High,
                        "HTF_ORDER_BLOCK",
                        names[i],
                        ob.Age,
                        OrderBlockWeight +
                        clusterWeight / 10.0 +
                        HtfRewardBonus);
                }

                double liquidity =
                    direction == 1
                        ? FindEqualHigh(
                            frames[i],
                            idx,
                            entry,
                            frameAtr)
                        : FindEqualLow(
                            frames[i],
                            idx,
                            entry,
                            frameAtr);

                if (UseHigherTfLiquidityTargets)
                {
                    AddLevel(
                        levels,
                        liquidity,
                        "HTF_LIQUIDITY",
                        names[i],
                        0,
                        LiquidityPoolWeight +
                        clusterWeight / 10.0 +
                        HtfRewardBonus);
                }
            }
        }

        private void AddPreviousPeriodLevels(
            List<Level> levels,
            int direction,
            double entry,
            double atr,
            DateTime reference)
        {
            int d1 =
                ClosedIndex(
                    _d1Bars,
                    reference);

            int w1 =
                ClosedIndex(
                    _w1Bars,
                    reference);

            if (UseDailyWeeklyLiquidity &&
                d1 > 0)
            {
                AddLevel(
                    levels,
                    direction == 1
                        ? _d1Bars.HighPrices[d1 - 1]
                        : _d1Bars.LowPrices[d1 - 1],
                    "PREVIOUS_DAY",
                    "D1",
                    1,
                    PreviousDayWeight);
            }

            if (UseDailyWeeklyLiquidity &&
                w1 > 0)
            {
                AddLevel(
                    levels,
                    direction == 1
                        ? _w1Bars.HighPrices[w1 - 1]
                        : _w1Bars.LowPrices[w1 - 1],
                    "PREVIOUS_WEEK",
                    "W1",
                    1,
                    PreviousWeekWeight);
            }
        }

        private void AddLevel(
            List<Level> levels,
            double price,
            string kind,
            string timeframe,
            int age,
            double baseScore)
        {
            if (!IsFinitePositive(price))
                return;

            double score =
                Math.Max(
                    0,
                    baseScore) * 0.60;

            if (age <= 5)
                score += 15;

            if (timeframe == "H1" ||
                timeframe == "H4" ||
                timeframe == "D1" ||
                timeframe == "W1")
                score += 8;

            if (kind.IndexOf(
                    "LIQUIDITY",
                    StringComparison.OrdinalIgnoreCase) >= 0)
                score += 5;

            if (kind.IndexOf(
                    "FVG",
                    StringComparison.OrdinalIgnoreCase) >= 0 ||
                kind.IndexOf(
                    "ORDER_BLOCK",
                    StringComparison.OrdinalIgnoreCase) >= 0)
                score += 4;

            Level level =
                new Level
                {
                    Price =
                        NormalizePrice(price),
                    Score =
                        Clamp(
                            score,
                            0,
                            100),
                    Kind = kind,
                    Timeframe = timeframe,
                    Age =
                        Math.Max(
                            0,
                            age),
                    Hits = 1
                };

            levels.Add(
                level);
        }



        private List<Level> MergeLevels(
            List<Level> input,
            double atr)
        {
            List<Level> result =
                new List<Level>();

            if (input == null ||
                input.Count == 0)
                return result;

            double tolerance =
                Math.Max(
                    Symbol.PipSize * 2,
                    atr *
                    Math.Max(
                        0.02,
                        SmartLevelClusterAtr));

            for (int i = 0;
                 i < input.Count;
                 i++)
            {
                Level current =
                    input[i];

                Level match =
                    result.FirstOrDefault(
                        x =>
                            Math.Abs(
                                x.Price -
                                current.Price) <=
                            tolerance);

                if (match == null)
                {
                    result.Add(current);
                    continue;
                }

                int matchHits =
                    Math.Max(
                        1,
                        match.Hits);

                int currentHits =
                    Math.Max(
                        1,
                        current.Hits);

                match.Price =
                    NormalizePrice(
                        (match.Price *
                         matchHits +
                         current.Price *
                         currentHits) /
                        (matchHits +
                         currentHits));

                match.Score =
                    Clamp(
                        Math.Max(
                            match.Score,
                            current.Score) +
                        Math.Min(
                            20,
                            Math.Min(
                                match.Score,
                                current.Score) *
                            0.25),
                        0,
                        100);

                match.Hits =
                    matchHits +
                    currentHits;

                match.Age =
                    Math.Min(
                        match.Age,
                        current.Age);

                if (current.Timeframe == "H1" ||
                    current.Timeframe == "H4" ||
                    current.Timeframe == "D1" ||
                    current.Timeframe == "W1")
                    match.Timeframe =
                        current.Timeframe;

                if (match.Kind == "SWING" &&
                    current.Kind != "SWING")
                    match.Kind =
                        current.Kind;
            }

            return
                result
                .OrderByDescending(
                    x => x.Score)
                .ToList();
        }

                private List<Level> SelectTargets(
            List<Level> levels,
            int closedM5,
            double entry,
            double risk,
            int direction,
            double atr)
        {
            // FIX (CFIP-BUG-STAGE-ALIGNMENT): this list always has exactly 4 slots
            // (index 0=TP1 .. 3=TP4), and a slot stays null when no candidate meets
            // that stage's own RR requirement. Previously the list only contained
            // the *found* levels appended in order, so if stage 0 (TP1) found
            // nothing but stage 1 (TP2) did, that TP2-grade level silently became
            // "selected[0]" and got labelled/priced as TP1 by every caller
            // (BuildPlan, SelectStructuralAutoTarget, live target refresh). That
            // meant the reported TP could be at the wrong RR tier without any
            // indication. Keeping fixed, possibly-null slots makes index == stage
            // always true, so downstream RR/labels are trustworthy.
            List<Level> selected =
                new List<Level>
                {
                    null, null, null, null
                };

            if (levels == null ||
                levels.Count == 0 ||
                risk <= 0 ||
                atr <= 0)
                return selected;

            double rrStep =
                Math.Max(
                    0.10,
                    StructuralTpRrStep);

            double adaptiveTp1RR =
                Math.Max(
                    Tp1MinimumRR,
                    MinimumRequiredRR());

            double maximumRR =
                Math.Max(
                    adaptiveTp1RR,
                    MaximumRewardRR);

            double[] requiredRR =
            {
                adaptiveTp1RR,
                Math.Max(
                    Tp2MinimumRR,
                    adaptiveTp1RR + rrStep),
                Math.Max(
                    Tp3MinimumRR,
                    Tp2MinimumRR + rrStep),
                Math.Max(
                    Tp4MinimumRR,
                    Tp3MinimumRR + rrStep)
            };

            for (int stage = 0;
                 stage < 4;
                 stage++)
            {
                if (requiredRR[stage] >
                    maximumRR)
                    continue;

                bool requireHtf =
                    stage == 0
                        ? RequireHtfRewardForTp1
                        : RequireHtfRewardForTp2Plus;

                Level best = null;
                double bestScore =
                    double.MinValue;

                // Nearest already-assigned earlier stage (skipping any stage
                // that stayed empty), falling back to entry. This keeps target
                // spacing meaningful even when a lower stage had no candidate.
                double previous = entry;

                for (int p = stage - 1; p >= 0; p--)
                {
                    if (selected[p] != null)
                    {
                        previous = selected[p].Price;
                        break;
                    }
                }

                for (int i = 0;
                     i < levels.Count;
                     i++)
                {
                    Level candidate =
                        levels[i];

                    if (!IsValidTarget(
                            direction,
                            entry,
                            candidate.Price))
                        continue;

                    if (candidate.Age >
                        MaximumSetupAgeBars)
                        continue;

                    bool htf =
                        IsHtfTimeframe(
                            candidate.Timeframe);

                    if (requireHtf &&
                        (!htf ||
                         candidate.Score <
                         MinimumHtfRewardQuality))
                        continue;

                    double distance =
                        Math.Abs(
                            candidate.Price -
                            entry);

                    double rr =
                        distance /
                        Math.Max(
                            Symbol.PipSize,
                            risk);

                    double minimumCandidateRR =
                        requiredRR[stage];

                    if (htf)
                        minimumCandidateRR =
                            Math.Max(
                                minimumCandidateRR,
                                MinimumHtfTargetRR);

                    if (rr < minimumCandidateRR ||
                        rr > maximumRR)
                        continue;

                    if (distance >
                        atr *
                        Math.Max(
                            1.0,
                            MaximumTargetExtensionAtr))
                        continue;

                    double spacing =
                        atr *
                        Math.Max(
                            0.05,
                            MinimumTpSpacingAtr);

                    if (selected.Any(
                        x =>
                            x != null &&
                            Math.Abs(
                                x.Price -
                                candidate.Price) <=
                            spacing * 0.50))
                        continue;

                    if (stage > 0)
                    {
                        if (direction == 1 &&
                            candidate.Price <=
                            previous +
                            spacing)
                            continue;

                        if (direction == -1 &&
                            candidate.Price >=
                            previous -
                            spacing)
                            continue;
                    }

                    if (RejectTargetObstacle &&
                        HasTargetObstacle(
                            _m5Bars,
                            closedM5,
                            direction,
                            entry,
                            candidate.Price,
                            atr))
                        continue;

                    if (RejectTargetObstacle &&
                        HasOpposingZonePathObstacle(
                            _m5Bars,
                            closedM5,
                            direction,
                            entry,
                            candidate.Price,
                            atr))
                        continue;

                    if (stage >= 1 &&
                        RejectTargetObstacle &&
                        HasHigherTfZonePathObstacle(
                            _m5Bars.OpenTimes[
                                closedM5],
                            direction,
                            entry,
                            candidate.Price))
                        continue;

                    double normalizedDistance =
                        distance /
                        Math.Max(
                            Symbol.PipSize,
                            atr);

                    double efficiency =
                        Math.Max(
                            0,
                            25 -
                            Math.Abs(
                                rr -
                                requiredRR[stage]) *
                            4);

                    double score =
                        candidate.Score +
                        efficiency;

                    if (htf)
                        score +=
                            Math.Max(
                                0,
                                HtfRewardBonus);

                    if (candidate.Kind.IndexOf(
                            "LIQUIDITY",
                            StringComparison.OrdinalIgnoreCase) >= 0)
                        score +=
                            Math.Max(
                                0,
                                LiquidityRewardBonus);

                    if (candidate.Kind.IndexOf(
                            "FVG",
                            StringComparison.OrdinalIgnoreCase) >= 0 ||
                        candidate.Kind.IndexOf(
                            "ORDER_BLOCK",
                            StringComparison.OrdinalIgnoreCase) >= 0)
                        score +=
                            Math.Max(
                                0,
                                ZoneRewardBonus);

                    score +=
                        Math.Min(
                            20,
                            Math.Max(
                                1,
                                candidate.Hits) *
                            2);

                    score +=
                        SmartTargetNearestBias /
                        (1.0 +
                         Math.Max(
                             0,
                             normalizedDistance)) *
                        10.0;

                    score +=
                        stage *
                        (htf
                            ? HtfRewardBonus * 0.35
                            : 2.0);

                    if (score > bestScore)
                    {
                        bestScore =
                            score;
                        best =
                            candidate;
                    }
                }

                if (best != null)
                    selected[stage] = best;
            }

            return selected;
        }

                private double SelectTarget(
            List<Level> selected,
            int position,
            double entry,
            double risk,
            int direction,
            double alternateRR)
        {
            if (selected != null &&
                position < selected.Count &&
                selected[position] != null)
                return selected[position].Price;

            bool requireHtf =
                position == 0
                    ? RequireHtfRewardForTp1
                    : RequireHtfRewardForTp2Plus;

            double minimumRR =
                Math.Max(
                    0.50,
                    alternateRR);

            double maximumRR =
                Math.Max(
                    minimumRR,
                    MaximumRewardRR);

            if (!AllowSyntheticTargetFallback ||
                requireHtf ||
                minimumRR > maximumRR)
                return 0;

            return NormalizePrice(
                direction == 1
                    ? entry +
                      risk *
                      minimumRR
                    : entry -
                      risk *
                      minimumRR);
        }

        private void ApplyTargetMeta(
            List<Level> candidates,
            double target,
            double atr,
            out string source,
            out int quality)
        {
            source =
                target > 0
                    ? "RR"
                    : "";

            quality =
                target > 0
                    ? 55
                    : 0;

            if (target <= 0)
                return;

            Level best = null;
            double bestDistance = double.MaxValue;

            for (int i = 0; i < candidates.Count; i++)
            {
                double distance =
                    Math.Abs(
                        candidates[i].Price -
                        target);

                if (distance <= atr * 0.15 &&
                    distance < bestDistance)
                {
                    best =
                        candidates[i];

                    bestDistance =
                        distance;
                }
            }

            if (best != null)
            {
                source =
                    best.Kind +
                    "@" +
                    best.Timeframe;

                quality =
                    ClampInt(
                        (int)Math.Round(
                            best.Score),
                        0,
                        100);

                if (best.Age <= 5)
                    quality =
                        Math.Min(
                            100,
                            quality + 5);
            }
        }

        // ============================================================
        #endregion

        #region Active Plan Management
        // ============================================================

        private void ReconcilePreTradePlanDirection(int closedM5)
        {
            if (_plan == null ||
                _plan.IsLivePosition ||
                _decision == null ||
                _decision.Direction == 0 ||
                _decision.Direction == _plan.Direction)
                return;

            RemovePlanObjects();
            _plan = null;
            _executionModel = null;
            _lastVisualDirection = _decision.Direction;

            SetAutoTradingState(
                "ARMED",
                "PRE-TRADE PLAN SUPERSEDED");
        }

        private void ActivatePlan(
            Plan plan)
        {
            _plan = plan;
            _lastSignalM5 = plan.CreatedM5;
            _lastConfirmedM5 = plan.CreatedM5;
            _lastConfirmedDirection = plan.Direction;
            _peakPrice = plan.Entry;

            _lastMarket =
                plan.Direction == 1
                    ? Symbol.Bid
                    : Symbol.Ask;

            _tp1Hit = 0;
            _tp2Hit = 0;
            _tp3Hit = 0;
            _tp4Hit = 0;
            _slHit = false;
            _outcomeRegistered = false;
            _outcomeTelemetryTimedOut = false;
            _brokerProtectionRecoveryRequired = false;
            _lastStructuralStopUpdateM5 =
                plan.CreatedM5;
            _lastTargetRepriceM5 =
                -1;

            _executionModel = null;

            SetLifecycleState(
                CFIPClean70LifecycleState.PlanReady,
                "PLAN READY");

            ClearWatchObjects();

            string message =
                "CFIP CLEAN70 " +
                (plan.Direction == 1
                    ? "BUY"
                    : "SELL") +
                " | CONF " +
                (_decision == null
                    ? 0
                    : _decision.Confidence) +
                " | SMART " +
                (_decision == null
                    ? 0
                    : _decision.SmartQuality) +
                " | MODE " +
                ExecutionModeText(plan.EntryMode) +
                " | ENTRY " +
                Price(plan.Entry) +
                " | TRIGGER " +
                Price(plan.EntryTrigger) +
                " | IDEAL " +
                Price(plan.IdealEntry) +
                " | ENTRY Q " +
                plan.EntryQuality +
                " | HTF TP " +
                plan.HtfTargetCount +
                " | SL " +
                Price(plan.Stop) +
                " | TP1 " +
                Price(plan.Tp1) +
                " | RR " +
                plan.Tp1RR.ToString("F2");

            if (AlertOnConfirmedSignal)
            {
                SendUnifiedAlert(
                    "SIGNAL|" +
                    plan.CreatedM5,
                    message,
                    plan.Direction,
                    true);
            }
        }

        private void EvaluateActivePlan(
            int closedM5)
        {
            if (_plan == null ||
                !_plan.IsLivePosition)
                return;

            double market =
                _plan.Direction == 1
                    ? Symbol.Bid
                    : Symbol.Ask;

            _lastMarket = market;

            if (!IsFinitePositive(market) ||
                _plan.Risk <= 0)
                return;

            if (_plan.Direction == 1)
                _peakPrice =
                    Math.Max(
                        _peakPrice,
                        market);
            else
                _peakPrice =
                    Math.Min(
                        _peakPrice,
                        market);

            double favorable =
                _plan.Direction == 1
                    ? _peakPrice - _plan.Entry
                    : _plan.Entry - _peakPrice;

            double currentMove =
                _plan.Direction == 1
                    ? market - _plan.Entry
                    : _plan.Entry - market;

            double previousStop =
                _plan.Stop;

            double previousTp1 =
                _plan.Tp1;

            double previousTp2 =
                _plan.Tp2;

            double peakRR =
                favorable /
                Math.Max(
                    Symbol.PipSize,
                    _plan.Risk);

            if (EnableLiveExitManagement)
            {
                bool structuralBarChanged =
                    closedM5 !=
                    _lastStructuralStopUpdateM5;

                double protectedStop =
                    CalculateProtectedStop(
                        market,
                        peakRR,
                        closedM5,
                        structuralBarChanged);

                if (BetterStop(
                        _plan.Direction,
                        protectedStop,
                        _plan.Stop))
                {
                    _plan.Stop =
                        NormalizePrice(
                            protectedStop);

                    RecalculatePlanRR();
                }

                if (UpdateUnhitTargets &&
                    peakRR >=
                    TargetUpdateTriggerRR &&
                    (!StructuralTargetUpdatesOnly ||
                     closedM5 !=
                     _lastTargetRepriceM5))
                {
                    UpdateUnhitTargetsLive(
                        closedM5,
                        market);
                }

                if (structuralBarChanged)
                    _lastStructuralStopUpdateM5 =
                        closedM5;
            }

            double updateAtr =
                Atr(
                    _m5Bars,
                    closedM5);

            bool changed =
                Math.Abs(
                    previousStop -
                    _plan.Stop) >=
                    Math.Max(
                        Symbol.PipSize,
                        updateAtr *
                        Math.Max(
                            0.01,
                            SlRepriceStepAtr)) ||
                Math.Abs(
                    previousTp1 -
                    _plan.Tp1) >=
                    Symbol.PipSize ||
                Math.Abs(
                    previousTp2 -
                    _plan.Tp2) >=
                    Symbol.PipSize;

            if (changed &&
                AlertOnExitPlanUpdate)
            {
                SendUnifiedAlert(
                    "PLANUPDATE|" +
                    closedM5 +
                    "|" +
                    Price(_plan.Stop) +
                    "|" +
                    Price(_plan.Tp1),
                    "CFIP CLEAN70 SMART PLAN UPDATE | SL " +
                    Price(_plan.Stop) +
                    " | TP1 " +
                    Price(_plan.Tp1) +
                    " | EXIT " +
                    GetSmartExitMode(),
                    _plan.Direction,
                    false);
            }

            if (RequirePlanIntegrity &&
                !ValidatePlanIntegrity(
                    _plan,
                    _plan.Direction,
                    _plan.Entry,
                    Math.Max(
                        Symbol.PipSize,
                        Atr(
                            _m5Bars,
                            closedM5)),
                    false))
            {
                SendUnifiedAlert(
                    "INVALIDPLAN|" +
                    _plan.CreatedM5,
                    "CFIP CLEAN70 PLAN INVALIDATED | STRUCTURE / RR / SPREAD GUARD",
                    _plan.Direction,
                    true);

                _lastExitM5 =
                    closedM5;

                Position integrityPosition =
                    GetManagedPositionById(
                        _plan.PositionId);

                if (integrityPosition != null)
                {
                    SetLifecycleState(
                        CFIPClean70LifecycleState.ExitRequested,
                        "PLAN INTEGRITY FAILURE");

                    if (!TryClosePosition(
                            integrityPosition,
                            "PLAN INTEGRITY FAILURE"))
                    {
                        SetLifecycleState(
                            CFIPClean70LifecycleState.RecoveryRequired,
                            "PLAN INTEGRITY EXIT REJECTED");

                        _autoExecutionBlockReason =
                            "PLAN INTEGRITY EXIT REJECTED";
                    }

                    return;
                }

                SetLifecycleState(
                    CFIPClean70LifecycleState.Closed,
                    "PLAN INVALID • NO BROKER POSITION");

                _plan = null;
                RemovePlanObjects();
                return;
            }

            double liveStop =
                GetActiveBrokerStopPrice();

            bool hitSl =
                _plan.Direction == 1
                    ? market <= liveStop
                    : market >= liveStop;

            bool hitTp1 =
                _plan.Tp1 > 0 &&
                (_plan.Direction == 1
                    ? market >= _plan.Tp1
                    : market <= _plan.Tp1);

            bool hitTp2 =
                _plan.Tp2 > 0 &&
                (_plan.Direction == 1
                    ? market >= _plan.Tp2
                    : market <= _plan.Tp2);

            bool hitTp3 =
                _plan.Tp3 > 0 &&
                (_plan.Direction == 1
                    ? market >= _plan.Tp3
                    : market <= _plan.Tp3);

            bool hitTp4 =
                _plan.Tp4 > 0 &&
                (_plan.Direction == 1
                    ? market >= _plan.Tp4
                    : market <= _plan.Tp4);

            if (hitSl &&
                !_slHit)
            {
                if (GetManagedLivePositionForPlan() == null)
                {
                    SetLifecycleState(
                        CFIPClean70LifecycleState.Closed,
                        "SL LEVEL • POSITION ALREADY CLOSED");
                    return;
                }

                if (RequestLivePlanExit(
                        closedM5,
                        "SL LEVEL HIT"))
                {
                    _slHit = true;

                    if (EnableLevelHitAlerts &&
                        AlertOnLevelHit &&
                        AlertOnSl)
                    {
                        SendUnifiedAlert(
                            "SL|" +
                            _plan.CreatedM5,
                            "CFIP CLEAN70 SL HIT • EXIT REQUESTED | " +
                            Price(_plan.Stop),
                            -1,
                            true);
                    }

                    DrawOutcomeMarker(
                        "SL HIT",
                        _plan.Stop,
                        false);
                }

                return;
            }

            if (hitTp1 &&
                _tp1Hit == 0)
            {
                bool tp1Processed =
                    ExecutePartialClose(
                        PartialCloseTp1Percent,
                        "TP1");

                if (tp1Processed)
                    _tp1Hit = 1;

                if (tp1Processed &&
                    EnableLevelHitAlerts &&
                    AlertOnLevelHit &&
                    AlertOnTp1)
                {
                    SendUnifiedAlert(
                        "TP1|" +
                        _plan.CreatedM5,
                        "CFIP CLEAN70 TP1 HIT | " +
                        Price(_plan.Tp1),
                        _plan.Direction,
                        true);
                }
            }

            if (hitTp2 &&
                _tp2Hit == 0)
            {
                bool tp2Processed =
                    ExecutePartialClose(
                        PartialCloseTp2Percent,
                        "TP2");

                if (tp2Processed)
                    _tp2Hit = 1;

                if (tp2Processed &&
                    EnableLevelHitAlerts &&
                    AlertOnLevelHit &&
                    AlertOnTp2)
                {
                    SendUnifiedAlert(
                        "TP2|" +
                        _plan.CreatedM5,
                        "CFIP CLEAN70 TP2 HIT | " +
                        Price(_plan.Tp2),
                        _plan.Direction,
                        false);
                }
            }

            if (hitTp3 &&
                _tp3Hit == 0)
            {
                _tp3Hit = 1;

                if (EnableLevelHitAlerts &&
                    AlertOnLevelHit &&
                    AlertOnTp3)
                {
                    SendUnifiedAlert(
                        "TP3|" +
                        _plan.CreatedM5,
                        "CFIP CLEAN70 TP3 HIT | " +
                        Price(_plan.Tp3),
                        _plan.Direction,
                        false);
                }
            }

            if (hitTp4 &&
                _tp4Hit == 0)
            {
                if (RequestLivePlanExit(
                        closedM5,
                        "TP4 LEVEL HIT"))
                {
                    _tp4Hit = 1;

                    if (EnableLevelHitAlerts &&
                        AlertOnLevelHit &&
                        AlertOnTp4)
                    {
                        SendUnifiedAlert(
                            "TP4|" +
                            _plan.CreatedM5,
                            "CFIP CLEAN70 TP4 HIT • EXIT REQUESTED | " +
                            Price(_plan.Tp4),
                            _plan.Direction,
                            true);
                    }

                    DrawOutcomeMarker(
                        "TP4 HIT",
                        _plan.Tp4,
                        true);
                }

                return;
            }

            if (CheckLiveReversalAgainstPlan(
                    closedM5))
                return;

            if (CheckProfitExhaustionExit(
                    closedM5,
                    market,
                    peakRR))
                return;

            if (CheckStructuralSetupInvalidation(
                    closedM5,
                    market))
                return;

            int barsSincePlan =
                Math.Max(
                    0,
                    closedM5 -
                    _plan.CreatedM5);

            if (UseFalseSignalGuard &&
                EnableSetupInvalidation &&
                currentMove < 0 &&
                barsSincePlan <=
                Math.Max(
                    1,
                    FalseSignalWatchBars) &&
                Math.Abs(
                    currentMove) >=
                _plan.Risk *
                Math.Max(
                    0.25,
                    FalseSignalAdverseR))
            {
                if (AlertOnFalseSignalRisk &&
                    _lastInvalidationAlertM5 !=
                    closedM5)
                {
                    _lastInvalidationAlertM5 =
                        closedM5;

                    SendUnifiedAlert(
                        "INVALID-RISK|" +
                        closedM5,
                        "CFIP CLEAN70 FALSE SIGNAL RISK | " +
                        (_plan.Direction == 1
                            ? "BUY"
                            : "SELL"),
                        0,
                        true);
                }

                if (InvalidateOnFalseSignal &&
                    Math.Abs(
                        currentMove) >=
                    _plan.Risk *
                    Math.Max(
                        1.0,
                        FalseSignalAdverseR))
                {
                    RequestLivePlanExit(
                        closedM5,
                        "FALSE SIGNAL INVALIDATION");

                    return;
                }
            }

            if (currentMove < 0 &&
                Math.Abs(
                    currentMove) >=
                _plan.Risk *
                Math.Max(
                    0.25,
                    FalseSignalAdverseR) &&
                AlertOnInvalidated &&
                _lastInvalidationAlertM5 !=
                closedM5)
            {
                SendUnifiedAlert(
                    "INVALID|" +
                    closedM5,
                    "CFIP CLEAN70 SETUP UNDER PRESSURE | " +
                    (_plan.Direction == 1
                        ? "BUY"
                        : "SELL") +
                    " | " +
                    (Math.Abs(
                         currentMove) /
                     _plan.Risk).ToString("F2") +
                    "R",
                    0,
                    true);

                _lastInvalidationAlertM5 =
                    closedM5;
            }
        }

                private bool CheckProfitExhaustionExit(
            int closedM5,
            double market,
            double peakRR)
        {
            if (!EnableProfitExhaustionProtection ||
                _plan == null ||
                !_plan.IsLivePosition ||
                peakRR <
                Math.Max(
                    0.8,
                    ExhaustionMinimumPeakRR) ||
                _plan.Risk <= 0)
                return false;

            double favorable =
                Math.Max(
                    Symbol.PipSize,
                    _plan.Risk) *
                peakRR;

            if (favorable <= 0)
                return false;

            double retracement =
                _plan.Direction == 1
                    ? _peakPrice - market
                    : market - _peakPrice;

            if (retracement <= 0)
                return false;

            double retracementPercent =
                retracement /
                favorable *
                100.0;

            if (retracementPercent <
                Math.Max(
                    15,
                    ExhaustionRetracementPercent))
                return false;

            double currentRR =
                _plan.Direction == 1
                    ? (market - _plan.Entry) /
                      Math.Max(
                          Symbol.PipSize,
                          _plan.Risk)
                    : (_plan.Entry - market) /
                      Math.Max(
                          Symbol.PipSize,
                          _plan.Risk);

            int pressure =
                CalculateSmartExitPressure(
                    market,
                    currentRR);

            if (pressure <
                Math.Max(
                    55,
                    ExhaustionPressureThreshold))
                return false;

            int opposite =
                _plan.Direction * -1;

            bool oppositeStructure =
                _m5Frame != null &&
                (opposite == 1
                    ? (_m5Frame.StructureBull ||
                       _m5Frame.MssBull ||
                       _m5Frame.ChochBull)
                    : (_m5Frame.StructureBear ||
                       _m5Frame.MssBear ||
                       _m5Frame.ChochBear));

            bool oppositeReaction =
                _reaction != null &&
                _reaction.Direction == opposite &&
                _reaction.Confidence >=
                Math.Max(
                    LiveReactionThreshold,
                    LiveReversalMinimumConfidence) &&
                _reaction.IndependentEvidence >=
                Math.Max(
                    1,
                    ExhaustionMinimumOppositeEvidence);

            if (!oppositeStructure &&
                !oppositeReaction)
                return false;

            Position position =
                GetManagedLivePositionForPlan();

            if (position == null ||
                position.NetProfit <= 0)
                return false;

            double protectedProfit =
                position.NetProfit;

            if (!RequestLivePlanExit(
                    closedM5,
                    "PROFIT EXHAUSTION"))
                return false;

            SendUnifiedAlert(
                "EXHAUSTION-CLOSE|" +
                position.Id,
                "CFIP CLEAN70 EXHAUSTION EXIT REQUESTED | #" +
                position.Id +
                " | +" +
                protectedProfit.ToString("F2") +
                " | PEAK RR " +
                peakRR.ToString("F2") +
                " | RETRACE " +
                retracementPercent.ToString("F0") +
                "% | PRESSURE " +
                pressure,
                _plan.Direction,
                true);

            SetAutoTradingState(
                "EXECUTED",
                "EXHAUSTION EXIT REQUESTED #" +
                position.Id);

            return true;
        }


                private bool CheckStructuralSetupInvalidation(
            int closedM5,
            double market)
        {
            if (!EnableSetupInvalidation ||
                _plan == null ||
                !_plan.IsLivePosition ||
                _m5Bars == null ||
                closedM5 < 30 ||
                !IsFinitePositive(market))
                return false;

            double atr =
                Atr(
                    _m5Bars,
                    closedM5);

            if (!IsFinitePositive(atr))
                return false;

            int swingLookback =
                Math.Max(
                    10,
                    Math.Min(
                        StructureLookback,
                        closedM5 - 1));

            int swingStart =
                Math.Max(
                    1,
                    closedM5 -
                    swingLookback);

            double swingHigh =
                _m5Bars.HighPrices[swingStart];

            double swingLow =
                _m5Bars.LowPrices[swingStart];

            for (int i = swingStart + 1;
                 i < closedM5;
                 i++)
            {
                swingHigh =
                    Math.Max(
                        swingHigh,
                        _m5Bars.HighPrices[i]);

                swingLow =
                    Math.Min(
                        swingLow,
                        _m5Bars.LowPrices[i]);
            }

            double structureBuffer =
                atr *
                Math.Max(
                    0.02,
                    InvalidationStructureAtr);

            bool structureFailure =
                _plan.Direction == 1
                    ? swingLow > 0 &&
                      market <
                      swingLow -
                      structureBuffer
                    : swingHigh > 0 &&
                      market >
                      swingHigh +
                      structureBuffer;

            double risk =
                Math.Max(
                    Symbol.PipSize,
                    _plan.Risk);

            double adverseR =
                _plan.Direction == 1
                    ? (_plan.Entry - market) /
                      risk
                    : (market - _plan.Entry) /
                      risk;

            double maxAdverseR =
                Math.Max(
                    0.30,
                    InvalidationMaxAdverseR);

            if (adverseR >= maxAdverseR)
                structureFailure = true;

            bool mtfFlip = false;

            if (_m5Frame != null)
            {
                mtfFlip =
                    _plan.Direction == 1
                        ? _m5Frame.Direction == -1 &&
                          (_m5Frame.MssBear ||
                           _m5Frame.ChochBear)
                        : _m5Frame.Direction == 1 &&
                          (_m5Frame.MssBull ||
                           _m5Frame.ChochBull);
            }

            double zoneTolerance =
                atr *
                Math.Max(
                    0.02,
                    InvalidationZoneCloseAtr);

            bool zoneFailure =
                _plan.Direction == 1
                    ? market <
                      _plan.Stop -
                      zoneTolerance &&
                      (_m5Frame == null ||
                       _m5Frame.StructureBear ||
                       _m5Frame.MssBear ||
                       _m5Frame.ChochBear)
                    : market >
                      _plan.Stop +
                      zoneTolerance &&
                      (_m5Frame == null ||
                       _m5Frame.StructureBull ||
                       _m5Frame.MssBull ||
                       _m5Frame.ChochBull);

            bool invalid =
                (structureFailure ||
                 zoneFailure) &&
                (!RequireMtfFlipForInvalidation ||
                 mtfFlip ||
                 adverseR >= maxAdverseR);

            if (!invalid)
                return false;

            int score =
                (structureFailure ? 40 : 0) +
                (zoneFailure ? 30 : 0) +
                (mtfFlip ? 30 : 0);

            if (AlertOnInvalidated &&
                _lastInvalidationAlertM5 !=
                closedM5)
            {
                SendUnifiedAlert(
                    "STRUCT-INVALID|" +
                    closedM5,
                    "CFIP CLEAN70 STRUCTURAL INVALIDATION | " +
                    (_plan.Direction == 1
                        ? "BUY"
                        : "SELL") +
                    " | SCORE " +
                    ClampInt(
                        score,
                        0,
                        100),
                    0,
                    true);

                _lastInvalidationAlertM5 =
                    closedM5;
            }

            Position position =
                GetManagedPositionById(
                    _plan.PositionId);

            if (position == null)
            {
                SetLifecycleState(
                    CFIPClean70LifecycleState.Closed,
                    "STRUCTURE INVALIDATED • POSITION ALREADY CLOSED");
                return false;
            }

            SetLifecycleState(
                CFIPClean70LifecycleState.ExitRequested,
                "STRUCTURAL INVALIDATION");

            if (!TryClosePosition(
                    position,
                    "STRUCTURAL INVALIDATION"))
            {
                SetLifecycleState(
                    CFIPClean70LifecycleState.RecoveryRequired,
                    "STRUCTURAL EXIT REJECTED");

                _autoExecutionBlockReason =
                    "STRUCTURAL EXIT REJECTED";

                SendUnifiedAlert(
                    "STRUCT-INVALID-EXIT-FAILED|" +
                    position.Id,
                    "CFIP CLEAN70 STRUCTURAL INVALIDATION • BROKER EXIT REJECTED | #" +
                    position.Id,
                    _plan.Direction,
                    true);
            }

            _lastExitM5 = closedM5;

            // _plan remains authoritative until OnPositionClosed confirms
            // that the broker position is actually gone.
            return true;
        }


        private int CalculateSmartExitPressure(
            double market,
            double currentRR)
        {
            if (_plan == null)
                return 0;

            int pressure = 0;
            int opposite = _plan.Direction * -1;

            if (_reaction != null &&
                _reaction.Direction == opposite)
            {
                pressure +=
                    Math.Min(
                        30,
                        Math.Max(
                            0,
                            _reaction.Confidence / 3));

                if (_reaction.IndependentEvidence >=
                    MinimumLiveReactionEvidence)
                    pressure += 10;
            }

            if (_m5Frame != null)
            {
                bool structure =
                    opposite == 1
                        ? _m5Frame.StructureBull
                        : _m5Frame.StructureBear;

                bool reversal =
                    opposite == 1
                        ? (_m5Frame.MssBull ||
                           _m5Frame.ChochBull)
                        : (_m5Frame.MssBear ||
                           _m5Frame.ChochBear);

                bool force =
                    opposite == 1
                        ? (_m5Frame.DisplacementBull &&
                           _m5Frame.LiquidityBull)
                        : (_m5Frame.DisplacementBear &&
                           _m5Frame.LiquidityBear);

                if (structure)
                    pressure += 15;

                if (reversal)
                    pressure += 15;

                if (force)
                    pressure += 20;
            }

            double atr =
                Atr(
                    _m5Bars,
                    Math.Max(
                        1,
                        _m5Bars.Count - 2));

            if (atr > 0)
            {
                Zone zone =
                    FindNearestOpposingZone(
                        _m5Bars,
                        Math.Max(
                            1,
                            _m5Bars.Count - 2),
                        _plan.Direction,
                        atr);

                if (zone != null &&
                    DistanceToZone(
                        market,
                        zone) <=
                    atr *
                    Math.Max(
                        0.05,
                        ZoneProximityAtr))
                    pressure += 10;
            }

            if (currentRR < 0)
                pressure += 10;

            return ClampInt(
                pressure,
                0,
                100);
        }

        private string GetSmartExitMode()
        {
            if (_plan == null)
                return "NO ACTIVE PLAN";

            double risk =
                Math.Max(
                    Symbol.PipSize,
                    _plan.Risk);

            double currentRR =
                _plan.Direction == 1
                    ? (_lastMarket - _plan.Entry) / risk
                    : (_plan.Entry - _lastMarket) / risk;

            int pressure =
                CalculateSmartExitPressure(
                    _lastMarket,
                    currentRR);

            if (pressure >=
                SmartExitPressureThreshold)
                return "PROTECT";

            if (pressure >=
                LiveReactionWatchThreshold)
                return "WATCH";

            return "HOLD";
        }

                private double CalculateProtectedStop(
            double market,
            double peakRR,
            int closedM5,
            bool structuralUpdate)
        {
            if (_plan == null)
                return 0;

            double candidate =
                _plan.Stop;

            if (MoveSlToBreakEven &&
                peakRR >=
                BreakEvenTriggerRR)
            {
                double be =
                    _plan.Direction == 1
                        ? _plan.Entry +
                          BreakEvenBufferPips *
                          Symbol.PipSize
                        : _plan.Entry -
                          BreakEvenBufferPips *
                          Symbol.PipSize;

                candidate =
                    _plan.Direction == 1
                        ? Math.Max(
                            candidate,
                            be)
                        : Math.Min(
                            candidate,
                            be);
            }

            if (UseSpreadAwareBreakEven &&
                MoveSlToBreakEven &&
                peakRR >= Math.Max(0.50, BreakEvenTriggerRR))
            {
                double spreadPips =
                    Math.Max(
                        0,
                        (Symbol.Ask - Symbol.Bid) /
                        Math.Max(Symbol.PipSize, 1e-9));

                double lockPips =
                    Math.Max(
                        RiskFreeLockPips,
                        spreadPips + BreakEvenBufferPips);

                double be =
                    _plan.Direction == 1
                        ? _plan.Entry + lockPips * Symbol.PipSize
                        : _plan.Entry - lockPips * Symbol.PipSize;

                if (IsValidStop(
                        _plan.Direction,
                        _plan.Entry,
                        be))
                {
                    candidate =
                        _plan.Direction == 1
                            ? Math.Max(candidate, be)
                            : Math.Min(candidate, be);
                }
            }

            if ((EnableStructuralSlRepricing ||
                 EnableDynamicSlTrail) &&
                (structuralUpdate ||
                 !StructuralStopManagementOnly) &&
                peakRR >=
                Math.Max(
                    SlRepriceStartRR,
                    SmartTrailMinimumRR) &&
                UseSwingStructureInTrail)
            {
                double atr =
                    Atr(
                        _m5Bars,
                        closedM5);

                if (atr > 0)
                {
                    double structural =
                        _plan.Direction == 1
                            ? FindSwingLowBelow(
                                _m5Bars,
                                closedM5,
                                market)
                            : FindSwingHighAbove(
                                _m5Bars,
                                closedM5,
                                market);

                    if (IsFinitePositive(structural))
                    {
                        double room =
                            atr *
                            Math.Max(
                                0.10,
                                TrailDistanceAtr);

                        if (_plan.Direction == 1 &&
                            structural <=
                            market - room)
                            candidate =
                                Math.Max(
                                    candidate,
                                    structural);
                        else if (_plan.Direction == -1 &&
                                 structural >=
                                 market + room)
                            candidate =
                                Math.Min(
                                    candidate,
                                    structural);
                    }
                }
            }

            int pressure =
                CalculateSmartExitPressure(
                    market,
                    peakRR);

            if (peakRR >= Math.Max(1.0, SmartTrailTightenAtRR))
            {
                double trailAtr =
                    Atr(_m5Bars, closedM5);

                bool momentumAligned =
                    _m5Frame != null &&
                    (_plan.Direction == 1
                        ? _m5Frame.MomentumBull && _m5Frame.StructureBull
                        : _m5Frame.MomentumBear && _m5Frame.StructureBear);

                if (trailAtr > 0 &&
                    momentumAligned)
                {
                    double room =
                        trailAtr *
                        Math.Max(
                            0.08,
                            TrailDistanceAtr -
                            SmartTrailMomentumBonusAtr);

                    double tightened =
                        _plan.Direction == 1
                            ? market - room
                            : market + room;

                    if (IsValidStop(
                            _plan.Direction,
                            _plan.Entry,
                            tightened))
                    {
                        candidate =
                            _plan.Direction == 1
                                ? Math.Max(candidate, tightened)
                                : Math.Min(candidate, tightened);
                    }
                }
            }

            if (!StructuralStopManagementOnly &&
                pressure >=
                SmartExitPressureThreshold &&
                peakRR >=
                SmartTrailMinimumRR)
            {
                double atr =
                    Atr(
                        _m5Bars,
                        closedM5);

                if (atr > 0)
                {
                    double tightRoom =
                        atr *
                        Math.Min(
                            0.45,
                            Math.Max(
                                0.10,
                                SlRepriceBreathingAtr));

                    double tightened =
                        _plan.Direction == 1
                            ? market - tightRoom
                            : market + tightRoom;

                    if (IsValidStop(
                            _plan.Direction,
                            _plan.Entry,
                            tightened))
                    {
                        candidate =
                            _plan.Direction == 1
                                ? Math.Max(
                                    candidate,
                                    tightened)
                                : Math.Min(
                                    candidate,
                                    tightened);
                    }
                }
            }

            double minimumDistance =
                Math.Max(
                    Symbol.PipSize * 2,
                    Symbol.Ask -
                    Symbol.Bid);

            candidate =
                _plan.Direction == 1
                    ? Math.Min(
                        candidate,
                        market -
                        minimumDistance)
                    : Math.Max(
                        candidate,
                        market +
                        minimumDistance);

            candidate =
                NormalizePrice(
                    candidate);

            if (!IsValidStop(
                    _plan.Direction,
                    _plan.Entry,
                    candidate))
                return _plan.Stop;

            double atrValue =
                Atr(
                    _m5Bars,
                    closedM5);

            double step =
                atrValue *
                Math.Max(
                    0.01,
                    TrailStepAtr);

            if (!BetterStop(
                    _plan.Direction,
                    candidate,
                    _plan.Stop) ||
                Math.Abs(
                    candidate -
                    _plan.Stop) <
                Math.Max(
                    Symbol.PipSize,
                    step))
                return _plan.Stop;

            return candidate;
        }

        private void UpdateUnhitTargetsLive(
            int closedM5,
            double market)
        {
            if (_plan == null)
                return;

            if (StructuralTargetUpdatesOnly &&
                _lastTargetRepriceM5 ==
                closedM5)
                return;

            double atr =
                Atr(
                    _m5Bars,
                    closedM5);

            if (atr <= 0)
                return;

            List<Level> levels =
                BuildTargetLevels(
                    closedM5,
                    _plan.Direction,
                    _plan.Entry,
                    atr);

            double step =
                atr *
                Math.Max(
                    0.05,
                    TargetUpdateStepAtr);

            double spacing =
                atr *
                Math.Max(
                    0.05,
                    MinimumTpSpacingAtr);

            bool changed = false;

            for (int stage = 0;
                 stage < 4;
                 stage++)
            {
                int hit =
                    stage == 0
                        ? _tp1Hit
                        : stage == 1
                            ? _tp2Hit
                            : stage == 2
                                ? _tp3Hit
                                : _tp4Hit;

                if (hit != 0)
                    continue;

                double current =
                    stage == 0
                        ? _plan.Tp1
                        : stage == 1
                            ? _plan.Tp2
                            : stage == 2
                                ? _plan.Tp3
                                : _plan.Tp4;

                if (!IsFinitePositive(current))
                    continue;

                bool requireHtf =
                    stage == 0
                        ? RequireHtfRewardForTp1
                        : RequireHtfRewardForTp2Plus;

                double previousTarget =
                    stage == 0
                        ? _plan.Entry
                        : stage == 1
                            ? _plan.Tp1
                            : stage == 2
                                ? _plan.Tp2
                                : _plan.Tp3;

                double nextTarget =
                    stage == 0
                        ? _plan.Tp2
                        : stage == 1
                            ? _plan.Tp3
                            : stage == 2
                                ? _plan.Tp4
                                : 0;

                double best =
                    current;

                double bestScore =
                    double.MinValue;

                for (int i = 0;
                     i < levels.Count;
                     i++)
                {
                    Level level =
                        levels[i];

                    if (level.Score <
                        SmartTargetQuality)
                        continue;

                    bool htf =
                        IsHtfTimeframe(
                            level.Timeframe);

                    if (requireHtf &&
                        !htf)
                        continue;

                    if (htf &&
                        !UseHigherTfLiquidityTargets &&
                        level.Kind.IndexOf(
                            "LIQUIDITY",
                            StringComparison.OrdinalIgnoreCase) >= 0)
                        continue;

                    double distance =
                        Math.Abs(
                            level.Price -
                            _plan.Entry);

                    double rr =
                        distance /
                        Math.Max(
                            Symbol.PipSize,
                            _plan.Risk);

                    if (rr >
                        Math.Max(
                            0,
                            MaximumRewardRR))
                        continue;

                    bool improves =
                        _plan.Direction == 1
                            ? level.Price >
                              best + step
                            : level.Price <
                              best - step;

                    if (!improves)
                        continue;

                    bool keepsPreviousSpacing =
                        _plan.Direction == 1
                            ? level.Price >
                              previousTarget +
                              spacing
                            : level.Price <
                              previousTarget -
                              spacing;

                    if (!keepsPreviousSpacing)
                        continue;

                    bool keepsNextSpacing =
                        nextTarget <= 0 ||
                        (_plan.Direction == 1
                            ? level.Price <
                              nextTarget -
                              spacing
                            : level.Price >
                              nextTarget +
                              spacing);

                    if (!keepsNextSpacing)
                        continue;

                    if (RejectTargetObstacle &&
                        HasTargetObstacle(
                            _m5Bars,
                            closedM5,
                            _plan.Direction,
                            _plan.Entry,
                            level.Price,
                            atr))
                        continue;

                    double score =
                        level.Score;

                    if (htf)
                        score +=
                            HtfRewardBonus;

                    if (level.Kind.IndexOf(
                            "LIQUIDITY",
                            StringComparison.OrdinalIgnoreCase) >= 0)
                        score +=
                            LiquidityRewardBonus;

                    if (level.Kind.IndexOf(
                            "FVG",
                            StringComparison.OrdinalIgnoreCase) >= 0 ||
                        level.Kind.IndexOf(
                            "ORDER_BLOCK",
                            StringComparison.OrdinalIgnoreCase) >= 0)
                        score +=
                            ZoneRewardBonus;

                    score +=
                        Math.Min(
                            20,
                            Math.Max(
                                1,
                                level.Hits) * 2);

                    if (score > bestScore)
                    {
                        bestScore =
                            score;
                        best =
                            level.Price;
                    }
                }

                if (Math.Abs(
                        best -
                        current) <
                    Symbol.PipSize)
                    continue;

                if (stage == 0)
                    _plan.Tp1 =
                        NormalizePrice(best);
                else if (stage == 1)
                    _plan.Tp2 =
                        NormalizePrice(best);
                else if (stage == 2)
                    _plan.Tp3 =
                        NormalizePrice(best);
                else
                    _plan.Tp4 =
                        NormalizePrice(best);

                changed = true;
            }

            _lastTargetRepriceM5 =
                closedM5;

            if (changed)
            {
                ApplyTargetMeta(
                    levels,
                    _plan.Tp1,
                    atr,
                    out _plan.Tp1Source,
                    out _plan.Tp1Quality);

                ApplyTargetMeta(
                    levels,
                    _plan.Tp2,
                    atr,
                    out _plan.Tp2Source,
                    out _plan.Tp2Quality);

                ApplyTargetMeta(
                    levels,
                    _plan.Tp3,
                    atr,
                    out _plan.Tp3Source,
                    out _plan.Tp3Quality);

                ApplyTargetMeta(
                    levels,
                    _plan.Tp4,
                    atr,
                    out _plan.Tp4Source,
                    out _plan.Tp4Quality);

                _plan.HtfTargetCount =
                    CountHtfTargetsInPlan(
                        _plan);
            }

            RecalculatePlanRR();
        }

        private void RecalculatePlanRR()
        {
            if (_plan == null ||
                _plan.Risk <= 0)
                return;

            _plan.Tp1RR =
                _plan.Tp1 > 0
                    ? Math.Abs(
                        _plan.Tp1 -
                        _plan.Entry) /
                      _plan.Risk
                    : 0;

            _plan.Tp2RR =
                _plan.Tp2 > 0
                    ? Math.Abs(
                        _plan.Tp2 -
                        _plan.Entry) /
                      _plan.Risk
                    : 0;

            _plan.Tp3RR =
                _plan.Tp3 > 0
                    ? Math.Abs(
                        _plan.Tp3 -
                        _plan.Entry) /
                      _plan.Risk
                    : 0;

            _plan.Tp4RR =
                _plan.Tp4 > 0
                    ? Math.Abs(
                        _plan.Tp4 -
                        _plan.Entry) /
                      _plan.Risk
                    : 0;
        }

        private bool BetterStop(
            int direction,
            double proposed,
            double current)
        {
            if (!IsFinitePositive(proposed) ||
                !IsFinitePositive(current))
                return false;

            return direction == 1
                ? proposed > current
                : proposed < current;
        }

        // ============================================================
        #endregion

        #region Structure and Zones
        // ============================================================

        private bool BullStructure(
            Bars bars,
            int index,
            double atr)
        {
            double swing =
                FindSwingHigh(
                    bars,
                    index,
                    SwingStrength,
                    1);

            return
                IsFinitePositive(swing) &&
                bars.ClosePrices[index] >
                swing +
                atr *
                StructureBreakAtr;
        }

        private bool BearStructure(
            Bars bars,
            int index,
            double atr)
        {
            double swing =
                FindSwingLow(
                    bars,
                    index,
                    SwingStrength,
                    1);

            return
                IsFinitePositive(swing) &&
                bars.ClosePrices[index] <
                swing -
                atr *
                StructureBreakAtr;
        }

        private bool BullMss(
            Bars bars,
            int index,
            double atr)
        {
            double previous =
                FindSwingHigh(
                    bars,
                    index - 1,
                    SwingStrength,
                    1);

            return
                UseMssChoch &&
                IsFinitePositive(previous) &&
                bars.ClosePrices[index] >
                previous +
                atr *
                StructureBreakAtr;
        }

        private bool BearMss(
            Bars bars,
            int index,
            double atr)
        {
            double previous =
                FindSwingLow(
                    bars,
                    index - 1,
                    SwingStrength,
                    1);

            return
                UseMssChoch &&
                IsFinitePositive(previous) &&
                bars.ClosePrices[index] <
                previous -
                atr *
                StructureBreakAtr;
        }

        private bool BullChoch(
            Bars bars,
            int index)
        {
            if (!UseMssChoch ||
                index < 12)
                return false;

            double level =
                Highest(
                    bars,
                    Math.Max(
                        0,
                        index - 8),
                    index - 1);

            return
                bars.ClosePrices[index] >
                level &&
                bars.ClosePrices[index - 1] <=
                level;
        }

        private bool BearChoch(
            Bars bars,
            int index)
        {
            if (!UseMssChoch ||
                index < 12)
                return false;

            double level =
                Lowest(
                    bars,
                    Math.Max(
                        0,
                        index - 8),
                    index - 1);

            return
                bars.ClosePrices[index] <
                level &&
                bars.ClosePrices[index - 1] >=
                level;
        }

        private bool BullDisplacement(
            Bars bars,
            int index,
            double atr)
        {
            double body =
                Math.Abs(
                    bars.ClosePrices[index] -
                    bars.OpenPrices[index]);

            return
                UseDisplacement &&
                bars.ClosePrices[index] >
                bars.OpenPrices[index] &&
                body >=
                atr *
                DisplacementAtr;
        }

        private bool BearDisplacement(
            Bars bars,
            int index,
            double atr)
        {
            double body =
                Math.Abs(
                    bars.ClosePrices[index] -
                    bars.OpenPrices[index]);

            return
                UseDisplacement &&
                bars.ClosePrices[index] <
                bars.OpenPrices[index] &&
                body >=
                atr *
                DisplacementAtr;
        }

        private bool BullLiquiditySweep(
            Bars bars,
            int index,
            double atr)
        {
            if (!UseLiquiditySweep ||
                index < 5)
                return false;

            double prior =
                Lowest(
                    bars,
                    Math.Max(
                        0,
                        index - LiquidityLookback),
                    index - 1);

            double minimumDepth =
                Math.Max(
                    Math.Abs(
                        Symbol.Ask -
                        Symbol.Bid),
                    atr *
                    LiquiditySweepMinimumDepthAtr);

            return
                bars.LowPrices[index] <
                prior -
                minimumDepth &&
                bars.ClosePrices[index] >
                prior &&
                bars.ClosePrices[index] >
                bars.OpenPrices[index];
        }

        private bool BearLiquiditySweep(
            Bars bars,
            int index,
            double atr)
        {
            if (!UseLiquiditySweep ||
                index < 5)
                return false;

            double prior =
                Highest(
                    bars,
                    Math.Max(
                        0,
                        index - LiquidityLookback),
                    index - 1);

            double minimumDepth =
                Math.Max(
                    Math.Abs(
                        Symbol.Ask -
                        Symbol.Bid),
                    atr *
                    LiquiditySweepMinimumDepthAtr);

            return
                bars.HighPrices[index] >
                prior +
                minimumDepth &&
                bars.ClosePrices[index] <
                prior &&
                bars.ClosePrices[index] <
                bars.OpenPrices[index];
        }

        private bool Momentum(
            Bars bars,
            int index,
            int direction,
            double atr)
        {
            if (index < 3)
                return false;

            double move =
                bars.ClosePrices[index] -
                bars.ClosePrices[index - 2];

            return direction == 1
                ? move > atr * 0.15
                : move < -atr * 0.15;
        }

        private bool Rejection(
            Bars bars,
            int index,
            int direction)
        {
            double range =
                Math.Max(
                    Symbol.PipSize,
                    bars.HighPrices[index] -
                    bars.LowPrices[index]);

            double body =
                Math.Abs(
                    bars.ClosePrices[index] -
                    bars.OpenPrices[index]);

            if (direction == 1)
            {
                double wick =
                    Math.Min(
                        bars.OpenPrices[index],
                        bars.ClosePrices[index]) -
                    bars.LowPrices[index];

                return wick > body * 1.25 &&
                       wick / range > 0.20;
            }

            double upper =
                bars.HighPrices[index] -
                Math.Max(
                    bars.OpenPrices[index],
                    bars.ClosePrices[index]);

            return upper > body * 1.25 &&
                   upper / range > 0.20;
        }

        private bool IsZoneFullyMitigated(
            Bars bars,
            int createdIndex,
            int currentIndex,
            int direction,
            double low,
            double high)
        {
            if (!UseZoneMitigationGuard ||
                bars == null ||
                createdIndex < 0 ||
                currentIndex <= createdIndex)
                return false;

            int start =
                Math.Max(
                    0,
                    createdIndex + 1);

            int end =
                Math.Min(
                    bars.Count - 1,
                    currentIndex);

            for (int i = start;
                 i <= end;
                 i++)
            {
                if (direction == 1)
                {
                    if (bars.LowPrices[i] <= low)
                        return true;
                }
                else if (direction == -1)
                {
                    if (bars.HighPrices[i] >= high)
                        return true;
                }
            }

            return false;
        }

        private bool HasZoneRetest(
            Bars bars,
            int createdIndex,
            int currentIndex,
            double low,
            double high)
        {
            if (bars == null ||
                createdIndex < 0 ||
                currentIndex <= createdIndex)
                return false;

            int start =
                Math.Max(
                    0,
                    createdIndex + 1);

            int end =
                Math.Min(
                    bars.Count - 1,
                    currentIndex);

            for (int i = start;
                 i <= end;
                 i++)
            {
                bool overlaps =
                    bars.HighPrices[i] >= low &&
                    bars.LowPrices[i] <= high;

                if (overlaps)
                    return true;
            }

            return false;
        }

        private Zone FindNearestFvg(
            Bars bars,
            int index,
            int direction,
            double atr)
        {
            if (!UseFvg ||
                bars == null ||
                index < 2 ||
                atr <= 0)
                return null;

            int first =
                Math.Max(
                    1,
                    index -
                    FvgLookback);

            Zone best = null;

            for (int i = index;
                 i >= first;
                 i--)
            {
                if (direction == 1)
                {
                    if (i >= 2)
                    {
                        double gap =
                            bars.LowPrices[i] -
                            bars.HighPrices[i - 2];

                        if (gap >=
                            atr *
                            MinimumFvgAtr)
                        {
                            best =
                                SelectNearestFvg(
                                    bars.ClosePrices[index],
                                    best,
                                    BuildManagedFvgZone(
                                        bars,
                                        i,
                                        index,
                                        direction,
                                        bars.HighPrices[i - 2],
                                        bars.LowPrices[i],
                                        gap,
                                        false,
                                        atr));
                        }
                    }

                    if (UseTwoBarImbalanceFvg)
                    {
                        double gap =
                            bars.LowPrices[i] -
                            bars.HighPrices[i - 1];

                        if (gap >=
                            atr *
                            MinimumFvgAtr)
                        {
                            best =
                                SelectNearestFvg(
                                    bars.ClosePrices[index],
                                    best,
                                    BuildManagedFvgZone(
                                        bars,
                                        i,
                                        index,
                                        direction,
                                        bars.HighPrices[i - 1],
                                        bars.LowPrices[i],
                                        gap,
                                        true,
                                        atr));
                        }
                    }
                }
                else
                {
                    if (i >= 2)
                    {
                        double gap =
                            bars.LowPrices[i - 2] -
                            bars.HighPrices[i];

                        if (gap >=
                            atr *
                            MinimumFvgAtr)
                        {
                            best =
                                SelectNearestFvg(
                                    bars.ClosePrices[index],
                                    best,
                                    BuildManagedFvgZone(
                                        bars,
                                        i,
                                        index,
                                        direction,
                                        bars.HighPrices[i],
                                        bars.LowPrices[i - 2],
                                        gap,
                                        false,
                                        atr));
                        }
                    }

                    if (UseTwoBarImbalanceFvg)
                    {
                        double gap =
                            bars.LowPrices[i - 1] -
                            bars.HighPrices[i];

                        if (gap >=
                            atr *
                            MinimumFvgAtr)
                        {
                            best =
                                SelectNearestFvg(
                                    bars.ClosePrices[index],
                                    best,
                                    BuildManagedFvgZone(
                                        bars,
                                        i,
                                        index,
                                        direction,
                                        bars.HighPrices[i],
                                        bars.LowPrices[i - 1],
                                        gap,
                                        true,
                                        atr));
                        }
                    }
                }
            }

            if (best == null ||
                best.Age >
                MaximumZoneAgeBars)
                return null;

            if (RequireFvgRetest)
            {
                double price =
                    bars.ClosePrices[index];

                double tolerance =
                    atr *
                    (RequireRetestQuality
                        ? RetestZoneToleranceAtr
                        : ZoneProximityAtr);

                if (price <
                    best.Low -
                    tolerance ||
                    price >
                    best.High +
                    tolerance)
                    return null;
            }

            return best;
        }

        private Zone BuildManagedFvgZone(
            Bars bars,
            int createdIndex,
            int currentIndex,
            int direction,
            double low,
            double high,
            double gap,
            bool twoBarImbalance,
            double atr)
        {
            if (bars == null ||
                createdIndex < 0 ||
                currentIndex < createdIndex ||
                low >= high)
                return null;

            double managedLow = low;
            double managedHigh = high;

            if (EnableFvgPartialMitigation &&
                UseZoneMitigationGuard &&
                currentIndex > createdIndex)
            {
                for (int i = createdIndex + 1;
                     i <= currentIndex;
                     i++)
                {
                    double breaker =
                        direction == 1
                            ? (FvgBreakByWicks
                                ? bars.LowPrices[i]
                                : Math.Min(
                                    bars.OpenPrices[i],
                                    bars.ClosePrices[i]))
                            : (FvgBreakByWicks
                                ? bars.HighPrices[i]
                                : Math.Max(
                                    bars.OpenPrices[i],
                                    bars.ClosePrices[i]));

                    // FIX (CFIP-BUG-FVG-RESET): a full breach used to reset
                    // managedLow/managedHigh back to the ZONE'S ORIGINAL,
                    // pristine size and stop scanning ("soft" mode), which
                    // made a completely swept FVG look full-size and fresh
                    // again to every caller (including live target/stop
                    // selection). A zone price has traded straight through
                    // has no valid remaining range in either mode — collapse
                    // it to zero width instead, so the standard
                    // "managedLow >= managedHigh" guard below excludes it
                    // exactly like the strict (FvgInvalidateOnFullFill=true)
                    // path already does. The toggle's only remaining effect
                    // is whether the scan exits immediately on first breach
                    // (true) or keeps evaluating for reporting purposes
                    // before arriving at the same exclusion (false).
                    if (direction == 1)
                    {
                        if (breaker <= managedLow)
                        {
                            if (FvgInvalidateOnFullFill)
                                return null;

                            managedHigh = managedLow;
                            break;
                        }

                        if (breaker < managedHigh)
                            managedHigh =
                                Math.Max(
                                    managedLow,
                                    breaker);
                    }
                    else
                    {
                        if (breaker >= managedHigh)
                        {
                            if (FvgInvalidateOnFullFill)
                                return null;

                            managedLow = managedHigh;
                            break;
                        }

                        if (breaker > managedLow)
                            managedLow =
                                Math.Min(
                                    managedHigh,
                                    breaker);
                    }

                    if (managedHigh -
                        managedLow <=
                        Symbol.TickSize)
                    {
                        if (FvgInvalidateOnFullFill)
                            return null;

                        managedHigh = managedLow;
                        break;
                    }
                }
            }
            else if (FvgInvalidateOnFullFill &&
                     IsZoneFullyMitigated(
                         bars,
                         createdIndex,
                         currentIndex,
                         direction,
                         low,
                         high))
            {
                return null;
            }

            if (managedLow >= managedHigh)
                return null;

            double normalizedGap =
                gap /
                Math.Max(
                    Symbol.PipSize,
                    atr);

            double remainingRatio =
                (managedHigh - managedLow) /
                Math.Max(
                    Symbol.TickSize,
                    high - low);

            int quality =
                70 +
                (int)Math.Round(
                    15 *
                    Math.Max(
                        0,
                        normalizedGap));

            if (EnableFvgPartialMitigation &&
                UseZoneMitigationGuard)
            {
                quality +=
                    (int)Math.Round(
                        10 *
                        ClampDouble(
                            remainingRatio,
                            0,
                            1));
            }

            if (twoBarImbalance)
                quality -= 3;

            Zone z = new Zone();

            z.Low = managedLow;
            z.High = managedHigh;
            z.Direction = direction;
            z.Kind =
                twoBarImbalance
                    ? "FVG_2BAR"
                    : "FVG";
            z.Age =
                currentIndex -
                createdIndex;
            z.Quality =
                ClampInt(
                    quality,
                    0,
                    100);

            return z;
        }

        private Zone SelectNearestFvg(
            double price,
            Zone best,
            Zone candidate)
        {
            if (candidate == null)
                return best;

            if (best == null)
                return candidate;

            double candidateDistance =
                DistanceToZone(
                    price,
                    candidate);

            double bestDistance =
                DistanceToZone(
                    price,
                    best);

            if (candidateDistance <
                bestDistance -
                Symbol.TickSize)
                return candidate;

            if (Math.Abs(
                    candidateDistance -
                    bestDistance) <=
                Symbol.TickSize &&
                candidate.Quality >
                best.Quality)
                return candidate;

            if (Math.Abs(
                    candidateDistance -
                    bestDistance) <=
                Symbol.TickSize &&
                candidate.Quality ==
                best.Quality &&
                candidate.Age <
                best.Age)
                return candidate;

            return best;
        }

        private Zone FindNearestOrderBlock(
            Bars bars,
            int index,
            int direction,
            double atr)
        {
            if (!UseOrderBlock ||
                bars == null ||
                index < 8 ||
                atr <= 0)
                return null;

            int first =
                Math.Max(
                    2,
                    index -
                    ObLookback);

            Zone best = null;
            double bestScore =
                double.MinValue;

            double market =
                direction == 1
                    ? Symbol.Ask
                    : Symbol.Bid;

            for (int i = index - 1;
                 i >= first;
                 i--)
            {
                bool opposite =
                    direction == 1
                        ? bars.ClosePrices[i] <
                          bars.OpenPrices[i]
                        : bars.ClosePrices[i] >
                          bars.OpenPrices[i];

                if (!opposite)
                    continue;

                Zone candidate =
                    BuildOrderBlockCandidate(
                        bars,
                        i,
                        index,
                        direction,
                        atr);

                if (candidate == null ||
                    candidate.Quality <
                    Math.Max(
                        50,
                        ObMinimumQuality))
                    continue;

                if (RequireObRetest &&
                    !HasZoneRetest(
                        bars,
                        i,
                        index,
                        candidate.Low,
                        candidate.High))
                    continue;

                double distance =
                    DistanceToZone(
                        market,
                        candidate);

                double distancePenalty =
                    14.0 *
                    Math.Min(
                        1.0,
                        distance /
                        Math.Max(
                            Symbol.TickSize,
                            atr * 3.0));

                double agePenalty =
                    Math.Min(
                        12.0,
                        candidate.Age * 0.35);

                double selectionScore =
                    candidate.Quality -
                    distancePenalty -
                    agePenalty;

                if (candidate.Quality >
                    (best == null
                        ? 0
                        : best.Quality))
                {
                    selectionScore += 2.0;
                }

                if (best == null ||
                    selectionScore >
                    bestScore)
                {
                    best =
                        candidate;
                    bestScore =
                        selectionScore;
                }
            }

            return best;
        }

        private Zone BuildOrderBlockCandidate(
            Bars bars,
            int createdIndex,
            int currentIndex,
            int direction,
            double atr)
        {
            if (bars == null ||
                createdIndex < 2 ||
                currentIndex <= createdIndex ||
                atr <= 0)
                return null;

            double open =
                bars.OpenPrices[
                    createdIndex];

            double close =
                bars.ClosePrices[
                    createdIndex];

            double high =
                bars.HighPrices[
                    createdIndex];

            double low =
                bars.LowPrices[
                    createdIndex];

            double bodyLow =
                Math.Min(
                    open,
                    close);

            double bodyHigh =
                Math.Max(
                    open,
                    close);

            double zoneLow =
                ObUseBodyForZone
                    ? bodyLow
                    : low;

            double zoneHigh =
                ObUseBodyForZone
                    ? bodyHigh
                    : high;

            if (zoneLow >=
                zoneHigh)
                return null;

            double range =
                high -
                low;

            double body =
                Math.Abs(
                    close -
                    open);

            if (range <= 0 ||
                body <= 0)
                return null;

            bool displacement =
                false;

            double strongestBody =
                0;

            int impulseEnd =
                Math.Min(
                    currentIndex,
                    createdIndex +
                    Math.Max(
                        2,
                        ObImpulseBars));

            for (int j =
                     createdIndex + 1;
                 j <= impulseEnd;
                 j++)
            {
                double nextBody =
                    Math.Abs(
                        bars.ClosePrices[j] -
                        bars.OpenPrices[j]);

                bool directional =
                    direction == 1
                        ? bars.ClosePrices[j] >
                          bars.OpenPrices[j]
                        : bars.ClosePrices[j] <
                          bars.OpenPrices[j];

                if (directional &&
                    nextBody >=
                    atr *
                    ObDisplacementAtr)
                {
                    displacement =
                        true;

                    strongestBody =
                        Math.Max(
                            strongestBody,
                            nextBody);
                }
            }

            if (RequireObDisplacement &&
                !displacement)
                return null;

            bool structureBreak =
                false;

            int structureStart =
                Math.Max(
                    1,
                    createdIndex -
                    Math.Max(
                        3,
                        ObStructureLookback));

            if (direction == 1)
            {
                double priorHigh =
                    Highest(
                        bars,
                        structureStart,
                        createdIndex - 1);

                for (int j =
                         createdIndex + 1;
                     j <= impulseEnd;
                     j++)
                {
                    if (bars.ClosePrices[j] >
                        priorHigh +
                        atr *
                        StructureBreakAtr)
                    {
                        structureBreak =
                            true;
                        break;
                    }
                }
            }
            else
            {
                double priorLow =
                    Lowest(
                        bars,
                        structureStart,
                        createdIndex - 1);

                for (int j =
                         createdIndex + 1;
                     j <= impulseEnd;
                     j++)
                {
                    if (bars.ClosePrices[j] <
                        priorLow -
                        atr *
                        StructureBreakAtr)
                    {
                        structureBreak =
                            true;
                        break;
                    }
                }
            }

            double managedLow =
                zoneLow;

            double managedHigh =
                zoneHigh;

            bool partiallyMitigated =
                false;

            double originalWidth =
                Math.Max(
                    Symbol.TickSize,
                    zoneHigh -
                    zoneLow);

            if (UseZoneMitigationGuard)
            {
                for (int j =
                         createdIndex + 1;
                     j <= currentIndex;
                     j++)
                {
                    double probe =
                        direction == 1
                            ? (ObBreakByWicks
                                ? bars.LowPrices[j]
                                : Math.Min(
                                    bars.OpenPrices[j],
                                    bars.ClosePrices[j]))
                            : (ObBreakByWicks
                                ? bars.HighPrices[j]
                                : Math.Max(
                                    bars.OpenPrices[j],
                                    bars.ClosePrices[j]));

                    if (direction == 1)
                    {
                        if (probe <=
                            zoneLow)
                            return null;

                        if (probe <
                            managedHigh)
                        {
                            managedHigh =
                                Math.Max(
                                    managedLow,
                                    probe);
                            partiallyMitigated =
                                true;
                        }
                    }
                    else
                    {
                        if (probe >=
                            zoneHigh)
                            return null;

                        if (probe >
                            managedLow)
                        {
                            managedLow =
                                Math.Min(
                                    managedHigh,
                                    probe);
                            partiallyMitigated =
                                true;
                        }
                    }

                    if (managedHigh -
                        managedLow <=
                        Symbol.TickSize)
                        return null;
                }
            }

            double remainingRatio =
                (managedHigh -
                 managedLow) /
                originalWidth;

            if (remainingRatio <=
                0.05)
                return null;

            bool liquiditySweep =
                HasOrderBlockLiquiditySweep(
                    bars,
                    createdIndex,
                    direction);

            bool fvgConfluence =
                HasOrderBlockFvgConfluence(
                    bars,
                    createdIndex,
                    impulseEnd,
                    direction,
                    atr,
                    managedLow,
                    managedHigh);

            double bodyRatio =
                body /
                Math.Max(
                    Symbol.TickSize,
                    range);

            double impulseRatio =
                strongestBody /
                Math.Max(
                    Symbol.PipSize,
                    atr);

            int quality =
                54;

            if (displacement)
                quality += 14;

            if (structureBreak)
                quality += 13;

            if (liquiditySweep)
                quality += 8;

            if (fvgConfluence)
                quality += 8;

            quality +=
                (int)Math.Round(
                    8 *
                    Math.Min(
                        1.0,
                        Math.Max(
                            0,
                            remainingRatio)));

            if (bodyRatio <=
                0.25)
                quality -= 4;
            else if (bodyRatio >=
                     0.65)
                quality += 3;

            if (impulseRatio >=
                1.50)
                quality += 4;
            else if (impulseRatio >=
                     1.00)
                quality += 2;

            quality -=
                Math.Min(
                    10,
                    (currentIndex -
                     createdIndex) /
                    6);

            if (partiallyMitigated)
            {
                quality -=
                    (int)Math.Round(
                        10 *
                        (1.0 -
                         Math.Min(
                             1.0,
                             Math.Max(
                                 0,
                                 remainingRatio))));
            }

            return new Zone
            {
                Low = managedLow,
                High = managedHigh,
                Direction = direction,
                Kind = "ORDER_BLOCK",
                Age = currentIndex -
                    createdIndex,
                Quality = ClampInt(
                    quality,
                    0,
                    100)
            };
        }

        private bool HasOrderBlockLiquiditySweep(
            Bars bars,
            int index,
            int direction)
        {
            if (bars == null ||
                index < 5)
                return false;

            int start =
                Math.Max(
                    1,
                    index -
                    Math.Max(
                        5,
                        Math.Min(
                            LiquidityLookback,
                            20)));

            if (direction == 1)
            {
                double priorLow =
                    Lowest(
                        bars,
                        start,
                        index - 1);

                return
                    bars.LowPrices[index] <
                    priorLow;
            }

            double priorHigh =
                Highest(
                    bars,
                    start,
                    index - 1);

            return
                bars.HighPrices[index] >
                priorHigh;
        }

        private bool HasOrderBlockFvgConfluence(
            Bars bars,
            int startIndex,
            int endIndex,
            int direction,
            double atr,
            double zoneLow,
            double zoneHigh)
        {
            if (!UseFvg ||
                bars == null ||
                atr <= 0 ||
                startIndex >=
                endIndex)
                return false;

            int first =
                Math.Max(
                    2,
                    startIndex + 1);

            for (int i = first;
                 i <= endIndex;
                 i++)
            {
                double fvgLow =
                    0;

                double fvgHigh =
                    0;

                if (direction == 1)
                {
                    double gap =
                        bars.LowPrices[i] -
                        bars.HighPrices[i - 2];

                    if (gap <
                        atr *
                        MinimumFvgAtr)
                        continue;

                    fvgLow =
                        bars.HighPrices[i - 2];

                    fvgHigh =
                        bars.LowPrices[i];
                }
                else
                {
                    double gap =
                        bars.LowPrices[i - 2] -
                        bars.HighPrices[i];

                    if (gap <
                        atr *
                        MinimumFvgAtr)
                        continue;

                    fvgLow =
                        bars.HighPrices[i];

                    fvgHigh =
                        bars.LowPrices[i - 2];
                }

                if (fvgHigh >=
                    zoneLow -
                    atr * 0.05 &&
                    fvgLow <=
                    zoneHigh +
                    atr * 0.05)
                    return true;
            }

            return false;
        }

        // Internal FVG engine: standard 3-candle FVG plus optional 2-bar imbalance, with body/wick-aware partial mitigation. No chart objects are created here.
        private Zone FindNearestOpposingZone(
            Bars bars,
            int index,
            int direction,
            double atr)
        {
            Zone fvg =
                FindNearestFvg(
                    bars,
                    index,
                    direction,
                    atr);

            Zone ob =
                FindNearestOrderBlock(
                    bars,
                    index,
                    direction,
                    atr);

            if (fvg == null)
                return ob;

            if (ob == null)
                return fvg;

            return
                ob.Quality >=
                fvg.Quality
                    ? ob
                    : fvg;
        }

        private double DistanceToZone(
            double price,
            Zone zone)
        {
            if (zone == null)
                return double.MaxValue;

            if (price < zone.Low)
                return zone.Low - price;

            if (price > zone.High)
                return price - zone.High;

            return 0;
        }

        private double FindSwingHigh(
            Bars bars,
            int index,
            int strength,
            int occurrence)
        {
            if (bars == null ||
                index < strength * 2 + 1)
                return 0;

            int first =
                Math.Max(
                    strength,
                    index -
                    StructureLookback);

            int last =
                Math.Min(
                    index -
                    strength,
                    bars.Count -
                    strength -
                    1);

            int found = 0;

            for (int i = last;
                 i >= first;
                 i--)
            {
                bool swing = true;

                for (int j = 1;
                     j <= strength;
                     j++)
                {
                    if (bars.HighPrices[i] <=
                        bars.HighPrices[i - j] ||
                        bars.HighPrices[i] <=
                        bars.HighPrices[i + j])
                    {
                        swing = false;
                        break;
                    }
                }

                if (!swing)
                    continue;

                found++;

                if (found ==
                    Math.Max(
                        1,
                        occurrence))
                    return bars.HighPrices[i];
            }

            return 0;
        }

        private double FindSwingLow(
            Bars bars,
            int index,
            int strength,
            int occurrence)
        {
            if (bars == null ||
                index < strength * 2 + 1)
                return 0;

            int first =
                Math.Max(
                    strength,
                    index -
                    StructureLookback);

            int last =
                Math.Min(
                    index -
                    strength,
                    bars.Count -
                    strength -
                    1);

            int found = 0;

            for (int i = last;
                 i >= first;
                 i--)
            {
                bool swing = true;

                for (int j = 1;
                     j <= strength;
                     j++)
                {
                    if (bars.LowPrices[i] >=
                        bars.LowPrices[i - j] ||
                        bars.LowPrices[i] >=
                        bars.LowPrices[i + j])
                    {
                        swing = false;
                        break;
                    }
                }

                if (!swing)
                    continue;

                found++;

                if (found ==
                    Math.Max(
                        1,
                        occurrence))
                    return bars.LowPrices[i];
            }

            return 0;
        }

        private double FindSwingHighAbove(
            Bars bars,
            int index,
            double price)
        {
            if (bars == null ||
                index < 10)
                return 0;

            int first =
                Math.Max(
                    SwingStrength,
                    index -
                    StructureLookback);

            int last =
                Math.Min(
                    index -
                    SwingStrength,
                    bars.Count -
                    SwingStrength -
                    1);

            double best = 0;

            for (int i = first;
                 i <= last;
                 i++)
            {
                bool swing = true;

                for (int j = 1;
                     j <= SwingStrength;
                     j++)
                {
                    if (bars.HighPrices[i] <=
                        bars.HighPrices[i - j] ||
                        bars.HighPrices[i] <=
                        bars.HighPrices[i + j])
                    {
                        swing = false;
                        break;
                    }
                }

                if (swing &&
                    bars.HighPrices[i] >
                    price &&
                    (best == 0 ||
                     bars.HighPrices[i] <
                     best))
                    best =
                        bars.HighPrices[i];
            }

            return best;
        }

        private double FindSwingLowBelow(
            Bars bars,
            int index,
            double price)
        {
            if (bars == null ||
                index < 10)
                return 0;

            int first =
                Math.Max(
                    SwingStrength,
                    index -
                    StructureLookback);

            int last =
                Math.Min(
                    index -
                    SwingStrength,
                    bars.Count -
                    SwingStrength -
                    1);

            double best = 0;

            for (int i = first;
                 i <= last;
                 i++)
            {
                bool swing = true;

                for (int j = 1;
                     j <= SwingStrength;
                     j++)
                {
                    if (bars.LowPrices[i] >=
                        bars.LowPrices[i - j] ||
                        bars.LowPrices[i] >=
                        bars.LowPrices[i + j])
                    {
                        swing = false;
                        break;
                    }
                }

                if (swing &&
                    bars.LowPrices[i] <
                    price &&
                    (best == 0 ||
                     bars.LowPrices[i] >
                     best))
                    best =
                        bars.LowPrices[i];
            }

            return best;
        }

        private double FindEqualHigh(
            Bars bars,
            int index,
            double reference,
            double atr)
        {
            if (!UseEqualHighLow ||
                bars == null ||
                index < 10 ||
                atr <= 0)
                return 0;

            double tolerance =
                Math.Max(
                    Symbol.PipSize * 2,
                    atr *
                    Math.Max(
                        0.02,
                        EqualLevelToleranceAtr));

            int first =
                Math.Max(
                    2,
                    index -
                    LiquidityLookback);

            Dictionary<long, List<double>> buckets =
                new Dictionary<long, List<double>>();

            double best = 0;

            for (int i = first;
                 i < index - 1;
                 i++)
            {
                double high =
                    bars.HighPrices[i];

                if (!IsFinitePositive(high))
                    continue;

                long bucket =
                    (long)Math.Floor(
                        high /
                        tolerance);

                for (long b = bucket - 1;
                     b <= bucket + 1;
                     b++)
                {
                    List<double> values;

                    if (!buckets.TryGetValue(
                            b,
                            out values))
                        continue;

                    for (int j = 0;
                         j < values.Count;
                         j++)
                    {
                        if (Math.Abs(
                                high -
                                values[j]) >
                            tolerance)
                            continue;

                        double level =
                            Math.Max(
                                high,
                                values[j]);

                        if (level > reference &&
                            (best <= 0 ||
                             level < best))
                            best = level;

                        break;
                    }
                }

                List<double> bucketValues;

                if (!buckets.TryGetValue(
                        bucket,
                        out bucketValues))
                {
                    bucketValues =
                        new List<double>();

                    buckets[bucket] =
                        bucketValues;
                }

                bucketValues.Add(
                    high);
            }

            return best;
        }

        private double FindEqualLow(
            Bars bars,
            int index,
            double reference,
            double atr)
        {
            if (!UseEqualHighLow ||
                bars == null ||
                index < 10 ||
                atr <= 0)
                return 0;

            double tolerance =
                Math.Max(
                    Symbol.PipSize * 2,
                    atr *
                    Math.Max(
                        0.02,
                        EqualLevelToleranceAtr));

            int first =
                Math.Max(
                    2,
                    index -
                    LiquidityLookback);

            Dictionary<long, List<double>> buckets =
                new Dictionary<long, List<double>>();

            double best = 0;

            for (int i = first;
                 i < index - 1;
                 i++)
            {
                double low =
                    bars.LowPrices[i];

                if (!IsFinitePositive(low))
                    continue;

                long bucket =
                    (long)Math.Floor(
                        low /
                        tolerance);

                for (long b = bucket - 1;
                     b <= bucket + 1;
                     b++)
                {
                    List<double> values;

                    if (!buckets.TryGetValue(
                            b,
                            out values))
                        continue;

                    for (int j = 0;
                         j < values.Count;
                         j++)
                    {
                        if (Math.Abs(
                                low -
                                values[j]) >
                            tolerance)
                            continue;

                        double level =
                            Math.Min(
                                low,
                                values[j]);

                        if (level < reference &&
                            (best <= 0 ||
                             level > best))
                            best = level;

                        break;
                    }
                }

                List<double> bucketValues;

                if (!buckets.TryGetValue(
                        bucket,
                        out bucketValues))
                {
                    bucketValues =
                        new List<double>();

                    buckets[bucket] =
                        bucketValues;
                }

                bucketValues.Add(
                    low);
            }

            return best;
        }

        private bool StableDirection(
            Bars bars,
            int index,
            int direction,
            int count)
        {
            for (int k = 0;
                 k < Math.Max(
                     1,
                     count);
                 k++)
            {
                int i =
                    index -
                    k;

                if (i < 15)
                    return false;

                double fast =
                    Ema(
                        bars,
                        i,
                        true);

                double slow =
                    Ema(
                        bars,
                        i,
                        false);

                double rsi =
                    Rsi(
                        bars,
                        i);

                double dmi =
                    DmiBias(
                        bars,
                        i);

                bool bull =
                    bars.ClosePrices[i] >
                    fast &&
                    fast >= slow &&
                    rsi >= 50 &&
                    dmi >= 0;

                bool bear =
                    bars.ClosePrices[i] <
                    fast &&
                    fast <= slow &&
                    rsi <= 50 &&
                    dmi <= 0;

                if (direction == 1 &&
                    !bull)
                    return false;

                if (direction == -1 &&
                    !bear)
                    return false;
            }

            return true;
        }

        // ============================================================
        #endregion

        #region Retest Regime and Filters
        // ============================================================

        private int RetestQuality(
            Bars bars,
            int index,
            int direction)
        {
            if (bars == null ||
                index < 15)
                return 0;

            double atr =
                Atr(
                    bars,
                    index);

            if (atr <= 0)
                return 0;

            int quality = 35;

            Zone zone =
                FindNearestOpposingZone(
                    bars,
                    index,
                    direction,
                    atr);

            if (zone != null)
            {
                double price =
                    bars.ClosePrices[index];

                double tolerance =
                    atr *
                    Math.Max(
                        ZoneProximityAtr,
                        RetestZoneToleranceAtr);

                bool near =
                    price >= zone.Low - tolerance &&
                    price <= zone.High + tolerance;

                if (near)
                    quality += 25;

                if (zone.Quality >=
                    FastReversalMinimumZoneQuality)
                    quality += 10;

                int first =
                    Math.Max(
                        2,
                        index -
                        RetestLookbackBars);

                bool touched = false;

                for (int i = first;
                     i <= index;
                     i++)
                {
                    if (bars.HighPrices[i] >= zone.Low &&
                        bars.LowPrices[i] <= zone.High)
                    {
                        touched = true;
                        break;
                    }
                }

                if (touched)
                    quality += 15;

                if (RequireRetestCloseConfirmation &&
                    !near)
                    quality -= 15;
            }

            double body =
                Math.Abs(
                    bars.ClosePrices[index] -
                    bars.OpenPrices[index]);

            if (body >=
                atr *
                Math.Max(
                    RetestRejectionBodyAtr,
                    MinimumTriggerBodyAtr))
                quality += 10;

            bool directional =
                direction == 1
                    ? bars.ClosePrices[index] >
                      bars.OpenPrices[index]
                    : bars.ClosePrices[index] <
                      bars.OpenPrices[index];

            if (directional)
                quality += 5;

            int firstRecent =
                Math.Max(
                    2,
                    index -
                    RetestMaxBarsAfterDisplacement);

            for (int i = firstRecent;
                 i <= index;
                 i++)
            {
                double bodySize =
                    Math.Abs(
                        bars.ClosePrices[i] -
                        bars.OpenPrices[i]);

                if (bodySize >=
                        atr * DisplacementAtr &&
                    (direction == 1
                        ? bars.ClosePrices[i] >
                          bars.OpenPrices[i]
                        : bars.ClosePrices[i] <
                          bars.OpenPrices[i]))
                {
                    quality += 10;
                    break;
                }
            }

            return ClampInt(
                quality,
                0,
                100);
        }

        private string DetectRegime(
            Bars bars,
            int index)
        {
            double atr =
                Atr(
                    bars,
                    index);

            double oldAtr =
                Atr(
                    bars,
                    Math.Max(
                        20,
                        index - 10));

            if (atr <= 0 ||
                oldAtr <= 0)
                return "UNKNOWN";

            double ratio =
                atr /
                oldAtr;

            double adx =
                Adx(
                    bars,
                    index);

            if (ratio >= 1.30)
                return "EXPANSION";

            if (ratio <= 0.80)
                return "COMPRESSION";

            if (adx < AdxMinimum)
                return "RANGE";

            if (Math.Abs(
                    Ema(
                        bars,
                        index,
                        true) -
                    Ema(
                        bars,
                        index,
                        false)) <=
                atr * 0.10)
                return "TRANSITION";

            return "TREND";
        }

        private int RegimeQuality(
            string regime,
            Bars bars,
            int index)
        {
            double adx =
                Adx(
                    bars,
                    index);

            if (regime == "TREND")
                return ClampInt(
                    (int)Math.Round(
                        60 +
                        Math.Min(
                            35,
                            adx)),
                    0,
                    100);

            if (regime == "EXPANSION")
                return ClampInt(
                    (int)Math.Round(
                        65 +
                        Math.Min(
                            30,
                            adx * 0.5)),
                    0,
                    100);

            if (regime == "RANGE")
                return 52;

            if (regime == "COMPRESSION")
                return 45;

            if (regime == "TRANSITION")
                return 48;

            return 35;
        }

        private bool SessionAllowed(DateTime utc)
        {
            if (!UseSessionFilter)
                return true;

            if (SessionStartUtc <= SessionEndUtc)
                return utc.Hour >= SessionStartUtc &&
                       utc.Hour < SessionEndUtc;

            return utc.Hour >= SessionStartUtc ||
                   utc.Hour < SessionEndUtc;
        }

        private bool FridayAllowed(DateTime utc)
        {
            if (!AvoidFridayLateEntry ||
                utc.DayOfWeek != DayOfWeek.Friday)
                return true;

            return utc.Hour < FridayCutoffUtc;
        }

        private bool SpreadAllowed(
            Bars bars,
            int index)
        {
            if (!UseSpreadFilter)
                return true;

            double atr =
                Atr(
                    bars,
                    index);

            if (atr <= 0)
                return false;

            double spread =
                Math.Max(
                    0,
                    Symbol.Ask -
                    Symbol.Bid);

            return spread <=
                   atr *
                   MaximumSpreadAtr;
        }

        private bool VolatilityBlocked(
            Bars bars,
            int index)
        {
            if ((!UseVolatilityGuard &&
                 !UseVolatilityEventGuard) ||
                bars == null ||
                index < 10)
                return false;

            int window =
                UseVolatilityEventGuard
                    ? Math.Max(
                        0,
                        EventGuardCooldownBars)
                    : 0;

            int first =
                Math.Max(
                    1,
                    index - window);

            for (int i = first;
                 i <= index;
                 i++)
            {
                double barAtr =
                    Atr(
                        bars,
                        i);

                double priorAtr =
                    Atr(
                        bars,
                        Math.Max(
                            5,
                            i - 10));

                if (barAtr <= 0 ||
                    priorAtr <= 0)
                    continue;

                double range =
                    bars.HighPrices[i] -
                    bars.LowPrices[i];

                if (range >=
                        barAtr *
                        EventShockRangeAtr &&
                    barAtr >=
                        priorAtr *
                        EventShockAtrExpansion)
                    return true;
            }

            return false;
        }

        private bool NewsBlocked(
            DateTime utc,
            out string reason)
        {
            reason = "";

            if (string.IsNullOrWhiteSpace(
                    NewsBlackoutUtc))
                return false;

            string[] items =
                NewsBlackoutUtc.Split(
                    new[] { ',', ';', '|' },
                    StringSplitOptions.RemoveEmptyEntries);

            int current =
                utc.Hour * 60 +
                utc.Minute;

            for (int i = 0;
                 i < items.Length;
                 i++)
            {
                string[] parts =
                    items[i]
                    .Trim()
                    .Split('-');

                if (parts.Length != 2)
                    continue;

                int start;
                int end;

                if (!TryParseMinutes(
                        parts[0],
                        out start) ||
                    !TryParseMinutes(
                        parts[1],
                        out end))
                    continue;

                bool blocked =
                    start <= end
                        ? current >= start &&
                          current <= end
                        : current >= start ||
                          current <= end;

                if (blocked)
                {
                    reason =
                        "NEWS BLACKOUT";
                    return true;
                }
            }

            return false;
        }

        private bool TryParseMinutes(
            string value,
            out int minutes)
        {
            minutes = 0;

            if (string.IsNullOrWhiteSpace(
                    value))
                return false;

            string[] parts =
                value.Trim().Split(':');

            if (parts.Length != 2)
                return false;

            int h;
            int m;

            if (!int.TryParse(
                    parts[0],
                    out h) ||
                !int.TryParse(
                    parts[1],
                    out m))
                return false;

            if (h < 0 ||
                h > 23 ||
                m < 0 ||
                m > 59)
                return false;

            minutes =
                h * 60 +
                m;

            return true;
        }

        private bool CooldownBlocked(
            int currentM5)
        {
            if (CooldownM5Bars <= 0 ||
                _lastSignalM5 < 0)
                return false;

            return currentM5 -
                   _lastSignalM5 <
                   CooldownM5Bars;
        }

        // ============================================================
        #endregion

        #region Plan Engine VALIDATION
        // ============================================================

        private double MinimumTakeProfitDistancePrice()
        {
            try
            {
                double distance =
                    Math.Max(
                        0,
                        Symbol.MinTakeProfitDistance);

                if (distance <= 0)
                    return
                        Math.Max(
                            Symbol.TickSize,
                            Symbol.PipSize);

                if (Symbol.MinDistanceType ==
                    SymbolMinDistanceType.Pips)
                    return
                        distance *
                        Math.Max(
                            Symbol.PipSize,
                            Symbol.TickSize);

                return
                    Symbol.Bid *
                    distance /
                    100.0;
            }
            catch
            {
                return
                    Math.Max(
                        Symbol.TickSize,
                        Symbol.PipSize);
            }
        }

        private bool IsValidTarget(
            int direction,
            double entry,
            double target)
        {
            if (!IsFinitePositive(entry) ||
                !IsFinitePositive(target))
                return false;

            double minimumDistance =
                Math.Max(
                    Symbol.TickSize,
                    MinimumTakeProfitDistancePrice());

            return direction == 1
                ? target > entry + minimumDistance
                : direction == -1 &&
                  target < entry - minimumDistance;
        }

        private double MinimumProtectionDistancePrice()
        {
            try
            {
                double distance =
                    Math.Max(
                        0,
                        Symbol.MinStopLossDistance);

                if (distance <= 0)
                    return
                        Math.Max(
                            Symbol.TickSize,
                            Symbol.PipSize);

                if (Symbol.MinDistanceType ==
                    SymbolMinDistanceType.Pips)
                    return
                        distance *
                        Math.Max(
                            Symbol.PipSize,
                            Symbol.TickSize);

                return
                    Symbol.Bid *
                    distance /
                    100.0;
            }
            catch
            {
                return
                    Math.Max(
                        Symbol.TickSize,
                        Symbol.PipSize);
            }
        }

        private bool IsValidStop(
            int direction,
            double entry,
            double stop)
        {
            if (!IsFinitePositive(entry) ||
                !IsFinitePositive(stop))
                return false;

            double minimumDistance =
                Math.Max(
                    Symbol.TickSize,
                    MinimumProtectionDistancePrice());

            return direction == 1
                ? stop < entry - minimumDistance
                : direction == -1 &&
                  stop > entry + minimumDistance;
        }

        private bool HasOpposingZonePathObstacle(
            Bars bars,
            int index,
            int direction,
            double entry,
            double target,
            double atr)
        {
            if (bars == null ||
                index < 8 ||
                atr <= 0 ||
                !IsFinitePositive(entry) ||
                !IsFinitePositive(target))
                return false;

            int first =
                Math.Max(
                    2,
                    index -
                    Math.Max(
                        5,
                        TargetObstacleLookbackBars));

            double clearance =
                atr *
                Math.Max(
                    TargetClearanceAtr,
                    TargetObstacleBufferAtr);

            for (int i = first;
                 i <= index - 1;
                 i++)
            {
                if (direction == 1)
                {
                    double gap =
                        bars.LowPrices[i] -
                        bars.HighPrices[i - 2];

                    if (gap >=
                        atr *
                        MinimumFvgAtr)
                    {
                        double low =
                            bars.HighPrices[i - 2];

                        double high =
                            bars.LowPrices[i];

                        if (ZoneBlocksRewardPath(
                                low,
                                high,
                                entry,
                                target,
                                clearance))
                            return true;
                    }
                }
                else
                {
                    double gap =
                        bars.LowPrices[i - 2] -
                        bars.HighPrices[i];

                    if (gap >=
                        atr *
                        MinimumFvgAtr)
                    {
                        double low =
                            bars.HighPrices[i];

                        double high =
                            bars.LowPrices[i - 2];

                        if (ZoneBlocksRewardPath(
                                low,
                                high,
                                entry,
                                target,
                                clearance))
                            return true;
                    }
                }

                if (UseOrderBlock)
                {
                    Zone oppositeOb =
                        BuildOrderBlockCandidate(
                            bars,
                            i,
                            index,
                            -direction,
                            atr);

                    if (oppositeOb != null &&
                        ZoneBlocksRewardPath(
                            oppositeOb.Low,
                            oppositeOb.High,
                            entry,
                            target,
                            clearance))
                        return true;
                }
            }

            return false;
        }

        private bool HasHigherTfZonePathObstacle(
            DateTime reference,
            int direction,
            double entry,
            double target)
        {
            Bars[] frames =
            {
                _m15Bars,
                _m30Bars,
                _h1Bars,
                _h4Bars
            };

            for (int i = 0;
                 i < frames.Length;
                 i++)
            {
                Bars frame =
                    frames[i];

                if (frame == null)
                    continue;

                int index =
                    ClosedIndex(
                        frame,
                        reference);

                if (index < 8)
                    continue;

                double frameAtr =
                    Atr(
                        frame,
                        index);

                if (frameAtr <= 0)
                    continue;

                if (HasOpposingZonePathObstacle(
                        frame,
                        index,
                        direction,
                        entry,
                        target,
                        frameAtr))
                    return true;
            }

            return false;
        }

        private bool ZoneBlocksRewardPath(
            double low,
            double high,
            double entry,
            double target,
            double clearance)
        {
            if (low >= high)
                return false;

            double pathLow =
                Math.Min(
                    entry,
                    target);

            double pathHigh =
                Math.Max(
                    entry,
                    target);

            if (high <=
                pathLow +
                clearance ||
                low >=
                pathHigh -
                clearance)
                return false;

            bool containsTarget =
                target >=
                    low - clearance &&
                target <=
                    high + clearance;

            if (containsTarget)
                return false;

            bool containsEntry =
                entry >=
                    low - clearance &&
                entry <=
                    high + clearance;

            if (containsEntry)
                return false;

            return
                high >
                pathLow + clearance &&
                low <
                pathHigh - clearance;
        }

        private bool HasTargetObstacle(
            Bars bars,
            int index,
            int direction,
            double entry,
            double target,
            double atr)
        {
            if (bars == null ||
                index < 8 ||
                atr <= 0 ||
                !IsFinitePositive(entry) ||
                !IsFinitePositive(target))
                return false;

            double clearance =
                atr *
                Math.Max(
                    TargetClearanceAtr,
                    TargetObstacleBufferAtr);

            int strength =
                Math.Max(
                    1,
                    Math.Min(
                        SwingStrength,
                        3));

            int start =
                Math.Max(
                    strength + 1,
                    index -
                    Math.Max(
                        5,
                        TargetObstacleLookbackBars));

            int last =
                Math.Max(
                    start,
                    index -
                    strength -
                    1);

            for (int i = start;
                 i <= last;
                 i++)
            {
                bool swing = true;

                for (int j = 1;
                     j <= strength;
                     j++)
                {
                    if (direction == 1)
                    {
                        if (bars.HighPrices[i] <=
                            bars.HighPrices[i - j] ||
                            bars.HighPrices[i] <=
                            bars.HighPrices[i + j])
                        {
                            swing = false;
                            break;
                        }
                    }
                    else
                    {
                        if (bars.LowPrices[i] >=
                            bars.LowPrices[i - j] ||
                            bars.LowPrices[i] >=
                            bars.LowPrices[i + j])
                        {
                            swing = false;
                            break;
                        }
                    }
                }

                if (!swing)
                    continue;

                if (direction == 1)
                {
                    double level =
                        bars.HighPrices[i];

                    if (level > entry &&
                        level < target - clearance)
                        return true;
                }
                else
                {
                    double level =
                        bars.LowPrices[i];

                    if (level < entry &&
                        level > target + clearance)
                        return true;
                }
            }

            if (UseEqualHighLow)
            {
                double liquidityLevel =
                    direction == 1
                        ? FindEqualHigh(
                            bars,
                            index,
                            entry,
                            atr)
                        : FindEqualLow(
                            bars,
                            index,
                            entry,
                            atr);

                if (direction == 1 &&
                    liquidityLevel > entry &&
                    liquidityLevel <
                    target - clearance)
                    return true;

                if (direction == -1 &&
                    liquidityLevel < entry &&
                    liquidityLevel >
                    target + clearance)
                    return true;
            }

            return false;
        }

        private void EnsureSignalPlan(
            int closedM5,
            bool allowUnconfirmedAutoPlan)
        {
            if (_plan != null ||
                _decision == null ||
                _decision.Direction == 0)
                return;

            if (!ShouldCreatePlan(
                    closedM5,
                    allowUnconfirmedAutoPlan))
                return;

            Plan plan =
                BuildPlan(
                    closedM5,
                    _decision.Direction);

            if (plan == null)
            {
                if (AutoTradingEnabled)
                {
                    SetAutoTradingState(
                        "BLOCKED",
                        "PLAN BUILD / STRUCTURAL REWARD GATE");
                }

                return;
            }

            _lastAutoPlanAttemptM5 =
                closedM5;

            ActivatePlan(
                plan);
        }



        private bool ShouldCreatePlan(
            int closedM5,
            bool allowUnconfirmedAutoPlan)
        {
            if (BlockNewSignalWhileActive &&
                _plan != null)
                return false;

            if (_decision == null ||
                _decision.Direction == 0)
                return false;

            if (!_decision.EntryAllowed &&
                !(allowUnconfirmedAutoPlan &&
                  _decision.Confidence >= MinimumAutoConfidence &&
                  _decision.SmartQuality >= MinimumAutoSmartQuality))
                return false;

            if (BlockSameBarReentryAfterExit &&
                _lastExitM5 == closedM5)
                return false;

            if (_lastSignalM5 >= 0 &&
                closedM5 - _lastSignalM5 <
                Math.Max(
                    CooldownBars,
                    Math.Max(
                        CooldownM5Bars,
                        ExitReentryCooldownM5)))
                return false;

            return _lastSignalM5 != closedM5;
        }

        private bool HasAnyHtfTargetLevel(
            List<Level> candidates)
        {
            if (candidates == null)
                return false;

            for (int i = 0;
                 i < candidates.Count;
                 i++)
            {
                string tf =
                    candidates[i].Timeframe;

                if (tf == "H1" ||
                    tf == "H4" ||
                    tf == "D1" ||
                    tf == "W1")
                    return true;
            }

            return false;
        }

        private void GetAdaptiveSmartThresholds(
            string regime,
            out int qualityThreshold,
            out int shareThreshold,
            out int edgeThreshold)
        {
            qualityThreshold =
                Math.Max(
                    40,
                    Math.Min(
                        95,
                        MinimumSmartQuality));

            shareThreshold =
                Math.Max(
                    50,
                    Math.Min(
                        90,
                        MinimumSmartDirectionShare));

            edgeThreshold =
                Math.Max(
                    4,
                    Math.Min(
                        30,
                        MinimumEdge));

            if (!AdaptiveSmartThresholds)
                return;

            int b =
                Math.Max(
                    0,
                    SmartRegimeBuffer);

            switch (
                regime ??
                "UNKNOWN")
            {
                case "TREND":
                case "EXPANSION":
                    qualityThreshold -= b;
                    shareThreshold -= Math.Max(1, b / 3);
                    edgeThreshold -= Math.Max(1, b / 3);
                    break;

                case "REVERSAL":
                    qualityThreshold -= Math.Max(1, b / 2);
                    break;

                case "RANGE":
                    qualityThreshold += Math.Max(1, b / 2);
                    shareThreshold += Math.Max(1, b / 3);
                    edgeThreshold += Math.Max(1, b / 3);
                    break;

                case "COMPRESSION":
                    qualityThreshold += b;
                    shareThreshold += Math.Max(1, b / 2);
                    edgeThreshold += Math.Max(1, b / 2);
                    break;
            }

            qualityThreshold =
                Math.Max(
                    40,
                    Math.Min(
                        95,
                        qualityThreshold));

            shareThreshold =
                Math.Max(
                    50,
                    Math.Min(
                        90,
                        shareThreshold));

            edgeThreshold =
                Math.Max(
                    4,
                    Math.Min(
                        30,
                        edgeThreshold));
        }

        private int SmartMinimumConsensusFloor()
        {
            return Math.Max(
                40,
                SmartConsensusThreshold - 12);
        }

        // ============================================================
        #endregion

        #region Chart Rendering
        // ============================================================

        private void RenderPlan()
        {
            if (_plan == null)
            {
                RemovePlanObjects();
                return;
            }

            // A live/confirmed Plan is authoritative for execution visuals.
            // Prediction and watch/reaction objects must not survive beside it,
            // otherwise identical prices can render as apparently duplicated
            // lines/arrows.
            RemovePredictionObjects();
            ClearWatchObjects();

            if (!ShowLevelLines)
            {
                RemovePlanLine(P + "ENTRY");
                RemovePlanLine(P + "IDEAL_ENTRY");
                RemovePlanLine(P + "TRIGGER");
                RemovePlanLine(P + "SL");
                RemovePlanLine(P + "TP1");
                RemovePlanLine(P + "TP2");
                RemovePlanLine(P + "TP3");
                RemovePlanLine(P + "TP4");
            }
            else
            {
                DrawPlanLine(
                    P + "ENTRY",
                    _plan.Entry,
                    EntryLineColor,
                    ShowEntry);

                bool idealDistinct =
                    IsFinitePositive(_plan.IdealEntry) &&
                    !SamePrice(
                        _plan.IdealEntry,
                        _plan.Entry);

                DrawPlanLine(
                    P + "IDEAL_ENTRY",
                    _plan.IdealEntry,
                    PanelAccentColor,
                    ShowEntry &&
                    idealDistinct);

                bool triggerDistinct =
                    IsFinitePositive(_plan.EntryTrigger) &&
                    !SamePrice(
                        _plan.EntryTrigger,
                        _plan.Entry) &&
                    (!idealDistinct ||
                     !SamePrice(
                         _plan.EntryTrigger,
                         _plan.IdealEntry));

                DrawPlanLine(
                    P + "TRIGGER",
                    _plan.EntryTrigger,
                    TriggerLineColor,
                    ShowTrigger &&
                    triggerDistinct &&
                    !_plan.IsLivePosition);

                double displayStop =
                    GetActiveBrokerStopPrice();

                DrawPlanLine(
                    P + "SL",
                    displayStop,
                    SlLineColor,
                    ShowSL);

                bool tp1Distinct =
                    IsFinitePositive(_plan.Tp1) &&
                    !SamePrice(_plan.Tp1, _plan.Entry) &&
                    !SamePrice(_plan.Tp1, _plan.EntryTrigger);

                DrawPlanLine(
                    P + "TP1",
                    _plan.Tp1,
                    TpLineColor,
                    ShowTP1 &&
                    tp1Distinct);

                bool tp2Distinct =
                    IsFinitePositive(_plan.Tp2) &&
                    (!tp1Distinct ||
                     !SamePrice(_plan.Tp2, _plan.Tp1)) &&
                    !SamePrice(_plan.Tp2, _plan.Entry) &&
                    !SamePrice(_plan.Tp2, _plan.EntryTrigger);

                DrawPlanLine(
                    P + "TP2",
                    _plan.Tp2,
                    Tp2LineColor,
                    ShowTP2 &&
                    tp2Distinct);

                bool tp3Distinct =
                    IsFinitePositive(_plan.Tp3) &&
                    (!tp2Distinct ||
                     !SamePrice(_plan.Tp3, _plan.Tp2)) &&
                    !SamePrice(_plan.Tp3, _plan.Entry) &&
                    !SamePrice(_plan.Tp3, _plan.EntryTrigger);

                DrawPlanLine(
                    P + "TP3",
                    _plan.Tp3,
                    Tp3LineColor,
                    ShowTP3 &&
                    tp3Distinct);

                bool tp4Distinct =
                    IsFinitePositive(_plan.Tp4) &&
                    (!tp3Distinct ||
                     !SamePrice(_plan.Tp4, _plan.Tp3)) &&
                    !SamePrice(_plan.Tp4, _plan.Entry) &&
                    !SamePrice(_plan.Tp4, _plan.EntryTrigger);

                DrawPlanLine(
                    P + "TP4",
                    _plan.Tp4,
                    Tp4LineColor,
                    ShowTP4 &&
                    tp4Distinct);

                double activeBrokerTarget =
                    GetActiveBrokerTargetPrice();

                bool activeBrokerTargetDistinct =
                    _plan.IsLivePosition &&
                    IsFinitePositive(activeBrokerTarget) &&
                    !SamePrice(activeBrokerTarget, _plan.Entry) &&
                    !SamePrice(activeBrokerTarget, _plan.Tp1) &&
                    !SamePrice(activeBrokerTarget, _plan.Tp2) &&
                    !SamePrice(activeBrokerTarget, _plan.Tp3) &&
                    !SamePrice(activeBrokerTarget, _plan.Tp4);

                DrawPlanLine(
                    P + "ACTIVE_TP",
                    activeBrokerTarget,
                    PanelAccentColor,
                    (ShowTP1 ||
                     ShowTP2 ||
                     ShowTP3 ||
                     ShowTP4) &&
                    activeBrokerTargetDistinct);
            }

            if (ShowLevelPriceLabels ||
                ShowSignalLabels)
                RenderPlanLabels();
            else
                RemovePlanLabels();

            if (!ShowSignalArrow ||
                Bars == null ||
                Bars.Count < 2)
            {
                Chart.RemoveObject(
                    P + "ARROW");
                return;
            }

            int hostBar =
                MapM5ToChart(
                    _plan.CreatedM5,
                    Bars.Count - 1);

            double atr =
                Atr(
                    Bars,
                    Math.Max(
                        1,
                        Math.Min(
                            Bars.Count - 1,
                            hostBar)));

            double offset =
                Math.Max(
                    Symbol.PipSize * 2,
                    atr * 0.18);

            double y =
                _plan.Direction == 1
                    ? Bars.LowPrices[hostBar] -
                      offset
                    : Bars.HighPrices[hostBar] +
                      offset;

            DrawIcon(
                P + "ARROW",
                _plan.Direction == 1
                    ? ChartIconType.UpArrow
                    : ChartIconType.DownArrow,
                hostBar,
                y,
                SignalArrowColorFor(
                    _plan.Direction,
                    _plan.IsLivePosition
                        ? "CONFIRMED"
                        : _decision != null &&
                          _decision.SmartQuality >=
                          SmartStrongSetupQuality
                            ? "STRONG"
                            : "CONFIRMED"));
        }

        private void RenderPlanLabels()
        {
            if (_plan == null ||
                (!ShowLevelPriceLabels &&
                 !ShowSignalLabels) ||
                Bars == null ||
                Bars.Count < 2)
                return;

            RemovePlanLabels();

            int bar =
                Math.Max(
                    0,
                    Math.Min(
                        Bars.Count - 1,
                        MapM5ToChart(
                            _plan.CreatedM5,
                            Bars.Count - 1)));

            if (ShowEntry)
            {
                DrawPlanLabel(
                    P + "ENTRY_LABEL",
                    "ENTRY " +
                    Price(
                        _plan.Entry),
                    bar,
                    _plan.Entry,
                    EntryLineColor);
            }

            bool idealDistinct =
                IsFinitePositive(_plan.IdealEntry) &&
                !SamePrice(
                    _plan.IdealEntry,
                    _plan.Entry);

            if (ShowEntry &&
                idealDistinct)
            {
                DrawPlanLabel(
                    P + "IDEAL_ENTRY_LABEL",
                    (_plan.EntryMode ==
                        CFIPClean70ExecutionMode.BreakoutMarket
                        ? "ZONE MID "
                        : "IDEAL ") +
                    Price(
                        _plan.IdealEntry),
                    bar,
                    _plan.IdealEntry,
                    PanelAccentColor);
            }

            bool triggerDistinct =
                IsFinitePositive(_plan.EntryTrigger) &&
                !SamePrice(
                    _plan.EntryTrigger,
                    _plan.Entry) &&
                (!idealDistinct ||
                 !SamePrice(
                     _plan.EntryTrigger,
                     _plan.IdealEntry));

            if (ShowTrigger &&
                triggerDistinct &&
                !_plan.IsLivePosition)
            {
                DrawPlanLabel(
                    P + "TRIGGER_LABEL",
                    "TRIGGER " +
                    Price(
                        _plan.EntryTrigger),
                    bar,
                    _plan.EntryTrigger,
                    TriggerLineColor);
            }

            double displayStop =
                GetActiveBrokerStopPrice();

            if (ShowSL &&
                IsFinitePositive(displayStop))
            {
                DrawPlanLabel(
                    P + "SL_LABEL",
                    "SL " +
                    Price(displayStop),
                    bar,
                    displayStop,
                    SlLineColor);
            }

            bool tp1Distinct =
                IsFinitePositive(_plan.Tp1) &&
                !SamePrice(_plan.Tp1, _plan.Entry) &&
                !SamePrice(_plan.Tp1, _plan.EntryTrigger);

            if (ShowTP1 &&
                tp1Distinct)
            {
                DrawPlanLabel(
                    P + "TP1_LABEL",
                    "TP1 " +
                    Price(
                        _plan.Tp1),
                    bar,
                    _plan.Tp1,
                    TpLineColor);
            }

            bool tp2Distinct =
                IsFinitePositive(_plan.Tp2) &&
                (!tp1Distinct ||
                 !SamePrice(_plan.Tp2, _plan.Tp1)) &&
                !SamePrice(_plan.Tp2, _plan.Entry) &&
                !SamePrice(_plan.Tp2, _plan.EntryTrigger);

            if (ShowTP2 &&
                tp2Distinct)
            {
                DrawPlanLabel(
                    P + "TP2_LABEL",
                    "TP2 " +
                    Price(
                        _plan.Tp2),
                    bar,
                    _plan.Tp2,
                    Tp2LineColor);
            }

            bool tp3Distinct =
                IsFinitePositive(_plan.Tp3) &&
                (!tp2Distinct ||
                 !SamePrice(_plan.Tp3, _plan.Tp2)) &&
                !SamePrice(_plan.Tp3, _plan.Entry) &&
                !SamePrice(_plan.Tp3, _plan.EntryTrigger);

            if (ShowTP3 &&
                tp3Distinct)
            {
                DrawPlanLabel(
                    P + "TP3_LABEL",
                    "TP3 " +
                    Price(
                        _plan.Tp3),
                    bar,
                    _plan.Tp3,
                    Tp3LineColor);
            }

            bool tp4Distinct =
                IsFinitePositive(_plan.Tp4) &&
                (!tp3Distinct ||
                 !SamePrice(_plan.Tp4, _plan.Tp3)) &&
                !SamePrice(_plan.Tp4, _plan.Entry) &&
                !SamePrice(_plan.Tp4, _plan.EntryTrigger);

            if (ShowTP4 &&
                tp4Distinct)
            {
                DrawPlanLabel(
                    P + "TP4_LABEL",
                    "TP4 " +
                    Price(
                        _plan.Tp4),
                    bar,
                    _plan.Tp4,
                    Tp4LineColor);
            }

            double activeBrokerTarget =
                GetActiveBrokerTargetPrice();

            bool activeBrokerTargetDistinct =
                _plan.IsLivePosition &&
                IsFinitePositive(activeBrokerTarget) &&
                !SamePrice(activeBrokerTarget, _plan.Entry) &&
                !SamePrice(activeBrokerTarget, _plan.Tp1) &&
                !SamePrice(activeBrokerTarget, _plan.Tp2) &&
                !SamePrice(activeBrokerTarget, _plan.Tp3) &&
                !SamePrice(activeBrokerTarget, _plan.Tp4);

            if ((ShowTP1 ||
                 ShowTP2 ||
                 ShowTP3 ||
                 ShowTP4) &&
                activeBrokerTargetDistinct)
            {
                DrawPlanLabel(
                    P + "ACTIVE_TP_LABEL",
                    "ACTIVE TP " +
                    Price(activeBrokerTarget),
                    bar,
                    activeBrokerTarget,
                    PanelAccentColor);
            }
        }

        private int GetLabelAnchorBar(
            string name,
            int referenceBar)
        {
            if (Bars == null ||
                Bars.Count < 2)
                return 0;

            bool prediction =
                name != null &&
                name.IndexOf(
                    "PRED_",
                    StringComparison.OrdinalIgnoreCase) >= 0;

            int left;

            if (!prediction &&
                FullWidthLevelLines)
            {
                try
                {
                    left =
                        Chart.FirstVisibleBarIndex;
                }
                catch
                {
                    left =
                        referenceBar -
                        Math.Max(
                            1,
                            LineLengthBars);
                }
            }
            else
            {
                left =
                    referenceBar -
                    Math.Max(
                        1,
                        LineLengthBars);
            }

            left =
                Math.Max(
                    0,
                    Math.Min(
                        Bars.Count - 1,
                        left));

            int offset =
                Math.Max(
                    1,
                    LabelLeftOffsetBars);

            return
                Math.Max(
                    0,
                    Math.Min(
                        Bars.Count - 1,
                        left + offset));
        }

        private void DrawPlanLabel(
            string name,
            string text,
            int bar,
            double price,
            Color color)
        {
            try
            {
                if (!IsFinitePositive(price) ||
                    Bars == null ||
                    Bars.Count < 2)
                    return;

                int labelBar =
                    GetLabelAnchorBar(
                        name,
                        Math.Max(
                            0,
                            Math.Min(
                                Bars.Count - 1,
                                bar)));

                double atr =
                    Bars.Count >= 3
                        ? Atr(
                            Bars,
                            Math.Max(
                                1,
                                Math.Min(
                                    Bars.Count - 2,
                                    labelBar)))
                        : 0;

                double labelOffset =
                    Math.Max(
                        Symbol.PipSize * 2,
                        atr > 0
                            ? atr * 0.06
                            : Symbol.PipSize * 3);

                double labelPrice =
                    NormalizePrice(
                        price +
                        labelOffset);

                ChartText label =
                    Chart.DrawText(
                        name,
                        text,
                        Bars.OpenTimes[labelBar],
                        labelPrice,
                        color);

                label.FontSize =
                    Math.Max(
                        8,
                        PanelFontSize);

                label.FontFamily =
                    string.IsNullOrWhiteSpace(
                        PanelFontFamily)
                        ? "Arial"
                        : PanelFontFamily;

                label.IsBold =
                    PanelBold;

                label.HorizontalAlignment =
                    HorizontalAlignment.Right;

                label.VerticalAlignment =
                    VerticalAlignment.Bottom;

                label.IsInteractive =
                    false;
            }
            catch (Exception ex)
            {
                Print(
                    "CFIP CLEAN70 plan label failed: {0}",
                    ex.Message);
            }
        }

        private void RemovePlanLabels()
        {
            Chart.RemoveObject(
                P + "ENTRY_LABEL");
            Chart.RemoveObject(
                P + "IDEAL_ENTRY_LABEL");
            Chart.RemoveObject(
                P + "TRIGGER_LABEL");
            Chart.RemoveObject(
                P + "SL_LABEL");
            Chart.RemoveObject(
                P + "TP1_LABEL");
            Chart.RemoveObject(
                P + "TP2_LABEL");
            Chart.RemoveObject(
                P + "TP3_LABEL");
            Chart.RemoveObject(
                P + "TP4_LABEL");
            Chart.RemoveObject(
                P + "ACTIVE_TP_LABEL");
        }

        private void DrawPlanLine(
            string name,
            double price,
            Color color,
            bool visible)
        {
            if (!visible ||
                !IsFinitePositive(price) ||
                Bars == null ||
                Bars.Count < 2)
            {
                RemovePlanLine(name);
                return;
            }

            double normalized =
                NormalizePrice(price);

            if (!IsFinitePositive(normalized))
            {
                RemovePlanLine(name);
                return;
            }

            try
            {
                int anchor =
                    _plan != null
                        ? MapM5ToChart(
                            _plan.CreatedM5,
                            Bars.Count - 1)
                        : Bars.Count - 1;

                anchor =
                    Math.Max(
                        0,
                        Math.Min(
                            Bars.Count - 1,
                            anchor));

                int left;
                int right;

                if (FullWidthLevelLines)
                {
                    left =
                        Math.Max(
                            0,
                            Math.Min(
                                Bars.Count - 1,
                                Chart.FirstVisibleBarIndex));

                    right =
                        Math.Max(
                            left,
                            Math.Min(
                                Bars.Count - 1,
                                Chart.LastVisibleBarIndex));

                    if (right <= left)
                    {
                        left = 0;
                        right = Bars.Count - 1;
                    }
                }
                else
                {
                    left =
                        Math.Max(
                            0,
                            anchor -
                            Math.Max(
                                1,
                                LineLengthBars));

                    right =
                        Math.Min(
                            Bars.Count - 1,
                            anchor +
                            Math.Max(
                                1,
                                LineForwardBars));
                }

                if (right <= left)
                {
                    RemovePlanLine(name);
                    return;
                }

                ChartTrendLine line =
                    Chart.FindObject(name)
                    as ChartTrendLine;

                if (line == null)
                {
                    ChartObject existing =
                        Chart.FindObject(name);

                    if (existing != null)
                        Chart.RemoveObject(name);

                    line =
                        Chart.DrawTrendLine(
                            name,
                            left,
                            normalized,
                            right,
                            normalized,
                            color,
                            Math.Max(
                                1,
                                LevelLineThickness),
                            LineStyle.Solid);
                }

                if (line == null)
                    return;

                line.Time1 =
                    Bars.OpenTimes[left];
                line.Y1 =
                    normalized;
                line.Time2 =
                    Bars.OpenTimes[right];
                line.Y2 =
                    normalized;
                line.Color =
                    color;
                line.Thickness =
                    Math.Max(
                        1,
                        LevelLineThickness);
                line.LineStyle =
                    LineStyle.Solid;
                line.ExtendToInfinity =
                    false;
                line.IsInteractive =
                    false;
            }
            catch (Exception ex)
            {
                Print(
                    "CFIP CLEAN70 level render failed [{0}]: {1}",
                    name,
                    ex.Message);
            }
        }

        private void RenderWatchAndReaction(
            int chartIndex,
            int closedM5)
        {
            ClearWatchObjects();

            if (Bars == null ||
                Bars.Count < 2)
                return;

            PendingOrder pendingAuthority =
                GetManagedPendingOrder();

            if (pendingAuthority != null)
            {
                Chart.RemoveObject(P + "WATCH_ARROW");
                Chart.RemoveObject(P + "REACTION_ARROW");
                return;
            }

            bool decisionReady =
                _decision != null &&
                _decision.EntryAllowed &&
                _decision.Direction != 0;

            bool reactionReady =
                EnableLiveReaction &&
                ShowReactionArrow &&
                _reaction != null &&
                _reaction.EntryAllowed &&
                _reaction.Direction != 0;

            int visualDirection =
                GetAuthoritativeDirection();

            if (visualDirection == 0)
            {
                Chart.RemoveObject(P + "WATCH_ARROW");
                Chart.RemoveObject(P + "REACTION_ARROW");
                return;
            }

            int hostBar =
                MapM5ToChart(
                    closedM5,
                    chartIndex);

            hostBar =
                Math.Max(
                    0,
                    Math.Min(
                        Bars.Count - 1,
                        hostBar));

            int reactionBar =
                MapM5ToChart(
                    _m5Bars.Count - 1,
                    chartIndex);

            reactionBar =
                Math.Max(
                    0,
                    Math.Min(
                        Bars.Count - 1,
                        reactionBar));

            int arrowBar =
                reactionReady &&
                visualDirection == _reaction.Direction
                    ? reactionBar
                    : hostBar;

            double atr =
                Atr(
                    Bars,
                    Math.Max(
                        1,
                        Math.Min(
                            Bars.Count - 1,
                            arrowBar)));

            double offset =
                Math.Max(
                    Symbol.PipSize *
                    Math.Max(
                        0.5,
                        MinimumArrowOffsetPips),
                    atr *
                    Math.Max(
                        0.02,
                        ArrowOffsetAtr));

            string arrowState =
                reactionReady &&
                visualDirection == _reaction.Direction
                    ? (_reaction.Confidence >=
                       Math.Max(
                           LiveReactionThreshold,
                           LiveReactionStrongThreshold)
                        ? "STRONG"
                        : "REACTION")
                    : decisionReady
                        ? "CONFIRMED"
                        : "WATCH";

            if (ShowSignalArrow)
            {
                DrawIcon(
                    P + "WATCH_ARROW",
                    visualDirection == 1
                        ? ChartIconType.UpArrow
                        : ChartIconType.DownArrow,
                    arrowBar,
                    visualDirection == 1
                        ? Bars.LowPrices[arrowBar] - offset
                        : Bars.HighPrices[arrowBar] + offset,
                    SignalArrowColorFor(
                        visualDirection,
                        arrowState));
            }
            else
            {
                Chart.RemoveObject(
                    P + "WATCH_ARROW");
            }

            Chart.RemoveObject(
                P + "REACTION_ARROW");

            if (_decision != null &&
                !decisionReady &&
                ShowEarlyWatch &&
                AlertOnEarlyWatch &&
                _decision.Confidence >=
                Math.Max(
                    60,
                    MinimumConfidence - 8) &&
                _decision.Confidence <
                MinimumConfidence &&
                _lastEarlyAlertM5 !=
                closedM5)
            {
                SendUnifiedAlert(
                    "WATCH|" +
                    closedM5 +
                    "|" +
                    _decision.Direction,
                    "CFIP CLEAN70 " +
                    (_decision.Direction == 1
                        ? "BUY"
                        : "SELL") +
                    " WATCH | CONF " +
                    _decision.Confidence +
                    " | SMART " +
                    _decision.SmartQuality +
                    " | " +
                    _decision.Reason,
                    _decision.Direction,
                    false);

                _lastEarlyAlertM5 =
                    closedM5;
            }

            if (reactionReady &&
                AlertOnReaction &&
                AlertOnLiveReaction &&
                _lastReactionAlertBar !=
                _m5Bars.Count - 1)
            {
                SendUnifiedAlert(
                    "REACTION|" +
                    _m5Bars.Count,
                    _reaction.Reason,
                    _reaction.Direction,
                    false);

                _lastReactionAlertBar =
                    _m5Bars.Count - 1;
            }

            _lastVisualDirection =
                visualDirection;
        }

        private int _lastReactionAlertBar = -1;

        private Color SignalArrowColorFor(
            int direction,
            string state)
        {
            if (state == "REACTION")
                return BlockedReactionArrowColor;

            if (state == "WATCH")
                return direction == 1
                    ? CautionBuyArrowColor
                    : CautionSellArrowColor;

            if (state == "CONFIRMED")
                return direction == 1
                    ? ConfirmedBuyArrowColor
                    : ConfirmedSellArrowColor;

            return direction == 1
                ? StrongBuyArrowColor
                : StrongSellArrowColor;
        }

        private void DrawIcon(
            string name,
            ChartIconType type,
            int bar,
            double price,
            Color color)
        {
            try
            {
                if (Bars == null ||
                    Bars.Count == 0 ||
                    !IsFinitePositive(price))
                    return;

                int safeBar =
                    Math.Max(
                        0,
                        Math.Min(
                            bar,
                            Bars.Count - 1));

                Chart.DrawIcon(
                    name,
                    type,
                    safeBar,
                    price,
                    color);
            }
            catch (Exception ex)
            {
                Print(
                    "CFIP CLEAN70 icon render failed: {0}",
                    ex.Message);
            }
        }

        private int MapM5ToChart(
            int m5Index,
            int alternate)
        {
            if (_m5Bars == null ||
                Bars == null ||
                m5Index < 0 ||
                m5Index >= _m5Bars.Count)
                return Math.Max(
                    0,
                    Math.Min(
                        alternate,
                        Bars.Count - 1));

            int mapped =
                Bars.OpenTimes.GetIndexByTime(
                    _m5Bars.OpenTimes[m5Index]);

            return
                mapped >= 0 &&
                mapped < Bars.Count
                    ? mapped
                    : Math.Max(
                        0,
                        Math.Min(
                            alternate,
                            Bars.Count - 1));
        }

        private void ClearWatchObjects()
        {
            Chart.RemoveObject(
                P + "WATCH_ARROW");

            Chart.RemoveObject(
                P + "REACTION_ARROW");

            Chart.RemoveObject(
                P + "BOS_MARKER");

            Chart.RemoveObject(
                P + "MSS_MARKER");

            Chart.RemoveObject(
                P + "SWEEP_MARKER");
        }

        private void ClearPlanObjects()
        {
            ClearWatchObjects();
            RemovePlanLabels();

            RemovePlanLine(
                P + "ENTRY");

            RemovePlanLine(
                P + "IDEAL_ENTRY");

            RemovePlanLine(
                P + "TRIGGER");

            RemovePlanLine(
                P + "SL");

            RemovePlanLine(
                P + "TP1");

            RemovePlanLine(
                P + "TP2");

            RemovePlanLine(
                P + "TP3");

            RemovePlanLine(
                P + "TP4");

            RemovePlanLine(
                P + "ACTIVE_TP");

            Chart.RemoveObject(
                P + "ARROW");
        }

        private bool SamePrice(
            double left,
            double right)
        {
            if (!IsFinitePositive(left) ||
                !IsFinitePositive(right))
                return false;

            double tolerance =
                Math.Max(
                    Symbol.TickSize,
                    Symbol.PipSize * 0.05);

            return Math.Abs(left - right) <=
                   Math.Max(
                       tolerance,
                       Symbol.TickSize * 0.5);
        }

        private void RemovePlanLine(
            string name)
        {
            Chart.RemoveObject(name);
        }

        private void RemovePlanObjects()
        {
            ClearPlanObjects();
            RemovePredictionObjects();
        }

        // ============================================================
        #endregion

        #region Unified Panel
        // ============================================================

        private const int PanelRowCount = 64;
        private const int PanelHeaderHeight = 30;
        private const int QuickExecutionRowHeight = 38;
        private const int QuickExecutionButtonHeight = 30;
        private const int QuickExecutionVerticalMargin = 4;

        private void CreatePanel()
        {
            if (_panel != null)
                return;

            try
            {
                _panelHeaderStack =
                    new StackPanel
                    {
                        Orientation =
                            Orientation.Horizontal,
                        HorizontalAlignment =
                            HorizontalAlignment.Stretch,
                        VerticalAlignment =
                            VerticalAlignment.Top,
                        Height = 30,
                        BackgroundColor =
                            Color.FromArgb(
                                0,
                                Color.Black)
                    };

                _panelHeaderTitle =
                    new TextBlock
                    {
                        Text = "CFIP SMART",
                        HorizontalAlignment =
                            HorizontalAlignment.Stretch,
                        VerticalAlignment =
                            VerticalAlignment.Center,
                        TextAlignment =
                            TextAlignment.Left,
                        TextWrapping =
                            TextWrapping.NoWrap,
                        TextTrimming =
                            TextTrimming.None,
                        FontWeight =
                            FontWeight.Bold,
                        BackgroundColor =
                            Color.FromArgb(
                                0,
                                Color.Black)
                    };

                _panelRowsStack =
                    new StackPanel
                    {
                        Orientation =
                            Orientation.Vertical,
                        HorizontalAlignment =
                            HorizontalAlignment.Stretch,
                        VerticalAlignment =
                            VerticalAlignment.Top,
                        BackgroundColor =
                            Color.FromArgb(
                                0,
                                Color.Black)
                    };

                _panelScroll =
                    new ScrollViewer
                    {
                        HorizontalAlignment =
                            HorizontalAlignment.Stretch,
                        VerticalAlignment =
                            VerticalAlignment.Top,
                        HorizontalScrollBarVisibility =
                            ScrollBarVisibility.Hidden,
                        VerticalScrollBarVisibility =
                            ScrollBarVisibility.Auto,
                        BackgroundColor =
                            Color.FromArgb(
                                0,
                                Color.Black)
                    };

                _panelScroll.Content =
                    _panelRowsStack;

                CreateQuickExecutionControls();

                _panelStack =
                    new StackPanel
                    {
                        Orientation =
                            Orientation.Vertical,
                        HorizontalAlignment =
                            HorizontalAlignment.Stretch,
                        VerticalAlignment =
                            VerticalAlignment.Top,
                        BackgroundColor =
                            Color.FromArgb(
                                0,
                                Color.Black)
                    };

                _buttonStack =
                    new StackPanel
                    {
                        Orientation =
                            Orientation.Horizontal,
                        HorizontalAlignment =
                            HorizontalAlignment.Stretch,
                        VerticalAlignment =
                            VerticalAlignment.Top,
                        BackgroundColor =
                            Color.FromArgb(
                                0,
                                Color.Black)
                    };

                _closeButton =
                    new Button();

                _cancelButton =
                    new Button();

                _closeButton.Click +=
                    args => CloseAllPositions();

                _cancelButton.Click +=
                    args => CancelAllOrders();

                // Moved (per user request): the hide/show toggle now lives at
                // the bottom of the box, to the left of Close/Cancel, instead
                // of the header. It must be added to _buttonStack BEFORE the
                // other two buttons so it renders left-most in this
                // horizontal StackPanel.
                CreatePanelToggleButton();

                if (_panelToggleButton != null)
                    _buttonStack.AddChild(
                        _panelToggleButton);

                _buttonStack.AddChild(
                    _closeButton);

                _buttonStack.AddChild(
                    _cancelButton);

                _panelHeaderStack.AddChild(
                    _panelHeaderTitle);

                CreatePanelRows();

                _panelStack.AddChild(
                    _panelHeaderStack);

                if (_quickExecutionStack != null)
                    _panelStack.AddChild(
                        _quickExecutionStack);

                _panelStack.AddChild(
                    _panelScroll);

                _panelStack.AddChild(
                    _buttonStack);

                _panel =
                    new Border
                    {
                        Child =
                            _panelStack,
                        IsHitTestVisible =
                            true,
                        BackgroundColor =
                            Color.FromArgb(
                                ShowPanelBackground
                                    ? Math.Max(
                                        0,
                                        Math.Min(
                                            255,
                                            PanelBackgroundAlpha))
                                    : 0,
                                PanelBackground),
                        BorderColor =
                            Color.FromArgb(
                                Math.Max(
                                    0,
                                    Math.Min(
                                        255,
                                        PanelBorderAlpha)),
                                PanelBorder),
                        BorderThickness =
                            Math.Max(
                                0,
                                PanelBorderThickness),
                        CornerRadius =
                            Math.Max(
                                0,
                                PanelCornerRadius)
                    };

                Chart.AddControl(
                    _panel);

                CreatePanelRestoreButton();
            }
            catch (Exception ex)
            {
                Print(
                    "CFIP CLEAN70 panel creation failed: {0}",
                    ex.Message);

                _panel = null;
                _panelStack = null;
                _panelHeaderStack = null;
                _panelHeaderTitle = null;
                _panelRowsStack = null;
                _panelScroll = null;
                _panelRows.Clear();
                _buttonStack = null;
                _closeButton = null;
                _cancelButton = null;
                _panelToggleButton = null;
                _panelRestoreButton = null;
            }
        }

        private void CreatePanelRows()
        {
            if (_panelRowsStack == null ||
                _panelRows.Count == PanelRowCount)
                return;

            _panelRows.Clear();

            for (int i = 0;
                 i < PanelRowCount;
                 i++)
            {
                TextBlock row =
                    new TextBlock
                    {
                        Text = "",
                        IsVisible = false,
                        IsHitTestVisible = false,
                        TextWrapping = TextWrapping.Wrap,
                        TextTrimming = TextTrimming.None,
                        TextAlignment = TextAlignment.Left,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        VerticalAlignment = VerticalAlignment.Top
                    };

                _panelRows.Add(
                    row);

                _panelRowsStack.AddChild(
                    row);
            }
        }

        private void CreatePanelToggleButton()
        {
            if (!ShowPanelToggleButton ||
                _panelToggleButton != null)
                return;

            try
            {
                _panelToggleButton =
                    new Button
                    {
                        Text = "−",
                        Width =
                            Math.Max(
                                22,
                                Math.Min(
                                    40,
                                    Math.Min(
                                        PanelToggleWidth,
                                        PanelToggleHeight))),
                        Height =
                            Math.Max(
                                22,
                                Math.Min(
                                    40,
                                    Math.Min(
                                        PanelToggleWidth,
                                        PanelToggleHeight))),
                        HorizontalAlignment =
                            HorizontalAlignment.Left,
                        VerticalAlignment =
                            VerticalAlignment.Center,
                        HorizontalContentAlignment =
                            HorizontalAlignment.Center,
                        VerticalContentAlignment =
                            VerticalAlignment.Center,
                        ForegroundColor =
                            PanelTextColor,
                        FontSize =
                            Math.Max(
                                9,
                                PanelFontSize),
                        FontWeight =
                            FontWeight.Bold,
                        BackgroundColor =
                            Color.FromArgb(
                                100,
                                Color.Black),
                        BorderColor =
                            Color.FromArgb(
                                Math.Max(
                                    0,
                                    Math.Min(
                                        255,
                                        PanelBorderAlpha)),
                                PanelBorder),
                        BorderThickness =
                            Math.Max(
                                0,
                                PanelBorderThickness),
                        CornerRadius =
                            Math.Min(
                                5,
                                Math.Max(
                                    0,
                                    PanelCornerRadius)),
                        Margin =
                            new Thickness(
                                0,
                                1,
                                8,
                                1)
                    };

                _panelToggleButton.Click +=
                    args => TogglePanel();
            }
            catch (Exception ex)
            {
                Print(
                    "CFIP CLEAN70 panel hide button failed: {0}",
                    ex.Message);

                _panelToggleButton = null;
            }
        }

        private void CreatePanelRestoreButton()
        {
            if (_panelRestoreButton != null)
                return;

            try
            {
                _panelRestoreButton =
                    new Button
                    {
                        Text = "+",
                        Width = 26,
                        Height = 26,
                        HorizontalAlignment =
                            HorizontalAlignment.Left,
                        VerticalAlignment =
                            VerticalAlignment.Bottom,
                        HorizontalContentAlignment =
                            HorizontalAlignment.Center,
                        VerticalContentAlignment =
                            VerticalAlignment.Center,
                        ForegroundColor =
                            PanelTextColor,
                        FontSize =
                            Math.Max(
                                9,
                                PanelFontSize),
                        FontWeight =
                            FontWeight.Bold,
                        BackgroundColor =
                            Color.FromArgb(
                                70,
                                Color.Black),
                        BorderColor =
                            Color.FromArgb(
                                Math.Max(
                                    0,
                                    Math.Min(
                                        255,
                                        PanelBorderAlpha)),
                                PanelBorder),
                        BorderThickness =
                            Math.Max(
                                0,
                                PanelBorderThickness),
                        CornerRadius =
                            Math.Min(
                                5,
                                Math.Max(
                                    0,
                                    PanelCornerRadius)),
                        IsVisible = false
                    };

                _panelRestoreButton.Click +=
                    args => TogglePanel();

                Chart.AddControl(
                    _panelRestoreButton);

                SetPanelRestoreAlignment();
            }
            catch (Exception ex)
            {
                Print(
                    "CFIP CLEAN70 panel restore button failed: {0}",
                    ex.Message);

                _panelRestoreButton = null;
            }
        }

        private void SetPanelRestoreAlignment()
        {
            if (_panelRestoreButton == null)
                return;

            switch (PanelPosition)
            {
                case CFIPClean70PanelCorner.TopLeft:
                    _panelRestoreButton.VerticalAlignment =
                        VerticalAlignment.Top;
                    _panelRestoreButton.HorizontalAlignment =
                        HorizontalAlignment.Left;
                    break;

                case CFIPClean70PanelCorner.TopRight:
                    _panelRestoreButton.VerticalAlignment =
                        VerticalAlignment.Top;
                    _panelRestoreButton.HorizontalAlignment =
                        HorizontalAlignment.Right;
                    break;

                case CFIPClean70PanelCorner.BottomRight:
                    _panelRestoreButton.VerticalAlignment =
                        VerticalAlignment.Bottom;
                    _panelRestoreButton.HorizontalAlignment =
                        HorizontalAlignment.Right;
                    break;

                default:
                    _panelRestoreButton.VerticalAlignment =
                        VerticalAlignment.Bottom;
                    _panelRestoreButton.HorizontalAlignment =
                        HorizontalAlignment.Left;
                    break;
            }

            int margin =
                Math.Max(
                    4,
                    PanelMargin);

            _panelRestoreButton.Margin =
                new Thickness(
                    margin,
                    margin,
                    margin,
                    margin);
        }

        private void TogglePanel()
        {
            _panelHidden =
                !_panelHidden;

            if (_panel != null)
                _panel.IsVisible =
                    !_panelHidden;

            if (_panelRestoreButton != null)
                _panelRestoreButton.IsVisible =
                    _panelHidden;
        }

        

        private void ApplyPanelVisualSettings(
            int contentWidth,
            int scrollHeight,
            int maxHeight,
            bool buttons,
            int buttonHeight,
            int buttonGap)
        {
            if (_panel == null ||
                _panelStack == null ||
                _panelHeaderStack == null ||
                _panelHeaderTitle == null ||
                _panelScroll == null ||
                _buttonStack == null)
                return;

            int padding =
                Math.Max(
                    0,
                    PanelPadding);

            int border =
                Math.Max(
                    0,
                    PanelBorderThickness);

            int headerHeight =
                PanelHeaderHeight;

            int quickExecutionHeight =
                QuickExecutionRowHeight;

            int buttonMargin =
                Math.Max(
                    0,
                    ActionButtonMargin);

            int toggleSideForLayout =
                Math.Max(
                    22,
                    Math.Min(
                        40,
                        Math.Min(
                            PanelToggleWidth,
                            PanelToggleHeight)));

            int buttonContentHeight =
                Math.Max(
                    buttonHeight,
                    ShowPanelToggleButton
                        ? toggleSideForLayout
                        : 0);

            int buttonAreaHeight =
                buttons
                    ? buttonContentHeight +
                      buttonMargin * 2
                    : 0;

            int panelHeight =
                headerHeight +
                quickExecutionHeight +
                scrollHeight +
                buttonAreaHeight +
                padding * 2 +
                border * 2;

            panelHeight =
                Math.Max(
                    170,
                    Math.Min(
                        panelHeight,
                        Math.Max(
                            200,
                            maxHeight)));

            int backgroundAlpha =
                ShowPanelBackground
                    ? Math.Max(
                        0,
                        Math.Min(
                            255,
                            PanelBackgroundAlpha))
                    : 0;

            int borderAlpha =
                Math.Max(
                    0,
                    Math.Min(
                        255,
                        PanelBorderAlpha));

            _panel.Width =
                Math.Max(
                    260,
                    PanelWidth);

            _panel.Height =
                panelHeight;

            _panel.MinWidth = 260;
            _panel.MaxWidth = 760;
            _panel.MinHeight = 170;
            _panel.MaxHeight =
                Math.Max(
                    220,
                    maxHeight);

            _panel.Padding =
                padding;

            _panel.Margin =
                Math.Max(
                    0,
                    PanelMargin);

            _panel.BackgroundColor =
                Color.FromArgb(
                    backgroundAlpha,
                    PanelBackground);

            _panel.BorderColor =
                Color.FromArgb(
                    borderAlpha,
                    PanelBorder);

            _panel.BorderThickness =
                border;

            _panel.CornerRadius =
                Math.Max(
                    0,
                    PanelCornerRadius);

            _panelHeaderStack.Width =
                Math.Max(
                    200,
                    contentWidth);

            _panelHeaderStack.Height =
                headerHeight;

            // The toggle button now lives in the bottom button row (moved per
            // user request), not the header, so the title reclaims the full
            // header content width.
            _panelHeaderTitle.Width =
                Math.Max(
                    150,
                    contentWidth -
                    8);

            _panelHeaderTitle.FontFamily =
                string.IsNullOrWhiteSpace(
                    PanelFontFamily)
                    ? "Arial"
                    : PanelFontFamily;

            _panelHeaderTitle.FontSize =
                Math.Max(
                    9,
                    PanelFontSize);

            _panelHeaderTitle.ForegroundColor =
                AutoTradingPanelColor();

            _panelHeaderTitle.Text =
                "CFIP SMART  •  " +
                AutoTradingPanelLine();

            _panelHeaderTitle.LineHeight =
                Math.Max(
                    14,
                    PanelFontSize + 2);

            _panelHeaderStack.BackgroundColor =
                Color.FromArgb(
                    0,
                    Color.Black);

            _panelStack.BackgroundColor =
                Color.FromArgb(
                    0,
                    Color.Black);

            _panelRowsStack.BackgroundColor =
                Color.FromArgb(
                    0,
                    Color.Black);

            _panelScroll.BackgroundColor =
                Color.FromArgb(
                    0,
                    Color.Black);

            _buttonStack.BackgroundColor =
                Color.FromArgb(
                    0,
                    Color.Black);

            _panelRowsStack.Width =
                Math.Max(
                    200,
                    contentWidth);

            _panelScroll.Width =
                Math.Max(
                    200,
                    contentWidth);

            _panelScroll.Height =
                Math.Max(
                    100,
                    scrollHeight);

            _buttonStack.Width =
                Math.Max(
                    200,
                    contentWidth);

            _buttonStack.Height =
                buttonAreaHeight;

            _quickExecutionStack.IsVisible =
                true;

            if (_quickExecutionStack != null)
            {
                int quickGap =
                    Math.Max(
                        2,
                        buttonGap);

                int quickWidth =
                    Math.Max(
                        108,
                        (contentWidth -
                         quickGap) / 2);

                int quickRightMargin =
                    quickGap / 2;

                int quickLeftMargin =
                    quickGap -
                    quickRightMargin;

                _quickExecutionStack.Width =
                    contentWidth;
                _quickExecutionStack.Height =
                    QuickExecutionRowHeight;

                if (_autoTradingQuickToggle != null)
                {
                    _autoTradingQuickToggle.Width =
                        quickWidth;
                    _autoTradingQuickToggle.Height =
                        QuickExecutionButtonHeight;
                    _autoTradingQuickToggle.Margin =
                        new Thickness(
                            0,
                            QuickExecutionVerticalMargin,
                            quickRightMargin,
                            QuickExecutionVerticalMargin);
                }

                if (_automaticOrdersQuickToggle != null)
                {
                    _automaticOrdersQuickToggle.Width =
                        quickWidth;
                    _automaticOrdersQuickToggle.Height =
                        QuickExecutionButtonHeight;
                    _automaticOrdersQuickToggle.Margin =
                        new Thickness(
                            quickLeftMargin,
                            QuickExecutionVerticalMargin,
                            0,
                            QuickExecutionVerticalMargin);
                }
            }

            _buttonStack.IsVisible =
                buttons;

            for (int i = 0;
                 i < _panelRows.Count;
                 i++)
            {
                TextBlock row =
                    _panelRows[i];

                row.Width =
                    Math.Max(
                        190,
                        contentWidth);

                row.FontSize =
                    Math.Max(
                        8,
                        PanelFontSize);

                row.FontFamily =
                    string.IsNullOrWhiteSpace(
                        PanelFontFamily)
                        ? "Arial"
                        : PanelFontFamily;

                row.LineHeight =
                    Math.Max(
                        14,
                        PanelFontSize + 3);

                row.BackgroundColor =
                    Color.FromArgb(
                        0,
                        Color.Black);
            }

            int toggleSide =
                Math.Max(
                    22,
                    Math.Min(
                        40,
                        Math.Min(
                            PanelToggleWidth,
                            PanelToggleHeight)));

            int halfGap =
                Math.Max(
                    0,
                    buttonGap / 2);

            int horizontalFootprint =
                ShowPanelToggleButton
                    ? toggleSide +
                      buttonMargin * 3 +
                      halfGap * 3
                    : buttonMargin * 2 +
                      buttonGap;

            int availableButtonWidth =
                Math.Max(
                    0,
                    contentWidth -
                    horizontalFootprint);

            int eachButtonWidth =
                Math.Max(
                    70,
                    Math.Min(
                        Math.Max(
                            70,
                            ActionButtonWidth),
                        availableButtonWidth / 2));

            bool showSafetyButtons =
                ShowTradeActionButtons ||
                AlwaysShowSafetyButtons;

            if (_closeButton != null)
            {
                _closeButton.IsVisible =
                    showSafetyButtons;

                _closeButton.Width =
                    eachButtonWidth;

                _closeButton.Height =
                    Math.Max(
                        26,
                        buttonHeight);

                _closeButton.Text =
                    eachButtonWidth < 120
                        ? "CLOSE"
                        : "CLOSE POSITIONS";

                _closeButton.ForegroundColor =
                    PanelTextColor;

                _closeButton.FontSize =
                    Math.Max(
                        8,
                        PanelFontSize - 1);

                _closeButton.FontWeight =
                    FontWeight.Bold;

                _closeButton.BackgroundColor =
                    Color.FromArgb(
                        ShowPanelBackground
                            ? 120
                            : 0,
                        SlLineColor);

                _closeButton.BorderColor =
                    SlLineColor;

                _closeButton.BorderThickness =
                    border;

                _closeButton.CornerRadius =
                    Math.Max(
                        0,
                        PanelCornerRadius);

                _closeButton.Margin =
                    new Thickness(
                        buttonMargin,
                        buttonMargin,
                        buttonGap / 2,
                        buttonMargin);

                _closeButton.HorizontalContentAlignment =
                    HorizontalAlignment.Center;

                _closeButton.VerticalContentAlignment =
                    VerticalAlignment.Center;
            }

            if (_cancelButton != null)
            {
                _cancelButton.IsVisible =
                    showSafetyButtons;

                _cancelButton.Width =
                    eachButtonWidth;

                _cancelButton.Height =
                    Math.Max(
                        26,
                        buttonHeight);

                _cancelButton.Text =
                    eachButtonWidth < 120
                        ? "CANCEL"
                        : "CANCEL ORDERS";

                _cancelButton.ForegroundColor =
                    PanelTextColor;

                _cancelButton.FontSize =
                    Math.Max(
                        8,
                        PanelFontSize - 1);

                _cancelButton.FontWeight =
                    FontWeight.Bold;

                _cancelButton.BackgroundColor =
                    Color.FromArgb(
                        ShowPanelBackground
                            ? 82
                            : 0,
                        Color.FromHex("#2A313B"));

                _cancelButton.BorderColor =
                    Color.FromArgb(
                        Math.Max(
                            60,
                            Math.Min(
                                255,
                                PanelBorderAlpha)),
                        PanelBorder);

                _cancelButton.BorderThickness =
                    border;

                _cancelButton.CornerRadius =
                    Math.Max(
                        0,
                        PanelCornerRadius);

                _cancelButton.Margin =
                    new Thickness(
                        buttonGap / 2,
                        buttonMargin,
                        buttonMargin,
                        buttonMargin);

                _cancelButton.HorizontalContentAlignment =
                    HorizontalAlignment.Center;

                _cancelButton.VerticalContentAlignment =
                    VerticalAlignment.Center;
            }

            if (_panelToggleButton != null)
            {
                _panelToggleButton.IsVisible =
                    ShowPanelToggleButton;

                _panelToggleButton.Width =
                    toggleSide;

                _panelToggleButton.Height =
                    toggleSide;

                _panelToggleButton.ForegroundColor =
                    PanelTextColor;

                _panelToggleButton.FontSize =
                    Math.Max(
                        9,
                        PanelFontSize);

                _panelToggleButton.BackgroundColor =
                    Color.FromArgb(
                        ShowPanelBackground
                            ? 70
                            : 0,
                        PanelBackground);

                _panelToggleButton.BorderColor =
                    Color.FromArgb(
                        borderAlpha,
                        PanelBorder);

                _panelToggleButton.BorderThickness =
                    border;

                _panelToggleButton.CornerRadius =
                    Math.Min(
                        5,
                        Math.Max(
                            0,
                            PanelCornerRadius));

                _panelToggleButton.VerticalAlignment =
                    VerticalAlignment.Center;

                _panelToggleButton.HorizontalAlignment =
                    HorizontalAlignment.Left;

                // Sits in the bottom button row now, left of Close/Cancel —
                // match their vertical margin so all three buttons line up.
                _panelToggleButton.Margin =
                    new Thickness(
                        buttonMargin,
                        buttonMargin,
                        buttonGap / 2,
                        buttonMargin);
            }

            if (_panelRestoreButton != null)
            {
                _panelRestoreButton.IsVisible =
                    _panelHidden;

                _panelRestoreButton.Width = 26;
                _panelRestoreButton.Height = 26;
                _panelRestoreButton.ForegroundColor =
                    PanelTextColor;
                _panelRestoreButton.FontSize =
                    Math.Max(
                        9,
                        PanelFontSize);
                _panelRestoreButton.BackgroundColor =
                    Color.FromArgb(
                        ShowPanelBackground
                            ? 70
                            : 0,
                        Color.Black);
                _panelRestoreButton.BorderColor =
                    Color.FromArgb(
                        borderAlpha,
                        PanelBorder);
                _panelRestoreButton.BorderThickness =
                    border;
                _panelRestoreButton.CornerRadius =
                    Math.Min(
                        5,
                        Math.Max(
                            0,
                            PanelCornerRadius));

                SetPanelRestoreAlignment();
            }

            SetPanelAlignment();
        }

                                private int EstimatePanelScrollHeight(
            int contentWidth,
            int maxScrollHeight)
        {
            int fontSize =
                Math.Max(
                    8,
                    PanelFontSize);

            int lineHeight =
                Math.Max(
                    14,
                    fontSize + 3);

            int charsPerLine =
                Math.Max(
                    24,
                    (int)(
                        Math.Max(
                            160,
                            contentWidth) /
                        Math.Max(
                            4.5,
                            fontSize * 0.55)));

            int total = 0;

            for (int i = 0;
                 i < _panelRows.Count;
                 i++)
            {
                TextBlock row =
                    _panelRows[i];

                if (row == null ||
                    !row.IsVisible)
                    continue;

                int length =
                    string.IsNullOrEmpty(
                        row.Text)
                        ? 1
                        : row.Text.Length;

                int lines =
                    Math.Max(
                        1,
                        (int)Math.Ceiling(
                            (double)length /
                            charsPerLine));

                lines =
                    Math.Min(
                        4,
                        lines);

                total +=
                    lines *
                    lineHeight +
                    Math.Max(
                        0,
                        PanelRowPadding) *
                    2 +
                    Math.Max(
                        1,
                        PanelRowGap);
            }

            return
                Math.Max(
                    120,
                    Math.Min(
                        Math.Max(
                            120,
                            maxScrollHeight),
                        total + 4));
        }

        private void RenderPanel()
        {
            if (!ShowUnifiedPanel)
            {
                RemovePanel();
                return;
            }

            if (_panel == null)
                CreatePanel();

            if (_panel == null ||
                _panelStack == null ||
                _panelHeaderStack == null ||
                _panelHeaderTitle == null ||
                _panelRowsStack == null ||
                _panelScroll == null ||
                _buttonStack == null ||
                _panelRows.Count != PanelRowCount)
                return;

            if (_panelToggleButton == null)
                CreatePanelToggleButton();

            if (_panelRestoreButton == null)
                CreatePanelRestoreButton();

            _panel.IsVisible =
                !_panelHidden;

            int padding =
                Math.Max(
                    0,
                    PanelPadding);

            int border =
                Math.Max(
                    0,
                    PanelBorderThickness);

            int effectivePanelWidth =
                Math.Max(
                    260,
                    PanelWidth);

            int contentWidth =
                Math.Max(
                    200,
                    effectivePanelWidth -
                    padding * 2 -
                    border * 2);

            bool showSafetyButtons =
                ShowTradeActionButtons ||
                AlwaysShowSafetyButtons;

            bool showButtonRow =
                showSafetyButtons ||
                ShowPanelToggleButton;

            bool buttons =
                showButtonRow;

            int buttonHeight =
                Math.Max(
                    26,
                    ActionButtonHeight);

            int buttonGap =
                Math.Max(
                    0,
                    PanelButtonGap);

            int buttonMargin =
                Math.Max(
                    0,
                    ActionButtonMargin);

            int toggleSideForLayout =
                Math.Max(
                    22,
                    Math.Min(
                        40,
                        Math.Min(
                            PanelToggleWidth,
                            PanelToggleHeight)));

            int buttonContentHeight =
                Math.Max(
                    buttonHeight,
                    ShowPanelToggleButton
                        ? toggleSideForLayout
                        : 0);

            int buttonAreaHeight =
                showButtonRow
                    ? buttonContentHeight +
                      buttonMargin * 2
                    : 0;

            int configuredMaxHeight =
                Math.Max(
                    240,
                    PanelMaxHeight);

            int availableChartHeight =
                0;

            try
            {
                availableChartHeight =
                    (int)Math.Round(
                        Math.Max(
                            0,
                            Chart.Height -
                            Math.Max(
                                0,
                                PanelMargin) * 2 -
                            8));
            }
            catch
            {
                availableChartHeight = 0;
            }

            int maxHeight =
                availableChartHeight > 0
                    ? Math.Max(
                        180,
                        Math.Min(
                            configuredMaxHeight,
                            availableChartHeight))
                    : configuredMaxHeight;

            int quickExecutionHeight =
                QuickExecutionRowHeight;

            int fixedHeight =
                PanelHeaderHeight +
                quickExecutionHeight +
                buttonAreaHeight +
                padding * 2 +
                border * 2;

            int maximumScrollHeight =
                Math.Max(
                    120,
                    maxHeight -
                    fixedHeight);

            for (int i = 0;
                 i < _panelRows.Count;
                 i++)
            {
                TextBlock row =
                    _panelRows[i];

                row.Margin =
                    new Thickness(
                        Math.Max(
                            0,
                            PanelRowPadding),
                        i == 0
                            ? 0
                            : Math.Max(
                                1,
                                PanelRowGap),
                        Math.Max(
                            0,
                            PanelRowPadding),
                        Math.Max(
                            0,
                            PanelRowPadding));

                row.IsVisible =
                    false;
            }

            RenderPanelRows(
                contentWidth);

            int scrollHeight =
                EstimatePanelScrollHeight(
                    contentWidth,
                    maximumScrollHeight);

            ApplyPanelVisualSettings(
                contentWidth,
                scrollHeight,
                maxHeight,
                buttons,
                buttonHeight,
                buttonGap);

            SyncQuickExecutionControls();
        }

        private string GetAutoTradingPanelState()
        {
            if (!AutoTradingEnabled)
                return "OFF";

            string state =
                string.IsNullOrWhiteSpace(
                    _autoTradingState)
                    ? "ARMED"
                    : _autoTradingState;

            if (string.IsNullOrWhiteSpace(
                    _autoTradingReason))
                return "ON • " + state;

            return
                "ON • " +
                state +
                " • " +
                CompactText(
                    _autoTradingReason,
                    72);
        }

        private Color GetAutoTradingPanelColor()
        {
            if (!AutoTradingEnabled)
                return PanelMutedTextColor;

            if ((EnableAggressiveAutoEntry &&
                 _reaction != null &&
                 _reaction.EntryAllowed) ||
                (_plan != null &&
                 _decision != null &&
                 _decision.EntryAllowed))
                return TpLineColor;

            return PanelAccentColor;
        }

        private string GetAutoProtectionPanelState()
        {
            if (!AutoTradingEnabled)
                return "DISABLED WITH AUTO ENGINE";

            if (!AutoBrokerProtection &&
                !AutoProtectBrokerPositions)
                return "OFF";

            string state = "";

            if (AutoBrokerProtection)
                state = "NEW TRADES";

            if (AutoProtectBrokerPositions)
                state +=
                    string.IsNullOrEmpty(state)
                        ? "MANAGED POSITIONS"
                        : " + MANAGED POSITIONS";

            return state;
        }

        private string PredictionReadinessText()
        {
            if (_decision == null)
                return "NO DECISION";

            if (_decision.EntryAllowed)
                return "ENTRY CONFIRMED";

            if (_reaction != null &&
                _reaction.EntryAllowed)
                return "LIVE REACTION READY";

            if (_prediction != null &&
                _prediction.Direction != 0)
                return
                    (_prediction.Direction == 1 ? "BUY" : "SELL") +
                    " PREDICTED • CONF " +
                    _prediction.Confidence;

            return
                "WAIT • " +
                (string.IsNullOrWhiteSpace(_decision.BlockReason)
                    ? "STRUCTURAL GATE"
                    : _decision.BlockReason);
        }

        private Color PredictionReadinessColor()
        {
            if (_decision != null &&
                _decision.EntryAllowed)
                return TpLineColor;

            if (_reaction != null &&
                _reaction.EntryAllowed)
                return PanelAccentColor;

            return PanelWarningColor;
        }

        private string DailyPivotPanelText()
        {
            if (_d1Bars == null ||
                _d1Bars.Count < 3)
                return "UNAVAILABLE";

            int idx =
                ClosedIndex(
                    _d1Bars,
                    DateTime.UtcNow);

            if (idx <= 0)
                return "UNAVAILABLE";

            int prev = idx - 1;

            double h = _d1Bars.HighPrices[prev];
            double l = _d1Bars.LowPrices[prev];
            double c = _d1Bars.ClosePrices[prev];

            if (h <= l)
                return "INVALID";

            double p = (h + l + c) / 3.0;

            double r1 = 2.0 * p - l;
            double s1 = 2.0 * p - h;
            double r2 = p + (h - l);
            double s2 = p - (h - l);

            return
                "P " + Price(p) +
                "  R1 " + Price(r1) +
                "  S1 " + Price(s1) +
                "  R2 " + Price(r2) +
                "  S2 " + Price(s2);
        }

        private string GetExecutionRelationText(
            ExecutionModel model,
            double entry)
        {
            if (model == null)
                return "NONE";

            if (model.Mode ==
                CFIPClean70ExecutionMode.WaitingForTrigger)
                return
                    model.Direction == 1
                        ? "BUY WAIT • ENTRY MUST REACH TRIGGER ABOVE"
                        : "SELL WAIT • ENTRY MUST REACH TRIGGER BELOW";

            if (!IsFinitePositive(entry))
                return "NOT EXECUTABLE";

            if (model.Mode ==
                CFIPClean70ExecutionMode.BreakoutMarket)
            {
                double tolerance =
                    Math.Max(
                        Symbol.TickSize * 2,
                        Math.Max(
                            Symbol.PipSize * 0.5,
                            (Symbol.Ask - Symbol.Bid) * 2));

                bool acceptable =
                    model.Direction == 1
                        ? entry >= model.Trigger - tolerance
                        : entry <= model.Trigger + tolerance;

                if (!acceptable)
                    return "BREAKOUT NOT CONFIRMED";

                return IsTriggerReached(
                    model.Direction,
                    entry,
                    model.Trigger)
                    ? "BREAKOUT CONFIRMED"
                    : "BREAKOUT • SLIPPAGE ACCEPTED";
            }

            if (model.Mode ==
                CFIPClean70ExecutionMode.RetestMarket)
                return "RETEST INSIDE ZONE";

            return ExecutionModeText(model.Mode);
        }

        private void RenderPanelRows(
            int contentWidth)
        {
            int slot = 0;

            int authoritativeDirection =
                GetAuthoritativeDirection();

            _authoritativeDirection =
                authoritativeDirection;

            _authoritativeState =
                GetAuthoritativeState(
                    authoritativeDirection);

            string stableState =
                GetStablePanelState(
                    _authoritativeState);

            int stateDirection =
                stableState.StartsWith(
                    "BUY",
                    StringComparison.OrdinalIgnoreCase)
                    ? 1
                    : stableState.StartsWith(
                        "SELL",
                        StringComparison.OrdinalIgnoreCase)
                        ? -1
                        : 0;

            AddPanelRow(
                ref slot,
                "CFIP SMART CLEAN70  •  " +
                stableState,
                PanelDirectionColor(
                    stateDirection),
                true,
                contentWidth);

            AddPanelRow(
                ref slot,
                AutoTradingPanelLine(),
                AutoTradingPanelColor(),
                true,
                contentWidth);

            AddPanelRow(
                ref slot,
                "SMART ACTION  •  " +
                _authoritativeState,
                PanelDirectionColor(
                    authoritativeDirection),
                true,
                contentWidth);

            AddPanelRow(
                ref slot,
                "SYNC  " +
                GetSignalSynchronizationText(),
                GetSignalSynchronizationColor(),
                true,
                contentWidth);

            AddPanelRow(
                ref slot,
                SymbolName +
                "  •  " +
                Bars.TimeFrame +
                "  •  " +
                DateTime.UtcNow.ToString(
                    "HH:mm:ss") +
                " UTC",
                PanelMutedTextColor,
                false,
                contentWidth);

            AddPanelRow(
                ref slot,
                GetSessionPanelText(),
                GetSessionPanelColor(),
                true,
                contentWidth);

            AddPanelRow(
                ref slot,
                "SUITABILITY  " +
                _marketSuitabilityScore +
                "/100  •  " +
                _marketSuitabilityState +
                "  •  " +
                CompactText(
                    _marketSuitabilityReason,
                    54),
                _marketSuitabilityScore >=
                    Math.Max(
                        50,
                        MinimumMarketSuitability)
                    ? TpLineColor
                    : PanelWarningColor,
                true,
                contentWidth);

            AddPanelRow(
                ref slot,
                "SMART RISK  " +
                EffectiveAutoRiskPercent().ToString("F2") +
                "%  •  " +
                SuitabilityRiskMultiplier().ToString("F2") +
                "x BASE",
                PanelSecondaryTextColor,
                false,
                contentWidth);

            AddPanelRow(
                ref slot,
                "AUTO EXEC  " +
                (AutoTradingEnabled
                    ? "ON"
                    : "OFF") +
                "  •  " +
                CompactText(
                    _autoExecutionBlockReason,
                    72),
                AutoTradingEnabled &&
                string.Equals(
                    _autoExecutionBlockReason,
                    "READY TO SUBMIT",
                    StringComparison.OrdinalIgnoreCase)
                    ? TpLineColor
                    : PanelSecondaryTextColor,
                false,
                contentWidth);

            AddPanelRow(
                ref slot,
                "EXEC MODE  " +
                (_executionModel == null
                    ? "NONE"
                    : ExecutionModeText(
                        _executionModel.Mode)) +
                "  •  ENTRY " +
                (_executionModel != null &&
                 IsFinitePositive(
                     _executionModel.ActualEntry)
                    ? Price(
                        _executionModel.ActualEntry)
                    : "WAIT") +
                "  •  TRIGGER " +
                (_executionModel != null
                    ? Price(
                        _executionModel.Trigger)
                    : "-"),
                _executionModel != null &&
                _executionModel.Mode ==
                    CFIPClean70ExecutionMode.BreakoutMarket
                    ? EntryLineColor
                    : TriggerLineColor,
                true,
                contentWidth);

            if (_executionModel != null &&
                _executionModel.Direction != 0)
            {
                AddPanelRow(
                    ref slot,
                    "ENTRY RELATION  " +
                    GetExecutionRelationText(
                        _executionModel,
                        _executionModel.ActualEntry),
                    PanelSecondaryTextColor,
                    false,
                    contentWidth);
            }

            AddPanelRow(
                ref slot,
                "AUTO ORDERS  " +
                (AutomaticOrdersEnabled
                    ? "ON"
                    : "OFF") +
                "  •  " +
                CompactText(
                    _autoOrdersBlockReason,
                    72),
                AutomaticOrdersEnabled &&
                string.Equals(
                    _autoOrdersBlockReason,
                    "ORDER PLACED",
                    StringComparison.OrdinalIgnoreCase)
                    ? TpLineColor
                    : PanelSecondaryTextColor,
                false,
                contentWidth);

            if (UseDailyPivots)
            {
                AddPanelRow(
                    ref slot,
                    "PIVOT  " + DailyPivotPanelText(),
                    PanelAccentColor,
                    false,
                    contentWidth);
            }

            bool tradingPermission =
                HasTradingPermission();

            AddPanelRow(
                ref slot,
                "PERMISSION  •  " +
                (tradingPermission
                    ? "TRADING ALLOWED"
                    : "TRADING NOT GRANTED"),
                tradingPermission
                    ? TpLineColor
                    : PanelWarningColor,
                true,
                contentWidth);

            if (ShowSpreadDiagnostics)
            {
                double spreadPips =
                    Math.Max(
                        0,
                        (Symbol.Ask - Symbol.Bid) /
                        Math.Max(
                            Symbol.PipSize,
                            1e-9));

                double spreadAtrRatio = 0;

                if (_m5Frame != null &&
                    _m5Frame.Atr > 0)
                {
                    spreadAtrRatio =
                        (Symbol.Ask - Symbol.Bid) /
                        _m5Frame.Atr;
                }

                AddPanelRow(
                    ref slot,
                    "SPREAD  " +
                    spreadPips.ToString("F1") +
                    " pips  •  ATR " +
                    spreadAtrRatio.ToString("F3"),
                    spreadPips > 0 &&
                    UseSpreadFilter &&
                    _m5Frame != null &&
                    _m5Frame.Atr > 0 &&
                    (Symbol.Ask - Symbol.Bid) /
                    _m5Frame.Atr >
                    MaximumSpreadAtr
                        ? PanelWarningColor
                        : PanelMutedTextColor,
                    false,
                    contentWidth);
            }

            if (ShowEngineStatus)
            {
                AddPanelRow(
                    ref slot,
                    "ENGINE  " +
                    _status,
                    _status.IndexOf(
                        "WAIT",
                        StringComparison.OrdinalIgnoreCase) >= 0
                        ? PanelWarningColor
                        : PanelMutedTextColor,
                    false,
                    contentWidth);
            }

            if (_decision != null)
            {
                int direction =
                    _decision.Direction;

                string decisionState =
                    direction == 1
                        ? "BUY"
                        : direction == -1
                            ? "SELL"
                            : "NEUTRAL";

                AddPanelRow(
                    ref slot,
                    "DECISION  •  " +
                    decisionState +
                    "  •  " +
                    (_decision.EntryAllowed
                        ? "READY"
                        : "WATCH / BLOCKED"),
                    PanelDirectionColor(
                        direction),
                    true,
                    contentWidth);

                AddPanelRow(
                    ref slot,
                    "CONF " +
                    _decision.Confidence +
                    "  •  EDGE " +
                    _decision.Edge +
                    "  •  SMART " +
                    _decision.SmartQuality,
                    PanelDirectionColor(
                        direction),
                    true,
                    contentWidth);

                AddPanelRow(
                    ref slot,
                    "MTF " +
                    _decision.TimeframeAgreement +
                    "  •  EVID " +
                    _decision.IndependentEvidence +
                    "  •  STRUCT " +
                    _decision.StructuralConfirmations,
                    PanelSecondaryTextColor,
                    false,
                    contentWidth);

                AddPanelRow(
                    ref slot,
                    "REGIME " +
                    _decision.Regime +
                    "  •  Q" +
                    _decision.RegimeQuality +
                    "  •  RETEST " +
                    _decision.RetestQuality +
                    "  •  SHARE " +
                    _decision.BuyShare +
                    "/" +
                    _decision.SellShare,
                    PanelSecondaryTextColor,
                    false,
                    contentWidth);

                AddPanelRow(
                    ref slot,
                    "CONFLUENCE  " +
                    ConfluenceText(
                        _m5Frame),
                    PanelAccentColor,
                    false,
                    contentWidth);

                AddPanelRow(
                    ref slot,
                    _decision.TriggerReady
                        ? "TRIGGER  CONFIRMED"
                        : "TRIGGER  WAITING",
                    _decision.TriggerReady
                        ? TriggerLineColor
                        : PanelWarningColor,
                    true,
                    contentWidth);

                if (!string.IsNullOrWhiteSpace(
                        _decision.BlockReason))
                {
                    AddPanelRow(
                        ref slot,
                        "BLOCK  " +
                        _decision.BlockReason,
                        SlLineColor,
                        true,
                        contentWidth);
                }

                if (_prediction != null &&
                    _prediction.Direction != 0 &&
                    _prediction.Confidence >=
                    Math.Max(
                        MinimumEarlyConfidence,
                        EarlySetupConfidence))
                {
                    AddPanelRow(
                        ref slot,
                        "EARLY ANALYSIS  •  " +
                        (_prediction.Direction == 1
                            ? "BUY"
                            : "SELL") +
                        "  •  CONF " +
                        _prediction.Confidence,
                        PanelDirectionColor(
                            _prediction.Direction),
                        true,
                        contentWidth);

                    AddPanelRow(
                        ref slot,
                        "PREDICTION  " +
                        ExecutionModeText(
                            _prediction.Mode) +
                        "  •  ENTRY " +
                        Price(_prediction.Entry) +
                        "  •  TRIGGER " +
                        Price(_prediction.Trigger),
                        PanelDirectionColor(
                            _prediction.Direction),
                        false,
                        contentWidth);

                    AddPanelRow(
                        ref slot,
                        "PRED TARGETS  " +
                        Price(_prediction.Target1) +
                        "  /  " +
                        Price(_prediction.Target2) +
                        "  /  " +
                        Price(_prediction.Target3) +
                        "  /  " +
                        Price(_prediction.Target4),
                        PanelSecondaryTextColor,
                        false,
                        contentWidth);
                }
            }

            AddPanelRow(
                ref slot,
                "READINESS  " +
                PredictionReadinessText(),
                PredictionReadinessColor(),
                true,
                contentWidth);

            if (_executionModel != null &&
                _executionModel.Direction != 0)
            {
                AddPanelRow(
                    ref slot,
                    "ENTRY MODEL  •  " +
                    ExecutionModeText(
                        _executionModel.Mode) +
                    "  •  " +
                    _executionModel.Source +
                    "  •  Q" +
                    _executionModel.Quality,
                    PanelDirectionColor(
                        _executionModel.Direction),
                    true,
                    contentWidth);

                AddPanelRow(
                    ref slot,
                    "IDEAL ENTRY  " +
                    Price(
                        _executionModel.IdealEntry) +
                    "  •  ZONE " +
                    Price(
                        _executionModel.ZoneLow) +
                    " → " +
                    Price(
                        _executionModel.ZoneHigh),
                    PanelSecondaryTextColor,
                    false,
                    contentWidth);

                AddPanelRow(
                    ref slot,
                    "ENTRY TRIGGER  " +
                    Price(
                        _executionModel.Trigger) +
                    "  •  INVALIDATION " +
                    Price(
                        _executionModel.Invalidation),
                    _executionModel.Ready
                        ? TriggerLineColor
                        : PanelWarningColor,
                    false,
                    contentWidth);
            }

            if (_plan != null &&
                ShowTradePlanPanel)
            {
                AddPanelRow(
                    ref slot,
                    "TRADE PLAN  •  " +
                    (_plan.Direction == 1
                        ? "BUY ACTIVE"
                        : "SELL ACTIVE"),
                    PanelDirectionColor(
                        _plan.Direction),
                    true,
                    contentWidth);

                if (ShowLevelPricesInUnifiedPanel &&
                    ShowEntry)
                {
                    AddPanelRow(
                        ref slot,
                        "ENTRY  " +
                        Price(_plan.Entry) +
                        "  •  IDEAL " +
                        Price(_plan.IdealEntry) +
                        "  •  Q" +
                        _plan.EntryQuality +
                        "  •  " +
                        _plan.EntrySource,
                        EntryLineColor,
                        true,
                        contentWidth);
                }

                if (ShowLevelPricesInUnifiedPanel &&
                    ShowSL)
                {
                    AddPanelRow(
                        ref slot,
                        "STOP LOSS  " +
                        Price(_plan.Stop) +
                        "  •  " +
                        _plan.StopSource +
                        "  •  Q" +
                        _plan.StopQuality +
                        "  •  RISK " +
                        (_plan.Risk /
                         Math.Max(
                             Symbol.PipSize,
                             1e-9)).ToString("F1") +
                        "p",
                        SlLineColor,
                        true,
                        contentWidth);
                }

                if (ShowLevelPricesInUnifiedPanel &&
                    ShowTP1)
                {
                    AddPanelRow(
                        ref slot,
                        "TAKE PROFIT 1  " +
                        Price(_plan.Tp1) +
                        "  •  RR " +
                        _plan.Tp1RR.ToString(
                            "F2") +
                        "  •  " +
                        _plan.Tp1Source +
                        "  •  Q" +
                        _plan.Tp1Quality +
                        (_tp1Hit != 0
                            ? "  •  HIT"
                            : ""),
                        TpLineColor,
                        true,
                        contentWidth);
                }

                if (ShowLevelPricesInUnifiedPanel &&
                    ShowTP2 &&
                    _plan.Tp2 > 0)
                {
                    AddPanelRow(
                        ref slot,
                        "TAKE PROFIT 2  " +
                        Price(_plan.Tp2) +
                        "  •  RR " +
                        _plan.Tp2RR.ToString(
                            "F2") +
                        "  •  " +
                        _plan.Tp2Source +
                        "  •  Q" +
                        _plan.Tp2Quality +
                        (_tp2Hit != 0
                            ? "  •  HIT"
                            : ""),
                        Tp2LineColor,
                        true,
                        contentWidth);
                }

                if (ShowLevelPricesInUnifiedPanel &&
                    ShowTP3 &&
                    _plan.Tp3 > 0)
                {
                    AddPanelRow(
                        ref slot,
                        "TAKE PROFIT 3  " +
                        Price(_plan.Tp3) +
                        "  •  RR " +
                        _plan.Tp3RR.ToString(
                            "F2") +
                        "  •  " +
                        _plan.Tp3Source +
                        "  •  Q" +
                        _plan.Tp3Quality +
                        (_tp3Hit != 0
                            ? "  •  HIT"
                            : ""),
                        Tp3LineColor,
                        true,
                        contentWidth);
                }

                if (ShowLevelPricesInUnifiedPanel &&
                    ShowTP4 &&
                    _plan.Tp4 > 0)
                {
                    AddPanelRow(
                        ref slot,
                        "TAKE PROFIT 4  " +
                        Price(_plan.Tp4) +
                        "  •  RR " +
                        _plan.Tp4RR.ToString(
                            "F2") +
                        "  •  " +
                        _plan.Tp4Source +
                        "  •  Q" +
                        _plan.Tp4Quality +
                        (_tp4Hit != 0
                            ? "  •  HIT"
                            : ""),
                        Tp4LineColor,
                        true,
                        contentWidth);
                }

                AddPanelRow(
                    ref slot,
                    "REWARD MODEL  •  HTF TARGETS " +
                    _plan.HtfTargetCount +
                    "  •  MAX RR " +
                    Math.Max(
                        0,
                        MaximumRewardRR).ToString("F2"),
                    PanelAccentColor,
                    false,
                    contentWidth);

                double liveRR =
                    _plan.Risk > 0
                        ? (_plan.Direction == 1
                            ? _lastMarket - _plan.Entry
                            : _plan.Entry - _lastMarket) /
                          _plan.Risk
                        : 0;

                int exitPressure =
                    CalculateSmartExitPressure(
                        _lastMarket,
                        liveRR);

                AddPanelRow(
                    ref slot,
                    "LIVE  RR " +
                    liveRR.ToString(
                        "F2") +
                    "  •  TP HIT " +
                    _tp1Hit +
                    "/" +
                    _tp2Hit +
                    "/" +
                    _tp3Hit +
                    "/" +
                    _tp4Hit,
                    liveRR >= 0
                        ? TpLineColor
                        : SlLineColor,
                    true,
                    contentWidth);

                 Position managedPosition =
                     GetManagedPosition();

                 if (managedPosition != null)
                 {
                     AddPanelRow(
                         ref slot,
                         "POSITION  •  " +
                         (managedPosition.TradeType ==
                              TradeType.Buy
                             ? "BUY"
                             : "SELL") +
                         "  #" +
                         managedPosition.Id +
                         "  •  VOL " +
                         managedPosition.VolumeInUnits.ToString("F0") +
                         "  •  P/L " +
                         managedPosition.NetProfit.ToString("F2") +
                         "  •  SL " +
                         (managedPosition.StopLoss.HasValue
                             ? Price(
                                 managedPosition.StopLoss.Value)
                             : "-") +
                         "  •  TP " +
                         (managedPosition.TakeProfit.HasValue
                             ? Price(
                                 managedPosition.TakeProfit.Value)
                             : "-") +
                         "  •  " +
                         BrokerTargetStageText(
                             managedPosition.TakeProfit.HasValue
                                 ? managedPosition.TakeProfit.Value
                                 : 0),
                         managedPosition.NetProfit >= 0
                             ? TpLineColor
                             : SlLineColor,
                         true,
                         contentWidth);
                 }



                AddPanelRow(
                    ref slot,
                    "SMART EXIT  " +
                    GetSmartExitMode() +
                    "  •  PRESSURE " +
                    exitPressure,
                    exitPressure >=
                        SmartExitPressureThreshold
                        ? SlLineColor
                        : exitPressure >=
                          LiveReactionWatchThreshold
                            ? PanelWarningColor
                            : TpLineColor,
                    true,
                    contentWidth);
            }

            if (_reaction != null &&
                _reaction.Direction != 0)
            {
                AddPanelRow(
                    ref slot,
                    "LIVE REACTION  •  " +
                    (_reaction.Direction == 1
                        ? "BUY"
                        : "SELL") +
                    "  •  Q" +
                    _reaction.Confidence +
                    "  •  EVID " +
                    _reaction.IndependentEvidence +
                    (_reaction.EntryAllowed
                        ? "  •  READY"
                        : "  •  WATCH"),
                    PanelDirectionColor(
                        _reaction.Direction),
                    true,
                    contentWidth);
            }

            AddPanelRow(
                ref slot,
                "MTF ALIGNMENT",
                PanelSectionColor,
                true,
                contentWidth);

            AddPanelRow(
                ref slot,
                "M1   " +
                FrameText(_m1Frame),
                PanelDirectionColor(
                    FrameDirection(_m1Frame)),
                false,
                contentWidth);

            AddPanelRow(
                ref slot,
                "M5   " +
                FrameText(_m5Frame),
                PanelDirectionColor(
                    FrameDirection(_m5Frame)),
                false,
                contentWidth);

            AddPanelRow(
                ref slot,
                "M15  " +
                FrameText(_m15Frame),
                PanelDirectionColor(
                    FrameDirection(_m15Frame)),
                false,
                contentWidth);

            AddPanelRow(
                ref slot,
                "M30  " +
                FrameText(_m30Frame),
                PanelDirectionColor(
                    FrameDirection(_m30Frame)),
                false,
                contentWidth);

            AddPanelRow(
                ref slot,
                "H1   " +
                FrameText(_h1Frame),
                PanelDirectionColor(
                    FrameDirection(_h1Frame)),
                false,
                contentWidth);

            AddPanelRow(
                ref slot,
                "H4   " +
                FrameText(_h4Frame),
                PanelDirectionColor(
                    FrameDirection(_h4Frame)),
                false,
                contentWidth);

            if (SmartWeeklyContext)
            {
                AddPanelRow(
                    ref slot,
                    "D1   " +
                    FrameText(_d1Frame),
                    PanelDirectionColor(
                        FrameDirection(_d1Frame)),
                    false,
                    contentWidth);

                AddPanelRow(
                    ref slot,
                    "W1   " +
                    FrameText(_w1Frame),
                    PanelDirectionColor(
                        FrameDirection(_w1Frame)),
                    false,
                    contentWidth);
            }

            if (ShowOutcomeDiagnostics)
            {
                AddPanelRow(
                    ref slot,
                    "OUTCOME  W" +
                    _wins +
                    "  •  L" +
                    _losses +
                    "  •  CAL " +
                    CalibrationText(),
                    PanelAccentColor,
                    false,
                    contentWidth);
            }

            AddPanelRow(
                ref slot,
                "AUTO TRADING  •  " +
                GetAutoTradingPanelState(),
                GetAutoTradingPanelColor(),
                true,
                contentWidth);

            PendingOrder managedPending =
                GetManagedPendingOrder();

            if (managedPending != null)
            {
                string pendingType =
                    managedPending.OrderType ==
                        PendingOrderType.Stop
                        ? "STOP"
                        : managedPending.OrderType ==
                          PendingOrderType.Limit
                            ? "LIMIT"
                            : "PENDING";

                AddPanelRow(
                    ref slot,
                    "AUTO ORDER  •  " +
                    pendingType +
                    "  •  ENTRY " +
                    Price(
                        managedPending.TargetPrice) +
                    (managedPending.StopLoss.HasValue
                        ? "  •  SL " +
                          Price(
                              managedPending.StopLoss.Value)
                        : "") +
                    (managedPending.TakeProfit.HasValue
                        ? "  •  TP " +
                          Price(
                              managedPending.TakeProfit.Value)
                        : ""),
                    TriggerLineColor,
                    true,
                    contentWidth);
            }



            AddPanelRow(
                ref slot,
                "AUTO CONFIG  •  " +
                (ConfirmedSignalsOnly
                    ? "CONFIRMED ONLY"
                    : "PLAN ELIGIBLE") +
                "  •  " +
                (SizingMode ==
                    CFIPClean70SizingMode.FixedLots
                    ? "FIXED " +
                      FixedLots.ToString("F2") +
                      " LOT"
                    : "RISK " +
                      RiskPercentEquity.ToString("F2") +
                      "%"),
                PanelSecondaryTextColor,
                false,
                contentWidth);

            if (AutoTradingEnabled &&
                (AutoBrokerProtection ||
                 AutoProtectBrokerPositions))
            {
                AddPanelRow(
                    ref slot,
                    "AUTO PROTECTION  •  " +
                    GetAutoProtectionPanelState(),
                    TpLineColor,
                    false,
                    contentWidth);
            }

            if (!string.IsNullOrWhiteSpace(
                    _lastAlertMessage))
            {
                AddPanelRow(
                    ref slot,
                    "LAST ALERT  •  " +
                    CompactText(
                        _lastAlertMessage,
                        120),
                    _lastAlertCritical
                        ? (_lastAlertDirection == 1
                            ? BuyArrowColor
                            : _lastAlertDirection == -1
                                ? SellArrowColor
                                : PanelWarningColor)
                        : PanelSecondaryTextColor,
                    false,
                    contentWidth);
            }

            while (slot < _panelRows.Count)
            {
                _panelRows[slot].IsVisible =
                    false;
                slot++;
            }
        }

        private void SetPanelRow(
            int index,
            string text,
            Color color,
            bool bold,
            int width)
        {
            if (index < 0 ||
                index >= _panelRows.Count)
                return;

            TextBlock row =
                _panelRows[index];

            row.Text =
                text ?? "";

            row.Width =
                Math.Max(
                    190,
                    width);

            row.ForegroundColor =
                color;

            row.TextAlignment =
                TextAlignment.Left;

            row.TextWrapping =
                TextWrapping.Wrap;

            row.TextTrimming =
                TextTrimming.None;

            row.LineHeight =
                Math.Max(
                    14,
                    PanelFontSize + 3);

            row.FontWeight =
                bold || PanelBold
                    ? FontWeight.Bold
                    : FontWeight.Normal;

            row.IsVisible =
                !string.IsNullOrWhiteSpace(
                    text);
        }

        private void AddPanelRow(
            ref int slot,
            string text,
            Color color,
            bool bold,
            int width)
        {
            if (slot >= _panelRows.Count)
                return;

            SetPanelRow(
                slot,
                text,
                color,
                bold,
                width);

            slot++;
        }

        private Color PanelDirectionColor(
            int direction)
        {
            if (direction == 1)
                return BuyArrowColor;

            if (direction == -1)
                return SellArrowColor;

            return PanelTextColor;
        }

        private int GetAuthoritativeDirection()
        {
            if (!UseAuthoritativeSignalState)
                return _decision != null
                    ? _decision.Direction
                    : 0;

            if (_plan != null &&
                _plan.IsLivePosition &&
                (_plan.Direction == 1 ||
                 _plan.Direction == -1))
                return _plan.Direction;

            if (_reaction != null &&
                _reaction.EntryAllowed &&
                _reaction.Confidence >=
                Math.Max(
                    50,
                    LiveReactionThreshold) &&
                (_reaction.Direction == 1 ||
                 _reaction.Direction == -1))
                return _reaction.Direction;

            if (_m5Frame != null &&
                (_m5Frame.Direction == 1 ||
                 _m5Frame.Direction == -1) &&
                _m5Frame.Quality >=
                Math.Max(
                    50,
                    LiveReactionThreshold) &&
                ((_m5Frame.Direction == 1 &&
                  (_m5Frame.MssBull ||
                   _m5Frame.ChochBull)) ||
                 (_m5Frame.Direction == -1 &&
                  (_m5Frame.MssBear ||
                   _m5Frame.ChochBear))))
                return _m5Frame.Direction;

            if (_decision != null &&
                (_decision.Direction == 1 ||
                 _decision.Direction == -1))
                return _decision.Direction;

            if (_prediction != null &&
                _prediction.Direction != 0 &&
                _prediction.Confidence >=
                Math.Max(
                    MinimumEarlyConfidence,
                    EarlySetupConfidence))
                return _prediction.Direction;

            return 0;
        }

        private string GetAuthoritativeState(
            int direction)
        {
            if (_plan != null &&
                _plan.IsLivePosition)
                return direction == 1
                    ? "BUY ACTIVE"
                    : direction == -1
                        ? "SELL ACTIVE"
                        : "ACTIVE";

            if (_reaction != null &&
                _reaction.EntryAllowed &&
                _reaction.Confidence >=
                Math.Max(
                    LiveReactionThreshold,
                    LiveReactionStrongThreshold))
                return direction == 1
                    ? "BUY REACTION"
                    : direction == -1
                        ? "SELL REACTION"
                        : "REACTION";

            if (_decision != null &&
                _decision.EntryAllowed)
                return direction == 1
                    ? "BUY READY"
                    : direction == -1
                        ? "SELL READY"
                        : "READY";

            if (_prediction != null &&
                _prediction.Direction != 0)
                return direction == 1
                    ? "BUY PREDICTION"
                    : direction == -1
                        ? "SELL PREDICTION"
                        : "PREDICTION";

            return direction == 1
                ? "BUY WATCH"
                : direction == -1
                    ? "SELL WATCH"
                    : "WAITING";
        }

        private string GetSignalSynchronizationText()
        {
            int direction =
                GetAuthoritativeDirection();

            bool planAligned =
                _plan == null ||
                direction == 0 ||
                _plan.Direction == direction;

            bool decisionAligned =
                _decision == null ||
                _decision.Direction == 0 ||
                direction == 0 ||
                _decision.Direction == direction;

            bool reactionAligned =
                _reaction == null ||
                _reaction.Direction == 0 ||
                direction == 0 ||
                !_reaction.EntryAllowed ||
                _reaction.Direction == direction;

            bool aligned =
                planAligned &&
                decisionAligned &&
                reactionAligned;

            string visualState =
                _plan != null
                    ? "PLAN+ARROW+LEVELS"
                    : _decision != null &&
                      _decision.EntryAllowed
                        ? "DECISION+ARROW"
                        : _prediction != null &&
                          _prediction.Direction != 0
                            ? "PREDICTION"
                            : "WAIT";

            return
                "STATE " +
                (direction == 1
                    ? "BUY"
                    : direction == -1
                        ? "SELL"
                        : "WAIT") +
                " | " +
                (aligned
                    ? "ALIGNED"
                    : "CHECK") +
                " | " +
                visualState;
        }

        private Color GetSignalSynchronizationColor()
        {
            string text =
                GetSignalSynchronizationText();

            return text.IndexOf(
                       "ALIGNED",
                       StringComparison.OrdinalIgnoreCase) >= 0
                ? TpLineColor
                : PanelWarningColor;
        }

        private int FrameDirection(
            Frame frame)
        {
            return frame == null
                ? 0
                : frame.Direction;
        }

        private void SetPanelAlignment()
        {
            VerticalAlignment vertical;
            HorizontalAlignment horizontal;

            switch (PanelPosition)
            {
                case CFIPClean70PanelCorner.TopLeft:
                    vertical = VerticalAlignment.Top;
                    horizontal = HorizontalAlignment.Left;
                    break;

                case CFIPClean70PanelCorner.TopRight:
                    vertical = VerticalAlignment.Top;
                    horizontal = HorizontalAlignment.Right;
                    break;

                case CFIPClean70PanelCorner.BottomRight:
                    vertical = VerticalAlignment.Bottom;
                    horizontal = HorizontalAlignment.Right;
                    break;

                default:
                    vertical = VerticalAlignment.Bottom;
                    horizontal = HorizontalAlignment.Left;
                    break;
            }

            if (_panel != null)
            {
                _panel.VerticalAlignment =
                    vertical;

                _panel.HorizontalAlignment =
                    horizontal;
            }
        }

        private string GetStablePanelState(
            string candidate)
        {
            if (string.IsNullOrWhiteSpace(
                    candidate))
                candidate = "WAITING";

            DateTime now =
                DateTime.UtcNow;

            bool authoritative =
                _plan != null ||
                candidate.IndexOf(
                    "ACTIVE",
                    StringComparison.OrdinalIgnoreCase) >= 0;

            if (authoritative ||
                PanelStateHoldSeconds <= 0 ||
                string.IsNullOrWhiteSpace(
                    _panelStableHeader))
            {
                _panelStableHeader =
                    candidate;

                _panelStableHeaderSinceUtc =
                    now;

                return _panelStableHeader;
            }

            double heldSeconds =
                (now -
                 _panelStableHeaderSinceUtc)
                .TotalSeconds;

            if (!string.Equals(
                    _panelStableHeader,
                    candidate,
                    StringComparison.OrdinalIgnoreCase) &&
                heldSeconds >=
                Math.Max(
                    0,
                    PanelStateHoldSeconds))
            {
                _panelStableHeader =
                    candidate;

                _panelStableHeaderSinceUtc =
                    now;
            }

            return _panelStableHeader;
        }

        private string ConfluenceText(
            Frame frame)
        {
            if (frame == null)
                return "WAIT";

            List<string> parts =
                new List<string>();

            if (UseVolumeExpansion)
                parts.Add(
                    frame.VolumeBull
                        ? "VOL+"
                        : frame.VolumeBear
                            ? "VOL-"
                            : "VOL0");

            if (UseMacdBias)
                parts.Add(
                    frame.MacdBull
                        ? "MACD+"
                        : frame.MacdBear
                            ? "MACD-"
                            : "MACD0");

            if (UseVwapBias)
                parts.Add(
                    frame.VwapBull
                        ? "VWAP+"
                        : frame.VwapBear
                            ? "VWAP-"
                            : "VWAP0");

            if (UseHealthyVolatility)
                parts.Add(
                    frame.VolatilityBull
                        ? "ATR+"
                        : frame.VolatilityBear
                            ? "ATR-"
                            : "ATR0");

            return parts.Count == 0
                ? "OFF"
                : string.Join(
                    " | ",
                    parts.ToArray());
        }

        private string FrameText(
            Frame frame)
        {
            if (frame == null)
                return "WAIT";

            return
                (frame.Direction == 1
                    ? "BUY"
                    : frame.Direction == -1
                        ? "SELL"
                        : "NEUTRAL") +
                " | Q" +
                frame.Quality +
                " | E" +
                frame.Evidence;
        }

        private void RemovePanel()
        {
            if (_panel != null)
            {
                try
                {
                    Chart.RemoveControl(
                        _panel);
                }
                catch
                {
                }
            }

            if (_panelRestoreButton != null)
            {
                try
                {
                    Chart.RemoveControl(
                        _panelRestoreButton);
                }
                catch
                {
                }
            }

            _panel = null;
            _panelStack = null;
            _panelHeaderStack = null;
            _panelHeaderTitle = null;
            _panelRowsStack = null;
            _panelScroll = null;
            _quickExecutionStack = null;
            _autoTradingQuickToggle = null;
            _automaticOrdersQuickToggle = null;
            _panelRows.Clear();
            _buttonStack = null;
            _closeButton = null;
            _cancelButton = null;
            _panelToggleButton = null;
            _panelRestoreButton = null;
            _panelHidden = false;
            _lastReactionAlertBar = -1;
            _panelStableHeader = "";
            _panelStableHeaderSinceUtc =
                DateTime.MinValue;
        }

        // ============================================================
        #endregion

        #region Popup Presentation
        // ============================================================

        private void ShowPopup(
            string message)
        {
            if (!ShowPopupAlerts)
                return;

            if (_popup == null)
            {
                try
                {
                    _popupText =
                        new TextBlock
                        {
                            Text = "",
                            TextWrapping = TextWrapping.Wrap,
                            TextAlignment = TextAlignment.Left,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            VerticalAlignment = VerticalAlignment.Top,
                            Margin = 0
                        };

                    StackPanel popupStack =
                        new StackPanel
                        {
                            Orientation =
                                Orientation.Vertical
                        };

                    popupStack.AddChild(
                        _popupText);

                    _popupCloseButton =
                        new Button();

                    _popupCloseButton.Text =
                        "CLOSE";

                    _popupCloseButton.Width = 70;
                    _popupCloseButton.Height = 22;
                    _popupCloseButton.HorizontalAlignment =
                        HorizontalAlignment.Right;

                    _popupCloseButton.Click +=
                        args => RemovePopup();

                    popupStack.AddChild(
                        _popupCloseButton);

                    _popup =
                        new Border
                        {
                            Child = popupStack,
                            IsHitTestVisible = true
                        };

                    Chart.AddControl(
                        _popup);
                }
                catch (Exception ex)
                {
                    Print(
                        "CFIP CLEAN70 popup creation failed: {0}",
                        ex.Message);

                    _popup = null;
                    _popupText = null;
                    return;
                }
            }

            if (_popupText == null)
                return;

            _popupText.Text =
                message;

            _popupText.FontSize =
                Math.Max(
                    8,
                    Math.Min(
                        22,
                        PopupFontSize));

            _popupText.FontWeight =
                PopupBold
                    ? FontWeight.Bold
                    : FontWeight.Normal;

            _popupText.FontFamily =
                string.IsNullOrWhiteSpace(
                    PopupFontFamily)
                    ? "Arial"
                    : PopupFontFamily;

            _popupText.ForegroundColor =
                PopupTextColor;

            _popup.Width =
                Math.Max(
                    220,
                    PopupWidth);

            _popup.Padding =
                Math.Max(
                    0,
                    PopupPadding);

            _popup.Margin =
                Math.Max(
                    0,
                    PopupMargin);

            _popup.BackgroundColor =
                Color.FromArgb(
                    Math.Max(
                        0,
                        Math.Min(
                            255,
                            PopupBackgroundAlpha)),
                    PopupBackgroundColor);

            _popup.BorderColor =
                Color.FromArgb(
                    Math.Max(
                        0,
                        Math.Min(
                            255,
                            PopupBorderAlpha)),
                    PopupBorderColor);

            _popup.BorderThickness =
                Math.Max(
                    0,
                    PopupBorderThickness);

            _popup.CornerRadius =
                Math.Max(
                    0,
                    PopupCornerRadius);

            switch (PopupPosition)
            {
                case CFIPClean70PanelCorner.TopLeft:
                    _popup.VerticalAlignment =
                        VerticalAlignment.Top;
                    _popup.HorizontalAlignment =
                        HorizontalAlignment.Left;
                    break;

                case CFIPClean70PanelCorner.BottomLeft:
                    _popup.VerticalAlignment =
                        VerticalAlignment.Bottom;
                    _popup.HorizontalAlignment =
                        HorizontalAlignment.Left;
                    break;

                case CFIPClean70PanelCorner.BottomRight:
                    _popup.VerticalAlignment =
                        VerticalAlignment.Bottom;
                    _popup.HorizontalAlignment =
                        HorizontalAlignment.Right;
                    break;

                default:
                    _popup.VerticalAlignment =
                        VerticalAlignment.Top;
                    _popup.HorizontalAlignment =
                        HorizontalAlignment.Right;
                    break;
            }

            if (_popupCloseButton != null)
                _popupCloseButton.IsVisible =
                    ShowPopupCloseButton;

            _popupUntilUtc =
                DateTime.UtcNow.AddSeconds(
                    Math.Max(
                        1,
                        PopupDurationSeconds));
        }

        private void RemoveExpiredPopup()
        {
            if (_popup == null)
                return;

            if (KeepPopupUntilNextAlert)
                return;

            if (_popupUntilUtc >
                DateTime.UtcNow)
                return;

            RemovePopup();
        }

        private void RemovePopup()
        {
            if (_popup != null)
            {
                try
                {
                    Chart.RemoveControl(
                        _popup);
                }
                catch
                {
                }
            }

            _popup = null;
            _popupText = null;
            _popupCloseButton = null;
            _popupUntilUtc =
                DateTime.MinValue;
        }

        // ============================================================
        #endregion

        #region Alert Engine
        // ============================================================

        private void SendUnifiedAlert(
            string key,
            string message,
            int direction,
            bool critical)
        {
            if (string.IsNullOrWhiteSpace(
                    message))
                return;

            DateTime now =
                DateTime.UtcNow;

            if (direction == 0)
                direction =
                    GetAuthoritativeDirection();

            bool restrictionAlert =
                key.StartsWith(
                    "RESTRICT|",
                    StringComparison.OrdinalIgnoreCase);

            if (restrictionAlert &&
                !string.IsNullOrWhiteSpace(
                    _lastRestrictionMessage) &&
                message.IndexOf(
                    _lastRestrictionMessage,
                    StringComparison.OrdinalIgnoreCase) >= 0)
                return;

            if (SuppressDuplicateAlerts)
            {
                int cooldownSeconds =
                    (key.StartsWith(
                        "SMART|",
                        StringComparison.OrdinalIgnoreCase) ||
                     key.StartsWith(
                        "REACTION|",
                        StringComparison.OrdinalIgnoreCase))
                        ? SmartAlertCooldownSeconds
                        : AlertCooldownSeconds;

                if (_alertCooldowns.TryGetValue(
                        key,
                        out DateTime lastSent) &&
                    (now - lastSent).TotalSeconds <
                    Math.Max(
                        1,
                        cooldownSeconds))
                    return;
            }


            _alertCooldowns[key] = now;

            // Light housekeeping so this dictionary can't grow forever over
            // a long-running session — unique keys (per-plan TP/SL, daily
            // markers) accumulate over days/weeks otherwise.
            if (_alertCooldowns.Count > 500)
            {
                List<string> stale =
                    new List<string>();

                foreach (KeyValuePair<string, DateTime> entry in
                         _alertCooldowns)
                {
                    if ((now - entry.Value).TotalHours > 24)
                        stale.Add(entry.Key);
                }

                foreach (string staleKey in stale)
                    _alertCooldowns.Remove(staleKey);
            }


            _lastAlertMessage =
                message;

            _lastAlertDirection =
                direction;

            _lastAlertCritical =
                critical;

            _lastAlertUtc =
                now;

            if (EnableSoundAlerts)
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(
                            SoundFilePath))
                    {
                        Notifications.PlaySound(
                            SoundFilePath);
                    }
                    else
                    {
                        Notifications.PlaySound(
                            ResolveAlertSoundType(
                                key,
                                critical));
                    }
                }
                catch (Exception ex)
                {
                    Print(
                        "CFIP CLEAN70 sound alert failed: {0}",
                        ex.Message);
                }
            }

            if (EnableEmailAlerts &&
                !string.IsNullOrWhiteSpace(
                    SenderEmail) &&
                !string.IsNullOrWhiteSpace(
                    ReceiverEmail))
            {
                try
                {
                    Notifications.SendEmail(
                        SenderEmail,
                        ReceiverEmail,
                        "CFIP SMART CLEAN70 " +
                        SymbolName,
                        message);
                }
                catch (Exception ex)
                {
                    Print(
                        "CFIP CLEAN70 email failed: {0}",
                        ex.Message);
                }
            }

            bool restrictionPopup =
                restrictionAlert &&
                ShowEntryRestrictionPopup;

            if (ShowPopupAlerts &&
                (restrictionPopup ||
                 (!restrictionAlert &&
                  (!PopupCriticalOnly ||
                   critical))))
            {
                ShowPopup(
                    message);
            }
        }

        private void CheckEndOfDayAlert(
            DateTime nowUtc)
        {
            DateTime dayClose =
                new DateTime(
                    nowUtc.Year,
                    nowUtc.Month,
                    nowUtc.Day,
                    ClampInt(
                        SessionEndUtc,
                        0,
                        23),
                    0,
                    0,
                    DateTimeKind.Utc);

            if (EnableEndOfDayAlert)
            {
                DateTime warnStart =
                    dayClose.AddMinutes(
                        -Math.Max(
                            5,
                            EndOfDayAlertMinutesBefore));

                if (nowUtc >= warnStart &&
                    nowUtc <= dayClose &&
                    _lastEndOfDayAlertDate.Date !=
                    nowUtc.Date &&
                    HasManagedOpenPosition())
                {
                    _lastEndOfDayAlertDate =
                        nowUtc.Date;

                    int minutesLeft =
                        Math.Max(
                            0,
                            (int)Math.Round(
                                (dayClose -
                                 nowUtc).TotalMinutes));

                    SendUnifiedAlert(
                        "DAYEND|" +
                        nowUtc.Date.ToString(
                            "yyyyMMdd"),
                        "Day-trading session closes in ~" +
                        minutesLeft +
                        " min (" +
                        ClampInt(
                            SessionEndUtc,
                            0,
                            23).ToString("00") +
                        ":00 UTC) - " +
                        (EnableEndOfDayAutoClose
                            ? "positions will be closed automatically at " +
                              "the session close."
                            : "review/close open positions manually. " +
                              "This indicator never closes trades " +
                              "automatically."),
                        0,
                        true);
                }
            }

            // ENHANCEMENT: optional actual auto-close at the day's close,
            // separate from the warning above and defaulting ON per
            // request. Fires once per day, only once we've reached the
            // configured close time, and only if a managed position is
            // still open at that point.
            if (EnableEndOfDayAutoClose &&
                nowUtc >= dayClose &&
                _lastEndOfDayCloseDate.Date !=
                nowUtc.Date &&
                HasManagedOpenPosition())
            {
                _lastEndOfDayCloseDate =
                    nowUtc.Date;

                CloseAllPositions();

                SendUnifiedAlert(
                    "DAYEND-CLOSED|" +
                    nowUtc.Date.ToString(
                        "yyyyMMdd"),
                    "CFIP closed all managed positions at day-trading " +
                    "session end (" +
                    ClampInt(
                        SessionEndUtc,
                        0,
                        23).ToString("00") +
                    ":00 UTC).",
                    0,
                    true);
            }
        }

        private bool HasManagedOpenPosition()
        {
            foreach (Position position in Positions)
            {
                if (position == null ||
                    position.SymbolName !=
                    SymbolName)
                    continue;

                if (!IsManagedPosition(position))
                    continue;

                return true;
            }

            return false;
        }

                private void CheckReversalProtection()
        {
            if (!EnableReversalProtectionClose ||
                _decision == null ||
                _decision.Direction == 0 ||
                (_plan != null &&
                 _lifecycleState !=
                    CFIPClean70LifecycleState.LivePosition))
                return;

            foreach (Position position in Positions)
            {
                if (position == null ||
                    position.SymbolName != SymbolName)
                    continue;

                if (!IsManagedPosition(position))
                    continue;

                int positionDirection =
                    position.TradeType == TradeType.Buy ? 1 : -1;

                if (_decision.Direction == positionDirection ||
                    _decision.SmartQuality < ReversalProtectionMinimumQuality ||
                    _decision.IndependentEvidence < Math.Max(2, ReversalCloseMinimumEvidence) ||
                    _decision.TimeframeAgreement < Math.Max(50, ReversalCloseMinimumMtf))
                    continue;

                if (!IsDecisiveOppositeDirection(positionDirection) ||
                    position.NetProfit <= 0)
                    continue;

                double protectedProfit =
                    position.NetProfit;

                if (_plan != null &&
                    _plan.IsLivePosition)
                {
                    SetLifecycleState(
                        CFIPClean70LifecycleState.ExitRequested,
                        "REVERSAL PROTECTION");
                }

                if (!TryClosePosition(
                        position,
                        "REVERSAL PROTECTION"))
                {
                    if (_plan != null &&
                        _plan.IsLivePosition)
                    {
                        SetLifecycleState(
                            CFIPClean70LifecycleState.RecoveryRequired,
                            "REVERSAL PROTECTION • EXIT REJECTED");
                    }

                    continue;
                }

                SendUnifiedAlert(
                    "REVERSAL-CLOSE|" +
                    position.Id,
                    "CFIP CLEAN70 REVERSAL EXIT REQUESTED | #" +
                    position.Id +
                    " | protected +" +
                    protectedProfit.ToString("F2") +
                    " | Q " +
                    _decision.SmartQuality +
                    " | MTF " +
                    _decision.TimeframeAgreement +
                    " | EVID " +
                    _decision.IndependentEvidence,
                    positionDirection,
                    true);
            }
        }


        private bool IsDecisiveOppositeDirection(int positionDirection)
        {
            if (_m5Frame == null ||
                _m5Bars == null ||
                positionDirection == 0)
                return false;

            int opposite = positionDirection * -1;

            bool m5Structural =
                opposite == 1
                    ? (_m5Frame.MssBull || _m5Frame.ChochBull)
                    : (_m5Frame.MssBear || _m5Frame.ChochBear);

            bool m5Force =
                opposite == 1
                    ? (_m5Frame.DisplacementBull &&
                       _m5Frame.LiquidityBull)
                    : (_m5Frame.DisplacementBear &&
                       _m5Frame.LiquidityBear);

            bool m15Aligned =
                _m15Frame != null &&
                _m15Frame.Direction == opposite;

            bool m15Structure =
                m15Aligned &&
                (opposite == 1
                    ? (_m15Frame.MssBull || _m15Frame.ChochBull)
                    : (_m15Frame.MssBear || _m15Frame.ChochBear));

            int evidence = 0;

            if (opposite == 1)
            {
                if (_m5Frame.MssBull) evidence++;
                if (_m5Frame.ChochBull) evidence++;
                if (_m5Frame.DisplacementBull) evidence++;
                if (_m5Frame.LiquidityBull) evidence++;
            }
            else
            {
                if (_m5Frame.MssBear) evidence++;
                if (_m5Frame.ChochBear) evidence++;
                if (_m5Frame.DisplacementBear) evidence++;
                if (_m5Frame.LiquidityBear) evidence++;
            }

            if (!m5Structural ||
                evidence < Math.Max(1, ReversalCloseMinimumEvidence))
                return false;

            if (RequireReversalForce &&
                !m5Force)
                return false;

            if (RequireM15ReversalForOpposite &&
                (!m15Aligned || !m15Structure))
                return false;

            return true;
        }

private bool ExecutePartialClose(
            double percentOfOriginal,
            string tag)
        {
            if (!EnablePartialTakeProfit ||
                _plan == null ||
                _plan.OriginalVolume <= 0 ||
                percentOfOriginal <= 0)
                return true;

            foreach (Position position in Positions)
            {
                if (position == null ||
                    position.SymbolName !=
                    SymbolName ||
                    !IsManagedPosition(position))
                    continue;

                double closeVolume =
                    Symbol.NormalizeVolumeInUnits(
                        _plan.OriginalVolume *
                        percentOfOriginal /
                        100.0,
                        RoundingMode.Down);

                closeVolume =
                    Math.Min(
                        closeVolume,
                        position.VolumeInUnits);

                if (closeVolume <
                    Symbol.VolumeInUnitsMin)
                    return false;

                double remainder =
                    position.VolumeInUnits -
                    closeVolume;

                if (remainder > 0 &&
                    remainder <
                    Symbol.VolumeInUnitsMin)
                    closeVolume =
                        position.VolumeInUnits;

                try
                {
                    bool closingEverything =
                        closeVolume >=
                        position.VolumeInUnits;

                    TradeResult closeResult =
                        closingEverything
                            ? ClosePosition(position)
                            : ClosePosition(
                                position,
                                closeVolume);

                    if (closeResult == null ||
                        !closeResult.IsSuccessful)
                    {
                        Print(
                            "CFIP CLEAN70 partial close rejected ({0}): {1}",
                            tag,
                            closeResult != null &&
                            closeResult.Error.HasValue
                                ? closeResult.Error.Value.ToString()
                                : "UNKNOWN");
                        return false;
                    }

                    if (MoveToBreakEvenAfterPartial &&
                        !closingEverything)
                    {
                        bool shouldMove =
                            !position.StopLoss.HasValue ||
                            BetterStop(
                                _plan.Direction,
                                position.EntryPrice,
                                position.StopLoss.Value);

                        if (shouldMove)
                        {
                            TryModifyStopLoss(
                                position,
                                position.EntryPrice,
                                "PARTIAL BREAK-EVEN");
                        }
                    }

                    if (EnableLevelHitAlerts &&
                        AlertOnLevelHit)
                    {
                        SendUnifiedAlert(
                            "PARTIAL|" +
                            tag +
                            "|" +
                            _plan.CreatedM5,
                            "CFIP partial close at " +
                            tag +
                            " - closed " +
                            percentOfOriginal.ToString(
                                "F0") +
                            "% of original size",
                            _plan.Direction,
                            false);
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    Print(
                        "CFIP CLEAN70 partial close failed ({0}): {1}",
                        tag,
                        ex.Message);
                    return false;
                }
            }

            return false;
        }

        private bool DailyLossLimitHit(
            DateTime nowUtc)
        {
            if (!EnableDailyLossLimit)
                return false;

            if (_dailyLossBaselineDate.Date !=
                nowUtc.Date)
            {
                _dailyLossBaselineDate =
                    nowUtc.Date;

                _dailyStartEquity =
                    Account.Equity;

                _dailyLossLimitAlerted =
                    false;
            }

            if (_dailyStartEquity <= 0)
                return false;

            double lossPercent =
                (_dailyStartEquity -
                 Account.Equity) /
                _dailyStartEquity *
                100.0;

            if (lossPercent <
                Math.Max(
                    0.5,
                    MaximumDailyLossPercent))
                return false;

            if (!_dailyLossLimitAlerted)
            {
                _dailyLossLimitAlerted =
                    true;

                SendUnifiedAlert(
                    "DAILYLOSS|" +
                    nowUtc.Date.ToString(
                        "yyyyMMdd"),
                    "Daily loss limit reached (" +
                    lossPercent.ToString("F2") +
                    "% >= " +
                    MaximumDailyLossPercent.ToString(
                        "F2") +
                    "%) - new auto-trade entries are blocked for the " +
                    "rest of the day. Open positions are left untouched.",
                    0,
                    true);
            }

            return true;
        }

        private SoundType ResolveAlertSoundType(
            string key,
            bool critical)
        {
            if (!UseSemanticAlertSounds)
                return AlertSoundType;

            if (key.StartsWith(
                    "SL|",
                    StringComparison.OrdinalIgnoreCase) ||
                key.StartsWith(
                    "INVALID",
                    StringComparison.OrdinalIgnoreCase) ||
                key.StartsWith(
                    "RESTRICT|",
                    StringComparison.OrdinalIgnoreCase) ||
                key.StartsWith(
                    "REVERSAL|",
                    StringComparison.OrdinalIgnoreCase))
                return SoundType.NegativeNotification;

            if (key.StartsWith(
                    "TP",
                    StringComparison.OrdinalIgnoreCase) ||
                key.StartsWith(
                    "AUTO",
                    StringComparison.OrdinalIgnoreCase) ||
                key.StartsWith(
                    "SIGNAL|",
                    StringComparison.OrdinalIgnoreCase) ||
                key.StartsWith(
                    "HIGH|",
                    StringComparison.OrdinalIgnoreCase) ||
                key.StartsWith(
                    "SMART|",
                    StringComparison.OrdinalIgnoreCase))
                return SoundType.PositiveNotification;

            return critical
                ? SoundType.Confirmation
                : SoundType.Announcement;
        }

                private bool IsInsideSessionWindow(DateTime utc)
        {
            int start =
                ClampInt(SessionStartUtc, 0, 23) * 60;
            int end =
                ClampInt(SessionEndUtc, 0, 23) * 60;
            int now =
                utc.Hour * 60 + utc.Minute;

            if (start == end)
                return true;

            return start < end
                ? now >= start && now < end
                : now >= start || now < end;
        }

        private double AverageAtr(Bars bars, int index, int lookback)
        {
            if (bars == null || index < 1)
                return 0;

            int count =
                Math.Max(2, Math.Min(lookback, index));
            int start =
                Math.Max(1, index - count + 1);

            double sum = 0;
            int samples = 0;

            for (int i = start; i <= index; i++)
            {
                double value = Atr(bars, i);

                if (value <= 0)
                    continue;

                sum += value;
                samples++;
            }

            return samples > 0 ? sum / samples : 0;
        }

        private int CalculateMarketSuitability(
            int closedM5,
            int direction,
            out string reason)
        {
            reason = "OK";

            if (direction != 1 && direction != -1)
            {
                reason = "NO DIRECTION";
                return 0;
            }

            if (_m5Bars == null ||
                _m5Frame == null ||
                _m15Frame == null)
            {
                reason = "MTF DATA";
                return 0;
            }

            if (!Symbol.IsTradingEnabled)
            {
                reason = "SYMBOL TRADING DISABLED";
                return 0;
            }

            if (Symbol.MarketHours == null ||
                !Symbol.MarketHours.IsOpened())
            {
                reason = "MARKET CLOSED";
                return 0;
            }

            DateTime nowUtc = DateTime.UtcNow;
            int score = 50;

            bool inSession =
                IsInsideSessionWindow(nowUtc);

            score += inSession ? 10 : -8;

            if (UseSessionFilter &&
                !SessionAllowed(nowUtc))
            {
                reason = "SESSION FILTER";
                return 25;
            }

            if (RequireSessionSuitability &&
                !inSession)
            {
                reason = "SESSION SUITABILITY";
                return 30;
            }

            if (!FridayAllowed(nowUtc))
            {
                reason = "FRIDAY CUTOFF";
                return 25;
            }

            string newsReason;

            if (NewsBlocked(nowUtc, out newsReason))
            {
                reason = newsReason;
                return 20;
            }

            if (VolatilityBlocked(_m5Bars, closedM5))
            {
                reason = "EVENT SHOCK / VOLATILITY";
                return 25;
            }

            double atr =
                Atr(_m5Bars, closedM5);

            if (atr <= 0)
            {
                reason = "ATR UNAVAILABLE";
                return 0;
            }

            double spreadRatio =
                Math.Max(0, Symbol.Ask - Symbol.Bid) /
                Math.Max(atr, Symbol.TickSize);

            if (spreadRatio <= MaximumSpreadAtr * 0.50)
                score += 18;
            else if (spreadRatio <= MaximumSpreadAtr)
                score += 8;
            else
                score -= 22;

            double averageAtr =
                AverageAtr(_m5Bars, closedM5 - 1, 20);

            double volatilityRatio =
                averageAtr > 0 ? atr / averageAtr : 1.0;

            if (volatilityRatio >=
                    Math.Max(0.75, HealthyAtrMinimumRatio) &&
                volatilityRatio <=
                    Math.Max(1.10, HealthyAtrMaximumRatio))
                score += 12;
            else if (volatilityRatio < 0.60 ||
                     volatilityRatio > 2.25)
                score -= 15;
            else
                score += 4;

            if (_m5Frame.Direction == direction)
                score += 9;
            else if (_m5Frame.Direction == -direction)
                score -= 10;

            if (_m15Frame.Direction == direction)
                score += 9;
            else if (_m15Frame.Direction == -direction)
                score -= 10;

            if (_m30Frame != null)
            {
                if (_m30Frame.Direction == direction)
                    score += 5;
                else if (_m30Frame.Direction == -direction)
                    score -= 6;
            }

            if (_h1Frame != null)
            {
                if (_h1Frame.Direction == direction)
                    score += 4;
                else if (_h1Frame.Direction == -direction)
                    score -= 5;
            }

            if (_h4Frame != null)
            {
                if (_h4Frame.Direction == direction)
                    score += 3;
                else if (_h4Frame.Direction == -direction)
                    score -= 4;
            }

            double averageAdx =
                (_m5Frame.Adx + _m15Frame.Adx) / 2.0;

            if (averageAdx >= 25)
                score += 12;
            else if (averageAdx >= 20)
                score += 7;
            else if (averageAdx < 15)
                score -= 8;

            if (_m5Frame.Choppy && _m15Frame.Choppy)
                score -= 18;
            else if (_m5Frame.Choppy || _m15Frame.Choppy)
                score -= 7;

            if (_decision != null &&
                _decision.TimeframeAgreement >=
                MinimumTimeframeAgreement)
                score += 8;
            else if (_decision != null &&
                     _decision.TimeframeAgreement < 60)
                score -= 8;

            if (_decision != null &&
                _decision.IndependentEvidence >=
                MinimumIndependentEvidence)
                score += 5;

            if (UseDailyPivots &&
                _d1Bars != null &&
                _d1Bars.Count >= 3)
            {
                int d1Index =
                    ClosedIndex(_d1Bars, nowUtc);

                if (d1Index > 0)
                {
                    int previous = d1Index - 1;
                    double pivot =
                        (_d1Bars.HighPrices[previous] +
                         _d1Bars.LowPrices[previous] +
                         _d1Bars.ClosePrices[previous]) / 3.0;

                    double market =
                        direction == 1
                            ? Symbol.Ask
                            : Symbol.Bid;

                    if ((direction == 1 && market >= pivot) ||
                        (direction == -1 && market <= pivot))
                        score += 5;
                    else
                        score -= 3;
                }
            }

            score = ClampInt(score, 0, 100);

            reason =
                score >= Math.Max(50, MinimumMarketSuitability)
                    ? "SUITABILITY " + score
                    : "SUITABILITY " + score +
                      " < " + MinimumMarketSuitability;

            return score;
        }

        private int RefreshMarketSuitability(
            int closedM5,
            int direction,
            bool force)
        {
            DateTime nowUtc = DateTime.UtcNow;

            int throttleSeconds =
                Math.Max(1, SuitabilityRecalculationSeconds);

            bool due =
                force ||
                closedM5 != _marketSuitabilityM5 ||
                _marketSuitabilityDirection != direction ||
                (nowUtc - _lastMarketSuitabilityUtc).TotalSeconds >=
                throttleSeconds;

            if (!due)
                return _marketSuitabilityScore;

            string reason;

            int score =
                CalculateMarketSuitability(
                    closedM5,
                    direction,
                    out reason);

            _marketSuitabilityM5 = closedM5;
            _marketSuitabilityDirection = direction;
            _marketSuitabilityScore = score;
            _marketSuitabilityReason = reason;
            _marketSuitabilityState =
                score >= Math.Max(50, MinimumMarketSuitability)
                    ? "SUITABLE"
                    : "UNSUITABLE";
            _lastMarketSuitabilityUtc = nowUtc;

            return score;
        }

        private bool PassesMarketSuitability(
            int closedM5,
            int direction,
            out string reason)
        {
            int score =
                RefreshMarketSuitability(
                    closedM5,
                    direction,
                    false);

            reason = _marketSuitabilityReason;

            if (!EnableMarketSuitabilityGuard)
                return true;

            bool hardContextBlock =
                _marketSuitabilityReason ==
                    "SYMBOL TRADING DISABLED" ||
                _marketSuitabilityReason ==
                    "MARKET CLOSED" ||
                _marketSuitabilityReason ==
                    "SESSION FILTER" ||
                _marketSuitabilityReason ==
                    "SESSION SUITABILITY" ||
                _marketSuitabilityReason ==
                    "FRIDAY CUTOFF" ||
                _marketSuitabilityReason ==
                    "NEWS BLACKOUT" ||
                _marketSuitabilityReason ==
                    "EVENT SHOCK / VOLATILITY";

            if (hardContextBlock &&
                HardMarketSuitabilityGate)
                return false;

            if (score <
                    Math.Max(
                        50,
                        MinimumMarketSuitability) &&
                HardMarketSuitabilityGate)
                return false;

            return true;
        }

        private double SuitabilityRiskMultiplier()
        {
            if (!UseSmartRiskScaling)
                return 1.0;

            double floor =
                ClampDouble(
                    MinimumSmartRiskMultiplier,
                    0.25,
                    1.0);

            double confidence =
                _decision == null
                    ? 0
                    : ClampDouble(
                        _decision.Confidence,
                        0,
                        100);

            double confidenceScale =
                ClampDouble(
                    (confidence - 70.0) /
                    Math.Max(
                        1.0,
                        Math.Max(
                            70,
                            FullRiskConfidenceThreshold) - 70.0),
                    0,
                    1);

            double suitabilityScale =
                ClampDouble(
                    _marketSuitabilityScore /
                    (double)Math.Max(
                        60,
                        FullRiskSuitabilityThreshold),
                    0,
                    1);

            double scale =
                floor +
                (1.0 - floor) *
                (0.60 * confidenceScale +
                 0.40 * suitabilityScale);

            if (PenalizeChoppyRegimeRisk &&
                _m5Frame != null &&
                _m15Frame != null &&
                (_m5Frame.Choppy ||
                 _m15Frame.Choppy))
                scale *= 0.82;

            return ClampDouble(
                scale,
                floor,
                1.0);
        }

        private double EffectiveAutoRiskPercent()
        {
            double baseRisk =
                Math.Max(
                    0.05,
                    RiskPercentEquity);

            if (!UseSmartRiskScaling)
                return baseRisk;

            return ClampDouble(
                baseRisk * SuitabilityRiskMultiplier(),
                0.05,
                baseRisk);
        }

        private double EffectiveAggressiveRiskPercent()
        {
            double baseRisk =
                Math.Max(
                    0.05,
                    AggressiveRiskPercentEquity);

            if (!UseSmartRiskScaling)
                return baseRisk;

            return ClampDouble(
                baseRisk * SuitabilityRiskMultiplier(),
                0.05,
                baseRisk);
        }

        private bool PassesAutoTradeSafetyGuards(
            TradeType tradeType,
            double volume,
            out string reason)
        {
            reason = "";

            if (!IsFinitePositive(volume))
            {
                reason = "INVALID VOLUME";
                return false;
            }

            if (UseMarketHoursGuard)
            {
                if (!Symbol.IsTradingEnabled)
                {
                    reason = "SYMBOL TRADING DISABLED";
                    return false;
                }

                if (Symbol.MarketHours == null ||
                    !Symbol.MarketHours.IsOpened())
                {
                    reason = "MARKET CLOSED";
                    return false;
                }
            }

            if (UseAutoMarginGuard)
            {
                double freeMargin =
                    Account.FreeMargin;

                if (!IsFinitePositive(freeMargin))
                {
                    reason = "NO FREE MARGIN";
                    return false;
                }

                double estimatedMargin =
                    Symbol.GetEstimatedMargin(
                        tradeType,
                        volume);

                if (!IsFinitePositive(estimatedMargin))
                {
                    reason = "MARGIN ESTIMATE FAILED";
                    return false;
                }

                double maximumUsage =
                    Math.Max(
                        10,
                        Math.Min(
                            100,
                            MaxAutoMarginUsagePercent -
                            Math.Max(
                                0,
                                Math.Min(
                                    40,
                                    MarginBufferPercent))));

                double allowedMargin =
                    freeMargin *
                    maximumUsage /
                    100.0;

                if (estimatedMargin >
                    allowedMargin)
                {
                    reason =
                        "MARGIN " +
                        estimatedMargin.ToString("F0") +
                        " > " +
                        allowedMargin.ToString("F0");
                    return false;
                }
            }

            return true;
        }

        private void SetAutoTradingState(
            string state,
            string reason)
        {
            _autoTradingState =
                string.IsNullOrWhiteSpace(state)
                    ? "WAIT"
                    : state.Trim();

            _autoTradingReason =
                string.IsNullOrWhiteSpace(reason)
                    ? ""
                    : reason.Trim();
        }

        private string AutoTradingPanelLine()
        {
            if (!AutoTradingEnabled)
                return
                    "AUTO TRADING  •  OFF  •  MANUAL REVIEW" +
                    (AutomaticOrdersEnabled
                        ? "  •  ORDERS ON"
                        : "  •  ORDERS OFF");

            string state =
                string.IsNullOrWhiteSpace(_autoTradingState)
                    ? "ARMED"
                    : _autoTradingState;

            return
                "AUTO TRADING  •  ON  •  " +
                state +
                "  •  " +
                (HasTradingPermission() ? "PERM OK" : "PERM OFF") +
                "  •  " +
                (AutomaticOrdersEnabled ? "ORDERS ON" : "ORDERS OFF") +
                "  •  SMART EXEC " +
                _marketSuitabilityScore +
                "/100";
        }

private Color AutoTradingPanelColor()
        {
            if (!AutoTradingEnabled)
                return PanelMutedTextColor;

            if (string.Equals(
                    _autoTradingState,
                    "EXECUTED",
                    StringComparison.OrdinalIgnoreCase))
                return TpLineColor;

            if (string.Equals(
                    _autoTradingState,
                    "BLOCKED",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    _autoTradingState,
                    "ERROR",
                    StringComparison.OrdinalIgnoreCase))
                return PanelWarningColor;

            return PanelAccentColor;
        }

        private double SelectStructuralAutoTarget(
            int closedM5,
            int direction,
            double entry,
            double stop,
            double atr,
            CFIPClean70TargetStage stage)
        {
            if (atr <= 0 ||
                !IsFinitePositive(entry) ||
                !IsFinitePositive(stop))
                return 0;

            double risk =
                Math.Abs(entry - stop);

            if (risk <= 0)
                return 0;

            List<Level> levels =
                BuildTargetLevels(
                    closedM5,
                    direction,
                    entry,
                    atr);

            List<Level> selected =
                SelectTargets(
                    levels,
                    closedM5,
                    entry,
                    risk,
                    direction,
                    atr);

            int stageIndex =
                ClampInt(
                    (int)stage,
                    0,
                    3);

            // All execution paths use the same progressive fallback policy:
            // requested stage -> nearest available earlier stage -> synthetic
            // RR target (when explicitly permitted). This keeps pending,
            // aggressive and market execution behavior identical.
            for (int i = stageIndex;
                 i >= 0;
                 i--)
            {
                if (i < selected.Count &&
                    selected[i] != null &&
                    IsFinitePositive(
                        selected[i].Price))
                    return NormalizePrice(
                        selected[i].Price);
            }

            double fallbackRR =
                stageIndex == 0
                    ? Math.Max(
                        FallbackTp1RR,
                        MinimumRequiredRR())
                    : stageIndex == 1
                        ? Math.Max(
                            FallbackTp2RR,
                            Tp2MinimumRR)
                        : stageIndex == 2
                            ? Math.Max(
                                FallbackTp3RR,
                                Tp3MinimumRR)
                            : Math.Max(
                                FallbackTp4RR,
                                Tp4MinimumRR);

            bool requiresHtf =
                stageIndex == 0
                    ? RequireHtfRewardForTp1
                    : RequireHtfRewardForTp2Plus;

            if (AllowSyntheticTargetFallback &&
                !requiresHtf &&
                fallbackRR > 0)
            {
                return NormalizePrice(
                    direction == 1
                        ? entry + risk * fallbackRR
                        : entry - risk * fallbackRR);
            }

            return 0;
        }

        private void ReconcileLivePlanToActualFill(
            Position position,
            int closedM5)
        {
            if (_plan == null ||
                position == null ||
                _m5Bars == null ||
                closedM5 < 20)
                return;

            int direction =
                position.TradeType == TradeType.Buy
                    ? 1
                    : -1;

            double actualEntry =
                NormalizePrice(
                    position.EntryPrice);

            if (!IsFinitePositive(actualEntry))
                return;

            double atr =
                Atr(
                    _m5Bars,
                    closedM5);

            if (atr <= 0)
                return;

            _plan.Entry =
                actualEntry;

            // The broker may fill at a slightly different price than the
            // executable quote. Rebuild the complete structural ladder from
            // the actual fill so chart, plan and broker protection converge.
            if (RebuildSmartExecutionLevels(
                    closedM5,
                    direction,
                    actualEntry,
                    atr))
            {
                _plan.Entry =
                    actualEntry;

                return;
            }

            double currentStop =
                _plan.Stop;

            if (!IsValidStop(
                    direction,
                    actualEntry,
                    currentStop))
            {
                string stopSource;
                int stopQuality;

                double rebuiltStop =
                    BuildStructuralStop(
                        closedM5,
                        direction,
                        actualEntry,
                        atr,
                        out stopSource,
                        out stopQuality);

                if (IsFinitePositive(rebuiltStop) &&
                    IsValidStop(
                        direction,
                        actualEntry,
                        rebuiltStop))
                {
                    currentStop =
                        rebuiltStop;

                    _plan.StopSource =
                        stopSource;

                    _plan.StopQuality =
                        stopQuality;
                }
                else if (position.StopLoss.HasValue &&
                         IsValidStop(
                             direction,
                             actualEntry,
                             position.StopLoss.Value))
                {
                    currentStop =
                        NormalizePrice(
                            position.StopLoss.Value);

                    _plan.StopSource =
                        "BROKER FILL PROTECTION";

                    _plan.StopQuality =
                        70;
                }
            }

            if (!IsValidStop(
                    direction,
                    actualEntry,
                    currentStop))
                return;

            _plan.Stop =
                NormalizePrice(
                    currentStop);

            _plan.Risk =
                Math.Max(
                    Symbol.PipSize,
                    Math.Abs(
                        _plan.Entry -
                        _plan.Stop));

            List<Level> levels =
                BuildTargetLevels(
                    closedM5,
                    direction,
                    _plan.Entry,
                    atr);

            List<Level> selected =
                SelectTargets(
                    levels,
                    closedM5,
                    _plan.Entry,
                    _plan.Risk,
                    direction,
                    atr);

            double oldTp1 = _plan.Tp1;
            double oldTp2 = _plan.Tp2;
            double oldTp3 = _plan.Tp3;
            double oldTp4 = _plan.Tp4;

            double tp1 =
                SelectTarget(
                    selected,
                    0,
                    _plan.Entry,
                    _plan.Risk,
                    direction,
                    Math.Max(
                        FallbackTp1RR,
                        MinimumRequiredRR()));

            double tp2 =
                SelectTarget(
                    selected,
                    1,
                    _plan.Entry,
                    _plan.Risk,
                    direction,
                    Math.Max(
                        FallbackTp2RR,
                        Tp2MinimumRR));

            double tp3 =
                SelectTarget(
                    selected,
                    2,
                    _plan.Entry,
                    _plan.Risk,
                    direction,
                    Math.Max(
                        FallbackTp3RR,
                        Tp3MinimumRR));

            double tp4 =
                SelectTarget(
                    selected,
                    3,
                    _plan.Entry,
                    _plan.Risk,
                    direction,
                    Math.Max(
                        FallbackTp4RR,
                        Tp4MinimumRR));

            _plan.Tp1 =
                IsValidTarget(
                    direction,
                    _plan.Entry,
                    tp1)
                    ? NormalizePrice(tp1)
                    : oldTp1;

            _plan.Tp2 =
                IsValidTarget(
                    direction,
                    _plan.Entry,
                    tp2)
                    ? NormalizePrice(tp2)
                    : oldTp2;

            _plan.Tp3 =
                IsValidTarget(
                    direction,
                    _plan.Entry,
                    tp3)
                    ? NormalizePrice(tp3)
                    : oldTp3;

            _plan.Tp4 =
                IsValidTarget(
                    direction,
                    _plan.Entry,
                    tp4)
                    ? NormalizePrice(tp4)
                    : oldTp4;

            ApplyTargetMeta(
                levels,
                _plan.Tp1,
                atr,
                out _plan.Tp1Source,
                out _plan.Tp1Quality);

            ApplyTargetMeta(
                levels,
                _plan.Tp2,
                atr,
                out _plan.Tp2Source,
                out _plan.Tp2Quality);

            ApplyTargetMeta(
                levels,
                _plan.Tp3,
                atr,
                out _plan.Tp3Source,
                out _plan.Tp3Quality);

            ApplyTargetMeta(
                levels,
                _plan.Tp4,
                atr,
                out _plan.Tp4Source,
                out _plan.Tp4Quality);

            _plan.HtfTargetCount =
                CountHtfTargetsInPlan(
                    _plan);

            _runtimeTpStageIndex =
                -1;

            _runtimeTpStagePlanCreatedM5 =
                -1;

            RecalculatePlanRR();
        }


        #region v66 Execution Coordination
        // ============================================================

        private void SetAutoTradingRuntimeState(bool enabled, string reason)
        {
            _autoTradingEnabledRuntime = enabled;
            _autoExecutionBlockReason =
                string.IsNullOrWhiteSpace(reason)
                    ? (enabled ? "NOT EVALUATED" : "DISABLED")
                    : reason;
            SyncQuickExecutionControls();
        }

        private void SetAutomaticOrdersRuntimeState(bool enabled, string reason)
        {
            _automaticOrdersEnabledRuntime = enabled;
            _autoOrdersBlockReason =
                string.IsNullOrWhiteSpace(reason)
                    ? (enabled ? "NOT EVALUATED" : "DISABLED")
                    : reason;
            SyncQuickExecutionControls();
        }

        private bool RequestLivePlanExit(
            int closedM5,
            string reason)
        {
            if (_plan == null ||
                !_plan.IsLivePosition)
                return false;

            Position position =
                GetManagedLivePositionForPlan();

            if (position == null)
            {
                _plan = null;
                _activeBrokerStop = 0;
                _activeBrokerTarget = 0;
                _executionModel = null;

                SetLifecycleState(
                    CFIPClean70LifecycleState.Closed,
                    reason +
                    " • POSITION ALREADY CLOSED");

                RemovePlanObjects();
                return true;
            }

            SetLifecycleState(
                CFIPClean70LifecycleState.ExitRequested,
                reason);

            if (!TryClosePosition(
                    position,
                    reason))
            {
                SetLifecycleState(
                    CFIPClean70LifecycleState.RecoveryRequired,
                    reason +
                    " • EXIT REJECTED");

                _autoExecutionBlockReason =
                    reason +
                    " • EXIT REJECTED";

                return false;
            }

            _lastExitM5 =
                Math.Max(
                    _lastExitM5,
                    closedM5);

            return true;
        }

        private void SetLifecycleState(
            CFIPClean70LifecycleState state,
            string reason)
        {
            _lifecycleState = state;
            _lifecycleReason =
                string.IsNullOrWhiteSpace(reason)
                    ? state.ToString().ToUpperInvariant()
                    : reason;
        }

        private Position GetManagedPositionById(long positionId)
        {
            if (positionId <= 0)
                return null;

            foreach (Position position in Positions)
            {
                if (position != null &&
                    position.Id == positionId &&
                    IsManagedPosition(position))
                    return position;
            }

            return null;
        }

        private bool TryModifyStopLoss(
            Position position,
            double price,
            string context)
        {
            if (position == null ||
                !IsFinitePositive(price))
                return false;

            double normalized =
                NormalizePrice(price);

            if (!IsFinitePositive(normalized))
                return false;

            try
            {
                TradeResult result =
                    position.ModifyStopLossPrice(normalized);

                if (result == null ||
                    !result.IsSuccessful)
                {
                    Print(
                        "CFIP CLEAN70 SL mutation rejected ({0}).",
                        context);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Print(
                    "CFIP CLEAN70 SL mutation failed ({0}): {1}",
                    context,
                    ex.Message);
                return false;
            }
        }

        private bool TryModifyTakeProfit(
            Position position,
            double price,
            string context)
        {
            if (position == null ||
                !IsFinitePositive(price))
                return false;

            double normalized =
                NormalizePrice(price);

            if (!IsFinitePositive(normalized))
                return false;

            try
            {
                TradeResult result =
                    position.ModifyTakeProfitPrice(normalized);

                if (result == null ||
                    !result.IsSuccessful)
                {
                    Print(
                        "CFIP CLEAN70 TP mutation rejected ({0}).",
                        context);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Print(
                    "CFIP CLEAN70 TP mutation failed ({0}): {1}",
                    context,
                    ex.Message);
                return false;
            }
        }

        private bool TryClosePosition(
            Position position,
            string context,
            double? volumeInUnits = null)
        {
            if (position == null)
                return false;

            try
            {
                TradeResult result =
                    volumeInUnits.HasValue
                        ? ClosePosition(
                            position,
                            volumeInUnits.Value)
                        : ClosePosition(position);

                if (result == null ||
                    !result.IsSuccessful)
                {
                    Print(
                        "CFIP CLEAN70 close rejected ({0}).",
                        context);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Print(
                    "CFIP CLEAN70 close failed ({0}): {1}",
                    context,
                    ex.Message);
                return false;
            }
        }

        private bool TryCancelPendingOrder(
            PendingOrder order,
            string context)
        {
            if (order == null)
                return false;

            try
            {
                TradeResult result =
                    CancelPendingOrder(order);

                if (result == null ||
                    !result.IsSuccessful)
                {
                    Print(
                        "CFIP CLEAN70 pending cancel rejected ({0}).",
                        context);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Print(
                    "CFIP CLEAN70 pending cancel failed ({0}): {1}",
                    context,
                    ex.Message);
                return false;
            }
        }

        private bool EnsureBrokerProtectionForPosition(
            Position position,
            double stop,
            double target,
            string context,
            int direction)
        {
            bool stopOk =
                TryModifyStopLoss(
                    position,
                    stop,
                    context + " • SL");

            bool targetOk =
                TryModifyTakeProfit(
                    position,
                    target,
                    context + " • TP");

            bool protectedOk =
                stopOk &&
                targetOk;

            _brokerProtectionRecoveryRequired =
                !protectedOk;

            if (!protectedOk)
            {
                SetLifecycleState(
                    CFIPClean70LifecycleState.RecoveryRequired,
                    context +
                    " • BROKER PROTECTION REJECTED");

                SendUnifiedAlert(
                    "PROTECTION-REJECTED|" +
                    position.Id,
                    "CFIP CLEAN70 BROKER PROTECTION REJECTED | #" +
                    position.Id,
                    direction,
                    true);
            }

            return protectedOk;
        }

        private bool IsExecutionPlanConsistent(
            int direction,
            double entry,
            double stop,
            double target)
        {
            return direction != 0 &&
                   IsFinitePositive(entry) &&
                   IsFinitePositive(stop) &&
                   IsFinitePositive(target) &&
                   IsValidStop(direction, entry, stop) &&
                   IsValidTarget(direction, entry, target);
        }

        // Single structural source of truth for executable Entry/SL/TP.
        private bool RebuildSmartExecutionLevels(
            int closedM5,
            int direction,
            double executionEntry,
            double atr)
        {
            if (_plan == null ||
                _m5Bars == null ||
                direction == 0 ||
                !IsFinitePositive(executionEntry) ||
                atr <= 0)
                return false;

            executionEntry = NormalizePrice(executionEntry);

            string stopSource;
            int stopQuality;

            double stop = BuildStructuralStop(
                closedM5,
                direction,
                executionEntry,
                atr,
                out stopSource,
                out stopQuality);

            if (!IsFinitePositive(stop) ||
                !IsValidStop(direction, executionEntry, stop))
                return false;

            double risk = Math.Max(
                Symbol.PipSize,
                Math.Abs(executionEntry - stop));

            List<Level> levels = BuildTargetLevels(
                closedM5,
                direction,
                executionEntry,
                atr);

            List<Level> selected = SelectTargets(
                levels,
                closedM5,
                executionEntry,
                risk,
                direction,
                atr);

            double tp1 = SelectTarget(
                selected, 0, executionEntry, risk, direction,
                Math.Max(FallbackTp1RR, MinimumRequiredRR()));
            double tp2 = SelectTarget(
                selected, 1, executionEntry, risk, direction,
                Math.Max(FallbackTp2RR, Tp2MinimumRR));
            double tp3 = SelectTarget(
                selected, 2, executionEntry, risk, direction,
                Math.Max(FallbackTp3RR, Tp3MinimumRR));
            double tp4 = SelectTarget(
                selected, 3, executionEntry, risk, direction,
                Math.Max(FallbackTp4RR, Tp4MinimumRR));

            if (!IsValidTarget(direction, executionEntry, tp1))
                return false;

            _plan.Entry = executionEntry;
            _plan.Stop = NormalizePrice(stop);
            _plan.Risk = risk;
            _plan.StopSource = stopSource;
            _plan.StopQuality = stopQuality;

            _plan.Tp1 = IsValidTarget(direction, executionEntry, tp1)
                ? NormalizePrice(tp1) : 0;
            _plan.Tp2 = IsValidTarget(direction, executionEntry, tp2)
                ? NormalizePrice(tp2) : 0;
            _plan.Tp3 = IsValidTarget(direction, executionEntry, tp3)
                ? NormalizePrice(tp3) : 0;
            _plan.Tp4 = IsValidTarget(direction, executionEntry, tp4)
                ? NormalizePrice(tp4) : 0;

            ApplyTargetMeta(levels, _plan.Tp1, atr,
                out _plan.Tp1Source, out _plan.Tp1Quality);
            ApplyTargetMeta(levels, _plan.Tp2, atr,
                out _plan.Tp2Source, out _plan.Tp2Quality);
            ApplyTargetMeta(levels, _plan.Tp3, atr,
                out _plan.Tp3Source, out _plan.Tp3Quality);
            ApplyTargetMeta(levels, _plan.Tp4, atr,
                out _plan.Tp4Source, out _plan.Tp4Quality);

            _plan.HtfTargetCount = CountHtfTargetsInPlan(_plan);
            _runtimeTpStageIndex = -1;
            _runtimeTpStagePlanCreatedM5 = -1;
            RecalculatePlanRR();

            double effectiveTarget =
                AutoTarget(_plan, EffectiveAutoTpStage());

            return IsExecutionPlanConsistent(
                direction, executionEntry, _plan.Stop, effectiveTarget);
        }

        private bool TryPrepareExecutablePlan(
            int closedM5,
            double executionEntry,
            out double stopPips,
            out double targetPips,
            out double target)
        {
            stopPips =
                0;

            targetPips =
                0;

            target =
                0;

            if (_plan == null || _m5Bars == null || closedM5 < 20)
                return false;

            double atr = Atr(_m5Bars, closedM5);
            if (atr <= 0)
                return false;

            if (!RebuildSmartExecutionLevels(
                    closedM5, _plan.Direction, executionEntry, atr))
                return false;

            double stop =
                _plan.Stop;

            target =
                AutoTarget(
                    _plan,
                    EffectiveAutoTpStage());

            if (!IsExecutionPlanConsistent(
                    _plan.Direction,
                    executionEntry,
                    stop,
                    target))
                return false;

            target =
                NormalizePrice(
                    target);

            stopPips =
                Math.Abs(
                    executionEntry -
                    stop) /
                Symbol.PipSize;

            targetPips =
                Math.Abs(
                    target -
                    executionEntry) /
                Symbol.PipSize;

            return stopPips > 0 &&
                   targetPips > 0 &&
                   IsExecutionPlanConsistent(
                       _plan.Direction,
                       executionEntry,
                       stop,
                       target);
        }

        #endregion

        private void TryAutoTrade(
            int closedM5)
        {
            _lastAutoTradeAttemptUtc =
                DateTime.UtcNow;

            if (!AutoTradingEnabled)
            {
                _lastAutoPlanAttemptM5 =
                    -1;

                SetAutoTradingState(
                    "OFF",
                    "DISABLED");
                return;
            }

            PendingOrder existingPending =
                GetManagedPendingOrder();

            if (existingPending != null)
            {
                _autoExecutionBlockReason =
                    "PENDING ORDER EXISTS";
                SetAutoTradingState(
                    "ARMED",
                    "WAITING FOR PENDING ORDER");
                return;
            }

            if (DailyLossLimitHit(
                    DateTime.UtcNow))
            {
                _autoExecutionBlockReason =
                    "DAILY LOSS LIMIT";
                SetAutoTradingState(
                    "BLOCKED",
                    "DAILY LOSS LIMIT REACHED");
                return;
            }

            if (_plan == null)
            {
                EnsureSignalPlan(
                    closedM5,
                    !ConfirmedSignalsOnly);

                if (_plan == null)
                {
                    if (_executionModel != null &&
                        _executionModel.Mode ==
                            CFIPClean70ExecutionMode.WaitingForTrigger)
                    {
                        _autoExecutionBlockReason =
                            "WAITING FOR TRIGGER";

                        SetAutoTradingState(
                            "ARMED",
                            "WAITING FOR TRIGGER");
                    }
                    else
                    {
                        _autoExecutionBlockReason =
                            "NO ELIGIBLE PLAN";

                        SetAutoTradingState(
                            "ARMED",
                            ConfirmedSignalsOnly
                                ? "WAITING FOR CONFIRMED PLAN"
                                : "WAITING FOR SMART-ELIGIBLE PLAN");
                    }

                    return;
                }
            }

            if (_decision == null ||
                _decision.Direction == 0)
            {
                _autoExecutionBlockReason =
                    "NO DECISION";
                SetAutoTradingState(
                    "ARMED",
                    "WAITING FOR DECISION");
                return;
            }

            if (ConfirmedSignalsOnly &&
                !_decision.EntryAllowed)
            {
                _autoExecutionBlockReason =
                    string.IsNullOrWhiteSpace(
                        _decision.BlockReason)
                        ? "WAITING FOR CONFIRMATION"
                        : _decision.BlockReason;
                SetAutoTradingState(
                    "ARMED",
                    string.IsNullOrWhiteSpace(
                        _decision.BlockReason)
                        ? "WAITING FOR CONFIRMATION"
                        : _decision.BlockReason);
                return;
            }

            if (OneOrderPerSignal &&
                _lastAutoM5 == closedM5)
            {
                _autoExecutionBlockReason =
                    "ALREADY TRADED THIS M5";
                SetAutoTradingState(
                    "ARMED",
                    "ALREADY TRADED THIS M5");
                return;
            }

            if (_decision.Confidence <
                MinimumAutoConfidence)
            {
                _autoExecutionBlockReason =
                    "CONF " +
                    _decision.Confidence +
                    " < " +
                    MinimumAutoConfidence;
                SetAutoTradingState(
                    "BLOCKED",
                    "CONF " +
                    _decision.Confidence +
                    " < " +
                    MinimumAutoConfidence);
                return;
            }

            if (_decision.SmartQuality <
                MinimumAutoSmartQuality)
            {
                _autoExecutionBlockReason =
                    "SMART Q " +
                    _decision.SmartQuality +
                    " < " +
                    MinimumAutoSmartQuality;
                SetAutoTradingState(
                    "BLOCKED",
                    "SMART Q " +
                    _decision.SmartQuality +
                    " < " +
                    MinimumAutoSmartQuality);
                return;
            }

            string suitabilityReason;

            if (!PassesMarketSuitability(
                    closedM5,
                    _plan.Direction,
                    out suitabilityReason))
            {
                _autoExecutionBlockReason =
                    "SUITABILITY • " +
                    suitabilityReason;
                SetAutoTradingState(
                    "BLOCKED",
                    suitabilityReason);
                return;
            }

            int levelQuality =
                Math.Min(
                    _plan.StopQuality,
                    _plan.Tp1Quality);

            if (levelQuality <
                MinimumAutoLevelQuality)
            {
                _autoExecutionBlockReason =
                    "LEVEL Q " +
                    levelQuality +
                    " < " +
                    MinimumAutoLevelQuality;
                SetAutoTradingState(
                    "BLOCKED",
                    "LEVEL Q " +
                    levelQuality +
                    " < " +
                    MinimumAutoLevelQuality);
                return;
            }

            if (ManagedPositionCount() >=
                Math.Max(
                    1,
                    MaximumOpenPositions))
            {
                _autoExecutionBlockReason =
                    "MAX OPEN POSITIONS";
                SetAutoTradingState(
                    "BLOCKED",
                    "MAX OPEN POSITIONS");
                return;
            }

            double plannedEntry = _plan.Entry;

            double entry =
                NormalizePrice(
                    _plan.Direction == 1
                        ? Symbol.Ask
                        : Symbol.Bid);

            double atr =
                Atr(
                    _m5Bars,
                    closedM5);

            if (atr <= 0)
            {
                SetAutoTradingState(
                    "BLOCKED",
                    "ATR UNAVAILABLE");
                return;
            }

            if (Math.Abs(
                    entry -
                    plannedEntry) >
                atr *
                MaximumEntryExtensionAtr)
            {
                SetAutoTradingState(
                    "BLOCKED",
                    "ENTRY EXTENSION");
                return;
            }

            double stopPips;
            double targetPips;
            double target;

            if (!TryPrepareExecutablePlan(
                    closedM5,
                    entry,
                    out stopPips,
                    out targetPips,
                    out target))
            {
                SetAutoTradingState(
                    "BLOCKED",
                    "INVALID SMART EXECUTION PLAN");
                return;
            }

            string executableEntryReason;

            if (!IsExecutableMarketEntry(
                    _plan,
                    entry,
                    out executableEntryReason))
            {
                _autoExecutionBlockReason =
                    executableEntryReason;
                SetAutoTradingState(
                    "ARMED",
                    executableEntryReason);
                return;
            }

            double effectiveStopPips =
                EffectiveRiskStopPips(stopPips);

            double volume =
                CalculateVolume(
                    effectiveStopPips);

            volume =
                AdjustVolumeForMargin(
                    _plan.Direction == 1
                        ? TradeType.Buy
                        : TradeType.Sell,
                    volume);

            if (volume <
                Symbol.VolumeInUnitsMin)
            {
                SetAutoTradingState(
                    "BLOCKED",
                    "VOLUME BELOW MINIMUM");
                return;
            }

            try
            {
                TradeType type =
                    _plan.Direction == 1
                        ? TradeType.Buy
                        : TradeType.Sell;

                if (!EnsureTradingPermission())
                {
                    _autoExecutionBlockReason =
                        "TRADING PERMISSION";
                    SetAutoTradingState(
                        "BLOCKED",
                        "TRADING PERMISSION NOT GRANTED");
                    return;
                }

                string guardReason;

                if (!PassesAutoTradeSafetyGuards(
                        type,
                        volume,
                        out guardReason))
                {
                    _autoExecutionBlockReason =
                        guardReason;

                    SetAutoTradingState(
                        "BLOCKED",
                        guardReason);
                    return;
                }

                TradeResult result =
                    ExecuteMarketOrder(
                        type,
                        SymbolName,
                        volume,
                        NormalizeLabel(),
                        stopPips,
                        targetPips,
                        "CFIP SMART70",
                        false);

                if (result == null)
                {
                    SetAutoTradingState(
                        "ERROR",
                        "NULL TRADE RESULT");
                    return;
                }

                if (!result.IsSuccessful ||
                    result.Position == null)
                {
                    SetAutoTradingState(
                        "ERROR",
                        result.Error.HasValue
                            ? result.Error.Value.ToString()
                            : "TRADE REJECTED");
                    return;
                }

                string fillExecutionReason;

                if (!IsExecutableFillPrice(
                        _plan,
                        result.Position.EntryPrice,
                        out fillExecutionReason))
                {
                    _autoExecutionBlockReason =
                        fillExecutionReason;
                    SetAutoTradingState(
                        "ERROR",
                        fillExecutionReason);
                    Print(
                        "CFIP CLEAN70 fill/execution mismatch: {0}",
                        fillExecutionReason);
                    return;
                }

                _lastAutoM5 =
                    closedM5;

                _autoExecutionBlockReason =
                    "EXECUTED";

                _plan.OriginalVolume =
                    result.Position.VolumeInUnits;

                _plan.IsLivePosition = true;
                _plan.PositionId = result.Position.Id;

                ReconcileLivePlanToActualFill(
                    result.Position,
                    closedM5);

                double structuralTarget =
                    AutoTarget(
                        _plan,
                        EffectiveAutoTpStage());

                if (IsValidTarget(
                        _plan.Direction,
                        result.Position.EntryPrice,
                        structuralTarget))
                {
                    target =
                        NormalizePrice(
                            structuralTarget);
                }
                else if (result.Position.TakeProfit.HasValue &&
                         IsValidTarget(
                             _plan.Direction,
                             result.Position.EntryPrice,
                             result.Position.TakeProfit.Value))
                {
                    target =
                        NormalizePrice(
                            result.Position.TakeProfit.Value);
                }
                else if (IsValidTarget(
                             _plan.Direction,
                             result.Position.EntryPrice,
                             _plan.Tp1))
                {
                    target =
                        NormalizePrice(
                            _plan.Tp1);
                }
                else
                {
                    _autoExecutionBlockReason =
                        "POST-FILL TARGET INVALID";

                    SetAutoTradingState(
                        "ERROR",
                        "POSITION OPENED • NO VALID TARGET");
                    return;
                }

                if (AutoBrokerProtection)
                {
                    EnsureBrokerProtectionForPosition(
                        result.Position,
                        _plan.Stop,
                        target,
                        "NEW MARKET ENTRY",
                        _plan.Direction);
                }

                SetAutoTradingState(
                    "EXECUTED",
                    "POSITION #" +
                    result.Position.Id);

                SendUnifiedAlert(
                    "AUTO|" +
                    closedM5,
                    "CFIP CLEAN70 AUTO " +
                    (_plan.Direction == 1
                        ? "BUY"
                        : "SELL") +
                    " EXECUTED | #" +
                    result.Position.Id +
                    " | ENTRY " +
                    Price(
                        result.Position.EntryPrice) +
                    " | SL " +
                    Price(
                        _plan.Stop) +
                    " | TP " +
                    Price(
                        target),
                    _plan.Direction,
                    true);
            }
            catch (Exception ex)
            {
                SetAutoTradingState(
                    "ERROR",
                    ex.Message);

                Print(
                    "CFIP CLEAN70 auto trade failed: {0}",
                    ex.Message);
            }
        }

        private double AdjustVolumeForMargin(
            TradeType tradeType,
            double volume)
        {
            if (!IsFinitePositive(volume) ||
                !UseAutoMarginGuard)
                return volume;

            try
            {
                double freeMargin =
                    Math.Max(
                        0,
                        Account.FreeMargin);

                double usage =
                    Math.Max(
                        10,
                        Math.Min(
                            100,
                            MaxAutoMarginUsagePercent -
                            Math.Max(
                                0,
                                Math.Min(
                                    40,
                                    MarginBufferPercent))));

                double allowed =
                    freeMargin *
                    usage /
                    100.0;

                if (allowed <= 0)
                    return 0;

                double estimated =
                    Symbol.GetEstimatedMargin(
                        tradeType,
                        volume);

                if (!IsFinitePositive(estimated) ||
                    estimated <= allowed)
                    return Symbol.NormalizeVolumeInUnits(
                        volume,
                        RoundingMode.Down);

                double reduced =
                    Symbol.NormalizeVolumeInUnits(
                        volume *
                        allowed /
                        estimated,
                        RoundingMode.Down);

                while (reduced >= Symbol.VolumeInUnitsMin)
                {
                    double check =
                        Symbol.GetEstimatedMargin(
                            tradeType,
                            reduced);

                    if (!IsFinitePositive(check) ||
                        check <= allowed)
                        break;

                    reduced =
                        Symbol.NormalizeVolumeInUnits(
                            reduced -
                            Symbol.VolumeInUnitsStep,
                            RoundingMode.Down);
                }

                return reduced >= Symbol.VolumeInUnitsMin
                    ? reduced
                    : 0;
            }
            catch (Exception ex)
            {
                Print(
                    "CFIP CLEAN70 margin sizing failed: {0}",
                    ex.Message);
                return 0;
            }
        }

        private double CalculateVolume(
            double stopPips)
        {
            try
            {
                double volume;

                if (SizingMode ==
                    CFIPClean70SizingMode.FixedLots)
                {
                    volume =
                        Symbol.QuantityToVolumeInUnits(
                            Math.Max(
                                0.001,
                                FixedLots));
                }
                else
                {
                    double riskAmount =
                        Math.Max(
                            0,
                            Account.Equity) *
                        EffectiveAutoRiskPercent() /
                        100.0;

                    if (riskAmount <= 0)
                        return 0;

                    volume =
                        Symbol.VolumeForFixedRisk(
                            riskAmount,
                            stopPips,
                            RoundingMode.Down);
                }

                if (!IsFinitePositive(
                        volume))
                    return 0;

                volume =
                    Symbol.NormalizeVolumeInUnits(
                        volume,
                        RoundingMode.Down);

                if (volume <
                    Symbol.VolumeInUnitsMin)
                    return 0;

                if (volume >
                    Symbol.VolumeInUnitsMax)
                {
                    volume =
                        Symbol.NormalizeVolumeInUnits(
                            Symbol.VolumeInUnitsMax,
                            RoundingMode.Down);
                }

                return volume;
            }
            catch (Exception ex)
            {
                Print(
                    "CFIP CLEAN70 volume calculation failed: {0}",
                    ex.Message);

                return 0;
            }
        }

        private int ManagedPositionCount()
        {
            int count = 0;

            foreach (Position position in Positions)
            {
                if (IsManagedPosition(position))
                    count++;
            }

            return count;
        }

        private bool IsAutoPlanValid(
            int direction,
            double entry,
            double stop,
            double target)
        {
            return
                (direction == 1 ||
                 direction == -1) &&
                IsFinitePositive(entry) &&
                IsValidStop(
                    direction,
                    entry,
                    stop) &&
                IsValidTarget(
                    direction,
                    entry,
                    target);
        }

        private double AutoTarget(
            Plan plan,
            CFIPClean70TargetStage stage)
        {
            if (stage ==
                    CFIPClean70TargetStage.TP4 &&
                plan.Tp4 > 0)
                return plan.Tp4;

            if (stage ==
                    CFIPClean70TargetStage.TP3 &&
                plan.Tp3 > 0)
                return plan.Tp3;

            if (stage ==
                    CFIPClean70TargetStage.TP2 &&
                plan.Tp2 > 0)
                return plan.Tp2;

            return plan.Tp1;
        }

        private int _runtimeTpStageIndex = -1;

        private int _runtimeTpStagePlanCreatedM5 = -1;

        private CFIPClean70TargetStage EffectiveAutoTpStage()
        {
            if (!EnableDynamicTpAdvance ||
                _plan == null)
                return AutoTpStage;

            int baseStage =
                (int)AutoTpStage;

            // A new plan resets the ratchet back to the configured base
            // stage — this is a per-trade advance, not a permanent state.
            if (_runtimeTpStagePlanCreatedM5 !=
                _plan.CreatedM5)
            {
                _runtimeTpStagePlanCreatedM5 =
                    _plan.CreatedM5;

                _runtimeTpStageIndex =
                    baseStage;
            }

            if (_runtimeTpStageIndex <
                baseStage)
                _runtimeTpStageIndex =
                    baseStage;

            double risk =
                Math.Max(
                    Symbol.PipSize,
                    _plan.Risk);

            while (_runtimeTpStageIndex < 3)
            {
                double currentTarget =
                    AutoTarget(
                        _plan,
                        (CFIPClean70TargetStage)
                        _runtimeTpStageIndex);

                double nextTarget =
                    AutoTarget(
                        _plan,
                        (CFIPClean70TargetStage)
                        (_runtimeTpStageIndex +
                         1));

                // AutoTarget() falls back to Tp1 for a stage with no valid
                // price, so confirm the "next" stage is a genuinely farther
                // level before treating it as something to advance to.
                bool nextIsFarther =
                    _plan.Direction == 1
                        ? nextTarget >
                          currentTarget
                        : nextTarget <
                          currentTarget;

                if (!nextIsFarther)
                    break;

                double distanceToCurrent =
                    Math.Abs(
                        currentTarget -
                        _plan.Entry);

                if (distanceToCurrent <=
                    risk * 0.1)
                    break;

                double covered =
                    _plan.Direction == 1
                        ? _lastMarket -
                          _plan.Entry
                        : _plan.Entry -
                          _lastMarket;

                double progressPercent =
                    covered /
                    distanceToCurrent *
                    100.0;

                if (progressPercent <
                    Math.Max(
                        50,
                        TpAdvanceProximityPercent))
                    break;

                _runtimeTpStageIndex++;
            }

            return
                (CFIPClean70TargetStage)
                _runtimeTpStageIndex;
        }

        private void SynchronizeLiveBrokerState()
        {
            _activeBrokerStop = 0;
            _activeBrokerTarget = 0;

            if (_plan == null ||
                !_plan.IsLivePosition)
                return;

            Position position =
                GetManagedLivePositionForPlan();

            if (position == null)
                return;

            int direction =
                position.TradeType == TradeType.Buy
                    ? 1
                    : -1;

            if (IsFinitePositive(position.EntryPrice))
                _plan.Entry =
                    NormalizePrice(position.EntryPrice);

            if (position.StopLoss.HasValue &&
                IsFinitePositive(position.StopLoss.Value) &&
                IsValidStop(
                    direction,
                    position.EntryPrice,
                    position.StopLoss.Value))
            {
                _activeBrokerStop =
                    NormalizePrice(position.StopLoss.Value);
            }

            if (position.TakeProfit.HasValue &&
                IsFinitePositive(position.TakeProfit.Value))
            {
                _activeBrokerTarget =
                    NormalizePrice(position.TakeProfit.Value);
            }
        }

        private double GetActiveBrokerStopPrice()
        {
            if (_plan != null &&
                _plan.IsLivePosition &&
                IsFinitePositive(_activeBrokerStop))
                return _activeBrokerStop;

            return _plan == null
                ? 0
                : _plan.Stop;
        }

        private double GetActiveBrokerTargetPrice()
        {
            return
                _plan != null &&
                _plan.IsLivePosition &&
                IsFinitePositive(_activeBrokerTarget)
                    ? _activeBrokerTarget
                    : 0;
        }

        private string BrokerTargetStageText(double target)
        {
            if (!IsFinitePositive(target) ||
                _plan == null)
                return "CUSTOM / NONE";

            if (SamePrice(target, _plan.Tp1))
                return "TP1";
            if (SamePrice(target, _plan.Tp2))
                return "TP2";
            if (SamePrice(target, _plan.Tp3))
                return "TP3";
            if (SamePrice(target, _plan.Tp4))
                return "TP4";

            return "CUSTOM";
        }

        private string NormalizeLabel()
        {
            return
                string.IsNullOrWhiteSpace(
                    AutoTradeLabel)
                    ? "CFIP-SMART-CLEAN66"
                    : AutoTradeLabel.Trim();
        }

                private void CloseAllPositions()
        {
            bool allClosedOrAbsent = true;

            foreach (Position position in Positions)
            {
                if (!IsManagedPosition(position))
                    continue;

                if (!TryClosePosition(
                        position,
                        "END OF DAY"))
                    allClosedOrAbsent = false;
            }

            if (GetManagedPosition() == null)
            {
                _plan = null;
                _executionModel = null;
                _activeBrokerStop = 0;
                _activeBrokerTarget = 0;

                SetLifecycleState(
                    CFIPClean70LifecycleState.Closed,
                    "END OF DAY • CLOSED");

                RemovePlanObjects();
            }
            else if (!allClosedOrAbsent)
            {
                SetLifecycleState(
                    CFIPClean70LifecycleState.RecoveryRequired,
                    "END OF DAY • CLOSE REJECTED");
            }
            else
            {
                SetLifecycleState(
                    CFIPClean70LifecycleState.ExitRequested,
                    "END OF DAY • EXIT REQUESTED");
            }
        }


                private void CancelAllOrders()
        {
            bool allCancelledOrAbsent = true;

            foreach (PendingOrder order in PendingOrders)
            {
                if (!IsManagedPendingOrder(order))
                    continue;

                if (!TryCancelPendingOrder(
                        order,
                        "PENDING CIRCUIT BREAKER"))
                    allCancelledOrAbsent = false;
            }

            if (GetManagedPendingOrder() == null)
            {
                RemoveManagedPendingOrderObjects();
            }
            else if (!allCancelledOrAbsent)
            {
                SetLifecycleState(
                    CFIPClean70LifecycleState.RecoveryRequired,
                    "PENDING CANCEL REJECTED");
            }
        }


        private int FreshTriggerEvidence(
            Bars bars,
            int index,
            int direction)
        {
            if (bars == null || index < 5)
                return 0;

            int evidence = 0;

            double atr =
                Atr(
                    bars,
                    index);

            double body =
                Math.Abs(
                    bars.ClosePrices[index] -
                    bars.OpenPrices[index]);

            if (direction == 1 &&
                bars.ClosePrices[index] >
                bars.OpenPrices[index])
                evidence++;

            if (direction == -1 &&
                bars.ClosePrices[index] <
                bars.OpenPrices[index])
                evidence++;

            if (atr > 0 &&
                body >=
                atr *
                MinimumTriggerBodyAtr)
                evidence++;

            if (direction == 1 &&
                bars.ClosePrices[index] >
                Highest(
                    bars,
                    Math.Max(
                        0,
                        index - 5),
                    index - 1))
                evidence++;

            if (direction == -1 &&
                bars.ClosePrices[index] <
                Lowest(
                    bars,
                    Math.Max(
                        0,
                        index - 5),
                    index - 1))
                evidence++;

            return evidence;
        }

        private int StructuralSequence(
            Bars bars,
            int index,
            int direction)
        {
            if (bars == null ||
                index < 8)
                return 0;

            double atr =
                Atr(
                    bars,
                    index);

            int result = 0;

            if (direction == 1)
            {
                if (BullStructure(
                        bars,
                        index,
                        atr))
                    result++;

                if (BullMss(
                        bars,
                        index,
                        atr))
                    result++;

                if (BullDisplacement(
                        bars,
                        index,
                        atr))
                    result++;
            }
            else
            {
                if (BearStructure(
                        bars,
                        index,
                        atr))
                    result++;

                if (BearMss(
                        bars,
                        index,
                        atr))
                    result++;

                if (BearDisplacement(
                        bars,
                        index,
                        atr))
                    result++;
            }

            return result;
        }

        private int EntryLocationQuality(
            Bars bars,
            int index,
            int direction)
        {
            if (bars == null ||
                index < 10)
                return 0;

            double atr =
                Atr(
                    bars,
                    index);

            if (atr <= 0)
                return 0;

            int quality = 40;

            Zone zone =
                FindNearestOpposingZone(
                    bars,
                    index,
                    direction,
                    atr);

            if (zone != null)
            {
                double price =
                    bars.ClosePrices[index];

                if (price >=
                        zone.Low -
                        atr *
                        ZoneProximityAtr &&
                    price <=
                        zone.High +
                        atr *
                        ZoneProximityAtr)
                    quality += 30;

                if (zone.Quality >= 80)
                    quality += 15;
            }

            if (PremiumDiscountBias(
                    bars,
                    index) ==
                direction)
                quality += 10;

            return ClampInt(
                quality,
                0,
                100);
        }

        private double ProxyExpectedValue(
            int quality,
            double rr)
        {
            double winRate =
                Clamp(
                    quality / 100.0,
                    0.05,
                    0.95);

            return
                winRate * rr -
                (1.0 - winRate);
        }

        private bool NoTradeRegimeBlocked(
            string regime,
            int quality)
        {
            if (quality <
                NoTradeMinimumSmartQuality)
                return true;

            if (BlockCompressionRegime &&
                regime == "COMPRESSION")
                return true;

            if (BlockWeakRangeTransition &&
                (regime == "RANGE" ||
                 regime == "TRANSITION") &&
                quality <
                SmartRegimeQualityFloor)
                return true;

            return false;
        }

        private int CalibratedConfidence(
            int baseConfidence,
            int direction)
        {
            if (!UseEmpiricalCalibration ||
                !EnableConfidenceCalibration ||
                !EnableOutcomeTelemetry ||
                !_directionSamples.ContainsKey(
                    direction))
                return baseConfidence;

            int totalSamples =
                _directionSamples.Values.Sum();

            if (totalSamples <
                Math.Max(
                    1,
                    CalibrationMinimumSamples))
                return baseConfidence;

            int samples =
                _directionSamples[direction];

            if (samples <
                CalibrationDirectionalMinimumSamples)
                return baseConfidence;

            int wins =
                _directionWins.ContainsKey(
                    direction)
                    ? _directionWins[direction]
                    : 0;

            double rate =
                samples <= 0
                    ? 0.5
                    : (double)wins /
                      samples;

            int adjustment =
                ClampInt(
                    (int)Math.Round(
                        (rate - 0.5) *
                        2.0 *
                        CalibrationMaxConfidenceAdjustment),
                    -CalibrationMaxConfidenceAdjustment,
                    CalibrationMaxConfidenceAdjustment);

            return ClampInt(
                baseConfidence +
                adjustment,
                0,
                100);
        }

        private void RegisterOutcome(
            int direction,
            bool win)
        {
            if (!EnableOutcomeTelemetry)
                return;

            if (!_directionSamples.ContainsKey(
                    direction))
                _directionSamples[direction] = 0;

            if (!_directionWins.ContainsKey(
                    direction))
                _directionWins[direction] = 0;

            _directionSamples[direction]++;

            if (win)
                _directionWins[direction]++;
        }

        private string CalibrationText()
        {
            int total =
                _wins +
                _losses;

            return
                total <= 0
                    ? "W0/L0"
                    : (100.0 *
                       _wins /
                       total)
                      .ToString("F0") +
                      "%";
        }

        private Prediction BuildEarlyPrediction(
            int closedM5)
        {
            Prediction p =
                new Prediction();

            if (!EnableEarlyPrediction ||
                _m5Frame == null ||
                _m15Frame == null ||
                closedM5 < 10)
                return p;

            double buy =
                _m5Frame.BullScore * 0.55 +
                _m15Frame.BullScore * 0.45;

            double sell =
                _m5Frame.BearScore * 0.55 +
                _m15Frame.BearScore * 0.45;

            if (UseLiquidityForecast)
            {
                if (_m5Frame.LiquidityBull)
                    buy += 8;

                if (_m5Frame.LiquidityBear)
                    sell += 8;
            }

            if (_m5Frame.VolumeBull)
                buy += 2;

            if (_m5Frame.VolumeBear)
                sell += 2;

            if (_m5Frame.VwapBull)
                buy += 1;

            if (_m5Frame.VwapBear)
                sell += 1;

            p.Direction =
                buy >= sell
                    ? 1
                    : -1;

            double total =
                Math.Max(
                    1,
                    buy + sell);

            p.Confidence =
                ClampInt(
                    (int)Math.Round(
                        100.0 *
                        Math.Max(
                            buy,
                            sell) /
                        total),
                    0,
                    100);

            if (p.Confidence <
                Math.Max(
                    MinimumEarlyConfidence,
                    EarlySetupConfidence))
                return p;

            double atr =
                Atr(
                    _m5Bars,
                    closedM5);

            if (atr <= 0)
                return p;

            ExecutionModel predictionExecution =
                BuildExecutionModel(
                    closedM5,
                    p.Direction);

            p.Mode =
                predictionExecution == null
                    ? CFIPClean70ExecutionMode.None
                    : predictionExecution.Mode;

            if (predictionExecution != null &&
                predictionExecution.ZoneHigh >
                predictionExecution.ZoneLow)
            {
                p.ZoneLow =
                    NormalizePrice(
                        predictionExecution.ZoneLow);

                p.ZoneHigh =
                    NormalizePrice(
                        predictionExecution.ZoneHigh);

                p.Trigger =
                    NormalizePrice(
                        predictionExecution.Trigger);

                p.Entry =
                    predictionExecution.Ready &&
                    IsFinitePositive(
                        predictionExecution.ActualEntry)
                        ? NormalizePrice(
                            predictionExecution.ActualEntry)
                        : p.Mode ==
                          CFIPClean70ExecutionMode.WaitingForTrigger
                            ? NormalizePrice(
                                predictionExecution.Trigger)
                            : NormalizePrice(
                                _m5Bars.ClosePrices[
                                    closedM5]);
            }
            else
            {
                p.Entry =
                    NormalizePrice(
                        _m5Bars.ClosePrices[
                            closedM5]);

                p.ZoneLow =
                    p.Entry -
                    atr * 0.30;

                p.ZoneHigh =
                    p.Entry +
                    atr * 0.30;

                p.Trigger =
                    p.Direction == 1
                        ? p.ZoneHigh +
                          atr * EntryBufferAtr
                        : p.ZoneLow -
                          atr * EntryBufferAtr;
            }

            string stopSource;
            int stopQuality;

            p.StopLoss =
                BuildStructuralStop(
                    closedM5,
                    p.Direction,
                    p.Entry,
                    atr,
                    out stopSource,
                    out stopQuality);

            if (!IsFinitePositive(
                    p.StopLoss) &&
                AllowExecutionFrameStopFallback)
            {
                p.StopLoss =
                    p.Direction == 1
                        ? p.Entry -
                          atr * FallbackSlAtr
                        : p.Entry +
                          atr * FallbackSlAtr;
            }

            double risk =
                Math.Abs(
                    p.Entry -
                    p.StopLoss);

            if (risk <= 0)
                return p;

            List<Level> candidates =
                BuildTargetLevels(
                    closedM5,
                    p.Direction,
                    p.Entry,
                    atr);

            List<Level> selected =
                SelectTargets(
                    candidates,
                    closedM5,
                    p.Entry,
                    risk,
                    p.Direction,
                    atr);

            p.Target1 =
                SelectTarget(
                    selected,
                    0,
                    p.Entry,
                    risk,
                    p.Direction,
                    Tp1MinimumRR);

            p.Target2 =
                SelectTarget(
                    selected,
                    1,
                    p.Entry,
                    risk,
                    p.Direction,
                    Tp2MinimumRR);

            p.Target3 =
                SelectTarget(
                    selected,
                    2,
                    p.Entry,
                    risk,
                    p.Direction,
                    Tp3MinimumRR);

            p.Target4 =
                SelectTarget(
                    selected,
                    3,
                    p.Entry,
                    risk,
                    p.Direction,
                    Tp4MinimumRR);

            p.Target =
                p.Target1;

            p.Reason =
                (p.Direction == 1
                    ? "BUY"
                    : "SELL") +
                " EARLY | CONF " +
                p.Confidence;

            return p;
        }

        private void RenderPredictionObjects(
            Prediction prediction,
            int closedM5)
        {
            RemovePredictionObjects();

            if (!ShowPredictionObjects ||
                !EnableEarlyPrediction ||
                prediction == null ||
                prediction.Direction == 0 ||
                prediction.Confidence <
                Math.Max(
                    MinimumEarlyConfidence,
                    EarlySetupConfidence))
                return;

            int start =
                MapM5ToChart(
                    Math.Max(
                        0,
                        closedM5 -
                        Math.Max(
                            2,
                            PredictionLookaheadBars / 2)),
                    Bars.Count - 1);

            int end =
                MapM5ToChart(
                    Math.Min(
                        _m5Bars.Count - 1,
                        closedM5 +
                        Math.Max(
                            2,
                            PredictionLookaheadBars)),
                    Bars.Count - 1);

            if (end <= start)
                end =
                    Math.Min(
                        Bars.Count - 1,
                        start + 4);

            if (ShowPredictionZone &&
                prediction.ZoneHigh >
                prediction.ZoneLow)
            {
                ChartRectangle zone =
                    Chart.DrawRectangle(
                        P + "PRED_ZONE",
                        start,
                        prediction.ZoneHigh,
                        end,
                        prediction.ZoneLow,
                        PredictionColor,
                        1,
                        LineStyle.Solid);

                zone.IsFilled = true;

                zone.Color =
                    Color.FromArgb(
                        35,
                        PredictionColor);

                zone.IsInteractive = false;
            }

            if (prediction.Entry > 0)
                DrawPredictionLine(
                    P + "PRED_ENTRY",
                    prediction.Entry);

            if (prediction.StopLoss > 0)
                DrawPredictionLine(
                    P + "PRED_STOP",
                    prediction.StopLoss);

            if (prediction.Trigger > 0)
                DrawPredictionLine(
                    P + "PRED_TRIGGER",
                    prediction.Trigger);

            if (ShowPredictionTargets)
            {
                if (prediction.Target1 > 0)
                    DrawPredictionLine(
                        P + "PRED_TARGET1",
                        prediction.Target1);

                if (prediction.Target2 > 0)
                    DrawPredictionLine(
                        P + "PRED_TARGET2",
                        prediction.Target2);

                if (prediction.Target3 > 0)
                    DrawPredictionLine(
                        P + "PRED_TARGET3",
                        prediction.Target3);

                if (prediction.Target4 > 0)
                    DrawPredictionLine(
                        P + "PRED_TARGET4",
                        prediction.Target4);
            }

            RenderPredictionLabels(
                prediction,
                closedM5);
        }

        private void RenderPredictionLabels(
            Prediction prediction,
            int closedM5)
        {
            if (prediction == null ||
                Bars == null ||
                Bars.Count < 2)
                return;

            int bar =
                MapM5ToChart(
                    closedM5,
                    Bars.Count - 1);

            bar =
                Math.Max(
                    0,
                    Math.Min(
                        Bars.Count - 1,
                        bar));

            DrawPlanLabel(
                P + "PRED_ENTRY_LABEL",
                "ENTRY " +
                Price(prediction.Entry),
                bar,
                prediction.Entry,
                EntryLineColor);

            if (prediction.StopLoss > 0)
                DrawPlanLabel(
                    P + "PRED_STOP_LABEL",
                    "SL " +
                    Price(prediction.StopLoss),
                    bar,
                    prediction.StopLoss,
                    SlLineColor);

            if (prediction.Trigger > 0 &&
                !SamePrice(
                    prediction.Trigger,
                    prediction.Entry))
                DrawPlanLabel(
                    P + "PRED_TRIGGER_LABEL",
                    "TRIGGER " +
                    Price(prediction.Trigger),
                    bar,
                    prediction.Trigger,
                    TriggerLineColor);

            if (ShowPredictionTargets)
            {
                if (prediction.Target1 > 0 &&
                    !SamePrice(
                        prediction.Target1,
                        prediction.Entry))
                    DrawPlanLabel(
                        P + "PRED_TARGET1_LABEL",
                        "TP1 " +
                        Price(prediction.Target1),
                        bar,
                        prediction.Target1,
                        TpLineColor);

                if (prediction.Target2 > 0 &&
                    !SamePrice(
                        prediction.Target2,
                        prediction.Target1))
                    DrawPlanLabel(
                        P + "PRED_TARGET2_LABEL",
                        "TP2 " +
                        Price(prediction.Target2),
                        bar,
                        prediction.Target2,
                        Tp2LineColor);

                if (prediction.Target3 > 0 &&
                    !SamePrice(
                        prediction.Target3,
                        prediction.Target2))
                    DrawPlanLabel(
                        P + "PRED_TARGET3_LABEL",
                        "TP3 " +
                        Price(prediction.Target3),
                        bar,
                        prediction.Target3,
                        Tp3LineColor);

                if (prediction.Target4 > 0 &&
                    !SamePrice(
                        prediction.Target4,
                        prediction.Target3))
                    DrawPlanLabel(
                        P + "PRED_TARGET4_LABEL",
                        "TP4 " +
                        Price(prediction.Target4),
                        bar,
                        prediction.Target4,
                        Tp4LineColor);
            }
        }

        private Color PredictionLineColor(
            string name)
        {
            if (name.IndexOf(
                    "PRED_TRIGGER",
                    StringComparison.OrdinalIgnoreCase) >= 0)
                return TriggerLineColor;

            if (name.IndexOf(
                    "PRED_STOP",
                    StringComparison.OrdinalIgnoreCase) >= 0)
                return SlLineColor;

            if (name.IndexOf(
                    "PRED_TARGET1",
                    StringComparison.OrdinalIgnoreCase) >= 0)
                return TpLineColor;

            if (name.IndexOf(
                    "PRED_TARGET2",
                    StringComparison.OrdinalIgnoreCase) >= 0)
                return Tp2LineColor;

            if (name.IndexOf(
                    "PRED_TARGET3",
                    StringComparison.OrdinalIgnoreCase) >= 0)
                return Tp3LineColor;

            if (name.IndexOf(
                    "PRED_TARGET4",
                    StringComparison.OrdinalIgnoreCase) >= 0)
                return Tp4LineColor;

            if (name.IndexOf(
                    "PRED_ENTRY",
                    StringComparison.OrdinalIgnoreCase) >= 0)
                return EntryLineColor;

            return PredictionColor;
        }

        private void DrawPredictionLine(
            string name,
            double price)
        {
            if (!IsFinitePositive(price) ||
                Bars == null ||
                Bars.Count < 2)
            {
                Chart.RemoveObject(name);
                return;
            }

            try
            {
                int anchor =
                    MapM5ToChart(
                        _m5Bars == null
                            ? Bars.Count - 1
                            : Math.Max(
                                1,
                                _m5Bars.Count - 1),
                        Bars.Count - 1);

                anchor =
                    Math.Max(
                        0,
                        Math.Min(
                            Bars.Count - 1,
                            anchor));

                int left =
                    Math.Max(
                        0,
                        anchor -
                        Math.Max(
                            1,
                            LineLengthBars));

                int right =
                    Math.Min(
                        Bars.Count - 1,
                        anchor +
                        Math.Max(
                            1,
                            Math.Max(
                                LineForwardBars,
                                PredictionLookaheadBars)));

                if (right <= left)
                {
                    Chart.RemoveObject(name);
                    return;
                }

                double normalized =
                    NormalizePrice(price);

                if (!IsFinitePositive(normalized))
                {
                    Chart.RemoveObject(name);
                    return;
                }

                ChartTrendLine line =
                    Chart.FindObject(name)
                    as ChartTrendLine;

                LineStyle lineStyle =
                    LineStyle.Solid;

                if (name.IndexOf(
                        "PRED_TRIGGER",
                        StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    lineStyle =
                        LineStyle.Solid;
                }
                else if (name.IndexOf(
                            "PRED_TARGET",
                            StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    lineStyle =
                        LineStyle.Solid;
                }

                if (line == null)
                {
                    ChartObject existing =
                        Chart.FindObject(name);

                    if (existing != null)
                        Chart.RemoveObject(name);

                    line =
                        Chart.DrawTrendLine(
                            name,
                            left,
                            normalized,
                            right,
                            normalized,
                            PredictionLineColor(name),
                            Math.Max(
                                1,
                                LevelLineThickness),
                            lineStyle);
                }

                if (line == null)
                    return;

                line.Time1 =
                    Bars.OpenTimes[left];
                line.Y1 =
                    normalized;
                line.Time2 =
                    Bars.OpenTimes[right];
                line.Y2 =
                    normalized;
                line.Color =
                    PredictionLineColor(name);
                line.Thickness =
                    Math.Max(
                        1,
                        LevelLineThickness);
                line.LineStyle =
                    lineStyle;
                line.ExtendToInfinity =
                    false;
                line.IsInteractive =
                    false;
            }
            catch (Exception ex)
            {
                Print(
                    "CFIP CLEAN70 prediction render failed: {0}",
                    ex.Message);
            }
        }

        private void RemovePredictionObjects()
        {
            Chart.RemoveObject(
                P + "PRED_ZONE");

            Chart.RemoveObject(
                P + "PRED_ENTRY");

            Chart.RemoveObject(
                P + "PRED_ENTRY_LABEL");

            Chart.RemoveObject(
                P + "PRED_STOP");

            Chart.RemoveObject(
                P + "PRED_STOP_LABEL");

            Chart.RemoveObject(
                P + "PRED_TRIGGER");

            Chart.RemoveObject(
                P + "PRED_TRIGGER_LABEL");

            Chart.RemoveObject(
                P + "PRED_TARGET1");

            Chart.RemoveObject(
                P + "PRED_TARGET1_LABEL");

            Chart.RemoveObject(
                P + "PRED_TARGET2");

            Chart.RemoveObject(
                P + "PRED_TARGET2_LABEL");

            Chart.RemoveObject(
                P + "PRED_TARGET3");

            Chart.RemoveObject(
                P + "PRED_TARGET3_LABEL");

            Chart.RemoveObject(
                P + "PRED_TARGET4");

            Chart.RemoveObject(
                P + "PRED_TARGET4_LABEL");
        }

        private void EmitContextAlerts(
            int closedM5)
        {
            if (_lastContextM5 ==
                closedM5 ||
                _m5Frame == null)
                return;

            _lastContextM5 =
                closedM5;

            if (AlertOnBos &&
                (_m5Frame.StructureBull ||
                 _m5Frame.StructureBear))
            {
                if (ShowContextEventMarker)
                {
                    int bar =
                        MapM5ToChart(
                            closedM5,
                            Bars.Count - 1);

                    DrawIcon(
                        P + "BOS_MARKER",
                        _m5Frame.StructureBull
                            ? ChartIconType.UpArrow
                            : ChartIconType.DownArrow,
                        bar,
                        _m5Frame.StructureBull
                            ? Bars.LowPrices[bar]
                            : Bars.HighPrices[bar],
                        _m5Frame.StructureBull
                            ? BuyArrowColor
                            : SellArrowColor);
                }

                int direction =
                    _m5Frame.StructureBull
                        ? 1
                        : -1;

                SendUnifiedAlert(
                    "BOS|" +
                    closedM5 +
                    "|" +
                    direction,
                    "CFIP CLEAN70 BOS | " +
                    (direction == 1
                        ? "BUY"
                        : "SELL"),
                    direction,
                    false);
            }

            if (AlertOnMssChoch &&
                (_m5Frame.MssBull ||
                 _m5Frame.MssBear ||
                 _m5Frame.ChochBull ||
                 _m5Frame.ChochBear))
            {
                int direction =
                    _m5Frame.MssBull ||
                    _m5Frame.ChochBull
                        ? 1
                        : -1;

                SendUnifiedAlert(
                    "MSS|" +
                    closedM5 +
                    "|" +
                    direction,
                    "CFIP CLEAN70 MSS/CHOCH | " +
                    (direction == 1
                        ? "BUY"
                        : "SELL"),
                    direction,
                    false);
            }

            if (AlertOnLiquiditySweep &&
                (_m5Frame.LiquidityBull ||
                 _m5Frame.LiquidityBear))
            {
                int direction =
                    _m5Frame.LiquidityBull
                        ? 1
                        : -1;

                SendUnifiedAlert(
                    "SWEEP|" +
                    closedM5 +
                    "|" +
                    direction,
                    "CFIP CLEAN70 LIQUIDITY SWEEP | " +
                    (direction == 1
                        ? "BUY"
                        : "SELL"),
                    direction,
                    false);
            }

            if (AlertOnEarlySetup &&
                _prediction != null &&
                _prediction.Confidence >=
                MinimumEarlyConfidence &&
                _prediction.Confidence <
                MinimumConfidence &&
                _lastEarlyAlertM5 !=
                closedM5)
            {
                SendUnifiedAlert(
                    "EARLY|" +
                    closedM5 +
                    "|" +
                    _prediction.Direction,
                    _prediction.Reason,
                    _prediction.Direction,
                    false);

                _lastEarlyAlertM5 =
                    closedM5;
            }
        }

        private bool CheckLiveReversalAgainstPlan(
            int closedM5)
        {
            if (_plan == null ||
                !_plan.IsLivePosition ||
                _m5Frame == null ||
                !EnableLiveStructuralReversal ||
                !EnableFastReversalIntelligence)
                return false;

            int opposite =
                _plan.Direction * -1;

            bool structural =
                opposite == 1
                    ? (_m5Frame.MssBull ||
                       _m5Frame.ChochBull)
                    : (_m5Frame.MssBear ||
                       _m5Frame.ChochBear);

            bool force =
                opposite == 1
                    ? (_m5Frame.DisplacementBull &&
                       _m5Frame.LiquidityBull)
                    : (_m5Frame.DisplacementBear &&
                       _m5Frame.LiquidityBear);

            int reversalConfidence =
                _reaction != null
                    ? Math.Max(
                        _reaction.Confidence,
                        _m5Frame.Quality)
                    : _m5Frame.Quality;

            if (reversalConfidence <
                Math.Max(
                    50,
                    LiveReversalMinimumConfidence))
                return false;

            if (!structural ||
                _m5Frame.Quality <
                LiveReversalStructuralScore ||
                _m5Frame.Evidence <
                LiveReversalMinimumEvidence)
                return false;

            if (RequireReversalForce &&
                !force)
                return false;

            if (!AllowReversalAgainstStaleHtf &&
                _m15Frame != null &&
                _m15Frame.Direction ==
                _plan.Direction)
                return false;

            if (RequireM15ReversalForOpposite &&
                _m15Frame != null &&
                _m15Frame.Direction !=
                opposite)
                return false;

            if (StructuralSequence(
                    _m5Bars,
                    closedM5,
                    opposite) <
                MinimumOppositeM5Structure)
                return false;

            if (PreventRapidDirectionFlip &&
                _lastSignalM5 >= 0 &&
                closedM5 -
                _lastSignalM5 <
                OppositeSignalCooldownM5)
                return false;

            int flipBars =
                Math.Max(
                    1,
                    SmartFlipConfirmationBars);

            if (flipBars > 1 &&
                !StableDirection(
                    _m5Bars,
                    closedM5,
                    opposite,
                    flipBars))
                return false;

            SendUnifiedAlert(
                "REVERSAL|" +
                closedM5 +
                "|" +
                opposite,
                "CFIP CLEAN70 ACTIVE PLAN REVERSAL | " +
                (opposite == 1
                    ? "BUY"
                    : "SELL") +
                " REVERSAL | Q " +
                _m5Frame.Quality,
                opposite,
                true);

            Position livePosition =
                GetManagedLivePositionForPlan();

            if (livePosition != null)
            {
                if (!EnableReversalProtectionClose ||
                    livePosition.NetProfit <= 0)
                {
                    SetAutoTradingState(
                        "BLOCKED",
                        "REVERSAL DETECTED • POSITION RETAINED");
                    return false;
                }

                double protectedProfit =
                    livePosition.NetProfit;

                SetLifecycleState(
                    CFIPClean70LifecycleState.ExitRequested,
                    "ACTIVE PLAN REVERSAL");

                if (!TryClosePosition(
                        livePosition,
                        "ACTIVE PLAN REVERSAL"))
                {
                    SetLifecycleState(
                        CFIPClean70LifecycleState.RecoveryRequired,
                        "ACTIVE PLAN REVERSAL • EXIT REJECTED");

                    SetAutoTradingState(
                        "ERROR",
                        "REVERSAL CLOSE REJECTED");

                    return false;
                }

                _lastExitM5 =
                    Math.Max(
                        _lastExitM5,
                        closedM5);

                SetAutoTradingState(
                    "EXECUTED",
                    "REVERSAL EXIT REQUESTED #" +
                    livePosition.Id);

                SendUnifiedAlert(
                    "REVERSAL-CLOSE|" +
                    livePosition.Id,
                    "CFIP CLEAN70 ACTIVE PLAN REVERSAL • EXIT REQUESTED | #" +
                    livePosition.Id +
                    " | protected +" +
                    protectedProfit.ToString("F2"),
                    _plan.Direction,
                    true);

                return true;
            }

            SetLifecycleState(
                CFIPClean70LifecycleState.Closed,
                "REVERSAL • POSITION ABSENT");

            _plan = null;
            _executionModel = null;
            RemovePlanObjects();

            return true;
        }

        private Position GetManagedLivePositionForPlan()
        {
            if (_plan == null ||
                !_plan.IsLivePosition ||
                _plan.PositionId <= 0)
                return null;

            foreach (Position position in Positions)
            {
                if (position != null &&
                    position.SymbolName == SymbolName &&
                    position.Id == _plan.PositionId)
                    return position;
            }

            return null;
        }

        private double CalculateAggressiveVolume(
            double stopPips)
        {
            try
            {
                if (stopPips <= 0)
                    return 0;

                double amount =
                    Math.Max(
                        0,
                        Account.Equity) *
                    EffectiveAggressiveRiskPercent() /
                    100.0;

                if (amount <= 0)
                    return 0;

                double volume =
                    Symbol.VolumeForFixedRisk(
                        amount,
                        stopPips,
                        RoundingMode.Down);

                return
                    Symbol.NormalizeVolumeInUnits(
                        volume,
                        RoundingMode.Down);
            }
            catch
            {
                return 0;
            }
        }

        

                private void ProtectBrokerPositions(
            int closedM5)
        {
            if (!AutoBrokerProtection &&
                !AutoProtectBrokerPositions)
                return;

            if (_plan == null ||
                !_plan.IsLivePosition ||
                _lifecycleState ==
                    CFIPClean70LifecycleState.ExitRequested)
                return;

            if ((DateTime.UtcNow -
                 _lastBrokerModifyUtc).TotalMilliseconds <
                Math.Max(
                    100,
                    BrokerModifyCooldownMs))
                return;

            string label =
                string.IsNullOrWhiteSpace(
                    ManagedPositionLabel)
                    ? NormalizeLabel()
                    : ManagedPositionLabel.Trim();

            foreach (Position position in Positions)
            {
                if (position == null ||
                    position.SymbolName !=
                    SymbolName)
                    continue;

                bool byPlanId =
                    _plan.PositionId > 0 &&
                    position.Id ==
                    _plan.PositionId;

                bool byManagedLabel =
                    AutoProtectBrokerPositions &&
                    position.Label ==
                    label;

                if (!byPlanId &&
                    !byManagedLabel)
                    continue;

                bool mutationRequired = false;
                bool mutationSucceeded = true;

                int positionDirection =
                    position.TradeType == TradeType.Buy
                        ? 1
                        : -1;

                if (IsValidStop(
                        positionDirection,
                        position.EntryPrice,
                        _plan.Stop))
                {
                    double normalizedStop =
                        NormalizePrice(_plan.Stop);

                    bool materiallyDifferent =
                        !position.StopLoss.HasValue ||
                        Math.Abs(
                            position.StopLoss.Value -
                            normalizedStop) >=
                        Math.Max(
                            Symbol.TickSize,
                            Symbol.PipSize * 0.25);

                    if (materiallyDifferent)
                    {
                        mutationRequired = true;
                        mutationSucceeded =
                            TryModifyStopLoss(
                                position,
                                normalizedStop,
                                "LIVE PROTECTION • SL") &&
                            mutationSucceeded;
                    }
                }

                if (SyncBrokerTakeProfit)
                {
                    double target =
                        AutoTarget(
                            _plan,
                            EffectiveAutoTpStage());

                    if (IsValidTarget(
                            positionDirection,
                            position.EntryPrice,
                            target))
                    {
                        bool move = true;

                        if (PreventBrokerTpBackwardMove &&
                            position.TakeProfit.HasValue)
                        {
                            double current =
                                position.TakeProfit.Value;

                            move =
                                positionDirection == 1
                                    ? target >= current
                                    : target <= current;
                        }

                        double normalizedTarget =
                            NormalizePrice(target);

                        bool materiallyDifferent =
                            move &&
                            (!position.TakeProfit.HasValue ||
                             Math.Abs(
                                 position.TakeProfit.Value -
                                 normalizedTarget) >=
                             Math.Max(
                                 Symbol.TickSize,
                                 Symbol.PipSize * 0.25));

                        if (materiallyDifferent)
                        {
                            mutationRequired = true;
                            mutationSucceeded =
                                TryModifyTakeProfit(
                                    position,
                                    normalizedTarget,
                                    "LIVE PROTECTION • TP") &&
                                mutationSucceeded;
                        }
                    }
                }

                if (mutationRequired &&
                    !mutationSucceeded)
                {
                    _brokerProtectionRecoveryRequired = true;

                    SetLifecycleState(
                        CFIPClean70LifecycleState.RecoveryRequired,
                        "BROKER PROTECTION MUTATION REJECTED");

                    SendUnifiedAlert(
                        "PROTECTION-SYNC-FAILED|" +
                        position.Id,
                        "CFIP CLEAN70 BROKER PROTECTION SYNC REJECTED | #" +
                        position.Id,
                        positionDirection,
                        true);
                }
                else if (!_brokerProtectionRecoveryRequired)
                {
                    SetLifecycleState(
                        CFIPClean70LifecycleState.LivePosition,
                        "LIVE POSITION • BROKER STATE SYNCHRONIZED");
                }

                _lastBrokerModifyUtc =
                    DateTime.UtcNow;

                break;
            }
        }


        private void TryAggressiveAutoTrade(
            int closedM5)
        {
            if (!AutoTradingEnabled ||
                !EnableAggressiveAutoEntry ||
                _lifecycleState ==
                    CFIPClean70LifecycleState.ExitRequested ||
                _plan != null ||
                _reaction == null ||
                !_reaction.EntryAllowed ||
                _reaction.Direction == 0)
                return;

            if (GetManagedPendingOrder() != null)
            {
                _autoExecutionBlockReason =
                    "PENDING ORDER EXISTS";
                return;
            }

            if (DailyLossLimitHit(
                    DateTime.UtcNow))
                return;

            if (OneOrderPerSignal &&
                _lastAutoM5 ==
                closedM5)
                return;

            if (_reaction.Confidence <
                AggressiveMinimumConfidence ||
                _reaction.IndependentEvidence <
                AggressiveMinimumEvidence)
                return;

            if (AggressiveRequireSmartAgreement &&
                (_decision == null ||
                 _decision.Direction !=
                 _reaction.Direction ||
                 _decision.SmartQuality <
                 AggressiveMinimumSmartQuality))
                return;

            string aggressiveSuitabilityReason;

            if (!PassesMarketSuitability(
                    closedM5,
                    _reaction.Direction,
                    out aggressiveSuitabilityReason))
            {
                SetAutoTradingState(
                    "BLOCKED",
                    "AGGRESSIVE • " +
                    aggressiveSuitabilityReason);
                return;
            }

            if (ManagedPositionCount() >=
                Math.Max(
                    1,
                    MaximumOpenPositions))
                return;

            double entry =
                NormalizePrice(
                    _reaction.Direction == 1
                        ? Symbol.Ask
                        : Symbol.Bid);

            double atr =
                Atr(
                    _m5Bars,
                    closedM5);

            if (atr <= 0)
            {
                _autoExecutionBlockReason =
                    "AGGRESSIVE • ATR UNAVAILABLE";
                return;
            }

            string source;
            int quality;

            double stop =
                BuildStructuralStop(
                    closedM5,
                    _reaction.Direction,
                    entry,
                    atr,
                    out source,
                    out quality);

            if (!IsFinitePositive(stop))
            {
                _autoExecutionBlockReason =
                    "AGGRESSIVE • INVALID SL";
                return;
            }

            double target =
                SelectStructuralAutoTarget(
                    closedM5,
                    _reaction.Direction,
                    entry,
                    stop,
                    atr,
                    AggressiveTpStage);

            if (!IsAutoPlanValid(
                    _reaction.Direction,
                    entry,
                    stop,
                    target))
            {
                SetAutoTradingState(
                    "BLOCKED",
                    "NO VALID STRUCTURAL TARGET");
                return;
            }

            double stopPips =
                Math.Abs(
                    entry -
                    stop) /
                Symbol.PipSize;

            double tpPips =
                Math.Abs(
                    target -
                    entry) /
                Symbol.PipSize;

            double effectiveStopPips =
                stopPips;

            if (IncludeSpreadInRiskSizing)
                effectiveStopPips +=
                    Math.Max(
                        0,
                        (Symbol.Ask - Symbol.Bid) /
                        Math.Max(
                            Symbol.PipSize,
                            1e-9));

            double volume =
                CalculateAggressiveVolume(
                    effectiveStopPips);

            volume =
                AdjustVolumeForMargin(
                    _reaction.Direction == 1
                        ? TradeType.Buy
                        : TradeType.Sell,
                    volume);

            if (volume <
                Symbol.VolumeInUnitsMin)
            {
                _autoExecutionBlockReason =
                    "AGGRESSIVE • VOLUME BELOW MINIMUM";
                return;
            }

            try
            {
                TradeType type =
                    _reaction.Direction == 1
                        ? TradeType.Buy
                        : TradeType.Sell;

                if (!EnsureTradingPermission())
                {
                    _autoExecutionBlockReason =
                        "TRADING PERMISSION";
                    SetAutoTradingState(
                        "BLOCKED",
                        "TRADING PERMISSION NOT GRANTED");
                    return;
                }

                string guardReason;

                if (!PassesAutoTradeSafetyGuards(
                        type,
                        volume,
                        out guardReason))
                {
                    SetAutoTradingState(
                        "BLOCKED",
                        guardReason);
                    return;
                }

                TradeResult result =
                    ExecuteMarketOrder(
                        type,
                        SymbolName,
                        volume,
                        NormalizeLabel(),
                        stopPips,
                        tpPips,
                        "CFIP SMART70",
                        false);

                if (result == null ||
                    !result.IsSuccessful ||
                    result.Position == null)
                {
                    _autoExecutionBlockReason =
                        result != null &&
                        result.Error.HasValue
                            ? "AGGRESSIVE • " +
                              result.Error.Value.ToString()
                            : "AGGRESSIVE • TRADE REJECTED";
                    SetAutoTradingState(
                        "ERROR",
                        _autoExecutionBlockReason);
                    return;
                }

                _lastAutoM5 =
                    closedM5;

                _autoExecutionBlockReason =
                    "EXECUTED";

                _plan =
                    CreateManagedPlanFromExecution(
                        _reaction.Direction,
                        result.Position.EntryPrice,
                        stop,
                        target,
                        closedM5,
                        result.Position.VolumeInUnits);

                _plan.PositionId =
                    result.Position.Id;

                EnrichLivePlanTargets(closedM5);

                if (AutoBrokerProtection)
                {
                    EnsureBrokerProtectionForPosition(
                        result.Position,
                        stop,
                        target,
                        "AGGRESSIVE ENTRY",
                        _reaction.Direction);
                }

                SendUnifiedAlert(
                    "AUTO-REACTION|" +
                    closedM5,
                    "CFIP CLEAN70 AUTO REACTION " +
                    (_reaction.Direction == 1
                        ? "BUY"
                        : "SELL") +
                    " EXECUTED | #" +
                    result.Position.Id +
                    " | ENTRY " +
                    Price(
                        result.Position.EntryPrice) +
                    " | SL " +
                    Price(stop) +
                    " | TP " +
                    Price(target),
                    _reaction.Direction,
                    true);
            }
            catch (Exception ex)
            {
                Print(
                    "CFIP CLEAN70 aggressive auto trade failed: {0}",
                    ex.Message);
            }
        }

        private void MonitorOutcome(
            int closedM5)
        {
            if (!EnableOutcomeTelemetry ||
                _plan == null ||
                !_plan.IsLivePosition ||
                OutcomeMaximumM5Bars <= 0)
                return;

            if (_outcomeTelemetryTimedOut ||
                closedM5 -
                _plan.CreatedM5 <
                OutcomeMaximumM5Bars)
                return;

            // Telemetry timeout is an observation boundary, not a position
            // lifecycle boundary. Keep broker ownership and live protection
            // active until the position is actually closed.
            _outcomeTelemetryTimedOut = true;

            SendUnifiedAlert(
                "OUTCOME-TIMEOUT|" +
                _plan.PositionId,
                "CFIP CLEAN70 OUTCOME WINDOW ELAPSED | POSITION #" +
                _plan.PositionId +
                " remains under live management",
                _plan.Direction,
                false);
        }

        private void DrawOutcomeMarker(
            string label,
            double price,
            bool success)
        {
            if (!ShowContextEventMarker ||
                Bars == null ||
                Bars.Count < 2 ||
                !IsFinitePositive(price))
                return;

            try
            {
                string name =
                    P +
                    "OUTCOME_" +
                    label.Replace(
                        " ",
                        "_") +
                    "_" +
                    Bars.Count +
                    "_" +
                    _outcomeSequence++;

                ChartText marker =
                    Chart.DrawText(
                        name,
                        label,
                        Bars.OpenTimes[
                            Bars.Count - 1],
                        NormalizePrice(price),
                        success
                            ? TpLineColor
                            : SlLineColor);

                marker.FontSize =
                    Math.Max(
                        8,
                        PanelFontSize);

                marker.IsBold = true;
                marker.IsInteractive = false;

                _outcomeSequence =
                    Math.Max(
                        0,
                        _outcomeSequence);
            }
            catch
            {
            }
        }

        // ============================================================
        #endregion

        #region v69 Runtime Execution Controls
        // ============================================================

        private bool HasTradingPermission()
        {
            try
            {
                return Permissions.TradingPermission.IsAllowed;
            }
            catch
            {
                return false;
            }
        }

        private bool EnsureTradingPermission()
        {
            if (HasTradingPermission())
            {
                _lastTradingPermissionRequestUtc =
                    DateTime.MinValue;
                return true;
            }

            DateTime nowUtc =
                DateTime.UtcNow;

            if ((nowUtc -
                 _lastTradingPermissionRequestUtc).TotalSeconds < 3)
                return false;

            _lastTradingPermissionRequestUtc =
                nowUtc;

            try
            {
                bool granted =
                    Permissions.TradingPermission.Request();

                return
                    granted &&
                    HasTradingPermission();
            }
            catch (Exception ex)
            {
                Print(
                    "CFIP CLEAN70 TradingPermission request failed: {0}",
                    ex.Message);
                return false;
            }
        }

        private bool IsManagedPosition(Position position)
        {
            if (position == null ||
                position.SymbolName != SymbolName)
                return false;

            if (!ManagedActionsOnly)
                return true;

            string managedLabel =
                string.IsNullOrWhiteSpace(
                    ManagedPositionLabel)
                    ? NormalizeLabel()
                    : ManagedPositionLabel.Trim();

            return string.Equals(
                position.Label,
                managedLabel,
                StringComparison.Ordinal);
        }

        private bool IsManagedPendingOrder(PendingOrder order)
        {
            return
                order != null &&
                order.SymbolName == SymbolName &&
                string.Equals(
                    order.Label,
                    PendingOrderLabel(),
                    StringComparison.Ordinal);
        }

        private string PendingOrderLabel()
        {
            return NormalizeLabel() + "-PENDING";
        }

        private int ManagedPendingOrderCount()
        {
            int count=0;

            foreach (PendingOrder order in PendingOrders)
            {
                if (IsManagedPendingOrder(order))
                    count++;
            }

            return count;
        }

        private void EnrichLivePlanTargets(int closedM5)
        {
            if (_plan == null ||
                !_plan.IsLivePosition ||
                _m5Bars == null)
                return;

            int index =
                Math.Max(
                    1,
                    Math.Min(
                        closedM5,
                        _m5Bars.Count - 2));

            double atr =
                Atr(
                    _m5Bars,
                    index);

            if (atr <= 0)
                return;

            List<Level> levels =
                BuildTargetLevels(
                    index,
                    _plan.Direction,
                    _plan.Entry,
                    atr);

            List<Level> selected =
                SelectTargets(
                    levels,
                    index,
                    _plan.Entry,
                    Math.Max(
                        Symbol.PipSize,
                        _plan.Risk),
                    _plan.Direction,
                    atr);

            double baseTarget = _plan.Tp1;

            _plan.Tp2 =
                FindFurtherLiveTarget(
                    selected,
                    index,
                    baseTarget,
                    atr);

            double base2 =
                _plan.Tp2 > 0
                    ? _plan.Tp2
                    : baseTarget;

            _plan.Tp3 =
                FindFurtherLiveTarget(
                    selected,
                    index,
                    base2,
                    atr);

            double base3 =
                _plan.Tp3 > 0
                    ? _plan.Tp3
                    : base2;

            _plan.Tp4 =
                FindFurtherLiveTarget(
                    selected,
                    index,
                    base3,
                    atr);

            ApplyTargetMeta(
                levels,
                _plan.Tp2,
                atr,
                out _plan.Tp2Source,
                out _plan.Tp2Quality);

            ApplyTargetMeta(
                levels,
                _plan.Tp3,
                atr,
                out _plan.Tp3Source,
                out _plan.Tp3Quality);

            ApplyTargetMeta(
                levels,
                _plan.Tp4,
                atr,
                out _plan.Tp4Source,
                out _plan.Tp4Quality);

            _plan.HtfTargetCount =
                CountHtfTargetsInPlan(
                    _plan);

            RecalculatePlanRR();
        }

        private double FindFurtherLiveTarget(
            List<Level> selected,
            int index,
            double previous,
            double atr)
        {
            if (selected == null ||
                !IsFinitePositive(previous) ||
                _plan == null)
                return 0;

            double best = 0;
            double bestScore = double.MinValue;

            foreach (Level level in selected)
            {
                if (level == null ||
                    !IsFinitePositive(level.Price) ||
                    level.Score < SmartTargetQuality)
                    continue;

                bool farther =
                    _plan.Direction == 1
                        ? level.Price > previous + Symbol.PipSize
                        : level.Price < previous - Symbol.PipSize;

                if (!farther)
                    continue;

                double rr =
                    Math.Abs(
                        level.Price -
                        _plan.Entry) /
                    Math.Max(
                        Symbol.PipSize,
                        _plan.Risk);

                if (rr >
                    Math.Max(
                        0,
                        MaximumRewardRR))
                    continue;

                if (RejectTargetObstacle &&
                    HasTargetObstacle(
                        _m5Bars,
                        index,
                        _plan.Direction,
                        _plan.Entry,
                        level.Price,
                        atr))
                    continue;

                double score =
                    level.Score +
                    (IsHtfTimeframe(level.Timeframe)
                        ? HtfRewardBonus
                        : 0);

                if (score > bestScore)
                {
                    bestScore = score;
                    best = NormalizePrice(level.Price);
                }
            }

            return best;
        }

        private Plan CreateManagedPlanFromExecution(
            int direction,
            double entry,
            double stop,
            double target,
            int createdM5,
            double volume,
            CFIPClean70ExecutionMode entryMode =
                CFIPClean70ExecutionMode.BreakoutMarket)
        {
            double risk =
                Math.Abs(
                    entry -
                    stop);

            return new Plan
            {
                Direction = direction,
                EntryMode = entryMode,
                Entry = NormalizePrice(entry),
                IdealEntry = NormalizePrice(entry),
                Stop = NormalizePrice(stop),
                Tp1 = NormalizePrice(target),
                Tp2 = 0,
                Tp3 = 0,
                Tp4 = 0,
                Risk = Math.Max(
                    Symbol.PipSize,
                    risk),
                Tp1RR =
                    risk > 0
                        ? Math.Abs(
                            target - entry) /
                          risk
                        : 0,
                StopSource = "LIVE / STRUCTURAL",
                StopQuality = 100,
                Tp1Source = "LIVE / ADAPTIVE",
                Tp1Quality = 100,
                CreatedM5 = createdM5,
                OriginalVolume = volume,
                IsLivePosition = true
            };
        }

                private void RecoverManagedLivePlan(int closedM5)
        {
            if (_plan != null &&
                _plan.IsLivePosition)
                return;

            foreach (Position position in Positions)
            {
                if (!IsManagedPosition(position))
                    continue;

                int direction =
                    position.TradeType == TradeType.Buy
                        ? 1
                        : -1;

                double entry =
                    position.EntryPrice;

                double atr =
                    _m5Bars == null
                        ? 0
                        : Atr(
                            _m5Bars,
                            Math.Max(
                                1,
                                closedM5));

                if (!IsFinitePositive(atr))
                {
                    atr =
                        Math.Max(
                            Symbol.PipSize * 20,
                            Math.Abs(
                                Symbol.Ask -
                                Symbol.Bid) *
                        10);
                }

                double stop =
                    position.StopLoss.HasValue &&
                    IsValidStop(
                        direction,
                        entry,
                        position.StopLoss.Value)
                        ? NormalizePrice(
                            position.StopLoss.Value)
                        : 0;

                double target =
                    position.TakeProfit.HasValue &&
                    IsValidTarget(
                        direction,
                        entry,
                        position.TakeProfit.Value)
                        ? NormalizePrice(
                            position.TakeProfit.Value)
                        : 0;

                bool protectionMissing =
                    !IsFinitePositive(stop) ||
                    !IsFinitePositive(target);

                if (!IsFinitePositive(stop))
                {
                    string stopSource;
                    int stopQuality;

                    stop =
                        BuildStructuralStop(
                            Math.Max(1, closedM5),
                            direction,
                            entry,
                            atr,
                            out stopSource,
                            out stopQuality);

                    if (!IsValidStop(
                            direction,
                            entry,
                            stop))
                    {
                        double fallbackRisk =
                            atr *
                            Math.Max(
                                0.10,
                                FallbackSlAtr);

                        stop =
                            direction == 1
                                ? entry - fallbackRisk
                                : entry + fallbackRisk;

                        stop =
                            NormalizePrice(stop);
                    }
                }

                if (!IsFinitePositive(target))
                {
                    target =
                        SelectStructuralAutoTarget(
                            Math.Max(1, closedM5),
                            direction,
                            entry,
                            stop,
                            atr,
                            EffectiveAutoTpStage());

                    if (!IsValidTarget(
                            direction,
                            entry,
                            target))
                    {
                        double risk =
                            Math.Max(
                                Symbol.PipSize,
                                Math.Abs(
                                    entry -
                                    stop));

                        double distance =
                            risk *
                            Math.Max(
                                1.0,
                                MinimumRequiredRR());

                        target =
                            direction == 1
                                ? entry + distance
                                : entry - distance;

                        target =
                            NormalizePrice(target);
                    }
                }

                if (!IsExecutionPlanConsistent(
                        direction,
                        entry,
                        stop,
                        target))
                {
                    _brokerProtectionRecoveryRequired = true;

                    SetLifecycleState(
                        CFIPClean70LifecycleState.RecoveryRequired,
                        "STARTUP RECOVERY FAILED");
                    continue;
                }

                _plan =
                    CreateManagedPlanFromExecution(
                        direction,
                        entry,
                        stop,
                        target,
                        Math.Max(1, closedM5),
                        position.VolumeInUnits);

                _plan.PositionId =
                    position.Id;

                _activeBrokerStop =
                    position.StopLoss.HasValue
                        ? NormalizePrice(
                            position.StopLoss.Value)
                        : 0;

                _activeBrokerTarget =
                    position.TakeProfit.HasValue
                        ? NormalizePrice(
                            position.TakeProfit.Value)
                        : 0;

                _brokerProtectionRecoveryRequired =
                    protectionMissing;

                SetLifecycleState(
                    protectionMissing
                        ? CFIPClean70LifecycleState.RecoveryRequired
                        : CFIPClean70LifecycleState.LivePosition,
                    protectionMissing
                        ? "STARTUP RECOVERY • BROKER PROTECTION MISSING"
                        : "STARTUP RECOVERY • LIVE");

                EnrichLivePlanTargets(closedM5);

                if (AutoProtectBrokerPositions ||
                    AutoBrokerProtection)
                {
                    bool protectionOk =
                        EnsureBrokerProtectionForPosition(
                            position,
                            stop,
                            target,
                            "STARTUP RECOVERY",
                            direction);

                    if (protectionOk)
                    {
                        _activeBrokerStop = stop;
                        _activeBrokerTarget = target;
                        _brokerProtectionRecoveryRequired = false;

                        SetLifecycleState(
                            CFIPClean70LifecycleState.LivePosition,
                            "STARTUP RECOVERY • PROTECTED");
                    }
                }

                _lastMarket =
                    direction == 1
                        ? Symbol.Bid
                        : Symbol.Ask;

                _peakPrice =
                    _lastMarket;

                break;
            }
        }


        private void CheckAutoTradingDisabledReminder(int closedM5)
        {
            if (_decision == null ||
                _decision.Direction == 0 ||
                !_decision.EntryAllowed ||
                _lastAutoTradingReminderM5 == closedM5)
                return;

            int confidenceFloor =
                Math.Max(
                    MinimumAutoConfidence,
                    HighConfidenceThreshold);

            if (_decision.Confidence < confidenceFloor ||
                _decision.SmartQuality < MinimumAutoSmartQuality ||
                _decision.IndependentEvidence < MinimumIndependentEvidence)
                return;

            _lastAutoTradingReminderM5 =
                closedM5;

            SetAutoTradingState(
                "OFF",
                "HIGH-CONFIDENCE SIGNAL READY");

            ShowPopup(
                "CFIP SMART\n" +
                (_decision.Direction == 1 ? "BUY" : "SELL") +
                " setup is confirmed while Auto Trading is OFF.\n" +
                "Review ENTRY / SL / TP before taking any manual action.");

            SendUnifiedAlert(
                "AUTOOFF|" + closedM5,
                "CFIP AUTO TRADING OFF | " +
                (_decision.Direction == 1 ? "BUY" : "SELL") +
                " SIGNAL READY | CONF " +
                _decision.Confidence +
                " | SMART " +
                _decision.SmartQuality,
                _decision.Direction,
                true);
        }

        private bool TrendContinuationStrong()
        {
            if (_decision == null ||
                _m5Frame == null ||
                _m15Frame == null ||
                _decision.Direction == 0)
                return false;

            return
                _m5Frame.Direction == _decision.Direction &&
                _m15Frame.Direction == _decision.Direction &&
                _decision.StructuralConfirmations >=
                Math.Max(
                    3,
                    MinimumStructuralConfirmations) &&
                _decision.Confidence >=
                PendingMinimumConfidence &&
                _decision.SmartQuality >=
                PendingMinimumSmartQuality &&
                _decision.TimeframeAgreement >=
                PendingMinimumTrendQuality;
        }

        private bool ReversalSetupStrong()
        {
            if (_reaction == null ||
                _decision == null ||
                _reaction.Direction == 0 ||
                _decision.Direction == 0)
                return false;

            return
                _reaction.Direction != _decision.Direction &&
                _reaction.EntryAllowed &&
                _reaction.Confidence >=
                Math.Max(
                    PendingMinimumConfidence,
                    ReversalProtectionMinimumQuality) &&
                _reaction.IndependentEvidence >=
                Math.Max(
                    2,
                    ReversalCloseMinimumEvidence);
        }

        private bool PendingModeAllowsStop()
        {
            return
                PendingOrderMode == CFIPClean70PendingOrderMode.Adaptive ||
                PendingOrderMode == CFIPClean70PendingOrderMode.ContinuationStop ||
                PendingOrderMode == CFIPClean70PendingOrderMode.Both;
        }

        private bool PendingModeAllowsLimit()
        {
            return
                PendingOrderMode == CFIPClean70PendingOrderMode.Adaptive ||
                PendingOrderMode == CFIPClean70PendingOrderMode.ReversalLimit ||
                PendingOrderMode == CFIPClean70PendingOrderMode.Both;
        }

        private void TrySmartPendingOrders(int closedM5)
        {
            _lastAutoOrderAttemptUtc =
                DateTime.UtcNow;

            if (AutomaticOrdersEnabled &&
                DailyLossLimitHit(DateTime.UtcNow))
            {
                _autoOrdersBlockReason =
                    "DAILY LOSS LIMIT";

                CancelAllOrders();
                return;
            }

            if (!AutomaticOrdersEnabled)
            {
                _autoOrdersBlockReason =
                    "DISABLED";
                return;
            }

            if (GetManagedPosition() != null)
            {
                _autoOrdersBlockReason =
                    "MANAGED POSITION ACTIVE";
                return;
            }

            if (_plan != null &&
                !_plan.IsLivePosition)
            {
                _autoOrdersBlockReason =
                    "MARKET PLAN ACTIVE";
                return;
            }

            CleanupPendingOrdersIfNeeded(closedM5);

            PendingOrder existingPending =
                GetManagedPendingOrder();

            if (existingPending != null)
            {
                _autoOrdersBlockReason =
                    "PENDING ORDER EXISTS";
                return;
            }

            if (ManagedPositionCount() >=
                Math.Max(1, MaximumOpenPositions))
            {
                _autoOrdersBlockReason =
                    "MAX OPEN POSITIONS";
                return;
            }

            if (ManagedPendingOrderCount() > 0)
            {
                _autoOrdersBlockReason =
                    "PENDING ORDER ALREADY EXISTS";
                return;
            }

            if (DailyLossLimitHit(DateTime.UtcNow))
            {
                _autoOrdersBlockReason =
                    "DAILY LOSS LIMIT";
                return;
            }

            int pendingDirection =
                TrendContinuationStrong()
                    ? _decision.Direction
                    : ReversalSetupStrong()
                        ? _reaction.Direction
                        : 0;

            if (pendingDirection != 0)
            {
                string pendingSuitabilityReason;

                if (!PassesMarketSuitability(
                        closedM5,
                        pendingDirection,
                        out pendingSuitabilityReason))
                {
                    _autoOrdersBlockReason =
                        "SUITABILITY • " +
                        pendingSuitabilityReason;
                    return;
                }
            }

            if (pendingDirection != 0 &&
                !EnsureTradingPermission())
            {
                _autoOrdersBlockReason = "TRADING PERMISSION";
                return;
            }

            if (TrendContinuationStrong() &&
                PendingModeAllowsStop())
            {
                if (PlaceContinuationStop(closedM5))
                {
                    _autoOrdersBlockReason =
                        "ORDER PLACED";
                    return;
                }
            }

            if (ReversalSetupStrong() &&
                PendingModeAllowsLimit())
            {
                CheckReversalProtection();

                if (PlaceReversalLimit(closedM5))
                {
                    _autoOrdersBlockReason =
                        "ORDER PLACED";
                    return;
                }
            }

            if (pendingDirection == 0)
            {
                _autoOrdersBlockReason =
                    "NO ELIGIBLE PENDING SETUP";
                return;
            }

            if (string.IsNullOrWhiteSpace(
                    _autoOrdersBlockReason) ||
                _autoOrdersBlockReason ==
                    "NOT EVALUATED")
            {
                _autoOrdersBlockReason =
                    "PENDING EXECUTION BLOCKED";
            }
        }

        private double EffectiveRiskStopPips(double stopPips)
        {
            double value =
                Math.Max(
                    0,
                    stopPips);

            if (IncludeSpreadInRiskSizing)
                value +=
                    Math.Max(
                        0,
                        (Symbol.Ask - Symbol.Bid) /
                        Math.Max(
                            Symbol.PipSize,
                            1e-9));

            return value;
        }

        private bool IsValidPendingEntry(
            int direction,
            double price,
            bool stopOrder)
        {
            if (!IsFinitePositive(price))
                return false;

            double tolerance =
                Math.Max(
                    Symbol.TickSize,
                    Symbol.PipSize * 0.10);

            if (direction == 1)
                return stopOrder
                    ? price > Symbol.Ask + tolerance
                    : price < Symbol.Ask - tolerance;

            if (direction == -1)
                return stopOrder
                    ? price < Symbol.Bid - tolerance
                    : price > Symbol.Bid + tolerance;

            return false;
        }

        private bool PlaceContinuationStop(int closedM5)
        {
            int direction =
                _decision.Direction;

            double atr =
                Atr(
                    _m5Bars,
                    closedM5);

            if (atr <= 0)
            {
                _autoOrdersBlockReason =
                    "PENDING STOP • ATR UNAVAILABLE";
                return false;
            }



            if (_executionModel == null ||
                _executionModel.Direction != direction ||
                _executionModel.Mode !=
                    CFIPClean70ExecutionMode.WaitingForTrigger)
            {
                _autoOrdersBlockReason =
                    "CONTINUATION STOP NOT ARMED";
                return false;
            }

            double trigger =
                _executionModel != null &&
                IsFinitePositive(
                    _executionModel.Trigger)
                    ? _executionModel.Trigger
                    : direction == 1
                        ? Highest(
                              _m5Bars,
                              Math.Max(
                                  1,
                                  closedM5 - 6),
                              closedM5 - 1) +
                          atr *
                          Math.Max(
                              0.02,
                              PendingEntryBufferAtr)
                        : Lowest(
                              _m5Bars,
                              Math.Max(
                                  1,
                                  closedM5 - 6),
                              closedM5 - 1) -
                          atr *
                          Math.Max(
                              0.02,
                              PendingEntryBufferAtr);

            trigger =
                NormalizePrice(
                    trigger);

            if (!IsValidPendingEntry(
                    direction,
                    trigger,
                    true))
            {
                _autoOrdersBlockReason =
                    "PENDING STOP • INVALID TRIGGER";
                return false;
            }

            string source;
            int quality;

            double stop =
                BuildStructuralStop(
                    closedM5,
                    direction,
                    trigger,
                    atr,
                    out source,
                    out quality);

            if (!IsFinitePositive(stop))
            {
                _autoOrdersBlockReason =
                    "PENDING STOP • INVALID SL";
                return false;
            }

            double target =
                SelectStructuralAutoTarget(
                    closedM5,
                    direction,
                    trigger,
                    stop,
                    atr,
                    EffectiveAutoTpStage());

            if (!IsAutoPlanValid(
                    direction,
                    trigger,
                    stop,
                    target))
            {
                _autoOrdersBlockReason =
                    "PENDING STOP • INVALID SL/TP";
                return false;
            }

            double stopPips =
                Math.Abs(
                    trigger -
                    stop) /
                Symbol.PipSize;

            double targetPips =
                Math.Abs(
                    target -
                    trigger) /
                Symbol.PipSize;

            double volume =
                CalculateVolume(
                    EffectiveRiskStopPips(
                        stopPips));

            volume =
                AdjustVolumeForMargin(
                    direction == 1
                        ? TradeType.Buy
                        : TradeType.Sell,
                    volume);

            if (volume <
                Symbol.VolumeInUnitsMin)
            {
                _autoOrdersBlockReason =
                    "PENDING STOP • VOLUME BELOW MINIMUM";
                return false;
            }

            string reason;

            if (!PassesAutoTradeSafetyGuards(
                    direction == 1
                        ? TradeType.Buy
                        : TradeType.Sell,
                    volume,
                    out reason))
            {
                _autoOrdersBlockReason =
                    "PENDING STOP • " +
                    reason;
                return false;
            }

            try
            {
                DateTime expiration =
                    Server.Time.AddMinutes(
                        Math.Max(
                            15,
                            PendingOrderExpiryMinutes));

                TradeResult result =
                    PlaceStopOrder(
                        direction == 1
                            ? TradeType.Buy
                            : TradeType.Sell,
                        SymbolName,
                        volume,
                        trigger,
                        PendingOrderLabel(),
                        stopPips,
                        targetPips,
                        ProtectionType.Relative,
                        expiration,
                        "CFIP SMART70",
                        false);

                if (result == null ||
                    !result.IsSuccessful ||
                    result.PendingOrder == null)
                {
                    _autoOrdersBlockReason =
                        result != null &&
                        result.Error.HasValue
                            ? result.Error.Value.ToString()
                            : "PENDING STOP REJECTED";

                    return false;
                }

                _lastPendingSignalM5 =
                    closedM5;

                _plan = null;
                _executionModel = null;
                RemovePlanObjects();

                _autoOrdersBlockReason =
                    "ORDER PLACED • STOP " + Price(trigger);

                SendUnifiedAlert(
                    "PENDING-STOP|" + closedM5,
                    "CFIP CLEAN70 STOP ORDER | " +
                    (direction == 1
                        ? "BUY"
                        : "SELL") +
                    " | ENTRY " +
                    Price(trigger) +
                    " | SL " +
                    Price(stop) +
                    " | TP " +
                    Price(target),
                    direction,
                    true);

                return true;
            }
            catch (Exception ex)
            {
                _autoOrdersBlockReason =
                    "PENDING STOP • " +
                    ex.Message;

                Print(
                    "CFIP CLEAN70 pending stop failed: {0}",
                    ex.Message);

                return false;
            }
        }

        private bool PlaceReversalLimit(int closedM5)
        {
            int direction =
                _reaction.Direction;

            double atr =
                Atr(
                    _m5Bars,
                    closedM5);

            if (atr <= 0)
            {
                _autoOrdersBlockReason =
                    "PENDING LIMIT • ATR UNAVAILABLE";
                return false;
            }

            ExecutionModel reversalModel =
                null;

            try
            {
                reversalModel =
                    BuildExecutionModel(
                        closedM5,
                        direction);
            }
            catch
            {
                reversalModel = null;
            }

            double targetEntry =
                reversalModel != null &&
                reversalModel.Direction == direction &&
                IsFinitePositive(
                    reversalModel.IdealEntry)
                    ? reversalModel.IdealEntry
                    : direction == 1
                        ? Symbol.Bid -
                          atr * 0.25
                        : Symbol.Ask +
                          atr * 0.25;

            targetEntry =
                NormalizePrice(
                    targetEntry);

            if (!IsValidPendingEntry(
                    direction,
                    targetEntry,
                    false))
            {
                _autoOrdersBlockReason =
                    "PENDING LIMIT • INVALID ENTRY";
                return false;
            }

            string source;
            int quality;

            double stop =
                BuildStructuralStop(
                    closedM5,
                    direction,
                    targetEntry,
                    atr,
                    out source,
                    out quality);

            if (!IsFinitePositive(stop))
                return false;

            double target =
                SelectStructuralAutoTarget(
                    closedM5,
                    direction,
                    targetEntry,
                    stop,
                    atr,
                    EffectiveAutoTpStage());

            if (!IsAutoPlanValid(
                    direction,
                    targetEntry,
                    stop,
                    target))
            {
                _autoOrdersBlockReason =
                    "PENDING LIMIT • INVALID SL/TP";
                return false;
            }

            double stopPips =
                Math.Abs(
                    targetEntry -
                    stop) /
                Symbol.PipSize;

            double targetPips =
                Math.Abs(
                    target -
                    targetEntry) /
                Symbol.PipSize;

            double volume =
                CalculateVolume(
                    EffectiveRiskStopPips(
                        stopPips));

            volume =
                AdjustVolumeForMargin(
                    direction == 1
                        ? TradeType.Buy
                        : TradeType.Sell,
                    volume);

            if (volume <
                Symbol.VolumeInUnitsMin)
            {
                _autoOrdersBlockReason =
                    "PENDING LIMIT • VOLUME BELOW MINIMUM";
                return false;
            }

            string reason;

            if (!PassesAutoTradeSafetyGuards(
                    direction == 1
                        ? TradeType.Buy
                        : TradeType.Sell,
                    volume,
                    out reason))
            {
                _autoOrdersBlockReason =
                    "PENDING LIMIT • " +
                    reason;
                return false;
            }

            try
            {
                DateTime expiration =
                    Server.Time.AddMinutes(
                        Math.Max(
                            15,
                            PendingOrderExpiryMinutes));

                TradeResult result =
                    PlaceLimitOrder(
                        direction == 1
                            ? TradeType.Buy
                            : TradeType.Sell,
                        SymbolName,
                        volume,
                        targetEntry,
                        PendingOrderLabel(),
                        stopPips,
                        targetPips,
                        ProtectionType.Relative,
                        expiration,
                        "CFIP SMART70",
                        false);

                if (result == null ||
                    !result.IsSuccessful ||
                    result.PendingOrder == null)
                {
                    _autoOrdersBlockReason =
                        result != null &&
                        result.Error.HasValue
                            ? result.Error.Value.ToString()
                            : "PENDING LIMIT REJECTED";

                    return false;
                }

                _lastPendingSignalM5 =
                    closedM5;

                _plan = null;
                _executionModel = null;
                RemovePlanObjects();

                _autoOrdersBlockReason =
                    "ORDER PLACED • LIMIT " +
                    Price(targetEntry);

                SendUnifiedAlert(
                    "PENDING-LIMIT|" + closedM5,
                    "CFIP CLEAN70 LIMIT ORDER | " +
                    (direction == 1
                        ? "BUY"
                        : "SELL") +
                    " | ENTRY " +
                    Price(targetEntry) +
                    " | SL " +
                    Price(stop) +
                    " | TP " +
                    Price(target),
                    direction,
                    true);

                return true;
            }
            catch (Exception ex)
            {
                _autoOrdersBlockReason =
                    "PENDING LIMIT • " +
                    ex.Message;

                Print(
                    "CFIP CLEAN70 pending limit failed: {0}",
                    ex.Message);

                return false;
            }
        }

                private void CleanupPendingOrdersIfNeeded(
            int closedM5)
        {
            if (!PendingAutoCleanup ||
                _lastPendingCleanupM5 == closedM5)
                return;

            _lastPendingCleanupM5 =
                closedM5;

            foreach (PendingOrder order in PendingOrders)
            {
                if (!IsManagedPendingOrder(order))
                    continue;

                bool stale =
                    order.ExpirationTime.HasValue &&
                    order.ExpirationTime.Value <=
                    DateTime.UtcNow;

                int expectedDirection =
                    _decision != null
                        ? _decision.Direction
                        : 0;

                if (order.OrderType ==
                        PendingOrderType.Limit &&
                    ReversalSetupStrong())
                {
                    expectedDirection =
                        _reaction.Direction;
                }

                bool wrongDirection =
                    expectedDirection != 0 &&
                    ((order.TradeType == TradeType.Buy &&
                      expectedDirection != 1) ||
                     (order.TradeType == TradeType.Sell &&
                      expectedDirection != -1));

                bool reversalSupersedesStop =
                    ReversalSetupStrong() &&
                    order.OrderType == PendingOrderType.Stop;

                if (!stale &&
                    !wrongDirection &&
                    !reversalSupersedesStop)
                    continue;

                if (!TryCancelPendingOrder(
                        order,
                        stale
                            ? "STALE PENDING ORDER"
                            : wrongDirection
                                ? "WRONG DIRECTION PENDING ORDER"
                                : "REVERSAL SUPERSEDES STOP"))
                {
                    SetLifecycleState(
                        CFIPClean70LifecycleState.RecoveryRequired,
                        "PENDING CLEANUP CANCEL REJECTED");

                    _autoOrdersBlockReason =
                        "PENDING CLEANUP CANCEL REJECTED";
                }
            }
        }


        private Position GetManagedPosition()
        {
            foreach (Position position in Positions)
            {
                if (IsManagedPosition(position) &&
                    position.SymbolName == SymbolName)
                    return position;
            }

            return null;
        }

        private PendingOrder GetManagedPendingOrder()
        {
            foreach (PendingOrder order in PendingOrders)
            {
                if (IsManagedPendingOrder(order) &&
                    order.SymbolName == SymbolName)
                    return order;
            }

            return null;
        }

        private void RenderManagedPendingOrder()
        {
            RemoveManagedPendingOrderObjects();

            PendingOrder pending =
                GetManagedPendingOrder();

            if (pending == null ||
                !IsFinitePositive(
                    pending.TargetPrice))
                return;

            if (!ShowLevelLines)
                return;

            int anchorBar =
                Bars == null ||
                Bars.Count < 2
                    ? -1
                    : Math.Max(
                        0,
                        Math.Min(
                            Bars.Count - 1,
                            MapM5ToChart(
                                Math.Max(
                                    1,
                                    _lastEvaluatedM5),
                                Bars.Count - 1)));

            if (anchorBar < 0)
                return;

            DrawPlanLine(
                P + "PENDING_ENTRY",
                pending.TargetPrice,
                TriggerLineColor,
                ShowTrigger);

            if (ShowSL &&
                pending.StopLoss.HasValue &&
                IsFinitePositive(
                    pending.StopLoss.Value))
            {
                DrawPlanLine(
                    P + "PENDING_SL",
                    pending.StopLoss.Value,
                    SlLineColor,
                    ShowSL);
            }

            if (ShowTP1 &&
                pending.TakeProfit.HasValue &&
                IsFinitePositive(
                    pending.TakeProfit.Value))
            {
                DrawPlanLine(
                    P + "PENDING_TP",
                    pending.TakeProfit.Value,
                    TpLineColor,
                    ShowTP1);
            }

            if (!ShowLevelPriceLabels &&
                !ShowSignalLabels)
                return;

            string typeText =
                pending.OrderType ==
                    PendingOrderType.Stop
                    ? "STOP"
                    : pending.OrderType ==
                      PendingOrderType.Limit
                        ? "LIMIT"
                        : "PENDING";

            if (ShowTrigger)
            {
                DrawPlanLabel(
                    P + "PENDING_ENTRY_LABEL",
                    "PENDING " +
                    typeText +
                    " " +
                    Price(
                        pending.TargetPrice),
                    anchorBar,
                    pending.TargetPrice,
                    TriggerLineColor);
            }

            if (pending.StopLoss.HasValue &&
                IsFinitePositive(
                    pending.StopLoss.Value))
            {
                DrawPlanLabel(
                    P + "PENDING_SL_LABEL",
                    "SL " +
                    Price(
                        pending.StopLoss.Value),
                    anchorBar,
                    pending.StopLoss.Value,
                    SlLineColor);
            }

            if (pending.TakeProfit.HasValue &&
                IsFinitePositive(
                    pending.TakeProfit.Value))
            {
                DrawPlanLabel(
                    P + "PENDING_TP_LABEL",
                    "TP " +
                    Price(
                        pending.TakeProfit.Value),
                    anchorBar,
                    pending.TakeProfit.Value,
                    TpLineColor);
            }
        }

        private void RemoveManagedPendingOrderObjects()
        {
            RemovePlanLine(
                P + "PENDING_ENTRY");
            RemovePlanLine(
                P + "PENDING_SL");
            RemovePlanLine(
                P + "PENDING_TP");

            Chart.RemoveObject(
                P + "PENDING_ENTRY_LABEL");
            Chart.RemoveObject(
                P + "PENDING_SL_LABEL");
            Chart.RemoveObject(
                P + "PENDING_TP_LABEL");
        }

        private void OnPositionOpened(PositionOpenedEventArgs args)
        {
            if (args == null ||
                !IsManagedPosition(args.Position))
                return;

            SendUnifiedAlert(
                "POSITION-OPEN|" +
                args.Position.Id,
                "CFIP CLEAN70 POSITION OPENED | #" +
                args.Position.Id,
                args.Position.TradeType == TradeType.Buy
                    ? 1
                    : -1,
                true);
        }

                private void OnPositionModified(
            PositionModifiedEventArgs args)
        {
            if (args == null ||
                args.Position == null ||
                !IsManagedPosition(args.Position))
                return;

            Position position =
                args.Position;

            _activeBrokerStop =
                position.StopLoss.HasValue
                    ? NormalizePrice(position.StopLoss.Value)
                    : 0;

            _activeBrokerTarget =
                position.TakeProfit.HasValue
                    ? NormalizePrice(position.TakeProfit.Value)
                    : 0;

            if (_plan != null &&
                _plan.IsLivePosition &&
                _plan.PositionId == position.Id &&
                _lifecycleState !=
                    CFIPClean70LifecycleState.ExitRequested)
            {
                _brokerProtectionRecoveryRequired =
                    !position.StopLoss.HasValue ||
                    !position.TakeProfit.HasValue;

                SetLifecycleState(
                    _brokerProtectionRecoveryRequired
                        ? CFIPClean70LifecycleState.RecoveryRequired
                        : CFIPClean70LifecycleState.LivePosition,
                    _brokerProtectionRecoveryRequired
                        ? "BROKER POSITION MODIFIED • PROTECTION MISSING"
                        : "BROKER POSITION MODIFIED");
            }
        }

        private void OnPendingOrderCreated(
            PendingOrderCreatedEventArgs args)
        {
            if (args == null ||
                args.PendingOrder == null ||
                !IsManagedPendingOrder(args.PendingOrder))
                return;

            SetLifecycleState(
                CFIPClean70LifecycleState.PendingOrder,
                "PENDING ORDER CREATED #" +
                args.PendingOrder.Id);
        }

        private void OnPendingOrderModified(
            PendingOrderModifiedEventArgs args)
        {
            if (args == null ||
                args.PendingOrder == null ||
                !IsManagedPendingOrder(args.PendingOrder))
                return;

            SetLifecycleState(
                CFIPClean70LifecycleState.PendingOrder,
                "PENDING ORDER MODIFIED #" +
                args.PendingOrder.Id);
        }

        private void OnPendingOrderCancelled(
            PendingOrderCancelledEventArgs args)
        {
            if (args == null ||
                args.PendingOrder == null ||
                !IsManagedPendingOrder(args.PendingOrder))
                return;

            PendingOrder remaining =
                GetManagedPendingOrder();

            if (remaining == null &&
                GetManagedPosition() == null &&
                _plan == null)
            {
                SetLifecycleState(
                    CFIPClean70LifecycleState.Closed,
                    "PENDING ORDER CANCELLED #" +
                    args.PendingOrder.Id);
            }
            else if (remaining == null &&
                     GetManagedPosition() != null)
            {
                SetLifecycleState(
                    CFIPClean70LifecycleState.LivePosition,
                    "PENDING ORDER CANCELLED • LIVE POSITION EXISTS");
            }
        }

        private void OnPositionClosed(PositionClosedEventArgs args)
        {
            if (args == null ||
                !IsManagedPosition(args.Position))
                return;

            _lastExitM5 =
                Math.Max(
                    _lastExitM5,
                    _lastEvaluatedM5);

            int direction =
                args.Position.TradeType == TradeType.Buy
                    ? 1
                    : -1;

            if (!_outcomeRegistered)
            {
                bool profitable =
                    args.Position.NetProfit > 0;

                if (EnableOutcomeTelemetry)
                {
                    RegisterOutcome(
                        direction,
                        profitable);
                }

                _outcomeRegistered = true;

                if (profitable)
                    _wins++;
                else
                    _losses++;
            }

            _brokerProtectionRecoveryRequired = false;

            if (_plan != null &&
                _plan.IsLivePosition &&
                _plan.PositionId ==
                args.Position.Id)
            {
                _plan = null;
                _activeBrokerStop = 0;
                _activeBrokerTarget = 0;
                _executionModel = null;

                SetLifecycleState(
                    CFIPClean70LifecycleState.Closed,
                    args.Position.NetProfit > 0
                        ? "POSITION CLOSED • PROFIT"
                        : "POSITION CLOSED • LOSS");

                RemovePlanObjects();
            }
        }


                private void OnPendingOrderFilled(PendingOrderFilledEventArgs args)
        {
            if (args == null ||
                args.Position == null ||
                !IsManagedPosition(args.Position))
                return;

            RemoveManagedPendingOrderObjects();

            int direction =
                args.Position.TradeType == TradeType.Buy
                    ? 1
                    : -1;

            int closedM5 =
                Math.Max(
                    1,
                    _lastEvaluatedM5);

            double entry =
                args.Position.EntryPrice;

            double atr =
                _m5Bars == null
                    ? 0
                    : Atr(
                        _m5Bars,
                        closedM5);

            if (!IsFinitePositive(atr))
            {
                atr =
                    Math.Max(
                        Symbol.PipSize * 20,
                        Math.Abs(
                            Symbol.Ask -
                            Symbol.Bid) *
                        10);
            }

            double stop =
                args.Position.StopLoss.HasValue &&
                IsValidStop(
                    direction,
                    entry,
                    args.Position.StopLoss.Value)
                    ? NormalizePrice(
                        args.Position.StopLoss.Value)
                    : 0;

            double target =
                args.Position.TakeProfit.HasValue &&
                IsValidTarget(
                    direction,
                    entry,
                    args.Position.TakeProfit.Value)
                    ? NormalizePrice(
                        args.Position.TakeProfit.Value)
                    : 0;

            bool protectionMissing =
                !IsFinitePositive(stop) ||
                !IsFinitePositive(target);

            if (!IsFinitePositive(stop))
            {
                string stopSource;
                int stopQuality;

                stop =
                    BuildStructuralStop(
                        closedM5,
                        direction,
                        entry,
                        atr,
                        out stopSource,
                        out stopQuality);

                if (!IsValidStop(
                        direction,
                        entry,
                        stop))
                {
                    double fallbackRisk =
                        atr *
                        Math.Max(
                            0.10,
                            FallbackSlAtr);

                    stop =
                        direction == 1
                            ? entry - fallbackRisk
                            : entry + fallbackRisk;

                    stop =
                        NormalizePrice(stop);
                }
            }

            if (!IsFinitePositive(target))
            {
                target =
                    SelectStructuralAutoTarget(
                        closedM5,
                        direction,
                        entry,
                        stop,
                        atr,
                        EffectiveAutoTpStage());

                if (!IsValidTarget(
                        direction,
                        entry,
                        target))
                {
                    double risk =
                        Math.Max(
                            Symbol.PipSize,
                            Math.Abs(
                                entry -
                                stop));

                    double fallbackDistance =
                        risk *
                        Math.Max(
                            1.0,
                            MinimumRequiredRR());

                    target =
                        direction == 1
                            ? entry + fallbackDistance
                            : entry - fallbackDistance;

                    target =
                        NormalizePrice(target);
                }
            }

            _plan =
                CreateManagedPlanFromExecution(
                    direction,
                    entry,
                    stop,
                    target,
                    closedM5,
                    args.Position.VolumeInUnits,
                    args.PendingOrder.OrderType ==
                        PendingOrderType.Stop
                        ? CFIPClean70ExecutionMode.ContinuationStop
                        : CFIPClean70ExecutionMode.ReversalLimit);

            _plan.PositionId =
                args.Position.Id;

            _activeBrokerStop =
                args.Position.StopLoss.HasValue
                    ? NormalizePrice(
                        args.Position.StopLoss.Value)
                    : 0;

            _activeBrokerTarget =
                args.Position.TakeProfit.HasValue
                    ? NormalizePrice(
                        args.Position.TakeProfit.Value)
                    : 0;

            _brokerProtectionRecoveryRequired =
                protectionMissing;

            SetLifecycleState(
                protectionMissing
                    ? CFIPClean70LifecycleState.RecoveryRequired
                    : CFIPClean70LifecycleState.LivePosition,
                protectionMissing
                    ? "PENDING FILL • BROKER PROTECTION MISSING"
                    : "PENDING FILL • LIVE");

            EnrichLivePlanTargets(closedM5);

            if (AutoBrokerProtection)
            {
                bool protectedOk =
                    EnsureBrokerProtectionForPosition(
                        args.Position,
                        stop,
                        target,
                        "PENDING FILL",
                        direction);

                if (protectedOk)
                {
                    _activeBrokerStop = stop;
                    _activeBrokerTarget = target;
                    _brokerProtectionRecoveryRequired = false;

                    SetLifecycleState(
                        CFIPClean70LifecycleState.LivePosition,
                        "PENDING FILL • PROTECTED");
                }
            }

            SendUnifiedAlert(
                "PENDING-FILLED|" +
                args.PendingOrder.Id,
                "CFIP CLEAN70 PENDING FILLED | #" +
                args.Position.Id +
                (_brokerProtectionRecoveryRequired
                    ? " | PROTECTION RECOVERY"
                    : ""),
                direction,
                true);
        }


        private void CreateQuickExecutionControls()
        {
            if (_quickExecutionStack != null)
                return;

            _quickExecutionStack =
                new StackPanel
                {
                    Orientation =
                        Orientation.Horizontal,
                    HorizontalAlignment =
                        HorizontalAlignment.Stretch,
                    VerticalAlignment =
                        VerticalAlignment.Center,
                    BackgroundColor =
                        Color.FromArgb(
                            0,
                            Color.Black),
                    Height =
                        QuickExecutionRowHeight
                };

            _autoTradingQuickToggle =
                CreateExecutionToggle(
                    AutoTradingEnabled,
                    "AUTO TRADE",
                    TpLineColor);

            _automaticOrdersQuickToggle =
                CreateExecutionToggle(
                    AutomaticOrdersEnabled,
                    "AUTO ORDERS",
                    TriggerLineColor);

            _autoTradingQuickToggle.Click +=
                OnAutoTradingQuickToggleClicked;

            _automaticOrdersQuickToggle.Click +=
                OnAutomaticOrdersQuickToggleClicked;

            _quickExecutionStack.AddChild(
                _autoTradingQuickToggle);

            _quickExecutionStack.AddChild(
                _automaticOrdersQuickToggle);
        }

        private ToggleButton CreateExecutionToggle(
            bool isChecked,
            string caption,
            Color accentColor)
        {
            return
                new ToggleButton
                {
                    Text =
                        isChecked
                            ? caption + "  •  ON"
                            : caption + "  •  OFF",
                    Width = 170,
                    Height =
                        QuickExecutionButtonHeight,
                    IsChecked =
                        isChecked,
                    FontFamily =
                        string.IsNullOrWhiteSpace(
                            PanelFontFamily)
                            ? "Arial"
                            : PanelFontFamily,
                    FontSize =
                        Math.Max(
                            8,
                            PanelFontSize - 1),
                    BackgroundColor =
                        Color.FromArgb(
                            105,
                            isChecked
                                ? accentColor
                                : Color.Black),
                    ForegroundColor =
                        PanelTextColor,
                    BorderColor =
                        isChecked
                            ? accentColor
                            : PanelBorder,
                    BorderThickness = 1,
                    CornerRadius =
                        Math.Min(
                            10,
                            Math.Max(
                                5,
                                PanelCornerRadius))
                };
        }

        private void OnAutoTradingQuickToggleClicked(
            ToggleButtonEventArgs args)
        {
            if (_executionToggleSyncing ||
                args == null ||
                args.ToggleButton == null)
                return;

            bool enabled =
                args.ToggleButton.IsChecked;

            SetAutoTradingRuntimeState(
                enabled,
                enabled
                    ? "AWAITING EXECUTION"
                    : "DISABLED");

            SetAutoTradingState(
                enabled
                    ? "ARMED"
                    : "OFF",
                enabled
                    ? "QUICK ENABLED"
                    : "QUICK DISABLED");
        }

        private void OnAutomaticOrdersQuickToggleClicked(
            ToggleButtonEventArgs args)
        {
            if (_executionToggleSyncing ||
                args == null ||
                args.ToggleButton == null)
                return;

            bool enabled =
                args.ToggleButton.IsChecked;

            SetAutomaticOrdersRuntimeState(
                enabled,
                enabled
                    ? "AWAITING ORDER SETUP"
                    : "DISABLED");
        }

        private void SyncQuickExecutionControls()
        {
            EnsureExecutionRuntimeState();

            _executionToggleSyncing = true;

            try
            {
                if (_autoTradingQuickToggle != null)
                {
                    _autoTradingQuickToggle.IsChecked =
                        AutoTradingEnabled;

                    bool compactTradeText =
                        _autoTradingQuickToggle.Width < 120;

                    _autoTradingQuickToggle.Text =
                        compactTradeText
                            ? (AutoTradingEnabled ? "TRADE • ON" : "TRADE • OFF")
                            : (AutoTradingEnabled ? "AUTO TRADE • ON" : "AUTO TRADE • OFF");

                    _autoTradingQuickToggle.BackgroundColor =
                        Color.FromArgb(
                            105,
                            AutoTradingEnabled
                                ? TpLineColor
                                : Color.Black);

                    _autoTradingQuickToggle.BorderColor =
                        AutoTradingEnabled
                            ? TpLineColor
                            : PanelBorder;

                    _autoTradingQuickToggle.ForegroundColor =
                        PanelTextColor;
                }

                if (_automaticOrdersQuickToggle != null)
                {
                    _automaticOrdersQuickToggle.IsChecked =
                        AutomaticOrdersEnabled;

                    bool compactOrderText =
                        _automaticOrdersQuickToggle.Width < 120;

                    _automaticOrdersQuickToggle.Text =
                        compactOrderText
                            ? (AutomaticOrdersEnabled ? "ORDERS • ON" : "ORDERS • OFF")
                            : (AutomaticOrdersEnabled ? "AUTO ORDERS • ON" : "AUTO ORDERS • OFF");

                    _automaticOrdersQuickToggle.BackgroundColor =
                        Color.FromArgb(
                            105,
                            AutomaticOrdersEnabled
                                ? TriggerLineColor
                                : Color.Black);

                    _automaticOrdersQuickToggle.BorderColor =
                        AutomaticOrdersEnabled
                            ? TriggerLineColor
                            : PanelBorder;

                    _automaticOrdersQuickToggle.ForegroundColor =
                        PanelTextColor;
                }
            }
            finally
            {
                _executionToggleSyncing = false;
            }
        }

        private string GetSessionPanelText()
        {
            DateTime utc =
                DateTime.UtcNow;

            DateTime local =
                DateTime.Now;

            int start =
                ClampInt(
                    SessionStartUtc,
                    0,
                    23);

            int end =
                ClampInt(
                    SessionEndUtc,
                    0,
                    23);

            int now =
                utc.Hour * 60 +
                utc.Minute;

            int startMin =
                start * 60;

            int endMin =
                end * 60;

            bool open =
                startMin <= endMin
                    ? now >= startMin &&
                      now < endMin
                    : now >= startMin ||
                      now < endMin;

            return
                "SESSION  " +
                (open ? "OPEN" : "CLOSED") +
                "  •  UTC " +
                utc.ToString("HH:mm") +
                "  •  LOCAL " +
                local.ToString("HH:mm") +
                "  •  " +
                start.ToString("00") +
                ":00–" +
                end.ToString("00") +
                ":00 UTC";
        }

        private Color GetSessionPanelColor()
        {
            DateTime utc =
                DateTime.UtcNow;

            int now =
                utc.Hour * 60 +
                utc.Minute;

            int start =
                ClampInt(
                    SessionStartUtc,
                    0,
                    23) * 60;

            int end =
                ClampInt(
                    SessionEndUtc,
                    0,
                    23) * 60;

            bool open =
                start <= end
                    ? now >= start &&
                      now < end
                    : now >= start ||
                      now < end;

            return open
                ? TpLineColor
                : PanelWarningColor;
        }

        // ============================================================
        #endregion

        #region Historical Rendering
        // ============================================================

        private void RenderHistoricalSignals()
        {
            RemoveHistoricalObjects();

            if (Bars == null ||
                Bars.Count < 60)
                return;

            int drawn = 0;
            int last =
                Bars.Count - 2;

            for (int i = last;
                 i >= 40 &&
                 drawn < HistoricalSignalLimit;
                 i--)
            {
                Frame frame =
                    AnalyzeFrame(
                        Bars,
                        i);

                if (frame.Direction == 0 ||
                    frame.Quality <
                    MinimumSmartQuality)
                    continue;

                int trigger =
                    frame.Direction == 1
                        ? BullTriggerScore(
                            Bars,
                            i)
                        : BearTriggerScore(
                            Bars,
                            i);

                if (trigger < 4)
                    continue;

                string name =
                    H +
                    i;

                double atr =
                    Atr(
                        Bars,
                        i);

                double offset =
                    Math.Max(
                        Symbol.PipSize * 2,
                        atr * 0.18);

                if (ShowHistoricalArrows)
                {
                Chart.DrawIcon(
                    name,
                    frame.Direction == 1
                        ? ChartIconType.UpArrow
                        : ChartIconType.DownArrow,
                    i,
                    frame.Direction == 1
                        ? Bars.LowPrices[i] -
                          offset
                        : Bars.HighPrices[i] +
                          offset,
                    frame.Direction == 1
                        ? BuyArrowColor
                        : SellArrowColor);

                }

                _historicalDrawn.Add(name);
                drawn++;
            }
        }

        private void RemoveHistoricalObjects()
        {
            foreach (string name in _historicalDrawn)
                Chart.RemoveObject(name);

            _historicalDrawn.Clear();
        }

        // ============================================================
        #endregion

        #region Index Math and Cleanup
        // ============================================================

        private int ClosedIndex(
            Bars bars,
            DateTime reference)
        {
            if (bars == null ||
                bars.Count < 2)
                return -1;

            int probe =
                bars.OpenTimes.GetIndexByTime(
                    reference);

            if (probe < 0)
                probe =
                    bars.Count - 1;

            probe =
                Math.Max(
                    0,
                    Math.Min(
                        probe,
                        bars.Count - 1));

            for (int i = probe;
                 i >= 0;
                 i--)
            {
                TimeSpan span;

                if (i + 1 < bars.Count)
                    span =
                        bars.OpenTimes[i + 1] -
                        bars.OpenTimes[i];
                else if (i > 0)
                    span =
                        bars.OpenTimes[i] -
                        bars.OpenTimes[i - 1];
                else
                    span =
                        TimeSpan.FromMinutes(1);

                if (span <= TimeSpan.Zero)
                    span =
                        TimeSpan.FromMinutes(1);

                if (bars.OpenTimes[i] +
                    span <=
                    reference)
                    return i;
            }

            return -1;
        }

        private double Highest(
            Bars bars,
            int start,
            int end)
        {
            if (bars == null ||
                bars.Count == 0)
                return 0;

            start =
                Math.Max(
                    0,
                    start);

            end =
                Math.Min(
                    bars.Count - 1,
                    end);

            if (end < start)
                return
                    bars.HighPrices[
                        Math.Max(
                            0,
                            Math.Min(
                                bars.Count - 1,
                                start))];

            double value =
                double.MinValue;

            for (int i = start;
                 i <= end;
                 i++)
                value =
                    Math.Max(
                        value,
                        bars.HighPrices[i]);

            return
                value ==
                double.MinValue
                    ? 0
                    : value;
        }

        private double Lowest(
            Bars bars,
            int start,
            int end)
        {
            if (bars == null ||
                bars.Count == 0)
                return 0;

            start =
                Math.Max(
                    0,
                    start);

            end =
                Math.Min(
                    bars.Count - 1,
                    end);

            if (end < start)
                return
                    bars.LowPrices[
                        Math.Max(
                            0,
                            Math.Min(
                                bars.Count - 1,
                                start))];

            double value =
                double.MaxValue;

            for (int i = start;
                 i <= end;
                 i++)
                value =
                    Math.Min(
                        value,
                        bars.LowPrices[i]);

            return
                value ==
                double.MaxValue
                    ? 0
                    : value;
        }

        private bool IsFinitePositive(
            double value)
        {
            return
                !double.IsNaN(value) &&
                !double.IsInfinity(value) &&
                value > 0;
        }

        private double SafePositive(
            double value)
        {
            return
                IsFinitePositive(value)
                    ? value
                    : 0;
        }

        private double NormalizePrice(
            double price)
        {
            if (!IsFinitePositive(price))
                return 0;

            if (Symbol.TickSize > 0)
            {
                price =
                    Math.Round(
                        price /
                        Symbol.TickSize,
                        MidpointRounding.AwayFromZero) *
                    Symbol.TickSize;
            }

            return Math.Round(
                price,
                Symbol.Digits);
        }

        private string Price(
            double value)
        {
            return
                NormalizePrice(
                    value)
                .ToString(
                    "F" +
                    Symbol.Digits);
        }

        private string CompactText(
            string value,
            int maxLength)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            int limit =
                Math.Max(
                    1,
                    maxLength);

            if (value.Length <=
                limit)
                return value;

            if (limit <= 3)
                return value.Substring(
                    0,
                    limit);

            return
                value.Substring(
                    0,
                    limit - 3) +
                "...";
        }

        private double ClampDouble(
            double value,
            double min,
            double max)
        {
            if (double.IsNaN(value) ||
                double.IsInfinity(value))
                return min;

            if (value < min)
                return min;

            if (value > max)
                return max;

            return value;
        }

        private double Clamp(
            double value,
            double min,
            double max)
        {
            if (value < min)
                return min;

            if (value > max)
                return max;

            return value;
        }

        private int ClampInt(
            int value,
            int min,
            int max)
        {
            if (value < min)
                return min;

            if (value > max)
                return max;

            return value;
        }

        private void RemoveAllChartObjects()
        {
            RemovePlanObjects();
            RemoveHistoricalObjects();
            RemoveManagedPendingOrderObjects();

            foreach (string name in _outcomeDrawn)
                Chart.RemoveObject(name);

            _outcomeDrawn.Clear();
        }

        // ============================================================
        #endregion

        #region Authoritative Presentation and State Helpers
        // ============================================================

        #endregion
    }
}