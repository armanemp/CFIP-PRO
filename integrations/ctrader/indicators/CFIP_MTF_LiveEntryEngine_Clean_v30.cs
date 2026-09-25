
using System;
using System.Collections.Generic;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;

namespace cAlgo
{
    public enum CFIPClean30PanelCorner
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }

    public enum CFIPClean30SizingMode
    {
        RiskPercentEquity = 0,
        FixedLots = 1
    }

    public enum CFIPClean30TargetStage
    {
        TP1 = 0,
        TP2 = 1,
        TP3 = 2,
        TP4 = 3
    }

    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class CFIP_MTF_LiveEntryEngine_Clean_v30 : Indicator
    {
        // ============================================================
        // 01 · DECISION
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

        // ============================================================
        // 02 · MTF
        // ============================================================

        [Parameter("M1 Trigger", Group = "02 · MTF", DefaultValue = true)]
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

        // ============================================================
        // 03 · STRUCTURE
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

        // ============================================================
        // 04 · FVG / ORDER BLOCKS
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

        [Parameter("Require FVG Retest", Group = "04 · Zones", DefaultValue = true)]
        public bool RequireFvgRetest { get; set; }

        [Parameter("Require OB Displacement", Group = "04 · Zones", DefaultValue = true)]
        public bool RequireObDisplacement { get; set; }

        [Parameter("Zone Proximity ATR", Group = "04 · Zones", DefaultValue = 0.25, MinValue = 0.05, MaxValue = 1)]
        public double ZoneProximityAtr { get; set; }

        [Parameter("Maximum Zone Age", Group = "04 · Zones", DefaultValue = 40, MinValue = 5, MaxValue = 200)]
        public int MaximumZoneAgeBars { get; set; }

        // ============================================================
        // 05 · LIQUIDITY
        // ============================================================

        [Parameter("Liquidity Lookback", Group = "05 · Liquidity", DefaultValue = 40, MinValue = 10, MaxValue = 150)]
        public int LiquidityLookback { get; set; }

        [Parameter("Use Equal High / Low", Group = "05 · Liquidity", DefaultValue = true)]
        public bool UseEqualHighLow { get; set; }

        [Parameter("Equal Level Tolerance ATR", Group = "05 · Liquidity", DefaultValue = 0.12, MinValue = 0.02, MaxValue = 0.5)]
        public double EqualLevelToleranceAtr { get; set; }

        [Parameter("Use Liquidity Sweep", Group = "05 · Liquidity", DefaultValue = true)]
        public bool UseLiquiditySweep { get; set; }

        [Parameter("Use Displacement", Group = "05 · Liquidity", DefaultValue = true)]
        public bool UseDisplacement { get; set; }

        [Parameter("Displacement ATR", Group = "05 · Liquidity", DefaultValue = 0.80, MinValue = 0.2, MaxValue = 3)]
        public double DisplacementAtr { get; set; }

        [Parameter("Use Premium / Discount", Group = "05 · Liquidity", DefaultValue = true)]
        public bool UsePremiumDiscount { get; set; }

        [Parameter("Use Daily Weekly Liquidity", Group = "05 · Liquidity", DefaultValue = true)]
        public bool UseDailyWeeklyLiquidity { get; set; }

        // ============================================================
        // 06 · INDICATORS
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

        // ============================================================
        // 07 · ENTRY PRECISION
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

        // ============================================================
        // 08 · SMART WEIGHTS
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

        // ============================================================
        // 09 · RISK / TARGETS
        // ============================================================

        [Parameter("Minimum SL ATR", Group = "09 · Risk & Targets", DefaultValue = 0.55, MinValue = 0.1, MaxValue = 5)]
        public double MinimumSlAtr { get; set; }

        [Parameter("Maximum SL ATR", Group = "09 · Risk & Targets", DefaultValue = 1.80, MinValue = 0.5, MaxValue = 10)]
        public double MaximumSlAtr { get; set; }

        [Parameter("alternate SL ATR", Group = "09 · Risk & Targets", DefaultValue = 1.00, MinValue = 0.1, MaxValue = 5)]
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

        // ============================================================
        // 10 · LIVE MANAGEMENT
        // ============================================================

        [Parameter("Enable Live Exit Management", Group = "10 · Live Management", DefaultValue = true)]
        public bool EnableLiveExitManagement { get; set; }

        [Parameter("Move SL To Break Even", Group = "10 · Live Management", DefaultValue = true)]
        public bool MoveSlToBreakEven { get; set; }

        [Parameter("Break Even Trigger RR", Group = "10 · Live Management", DefaultValue = 1.25, MinValue = 0.2, MaxValue = 10)]
        public double BreakEvenTriggerRR { get; set; }

        [Parameter("Break Even Buffer Pips", Group = "10 · Live Management", DefaultValue = 1.5, MinValue = 0, MaxValue = 20)]
        public double BreakEvenBufferPips { get; set; }

        [Parameter("Enable Structural SL Repricing", Group = "10 · Live Management", DefaultValue = true)]
        public bool EnableStructuralSlRepricing { get; set; }

        [Parameter("SL Reprice Start RR", Group = "10 · Live Management", DefaultValue = 1.25, MinValue = 0.5, MaxValue = 10)]
        public double SlRepriceStartRR { get; set; }

        [Parameter("SL Reprice Breathing ATR", Group = "10 · Live Management", DefaultValue = 0.85, MinValue = 0.2, MaxValue = 5)]
        public double SlRepriceBreathingAtr { get; set; }

        [Parameter("SL Reprice Step ATR", Group = "10 · Live Management", DefaultValue = 0.08, MinValue = 0.01, MaxValue = 1)]
        public double SlRepriceStepAtr { get; set; }

        [Parameter("Update Unhit Targets", Group = "10 · Live Management", DefaultValue = true)]
        public bool UpdateUnhitTargets { get; set; }

        [Parameter("Target Update Trigger RR", Group = "10 · Live Management", DefaultValue = 1.50, MinValue = 0.5, MaxValue = 10)]
        public double TargetUpdateTriggerRR { get; set; }

        // ============================================================
        // 11 · FILTERS
        // ============================================================

        [Parameter("Use Session Filter", Group = "11 · Filters", DefaultValue = false)]
        public bool UseSessionFilter { get; set; }

        [Parameter("Session Start UTC", Group = "11 · Filters", DefaultValue = 6, MinValue = 0, MaxValue = 23)]
        public int SessionStartUtc { get; set; }

        [Parameter("Session End UTC", Group = "11 · Filters", DefaultValue = 20, MinValue = 0, MaxValue = 23)]
        public int SessionEndUtc { get; set; }

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

        // ============================================================
        // 12 · ALERTS
        // ============================================================

        [Parameter("Enable Sound Alerts", Group = "12 · Alerts", DefaultValue = true)]
        public bool EnableSoundAlerts { get; set; }

        [Parameter("Show Popup Alerts", Group = "12 · Alerts", DefaultValue = false)]
        public bool ShowPopupAlerts { get; set; }

        [Parameter("Popup Critical Only", Group = "12 · Alerts", DefaultValue = true)]
        public bool PopupCriticalOnly { get; set; }

        [Parameter("Popup Duration Seconds", Group = "12 · Alerts", DefaultValue = 6, MinValue = 1, MaxValue = 60)]
        public int PopupDurationSeconds { get; set; }

        [Parameter("Alert On Confirmed Signal", Group = "12 · Alerts", DefaultValue = true)]
        public bool AlertOnConfirmedSignal { get; set; }

        [Parameter("Alert On Reaction", Group = "12 · Alerts", DefaultValue = true)]
        public bool AlertOnReaction { get; set; }

        [Parameter("Alert On Early Watch", Group = "12 · Alerts", DefaultValue = true)]
        public bool AlertOnEarlyWatch { get; set; }

        [Parameter("Alert On Level Hit", Group = "12 · Alerts", DefaultValue = true)]
        public bool AlertOnLevelHit { get; set; }

        [Parameter("Alert On Invalidated", Group = "12 · Alerts", DefaultValue = true)]
        public bool AlertOnInvalidated { get; set; }

        [Parameter("Alert Cooldown Seconds", Group = "12 · Alerts", DefaultValue = 8, MinValue = 1, MaxValue = 60)]
        public int AlertCooldownSeconds { get; set; }

        [Parameter("Suppress Duplicate Alerts", Group = "12 · Alerts", DefaultValue = true)]
        public bool SuppressDuplicateAlerts { get; set; }

        [Parameter("Enable Email Alerts", Group = "12 · Alerts", DefaultValue = false)]
        public bool EnableEmailAlerts { get; set; }

        [Parameter("Sender Email", Group = "12 · Alerts", DefaultValue = "")]
        public string SenderEmail { get; set; }

        [Parameter("Receiver Email", Group = "12 · Alerts", DefaultValue = "")]
        public string ReceiverEmail { get; set; }

        // ============================================================
        // 13 · AUTO TRADING
        // ============================================================

        [Parameter("Enable Auto Trading", Group = "13 · Auto Trading", DefaultValue = false)]
        public bool EnableAutoTrading { get; set; }

        [Parameter("Confirmed Signals Only", Group = "13 · Auto Trading", DefaultValue = true)]
        public bool ConfirmedSignalsOnly { get; set; }

        [Parameter("Sizing Mode", Group = "13 · Auto Trading", DefaultValue = CFIPClean30SizingMode.RiskPercentEquity)]
        public CFIPClean30SizingMode SizingMode { get; set; }

        [Parameter("Risk % Equity", Group = "13 · Auto Trading", DefaultValue = 0.50, MinValue = 0.05, MaxValue = 5)]
        public double RiskPercentEquity { get; set; }

        [Parameter("Fixed Lots", Group = "13 · Auto Trading", DefaultValue = 0.01, MinValue = 0.001, MaxValue = 100, Step = 0.001)]
        public double FixedLots { get; set; }

        [Parameter("Minimum Auto Confidence", Group = "13 · Auto Trading", DefaultValue = 86, MinValue = 50, MaxValue = 99)]
        public int MinimumAutoConfidence { get; set; }

        [Parameter("Minimum Auto Smart Quality", Group = "13 · Auto Trading", DefaultValue = 80, MinValue = 50, MaxValue = 95)]
        public int MinimumAutoSmartQuality { get; set; }

        [Parameter("Minimum Auto Level Quality", Group = "13 · Auto Trading", DefaultValue = 72, MinValue = 40, MaxValue = 100)]
        public int MinimumAutoLevelQuality { get; set; }

        [Parameter("Auto TP Stage", Group = "13 · Auto Trading", DefaultValue = CFIPClean30TargetStage.TP2)]
        public CFIPClean30TargetStage AutoTpStage { get; set; }

        [Parameter("Maximum Open Positions", Group = "13 · Auto Trading", DefaultValue = 1, MinValue = 1, MaxValue = 20)]
        public int MaximumOpenPositions { get; set; }

        [Parameter("Auto Trade Label", Group = "13 · Auto Trading", DefaultValue = "CFIP-SMART-CLEAN30")]
        public string AutoTradeLabel { get; set; }

        [Parameter("Auto Broker Protection", Group = "13 · Auto Trading", DefaultValue = true)]
        public bool AutoBrokerProtection { get; set; }

        [Parameter("One Order Per Signal", Group = "13 · Auto Trading", DefaultValue = true)]
        public bool OneOrderPerSignal { get; set; }

        // ============================================================
        // 14 · DISPLAY
        // ============================================================

        [Parameter("Show Level Lines", Group = "14 · Display", DefaultValue = true)]
        public bool ShowLevelLines { get; set; }

        [Parameter("Full Width Level Lines", Group = "14 · Display", DefaultValue = true)]
        public bool FullWidthLevelLines { get; set; }

        [Parameter("Level Line Thickness", Group = "14 · Display", DefaultValue = 1, MinValue = 1, MaxValue = 3)]
        public int LevelLineThickness { get; set; }

        [Parameter("Plan Line Style", Group = "14 · Display", DefaultValue = LineStyle.Solid)]
        public LineStyle PlanLineStyle { get; set; }

        [Parameter("Show Entry", Group = "14 · Display", DefaultValue = true)]
        public bool ShowEntry { get; set; }

        [Parameter("Show SL", Group = "14 · Display", DefaultValue = true)]
        public bool ShowSL { get; set; }

        [Parameter("Show TP1", Group = "14 · Display", DefaultValue = true)]
        public bool ShowTP1 { get; set; }

        [Parameter("Show TP2", Group = "14 · Display", DefaultValue = true)]
        public bool ShowTP2 { get; set; }

        [Parameter("Show TP3", Group = "14 · Display", DefaultValue = true)]
        public bool ShowTP3 { get; set; }

        [Parameter("Show TP4", Group = "14 · Display", DefaultValue = true)]
        public bool ShowTP4 { get; set; }

        [Parameter("Show Signal Arrow", Group = "14 · Display", DefaultValue = true)]
        public bool ShowSignalArrow { get; set; }

        [Parameter("Show Early Watch", Group = "14 · Display", DefaultValue = true)]
        public bool ShowEarlyWatch { get; set; }

        [Parameter("Show Historical Signals", Group = "14 · Display", DefaultValue = false)]
        public bool ShowHistoricalSignals { get; set; }

        [Parameter("Historical Signal Limit", Group = "14 · Display", DefaultValue = 10, MinValue = 1, MaxValue = 50)]
        public int HistoricalSignalLimit { get; set; }

        [Parameter("Show Unified Panel", Group = "14 · Display", DefaultValue = true)]
        public bool ShowUnifiedPanel { get; set; }

        [Parameter("Panel Position", Group = "14 · Display", DefaultValue = CFIPClean30PanelCorner.BottomLeft)]
        public CFIPClean30PanelCorner PanelPosition { get; set; }

        [Parameter("Panel Width", Group = "14 · Display", DefaultValue = 430, MinValue = 220, MaxValue = 700)]
        public int PanelWidth { get; set; }

        [Parameter("Panel Font Size", Group = "14 · Display", DefaultValue = 11, MinValue = 8, MaxValue = 20)]
        public int PanelFontSize { get; set; }

        [Parameter("Panel Font Family", Group = "14 · Display", DefaultValue = "Arial")]
        public string PanelFontFamily { get; set; }

        [Parameter("Panel Bold", Group = "14 · Display", DefaultValue = false)]
        public bool PanelBold { get; set; }

        [Parameter("Panel Background", Group = "14 · Display", DefaultValue = "Black")]
        public Color PanelBackground { get; set; }

        [Parameter("Panel Background Alpha", Group = "14 · Display", DefaultValue = 225, MinValue = 0, MaxValue = 255)]
        public int PanelBackgroundAlpha { get; set; }

        [Parameter("Panel Border", Group = "14 · Display", DefaultValue = "#3A4656")]
        public Color PanelBorder { get; set; }

        [Parameter("Panel Border Alpha", Group = "14 · Display", DefaultValue = 230, MinValue = 0, MaxValue = 255)]
        public int PanelBorderAlpha { get; set; }

        [Parameter("Panel Border Thickness", Group = "14 · Display", DefaultValue = 1, MinValue = 0, MaxValue = 4)]
        public int PanelBorderThickness { get; set; }

        [Parameter("Panel Corner Radius", Group = "14 · Display", DefaultValue = 5, MinValue = 0, MaxValue = 20)]
        public int PanelCornerRadius { get; set; }

        [Parameter("Panel Padding", Group = "14 · Display", DefaultValue = 9, MinValue = 0, MaxValue = 30)]
        public int PanelPadding { get; set; }

        [Parameter("Panel Text Color", Group = "14 · Display", DefaultValue = "White")]
        public Color PanelTextColor { get; set; }

        [Parameter("Entry Line Color", Group = "14 · Display", DefaultValue = "White")]
        public Color EntryLineColor { get; set; }

        [Parameter("SL Line Color", Group = "14 · Display", DefaultValue = "Red")]
        public Color SlLineColor { get; set; }

        [Parameter("TP Line Color", Group = "14 · Display", DefaultValue = "Lime")]
        public Color TpLineColor { get; set; }

        [Parameter("BUY Arrow Color", Group = "14 · Display", DefaultValue = "Lime")]
        public Color BuyArrowColor { get; set; }

        [Parameter("SELL Arrow Color", Group = "14 · Display", DefaultValue = "Red")]
        public Color SellArrowColor { get; set; }

        [Parameter("Live Trigger Score", Group = "15 · Advanced Control", DefaultValue = 4, MinValue = 1, MaxValue = 6)]
        public int LiveTriggerScore { get; set; }

        [Parameter("Precision Trigger Score", Group = "15 · Advanced Control", DefaultValue = 5, MinValue = 2, MaxValue = 6)]
        public int PrecisionTriggerScore { get; set; }

        [Parameter("Allow Strong M5 Trigger Override", Group = "15 · Advanced Control", DefaultValue = true)]
        public bool AllowStrongM5TriggerOverride { get; set; }

        [Parameter("M5 Only Confirmed Trigger", Group = "15 · Advanced Control", DefaultValue = true)]
        public bool M5OnlyConfirmedTrigger { get; set; }

        [Parameter("Allow M15 Neutral Pullback", Group = "15 · Advanced Control", DefaultValue = true)]
        public bool AllowM15NeutralPullback { get; set; }

        [Parameter("Higher TF Penalty", Group = "15 · Advanced Control", DefaultValue = 7, MinValue = 0, MaxValue = 20)]
        public int HigherTfPenalty { get; set; }

        [Parameter("Use Zone Confluence", Group = "15 · Advanced Control", DefaultValue = true)]
        public bool UseZoneConfluence { get; set; }

        [Parameter("Use Higher TF Liquidity Targets", Group = "15 · Advanced Control", DefaultValue = true)]
        public bool UseHigherTfLiquidityTargets { get; set; }

        [Parameter("Minimum HTF Target RR", Group = "15 · Advanced Control", DefaultValue = 2.50, MinValue = 1, MaxValue = 20)]
        public double MinimumHtfTargetRR { get; set; }

        [Parameter("Structural TP RR Step", Group = "15 · Advanced Control", DefaultValue = 0.50, MinValue = 0.10, MaxValue = 2.0, Step = 0.05)]
        public double StructuralTpRrStep { get; set; }

        [Parameter("Minimum Trade RR", Group = "15 · Advanced Control", DefaultValue = 2.00, MinValue = 0.5, MaxValue = 20)]
        public double MinimumTradeRR { get; set; }

        [Parameter("Use RR Filter", Group = "15 · Advanced Control", DefaultValue = true)]
        public bool UseRRFilter { get; set; }

        [Parameter("Avoid Late Entry", Group = "15 · Advanced Control", DefaultValue = true)]
        public bool AvoidLateEntry { get; set; }

        [Parameter("Use Precision Execution Model", Group = "15 · Advanced Control", DefaultValue = true)]
        public bool UsePrecisionExecutionModel { get; set; }

        [Parameter("Stop Buffer ATR", Group = "15 · Advanced Control", DefaultValue = 0.10, MinValue = 0.01, MaxValue = 1.0)]
        public double StopBufferAtr { get; set; }

        [Parameter("Require HTF Targets", Group = "15 · Advanced Control", DefaultValue = false)]
        public bool RequireHtfTargets { get; set; }

        [Parameter("Maximum Structural Stop ATR", Group = "15 · Advanced Control", DefaultValue = 2.25, MinValue = 0.5, MaxValue = 10)]
        public double MaximumStructuralStopAtr { get; set; }

        [Parameter("Target Obstacle Lookback Bars", Group = "15 · Advanced Control", DefaultValue = 8, MinValue = 3, MaxValue = 50)]
        public int TargetObstacleLookbackBars { get; set; }

        [Parameter("Allow Direct Displacement Override", Group = "15 · Advanced Control", DefaultValue = true)]
        public bool AllowDirectDisplacementOverride { get; set; }

        [Parameter("Direct Displacement Override Score", Group = "15 · Advanced Control", DefaultValue = 6, MinValue = 3, MaxValue = 6)]
        public int DirectDisplacementOverrideScore { get; set; }

        [Parameter("Maximum Setup Age Bars", Group = "15 · Advanced Control", DefaultValue = 8, MinValue = 1, MaxValue = 50)]
        public int MaximumSetupAgeBars { get; set; }

        [Parameter("Allow Synthetic Target Fallback", Group = "15 · Advanced Control", DefaultValue = true)]
        public bool AllowSyntheticTargetFallback { get; set; }

        [Parameter("HTF Stop Buffer ATR", Group = "15 · Advanced Control", DefaultValue = 0.15, MinValue = 0.01, MaxValue = 1.0)]
        public double HtfStopBufferAtr { get; set; }

        [Parameter("Block New Signal While Active", Group = "15 · Advanced Control", DefaultValue = true)]
        public bool BlockNewSignalWhileActive { get; set; }

        [Parameter("Cooldown Bars", Group = "15 · Advanced Control", DefaultValue = 3, MinValue = 0, MaxValue = 50)]
        public int CooldownBars { get; set; }

        [Parameter("Use News Event Guard", Group = "15 · Advanced Control", DefaultValue = true)]
        public bool UseNewsEventGuard { get; set; }

        [Parameter("Use Volatility Event Guard", Group = "15 · Advanced Control", DefaultValue = true)]
        public bool UseVolatilityEventGuard { get; set; }

        [Parameter("Event Guard Cooldown Bars", Group = "15 · Advanced Control", DefaultValue = 3, MinValue = 0, MaxValue = 50)]
        public int EventGuardCooldownBars { get; set; }

        [Parameter("Use Regime No-Trade Guard", Group = "15 · Advanced Control", DefaultValue = true)]
        public bool UseRegimeNoTradeGuard { get; set; }

        [Parameter("Show Reaction Arrow", Group = "15 · Advanced Control", DefaultValue = true)]
        public bool ShowReactionArrow { get; set; }

        [Parameter("Show Historical Arrows", Group = "15 · Advanced Control", DefaultValue = true)]
        public bool ShowHistoricalArrows { get; set; }

        [Parameter("Enable Dynamic Structural Stop Alias", Group = "15 · Advanced Control", DefaultValue = true)]
        public bool EnableDynamicSlTrail { get; set; }

        [Parameter("Structural Stop Breathing ATR", Group = "15 · Advanced Control", DefaultValue = 0.85, MinValue = 0.20, MaxValue = 5)]
        public double TrailDistanceAtr { get; set; }

        [Parameter("Structural Stop Step ATR", Group = "15 · Advanced Control", DefaultValue = 0.08, MinValue = 0.01, MaxValue = 1)]
        public double TrailStepAtr { get; set; }

        [Parameter("Target Update Step ATR", Group = "15 · Advanced Control", DefaultValue = 0.20, MinValue = 0.02, MaxValue = 2)]
        public double TargetUpdateStepAtr { get; set; }

        [Parameter("Use Swing Structure In Structural Stop", Group = "15 · Advanced Control", DefaultValue = true)]
        public bool UseSwingStructureInTrail { get; set; }

        [Parameter("Smart Minimum Independent Evidence", Group = "15 · Advanced Control", DefaultValue = 4, MinValue = 2, MaxValue = 8)]
        public int SmartMinimumIndependentEvidence { get; set; }

        [Parameter("Smart Stop Zone Bonus", Group = "15 · Advanced Control", DefaultValue = 10, MinValue = 0, MaxValue = 30)]
        public int SmartStopZoneBonus { get; set; }

        [Parameter("Smart Liquidity Pool Bonus", Group = "15 · Advanced Control", DefaultValue = 12, MinValue = 0, MaxValue = 30)]
        public int SmartLiquidityPoolBonus { get; set; }

        [Parameter("Smart Trail Minimum RR", Group = "15 · Advanced Control", DefaultValue = 1.00, MinValue = 0.5, MaxValue = 10)]
        public double SmartTrailMinimumRR { get; set; }

        [Parameter("Smart Use Closed-Bar Decision", Group = "15 · Advanced Control", DefaultValue = true)]
        public bool SmartUseClosedBarDecision { get; set; }

        [Parameter("Smart Target Nearest Bias", Group = "15 · Advanced Control", DefaultValue = 0.65, MinValue = 0.20, MaxValue = 1.0, Step = 0.05)]
        public double SmartTargetNearestBias { get; set; }

        [Parameter("Require Smart Consensus", Group = "15 · Advanced Control", DefaultValue = true)]
        public bool RequireSmartConsensus { get; set; }

        [Parameter("Smart Strong Setup Quality", Group = "15 · Advanced Control", DefaultValue = 82, MinValue = 60, MaxValue = 99)]
        public int SmartStrongSetupQuality { get; set; }

        [Parameter("Smart Strong Setup Edge", Group = "15 · Advanced Control", DefaultValue = 10, MinValue = 4, MaxValue = 30)]
        public int SmartStrongSetupEdge { get; set; }

        [Parameter("Smart Flip Confirmation Bars", Group = "15 · Advanced Control", DefaultValue = 2, MinValue = 1, MaxValue = 5)]
        public int SmartFlipConfirmationBars { get; set; }

        [Parameter("Allow Smart Soft Gate", Group = "15 · Advanced Control", DefaultValue = true)]
        public bool AllowSmartSoftGate { get; set; }

        [Parameter("Fast Reversal Minimum Quality", Group = "15 · Advanced Control", DefaultValue = 74, MinValue = 50, MaxValue = 95)]
        public int FastReversalMinimumQuality { get; set; }

        [Parameter("Fast Reversal Lookback Bars", Group = "15 · Advanced Control", DefaultValue = 6, MinValue = 3, MaxValue = 15)]
        public int FastReversalLookbackBars { get; set; }

        [Parameter("Fast Reversal Minimum Zone Quality", Group = "15 · Advanced Control", DefaultValue = 60, MinValue = 40, MaxValue = 90)]
        public int FastReversalMinimumZoneQuality { get; set; }

        [Parameter("Allow Fast M5 Reversal Before M15", Group = "15 · Advanced Control", DefaultValue = true)]
        public bool AllowFastM5ReversalBeforeM15 { get; set; }

        [Parameter("Retest Lookback Bars", Group = "15 · Advanced Control", DefaultValue = 8, MinValue = 3, MaxValue = 30)]
        public int RetestLookbackBars { get; set; }

        [Parameter("Retest Max Bars After Displacement", Group = "15 · Advanced Control", DefaultValue = 6, MinValue = 1, MaxValue = 20)]
        public int RetestMaxBarsAfterDisplacement { get; set; }

        [Parameter("Retest Zone Tolerance ATR", Group = "15 · Advanced Control", DefaultValue = 0.25, MinValue = 0.05, MaxValue = 1)]
        public double RetestZoneToleranceAtr { get; set; }

        [Parameter("Retest Rejection Body ATR", Group = "15 · Advanced Control", DefaultValue = 0.12, MinValue = 0.02, MaxValue = 1)]
        public double RetestRejectionBodyAtr { get; set; }

        [Parameter("Require Retest Close Confirmation", Group = "15 · Advanced Control", DefaultValue = true)]
        public bool RequireRetestCloseConfirmation { get; set; }

        [Parameter("Use Extended Liquidity Map", Group = "15 · Advanced Control", DefaultValue = true)]
        public bool UseExtendedLiquidityMap { get; set; }

        [Parameter("Use Session Liquidity Targets", Group = "15 · Advanced Control", DefaultValue = true)]
        public bool UseSessionLiquidityTargets { get; set; }

        [Parameter("Liquidity Target Minimum Score", Group = "15 · Advanced Control", DefaultValue = 72, MinValue = 40, MaxValue = 100)]
        public int LiquidityTargetMinimumScore { get; set; }

        [Parameter("Target Obstacle Buffer ATR", Group = "15 · Advanced Control", DefaultValue = 0.10, MinValue = 0.01, MaxValue = 1)]
        public double TargetObstacleBufferAtr { get; set; }

        [Parameter("Require Obstacle Free TP1", Group = "15 · Advanced Control", DefaultValue = true)]
        public bool RequireObstacleFreeTp1 { get; set; }

        // ============================================================
        // 22 · CONFLUENCE EXTENSIONS
        // ============================================================

        [Parameter("Use Volume Expansion", Group = "22 · Confluence Extensions", DefaultValue = false)]
        public bool UseVolumeExpansion { get; set; }

        [Parameter("Volume Expansion Ratio", Group = "22 · Confluence Extensions", DefaultValue = 1.15, MinValue = 1.0, MaxValue = 3.0, Step = 0.05)]
        public double VolumeExpansionRatio { get; set; }

        [Parameter("Use MACD Bias", Group = "22 · Confluence Extensions", DefaultValue = false)]
        public bool UseMacdBias { get; set; }

        [Parameter("MACD Fast Period", Group = "22 · Confluence Extensions", DefaultValue = 12, MinValue = 2, MaxValue = 50)]
        public int MacdFastPeriod { get; set; }

        [Parameter("MACD Slow Period", Group = "22 · Confluence Extensions", DefaultValue = 26, MinValue = 3, MaxValue = 100)]
        public int MacdSlowPeriod { get; set; }

        [Parameter("Use VWAP Bias", Group = "22 · Confluence Extensions", DefaultValue = false)]
        public bool UseVwapBias { get; set; }

        [Parameter("VWAP Lookback Bars", Group = "22 · Confluence Extensions", DefaultValue = 48, MinValue = 10, MaxValue = 200)]
        public int VwapLookbackBars { get; set; }

        [Parameter("Use Healthy Volatility", Group = "22 · Confluence Extensions", DefaultValue = false)]
        public bool UseHealthyVolatility { get; set; }

        [Parameter("Healthy ATR Minimum Ratio", Group = "22 · Confluence Extensions", DefaultValue = 0.85, MinValue = 0.50, MaxValue = 1.50, Step = 0.05)]
        public double HealthyAtrMinimumRatio { get; set; }

        [Parameter("Healthy ATR Maximum Ratio", Group = "22 · Confluence Extensions", DefaultValue = 1.80, MinValue = 1.0, MaxValue = 3.0, Step = 0.05)]
        public double HealthyAtrMaximumRatio { get; set; }

        // ============================================================
        // RUNTIME MODELS
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
            public bool HealthyBull;
            public bool HealthyBear;
        }

        private sealed class Level
        {
            public double Price;
            public double Score;
            public string Kind;
            public string Timeframe;
            public int Age;
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

[Parameter("Enable Early Prediction", Group = "15 · Early Intelligence", DefaultValue = true)]
        public bool EnableEarlyPrediction { get; set; }

[Parameter("Minimum Early Confidence", Group = "15 · Early Intelligence", DefaultValue = 56, MinValue = 40, MaxValue = 95)]
        public int MinimumEarlyConfidence { get; set; }

[Parameter("Prediction Lookahead Bars", Group = "15 · Early Intelligence", DefaultValue = 10, MinValue = 2, MaxValue = 30)]
        public int PredictionLookaheadBars { get; set; }

[Parameter("Show Prediction Zone", Group = "15 · Early Intelligence", DefaultValue = true)]
        public bool ShowPredictionZone { get; set; }

[Parameter("Show Prediction Targets", Group = "15 · Early Intelligence", DefaultValue = true)]
        public bool ShowPredictionTargets { get; set; }

[Parameter("Use Liquidity Forecast", Group = "15 · Early Intelligence", DefaultValue = true)]
        public bool UseLiquidityForecast { get; set; }

[Parameter("Alert On Early Setup", Group = "15 · Early Intelligence", DefaultValue = true)]
        public bool AlertOnEarlySetup { get; set; }

[Parameter("Alert On BOS", Group = "15 · Early Intelligence", DefaultValue = true)]
        public bool AlertOnBos { get; set; }

[Parameter("Alert On MSS / CHOCH", Group = "15 · Early Intelligence", DefaultValue = true)]
        public bool AlertOnMssChoch { get; set; }

[Parameter("Alert On Liquidity Sweep", Group = "15 · Early Intelligence", DefaultValue = true)]
        public bool AlertOnLiquiditySweep { get; set; }

[Parameter("Enable Outcome Telemetry", Group = "15 · Early Intelligence", DefaultValue = true)]
        public bool EnableOutcomeTelemetry { get; set; }

[Parameter("Outcome Maximum M5 Bars", Group = "15 · Early Intelligence", DefaultValue = 72, MinValue = 10, MaxValue = 500)]
        public int OutcomeMaximumM5Bars { get; set; }

[Parameter("Enable Confidence Calibration", Group = "15 · Early Intelligence", DefaultValue = true)]
        public bool EnableConfidenceCalibration { get; set; }

[Parameter("Calibration Directional Minimum Samples", Group = "15 · Early Intelligence", DefaultValue = 6, MinValue = 2, MaxValue = 250)]
        public int CalibrationDirectionalMinimumSamples { get; set; }

[Parameter("Calibration Max Confidence Adjustment", Group = "15 · Early Intelligence", DefaultValue = 8, MinValue = 0, MaxValue = 20)]
        public int CalibrationMaxConfidenceAdjustment { get; set; }

[Parameter("Show Outcome Diagnostics", Group = "15 · Early Intelligence", DefaultValue = true)]
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

[Parameter("Invalidate On False Signal", Group = "16 · Accuracy", DefaultValue = true)]
        public bool InvalidateOnFalseSignal { get; set; }

[Parameter("Enable Setup Invalidation", Group = "16 · Accuracy", DefaultValue = true)]
        public bool EnableSetupInvalidation { get; set; }

[Parameter("Enable Live Structural Reversal", Group = "16 · Accuracy", DefaultValue = true)]
        public bool EnableLiveStructuralReversal { get; set; }

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

[Parameter("Alert On Live Reaction", Group = "18 · Alerts", DefaultValue = true)]
        public bool AlertOnLiveReaction { get; set; }

[Parameter("Enable Level Hit Alerts", Group = "18 · Alerts", DefaultValue = true)]
        public bool EnableLevelHitAlerts { get; set; }

[Parameter("Alert On TP1", Group = "18 · Alerts", DefaultValue = true)]
        public bool AlertOnTp1 { get; set; }

[Parameter("Alert On TP2", Group = "18 · Alerts", DefaultValue = true)]
        public bool AlertOnTp2 { get; set; }

[Parameter("Alert On TP3", Group = "18 · Alerts", DefaultValue = true)]
        public bool AlertOnTp3 { get; set; }

[Parameter("Alert On TP4", Group = "18 · Alerts", DefaultValue = true)]
        public bool AlertOnTp4 { get; set; }

[Parameter("Alert On SL", Group = "18 · Alerts", DefaultValue = true)]
        public bool AlertOnSl { get; set; }

[Parameter("Alert On False Signal Risk", Group = "18 · Alerts", DefaultValue = true)]
        public bool AlertOnFalseSignalRisk { get; set; }

[Parameter("Alert On Entry Restriction", Group = "18 · Alerts", DefaultValue = false)]
        public bool AlertOnEntryRestriction { get; set; }

[Parameter("Alert On High Confidence Entry", Group = "18 · Alerts", DefaultValue = true)]
        public bool AlertOnHighConfidenceEntry { get; set; }

[Parameter("High Confidence Threshold", Group = "18 · Alerts", DefaultValue = 82, MinValue = 70, MaxValue = 99)]
        public int HighConfidenceThreshold { get; set; }

[Parameter("Alert Sound Type", Group = "18 · Alerts", DefaultValue = SoundType.PositiveNotification)]
        public cAlgo.API.SoundType AlertSoundType { get; set; }

[Parameter("Sound File Path", Group = "18 · Alerts", DefaultValue = "")]
        public string SoundFilePath { get; set; }

[Parameter("Popup Position", Group = "18 · Alerts", DefaultValue = CFIPClean30PanelCorner.TopRight)]
        public CFIPClean30PanelCorner PopupPosition { get; set; }

[Parameter("Popup Width", Group = "18 · Alerts", DefaultValue = 430, MinValue = 220, MaxValue = 700)]
        public int PopupWidth { get; set; }

[Parameter("Keep Popup Until Next Alert", Group = "18 · Alerts", DefaultValue = false)]
        public bool KeepPopupUntilNextAlert { get; set; }

[Parameter("Show Popup Close Button", Group = "18 · Alerts", DefaultValue = true)]
        public bool ShowPopupCloseButton { get; set; }

[Parameter("Popup Background", Group = "18 · Alerts", DefaultValue = "Black")]
        public Color PopupBackgroundColor { get; set; }

[Parameter("Popup Background Alpha", Group = "18 · Alerts", DefaultValue = 235, MinValue = 0, MaxValue = 255)]
        public int PopupBackgroundAlpha { get; set; }

[Parameter("Popup Border", Group = "18 · Alerts", DefaultValue = "#3A4656")]
        public Color PopupBorderColor { get; set; }

[Parameter("Popup Border Thickness", Group = "18 · Alerts", DefaultValue = 1, MinValue = 0, MaxValue = 4)]
        public int PopupBorderThickness { get; set; }

[Parameter("Popup Corner Radius", Group = "18 · Alerts", DefaultValue = 5, MinValue = 0, MaxValue = 20)]
        public int PopupCornerRadius { get; set; }

[Parameter("Popup Padding", Group = "18 · Alerts", DefaultValue = 8, MinValue = 0, MaxValue = 30)]
        public int PopupPadding { get; set; }

[Parameter("Popup Text Color", Group = "18 · Alerts", DefaultValue = "White")]
        public Color PopupTextColor { get; set; }

[Parameter("Line Length Bars", Group = "19 · Display Advanced", DefaultValue = 40, MinValue = 5, MaxValue = 300)]
        public int LineLengthBars { get; set; }

[Parameter("Line Forward Bars", Group = "19 · Display Advanced", DefaultValue = 10, MinValue = 1, MaxValue = 100)]
        public int LineForwardBars { get; set; }

[Parameter("Show Signal Labels", Group = "19 · Display Advanced", DefaultValue = true)]
        public bool ShowSignalLabels { get; set; }

[Parameter("Show Level Price Labels", Group = "19 · Display Advanced", DefaultValue = true)]
        public bool ShowLevelPriceLabels { get; set; }

[Parameter("Show Context Event Marker", Group = "19 · Display Advanced", DefaultValue = true)]
        public bool ShowContextEventMarker { get; set; }

[Parameter("Show Prediction Objects", Group = "19 · Display Advanced", DefaultValue = true)]
        public bool ShowPredictionObjects { get; set; }

[Parameter("Arrow Offset ATR", Group = "19 · Display Advanced", DefaultValue = 0.18, MinValue = 0.02, MaxValue = 1)]
        public double ArrowOffsetAtr { get; set; }

[Parameter("Minimum Arrow Offset Pips", Group = "19 · Display Advanced", DefaultValue = 2.0, MinValue = 0.5, MaxValue = 20)]
        public double MinimumArrowOffsetPips { get; set; }

[Parameter("Show Early Arrow", Group = "19 · Display Advanced", DefaultValue = true)]
        public bool ShowEarlyArrow { get; set; }

[Parameter("Show Panel Toggle Button", Group = "19 · Display Advanced", DefaultValue = true)]
        public bool ShowPanelToggleButton { get; set; }

[Parameter("Panel Toggle Width", Group = "19 · Display Advanced", DefaultValue = 110, MinValue = 80, MaxValue = 220)]
        public int PanelToggleWidth { get; set; }

[Parameter("Panel Toggle Height", Group = "19 · Display Advanced", DefaultValue = 25, MinValue = 20, MaxValue = 50)]
        public int PanelToggleHeight { get; set; }

[Parameter("Action Button Width", Group = "19 · Display Advanced", DefaultValue = 150, MinValue = 100, MaxValue = 240)]
        public int ActionButtonWidth { get; set; }

[Parameter("Action Button Height", Group = "19 · Display Advanced", DefaultValue = 25, MinValue = 20, MaxValue = 50)]
        public int ActionButtonHeight { get; set; }

[Parameter("Auto Protect Broker Positions", Group = "20 · Broker Protection", DefaultValue = false)]
        public bool AutoProtectBrokerPositions { get; set; }

[Parameter("Managed Position Label", Group = "20 · Broker Protection", DefaultValue = "")]
        public string ManagedPositionLabel { get; set; }

[Parameter("Sync Broker Take Profit", Group = "20 · Broker Protection", DefaultValue = false)]
        public bool SyncBrokerTakeProfit { get; set; }

[Parameter("Prevent Broker TP Backward Move", Group = "20 · Broker Protection", DefaultValue = true)]
        public bool PreventBrokerTpBackwardMove { get; set; }

[Parameter("Broker Modify Cooldown ms", Group = "20 · Broker Protection", DefaultValue = 750, MinValue = 100, MaxValue = 5000, Step = 50)]
        public int BrokerModifyCooldownMs { get; set; }

[Parameter("Enable Aggressive Auto Entry", Group = "21 · Auto Intelligence", DefaultValue = false)]
        public bool EnableAggressiveAutoEntry { get; set; }

[Parameter("Aggressive Minimum Confidence", Group = "21 · Auto Intelligence", DefaultValue = 88, MinValue = 50, MaxValue = 99)]
        public int AggressiveMinimumConfidence { get; set; }

[Parameter("Aggressive Minimum Evidence", Group = "21 · Auto Intelligence", DefaultValue = 4, MinValue = 1, MaxValue = 8)]
        public int AggressiveMinimumEvidence { get; set; }

[Parameter("Aggressive Minimum Smart Quality", Group = "21 · Auto Intelligence", DefaultValue = 78, MinValue = 50, MaxValue = 95)]
        public int AggressiveMinimumSmartQuality { get; set; }

[Parameter("Aggressive Risk % Equity", Group = "21 · Auto Intelligence", DefaultValue = 0.25, MinValue = 0.05, MaxValue = 5)]
        public double AggressiveRiskPercentEquity { get; set; }

[Parameter("Aggressive TP Stage", Group = "21 · Auto Intelligence", DefaultValue = CFIPClean30TargetStage.TP1)]
        public CFIPClean30TargetStage AggressiveTpStage { get; set; }

[Parameter("Aggressive Require Smart Agreement", Group = "21 · Auto Intelligence", DefaultValue = true)]
        public bool AggressiveRequireSmartAgreement { get; set; }

        private sealed class Prediction
        {
            public int Direction;
            public int Confidence;
            public double ZoneLow;
            public double ZoneHigh;
            public double Trigger;
            public double Target;
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
            public double Entry;
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
            public int CreatedM5;
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
        // STATE
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

        private Plan _plan;
        private Decision _decision;
        private Decision _reaction;
        private Prediction _prediction;

        private int _lastEvaluatedM5 = -1;
        private int _lastSignalM5 = -1;
        private int _lastAutoM5 = -1;
        private int _lastEarlyAlertM5 = -1;
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
        private string _alertKey = "";
        private DateTime _alertUtc = DateTime.MinValue;

        private Border _panel;
        private StackPanel _panelStack;
        private TextBlock _panelText;
        private StackPanel _buttonStack;
        private Button _closeButton;
        private Button _cancelButton;

        private Border _popup;
        private TextBlock _popupText;
        private DateTime _popupUntilUtc = DateTime.MinValue;

        private const string P = "CFIP_CLEAN30_";
        private const string H = "CFIP_CLEAN30_H_";

        private int _lastContextM5 = -1;
        private int _lastBrokerModifyM5 = -1;
        private int _lastInvalidationAlertM5 = -1;
        private bool _panelHidden;
        private Button _panelToggleButton;
        private Button _popupCloseButton;

        private readonly Dictionary<int, int> _directionSamples =
            new Dictionary<int, int>();

        private readonly Dictionary<int, int> _directionWins =
            new Dictionary<int, int>();

        private DateTime _lastBrokerModifyUtc = DateTime.MinValue;
        private int _lastEventGuardM5 = -1;
        private int _lastRestrictionM5 = -1;
        private int _lastSmartDecisionAlertM5 = -1;
        private int _lastHistoricalHostBar = -1;

        // ============================================================
        // LIFECYCLE
        // ============================================================

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

            RegisterAllNative();
            _status = "READY";
        }

        protected override void OnDestroy()
        {
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

                _decision =
                    SmartUseClosedBarDecision
                        ? BuildDecision(
                            index,
                            closedM5,
                            reference)
                        : BuildDecision(
                            index,
                            Math.Max(
                                1,
                                _m5Bars.Count - 1),
                            reference);

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
                            "CFIP CLEAN30 HIGH CONFIDENCE | " +
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
                        AlertOnEntryRestriction &&
                        _lastRestrictionM5 !=
                        closedM5 &&
                        !string.IsNullOrWhiteSpace(
                            _decision.BlockReason))
                    {
                        SendUnifiedAlert(
                            "RESTRICT|" +
                            closedM5 +
                            "|" +
                            _decision.BlockReason,
                            "CFIP CLEAN30 ENTRY BLOCKED | " +
                            _decision.BlockReason,
                            _decision.Direction,
                            false);

                        _lastRestrictionM5 =
                            closedM5;
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
                            "CFIP CLEAN30 SMART DECISION | " +
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

                if (_plan == null &&
                    _decision != null &&
                    _decision.EntryAllowed &&
                    ShouldCreatePlan(
                        closedM5))
                {
                    Plan plan =
                        BuildPlan(
                            closedM5,
                            _decision.Direction);

                    if (plan != null)
                        ActivatePlan(plan);
                }

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

            EvaluateActivePlan(
                closedM5);

            if (_plan != null)
                RenderPlan();
            else
                RenderWatchAndReaction(
                    index,
                    closedM5);

            TryAutoTrade(
                closedM5);

            TryAggressiveAutoTrade(
                closedM5);

            ProtectBrokerPositions(
                closedM5);

            MonitorOutcome(
                closedM5);

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
        // NATIVE INDICATORS
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
                    "CFIP CLEAN30 indicator initialization failed: {0}",
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
        // FRAME ANALYSIS
        // ============================================================

        private Frame AnalyzeFrame(
            Bars bars,
            int index)
        {
            Frame f = new Frame
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
                BullMss(
                    bars,
                    index,
                    f.Atr);

            f.MssBear =
                BearMss(
                    bars,
                    index,
                    f.Atr);

            f.ChochBull =
                BullChoch(
                    bars,
                    index);

            f.ChochBear =
                BearChoch(
                    bars,
                    index);

            f.DisplacementBull =
                BullDisplacement(
                    bars,
                    index,
                    f.Atr);

            f.DisplacementBear =
                BearDisplacement(
                    bars,
                    index,
                    f.Atr);

            f.LiquidityBull =
                BullLiquiditySweep(
                    bars,
                    index);

            f.LiquidityBear =
                BearLiquiditySweep(
                    bars,
                    index);

            f.FvgBull =
                FindNearestFvg(
                    bars,
                    index,
                    1,
                    f.Atr) != null;

            f.FvgBear =
                FindNearestFvg(
                    bars,
                    index,
                    -1,
                    f.Atr) != null;

            f.ObBull =
                FindNearestOrderBlock(
                    bars,
                    index,
                    1,
                    f.Atr) != null;

            f.ObBear =
                FindNearestOrderBlock(
                    bars,
                    index,
                    -1,
                    f.Atr) != null;

            f.TrendBull =
                f.EmaFast > f.EmaSlow &&
                bars.ClosePrices[index] > f.EmaFast;

            f.TrendBear =
                f.EmaFast < f.EmaSlow &&
                bars.ClosePrices[index] < f.EmaFast;

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

            f.EqualHigh =
                FindEqualHigh(
                    bars,
                    index,
                    bars.ClosePrices[index],
                    f.Atr) > 0;

            f.EqualLow =
                FindEqualLow(
                    bars,
                    index,
                    bars.ClosePrices[index],
                    f.Atr) > 0;

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

            f.HealthyBull =
                HasHealthyVolatility(
                    bars,
                    index,
                    1);

            f.HealthyBear =
                HasHealthyVolatility(
                    bars,
                    index,
                    -1);

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
            AddScore(f.EqualLow, 5, ref bull, ref evidence);
            AddScore(f.EqualHigh, 5, ref bear, ref evidence);
            AddScore(f.VolumeBull, 4, ref bull, ref evidence);
            AddScore(f.VolumeBear, 4, ref bear, ref evidence);
            AddScore(f.MacdBull, 4, ref bull, ref evidence);
            AddScore(f.MacdBear, 4, ref bear, ref evidence);
            AddScore(f.VwapBull, 4, ref bull, ref evidence);
            AddScore(f.VwapBear, 4, ref bear, ref evidence);
            AddScore(f.HealthyBull, 3, ref bull, ref evidence);
            AddScore(f.HealthyBear, 3, ref bear, ref evidence);

            if (f.Rsi > 50)
                bull += 3;
            else if (f.Rsi < 50)
                bear += 3;

            if (f.Adx >= AdxMinimum)
            {
                if (DmiBias(bars, index) > 0)
                    bull += 4;
                else if (DmiBias(bars, index) < 0)
                    bear += 4;
            }

            if (UseEmaSlope && index > 2)
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
                    bull = Math.Max(0, bull - 5);

                if (f.Rsi <= 25)
                    bear = Math.Max(0, bear - 5);
            }

            f.BullScore = bull;
            f.BearScore = bear;
            f.Evidence = evidence;

            if (bull >= 35 && bull >= bear + 8)
                f.Direction = 1;
            else if (bear >= 35 && bear >= bull + 8)
                f.Direction = -1;

            double total =
                Math.Max(
                    1,
                    bull + bear);

            int share =
                (int)Math.Round(
                    100.0 *
                    Math.Max(bull, bear) /
                    total);

            f.Quality =
                ClampInt(
                    (int)Math.Round(
                        share * 0.55 +
                        Math.Min(100, f.Adx * 1.5) * 0.15 +
                        Math.Min(100, evidence * 5) * 0.30),
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
            Decision d = new Decision();

            double buy = 0;
            double sell = 0;
            int evidence = 0;

            AddFrame(
                _m5Frame,
                M5Weight,
                ref buy,
                ref sell,
                ref evidence);

            AddFrame(
                _m15Frame,
                M15Weight,
                ref buy,
                ref sell,
                ref evidence);

            AddFrame(
                _m30Frame,
                M30Weight,
                ref buy,
                ref sell,
                ref evidence);

            AddFrame(
                _h1Frame,
                H1Weight,
                ref buy,
                ref sell,
                ref evidence);

            AddFrame(
                _h4Frame,
                H4Weight,
                ref buy,
                ref sell,
                ref evidence);

            AddFrame(
                _d1Frame,
                D1Weight,
                ref buy,
                ref sell,
                ref evidence);

            AddFrame(
                _w1Frame,
                W1Weight,
                ref buy,
                ref sell,
                ref evidence);

            if (UseAdvancedConfluence)
            {
                buy += LiveBias(chartIndex, 1);
                sell += LiveBias(chartIndex, -1);
            }

            if (EnableSmartDecisionEngine &&
                UsePremiumDiscount)
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

            if (EnableSmartDecisionEngine &&
                UseM1Trigger &&
                _m1Frame != null)
            {
                if (_m1Frame.Direction == 1)
                    buy += 3;
                else if (_m1Frame.Direction == -1)
                    sell += 3;
            }

            if (UsePremiumDiscount)
            {
                int pd = PremiumDiscountBias(
                    _m5Bars,
                    closedM5);

                if (pd == 1)
                    buy += 6;
                else if (pd == -1)
                    sell += 6;
            }

            if (UseM1Trigger && _m1Frame != null)
            {
                if (_m1Frame.Direction == 1)
                    buy += 3;
                else if (_m1Frame.Direction == -1)
                    sell += 3;
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

            double softTotal =
                Math.Max(
                    1e-9,
                    expBuy + expSell);

            int buyShare =
                ClampInt(
                    (int)Math.Round(
                        100.0 *
                        expBuy /
                        softTotal),
                    0,
                    100);

            int sellShare =
                100 -
                buyShare;

            d.Direction =
                buyShare >= sellShare
                    ? 1
                    : -1;

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

            d.Regime =
                DetectRegime(
                    _m5Bars,
                    closedM5);

            d.RegimeQuality =
                RegimeQuality(
                    d.Regime,
                    _m5Bars,
                    closedM5);

            d.SmartQuality =
                ClampInt(
                    (int)Math.Round(
                        Math.Max(buyShare, sellShare) * 0.28 +
                        d.TimeframeAgreement * 0.24 +
                        Math.Min(100, d.IndependentEvidence * 14) * 0.18 +
                        Math.Min(100, d.StructuralConfirmations * 18) * 0.18 +
                        d.RegimeQuality * 0.12),
                    0,
                    100);

            d.Confidence =
                ClampInt(
                    (int)Math.Round(
                        Math.Max(buyShare, sellShare) * 0.45 +
                        d.TimeframeAgreement * 0.25 +
                        d.SmartQuality * 0.30),
                    0,
                    100);

            d.Confidence =
                CalibratedConfidence(
                    d.Confidence,
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

                if (h1Against || h4Against)
                {
                    d.Confidence =
                        ClampInt(
                            d.Confidence -
                            HigherTfPenalty,
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

        private int IndependentEvidence(int direction)
        {
            int count = 0;

            if (direction == 1)
            {
                if (_m5Frame.StructureBull) count++;
                if (_m5Frame.LiquidityBull) count++;
                if (_m5Frame.FvgBull) count++;
                if (_m5Frame.ObBull) count++;
                if (_m5Frame.DisplacementBull) count++;
                if (_m5Frame.MomentumBull) count++;
            }
            else
            {
                if (_m5Frame.StructureBear) count++;
                if (_m5Frame.LiquidityBear) count++;
                if (_m5Frame.FvgBear) count++;
                if (_m5Frame.ObBear) count++;
                if (_m5Frame.DisplacementBear) count++;
                if (_m5Frame.MomentumBear) count++;
            }

            return count;
        }

        private int StructuralConfirmations(int direction)
        {
            int count = 0;

            if (direction == 1)
            {
                if (_m5Frame.StructureBull) count++;
                if (_m5Frame.MssBull || _m5Frame.ChochBull) count++;
                if (_m5Frame.DisplacementBull) count++;
                if (_m15Frame.StructureBull) count++;
                if (_h1Frame.StructureBull) count++;
                if (_h4Frame.StructureBull) count++;
            }
            else
            {
                if (_m5Frame.StructureBear) count++;
                if (_m5Frame.MssBear || _m5Frame.ChochBear) count++;
                if (_m5Frame.DisplacementBear) count++;
                if (_m15Frame.StructureBear) count++;
                if (_h1Frame.StructureBear) count++;
                if (_h4Frame.StructureBear) count++;
            }

            return count;
        }

        private bool PassesDecisionFilters(
            int chartIndex,
            int closedM5,
            DateTime reference,
            Decision d,
            out string reason)
        {
            reason = "";

            if (d.Direction == 0)
            {
                reason = "NO DIRECTION";
                return false;
            }

            if (d.Confidence < MinimumConfidence)
            {
                reason = "CONFIDENCE";
                return false;
            }

            if (d.Edge < MinimumEdge)
            {
                reason = "EDGE";
                return false;
            }

            if (d.SmartQuality < MinimumSmartQuality)
            {
                reason = "SMART QUALITY";
                return false;
            }

            if (RequireHigherTfAgreement &&
                d.TimeframeAgreement < MinimumTimeframeAgreement)
            {
                reason = "MTF AGREEMENT";
                return false;
            }

            if (d.IndependentEvidence < MinimumIndependentEvidence)
            {
                reason = "INDEPENDENT EVIDENCE";
                return false;
            }

            if (RequireStructuralConfirmation &&
                d.StructuralConfirmations < MinimumStructuralConfirmations)
            {
                reason = "STRUCTURE";
                return false;
            }

            if (RequireCoreAgreement)
            {
                bool m15Neutral =
                    _m15Frame != null &&
                    _m15Frame.Direction == 0;

                if (d.Direction == 1 &&
                    (_m5Frame.Direction != 1 ||
                     (!m15Neutral &&
                      _m15Frame.Direction != 1) ||
                     (m15Neutral &&
                      !AllowM15NeutralPullback)))
                {
                    reason = "CORE ALIGNMENT";
                    return false;
                }

                if (d.Direction == -1 &&
                    (_m5Frame.Direction != -1 ||
                     (!m15Neutral &&
                      _m15Frame.Direction != -1) ||
                     (m15Neutral &&
                      !AllowM15NeutralPullback)))
                {
                    reason = "CORE ALIGNMENT";
                    return false;
                }
            }

            if (UseM5Confirmation &&
                ((d.Direction == 1 &&
                  _m5Frame.Direction != 1) ||
                 (d.Direction == -1 &&
                  _m5Frame.Direction != -1)))
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
                reason = "M5 TRIGGER";
                return false;
            }

            if (UseM1Trigger &&
                _m1Frame != null &&
                _m1Frame.Direction != 0 &&
                _m1Frame.Direction != d.Direction)
            {
                reason = "M1 MISALIGNMENT";
                return false;
            }
            
if (UseM1Trigger &&
                _m1Frame != null &&
                _m1Frame.Direction != 0 &&
                _m1Frame.Direction != d.Direction)
            {
                reason = "M1 MISALIGNMENT";
                return false;
            }

            if (UseSmartEntryQualityFilter)
            {
                int smartFloor =
                    Math.Max(
                        40,
                        SmartQualityThreshold);

                if (EnableSmartDecisionEngine &&
                    RequireSmartConsensus)
                {
                    smartFloor =
                        Math.Max(
                            smartFloor,
                            SmartConsensusThreshold);
                }
                else if (EnableSmartDecisionEngine &&
                         AllowSmartSoftGate)
                {
                    smartFloor =
                        Math.Max(
                            smartFloor,
                            SmartMinimumConsensusFloor());
                }

                if (d.SmartQuality <
                    smartFloor)
                {
                    reason =
                        "SMART QUALITY";
                    return false;
                }
            }

            if (EnableSmartDecisionEngine &&
                d.IndependentEvidence <
                Math.Max(
                    MinimumIndependentEvidence,
                    SmartMinimumIndependentEvidence))
            {
                reason =
                    "SMART EVIDENCE";
                return false;
            }

            if (EnableSmartDecisionEngine &&
                RequireSmartConsensus &&
                d.Edge <
                Math.Max(
                    MinimumEdge,
                    SmartStrongSetupEdge))
            {
                reason =
                    "SMART CONSENSUS";
                return false;
            }

            if (EnableSmartDecisionEngine &&
                d.TimeframeAgreement <
                SmartMinimumTimeframeAgreement)
            {
                reason = "SMART MTF";
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
            
if (UseM1Trigger &&
                _m1Frame != null &&
                _m1Frame.Direction != 0 &&
                _m1Frame.Direction != d.Direction)
            {
                reason = "M1 TRIGGER MISALIGNED";
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
                m15Closed >= StableM15Bars + 5 &&
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
                d.RetestQuality < MinimumRetestQuality)
            {
                if (!(AllowStrongTriggerOverride &&
                      AllowStrongM5TriggerOverride &&
                      d.Confidence >= 85 &&
                      d.Edge >= 20 &&
                      d.IndependentEvidence >= MinimumIndependentEvidence + 1))
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

            if (!SessionAllowed(DateTime.UtcNow))
            {
                reason = "SESSION";
                return false;
            }

            if (!FridayAllowed(DateTime.UtcNow))
            {
                reason = "FRIDAY";
                return false;
            }

            if (!SpreadAllowed(_m5Bars, closedM5))
            {
                reason = "SPREAD";
                return false;
            }

            if (UseVolatilityGuard &&
                VolatilityBlocked(
                    _m5Bars,
                    closedM5))
            {
                reason = "VOLATILITY GUARD";
                return false;
            }

            if (UseVolatilityEventGuard &&
                VolatilityBlocked(
                    _m5Bars,
                    closedM5))
            {
                _lastEventGuardM5 =
                    closedM5;

                reason =
                    "VOLATILITY EVENT";
                return false;
            }

            if (UseVolatilityEventGuard &&
                EventGuardCooldownBars > 0 &&
                _lastEventGuardM5 >= 0 &&
                closedM5 -
                _lastEventGuardM5 <
                EventGuardCooldownBars)
            {
                reason =
                    "EVENT COOLDOWN";
                return false;
            }

            if (UseNewsEventGuard &&
                NewsBlocked(
                    DateTime.UtcNow,
                    out reason))
                return false;

            if (CooldownBlocked(closedM5))
            {
                reason = "COOLDOWN";
                return false;
            }

            return true;
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
        // REACTION
        // ============================================================

        private Decision BuildReaction()
        {
            if (_m5Bars == null ||
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

            int minimumQuality =
                Math.Max(
                    50,
                    FastReversalMinimumQuality);

            if (buyQuality < minimumQuality &&
                sellQuality < minimumQuality)
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
                minimumQuality &&
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
        // ENTRY TRIGGER
        // ============================================================

        private bool EntryTriggerReady(
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
                body < atr * MinimumTriggerBodyAtr)
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

            if (range > atr * MaximumTriggerRangeAtr)
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

            double triggerBuffer =
                atr *
                Math.Max(
                    0.0,
                    EntryBufferAtr);

            bool extensionOk =
                Math.Abs(
                    market -
                    bars.ClosePrices[index]) <=
                atr *
                MaximumEntryExtensionAtr;

            bool bufferPassed =
                direction == 1
                    ? market >=
                      bars.ClosePrices[index] +
                      triggerBuffer
                    : market <=
                      bars.ClosePrices[index] -
                      triggerBuffer;

            if (RequireFreshM5Trigger &&
                FreshTriggerEvidence(
                    bars,
                    index,
                    direction) <
                MinimumFreshTriggerEvidence)
            {
                bool directOverride =
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

                if (!directOverride)
                    return false;
            }

            bool strongM5Override =
                AllowStrongM5TriggerOverride &&
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

            if ((trigger >= 5 ||
                 strongM5Override) &&
                breakReady &&
                bufferPassed)
                return true;

            return
                trigger >= 4 &&
                breakReady &&
                extensionOk &&
                bufferPassed;
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
        // PLAN
        // ============================================================

        private Plan BuildPlan(
            int closedM5,
            int direction)
        {
            double entry =
                NormalizePrice(
                    direction == 1
                        ? Symbol.Ask
                        : Symbol.Bid);

            if (AvoidLateEntry)
            {
                double referenceClose =
                    _m5Bars.ClosePrices[
                        closedM5];

                double executionAtr =
                    Atr(
                        _m5Bars,
                        closedM5);

                if (executionAtr > 0 &&
                    Math.Abs(
                        entry -
                        referenceClose) >
                    executionAtr *
                    MaximumEntryExtensionAtr)
                    return null;
            }

            double atr =
                Atr(
                    _m5Bars,
                    closedM5);

            if (!IsFinitePositive(entry) ||
                atr <= 0)
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

                stopSource = "ATR";
                stopQuality = 50;
            }

            double risk =
                Math.Abs(
                    entry -
                    stop);

            if (risk <
                    atr *
                    MinimumSlAtr ||
                risk >
                    atr *
                    Math.Min(
                        MaximumSlAtr,
                        MaximumStructuralStopAtr))
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
                    entry,
                    risk,
                    direction,
                    atr);

            double tp1 =
                SelectTarget(
                    selected,
                    0,
                    entry,
                    risk,
                    direction,
                    Tp1MinimumRR);

            double tp2 =
                SelectTarget(
                    selected,
                    1,
                    entry,
                    risk,
                    direction,
                    Tp2MinimumRR);

            double tp3 =
                SelectTarget(
                    selected,
                    2,
                    entry,
                    risk,
                    direction,
                    Tp3MinimumRR);

            double tp4 =
                SelectTarget(
                    selected,
                    3,
                    entry,
                    risk,
                    direction,
                    Tp4MinimumRR);

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
                MinimumTradeRR)
                return null;

            if (RequireHtfTargets &&
                !HasAnyHtfTargetLevel(
                    candidates))
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

            if (Math.Abs(tp1 - entry) <
                risk *
                MinimumRequiredRR())
                return null;

            Plan p = new Plan();

            p.Direction = direction;
            p.Entry = entry;
            p.Stop = NormalizePrice(stop);
            p.Tp1 = NormalizePrice(tp1);
            p.Tp2 =
                IsValidTarget(
                    direction,
                    entry,
                    tp2)
                    ? NormalizePrice(tp2)
                    : 0;
            p.Tp3 =
                IsValidTarget(
                    direction,
                    entry,
                    tp3)
                    ? NormalizePrice(tp3)
                    : 0;
            p.Tp4 =
                IsValidTarget(
                    direction,
                    entry,
                    tp4)
                    ? NormalizePrice(tp4)
                    : 0;

            p.Risk =
                Math.Abs(
                    p.Entry -
                    p.Stop);

            p.Tp1RR =
                Math.Abs(
                    p.Tp1 -
                    p.Entry) /
                p.Risk;

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

            p.StopSource = stopSource;
            p.StopQuality = stopQuality;

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

            p.CreatedM5 = closedM5;

            return p;
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

            double best =
                direction == 1
                    ? FindSwingLowBelow(
                        _m5Bars,
                        closedM5,
                        entry)
                    : FindSwingHighAbove(
                        _m5Bars,
                        closedM5,
                        entry);

            if (IsFinitePositive(best))
            {
                source = "SWING";
                quality = 84;
            }

            if (UseHtfStructureForStop)
            {
                int htfIndex =
                    ClosedIndex(
                        _h1Bars,
                        _m5Bars.OpenTimes[closedM5]);

                double htf =
                    direction == 1
                        ? FindSwingLowBelow(
                            _h1Bars,
                            htfIndex,
                            entry)
                        : FindSwingHighAbove(
                            _h1Bars,
                            htfIndex,
                            entry);

                if (IsFinitePositive(htf))
                {
                    if (!IsFinitePositive(best))
                    {
                        best = htf;
                        source = "HTF STRUCTURE";
                        quality = 88;
                    }
                    else if (direction == 1 &&
                             htf > best &&
                             htf < entry)
                    {
                        best = htf;
                        source = "HTF STRUCTURE";
                        quality = 88;
                    }
                    else if (direction == -1 &&
                             htf < best &&
                             htf > entry)
                    {
                        best = htf;
                        source = "HTF STRUCTURE";
                        quality = 88;
                    }
                }
            }

            Zone zone =
                FindNearestOpposingZone(
                    _m5Bars,
                    closedM5,
                    direction,
                    atr);

            if (zone != null)
            {
                double level =
                    direction == 1
                        ? zone.Low
                        : zone.High;

                if (IsValidStop(
                        direction,
                        entry,
                        level))
                {
                    bool improve =
                        !IsFinitePositive(best) ||
                        (direction == 1 &&
                         level > best &&
                         level < entry) ||
                        (direction == -1 &&
                         level < best &&
                         level > entry);

                    if (improve)
                    {
                        best = level;
                        source = zone.Kind;
                        quality = zone.Quality;
                    }
                }
            }

            if (!IsFinitePositive(best))
                return 0;

            double buffer =
                atr *
                Math.Max(
                    0.02,
                    StopBufferAtr);

            if (source.IndexOf(
                    "HTF",
                    StringComparison.OrdinalIgnoreCase) >= 0)
            {
                buffer =
                    Math.Max(
                        buffer,
                        atr *
                        Math.Max(
                            0.02,
                            HtfStopBufferAtr));
            }

            best =
                direction == 1
                    ? best - buffer
                    : best + buffer;

            if (source.IndexOf(
                    "ZONE",
                    StringComparison.OrdinalIgnoreCase) >= 0 ||
                source.IndexOf(
                    "FVG",
                    StringComparison.OrdinalIgnoreCase) >= 0 ||
                source.IndexOf(
                    "ORDER_BLOCK",
                    StringComparison.OrdinalIgnoreCase) >= 0)
            {
                quality =
                    ClampInt(
                        quality +
                        Math.Max(
                            0,
                            SmartStopZoneBonus),
                        0,
                        100);
            }

            double liquidityLevel =
                direction == 1
                    ? FindEqualLow(
                        _m5Bars,
                        closedM5,
                        entry,
                        atr)
                    : FindEqualHigh(
                        _m5Bars,
                        closedM5,
                        entry,
                        atr);

            if (liquidityLevel > 0 &&
                Math.Abs(
                    best -
                    liquidityLevel) <=
                atr * 0.20)
            {
                quality =
                    ClampInt(
                        quality +
                        Math.Max(
                            0,
                            SmartLiquidityPoolBonus),
                        0,
                        100);
            }

            best = NormalizePrice(best);

            if (!IsValidStop(
                    direction,
                    entry,
                    best))
                return 0;

            return best;
        }

        private List<Level> BuildTargetLevels(
            int closedM5,
            int direction,
            double entry,
            double atr)
        {
            List<Level> levels =
                new List<Level>();

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
                "SWING",
                "M5",
                0,
                SwingStructureWeight);

            double equal =
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
                equal,
                "EQUAL_POOL",
                "M5",
                0,
                EqualHighLowWeight);

            Zone fvg =
                FindNearestFvg(
                    _m5Bars,
                    closedM5,
                    direction,
                    atr);

            if (fvg != null)
            {
                AddLevel(
                    levels,
                    direction == 1
                        ? fvg.High
                        : fvg.Low,
                    "FVG",
                    "M5",
                    fvg.Age,
                    FvgWeight);
            }

            Zone ob =
                FindNearestOrderBlock(
                    _m5Bars,
                    closedM5,
                    direction,
                    atr);

            if (ob != null)
            {
                AddLevel(
                    levels,
                    direction == 1
                        ? ob.High
                        : ob.Low,
                    "ORDER_BLOCK",
                    "M5",
                    ob.Age,
                    OrderBlockWeight);
            }

            AddHtfTargets(
                levels,
                direction,
                entry,
                atr,
                _m5Bars.OpenTimes[closedM5]);

            AddPreviousPeriodLevels(
                levels,
                direction,
                entry,
                atr,
                _m5Bars.OpenTimes[closedM5]);

            AddSupplyDemandAndLiquidityLevels(
                levels,
                closedM5,
                direction,
                entry,
                atr);

            AddSmartExtraTargetLevels(
                levels,
                closedM5,
                direction,
                entry,
                atr);

            if (levels.Count >
                Math.Max(
                    1,
                    SmartTargetMaxCandidates))
            {
                levels =
                    levels
                    .OrderByDescending(
                        x => x.Score)
                    .Take(
                        Math.Max(
                            1,
                            SmartTargetMaxCandidates))
                    .ToList();
            }

            return MergeLevels(
                levels,
                atr);
        }

        private void AddSupplyDemandAndLiquidityLevels(
            List<Level> levels,
            int closedM5,
            int direction,
            double entry,
            double atr)
        {
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
                LiquidityPoolWeight);

            int d1 =
                ClosedIndex(
                    _d1Bars,
                    _m5Bars.OpenTimes[closedM5]);

            if (UseExtendedLiquidityMap &&
                d1 > 1)
            {
                AddLiquidityLevel(
                    levels,
                    direction == 1
                        ? _d1Bars.HighPrices[d1 - 2]
                        : _d1Bars.LowPrices[d1 - 2],
                    "LIQUIDITY_POOL",
                    "D1",
                    2,
                    LiquidityPoolWeight);
            }

            int w1 =
                ClosedIndex(
                    _w1Bars,
                    _m5Bars.OpenTimes[closedM5]);

            if (UseExtendedLiquidityMap &&
                w1 > 1)
            {
                AddLiquidityLevel(
                    levels,
                    direction == 1
                        ? _w1Bars.HighPrices[w1 - 2]
                        : _w1Bars.LowPrices[w1 - 2],
                    "LIQUIDITY_POOL",
                    "W1",
                    2,
                    PreviousWeekWeight);
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

                AddLiquidityLevel(
                    levels,
                    direction == 1
                        ? sessionHigh
                        : sessionLow,
                    "SESSION",
                    "M5",
                    0,
                    SessionWeight);
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

            DateTime anchor = bars.OpenTimes[index];

            DateTime dayStart =
                new DateTime(
                    anchor.Year,
                    anchor.Month,
                    anchor.Day,
                    0,
                    0,
                    0);

            DateTime from =
                dayStart.AddHours(startHour);

            DateTime to =
                startHour < endHour
                    ? dayStart.AddHours(endHour)
                    : dayStart.AddDays(1).AddHours(endHour);

            int first = -1;
            int last = -1;

            for (int i = index;
                 i >= Math.Max(0, index - 400);
                 i--)
            {
                DateTime t = bars.OpenTimes[i];

                if (t < from)
                    break;

                if (t <= to)
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

        private void AddSmartExtraTargetLevels(
            List<Level> levels,
            int closedM5,
            int direction,
            double entry,
            double atr)
        {
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
                LiquidityPoolWeight);
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
                _h4Bars
            };

            string[] names =
            {
                "M15",
                "M30",
                "H1",
                "H4"
            };

            int[] weights =
            {
                MtfClusterWeight,
                MtfClusterWeight,
                HtfStructureWeight,
                HtfStructureWeight
            };

            for (int i = 0; i < frames.Length; i++)
            {
                int idx =
                    ClosedIndex(
                        frames[i],
                        reference);

                if (idx < 10)
                    continue;

                double level =
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
                    level,
                    "MTF_CLUSTER",
                    names[i],
                    0,
                    weights[i]);
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

        private void AddLiquidityLevel(
            List<Level> levels,
            double price,
            string kind,
            string timeframe,
            int age,
            double baseScore)
        {
            if (baseScore <
                Math.Max(
                    0,
                    LiquidityTargetMinimumScore))
                return;

            AddLevel(
                levels,
                price,
                kind,
                timeframe,
                age,
                baseScore);
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

            Level l = new Level();

            l.Price =
                NormalizePrice(price);

            l.Kind = kind;
            l.Timeframe = timeframe;
            l.Age = Math.Max(0, age);
            l.Score =
                baseScore *
                FamilyWeight(kind) /
                100.0;

            levels.Add(l);
        }

        private double FamilyWeight(string kind)
        {
            string k = kind ?? "";

            if (k.IndexOf(
                    "SUPPLY",
                    StringComparison.OrdinalIgnoreCase) >= 0 ||
                k.IndexOf(
                    "DEMAND",
                    StringComparison.OrdinalIgnoreCase) >= 0)
                return SupplyDemandWeight;

            if (k.IndexOf(
                    "FVG",
                    StringComparison.OrdinalIgnoreCase) >= 0)
                return FvgWeight;

            if (k.IndexOf(
                    "ORDER_BLOCK",
                    StringComparison.OrdinalIgnoreCase) >= 0)
                return OrderBlockWeight;

            if (k.IndexOf(
                    "LIQUIDITY",
                    StringComparison.OrdinalIgnoreCase) >= 0)
                return LiquidityPoolWeight;

            if (k.IndexOf(
                    "EQUAL",
                    StringComparison.OrdinalIgnoreCase) >= 0)
                return EqualHighLowWeight;

            if (k.IndexOf(
                    "MTF",
                    StringComparison.OrdinalIgnoreCase) >= 0)
                return MtfClusterWeight;

            if (k == "PREVIOUS_DAY")
                return PreviousDayWeight;

            if (k == "PREVIOUS_WEEK")
                return PreviousWeekWeight;

            if (k == "SESSION")
                return SessionWeight;

            if (k.IndexOf(
                    "HTF",
                    StringComparison.OrdinalIgnoreCase) >= 0)
                return HtfStructureWeight;

            return SwingStructureWeight;
        }

        private List<Level> MergeLevels(
            List<Level> input,
            double atr)
        {
            List<Level> result =
                new List<Level>();

            double tolerance =
                Math.Max(
                    Symbol.PipSize * 2,
                    atr * 0.10);

            for (int i = 0; i < input.Count; i++)
            {
                Level current = input[i];

                Level match =
                    result.FirstOrDefault(
                        x => Math.Abs(
                                 x.Price -
                                 current.Price) <=
                             tolerance);

                if (match == null)
                {
                    result.Add(current);
                    continue;
                }

                if (current.Score > match.Score)
                    match.Score = current.Score;

                if (current.Age < match.Age)
                    match.Age = current.Age;

                if (current.Kind == "MTF_CLUSTER")
                    match.Kind = current.Kind;
            }

            return result;
        }

        private List<Level> SelectTargets(
            List<Level> levels,
            double entry,
            double risk,
            int direction,
            double atr)
        {
            List<Level> selected =
                new List<Level>();

            double[] requiredRR =
            {
                Tp1MinimumRR,
                Tp2MinimumRR,
                Tp3MinimumRR,
                Tp4MinimumRR
            };

            for (int stage = 0;
                 stage < requiredRR.Length;
                 stage++)
            {
                Level best = null;
                double last =
                    selected.Count == 0
                        ? entry
                        : selected[selected.Count - 1].Price;

                for (int i = 0; i < levels.Count; i++)
                {
                    Level candidate =
                        levels[i];

                    if (!IsValidTarget(
                            direction,
                            entry,
                            candidate.Price))
                        continue;

                    if (selected.Any(
                        x => Math.Abs(
                                 x.Price -
                                 candidate.Price) <=
                             atr *
                             MinimumTpSpacingAtr *
                             0.50))
                        continue;

                    double distance =
                        Math.Abs(
                            candidate.Price -
                            entry);

                    if (distance >
                        atr *
                        Math.Max(
                            1.0,
                            MaximumTargetExtensionAtr))
                        continue;

                    double rr =
                        distance /
                        Math.Max(
                            Symbol.PipSize,
                            risk);

                    if (candidate.Age >
                        MaximumSetupAgeBars)
                        continue;

                    if ((candidate.Timeframe == "H1" ||
                         candidate.Timeframe == "H4" ||
                         candidate.Timeframe == "D1" ||
                         candidate.Timeframe == "W1"))
                    {
                        if (!UseHigherTfLiquidityTargets)
                            continue;

                        if (rr <
                            Math.Max(
                                requiredRR[stage],
                                MinimumHtfTargetRR))
                            continue;
                    }

                    if (rr < requiredRR[stage])
                        continue;

                    if (selected.Count > 0)
                    {
                        if (direction == 1 &&
                            candidate.Price <=
                            last +
                            atr *
                            MinimumTpSpacingAtr)
                            continue;

                        if (direction == -1 &&
                            candidate.Price >=
                            last -
                            atr *
                            MinimumTpSpacingAtr)
                            continue;
                    }

                    bool requireObstacleFree =
                        stage == 0
                            ? RequireObstacleFreeTp1
                            : true;

                    if (RejectTargetObstacle &&
                        requireObstacleFree &&
                        HasTargetObstacle(
                            _m5Bars,
                            _m5Bars.Count - 2,
                            direction,
                            entry,
                            candidate.Price,
                            atr))
                        continue;

                    double targetDistance =
                        Math.Abs(
                            candidate.Price -
                            entry);

                    double selectionScore =
                        candidate.Score *
                        (1.0 +
                         SmartTargetNearestBias /
                         (1.0 +
                          targetDistance /
                          Math.Max(
                              Symbol.PipSize,
                              atr)));

                    double bestScore =
                        best == null
                            ? double.MinValue
                            : best.Score *
                              (1.0 +
                               SmartTargetNearestBias /
                               (1.0 +
                                Math.Abs(
                                    best.Price -
                                    entry) /
                                Math.Max(
                                    Symbol.PipSize,
                                    atr)));

                    if (best == null ||
                        selectionScore > bestScore)
                        best = candidate;
                }

                if (best == null)
                    continue;

                selected.Add(best);
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
                position < selected.Count)
                return selected[position].Price;

            if (!AllowSyntheticTargetFallback)
                return 0;

            return NormalizePrice(
                direction == 1
                    ? entry + risk * alternateRR
                    : entry - risk * alternateRR);
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
        // ACTIVE PLAN
        // ============================================================

        private void ActivatePlan(Plan plan)
        {
            _plan = plan;
            _lastSignalM5 = plan.CreatedM5;
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

            ClearWatchObjects();

            string message =
                "CFIP CLEAN30 " +
                (plan.Direction == 1 ? "BUY" : "SELL") +
                " | CONF " +
                (_decision == null
                    ? 0
                    : _decision.Confidence) +
                " | SMART " +
                (_decision == null
                    ? 0
                    : _decision.SmartQuality) +
                " | ENTRY " +
                Price(plan.Entry) +
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

        private void EvaluateActivePlan(int closedM5)
        {
            if (_plan == null)
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

            double peakRR =
                favorable /
                Math.Max(
                    Symbol.PipSize,
                    _plan.Risk);

            if (EnableLiveExitManagement)
            {
                double protectedStop =
                    CalculateProtectedStop(
                        market,
                        peakRR,
                        closedM5);

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
                    peakRR >= TargetUpdateTriggerRR)
                {
                    UpdateUnhitTargetsLive(
                        closedM5,
                        market);
                }
            }

            bool hitSl =
                _plan.Direction == 1
                    ? market <= _plan.Stop
                    : market >= _plan.Stop;

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
                _slHit = true;
                RegisterOutcome(
                    _plan.Direction,
                    false);
                _losses++;

                if (AlertOnLevelHit &&
                    EnableLevelHitAlerts &&
                    AlertOnSl)
                {
                    SendUnifiedAlert(
                        "SL|" +
                        _plan.CreatedM5,
                        "CFIP CLEAN30 SL HIT | " +
                        Price(_plan.Stop),
                        -1,
                        true);
                }

                _plan = null;
                RemovePlanObjects();
                return;
            }

            if (hitTp1 &&
                _tp1Hit == 0)
            {
                _tp1Hit = 1;

                if (AlertOnLevelHit &&
                    EnableLevelHitAlerts &&
                    AlertOnTp1)
                {
                    SendUnifiedAlert(
                        "TP1|" +
                        _plan.CreatedM5,
                        "CFIP CLEAN30 TP1 HIT | " +
                        Price(_plan.Tp1),
                        _plan.Direction,
                        true);
                }
            }

            if (hitTp2 &&
                _tp2Hit == 0)
            {
                _tp2Hit = 1;

                if (AlertOnLevelHit &&
                    EnableLevelHitAlerts &&
                    AlertOnTp2)
                {
                    SendUnifiedAlert(
                        "TP2|" +
                        _plan.CreatedM5,
                        "CFIP CLEAN30 TP2 HIT | " +
                        Price(_plan.Tp2),
                        _plan.Direction,
                        false);
                }
            }

            if (hitTp3 &&
                _tp3Hit == 0)
            {
                _tp3Hit = 1;

                if (AlertOnLevelHit &&
                    EnableLevelHitAlerts &&
                    AlertOnTp3)
                {
                    SendUnifiedAlert(
                        "TP3|" +
                        _plan.CreatedM5,
                        "CFIP CLEAN30 TP3 HIT | " +
                        Price(_plan.Tp3),
                        _plan.Direction,
                        false);
                }
            }

            if (hitTp4 &&
                _tp4Hit == 0)
            {
                _tp4Hit = 1;
                RegisterOutcome(
                    _plan.Direction,
                    true);
                _wins++;

                if (AlertOnLevelHit &&
                    EnableLevelHitAlerts &&
                    AlertOnTp4)
                {
                    SendUnifiedAlert(
                        "TP4|" +
                        _plan.CreatedM5,
                        "CFIP CLEAN30 TP4 HIT | " +
                        Price(_plan.Tp4),
                        _plan.Direction,
                        true);
                }

                _plan = null;
                RemovePlanObjects();
                return;
            }

            if (CheckLiveReversalAgainstPlan(
                    closedM5))
            {
                return;
            }

            if (UseFalseSignalGuard &&
                EnableSetupInvalidation &&
                currentMove < 0 &&
                Math.Abs(currentMove) >=
                _plan.Risk *
                Math.Max(
                    0.25,
                    FalseSignalAdverseR) &&
                AlertOnFalseSignalRisk)
            {
                if (_lastInvalidationAlertM5 !=
                    closedM5)
                {
                    _lastInvalidationAlertM5 =
                        closedM5;

                    SendUnifiedAlert(
                        "INVALID-RISK|" +
                        closedM5,
                        "CFIP CLEAN30 FALSE SIGNAL RISK | " +
                        (_plan.Direction == 1
                            ? "BUY"
                            : "SELL"),
                        0,
                        true);
                }

                if (InvalidateOnFalseSignal &&
                    Math.Abs(currentMove) >=
                    _plan.Risk *
                    Math.Max(
                        1.0,
                        FalseSignalAdverseR))
                {
                    RegisterOutcome(
                        _plan.Direction,
                        false);

                    _losses++;
                    _plan = null;
                    RemovePlanObjects();
                    return;
                }
            }

            if (currentMove < 0 &&
                Math.Abs(currentMove) >=
                _plan.Risk * 1.10 &&
                AlertOnInvalidated)
            {
                SendUnifiedAlert(
                    "INVALID|" +
                    _plan.CreatedM5,
                    "CFIP CLEAN30 SETUP UNDER PRESSURE | " +
                    (_plan.Direction == 1 ? "BUY" : "SELL") +
                    " | " +
                    (Math.Abs(currentMove) /
                     _plan.Risk).ToString("F2") +
                    "R",
                    0,
                    true);
            }
        }

        private double CalculateProtectedStop(
            double market,
            double peakRR,
            int closedM5)
        {
            double candidate =
                _plan.Stop;

            if (MoveSlToBreakEven &&
                peakRR >= BreakEvenTriggerRR)
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

            if ((EnableStructuralSlRepricing ||
                 EnableDynamicSlTrail) &&
                peakRR >=
                Math.Max(
                    SlRepriceStartRR,
                    SmartTrailMinimumRR))
            {
                double atr =
                    Atr(
                        _m5Bars,
                        closedM5);

                if (atr > 0)
                {
                    double structural = 0;

                    if (UseSwingStructureInTrail)
                    {
                        structural =
                            _plan.Direction == 1
                                ? FindSwingLowBelow(
                                    _m5Bars,
                                    closedM5,
                                    market)
                                : FindSwingHighAbove(
                                    _m5Bars,
                                    closedM5,
                                    market);
                    }

                    if (!IsFinitePositive(
                            structural))
                    {
                        Zone trailZone =
                            FindNearestOpposingZone(
                                _m5Bars,
                                closedM5,
                                _plan.Direction,
                                atr);

                        if (trailZone != null)
                        {
                            structural =
                                _plan.Direction == 1
                                    ? trailZone.Low
                                    : trailZone.High;
                        }
                    }

                    if (IsFinitePositive(
                            structural))
                    {
                        double room =
                            atr *
                            Math.Max(
                                0.10,
                                Math.Max(
                                    SlRepriceBreathingAtr,
                                    TrailDistanceAtr));

                        if (_plan.Direction == 1 &&
                            structural <=
                            market - room)
                        {
                            candidate =
                                Math.Max(
                                    candidate,
                                    structural);
                        }
                        else if (_plan.Direction == -1 &&
                                 structural >=
                                 market + room)
                        {
                            candidate =
                                Math.Min(
                                    candidate,
                                    structural);
                        }
                    }
                }
            }

            double minimumDistance =
                Math.Max(
                    Symbol.PipSize * 2,
                    Symbol.Ask - Symbol.Bid);

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

            double step =
                Atr(
                    _m5Bars,
                    closedM5) *
                Math.Max(
                    0.01,
                    Math.Max(
                        SlRepriceStepAtr,
                        TrailStepAtr));

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
                    market,
                    atr);

            for (int i = 0;
                 i < levels.Count;
                 i++)
            {
                Level level =
                    levels[i];

                if (_plan.Direction == 1)
                {
                    if (_tp1Hit == 0 &&
                        level.Price >
                        _plan.Tp1 +
                        atr *
                        Math.Max(
                            0.05,
                            TargetUpdateStepAtr))
                    {
                        _plan.Tp1 =
                            level.Price;

                        ApplyTargetMeta(
                            levels,
                            _plan.Tp1,
                            atr,
                            out _plan.Tp1Source,
                            out _plan.Tp1Quality);

                        break;
                    }

                    if (_tp2Hit == 0 &&
                        _plan.Tp2 > 0 &&
                        level.Price >
                        _plan.Tp2 +
                        atr *
                        MinimumTpSpacingAtr)
                    {
                        _plan.Tp2 =
                            level.Price;

                        ApplyTargetMeta(
                            levels,
                            _plan.Tp2,
                            atr,
                            out _plan.Tp2Source,
                            out _plan.Tp2Quality);

                        break;
                    }
                }
                else
                {
                    if (_tp1Hit == 0 &&
                        level.Price <
                        _plan.Tp1 -
                        atr *
                        Math.Max(
                            0.05,
                            TargetUpdateStepAtr))
                    {
                        _plan.Tp1 =
                            level.Price;

                        ApplyTargetMeta(
                            levels,
                            _plan.Tp1,
                            atr,
                            out _plan.Tp1Source,
                            out _plan.Tp1Quality);

                        break;
                    }

                    if (_tp2Hit == 0 &&
                        _plan.Tp2 > 0 &&
                        level.Price <
                        _plan.Tp2 -
                        atr *
                        MinimumTpSpacingAtr)
                    {
                        _plan.Tp2 =
                            level.Price;

                        ApplyTargetMeta(
                            levels,
                            _plan.Tp2,
                            atr,
                            out _plan.Tp2Source,
                            out _plan.Tp2Quality);

                        break;
                    }
                }
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
        // STRUCTURE / ZONES
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
            int index)
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

            return
                bars.LowPrices[index] <
                prior &&
                bars.ClosePrices[index] >
                prior &&
                bars.ClosePrices[index] >
                bars.OpenPrices[index];
        }

        private bool BearLiquiditySweep(
            Bars bars,
            int index)
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

            return
                bars.HighPrices[index] >
                prior &&
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

        private Zone FindNearestFvg(
            Bars bars,
            int index,
            int direction,
            double atr)
        {
            if (!UseFvg ||
                bars == null ||
                index < 3 ||
                atr <= 0)
                return null;

            int first =
                Math.Max(
                    2,
                    index -
                    FvgLookback);

            Zone best = null;

            for (int i = index;
                 i >= first;
                 i--)
            {
                if (direction == 1)
                {
                    double gap =
                        bars.LowPrices[i] -
                        bars.HighPrices[i - 2];

                    if (gap <
                        atr *
                        MinimumFvgAtr)
                        continue;

                    Zone z = new Zone();

                    z.Low =
                        bars.HighPrices[i - 2];

                    z.High =
                        bars.LowPrices[i];

                    z.Direction = 1;
                    z.Kind = "FVG";
                    z.Age =
                        index - i;

                    z.Quality =
                        ClampInt(
                            (int)Math.Round(
                                70 +
                                15 *
                                gap /
                                Math.Max(
                                    Symbol.PipSize,
                                    atr)),
                            0,
                            100);

                    if (best == null ||
                        DistanceToZone(
                            bars.ClosePrices[index],
                            z) <
                        DistanceToZone(
                            bars.ClosePrices[index],
                            best))
                        best = z;
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

                    Zone z = new Zone();

                    z.Low =
                        bars.HighPrices[i];

                    z.High =
                        bars.LowPrices[i - 2];

                    z.Direction = -1;
                    z.Kind = "FVG";
                    z.Age =
                        index - i;

                    z.Quality =
                        ClampInt(
                            (int)Math.Round(
                                70 +
                                15 *
                                gap /
                                Math.Max(
                                    Symbol.PipSize,
                                    atr)),
                            0,
                            100);

                    if (best == null ||
                        DistanceToZone(
                            bars.ClosePrices[index],
                            z) <
                        DistanceToZone(
                            bars.ClosePrices[index],
                            best))
                        best = z;
                }
            }

            if (best != null &&
                best.Age > MaximumZoneAgeBars)
                return null;

            if (best != null &&
                RequireFvgRetest)
            {
                double price =
                    bars.ClosePrices[index];

                double tolerance =
                    atr *
                    (UseRetestQualityGate
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

        private Zone FindNearestOrderBlock(
            Bars bars,
            int index,
            int direction,
            double atr)
        {
            if (!UseOrderBlock ||
                bars == null ||
                index < 6 ||
                atr <= 0)
                return null;

            int first =
                Math.Max(
                    2,
                    index -
                    ObLookback);

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

                bool displacement = false;

                for (int j = i + 1;
                     j <= Math.Min(
                         index,
                         i + 3);
                     j++)
                {
                    double body =
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
                        body >=
                        atr *
                        ObDisplacementAtr)
                    {
                        displacement = true;
                        break;
                    }
                }

                if (RequireObDisplacement &&
                    !displacement)
                    continue;

                Zone z =
                    new Zone();

                z.Low =
                    bars.LowPrices[i];

                z.High =
                    bars.HighPrices[i];

                z.Direction =
                    direction;

                z.Kind =
                    "ORDER_BLOCK";

                z.Age =
                    index - i;

                z.Quality =
                    displacement
                        ? 90
                        : 72;

                if (z.Age >
                    MaximumZoneAgeBars)
                    return null;

                return z;
            }

            return null;
        }

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
                index < 10)
                return 0;

            double tolerance =
                atr *
                EqualLevelToleranceAtr;

            double best = 0;

            int first =
                Math.Max(
                    2,
                    index -
                    LiquidityLookback);

            for (int i = first;
                 i < index - 2;
                 i++)
            {
                for (int j = i + 2;
                     j < index;
                     j++)
                {
                    if (Math.Abs(
                            bars.HighPrices[i] -
                            bars.HighPrices[j]) >
                        tolerance)
                        continue;

                    double level =
                        Math.Max(
                            bars.HighPrices[i],
                            bars.HighPrices[j]);

                    if (level > reference &&
                        (best == 0 ||
                         level < best))
                        best = level;
                }
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
                index < 10)
                return 0;

            double tolerance =
                atr *
                EqualLevelToleranceAtr;

            double best = 0;

            int first =
                Math.Max(
                    2,
                    index -
                    LiquidityLookback);

            for (int i = first;
                 i < index - 2;
                 i++)
            {
                for (int j = i + 2;
                     j < index;
                     j++)
                {
                    if (Math.Abs(
                            bars.LowPrices[i] -
                            bars.LowPrices[j]) >
                        tolerance)
                        continue;

                    double level =
                        Math.Min(
                            bars.LowPrices[i],
                            bars.LowPrices[j]);

                    if (level < reference &&
                        (best == 0 ||
                         level > best))
                        best = level;
                }
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
        // RETEST / REGIME / FILTERS
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

            int q = 35;

            Zone zone =
                FindNearestOpposingZone(
                    bars,
                    index,
                    direction,
                    atr);

            double price =
                bars.ClosePrices[index];

            if (zone != null)
            {
                double tolerance =
                    atr *
                    Math.Max(
                        0.05,
                        RetestZoneToleranceAtr);

                if (price >=
                        zone.Low -
                        tolerance &&
                    price <=
                        zone.High +
                        tolerance)
                    q += 30;

                if (zone.Quality >= 80)
                    q += 15;
            }

            double body =
                Math.Abs(
                    bars.ClosePrices[index] -
                    bars.OpenPrices[index]);

            if (body >=
                atr *
                (UseRetestQualityGate
                    ? RetestRejectionBodyAtr
                    : MinimumTriggerBodyAtr))
                q += 10;

            bool directionalClose =
                direction == 1
                    ? bars.ClosePrices[index] >
                      bars.OpenPrices[index]
                    : bars.ClosePrices[index] <
                      bars.OpenPrices[index];

            if (directionalClose)
                q += 5;

            int displacementAge = -1;
            int first =
                Math.Max(
                    5,
                    index -
                    Math.Max(
                        1,
                        RetestLookbackBars));

            for (int i = index;
                 i >= first;
                 i--)
            {
                bool displacement =
                    direction == 1
                        ? BullDisplacement(
                            bars,
                            i,
                            atr)
                        : BearDisplacement(
                            bars,
                            i,
                            atr);

                if (displacement)
                {
                    displacementAge =
                        index - i;
                    break;
                }
            }

            if (displacementAge >= 0)
            {
                if (displacementAge <=
                    Math.Max(
                        1,
                        RetestMaxBarsAfterDisplacement))
                    q += 10;
                else
                    q = Math.Max(
                        0,
                        q - 10);
            }

            if (RequireRetestCloseConfirmation &&
                zone != null &&
                !directionalClose)
                return 0;

            if (zone != null &&
                (price < zone.Low - atr * RetestZoneToleranceAtr ||
                 price > zone.High + atr * RetestZoneToleranceAtr))
                q = Math.Max(
                    0,
                    q - 20);

            return ClampInt(
                q,
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

            double range =
                bars.HighPrices[index] -
                bars.LowPrices[index];

            return
                range >=
                atr *
                EventShockRangeAtr &&
                atr >=
                oldAtr *
                EventShockAtrExpansion;
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
        // PLAN VALIDATION
        // ============================================================

        private bool IsValidTarget(
            int direction,
            double entry,
            double target)
        {
            if (!IsFinitePositive(entry) ||
                !IsFinitePositive(target))
                return false;

            return direction == 1
                ? target > entry
                : direction == -1 &&
                  target < entry;
        }

        private bool IsValidStop(
            int direction,
            double entry,
            double stop)
        {
            if (!IsFinitePositive(entry) ||
                !IsFinitePositive(stop))
                return false;

            return direction == 1
                ? stop < entry
                : direction == -1 &&
                  stop > entry;
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
                index < 5)
                return false;

            int start =
                Math.Max(
                    2,
                    index -
                    Math.Max(
                        3,
                        TargetObstacleLookbackBars));

            for (int i = start;
                 i < index;
                 i++)
            {
                if (direction == 1 &&
                    bars.HighPrices[i] >
                    entry &&
                    bars.HighPrices[i] <
                    target -
                    atr *
                    Math.Max(
                        TargetClearanceAtr,
                        TargetObstacleBufferAtr))
                    return true;

                if (direction == -1 &&
                    bars.LowPrices[i] <
                    entry &&
                    bars.LowPrices[i] >
                    target +
                    atr *
                    Math.Max(
                        TargetClearanceAtr,
                        TargetObstacleBufferAtr))
                    return true;
            }

            return false;
        }

        private bool ShouldCreatePlan(
            int closedM5)
        {
            if (BlockNewSignalWhileActive &&
                _plan != null)
                return false;

            if (_decision == null ||
                !_decision.EntryAllowed)
                return false;

            if (_lastSignalM5 >= 0 &&
                closedM5 -
                _lastSignalM5 <
                Math.Max(
                    CooldownBars,
                    Math.Max(
                        CooldownM5Bars,
                        ExitReentryCooldownM5)))
                return false;

            return
                _lastSignalM5 !=
                closedM5;
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

        private int SmartMinimumConsensusFloor()
        {
            return Math.Max(
                40,
                SmartConsensusThreshold - 12);
        }

        // ============================================================
        // RENDERING
        // ============================================================

        private void RenderPlan()
        {
            if (_plan == null)
            {
                RemovePlanObjects();
                return;
            }

            if (!ShowLevelLines)
            {
                RemovePlanLine(P + "ENTRY");
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

                DrawPlanLine(
                    P + "SL",
                    _plan.Stop,
                    SlLineColor,
                    ShowSL);

                DrawPlanLine(
                    P + "TP1",
                    _plan.Tp1,
                    TpLineColor,
                    ShowTP1);

                DrawPlanLine(
                    P + "TP2",
                    _plan.Tp2,
                    TpLineColor,
                    ShowTP2);

                DrawPlanLine(
                    P + "TP3",
                    _plan.Tp3,
                    TpLineColor,
                    ShowTP3);

                DrawPlanLine(
                    P + "TP4",
                    _plan.Tp4,
                    TpLineColor,
                    ShowTP4);
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
                _plan.Direction == 1
                    ? BuyArrowColor
                    : SellArrowColor);
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

            DrawPlanLabel(
                P + "ENTRY_LABEL",
                "ENTRY " +
                Price(
                    _plan.Entry),
                bar,
                _plan.Entry,
                EntryLineColor);

            DrawPlanLabel(
                P + "SL_LABEL",
                "SL " +
                Price(
                    _plan.Stop),
                bar,
                _plan.Stop,
                SlLineColor);

            DrawPlanLabel(
                P + "TP1_LABEL",
                "TP1 " +
                Price(
                    _plan.Tp1),
                bar,
                _plan.Tp1,
                TpLineColor);

            if (_plan.Tp2 > 0)
                DrawPlanLabel(
                    P + "TP2_LABEL",
                    "TP2 " +
                    Price(
                        _plan.Tp2),
                    bar,
                    _plan.Tp2,
                    TpLineColor);

            if (_plan.Tp3 > 0)
                DrawPlanLabel(
                    P + "TP3_LABEL",
                    "TP3 " +
                    Price(
                        _plan.Tp3),
                    bar,
                    _plan.Tp3,
                    TpLineColor);

            if (_plan.Tp4 > 0)
                DrawPlanLabel(
                    P + "TP4_LABEL",
                    "TP4 " +
                    Price(
                        _plan.Tp4),
                    bar,
                    _plan.Tp4,
                    TpLineColor);
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
                if (!IsFinitePositive(price))
                    return;

                ChartText label =
                    Chart.DrawText(
                        name,
                        text,
                        Bars.OpenTimes[bar],
                        price,
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

                label.IsInteractive =
                    false;
            }
            catch (Exception ex)
            {
                Print(
                    "CFIP CLEAN30 plan label failed: {0}",
                    ex.Message);
            }
        }

        private void RemovePlanLabels()
        {
            Chart.RemoveObject(
                P + "ENTRY_LABEL");
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
                            PlanLineStyle);
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
                    PlanLineStyle;
                line.ExtendToInfinity =
                    false;
                line.IsInteractive =
                    false;
            }
            catch (Exception ex)
            {
                Print(
                    "CFIP CLEAN30 level render failed [{0}]: {1}",
                    name,
                    ex.Message);
            }
        }

        private void RenderWatchAndReaction(
            int chartIndex,
            int closedM5)
        {
            ClearPlanObjects();

            if (!ShowEarlyWatch ||
                _decision == null ||
                _decision.Direction == 0)
                return;

            if (AlertOnEarlyWatch &&
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
                    "CFIP CLEAN30 " +
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

            int hostBar =
                MapM5ToChart(
                    closedM5,
                    chartIndex);

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
                    Symbol.PipSize *
                    Math.Max(
                        0.5,
                        MinimumArrowOffsetPips),
                    atr *
                    Math.Max(
                        0.02,
                        ArrowOffsetAtr));

            if (ShowSignalArrow &&
                ShowEarlyArrow)
            {
                DrawIcon(
                    P + "WATCH_ARROW",
                    _decision.Direction == 1
                        ? ChartIconType.UpArrow
                        : ChartIconType.DownArrow,
                    hostBar,
                    _decision.Direction == 1
                        ? Bars.LowPrices[hostBar] -
                          offset
                        : Bars.HighPrices[hostBar] +
                          offset,
                    _decision.Direction == 1
                        ? BuyArrowColor
                        : SellArrowColor);
            }

            if (ShowReactionArrow &&
                _reaction != null &&
                _reaction.EntryAllowed)
            {
                DrawIcon(
                    P + "REACTION_ARROW",
                    _reaction.Direction == 1
                        ? ChartIconType.UpArrow
                        : ChartIconType.DownArrow,
                    MapM5ToChart(
                        _m5Bars.Count - 1,
                        chartIndex),
                    _reaction.Direction == 1
                        ? Bars.LowPrices[
                              MapM5ToChart(
                                  _m5Bars.Count - 1,
                                  chartIndex)] -
                          offset
                        : Bars.HighPrices[
                              MapM5ToChart(
                                  _m5Bars.Count - 1,
                                  chartIndex)] +
                          offset,
                    _reaction.Direction == 1
                        ? BuyArrowColor
                        : SellArrowColor);

                if (AlertOnReaction &&
                    AlertOnLiveReaction &&
                    (_lastReactionAlertBar() !=
                     _m5Bars.Count - 1))
                {
                    SendUnifiedAlert(
                        "REACTION|" +
                        _m5Bars.Count,
                        _reaction.Reason,
                        _reaction.Direction,
                        false);

                    SetLastReactionAlertBar(
                        _m5Bars.Count - 1);
                }
            }
        }

        private int _reactionAlertBar = -1;

        private int _lastReactionAlertBar()
        {
            return _reactionAlertBar;
        }

        private void SetLastReactionAlertBar(
            int value)
        {
            _reactionAlertBar = value;
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
                Chart.DrawIcon(
                    name,
                    type,
                    Math.Max(
                        0,
                        Math.Min(
                            bar,
                            Bars.Count - 1)),
                    price,
                    color);
            }
            catch (Exception ex)
            {
                Print(
                    "CFIP CLEAN30 icon render failed: {0}",
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

            RemovePlanLine(
                P + "ENTRY");

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

            Chart.RemoveObject(
                P + "ARROW");
        }

        private void RemovePlanLine(
            string name)
        {
            Chart.RemoveObject(name);
        }

        private void RemovePlanObjects()
        {
            ClearPlanObjects();
            RemovePlanLabels();
            RemovePredictionObjects();
        }

        // ============================================================
        // PANEL
        // ============================================================

        private void CreatePanel()
        {
            if (_panel != null)
                return;

            try
            {
                _panelText =
                    new TextBlock
                    {
                        Text = "",
                        IsHitTestVisible = false,
                        TextWrapping = TextWrapping.Wrap,
                        TextAlignment = TextAlignment.Left,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Top,
                        Margin = 0
                    };

                _closeButton =
                    new Button();

                _cancelButton =
                    new Button();

                _buttonStack =
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Bottom
                    };

                _closeButton.Click +=
                    args => CloseAllPositions();

                _cancelButton.Click +=
                    args => CancelAllOrders();

                _closeButton.Margin = 2;
                _cancelButton.Margin = 2;

                _buttonStack.AddChild(
                    _closeButton);

                _buttonStack.AddChild(
                    _cancelButton);

                _panelStack =
                    new StackPanel
                    {
                        Orientation = Orientation.Vertical,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Bottom
                    };

                _panelStack.AddChild(
                    _panelText);

                _panelStack.AddChild(
                    _buttonStack);

                _panel =
                    new Border
                    {
                        Child = _panelStack,
                        IsHitTestVisible = true
                    };

                Chart.AddControl(
                    _panel);

                CreatePanelToggleButton();
            }
            catch (Exception ex)
            {
                Print(
                    "CFIP CLEAN30 panel creation failed: {0}",
                    ex.Message);

                _panel = null;
                _panelStack = null;
                _panelText = null;
                _buttonStack = null;
                _closeButton = null;
                _cancelButton = null;
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
                    new Button();

                _panelToggleButton.Text =
                    "HIDE PANEL";

                _panelToggleButton.Width =
                    Math.Max(
                        80,
                        PanelToggleWidth);

                _panelToggleButton.Height =
                    Math.Max(
                        20,
                        PanelToggleHeight);

                _panelToggleButton.Margin = 8;
                _panelToggleButton.ForegroundColor =
                    PanelTextColor;

                _panelToggleButton.FontSize =
                    Math.Max(
                        8,
                        PanelFontSize - 1);

                _panelToggleButton.FontWeight =
                    PanelBold
                        ? FontWeight.Bold
                        : FontWeight.Normal;

                _panelToggleButton.BackgroundColor =
                    Color.FromArgb(
                        Math.Max(
                            0,
                            Math.Min(
                                255,
                                PanelBackgroundAlpha)),
                        PanelBackground);

                _panelToggleButton.BorderColor =
                    Color.FromArgb(
                        Math.Max(
                            0,
                            Math.Min(
                                255,
                                PanelBorderAlpha)),
                    PanelBorder);

                _panelToggleButton.BorderThickness =
                    Math.Max(
                        0,
                        PanelBorderThickness);

                _panelToggleButton.CornerRadius =
                    Math.Max(
                        0,
                        PanelCornerRadius);

                _panelToggleButton.Click +=
                    args => TogglePanel();

                Chart.AddControl(
                    _panelToggleButton);

                SetPanelToggleAlignment();
            }
            catch (Exception ex)
            {
                Print(
                    "CFIP CLEAN30 panel toggle failed: {0}",
                    ex.Message);
            }
        }

        private void TogglePanel()
        {
            _panelHidden =
                !_panelHidden;

            if (_panel != null)
                _panel.IsVisible =
                    !_panelHidden;

            if (_panelToggleButton != null)
                _panelToggleButton.Text =
                    _panelHidden
                        ? "SHOW PANEL"
                        : "HIDE PANEL";
        }

        private void SetPanelToggleAlignment()
        {
            if (_panelToggleButton == null)
                return;

            switch (PanelPosition)
            {
                case CFIPClean30PanelCorner.TopLeft:
                    _panelToggleButton.VerticalAlignment =
                        VerticalAlignment.Top;
                    _panelToggleButton.HorizontalAlignment =
                        HorizontalAlignment.Left;
                    break;

                case CFIPClean30PanelCorner.TopRight:
                    _panelToggleButton.VerticalAlignment =
                        VerticalAlignment.Top;
                    _panelToggleButton.HorizontalAlignment =
                        HorizontalAlignment.Right;
                    break;

                case CFIPClean30PanelCorner.BottomRight:
                    _panelToggleButton.VerticalAlignment =
                        VerticalAlignment.Bottom;
                    _panelToggleButton.HorizontalAlignment =
                        HorizontalAlignment.Right;
                    break;

                default:
                    _panelToggleButton.VerticalAlignment =
                        VerticalAlignment.Bottom;
                    _panelToggleButton.HorizontalAlignment =
                        HorizontalAlignment.Left;
                    break;
            }
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
                _panelText == null)
                return;

            if (_panelToggleButton == null)
                CreatePanelToggleButton();

            _panel.IsVisible =
                !_panelHidden;

            if (_panelToggleButton != null)
                _panelToggleButton.Text =
                    _panelHidden
                        ? "SHOW PANEL"
                        : "HIDE PANEL";

            SetPanelToggleAlignment();

            _panelText.Text =
                BuildPanelText();

            _panelText.FontSize =
                Math.Max(
                    8,
                    PanelFontSize);

            _panelText.FontFamily =
                string.IsNullOrWhiteSpace(
                    PanelFontFamily)
                    ? "Arial"
                    : PanelFontFamily;

            _panelText.FontWeight =
                PanelBold
                    ? FontWeight.Bold
                    : FontWeight.Normal;

            _panelText.ForegroundColor =
                PanelTextColor;

            _panelText.LineHeight =
                Math.Max(
                    13,
                    PanelFontSize + 2);

            _panel.Width =
                Math.Max(
                    220,
                    PanelWidth);

            _panel.Padding =
                Math.Max(
                    0,
                    PanelPadding);

            _panel.Margin = 8;

            _panel.BackgroundColor =
                Color.FromArgb(
                    Math.Max(
                        0,
                        Math.Min(
                            255,
                            PanelBackgroundAlpha)),
                    PanelBackground);

            _panel.BorderColor =
                Color.FromArgb(
                    Math.Max(
                        0,
                        Math.Min(
                            255,
                            PanelBorderAlpha)),
                    PanelBorder);

            _panel.BorderThickness =
                Math.Max(
                    0,
                    PanelBorderThickness);

            _panel.CornerRadius =
                Math.Max(
                    0,
                    PanelCornerRadius);

            SetPanelAlignment();

            bool buttons =
                EnableAutoTrading;

            _buttonStack.IsVisible =
                buttons;

            _closeButton.IsVisible =
                buttons;

            _cancelButton.IsVisible =
                buttons;

            _closeButton.Width =
                Math.Max(
                    100,
                    ActionButtonWidth);

            _cancelButton.Width =
                Math.Max(
                    100,
                    ActionButtonWidth);

            _closeButton.Height =
                Math.Max(
                    20,
                    ActionButtonHeight);

            _cancelButton.Height =
                Math.Max(
                    20,
                    ActionButtonHeight);

            _closeButton.Text =
                "CLOSE ALL POSITIONS";

            _cancelButton.Text =
                "CANCEL ALL ORDERS";

            _closeButton.ForegroundColor =
                PanelTextColor;

            _cancelButton.ForegroundColor =
                PanelTextColor;

            _closeButton.FontSize =
                Math.Max(
                    8,
                    PanelFontSize - 1);

            _cancelButton.FontSize =
                Math.Max(
                    8,
                    PanelFontSize - 1);

            _closeButton.FontWeight =
                PanelBold
                    ? FontWeight.Bold
                    : FontWeight.Normal;

            _cancelButton.FontWeight =
                PanelBold
                    ? FontWeight.Bold
                    : FontWeight.Normal;

            _closeButton.BackgroundColor =
                Color.FromArgb(
                    Math.Max(
                        0,
                        Math.Min(
                            255,
                            PanelBackgroundAlpha)),
                    PanelBackground);

            _cancelButton.BackgroundColor =
                Color.FromArgb(
                    Math.Max(
                        0,
                        Math.Min(
                            255,
                            PanelBackgroundAlpha)),
                    PanelBackground);

            _closeButton.BorderColor =
                Color.FromArgb(
                    Math.Max(
                        0,
                        Math.Min(
                            255,
                            PanelBorderAlpha)),
                    PanelBorder);

            _cancelButton.BorderColor =
                Color.FromArgb(
                    Math.Max(
                        0,
                        Math.Min(
                            255,
                            PanelBorderAlpha)),
                    PanelBorder);

            _closeButton.BorderThickness =
                Math.Max(
                    0,
                    PanelBorderThickness);

            _cancelButton.BorderThickness =
                Math.Max(
                    0,
                    PanelBorderThickness);

            _closeButton.CornerRadius =
                Math.Max(
                    0,
                    PanelCornerRadius);

            _cancelButton.CornerRadius =
                Math.Max(
                    0,
                    PanelCornerRadius);
        }

        private string BuildPanelText()
        {
            List<string> lines =
                new List<string>();

            string state =
                _plan != null
                    ? (_plan.Direction == 1
                        ? "BUY ACTIVE"
                        : "SELL ACTIVE")
                    : _reaction != null &&
                      _reaction.EntryAllowed
                        ? (_reaction.Direction == 1
                            ? "BUY REACTION"
                            : "SELL REACTION")
                        : _decision != null &&
                          _decision.Direction != 0
                            ? (_decision.Direction == 1
                                ? "BUY WATCH"
                                : "SELL WATCH")
                            : "WAITING";

            lines.Add(
                "CFIP SMART CLEAN30 | " +
                state);

            lines.Add(
                SymbolName +
                " | " +
                Bars.TimeFrame);

            if (ShowEngineStatus)
            {
                lines.Add(
                    "STATUS  " +
                    _status);
            }

            if (_decision != null)
            {
                lines.Add(
                    "DECISION  " +
                    (_decision.Direction == 1
                        ? "BUY"
                        : _decision.Direction == -1
                            ? "SELL"
                            : "NEUTRAL") +
                    " | CONF " +
                    _decision.Confidence +
                    " | EDGE " +
                    _decision.Edge);

                lines.Add(
                    "SMART  Q" +
                    _decision.SmartQuality +
                    " | MTF " +
                    _decision.TimeframeAgreement +
                    " | EVID " +
                    _decision.IndependentEvidence +
                    " | STRUCT " +
                    _decision.StructuralConfirmations);

                lines.Add(
                    "REGIME  " +
                    _decision.Regime +
                    " | Q" +
                    _decision.RegimeQuality +
                    " | RETEST " +
                    _decision.RetestQuality);

                if (!string.IsNullOrWhiteSpace(
                        _decision.BlockReason))
                    lines.Add(
                        "BLOCK  " +
                        _decision.BlockReason);

                if (!string.IsNullOrWhiteSpace(
                        _decision.Reason))
                    lines.Add(
                        "WHY  " +
                        _decision.Reason);

                if (_prediction != null &&
                    _prediction.Direction != 0 &&
                    _prediction.Confidence >=
                    MinimumEarlyConfidence)
                {
                    lines.Add(
                        "EARLY  " +
                        (_prediction.Direction == 1
                            ? "BUY"
                            : "SELL") +
                        " | CONF " +
                        _prediction.Confidence);

                    lines.Add(
                        "PRED  TRG " +
                        Price(
                            _prediction.Trigger) +
                        " | TGT " +
                        Price(
                            _prediction.Target));
                }
            }

            if (_plan != null)
            {
                lines.Add("");
                lines.Add("TRADE PLAN");

                if (ShowEntry)
                    lines.Add(
                        "ENTRY  " +
                        Price(_plan.Entry));

                if (ShowSL)
                    lines.Add(
                        "SL     " +
                        Price(_plan.Stop) +
                        " | " +
                        _plan.StopSource +
                        " | Q" +
                        _plan.StopQuality);

                if (ShowTP1)
                    lines.Add(
                        "TP1    " +
                        Price(_plan.Tp1) +
                        " | RR " +
                        _plan.Tp1RR.ToString("F2") +
                        " | " +
                        _plan.Tp1Source +
                        " | Q" +
                        _plan.Tp1Quality);

                if (ShowTP2 &&
                    _plan.Tp2 > 0)
                    lines.Add(
                        "TP2    " +
                        Price(_plan.Tp2) +
                        " | RR " +
                        _plan.Tp2RR.ToString("F2") +
                        " | " +
                        _plan.Tp2Source +
                        " | Q" +
                        _plan.Tp2Quality);

                if (ShowTP3 &&
                    _plan.Tp3 > 0)
                    lines.Add(
                        "TP3    " +
                        Price(_plan.Tp3) +
                        " | RR " +
                        _plan.Tp3RR.ToString("F2") +
                        " | " +
                        _plan.Tp3Source +
                        " | Q" +
                        _plan.Tp3Quality);

                if (ShowTP4 &&
                    _plan.Tp4 > 0)
                    lines.Add(
                        "TP4    " +
                        Price(_plan.Tp4) +
                        " | RR " +
                        _plan.Tp4RR.ToString("F2") +
                        " | " +
                        _plan.Tp4Source +
                        " | Q" +
                        _plan.Tp4Quality);

                double liveRR =
                    _plan.Risk > 0
                        ? (_plan.Direction == 1
                            ? _lastMarket - _plan.Entry
                            : _plan.Entry - _lastMarket) /
                          _plan.Risk
                        : 0;

                lines.Add(
                    "LIVE  RR " +
                    liveRR.ToString("F2") +
                    " | HIT " +
                    _tp1Hit + "/" +
                    _tp2Hit + "/" +
                    _tp3Hit + "/" +
                    _tp4Hit);
            }
            else if (_reaction != null &&
                     _reaction.EntryAllowed)
            {
                lines.Add("");
                lines.Add(
                    "REACTION  " +
                    (_reaction.Direction == 1
                        ? "BUY"
                        : "SELL") +
                    " | Q" +
                    _reaction.Confidence +
                    " | EVID " +
                    _reaction.IndependentEvidence);
            }

            lines.Add("");
            lines.Add("MTF");

            if (UseVolumeExpansion ||
                UseMacdBias ||
                UseVwapBias ||
                UseHealthyVolatility)
            {
                lines.Add(
                    "CONFL  " +
                    ConfluenceText(
                        _m5Frame));
            }

            lines.Add(
                "M5  " +
                FrameText(_m5Frame));

            lines.Add(
                "M15 " +
                FrameText(_m15Frame));

            lines.Add(
                "M30 " +
                FrameText(_m30Frame));

            lines.Add(
                "H1  " +
                FrameText(_h1Frame));

            lines.Add(
                "H4  " +
                FrameText(_h4Frame));

            if (ShowOutcomeDiagnostics)
            {
                lines.Add(
                    "OUTCOME  W" +
                    _wins +
                    " | L" +
                    _losses +
                    " | CAL " +
                    CalibrationText());
            }

            lines.Add(
                "AUTO  " +
                (EnableAutoTrading
                    ? (EnableAggressiveAutoEntry
                        ? "ARMED + REACTION"
                        : "ARMED")
                    : "OFF"));

            if (AutoProtectBrokerPositions)
                lines.Add(
                    "BROKER  PROTECTION");

            return string.Join(
                Environment.NewLine,
                lines.ToArray());
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
                    frame.HealthyBull
                        ? "ATR+"
                        : frame.HealthyBear
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

        private void SetPanelAlignment()
        {
            VerticalAlignment vertical;
            HorizontalAlignment horizontal;

            switch (PanelPosition)
            {
                case CFIPClean30PanelCorner.TopLeft:
                    vertical = VerticalAlignment.Top;
                    horizontal = HorizontalAlignment.Left;
                    break;

                case CFIPClean30PanelCorner.TopRight:
                    vertical = VerticalAlignment.Top;
                    horizontal = HorizontalAlignment.Right;
                    break;

                case CFIPClean30PanelCorner.BottomRight:
                    vertical = VerticalAlignment.Bottom;
                    horizontal = HorizontalAlignment.Right;
                    break;

                default:
                    vertical = VerticalAlignment.Bottom;
                    horizontal = HorizontalAlignment.Left;
                    break;
            }

            _panel.VerticalAlignment =
                vertical;

            _panel.HorizontalAlignment =
                horizontal;
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

            if (_panelToggleButton != null)
            {
                try
                {
                    Chart.RemoveControl(
                        _panelToggleButton);
                }
                catch
                {
                }
            }

            _panelToggleButton = null;
            _panelHidden = false;
            _panel = null;
            _panelStack = null;
            _panelText = null;
            _buttonStack = null;
            _closeButton = null;
            _cancelButton = null;
        }

        // ============================================================
        // POPUP
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
                        "CFIP CLEAN30 popup creation failed: {0}",
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
                    9,
                    Math.Min(
                        18,
                        PanelFontSize));

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

            _popup.Margin = 8;

            _popup.BackgroundColor =
                Color.FromArgb(
                    Math.Max(
                        0,
                        Math.Min(
                            255,
                            PopupBackgroundAlpha)),
                    PopupBackgroundColor);

            _popup.BorderColor =
                PopupBorderColor;

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
                case CFIPClean30PanelCorner.TopLeft:
                    _popup.VerticalAlignment =
                        VerticalAlignment.Top;
                    _popup.HorizontalAlignment =
                        HorizontalAlignment.Left;
                    break;

                case CFIPClean30PanelCorner.BottomLeft:
                    _popup.VerticalAlignment =
                        VerticalAlignment.Bottom;
                    _popup.HorizontalAlignment =
                        HorizontalAlignment.Left;
                    break;

                case CFIPClean30PanelCorner.BottomRight:
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
        // ALERTS
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

            if (SuppressDuplicateAlerts)
            {
                if (key == _alertKey &&
                    (now - _alertUtc).TotalSeconds <
                    Math.Max(
                        1,
                        AlertCooldownSeconds))
                    return;
            }

            _alertKey = key;
            _alertUtc = now;

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
                            AlertSoundType);
                    }
                }
                catch (Exception ex)
                {
                    Print(
                        "CFIP CLEAN30 sound alert failed: {0}",
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
                        "CFIP SMART CLEAN30 " +
                        SymbolName,
                        message);
                }
                catch (Exception ex)
                {
                    Print(
                        "CFIP CLEAN30 email failed: {0}",
                        ex.Message);
                }
            }

            if (ShowPopupAlerts &&
                (!PopupCriticalOnly ||
                 critical))
            {
                ShowPopup(
                    message);
            }

            _status =
                message;
        }

        // ============================================================
        // AUTO TRADING
        // ============================================================

        private void TryAutoTrade(
            int closedM5)
        {
            if (!EnableAutoTrading ||
                !ConfirmedSignalsOnly ||
                _plan == null ||
                _decision == null ||
                _decision.Direction == 0)
                return;

            if (OneOrderPerSignal &&
                _lastAutoM5 ==
                closedM5)
                return;

            if (_decision.Confidence <
                MinimumAutoConfidence)
                return;

            if (_decision.SmartQuality <
                MinimumAutoSmartQuality)
                return;

            int levelQuality =
                Math.Min(
                    _plan.StopQuality,
                    _plan.Tp1Quality);

            if (levelQuality <
                MinimumAutoLevelQuality)
                return;

            if (ManagedPositionCount() >=
                Math.Max(
                    1,
                    MaximumOpenPositions))
                return;

            double entry =
                NormalizePrice(
                    _plan.Direction == 1
                        ? Symbol.Ask
                        : Symbol.Bid);

            double target =
                AutoTarget(
                    _plan,
                    AutoTpStage);

            if (!IsAutoPlanValid(
                    _plan.Direction,
                    entry,
                    _plan.Stop,
                    target))
                return;

            double atr =
                Atr(
                    _m5Bars,
                    closedM5);

            if (atr <= 0 ||
                Math.Abs(
                    entry -
                    _plan.Entry) >
                atr *
                MaximumEntryExtensionAtr)
                return;

            double stopPips =
                Math.Abs(
                    entry -
                    _plan.Stop) /
                Symbol.PipSize;

            double targetPips =
                Math.Abs(
                    target -
                    entry) /
                Symbol.PipSize;

            if (stopPips <= 0 ||
                targetPips <= 0)
                return;

            double volume =
                CalculateVolume(
                    stopPips);

            if (volume <
                Symbol.VolumeInUnitsMin)
                return;

            try
            {
                TradeType type =
                    _plan.Direction == 1
                        ? TradeType.Buy
                        : TradeType.Sell;

                TradeResult result =
                    ExecuteMarketOrder(
                        type,
                        SymbolName,
                        volume,
                        NormalizeLabel(),
                        stopPips,
                        targetPips);

                if (result == null ||
                    !result.IsSuccessful ||
                    result.Position == null)
                    return;

                _lastAutoM5 =
                    closedM5;

                if (AutoBrokerProtection)
                {
                    try
                    {
                        result.Position.ModifyStopLossPrice(
                            NormalizePrice(
                                _plan.Stop));

                        result.Position.ModifyTakeProfitPrice(
                            NormalizePrice(
                                target));
                    }
                    catch (Exception ex)
                    {
                        Print(
                            "CFIP CLEAN30 broker protection failed: {0}",
                            ex.Message);
                    }
                }

                SendUnifiedAlert(
                    "AUTO|" +
                    closedM5,
                    "CFIP CLEAN30 AUTO " +
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
                Print(
                    "CFIP CLEAN30 auto trade failed: {0}",
                    ex.Message);
            }
        }

        private double CalculateVolume(
            double stopPips)
        {
            try
            {
                double volume;

                if (SizingMode ==
                    CFIPClean30SizingMode.FixedLots)
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
                        Math.Max(
                            0.05,
                            RiskPercentEquity) /
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
                    "CFIP CLEAN30 volume calculation failed: {0}",
                    ex.Message);

                return 0;
            }
        }

        private int ManagedPositionCount()
        {
            int count = 0;
            string label =
                NormalizeLabel();

            foreach (Position position in Positions)
            {
                if (position == null)
                    continue;

                if (position.SymbolName ==
                    SymbolName &&
                    position.Label ==
                    label)
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
            CFIPClean30TargetStage stage)
        {
            if (stage ==
                    CFIPClean30TargetStage.TP4 &&
                plan.Tp4 > 0)
                return plan.Tp4;

            if (stage ==
                    CFIPClean30TargetStage.TP3 &&
                plan.Tp3 > 0)
                return plan.Tp3;

            if (stage ==
                    CFIPClean30TargetStage.TP2 &&
                plan.Tp2 > 0)
                return plan.Tp2;

            return plan.Tp1;
        }

        private string NormalizeLabel()
        {
            return
                string.IsNullOrWhiteSpace(
                    AutoTradeLabel)
                    ? "CFIP-SMART-CLEAN30"
                    : AutoTradeLabel.Trim();
        }

        private void CloseAllPositions()
        {
            foreach (Position position in Positions)
            {
                if (position == null ||
                    position.SymbolName !=
                    SymbolName)
                    continue;

                try
                {
                    ClosePosition(
                        position);
                }
                catch (Exception ex)
                {
                    Print(
                        "CFIP CLEAN30 close failed: {0}",
                        ex.Message);
                }
            }
        }

        private void CancelAllOrders()
        {
            foreach (PendingOrder order in PendingOrders)
            {
                if (order == null ||
                    order.SymbolName !=
                    SymbolName)
                    continue;

                try
                {
                    CancelPendingOrder(
                        order);
                }
                catch (Exception ex)
                {
                    Print(
                        "CFIP CLEAN30 cancel failed: {0}",
                        ex.Message);
                }
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
            if (!EnableConfidenceCalibration ||
                !EnableOutcomeTelemetry ||
                !_directionSamples.ContainsKey(
                    direction))
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
                _m15Frame == null)
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
                MinimumEarlyConfidence)
                return p;

            double atr =
                Atr(
                    _m5Bars,
                    closedM5);

            if (atr <= 0)
                return p;

            Zone zone =
                FindNearestOpposingZone(
                    _m5Bars,
                    closedM5,
                    p.Direction,
                    atr);

            if (zone != null)
            {
                p.ZoneLow = zone.Low;
                p.ZoneHigh = zone.High;
            }
            else
            {
                double price =
                    _m5Bars.ClosePrices[closedM5];

                p.ZoneLow =
                    price -
                    atr * 0.30;

                p.ZoneHigh =
                    price +
                    atr * 0.30;
            }

            p.Trigger =
                p.Direction == 1
                    ? p.ZoneHigh +
                      atr * EntryBufferAtr
                    : p.ZoneLow -
                      atr * EntryBufferAtr;

            p.Target =
                p.Direction == 1
                    ? FindEqualHigh(
                        _m5Bars,
                        closedM5,
                        _m5Bars.ClosePrices[closedM5],
                        atr)
                    : FindEqualLow(
                        _m5Bars,
                        closedM5,
                        _m5Bars.ClosePrices[closedM5],
                        atr);

            if (!IsFinitePositive(
                    p.Target))
            {
                p.Target =
                    p.Direction == 1
                        ? p.Trigger +
                          atr *
                          Math.Max(
                              1.5,
                              SmartTargetMinimumRR)
                        : p.Trigger -
                          atr *
                          Math.Max(
                              1.5,
                              SmartTargetMinimumRR);
            }

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
                MinimumEarlyConfidence)
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
                        predictionColor(),
                        1,
                        PlanLineStyle);

                zone.IsFilled =
                    true;

                zone.Color =
                    Color.FromArgb(
                        35,
                        predictionColor());

                zone.IsInteractive =
                    false;
            }

            if (prediction.Trigger > 0)
                DrawPredictionLine(
                    P + "PRED_TRIGGER",
                    prediction.Trigger);

            if (ShowPredictionTargets &&
                prediction.Target > 0)
                DrawPredictionLine(
                    P + "PRED_TARGET",
                    prediction.Target);
        }

        private Color predictionColor()
        {
            return
                _prediction != null &&
                _prediction.Direction == -1
                    ? SellArrowColor
                    : BuyArrowColor;
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
                            predictionColor(),
                            Math.Max(
                                1,
                                LevelLineThickness),
                            LineStyle.Dots);
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
                    predictionColor();
                line.Thickness =
                    Math.Max(
                        1,
                        LevelLineThickness);
                line.LineStyle =
                    LineStyle.Dots;
                line.ExtendToInfinity =
                    false;
                line.IsInteractive =
                    false;
            }
            catch (Exception ex)
            {
                Print(
                    "CFIP CLEAN30 prediction render failed: {0}",
                    ex.Message);
            }
        }

        private void RemovePredictionObjects()
        {
            Chart.RemoveObject(
                P + "PRED_ZONE");

            Chart.RemoveObject(
                P + "PRED_TRIGGER");

            Chart.RemoveObject(
                P + "PRED_TARGET");
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
            {
                int direction =
                    _m5Frame.StructureBull
                        ? 1
                        : -1;

                SendUnifiedAlert(
                    "BOS|" +
                    closedM5 +
                    "|" +
                    direction,
                    "CFIP CLEAN30 BOS | " +
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
                    "CFIP CLEAN30 MSS/CHOCH | " +
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
                    "CFIP CLEAN30 LIQUIDITY SWEEP | " +
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
                _m5Frame == null ||
                !EnableLiveStructuralReversal)
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

            if (!structural ||
                _m5Frame.Quality <
                LiveReversalStructuralScore ||
                _m5Frame.Evidence <
                LiveReversalMinimumEvidence)
                return false;

            if (RequireReversalForce &&
                !force)
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
                "CFIP CLEAN30 ACTIVE PLAN INVALIDATED | " +
                (opposite == 1
                    ? "BUY"
                    : "SELL") +
                " REVERSAL | Q " +
                _m5Frame.Quality,
                opposite,
                true);

            RegisterOutcome(
                _plan.Direction,
                false);

            _losses++;
            _plan = null;
            RemovePlanObjects();

            return true;
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
                    Math.Max(
                        0.05,
                        AggressiveRiskPercentEquity) /
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

        private double AggressiveTargetRR()
        {
            switch (AggressiveTpStage)
            {
                case CFIPClean30TargetStage.TP4:
                    return Math.Max(
                        3.0,
                        Tp4MinimumRR);

                case CFIPClean30TargetStage.TP3:
                    return Math.Max(
                        2.5,
                        Tp3MinimumRR);

                case CFIPClean30TargetStage.TP2:
                    return Math.Max(
                        2.0,
                        Tp2MinimumRR);

                default:
                    return Math.Max(
                        1.5,
                        Tp1MinimumRR);
            }
        }

        private void ProtectBrokerPositions(
            int closedM5)
        {
            if (!AutoProtectBrokerPositions ||
                _plan == null ||
                _lastBrokerModifyM5 ==
                closedM5)
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
                    SymbolName ||
                    position.Label !=
                    label)
                    continue;

                try
                {
                    if (IsValidStop(
                            _plan.Direction,
                            position.EntryPrice,
                            _plan.Stop))
                    {
                        position.ModifyStopLossPrice(
                            NormalizePrice(
                                _plan.Stop));
                    }

                    if (SyncBrokerTakeProfit)
                    {
                        double target =
                            AutoTarget(
                                _plan,
                                AutoTpStage);

                        if (IsValidTarget(
                                _plan.Direction,
                                position.EntryPrice,
                                target))
                        {
                            bool move =
                                true;

                            if (PreventBrokerTpBackwardMove &&
                                position.TakeProfit.HasValue)
                            {
                                double current =
                                    position.TakeProfit.Value;

                                move =
                                    _plan.Direction == 1
                                        ? target >= current
                                        : target <= current;
                            }

                            if (move)
                            {
                                position.ModifyTakeProfitPrice(
                                    NormalizePrice(
                                        target));
                            }
                        }
                    }

                    _lastBrokerModifyM5 =
                        closedM5;

                    _lastBrokerModifyUtc =
                        DateTime.UtcNow;

                    break;
                }
                catch (Exception ex)
                {
                    Print(
                        "CFIP CLEAN30 broker protection failed: {0}",
                        ex.Message);
                }
            }
        }

        private void TryAggressiveAutoTrade(
            int closedM5)
        {
            if (!EnableAutoTrading ||
                !EnableAggressiveAutoEntry ||
                _plan != null ||
                _reaction == null ||
                !_reaction.EntryAllowed ||
                _reaction.Direction == 0)
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
                return;

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
                return;

            double target =
                _reaction.Direction == 1
                    ? entry +
                      Math.Abs(
                          entry -
                          stop) *
                      AggressiveTargetRR()
                    : entry -
                      Math.Abs(
                          entry -
                          stop) *
                      AggressiveTargetRR();

            if (!IsAutoPlanValid(
                    _reaction.Direction,
                    entry,
                    stop,
                    target))
                return;

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

            double volume =
                CalculateAggressiveVolume(
                    stopPips);

            if (volume <
                Symbol.VolumeInUnitsMin)
                return;

            try
            {
                TradeType type =
                    _reaction.Direction == 1
                        ? TradeType.Buy
                        : TradeType.Sell;

                TradeResult result =
                    ExecuteMarketOrder(
                        type,
                        SymbolName,
                        volume,
                        NormalizeLabel(),
                        stopPips,
                        tpPips);

                if (result == null ||
                    !result.IsSuccessful ||
                    result.Position == null)
                    return;

                _lastAutoM5 =
                    closedM5;

                if (AutoBrokerProtection)
                {
                    try
                    {
                        result.Position.ModifyStopLossPrice(
                            NormalizePrice(
                                stop));

                        result.Position.ModifyTakeProfitPrice(
                            NormalizePrice(
                                target));
                    }
                    catch (Exception ex)
                    {
                        Print(
                            "CFIP CLEAN30 aggressive protection failed: {0}",
                            ex.Message);
                    }
                }

                SendUnifiedAlert(
                    "AUTO-REACTION|" +
                    closedM5,
                    "CFIP CLEAN30 AUTO REACTION " +
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
                    "CFIP CLEAN30 aggressive auto trade failed: {0}",
                    ex.Message);
            }
        }

        private void MonitorOutcome(
            int closedM5)
        {
            if (!EnableOutcomeTelemetry ||
                _plan == null ||
                OutcomeMaximumM5Bars <= 0)
                return;

            if (closedM5 -
                _plan.CreatedM5 <
                OutcomeMaximumM5Bars)
                return;

            bool win =
                _tp1Hit > 0 ||
                _tp2Hit > 0 ||
                _tp3Hit > 0 ||
                _tp4Hit > 0;

            RegisterOutcome(
                _plan.Direction,
                win);

            if (win)
                _wins++;
            else
                _losses++;

            _plan = null;
            RemovePlanObjects();
        }

        // ============================================================
        // HISTORICAL
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
        // INDEX / MATH / CLEANUP
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
        }

        // ============================================================
        // REQUIRED PRESENTATION / STATE HELPERS
        // ============================================================

        private void ClearPlanVisualState()
        {
            RemovePlanObjects();
        }
    }
}
}
