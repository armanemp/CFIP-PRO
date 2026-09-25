
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
        public double alternateSlAtr { get; set; }

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
            if (!IsLastBar || Bars == null)
                return;

            if (!HasEnoughData())
            {
                _status = "BUILDING DATA";
                RenderPanel();
                return;
            }

            RemoveExpiredPopup();

            int closedM5 = _m5Bars.Count - 2;
            if (closedM5 < 30)
            {
                _status = "WAITING FOR CLOSED M5";
                RenderPanel();
                return;
            }

            DateTime reference = _m5Bars.OpenTimes[_m5Bars.Count - 1];

            int m15Index = ClosedIndex(_m15Bars, reference);
            int m30Index = ClosedIndex(_m30Bars, reference);
            int h1Index = ClosedIndex(_h1Bars, reference);
            int h4Index = ClosedIndex(_h4Bars, reference);
            int d1Index = ClosedIndex(_d1Bars, reference);
            int w1Index = ClosedIndex(_w1Bars, reference);

            if (m15Index < 30 || m30Index < 30 || h1Index < 30 || h4Index < 30)
            {
                _status = "WAITING FOR MTF DATA";
                RenderPanel();
                return;
            }

            int m1Index = ClosedIndex(_m1Bars, reference);

            _m1Frame =
                m1Index >= 30
                    ? AnalyzeFrame(_m1Bars, m1Index)
                    : null;

            _m5Frame = AnalyzeFrame(_m5Bars, closedM5);
            _m15Frame = AnalyzeFrame(_m15Bars, m15Index);
            _m30Frame = AnalyzeFrame(_m30Bars, m30Index);
            _h1Frame = AnalyzeFrame(_h1Bars, h1Index);
            _h4Frame = AnalyzeFrame(_h4Bars, h4Index);
            _d1Frame = d1Index >= 10 ? AnalyzeFrame(_d1Bars, d1Index) : null;
            _w1Frame = w1Index >= 10 ? AnalyzeFrame(_w1Bars, w1Index) : null;

            EvaluateActivePlan(closedM5);

            _decision = BuildDecision(index, closedM5, reference);
            _reaction = BuildReaction();

            if (_plan == null &&
                _decision != null &&
                _decision.EntryAllowed &&
                ShouldCreatePlan(closedM5))
            {
                Plan plan = BuildPlan(closedM5, _decision.Direction);
                if (plan != null)
                    ActivatePlan(plan);
            }

            if (_plan != null)
            {
                RenderPlan();
            }
            else
            {
                RenderWatchAndReaction(index, closedM5);
            }

            TryAutoTrade(closedM5);
            RenderPanel();

            if (ShowHistoricalSignals)
                RenderHistoricalSignals();
            else
                RemoveHistoricalObjects();
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

            double total =
                Math.Max(
                    1,
                    buy + sell);

            int buyShare =
                ClampInt(
                    (int)Math.Round(
                        100.0 * buy / total),
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
                if (d.Direction == 1 &&
                    (_m5Frame.Direction != 1 ||
                     _m15Frame.Direction != 1))
                {
                    reason = "CORE ALIGNMENT";
                    return false;
                }

                if (d.Direction == -1 &&
                    (_m5Frame.Direction != -1 ||
                     _m15Frame.Direction != -1))
                {
                    reason = "CORE ALIGNMENT";
                    return false;
                }
            }
            
            if (UseM5Confirmation &&
                ((d.Direction == 1 && _m5Frame.Direction != 1) ||
                 (d.Direction == -1 && _m5Frame.Direction != -1)))
            {
                reason = "M5 CONFIRMATION";
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

            if (NewsBlocked(
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

            if (buyQuality < 74 &&
                sellQuality < 74)
                return new Decision();

            Decision d = new Decision();

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

            d.SmartQuality = d.Confidence;
            d.EntryAllowed =
                d.Confidence >= 74 &&
                d.IndependentEvidence >= 3;

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
                closeLocation >= 0.65;

            bool displacement =
                body >= atr * 0.60;

            bool breakMicro =
                direction == 1
                    ? bars.ClosePrices[index] >
                      Highest(
                          bars,
                          Math.Max(
                              0,
                              index - 5),
                          index - 1)
                    : bars.ClosePrices[index] <
                      Lowest(
                          bars,
                          Math.Max(
                              0,
                              index - 5),
                          index - 1);

            bool rsiSupport =
                direction == 1
                    ? Rsi(bars, index) >= 52
                    : Rsi(bars, index) <= 48;

            bool emaSupport =
                direction == 1
                    ? Ema(bars, index, true) >
                      Ema(bars, index, false)
                    : Ema(bars, index, true) <
                      Ema(bars, index, false);

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
                    ? BullTriggerScore(bars, index)
                    : BearTriggerScore(bars, index);

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

            if (trigger >= 5 &&
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
                          alternateSlAtr
                        : entry +
                          atr *
                          alternateSlAtr;

                stopSource = "ATR";
                stopQuality = 50;
            }

            double risk =
                Math.Abs(
                    entry -
                    stop);

            if (risk < atr * MinimumSlAtr ||
                risk > atr * MaximumSlAtr)
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

            if (RejectTargetObstacle &&
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

            if (_decision.Regime == "EXPANSION")
                return Math.Max(
                    2.10,
                    Tp1MinimumRR);

            if (_decision.Regime == "RANGE")
                return Math.Max(
                    1.75,
                    Tp1MinimumRR - 0.15);

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
                    TargetClearanceAtr);

            best =
                direction == 1
                    ? best - buffer
                    : best + buffer;

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
                SessionWeight);
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

                    if (RejectTargetObstacle &&
                        HasTargetObstacle(
                            _m5Bars,
                            _m5Bars.Count - 2,
                            direction,
                            entry,
                            candidate.Price,
                            atr))
                        continue;

                    if (best == null ||
                        candidate.Score > best.Score)
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
                _losses++;

                if (AlertOnLevelHit)
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

                if (AlertOnLevelHit)
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

                if (AlertOnLevelHit)
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

                if (AlertOnLevelHit)
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
                _wins++;

                if (AlertOnLevelHit)
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

            if (EnableStructuralSlRepricing &&
                peakRR >= SlRepriceStartRR)
            {
                double atr =
                    Atr(
                        _m5Bars,
                        closedM5);

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
                            SlRepriceBreathingAtr);

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
                    SlRepriceStepAtr);

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
                        atr * 0.25)
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
                        atr * 0.25)
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
                    ZoneProximityAtr;

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

            if (zone != null)
            {
                double price =
                    bars.ClosePrices[index];

                double tolerance =
                    atr *
                    ZoneProximityAtr;

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
                MinimumTriggerBodyAtr)
                q += 10;

            if ((direction == 1 &&
                 bars.ClosePrices[index] >
                 bars.OpenPrices[index]) ||
                (direction == -1 &&
                 bars.ClosePrices[index] <
                 bars.OpenPrices[index]))
                q += 5;

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
                    index - 8);

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
                    TargetClearanceAtr)
                    return true;

                if (direction == -1 &&
                    bars.LowPrices[i] <
                    entry &&
                    bars.LowPrices[i] >
                    target +
                    atr *
                    TargetClearanceAtr)
                    return true;
            }

            return false;
        }

        private bool ShouldCreatePlan(
            int closedM5)
        {
            if (_plan != null ||
                _decision == null ||
                !_decision.EntryAllowed)
                return false;

            return
                _lastSignalM5 !=
                closedM5;
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

        private void DrawPlanLine(
            string name,
            double price,
            Color color,
            bool visible)
        {
            if (!visible ||
                !IsFinitePositive(price))
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
                if (FullWidthLevelLines)
                {
                    ChartHorizontalLine line =
                        Chart.FindObject(name)
                        as ChartHorizontalLine;

                    if (line == null)
                    {
                        ChartObject existing =
                            Chart.FindObject(name);

                        if (existing != null)
                            Chart.RemoveObject(name);

                        line =
                            Chart.DrawHorizontalLine(
                                name,
                                normalized,
                                color,
                                Math.Max(
                                    1,
                                    LevelLineThickness),
                                PlanLineStyle);
                    }

                    if (line == null)
                        return;

                    line.Y =
                        normalized;

                    line.Color =
                        color;

                    line.Thickness =
                        Math.Max(
                            1,
                            LevelLineThickness);

                    line.LineStyle =
                        PlanLineStyle;

                    return;
                }

                int anchor =
                    _plan != null
                        ? MapM5ToChart(
                            _plan.CreatedM5,
                            Bars.Count - 1)
                        : Bars.Count - 1;

                int left =
                    Math.Max(
                        0,
                        anchor - 14);

                int right =
                    Math.Min(
                        Bars.Count - 1,
                        anchor + 10);

                if (right <= left)
                    return;

                ChartTrendLine trend =
                    Chart.FindObject(name)
                    as ChartTrendLine;

                if (trend == null)
                {
                    ChartObject existing =
                        Chart.FindObject(name);

                    if (existing != null)
                        Chart.RemoveObject(name);

                    trend =
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

                if (trend == null)
                    return;

                trend.Time1 =
                    Bars.OpenTimes[left];

                trend.Y1 =
                    normalized;

                trend.Time2 =
                    Bars.OpenTimes[right];

                trend.Y2 =
                    normalized;

                trend.Color =
                    color;

                trend.Thickness =
                    Math.Max(
                        1,
                        LevelLineThickness);

                trend.LineStyle =
                    PlanLineStyle;

                trend.ExtendToInfinity =
                    false;

                trend.IsInteractive =
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
                    Symbol.PipSize * 2,
                    atr * 0.15);

            if (ShowSignalArrow)
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

            if (_reaction != null &&
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
                        TextWrapping = TextWrapping.NoWrap,
                        TextTrimming = TextTrimming.CharacterEllipsis,
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

            _closeButton.Width = 150;
            _cancelButton.Width = 150;
            _closeButton.Height = 25;
            _cancelButton.Height = 25;

            _closeButton.Text =
                "CLOSE ALL POSITIONS";

            _cancelButton.Text =
                "CANCEL ALL ORDERS";

            _closeButton.ForegroundColor =
                Color.White;

            _cancelButton.ForegroundColor =
                Color.White;
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

            lines.Add(
                "STATUS  " +
                _status);

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

            lines.Add(
                "OUTCOME  W" +
                _wins +
                " | L" +
                _losses);

            lines.Add(
                "AUTO  " +
                (EnableAutoTrading
                    ? "ARMED"
                    : "OFF"));

            return string.Join(
                Environment.NewLine,
                lines.ToArray());
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

                    _popup =
                        new Border
                        {
                            Child = _popupText,
                            IsHitTestVisible = false
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
                PanelTextColor;

            _popup.Width =
                Math.Max(
                    240,
                    PanelWidth);

            _popup.Padding =
                Math.Max(
                    6,
                    PanelPadding);

            _popup.Margin = 8;

            _popup.BackgroundColor =
                Color.FromArgb(
                    235,
                    PanelBackground);

            _popup.BorderColor =
                PanelBorder;

            _popup.BorderThickness =
                1;

            _popup.CornerRadius =
                Math.Max(
                    0,
                    PanelCornerRadius);

            _popup.VerticalAlignment =
                VerticalAlignment.Top;

            _popup.HorizontalAlignment =
                HorizontalAlignment.Right;

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
                    Notifications.PlaySound();
                }
                catch
                {
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
