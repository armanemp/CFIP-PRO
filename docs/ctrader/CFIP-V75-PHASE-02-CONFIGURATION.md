# CFIP-PRO cTrader v75 — Phase 2 Parameter Architecture & Inventory

**Date:** 2026-09-27  
**Baseline:** `integrations/ctrader/calude-edit-v73.cs`  
**Implementation:** `integrations/ctrader/calude-edit-v75.cs`  
**Parameter count:** 512  

## Phase objective

The v73 parameter surface is carried forward without silent capability loss, while establishing one configuration snapshot and an explicit disposition for every parameter. Similar names are not merged unless their semantic ownership is actually identical.

## Disposition rules

- **ACTIVE** — remains an active configuration input.
- **DEPRECATED** — retained temporarily for preset compatibility, but a single canonical parameter becomes the future authority.
- **REMOVED** — excluded from clean-architecture behavior. The property may remain temporarily only as a compatibility carrier and must not control runtime behavior.

## Critical trading principles preserved

The configuration layer does not weaken the decision or execution pipeline. Signal generation, MTF confluence, structure, FVG/OB/liquidity, Entry/Trigger, smart SL, target ladder, automatic market trading, automatic pending orders, broker protection, reversal, partial TP, live management, outcome and calibration remain planned capabilities. Phase 2 changes configuration ownership, not trading semantics.

## Canonical group mapping

| Canonical section | Source groups |
|---|---|
| Decision | `01 · Decision` |
| MTF | `02 · MTF` |
| Structure | `03 · Structure` |
| Zones | `04 · Zones` |
| Liquidity | `05 · Liquidity` |
| Indicators | `06 · Indicators` |
| Entry | `07 · Entry Precision`, `23 · Structural Execution` |
| Smart Weights | `08 · Smart Weights` |
| Risk / Targets | `09 · Risk & Targets` |
| Live Management | `10 · Live Management` |
| Filters | `11 · Filters`, selected `15 · CONTROL` gates |
| Alerts | `12 · ALERTS — CORE`, `12 · ALERTS — ADVANCED` |
| Automation | `13 · AUTO TRADING` |
| Smart Execution | `24 · SMART EXECUTION` |
| Display | `14 · DISPLAY — CORE/ADVANCED/PANEL` |
| Intelligence | `15 · INTELLIGENCE — EARLY`, `16 · Accuracy`, `17 · Smart Engine`, `21 · Complete Intelligence` |
| Confluence | `20 · Confluence Extensions` |
| Safety / Precision | `22 · Safety & Precision` |

## Parameter inventory

| # | Parameter | Type | Source group | Canonical section | Disposition | Canonical key / rationale |
|---:|---|---|---|---|---|---|
| 1 | `MinimumConfidence` | `int` | 01 · Decision | Decision | **ACTIVE** | `MinimumConfidence` — Unique semantic input retained. |
| 2 | `MinimumEdge` | `int` | 01 · Decision | Decision | **ACTIVE** | `MinimumEdge` — Unique semantic input retained. |
| 3 | `MinimumSmartQuality` | `int` | 01 · Decision | Decision | **ACTIVE** | `MinimumSmartQuality` — Unique semantic input retained. |
| 4 | `MinimumStructuralConfirmations` | `int` | 01 · Decision | Decision | **ACTIVE** | `MinimumStructuralConfirmations` — Unique semantic input retained. |
| 5 | `MinimumIndependentEvidence` | `int` | 01 · Decision | Decision | **ACTIVE** | `MinimumIndependentEvidence` — Unique semantic input retained. |
| 6 | `MinimumTimeframeAgreement` | `int` | 01 · Decision | Decision | **ACTIVE** | `MinimumTimeframeAgreement` — Unique semantic input retained. |
| 7 | `RequireHigherTfAgreement` | `bool` | 01 · Decision | Decision | **ACTIVE** | `RequireHigherTfAgreement` — Unique semantic input retained. |
| 8 | `RequireCoreAgreement` | `bool` | 01 · Decision | Decision | **ACTIVE** | `RequireCoreAgreement` — Unique semantic input retained. |
| 9 | `RequireStructuralConfirmation` | `bool` | 01 · Decision | Decision | **ACTIVE** | `RequireStructuralConfirmation` — Unique semantic input retained. |
| 10 | `UseAdvancedConfluence` | `bool` | 01 · Decision | Decision | **ACTIVE** | `UseAdvancedConfluence` — Unique semantic input retained. |
| 11 | `AllowStrongTriggerOverride` | `bool` | 01 · Decision | Decision | **ACTIVE** | `AllowStrongTriggerOverride` — Unique semantic input retained. |
| 12 | `UseM1Trigger` | `bool` | 02 · MTF | Mtf | **ACTIVE** | `UseM1Trigger` — Unique semantic input retained. |
| 13 | `UseM5Confirmation` | `bool` | 02 · MTF | Mtf | **ACTIVE** | `UseM5Confirmation` — Unique semantic input retained. |
| 14 | `M5Weight` | `int` | 02 · MTF | Mtf | **ACTIVE** | `M5Weight` — Unique semantic input retained. |
| 15 | `M15Weight` | `int` | 02 · MTF | Mtf | **ACTIVE** | `M15Weight` — Unique semantic input retained. |
| 16 | `M30Weight` | `int` | 02 · MTF | Mtf | **ACTIVE** | `M30Weight` — Unique semantic input retained. |
| 17 | `H1Weight` | `int` | 02 · MTF | Mtf | **ACTIVE** | `H1Weight` — Unique semantic input retained. |
| 18 | `H4Weight` | `int` | 02 · MTF | Mtf | **ACTIVE** | `H4Weight` — Unique semantic input retained. |
| 19 | `D1Weight` | `int` | 02 · MTF | Mtf | **ACTIVE** | `D1Weight` — Unique semantic input retained. |
| 20 | `W1Weight` | `int` | 02 · MTF | Mtf | **ACTIVE** | `W1Weight` — Unique semantic input retained. |
| 21 | `StructureLookback` | `int` | 03 · Structure | Structure | **ACTIVE** | `StructureLookback` — Unique semantic input retained. |
| 22 | `SwingStrength` | `int` | 03 · Structure | Structure | **ACTIVE** | `SwingStrength` — Unique semantic input retained. |
| 23 | `StructureBreakAtr` | `double` | 03 · Structure | Structure | **ACTIVE** | `StructureBreakAtr` — Unique semantic input retained. |
| 24 | `UseInternalStructure` | `bool` | 03 · Structure | Structure | **ACTIVE** | `UseInternalStructure` — Unique semantic input retained. |
| 25 | `UseMssChoch` | `bool` | 03 · Structure | Structure | **ACTIVE** | `UseMssChoch` — Unique semantic input retained. |
| 26 | `UseFvg` | `bool` | 04 · Zones | Zones | **ACTIVE** | `UseFvg` — Unique semantic input retained. |
| 27 | `FvgLookback` | `int` | 04 · Zones | Zones | **ACTIVE** | `FvgLookback` — Unique semantic input retained. |
| 28 | `MinimumFvgAtr` | `double` | 04 · Zones | Zones | **ACTIVE** | `MinimumFvgAtr` — Unique semantic input retained. |
| 29 | `UseOrderBlock` | `bool` | 04 · Zones | Zones | **ACTIVE** | `UseOrderBlock` — Unique semantic input retained. |
| 30 | `ObLookback` | `int` | 04 · Zones | Zones | **ACTIVE** | `ObLookback` — Unique semantic input retained. |
| 31 | `ObDisplacementAtr` | `double` | 04 · Zones | Zones | **ACTIVE** | `ObDisplacementAtr` — Unique semantic input retained. |
| 32 | `ObUseBodyForZone` | `bool` | 04 · Zones | Zones | **ACTIVE** | `ObUseBodyForZone` — Unique semantic input retained. |
| 33 | `ObBreakByWicks` | `bool` | 04 · Zones | Zones | **ACTIVE** | `ObBreakByWicks` — Unique semantic input retained. |
| 34 | `ObStructureLookback` | `int` | 04 · Zones | Zones | **ACTIVE** | `ObStructureLookback` — Unique semantic input retained. |
| 35 | `ObImpulseBars` | `int` | 04 · Zones | Zones | **ACTIVE** | `ObImpulseBars` — Unique semantic input retained. |
| 36 | `ObMinimumQuality` | `int` | 04 · Zones | Zones | **ACTIVE** | `ObMinimumQuality` — Unique semantic input retained. |
| 37 | `RequireFvgRetest` | `bool` | 04 · Zones | Zones | **ACTIVE** | `RequireFvgRetest` — Unique semantic input retained. |
| 38 | `UseTwoBarImbalanceFvg` | `bool` | 04 · Zones | Zones | **ACTIVE** | `UseTwoBarImbalanceFvg` — Unique semantic input retained. |
| 39 | `RequireObDisplacement` | `bool` | 04 · Zones | Zones | **ACTIVE** | `RequireObDisplacement` — Unique semantic input retained. |
| 40 | `ZoneProximityAtr` | `double` | 04 · Zones | Zones | **ACTIVE** | `ZoneProximityAtr` — Unique semantic input retained. |
| 41 | `MaximumZoneAgeBars` | `int` | 04 · Zones | Zones | **ACTIVE** | `MaximumZoneAgeBars` — Unique semantic input retained. |
| 42 | `LiquidityLookback` | `int` | 05 · Liquidity | Liquidity | **ACTIVE** | `LiquidityLookback` — Unique semantic input retained. |
| 43 | `UseEqualHighLow` | `bool` | 05 · Liquidity | Liquidity | **ACTIVE** | `UseEqualHighLow` — Unique semantic input retained. |
| 44 | `EqualLevelToleranceAtr` | `double` | 05 · Liquidity | Liquidity | **ACTIVE** | `EqualLevelToleranceAtr` — Unique semantic input retained. |
| 45 | `UseLiquiditySweep` | `bool` | 05 · Liquidity | Liquidity | **ACTIVE** | `UseLiquiditySweep` — Unique semantic input retained. |
| 46 | `LiquiditySweepMinimumDepthAtr` | `double` | 05 · Liquidity | Liquidity | **ACTIVE** | `LiquiditySweepMinimumDepthAtr` — Unique semantic input retained. |
| 47 | `UseDisplacement` | `bool` | 05 · Liquidity | Liquidity | **ACTIVE** | `UseDisplacement` — Unique semantic input retained. |
| 48 | `DisplacementAtr` | `double` | 05 · Liquidity | Liquidity | **ACTIVE** | `DisplacementAtr` — Unique semantic input retained. |
| 49 | `UsePremiumDiscount` | `bool` | 05 · Liquidity | Liquidity | **ACTIVE** | `UsePremiumDiscount` — Unique semantic input retained. |
| 50 | `UseDailyWeeklyLiquidity` | `bool` | 05 · Liquidity | Liquidity | **ACTIVE** | `UseDailyWeeklyLiquidity` — Unique semantic input retained. |
| 51 | `UseDailyPivots` | `bool` | 05 · Liquidity | Liquidity | **ACTIVE** | `UseDailyPivots` — Unique semantic input retained. |
| 52 | `DailyPivotWeight` | `int` | 05 · Liquidity | Liquidity | **ACTIVE** | `DailyPivotWeight` — Unique semantic input retained. |
| 53 | `FastEma` | `int` | 06 · Indicators | Indicators | **ACTIVE** | `FastEma` — Unique semantic input retained. |
| 54 | `SlowEma` | `int` | 06 · Indicators | Indicators | **ACTIVE** | `SlowEma` — Unique semantic input retained. |
| 55 | `RsiPeriod` | `int` | 06 · Indicators | Indicators | **ACTIVE** | `RsiPeriod` — Unique semantic input retained. |
| 56 | `AdxPeriod` | `int` | 06 · Indicators | Indicators | **ACTIVE** | `AdxPeriod` — Unique semantic input retained. |
| 57 | `AdxMinimum` | `double` | 06 · Indicators | Indicators | **ACTIVE** | `AdxMinimum` — Unique semantic input retained. |
| 58 | `AtrPeriod` | `int` | 06 · Indicators | Indicators | **ACTIVE** | `AtrPeriod` — Unique semantic input retained. |
| 59 | `UseEmaSlope` | `bool` | 06 · Indicators | Indicators | **ACTIVE** | `UseEmaSlope` — Unique semantic input retained. |
| 60 | `AvoidRsiExhaustion` | `bool` | 06 · Indicators | Indicators | **ACTIVE** | `AvoidRsiExhaustion` — Unique semantic input retained. |
| 61 | `MinimumTriggerBodyAtr` | `double` | 07 · Entry Precision | Entry | **ACTIVE** | `MinimumTriggerBodyAtr` — Unique semantic input retained. |
| 62 | `MinimumCloseLocation` | `double` | 07 · Entry Precision | Entry | **ACTIVE** | `MinimumCloseLocation` — Unique semantic input retained. |
| 63 | `MaximumTriggerRangeAtr` | `double` | 07 · Entry Precision | Entry | **ACTIVE** | `MaximumTriggerRangeAtr` — Unique semantic input retained. |
| 64 | `RequireStableM5Direction` | `bool` | 07 · Entry Precision | Entry | **ACTIVE** | `RequireStableM5Direction` — Unique semantic input retained. |
| 65 | `StableM5Bars` | `int` | 07 · Entry Precision | Entry | **ACTIVE** | `StableM5Bars` — Unique semantic input retained. |
| 66 | `RequireStableM15Direction` | `bool` | 07 · Entry Precision | Entry | **ACTIVE** | `RequireStableM15Direction` — Unique semantic input retained. |
| 67 | `StableM15Bars` | `int` | 07 · Entry Precision | Entry | **ACTIVE** | `StableM15Bars` — Unique semantic input retained. |
| 68 | `EntryBufferAtr` | `double` | 07 · Entry Precision | Entry | **ACTIVE** | `EntryBufferAtr` — Unique semantic input retained. |
| 69 | `MaximumEntryExtensionAtr` | `double` | 07 · Entry Precision | Entry | **ACTIVE** | `MaximumEntryExtensionAtr` — Unique semantic input retained. |
| 70 | `MinimumRetestQuality` | `int` | 07 · Entry Precision | Entry | **ACTIVE** | `MinimumRetestQuality` — Unique semantic input retained. |
| 71 | `RequireRetestQuality` | `bool` | 07 · Entry Precision | Entry | **ACTIVE** | `RequireRetestQuality` — Unique semantic input retained. |
| 72 | `SupplyDemandWeight` | `int` | 08 · Smart Weights | Smart Weights | **ACTIVE** | `SupplyDemandWeight` — Unique semantic input retained. |
| 73 | `FvgWeight` | `int` | 08 · Smart Weights | Smart Weights | **ACTIVE** | `FvgWeight` — Unique semantic input retained. |
| 74 | `OrderBlockWeight` | `int` | 08 · Smart Weights | Smart Weights | **ACTIVE** | `OrderBlockWeight` — Unique semantic input retained. |
| 75 | `LiquidityPoolWeight` | `int` | 08 · Smart Weights | Smart Weights | **ACTIVE** | `LiquidityPoolWeight` — Unique semantic input retained. |
| 76 | `EqualHighLowWeight` | `int` | 08 · Smart Weights | Smart Weights | **ACTIVE** | `EqualHighLowWeight` — Unique semantic input retained. |
| 77 | `SwingStructureWeight` | `int` | 08 · Smart Weights | Smart Weights | **ACTIVE** | `SwingStructureWeight` — Unique semantic input retained. |
| 78 | `MtfClusterWeight` | `int` | 08 · Smart Weights | Smart Weights | **ACTIVE** | `MtfClusterWeight` — Unique semantic input retained. |
| 79 | `PreviousDayWeight` | `int` | 08 · Smart Weights | Smart Weights | **ACTIVE** | `PreviousDayWeight` — Unique semantic input retained. |
| 80 | `PreviousWeekWeight` | `int` | 08 · Smart Weights | Smart Weights | **ACTIVE** | `PreviousWeekWeight` — Unique semantic input retained. |
| 81 | `SessionWeight` | `int` | 08 · Smart Weights | Smart Weights | **ACTIVE** | `SessionWeight` — Unique semantic input retained. |
| 82 | `HtfStructureWeight` | `int` | 08 · Smart Weights | Smart Weights | **ACTIVE** | `HtfStructureWeight` — Unique semantic input retained. |
| 83 | `MinimumSlAtr` | `double` | 09 · Risk & Targets | Risk / Targets | **ACTIVE** | `MinimumSlAtr` — Unique semantic input retained. |
| 84 | `MaximumSlAtr` | `double` | 09 · Risk & Targets | Risk / Targets | **ACTIVE** | `MaximumSlAtr` — Unique semantic input retained. |
| 85 | `FallbackSlAtr` | `double` | 09 · Risk & Targets | Risk / Targets | **ACTIVE** | `FallbackSlAtr` — Unique semantic input retained. |
| 86 | `Tp1MinimumRR` | `double` | 09 · Risk & Targets | Risk / Targets | **ACTIVE** | `Tp1MinimumRR` — Unique semantic input retained. |
| 87 | `Tp2MinimumRR` | `double` | 09 · Risk & Targets | Risk / Targets | **ACTIVE** | `Tp2MinimumRR` — Unique semantic input retained. |
| 88 | `Tp3MinimumRR` | `double` | 09 · Risk & Targets | Risk / Targets | **ACTIVE** | `Tp3MinimumRR` — Unique semantic input retained. |
| 89 | `Tp4MinimumRR` | `double` | 09 · Risk & Targets | Risk / Targets | **ACTIVE** | `Tp4MinimumRR` — Unique semantic input retained. |
| 90 | `MinimumTpSpacingAtr` | `double` | 09 · Risk & Targets | Risk / Targets | **ACTIVE** | `MinimumTpSpacingAtr` — Unique semantic input retained. |
| 91 | `TargetClearanceAtr` | `double` | 09 · Risk & Targets | Risk / Targets | **ACTIVE** | `TargetClearanceAtr` — Unique semantic input retained. |
| 92 | `RejectTargetObstacle` | `bool` | 09 · Risk & Targets | Risk / Targets | **ACTIVE** | `RejectTargetObstacle` — Unique semantic input retained. |
| 93 | `MaximumTargetExtensionAtr` | `double` | 09 · Risk & Targets | Risk / Targets | **ACTIVE** | `MaximumTargetExtensionAtr` — Unique semantic input retained. |
| 94 | `UseHtfStructureForStop` | `bool` | 09 · Risk & Targets | Risk / Targets | **ACTIVE** | `UseHtfStructureForStop` — Unique semantic input retained. |
| 95 | `RequireStructuralStop` | `bool` | 09 · Risk & Targets | Risk / Targets | **ACTIVE** | `RequireStructuralStop` — Unique semantic input retained. |
| 96 | `AdaptiveStructuralRR` | `bool` | 09 · Risk & Targets | Risk / Targets | **ACTIVE** | `AdaptiveStructuralRR` — Unique semantic input retained. |
| 97 | `EnableLiveExitManagement` | `bool` | 10 · Live Management | Live Management | **ACTIVE** | `EnableLiveExitManagement` — Unique semantic input retained. |
| 98 | `MoveSlToBreakEven` | `bool` | 10 · Live Management | Live Management | **ACTIVE** | `MoveSlToBreakEven` — Unique semantic input retained. |
| 99 | `BreakEvenTriggerRR` | `double` | 10 · Live Management | Live Management | **ACTIVE** | `BreakEvenTriggerRR` — Unique semantic input retained. |
| 100 | `BreakEvenBufferPips` | `double` | 10 · Live Management | Live Management | **ACTIVE** | `BreakEvenBufferPips` — Unique semantic input retained. |
| 101 | `UseSpreadAwareBreakEven` | `bool` | 10 · Live Management | Live Management | **ACTIVE** | `UseSpreadAwareBreakEven` — Unique semantic input retained. |
| 102 | `RiskFreeLockPips` | `double` | 10 · Live Management | Live Management | **ACTIVE** | `RiskFreeLockPips` — Unique semantic input retained. |
| 103 | `SmartTrailMomentumBonusAtr` | `double` | 10 · Live Management | Live Management | **ACTIVE** | `SmartTrailMomentumBonusAtr` — Unique semantic input retained. |
| 104 | `SmartTrailTightenAtRR` | `double` | 10 · Live Management | Live Management | **ACTIVE** | `SmartTrailTightenAtRR` — Unique semantic input retained. |
| 105 | `EnableStructuralSlRepricing` | `bool` | 10 · Live Management | Live Management | **ACTIVE** | `EnableStructuralSlRepricing` — Unique semantic input retained. |
| 106 | `SlRepriceStartRR` | `double` | 10 · Live Management | Live Management | **ACTIVE** | `SlRepriceStartRR` — Unique semantic input retained. |
| 107 | `SlRepriceBreathingAtr` | `double` | 10 · Live Management | Live Management | **ACTIVE** | `SlRepriceBreathingAtr` — Unique semantic input retained. |
| 108 | `SlRepriceStepAtr` | `double` | 10 · Live Management | Live Management | **ACTIVE** | `SlRepriceStepAtr` — Unique semantic input retained. |
| 109 | `UpdateUnhitTargets` | `bool` | 10 · Live Management | Live Management | **ACTIVE** | `UpdateUnhitTargets` — Unique semantic input retained. |
| 110 | `TargetUpdateTriggerRR` | `double` | 10 · Live Management | Live Management | **ACTIVE** | `TargetUpdateTriggerRR` — Unique semantic input retained. |
| 111 | `EnableProfitExhaustionProtection` | `bool` | 10 · Live Management | Live Management | **ACTIVE** | `EnableProfitExhaustionProtection` — Unique semantic input retained. |
| 112 | `ExhaustionMinimumPeakRR` | `double` | 10 · Live Management | Live Management | **ACTIVE** | `ExhaustionMinimumPeakRR` — Unique semantic input retained. |
| 113 | `ExhaustionRetracementPercent` | `double` | 10 · Live Management | Live Management | **ACTIVE** | `ExhaustionRetracementPercent` — Unique semantic input retained. |
| 114 | `ExhaustionPressureThreshold` | `int` | 10 · Live Management | Live Management | **ACTIVE** | `ExhaustionPressureThreshold` — Unique semantic input retained. |
| 115 | `ExhaustionMinimumOppositeEvidence` | `int` | 10 · Live Management | Live Management | **ACTIVE** | `ExhaustionMinimumOppositeEvidence` — Unique semantic input retained. |
| 116 | `UseSessionFilter` | `bool` | 11 · Filters | Filters | **ACTIVE** | `UseSessionFilter` — Unique semantic input retained. |
| 117 | `SessionStartUtc` | `int` | 11 · Filters | Filters | **ACTIVE** | `SessionStartUtc` — Unique semantic input retained. |
| 118 | `SessionEndUtc` | `int` | 11 · Filters | Filters | **ACTIVE** | `SessionEndUtc` — Unique semantic input retained. |
| 119 | `EnableEndOfDayAlert` | `bool` | 11 · Filters | Filters | **ACTIVE** | `EnableEndOfDayAlert` — Unique semantic input retained. |
| 120 | `EndOfDayAlertMinutesBefore` | `int` | 11 · Filters | Filters | **ACTIVE** | `EndOfDayAlertMinutesBefore` — Unique semantic input retained. |
| 121 | `EnableEndOfDayAutoClose` | `bool` | 11 · Filters | Filters | **ACTIVE** | `EnableEndOfDayAutoClose` — Unique semantic input retained. |
| 122 | `UseSpreadFilter` | `bool` | 11 · Filters | Filters | **ACTIVE** | `UseSpreadFilter` — Unique semantic input retained. |
| 123 | `MaximumSpreadAtr` | `double` | 11 · Filters | Filters | **ACTIVE** | `MaximumSpreadAtr` — Unique semantic input retained. |
| 124 | `CooldownM5Bars` | `int` | 11 · Filters | Filters | **ACTIVE** | `CooldownM5Bars` — Unique semantic input retained. |
| 125 | `AvoidFridayLateEntry` | `bool` | 11 · Filters | Filters | **ACTIVE** | `AvoidFridayLateEntry` — Unique semantic input retained. |
| 126 | `FridayCutoffUtc` | `int` | 11 · Filters | Filters | **ACTIVE** | `FridayCutoffUtc` — Unique semantic input retained. |
| 127 | `UseVolatilityGuard` | `bool` | 11 · Filters | Filters | **ACTIVE** | `UseVolatilityGuard` — Unique semantic input retained. |
| 128 | `EventShockRangeAtr` | `double` | 11 · Filters | Filters | **ACTIVE** | `EventShockRangeAtr` — Unique semantic input retained. |
| 129 | `EventShockAtrExpansion` | `double` | 11 · Filters | Filters | **ACTIVE** | `EventShockAtrExpansion` — Unique semantic input retained. |
| 130 | `NewsBlackoutUtc` | `string` | 11 · Filters | Filters | **ACTIVE** | `NewsBlackoutUtc` — Unique semantic input retained. |
| 131 | `EnableSoundAlerts` | `bool` | 12 · ALERTS — CORE | Alerts | **ACTIVE** | `EnableSoundAlerts` — Unique semantic input retained. |
| 132 | `ShowPopupAlerts` | `bool` | 12 · ALERTS — CORE | Alerts | **ACTIVE** | `ShowPopupAlerts` — Unique semantic input retained. |
| 133 | `PopupCriticalOnly` | `bool` | 12 · ALERTS — CORE | Alerts | **ACTIVE** | `PopupCriticalOnly` — Unique semantic input retained. |
| 134 | `PopupDurationSeconds` | `int` | 12 · ALERTS — CORE | Alerts | **ACTIVE** | `PopupDurationSeconds` — Unique semantic input retained. |
| 135 | `PopupFontSize` | `int` | 12 · ALERTS — CORE | Alerts | **ACTIVE** | `PopupFontSize` — Unique semantic input retained. |
| 136 | `ShowEntryRestrictionPopup` | `bool` | 12 · ALERTS — CORE | Alerts | **ACTIVE** | `ShowEntryRestrictionPopup` — Unique semantic input retained. |
| 137 | `AlertOnNewsEventGuard` | `bool` | 12 · ALERTS — CORE | Alerts | **ACTIVE** | `AlertOnNewsEventGuard` — Unique semantic input retained. |
| 138 | `PopupMargin` | `int` | 12 · ALERTS — CORE | Alerts | **ACTIVE** | `PopupMargin` — Unique semantic input retained. |
| 139 | `PopupBorderAlpha` | `int` | 12 · ALERTS — CORE | Alerts | **ACTIVE** | `PopupBorderAlpha` — Unique semantic input retained. |
| 140 | `PopupBold` | `bool` | 12 · ALERTS — CORE | Alerts | **ACTIVE** | `PopupBold` — Unique semantic input retained. |
| 141 | `PopupFontFamily` | `string` | 12 · ALERTS — CORE | Alerts | **ACTIVE** | `PopupFontFamily` — Unique semantic input retained. |
| 142 | `AlertOnConfirmedSignal` | `bool` | 12 · ALERTS — CORE | Alerts | **ACTIVE** | `AlertOnConfirmedSignal` — Unique semantic input retained. |
| 143 | `AlertOnReaction` | `bool` | 12 · ALERTS — CORE | Alerts | **ACTIVE** | `AlertOnReaction` — Unique semantic input retained. |
| 144 | `AlertOnEarlyWatch` | `bool` | 12 · ALERTS — CORE | Alerts | **ACTIVE** | `AlertOnEarlyWatch` — Unique semantic input retained. |
| 145 | `AlertOnLevelHit` | `bool` | 12 · ALERTS — CORE | Alerts | **ACTIVE** | `AlertOnLevelHit` — Unique semantic input retained. |
| 146 | `AlertOnInvalidated` | `bool` | 12 · ALERTS — CORE | Alerts | **ACTIVE** | `AlertOnInvalidated` — Unique semantic input retained. |
| 147 | `AlertCooldownSeconds` | `int` | 12 · ALERTS — CORE | Alerts | **ACTIVE** | `AlertCooldownSeconds` — Unique semantic input retained. |
| 148 | `SuppressDuplicateAlerts` | `bool` | 12 · ALERTS — CORE | Alerts | **ACTIVE** | `SuppressDuplicateAlerts` — Unique semantic input retained. |
| 149 | `EnableEmailAlerts` | `bool` | 12 · ALERTS — CORE | Alerts | **ACTIVE** | `EnableEmailAlerts` — Unique semantic input retained. |
| 150 | `SenderEmail` | `string` | 12 · ALERTS — CORE | Alerts | **ACTIVE** | `SenderEmail` — Unique semantic input retained. |
| 151 | `ReceiverEmail` | `string` | 12 · ALERTS — CORE | Alerts | **ACTIVE** | `ReceiverEmail` — Unique semantic input retained. |
| 152 | `EnableAutoTrading` | `bool` | 13 · AUTO TRADING | Automation | **ACTIVE** | `EnableAutoTrading` — Unique semantic input retained. |
| 153 | `EnableAutomaticOrders` | `bool` | 13 · AUTO TRADING | Automation | **ACTIVE** | `EnableAutomaticOrders` — Unique semantic input retained. |
| 154 | `PendingOrderMode` | `CFIPClean75PendingOrderMode` | 13 · AUTO TRADING | Automation | **ACTIVE** | `PendingOrderMode` — Unique semantic input retained. |
| 155 | `PendingOrderExpiryMinutes` | `int` | 13 · AUTO TRADING | Automation | **ACTIVE** | `PendingOrderExpiryMinutes` — Unique semantic input retained. |
| 156 | `PendingEntryBufferAtr` | `double` | 13 · AUTO TRADING | Automation | **ACTIVE** | `PendingEntryBufferAtr` — Unique semantic input retained. |
| 157 | `PendingMinimumConfidence` | `int` | 13 · AUTO TRADING | Automation | **ACTIVE** | `PendingMinimumConfidence` — Unique semantic input retained. |
| 158 | `PendingMinimumSmartQuality` | `int` | 13 · AUTO TRADING | Automation | **ACTIVE** | `PendingMinimumSmartQuality` — Unique semantic input retained. |
| 159 | `PendingMinimumTrendQuality` | `int` | 13 · AUTO TRADING | Automation | **ACTIVE** | `PendingMinimumTrendQuality` — Unique semantic input retained. |
| 160 | `PendingAutoCleanup` | `bool` | 13 · AUTO TRADING | Automation | **ACTIVE** | `PendingAutoCleanup` — Unique semantic input retained. |
| 161 | `AutoTradingReminder` | `bool` | 13 · AUTO TRADING | Automation | **ACTIVE** | `AutoTradingReminder` — Unique semantic input retained. |
| 162 | `ReversalCloseMinimumEvidence` | `int` | 13 · AUTO TRADING | Automation | **ACTIVE** | `ReversalCloseMinimumEvidence` — Unique semantic input retained. |
| 163 | `ReversalCloseMinimumMtf` | `int` | 13 · AUTO TRADING | Automation | **ACTIVE** | `ReversalCloseMinimumMtf` — Unique semantic input retained. |
| 164 | `ConfirmedSignalsOnly` | `bool` | 13 · AUTO TRADING | Automation | **ACTIVE** | `ConfirmedSignalsOnly` — Unique semantic input retained. |
| 165 | `SizingMode` | `CFIPClean75SizingMode` | 13 · AUTO TRADING | Automation | **ACTIVE** | `SizingMode` — Unique semantic input retained. |
| 166 | `RiskPercentEquity` | `double` | 13 · AUTO TRADING | Automation | **ACTIVE** | `RiskPercentEquity` — Unique semantic input retained. |
| 167 | `FixedLots` | `double` | 13 · AUTO TRADING | Automation | **ACTIVE** | `FixedLots` — Unique semantic input retained. |
| 168 | `MinimumAutoConfidence` | `int` | 13 · AUTO TRADING | Automation | **ACTIVE** | `MinimumAutoConfidence` — Unique semantic input retained. |
| 169 | `MinimumAutoSmartQuality` | `int` | 13 · AUTO TRADING | Automation | **ACTIVE** | `MinimumAutoSmartQuality` — Unique semantic input retained. |
| 170 | `MinimumAutoLevelQuality` | `int` | 13 · AUTO TRADING | Automation | **ACTIVE** | `MinimumAutoLevelQuality` — Unique semantic input retained. |
| 171 | `AutoTpStage` | `CFIPClean75TargetStage` | 13 · AUTO TRADING | Automation | **ACTIVE** | `AutoTpStage` — Unique semantic input retained. |
| 172 | `EnableDynamicTpAdvance` | `bool` | 13 · AUTO TRADING | Automation | **ACTIVE** | `EnableDynamicTpAdvance` — Unique semantic input retained. |
| 173 | `TpAdvanceProximityPercent` | `double` | 13 · AUTO TRADING | Automation | **ACTIVE** | `TpAdvanceProximityPercent` — Unique semantic input retained. |
| 174 | `EnablePartialTakeProfit` | `bool` | 13 · AUTO TRADING | Automation | **ACTIVE** | `EnablePartialTakeProfit` — Unique semantic input retained. |
| 175 | `PartialCloseTp1Percent` | `double` | 13 · AUTO TRADING | Automation | **ACTIVE** | `PartialCloseTp1Percent` — Unique semantic input retained. |
| 176 | `PartialCloseTp2Percent` | `double` | 13 · AUTO TRADING | Automation | **ACTIVE** | `PartialCloseTp2Percent` — Unique semantic input retained. |
| 177 | `MoveToBreakEvenAfterPartial` | `bool` | 13 · AUTO TRADING | Automation | **ACTIVE** | `MoveToBreakEvenAfterPartial` — Unique semantic input retained. |
| 178 | `EnableReversalProtectionClose` | `bool` | 13 · AUTO TRADING | Automation | **ACTIVE** | `EnableReversalProtectionClose` — Unique semantic input retained. |
| 179 | `ReversalProtectionMinimumQuality` | `int` | 13 · AUTO TRADING | Automation | **ACTIVE** | `ReversalProtectionMinimumQuality` — Unique semantic input retained. |
| 180 | `MaximumOpenPositions` | `int` | 13 · AUTO TRADING | Automation | **ACTIVE** | `MaximumOpenPositions` — Unique semantic input retained. |
| 181 | `EnableDailyLossLimit` | `bool` | 13 · AUTO TRADING | Automation | **ACTIVE** | `EnableDailyLossLimit` — Unique semantic input retained. |
| 182 | `MaximumDailyLossPercent` | `double` | 13 · AUTO TRADING | Automation | **ACTIVE** | `MaximumDailyLossPercent` — Unique semantic input retained. |
| 183 | `UseMarketHoursGuard` | `bool` | 13 · AUTO TRADING | Automation | **ACTIVE** | `UseMarketHoursGuard` — Unique semantic input retained. |
| 184 | `UseAutoMarginGuard` | `bool` | 13 · AUTO TRADING | Automation | **ACTIVE** | `UseAutoMarginGuard` — Unique semantic input retained. |
| 185 | `MaxAutoMarginUsagePercent` | `double` | 13 · AUTO TRADING | Automation | **ACTIVE** | `MaxAutoMarginUsagePercent` — Unique semantic input retained. |
| 186 | `MarginBufferPercent` | `double` | 13 · AUTO TRADING | Automation | **ACTIVE** | `MarginBufferPercent` — Unique semantic input retained. |
| 187 | `AutoTradeLabel` | `string` | 13 · AUTO TRADING | Automation | **ACTIVE** | `AutoTradeLabel` — Unique semantic input retained. |
| 188 | `AutoBrokerProtection` | `bool` | 13 · AUTO TRADING | Automation | **ACTIVE** | `AutoBrokerProtection` — Unique semantic input retained. |
| 189 | `OneOrderPerSignal` | `bool` | 13 · AUTO TRADING | Automation | **ACTIVE** | `OneOrderPerSignal` — Unique semantic input retained. |
| 190 | `EnableMarketSuitabilityGuard` | `bool` | 24 · SMART EXECUTION | Smart Execution | **ACTIVE** | `EnableMarketSuitabilityGuard` — Unique semantic input retained. |
| 191 | `MinimumMarketSuitability` | `int` | 24 · SMART EXECUTION | Smart Execution | **ACTIVE** | `MinimumMarketSuitability` — Unique semantic input retained. |
| 192 | `HardMarketSuitabilityGate` | `bool` | 24 · SMART EXECUTION | Smart Execution | **ACTIVE** | `HardMarketSuitabilityGate` — Unique semantic input retained. |
| 193 | `RequireSessionSuitability` | `bool` | 24 · SMART EXECUTION | Smart Execution | **ACTIVE** | `RequireSessionSuitability` — Unique semantic input retained. |
| 194 | `UseSmartRiskScaling` | `bool` | 24 · SMART EXECUTION | Smart Execution | **ACTIVE** | `UseSmartRiskScaling` — Unique semantic input retained. |
| 195 | `MinimumSmartRiskMultiplier` | `double` | 24 · SMART EXECUTION | Smart Execution | **ACTIVE** | `MinimumSmartRiskMultiplier` — Unique semantic input retained. |
| 196 | `FullRiskConfidenceThreshold` | `int` | 24 · SMART EXECUTION | Smart Execution | **ACTIVE** | `FullRiskConfidenceThreshold` — Unique semantic input retained. |
| 197 | `FullRiskSuitabilityThreshold` | `int` | 24 · SMART EXECUTION | Smart Execution | **ACTIVE** | `FullRiskSuitabilityThreshold` — Unique semantic input retained. |
| 198 | `PenalizeChoppyRegimeRisk` | `bool` | 24 · SMART EXECUTION | Smart Execution | **ACTIVE** | `PenalizeChoppyRegimeRisk` — Unique semantic input retained. |
| 199 | `SuitabilityRecalculationSeconds` | `int` | 24 · SMART EXECUTION | Smart Execution | **ACTIVE** | `SuitabilityRecalculationSeconds` — Unique semantic input retained. |
| 200 | `ShowLevelLines` | `bool` | 14 · DISPLAY — CORE | Display | **ACTIVE** | `ShowLevelLines` — Unique semantic input retained. |
| 201 | `FullWidthLevelLines` | `bool` | 14 · DISPLAY — CORE | Display | **ACTIVE** | `FullWidthLevelLines` — Unique semantic input retained. |
| 202 | `LevelLineThickness` | `int` | 14 · DISPLAY — CORE | Display | **ACTIVE** | `LevelLineThickness` — Unique semantic input retained. |
| 203 | `ShowEntry` | `bool` | 14 · DISPLAY — CORE | Display | **ACTIVE** | `ShowEntry` — Unique semantic input retained. |
| 204 | `ShowTrigger` | `bool` | 14 · DISPLAY — CORE | Display | **ACTIVE** | `ShowTrigger` — Unique semantic input retained. |
| 205 | `ShowSL` | `bool` | 14 · DISPLAY — CORE | Display | **ACTIVE** | `ShowSL` — Unique semantic input retained. |
| 206 | `ShowTP1` | `bool` | 14 · DISPLAY — CORE | Display | **ACTIVE** | `ShowTP1` — Unique semantic input retained. |
| 207 | `ShowTP2` | `bool` | 14 · DISPLAY — CORE | Display | **ACTIVE** | `ShowTP2` — Unique semantic input retained. |
| 208 | `ShowTP3` | `bool` | 14 · DISPLAY — CORE | Display | **ACTIVE** | `ShowTP3` — Unique semantic input retained. |
| 209 | `ShowTP4` | `bool` | 14 · DISPLAY — CORE | Display | **ACTIVE** | `ShowTP4` — Unique semantic input retained. |
| 210 | `ShowSignalArrow` | `bool` | 14 · DISPLAY — CORE | Display | **ACTIVE** | `ShowSignalArrow` — Unique semantic input retained. |
| 211 | `ShowEarlyWatch` | `bool` | 14 · DISPLAY — CORE | Display | **ACTIVE** | `ShowEarlyWatch` — Unique semantic input retained. |
| 212 | `ShowHistoricalSignals` | `bool` | 14 · DISPLAY — CORE | Display | **ACTIVE** | `ShowHistoricalSignals` — Unique semantic input retained. |
| 213 | `HistoricalSignalLimit` | `int` | 14 · DISPLAY — CORE | Display | **ACTIVE** | `HistoricalSignalLimit` — Unique semantic input retained. |
| 214 | `ShowUnifiedPanel` | `bool` | 14 · DISPLAY — CORE | Display | **ACTIVE** | `ShowUnifiedPanel` — Unique semantic input retained. |
| 215 | `ShowPanelBackground` | `bool` | 14 · DISPLAY — CORE | Display | **ACTIVE** | `ShowPanelBackground` — Unique semantic input retained. |
| 216 | `PanelPosition` | `CFIPClean75PanelCorner` | 14 · DISPLAY — CORE | Display | **ACTIVE** | `PanelPosition` — Unique semantic input retained. |
| 217 | `PanelWidth` | `int` | 14 · DISPLAY — CORE | Display | **ACTIVE** | `PanelWidth` — Unique semantic input retained. |
| 218 | `PanelFontSize` | `int` | 14 · DISPLAY — CORE | Display | **ACTIVE** | `PanelFontSize` — Unique semantic input retained. |
| 219 | `PanelFontFamily` | `string` | 14 · DISPLAY — CORE | Display | **ACTIVE** | `PanelFontFamily` — Unique semantic input retained. |
| 220 | `PanelBold` | `bool` | 14 · DISPLAY — CORE | Display | **ACTIVE** | `PanelBold` — Unique semantic input retained. |
| 221 | `PanelBackground` | `Color` | 14 · DISPLAY — CORE | Display | **ACTIVE** | `PanelBackground` — Unique semantic input retained. |
| 222 | `PanelBackgroundAlpha` | `int` | 14 · DISPLAY — CORE | Display | **ACTIVE** | `PanelBackgroundAlpha` — Unique semantic input retained. |
| 223 | `PanelBorder` | `Color` | 14 · DISPLAY — CORE | Display | **ACTIVE** | `PanelBorder` — Unique semantic input retained. |
| 224 | `PanelBorderAlpha` | `int` | 14 · DISPLAY — CORE | Display | **ACTIVE** | `PanelBorderAlpha` — Unique semantic input retained. |
| 225 | `PanelBorderThickness` | `int` | 14 · DISPLAY — CORE | Display | **ACTIVE** | `PanelBorderThickness` — Unique semantic input retained. |
| 226 | `PanelCornerRadius` | `int` | 14 · DISPLAY — CORE | Display | **ACTIVE** | `PanelCornerRadius` — Unique semantic input retained. |
| 227 | `PanelPadding` | `int` | 14 · DISPLAY — CORE | Display | **ACTIVE** | `PanelPadding` — Unique semantic input retained. |
| 228 | `PanelMargin` | `int` | 14 · DISPLAY — CORE | Display | **ACTIVE** | `PanelMargin` — Unique semantic input retained. |
| 229 | `PanelRowGap` | `int` | 14 · DISPLAY — CORE | Display | **ACTIVE** | `PanelRowGap` — Unique semantic input retained. |
| 230 | `PanelMaxHeight` | `int` | 14 · DISPLAY — CORE | Display | **ACTIVE** | `PanelMaxHeight` — Unique semantic input retained. |
| 231 | `PanelRowPadding` | `int` | 14 · DISPLAY — CORE | Display | **ACTIVE** | `PanelRowPadding` — Unique semantic input retained. |
| 232 | `PanelButtonGap` | `int` | 14 · DISPLAY — CORE | Display | **ACTIVE** | `PanelButtonGap` — Unique semantic input retained. |
| 233 | `PanelAccentColor` | `Color` | 14 · DISPLAY — CORE | Display | **ACTIVE** | `PanelAccentColor` — Unique semantic input retained. |
| 234 | `PanelSectionColor` | `Color` | 14 · DISPLAY — CORE | Display | **ACTIVE** | `PanelSectionColor` — Unique semantic input retained. |
| 235 | `PanelSecondaryTextColor` | `Color` | 14 · DISPLAY — CORE | Display | **ACTIVE** | `PanelSecondaryTextColor` — Unique semantic input retained. |
| 236 | `PanelMutedTextColor` | `Color` | 14 · DISPLAY — CORE | Display | **ACTIVE** | `PanelMutedTextColor` — Unique semantic input retained. |
| 237 | `PanelWarningColor` | `Color` | 14 · DISPLAY — CORE | Display | **ACTIVE** | `PanelWarningColor` — Unique semantic input retained. |
| 238 | `PanelTextColor` | `Color` | 14 · DISPLAY — CORE | Display | **ACTIVE** | `PanelTextColor` — Unique semantic input retained. |
| 239 | `EntryLineColor` | `Color` | 14 · DISPLAY — CORE | Display | **ACTIVE** | `EntryLineColor` — Unique semantic input retained. |
| 240 | `TriggerLineColor` | `Color` | 14 · DISPLAY — CORE | Display | **ACTIVE** | `TriggerLineColor` — Unique semantic input retained. |
| 241 | `SlLineColor` | `Color` | 14 · DISPLAY — CORE | Display | **ACTIVE** | `SlLineColor` — Unique semantic input retained. |
| 242 | `TpLineColor` | `Color` | 14 · DISPLAY — CORE | Display | **ACTIVE** | `TpLineColor` — Unique semantic input retained. |
| 243 | `Tp2LineColor` | `Color` | 14 · DISPLAY — CORE | Display | **ACTIVE** | `Tp2LineColor` — Unique semantic input retained. |
| 244 | `Tp3LineColor` | `Color` | 14 · DISPLAY — CORE | Display | **ACTIVE** | `Tp3LineColor` — Unique semantic input retained. |
| 245 | `Tp4LineColor` | `Color` | 14 · DISPLAY — CORE | Display | **ACTIVE** | `Tp4LineColor` — Unique semantic input retained. |
| 246 | `BuyArrowColor` | `Color` | 14 · DISPLAY — CORE | Display | **ACTIVE** | `BuyArrowColor` — Unique semantic input retained. |
| 247 | `SellArrowColor` | `Color` | 14 · DISPLAY — CORE | Display | **ACTIVE** | `SellArrowColor` — Unique semantic input retained. |
| 248 | `LiveTriggerScore` | `int` | 15 · CONTROL — ADVANCED | Control | **ACTIVE** | `LiveTriggerScore` — Unique semantic input retained. |
| 249 | `PrecisionTriggerScore` | `int` | 15 · CONTROL — ADVANCED | Control | **ACTIVE** | `PrecisionTriggerScore` — Unique semantic input retained. |
| 250 | `AllowStrongM5TriggerOverride` | `bool` | 15 · CONTROL — ADVANCED | Control | **ACTIVE** | `AllowStrongM5TriggerOverride` — Unique semantic input retained. |
| 251 | `M5OnlyConfirmedTrigger` | `bool` | 15 · CONTROL — ADVANCED | Control | **ACTIVE** | `M5OnlyConfirmedTrigger` — Unique semantic input retained. |
| 252 | `AllowM15NeutralPullback` | `bool` | 15 · CONTROL — ADVANCED | Control | **ACTIVE** | `AllowM15NeutralPullback` — Unique semantic input retained. |
| 253 | `HigherTfPenalty` | `int` | 15 · CONTROL — ADVANCED | Control | **ACTIVE** | `HigherTfPenalty` — Unique semantic input retained. |
| 254 | `UseZoneConfluence` | `bool` | 15 · CONTROL — ADVANCED | Control | **ACTIVE** | `UseZoneConfluence` — Unique semantic input retained. |
| 255 | `UseHigherTfLiquidityTargets` | `bool` | 15 · CONTROL — ADVANCED | Control | **ACTIVE** | `UseHigherTfLiquidityTargets` — Unique semantic input retained. |
| 256 | `MinimumHtfTargetRR` | `double` | 15 · CONTROL — ADVANCED | Control | **ACTIVE** | `MinimumHtfTargetRR` — Unique semantic input retained. |
| 257 | `StructuralTpRrStep` | `double` | 15 · CONTROL — ADVANCED | Control | **ACTIVE** | `StructuralTpRrStep` — Unique semantic input retained. |
| 258 | `MinimumTradeRR` | `double` | 15 · CONTROL — ADVANCED | Control | **ACTIVE** | `MinimumTradeRR` — Unique semantic input retained. |
| 259 | `UseRRFilter` | `bool` | 15 · CONTROL — ADVANCED | Control | **ACTIVE** | `UseRRFilter` — Unique semantic input retained. |
| 260 | `AvoidLateEntry` | `bool` | 15 · CONTROL — ADVANCED | Control | **ACTIVE** | `AvoidLateEntry` — Unique semantic input retained. |
| 261 | `UsePrecisionExecutionModel` | `bool` | 15 · CONTROL — ADVANCED | Control | **ACTIVE** | `UsePrecisionExecutionModel` — Unique semantic input retained. |
| 262 | `StopBufferAtr` | `double` | 15 · CONTROL — ADVANCED | Control | **ACTIVE** | `StopBufferAtr` — Unique semantic input retained. |
| 263 | `RequireHtfTargets` | `bool` | 15 · CONTROL — ADVANCED | Control | **ACTIVE** | `RequireHtfTargets` — Unique semantic input retained. |
| 264 | `MaximumStructuralStopAtr` | `double` | 15 · CONTROL — ADVANCED | Control | **ACTIVE** | `MaximumStructuralStopAtr` — Unique semantic input retained. |
| 265 | `TargetObstacleLookbackBars` | `int` | 15 · CONTROL — ADVANCED | Control | **ACTIVE** | `TargetObstacleLookbackBars` — Unique semantic input retained. |
| 266 | `AllowDirectDisplacementOverride` | `bool` | 15 · CONTROL — ADVANCED | Control | **ACTIVE** | `AllowDirectDisplacementOverride` — Unique semantic input retained. |
| 267 | `DirectDisplacementOverrideScore` | `int` | 15 · CONTROL — ADVANCED | Control | **ACTIVE** | `DirectDisplacementOverrideScore` — Unique semantic input retained. |
| 268 | `MaximumSetupAgeBars` | `int` | 15 · CONTROL — ADVANCED | Control | **ACTIVE** | `MaximumSetupAgeBars` — Unique semantic input retained. |
| 269 | `AllowSyntheticTargetFallback` | `bool` | 15 · CONTROL — ADVANCED | Control | **ACTIVE** | `AllowSyntheticTargetFallback` — Unique semantic input retained. |
| 270 | `HtfStopBufferAtr` | `double` | 15 · CONTROL — ADVANCED | Control | **ACTIVE** | `HtfStopBufferAtr` — Unique semantic input retained. |
| 271 | `BlockNewSignalWhileActive` | `bool` | 15 · CONTROL — ADVANCED | Control | **ACTIVE** | `BlockNewSignalWhileActive` — Unique semantic input retained. |
| 272 | `CooldownBars` | `int` | 15 · CONTROL — ADVANCED | Control | **ACTIVE** | `CooldownBars` — Unique semantic input retained. |
| 273 | `UseNewsEventGuard` | `bool` | 15 · CONTROL — ADVANCED | Control | **ACTIVE** | `UseNewsEventGuard` — Unique semantic input retained. |
| 274 | `UseVolatilityEventGuard` | `bool` | 15 · CONTROL — ADVANCED | Control | **ACTIVE** | `UseVolatilityEventGuard` — Unique semantic input retained. |
| 275 | `EventGuardCooldownBars` | `int` | 15 · CONTROL — ADVANCED | Control | **ACTIVE** | `EventGuardCooldownBars` — Unique semantic input retained. |
| 276 | `UseRegimeNoTradeGuard` | `bool` | 15 · CONTROL — ADVANCED | Control | **ACTIVE** | `UseRegimeNoTradeGuard` — Unique semantic input retained. |
| 277 | `ShowReactionArrow` | `bool` | 15 · CONTROL — ADVANCED | Control | **ACTIVE** | `ShowReactionArrow` — Unique semantic input retained. |
| 278 | `ShowHistoricalArrows` | `bool` | 15 · CONTROL — ADVANCED | Control | **ACTIVE** | `ShowHistoricalArrows` — Unique semantic input retained. |
| 279 | `EnableDynamicSlTrail` | `bool` | 15 · CONTROL — ADVANCED | Control | **ACTIVE** | `EnableDynamicSlTrail` — Unique semantic input retained. |
| 280 | `TrailDistanceAtr` | `double` | 15 · CONTROL — ADVANCED | Control | **ACTIVE** | `TrailDistanceAtr` — Unique semantic input retained. |
| 281 | `TrailStepAtr` | `double` | 15 · CONTROL — ADVANCED | Control | **ACTIVE** | `TrailStepAtr` — Unique semantic input retained. |
| 282 | `TargetUpdateStepAtr` | `double` | 15 · CONTROL — ADVANCED | Control | **ACTIVE** | `TargetUpdateStepAtr` — Unique semantic input retained. |
| 283 | `UseSwingStructureInTrail` | `bool` | 15 · CONTROL — ADVANCED | Control | **ACTIVE** | `UseSwingStructureInTrail` — Unique semantic input retained. |
| 284 | `SmartMinimumIndependentEvidence` | `int` | 15 · CONTROL — ADVANCED | Control | **ACTIVE** | `SmartMinimumIndependentEvidence` — Unique semantic input retained. |
| 285 | `SmartStopZoneBonus` | `int` | 15 · CONTROL — ADVANCED | Control | **ACTIVE** | `SmartStopZoneBonus` — Unique semantic input retained. |
| 286 | `SmartLiquidityPoolBonus` | `int` | 15 · CONTROL — ADVANCED | Control | **ACTIVE** | `SmartLiquidityPoolBonus` — Unique semantic input retained. |
| 287 | `SmartTrailMinimumRR` | `double` | 15 · CONTROL — ADVANCED | Control | **ACTIVE** | `SmartTrailMinimumRR` — Unique semantic input retained. |
| 288 | `SmartUseClosedBarDecision` | `bool` | 15 · CONTROL — ADVANCED | Control | **ACTIVE** | `SmartUseClosedBarDecision` — Unique semantic input retained. |
| 289 | `SmartTargetNearestBias` | `double` | 15 · CONTROL — ADVANCED | Control | **ACTIVE** | `SmartTargetNearestBias` — Unique semantic input retained. |
| 290 | `RequireSmartConsensus` | `bool` | 15 · CONTROL — ADVANCED | Control | **ACTIVE** | `RequireSmartConsensus` — Unique semantic input retained. |
| 291 | `SmartStrongSetupQuality` | `int` | 15 · CONTROL — ADVANCED | Control | **ACTIVE** | `SmartStrongSetupQuality` — Unique semantic input retained. |
| 292 | `SmartStrongSetupEdge` | `int` | 15 · CONTROL — ADVANCED | Control | **ACTIVE** | `SmartStrongSetupEdge` — Unique semantic input retained. |
| 293 | `SmartFlipConfirmationBars` | `int` | 15 · CONTROL — ADVANCED | Control | **ACTIVE** | `SmartFlipConfirmationBars` — Unique semantic input retained. |
| 294 | `AllowSmartSoftGate` | `bool` | 15 · CONTROL — ADVANCED | Control | **ACTIVE** | `AllowSmartSoftGate` — Unique semantic input retained. |
| 295 | `EnableFastReversalIntelligence` | `bool` | 15 · CONTROL — ADVANCED | Control | **ACTIVE** | `EnableFastReversalIntelligence` — Unique semantic input retained. |
| 296 | `FastReversalMinimumQuality` | `int` | 15 · CONTROL — ADVANCED | Control | **ACTIVE** | `FastReversalMinimumQuality` — Unique semantic input retained. |
| 297 | `FastReversalLookbackBars` | `int` | 15 · CONTROL — ADVANCED | Control | **ACTIVE** | `FastReversalLookbackBars` — Unique semantic input retained. |
| 298 | `FastReversalMinimumZoneQuality` | `int` | 15 · CONTROL — ADVANCED | Control | **ACTIVE** | `FastReversalMinimumZoneQuality` — Unique semantic input retained. |
| 299 | `AllowFastM5ReversalBeforeM15` | `bool` | 15 · CONTROL — ADVANCED | Control | **ACTIVE** | `AllowFastM5ReversalBeforeM15` — Unique semantic input retained. |
| 300 | `RetestLookbackBars` | `int` | 15 · CONTROL — ADVANCED | Control | **ACTIVE** | `RetestLookbackBars` — Unique semantic input retained. |
| 301 | `RetestMaxBarsAfterDisplacement` | `int` | 15 · CONTROL — ADVANCED | Control | **ACTIVE** | `RetestMaxBarsAfterDisplacement` — Unique semantic input retained. |
| 302 | `RetestZoneToleranceAtr` | `double` | 15 · CONTROL — ADVANCED | Control | **ACTIVE** | `RetestZoneToleranceAtr` — Unique semantic input retained. |
| 303 | `RetestRejectionBodyAtr` | `double` | 15 · CONTROL — ADVANCED | Control | **ACTIVE** | `RetestRejectionBodyAtr` — Unique semantic input retained. |
| 304 | `RequireRetestCloseConfirmation` | `bool` | 15 · CONTROL — ADVANCED | Control | **ACTIVE** | `RequireRetestCloseConfirmation` — Unique semantic input retained. |
| 305 | `UseExtendedLiquidityMap` | `bool` | 15 · CONTROL — ADVANCED | Control | **ACTIVE** | `UseExtendedLiquidityMap` — Unique semantic input retained. |
| 306 | `UseSessionLiquidityTargets` | `bool` | 15 · CONTROL — ADVANCED | Control | **ACTIVE** | `UseSessionLiquidityTargets` — Unique semantic input retained. |
| 307 | `LiquidityTargetMinimumScore` | `int` | 15 · CONTROL — ADVANCED | Control | **ACTIVE** | `LiquidityTargetMinimumScore` — Unique semantic input retained. |
| 308 | `TargetObstacleBufferAtr` | `double` | 15 · CONTROL — ADVANCED | Control | **ACTIVE** | `TargetObstacleBufferAtr` — Unique semantic input retained. |
| 309 | `RequireObstacleFreeTp1` | `bool` | 15 · CONTROL — ADVANCED | Control | **ACTIVE** | `RequireObstacleFreeTp1` — Unique semantic input retained. |
| 310 | `UseVolumeExpansion` | `bool` | 20 · Confluence Extensions | Confluence Extensions | **ACTIVE** | `UseVolumeExpansion` — Unique semantic input retained. |
| 311 | `VolumeExpansionRatio` | `double` | 20 · Confluence Extensions | Confluence Extensions | **ACTIVE** | `VolumeExpansionRatio` — Unique semantic input retained. |
| 312 | `UseMacdBias` | `bool` | 20 · Confluence Extensions | Confluence Extensions | **ACTIVE** | `UseMacdBias` — Unique semantic input retained. |
| 313 | `MacdFastPeriod` | `int` | 20 · Confluence Extensions | Confluence Extensions | **ACTIVE** | `MacdFastPeriod` — Unique semantic input retained. |
| 314 | `MacdSlowPeriod` | `int` | 20 · Confluence Extensions | Confluence Extensions | **ACTIVE** | `MacdSlowPeriod` — Unique semantic input retained. |
| 315 | `UseVwapBias` | `bool` | 20 · Confluence Extensions | Confluence Extensions | **ACTIVE** | `UseVwapBias` — Unique semantic input retained. |
| 316 | `VwapLookbackBars` | `int` | 20 · Confluence Extensions | Confluence Extensions | **ACTIVE** | `VwapLookbackBars` — Unique semantic input retained. |
| 317 | `UseHealthyVolatility` | `bool` | 20 · Confluence Extensions | Confluence Extensions | **ACTIVE** | `UseHealthyVolatility` — Unique semantic input retained. |
| 318 | `HealthyAtrMinimumRatio` | `double` | 20 · Confluence Extensions | Confluence Extensions | **ACTIVE** | `HealthyAtrMinimumRatio` — Unique semantic input retained. |
| 319 | `HealthyAtrMaximumRatio` | `double` | 20 · Confluence Extensions | Confluence Extensions | **ACTIVE** | `HealthyAtrMaximumRatio` — Unique semantic input retained. |
| 320 | `EarlySetupConfidence` | `int` | 21 · Complete Intelligence | Complete Intelligence | **ACTIVE** | `EarlySetupConfidence` — Unique semantic input retained. |
| 321 | `EnableLiveReaction` | `bool` | 21 · Complete Intelligence | Complete Intelligence | **ACTIVE** | `EnableLiveReaction` — Unique semantic input retained. |
| 322 | `LiveReactionWatchThreshold` | `int` | 21 · Complete Intelligence | Complete Intelligence | **ACTIVE** | `LiveReactionWatchThreshold` — Unique semantic input retained. |
| 323 | `LiveReactionThreshold` | `int` | 21 · Complete Intelligence | Complete Intelligence | **ACTIVE** | `LiveReactionThreshold` — Unique semantic input retained. |
| 324 | `LiveReactionStrongThreshold` | `int` | 21 · Complete Intelligence | Complete Intelligence | **ACTIVE** | `LiveReactionStrongThreshold` — Unique semantic input retained. |
| 325 | `MinimumLiveReactionEvidence` | `int` | 21 · Complete Intelligence | Complete Intelligence | **ACTIVE** | `MinimumLiveReactionEvidence` — Unique semantic input retained. |
| 326 | `UseVolumeExpansionEvidence` | `bool` | 21 · Complete Intelligence | Complete Intelligence | **ACTIVE** | `UseVolumeExpansionEvidence` — Unique semantic input retained. |
| 327 | `UseMacdEvidence` | `bool` | 21 · Complete Intelligence | Complete Intelligence | **ACTIVE** | `UseMacdEvidence` — Unique semantic input retained. |
| 328 | `UseVwapEvidence` | `bool` | 21 · Complete Intelligence | Complete Intelligence | **ACTIVE** | `UseVwapEvidence` — Unique semantic input retained. |
| 329 | `UseHealthyVolatilityEvidence` | `bool` | 21 · Complete Intelligence | Complete Intelligence | **ACTIVE** | `UseHealthyVolatilityEvidence` — Unique semantic input retained. |
| 330 | `MinimumSmartDirectionShare` | `int` | 21 · Complete Intelligence | Complete Intelligence | **ACTIVE** | `MinimumSmartDirectionShare` — Unique semantic input retained. |
| 331 | `AdaptiveSmartThresholds` | `bool` | 21 · Complete Intelligence | Complete Intelligence | **ACTIVE** | `AdaptiveSmartThresholds` — Unique semantic input retained. |
| 332 | `SmartRegimeBuffer` | `int` | 21 · Complete Intelligence | Complete Intelligence | **ACTIVE** | `SmartRegimeBuffer` — Unique semantic input retained. |
| 333 | `SmartScoreTemperature` | `double` | 21 · Complete Intelligence | Complete Intelligence | **ACTIVE** | `SmartScoreTemperature` — Unique semantic input retained. |
| 334 | `SmartConsensusThreshold` | `int` | 21 · Complete Intelligence | Complete Intelligence | **ACTIVE** | `SmartConsensusThreshold` — Unique semantic input retained. |
| 335 | `AdaptiveRegimeWeighting` | `bool` | 21 · Complete Intelligence | Complete Intelligence | **ACTIVE** | `AdaptiveRegimeWeighting` — Unique semantic input retained. |
| 336 | `UseMultiTfLevelMap` | `bool` | 21 · Complete Intelligence | Complete Intelligence | **ACTIVE** | `UseMultiTfLevelMap` — Unique semantic input retained. |
| 337 | `SmartLevelClusterAtr` | `double` | 21 · Complete Intelligence | Complete Intelligence | **ACTIVE** | `SmartLevelClusterAtr` — Unique semantic input retained. |
| 338 | `SmartTargetQuality` | `int` | 21 · Complete Intelligence | Complete Intelligence | **ACTIVE** | `SmartTargetQuality` — Unique semantic input retained. |
| 339 | `SmartStopQuality` | `int` | 21 · Complete Intelligence | Complete Intelligence | **ACTIVE** | `SmartStopQuality` — Unique semantic input retained. |
| 340 | `SmartExitPressureThreshold` | `int` | 21 · Complete Intelligence | Complete Intelligence | **ACTIVE** | `SmartExitPressureThreshold` — Unique semantic input retained. |
| 341 | `SmartAlertCooldownSeconds` | `int` | 21 · Complete Intelligence | Complete Intelligence | **ACTIVE** | `SmartAlertCooldownSeconds` — Unique semantic input retained. |
| 342 | `SmartWeeklyContext` | `bool` | 21 · Complete Intelligence | Complete Intelligence | **ACTIVE** | `SmartWeeklyContext` — Unique semantic input retained. |
| 343 | `BlockSameBarReentryAfterExit` | `bool` | 21 · Complete Intelligence | Complete Intelligence | **ACTIVE** | `BlockSameBarReentryAfterExit` — Unique semantic input retained. |
| 344 | `AllowExecutionFrameStopFallback` | `bool` | 21 · Complete Intelligence | Complete Intelligence | **ACTIVE** | `AllowExecutionFrameStopFallback` — Unique semantic input retained. |
| 345 | `UseHistoricalChoppinessGuard` | `bool` | 21 · Complete Intelligence | Complete Intelligence | **ACTIVE** | `UseHistoricalChoppinessGuard` — Unique semantic input retained. |
| 346 | `AlertOnNewsEvent` | `bool` | 21 · Complete Intelligence | Complete Intelligence | **ACTIVE** | `AlertOnNewsEvent` — Unique semantic input retained. |
| 347 | `AlertOnSessionBlock` | `bool` | 21 · Complete Intelligence | Complete Intelligence | **ACTIVE** | `AlertOnSessionBlock` — Unique semantic input retained. |
| 348 | `AlertOnSpreadBlock` | `bool` | 21 · Complete Intelligence | Complete Intelligence | **ACTIVE** | `AlertOnSpreadBlock` — Unique semantic input retained. |
| 349 | `AlertOnFridayBlock` | `bool` | 21 · Complete Intelligence | Complete Intelligence | **ACTIVE** | `AlertOnFridayBlock` — Unique semantic input retained. |
| 350 | `AlertOnRegimeNoTrade` | `bool` | 21 · Complete Intelligence | Complete Intelligence | **ACTIVE** | `AlertOnRegimeNoTrade` — Unique semantic input retained. |
| 351 | `AlertOnCooldownBlock` | `bool` | 21 · Complete Intelligence | Complete Intelligence | **ACTIVE** | `AlertOnCooldownBlock` — Unique semantic input retained. |
| 352 | `AlertOnExitPlanUpdate` | `bool` | 21 · Complete Intelligence | Complete Intelligence | **ACTIVE** | `AlertOnExitPlanUpdate` — Unique semantic input retained. |
| 353 | `ShowEngineStatus` | `bool` | 21 · Complete Intelligence | Complete Intelligence | **ACTIVE** | `ShowEngineStatus` — Unique semantic input retained. |
| 354 | `PanelStateHoldSeconds` | `int` | 21 · Complete Intelligence | Complete Intelligence | **ACTIVE** | `PanelStateHoldSeconds` — Unique semantic input retained. |
| 355 | `ShowLevelPricesInUnifiedPanel` | `bool` | 21 · Complete Intelligence | Complete Intelligence | **ACTIVE** | `ShowLevelPricesInUnifiedPanel` — Unique semantic input retained. |
| 356 | `ShowTradePlanPanel` | `bool` | 21 · Complete Intelligence | Complete Intelligence | **ACTIVE** | `ShowTradePlanPanel` — Unique semantic input retained. |
| 357 | `ShowTradeActionButtons` | `bool` | 13 · AUTO TRADING | Automation | **REMOVED** | — — Manual BUY/SELL/order-placement UI is excluded from the clean product architecture. Parameter is retained only for preset compatibility and is not an authority. |
| 358 | `AlwaysShowSafetyButtons` | `bool` | 13 · AUTO TRADING | Automation | **ACTIVE** | `AlwaysShowSafetyButtons` — Unique semantic input retained. |
| 359 | `ActionButtonMargin` | `int` | 13 · AUTO TRADING | Automation | **ACTIVE** | `ActionButtonMargin` — Unique semantic input retained. |
| 360 | `UseAuthoritativeSignalState` | `bool` | 21 · Complete Intelligence | Complete Intelligence | **ACTIVE** | `UseAuthoritativeSignalState` — Unique semantic input retained. |
| 361 | `RequirePlanIntegrity` | `bool` | 21 · Complete Intelligence | Complete Intelligence | **ACTIVE** | `RequirePlanIntegrity` — Unique semantic input retained. |
| 362 | `MaximumSpreadToStopRiskRatio` | `double` | 21 · Complete Intelligence | Complete Intelligence | **ACTIVE** | `MaximumSpreadToStopRiskRatio` — Unique semantic input retained. |
| 363 | `MinimumSmartTargetQualityForTp1` | `int` | 21 · Complete Intelligence | Complete Intelligence | **ACTIVE** | `MinimumSmartTargetQualityForTp1` — Unique semantic input retained. |
| 364 | `PredictionColor` | `Color` | 21 · Complete Intelligence | Complete Intelligence | **ACTIVE** | `PredictionColor` — Unique semantic input retained. |
| 365 | `StrongBuyArrowColor` | `Color` | 21 · Complete Intelligence | Complete Intelligence | **ACTIVE** | `StrongBuyArrowColor` — Unique semantic input retained. |
| 366 | `StrongSellArrowColor` | `Color` | 21 · Complete Intelligence | Complete Intelligence | **ACTIVE** | `StrongSellArrowColor` — Unique semantic input retained. |
| 367 | `ConfirmedBuyArrowColor` | `Color` | 21 · Complete Intelligence | Complete Intelligence | **ACTIVE** | `ConfirmedBuyArrowColor` — Unique semantic input retained. |
| 368 | `ConfirmedSellArrowColor` | `Color` | 21 · Complete Intelligence | Complete Intelligence | **ACTIVE** | `ConfirmedSellArrowColor` — Unique semantic input retained. |
| 369 | `CautionBuyArrowColor` | `Color` | 21 · Complete Intelligence | Complete Intelligence | **ACTIVE** | `CautionBuyArrowColor` — Unique semantic input retained. |
| 370 | `CautionSellArrowColor` | `Color` | 21 · Complete Intelligence | Complete Intelligence | **ACTIVE** | `CautionSellArrowColor` — Unique semantic input retained. |
| 371 | `BlockedReactionArrowColor` | `Color` | 21 · Complete Intelligence | Complete Intelligence | **ACTIVE** | `BlockedReactionArrowColor` — Unique semantic input retained. |
| 372 | `FallbackTp1RR` | `double` | 21 · Complete Intelligence | Complete Intelligence | **ACTIVE** | `FallbackTp1RR` — Unique semantic input retained. |
| 373 | `FallbackTp2RR` | `double` | 21 · Complete Intelligence | Complete Intelligence | **ACTIVE** | `FallbackTp2RR` — Unique semantic input retained. |
| 374 | `FallbackTp3RR` | `double` | 21 · Complete Intelligence | Complete Intelligence | **ACTIVE** | `FallbackTp3RR` — Unique semantic input retained. |
| 375 | `FallbackTp4RR` | `double` | 21 · Complete Intelligence | Complete Intelligence | **ACTIVE** | `FallbackTp4RR` — Unique semantic input retained. |
| 376 | `RequirePrecisionEntry` | `bool` | 23 · Structural Execution | Structural Execution | **ACTIVE** | `RequirePrecisionEntry` — Unique semantic input retained. |
| 377 | `ExecutionZoneAtr` | `double` | 23 · Structural Execution | Structural Execution | **ACTIVE** | `ExecutionZoneAtr` — Unique semantic input retained. |
| 378 | `MinimumEntryQuality` | `int` | 23 · Structural Execution | Structural Execution | **ACTIVE** | `MinimumEntryQuality` — Unique semantic input retained. |
| 379 | `MaximumEntryDistanceAtr` | `double` | 23 · Structural Execution | Structural Execution | **ACTIVE** | `MaximumEntryDistanceAtr` — Unique semantic input retained. |
| 380 | `AllowPrecisionBreakoutEntry` | `bool` | 23 · Structural Execution | Structural Execution | **ACTIVE** | `AllowPrecisionBreakoutEntry` — Unique semantic input retained. |
| 381 | `PrecisionBreakoutBufferAtr` | `double` | 23 · Structural Execution | Structural Execution | **ACTIVE** | `PrecisionBreakoutBufferAtr` — Unique semantic input retained. |
| 382 | `MinimumTargetsForPlan` | `int` | 23 · Structural Execution | Structural Execution | **ACTIVE** | `MinimumTargetsForPlan` — Unique semantic input retained. |
| 383 | `RequireHtfRewardForTp1` | `bool` | 23 · Structural Execution | Structural Execution | **ACTIVE** | `RequireHtfRewardForTp1` — Unique semantic input retained. |
| 384 | `RequireHtfRewardForTp2Plus` | `bool` | 23 · Structural Execution | Structural Execution | **ACTIVE** | `RequireHtfRewardForTp2Plus` — Unique semantic input retained. |
| 385 | `MinimumHtfRewardQuality` | `int` | 23 · Structural Execution | Structural Execution | **ACTIVE** | `MinimumHtfRewardQuality` — Unique semantic input retained. |
| 386 | `MaximumRewardRR` | `double` | 23 · Structural Execution | Structural Execution | **ACTIVE** | `MaximumRewardRR` — Unique semantic input retained. |
| 387 | `HtfRewardBonus` | `int` | 23 · Structural Execution | Structural Execution | **ACTIVE** | `HtfRewardBonus` — Unique semantic input retained. |
| 388 | `LiquidityRewardBonus` | `int` | 23 · Structural Execution | Structural Execution | **ACTIVE** | `LiquidityRewardBonus` — Unique semantic input retained. |
| 389 | `ZoneRewardBonus` | `int` | 23 · Structural Execution | Structural Execution | **ACTIVE** | `ZoneRewardBonus` — Unique semantic input retained. |
| 390 | `PreferredStopRiskAtr` | `double` | 23 · Structural Execution | Structural Execution | **ACTIVE** | `PreferredStopRiskAtr` — Unique semantic input retained. |
| 391 | `StopRiskBalanceWeight` | `int` | 23 · Structural Execution | Structural Execution | **ACTIVE** | `StopRiskBalanceWeight` — Unique semantic input retained. |
| 392 | `MinimumStructuralStopQuality` | `int` | 23 · Structural Execution | Structural Execution | **ACTIVE** | `MinimumStructuralStopQuality` — Unique semantic input retained. |
| 393 | `StructuralStopManagementOnly` | `bool` | 23 · Structural Execution | Structural Execution | **ACTIVE** | `StructuralStopManagementOnly` — Unique semantic input retained. |
| 394 | `StructuralTargetUpdatesOnly` | `bool` | 23 · Structural Execution | Structural Execution | **ACTIVE** | `StructuralTargetUpdatesOnly` — Unique semantic input retained. |
| 395 | `UseM5StructureForStop` | `bool` | 22 · Safety & Precision | Safety / Precision | **ACTIVE** | `UseM5StructureForStop` — Unique semantic input retained. |
| 396 | `UseZoneMitigationGuard` | `bool` | 22 · Safety & Precision | Safety / Precision | **ACTIVE** | `UseZoneMitigationGuard` — Unique semantic input retained. |
| 397 | `RequireObRetest` | `bool` | 22 · Safety & Precision | Safety / Precision | **ACTIVE** | `RequireObRetest` — Unique semantic input retained. |
| 398 | `FvgInvalidateOnFullFill` | `bool` | 22 · Safety & Precision | Safety / Precision | **ACTIVE** | `FvgInvalidateOnFullFill` — Unique semantic input retained. |
| 399 | `EnableFvgPartialMitigation` | `bool` | 22 · Safety & Precision | Safety / Precision | **ACTIVE** | `EnableFvgPartialMitigation` — Unique semantic input retained. |
| 400 | `FvgBreakByWicks` | `bool` | 22 · Safety & Precision | Safety / Precision | **ACTIVE** | `FvgBreakByWicks` — Unique semantic input retained. |
| 401 | `IncludeSpreadInRiskSizing` | `bool` | 13 · AUTO TRADING | Automation | **ACTIVE** | `IncludeSpreadInRiskSizing` — Unique semantic input retained. |
| 402 | `UseSemanticAlertSounds` | `bool` | 22 · Safety & Precision | Safety / Precision | **ACTIVE** | `UseSemanticAlertSounds` — Unique semantic input retained. |
| 403 | `ManagedActionsOnly` | `bool` | 13 · AUTO TRADING | Automation | **ACTIVE** | `ManagedActionsOnly` — Unique semantic input retained. |
| 404 | `ShowSpreadDiagnostics` | `bool` | 22 · Safety & Precision | Safety / Precision | **ACTIVE** | `ShowSpreadDiagnostics` — Unique semantic input retained. |
| 405 | `EnableEarlyPrediction` | `bool` | 15 · INTELLIGENCE — EARLY | Early Intelligence | **ACTIVE** | `EnableEarlyPrediction` — Unique semantic input retained. |
| 406 | `MinimumEarlyConfidence` | `int` | 15 · INTELLIGENCE — EARLY | Early Intelligence | **ACTIVE** | `MinimumEarlyConfidence` — Unique semantic input retained. |
| 407 | `PredictionLookaheadBars` | `int` | 15 · INTELLIGENCE — EARLY | Early Intelligence | **ACTIVE** | `PredictionLookaheadBars` — Unique semantic input retained. |
| 408 | `ShowPredictionZone` | `bool` | 15 · INTELLIGENCE — EARLY | Early Intelligence | **ACTIVE** | `ShowPredictionZone` — Unique semantic input retained. |
| 409 | `ShowPredictionTargets` | `bool` | 15 · INTELLIGENCE — EARLY | Early Intelligence | **ACTIVE** | `ShowPredictionTargets` — Unique semantic input retained. |
| 410 | `UseLiquidityForecast` | `bool` | 15 · INTELLIGENCE — EARLY | Early Intelligence | **ACTIVE** | `UseLiquidityForecast` — Unique semantic input retained. |
| 411 | `AlertOnEarlySetup` | `bool` | 15 · INTELLIGENCE — EARLY | Early Intelligence | **ACTIVE** | `AlertOnEarlySetup` — Unique semantic input retained. |
| 412 | `AlertOnBos` | `bool` | 15 · INTELLIGENCE — EARLY | Early Intelligence | **ACTIVE** | `AlertOnBos` — Unique semantic input retained. |
| 413 | `AlertOnMssChoch` | `bool` | 15 · INTELLIGENCE — EARLY | Early Intelligence | **ACTIVE** | `AlertOnMssChoch` — Unique semantic input retained. |
| 414 | `AlertOnLiquiditySweep` | `bool` | 15 · INTELLIGENCE — EARLY | Early Intelligence | **ACTIVE** | `AlertOnLiquiditySweep` — Unique semantic input retained. |
| 415 | `EnableOutcomeTelemetry` | `bool` | 15 · INTELLIGENCE — EARLY | Early Intelligence | **ACTIVE** | `EnableOutcomeTelemetry` — Unique semantic input retained. |
| 416 | `OutcomeMaximumM5Bars` | `int` | 15 · INTELLIGENCE — EARLY | Early Intelligence | **ACTIVE** | `OutcomeMaximumM5Bars` — Unique semantic input retained. |
| 417 | `EnableConfidenceCalibration` | `bool` | 15 · INTELLIGENCE — EARLY | Early Intelligence | **ACTIVE** | `EnableConfidenceCalibration` — Unique semantic input retained. |
| 418 | `UseEmpiricalCalibration` | `bool` | 15 · INTELLIGENCE — EARLY | Early Intelligence | **ACTIVE** | `UseEmpiricalCalibration` — Unique semantic input retained. |
| 419 | `CalibrationDirectionalMinimumSamples` | `int` | 15 · INTELLIGENCE — EARLY | Early Intelligence | **ACTIVE** | `CalibrationDirectionalMinimumSamples` — Unique semantic input retained. |
| 420 | `CalibrationMinimumSamples` | `int` | 15 · INTELLIGENCE — EARLY | Early Intelligence | **ACTIVE** | `CalibrationMinimumSamples` — Unique semantic input retained. |
| 421 | `CalibrationMaxConfidenceAdjustment` | `int` | 15 · INTELLIGENCE — EARLY | Early Intelligence | **ACTIVE** | `CalibrationMaxConfidenceAdjustment` — Unique semantic input retained. |
| 422 | `ShowOutcomeDiagnostics` | `bool` | 15 · INTELLIGENCE — EARLY | Early Intelligence | **ACTIVE** | `ShowOutcomeDiagnostics` — Unique semantic input retained. |
| 423 | `UseSmartEntryQualityFilter` | `bool` | 16 · Accuracy | Accuracy | **ACTIVE** | `UseSmartEntryQualityFilter` — Unique semantic input retained. |
| 424 | `SmartQualityThreshold` | `int` | 16 · Accuracy | Accuracy | **ACTIVE** | `SmartQualityThreshold` — Unique semantic input retained. |
| 425 | `RequireFreshM5Trigger` | `bool` | 16 · Accuracy | Accuracy | **ACTIVE** | `RequireFreshM5Trigger` — Unique semantic input retained. |
| 426 | `MinimumFreshTriggerEvidence` | `int` | 16 · Accuracy | Accuracy | **ACTIVE** | `MinimumFreshTriggerEvidence` — Unique semantic input retained. |
| 427 | `UseFalseSignalGuard` | `bool` | 16 · Accuracy | Accuracy | **ACTIVE** | `UseFalseSignalGuard` — Unique semantic input retained. |
| 428 | `FalseSignalAdverseR` | `double` | 16 · Accuracy | Accuracy | **ACTIVE** | `FalseSignalAdverseR` — Unique semantic input retained. |
| 429 | `FalseSignalWatchBars` | `int` | 16 · Accuracy | Accuracy | **ACTIVE** | `FalseSignalWatchBars` — Unique semantic input retained. |
| 430 | `InvalidateOnFalseSignal` | `bool` | 16 · Accuracy | Accuracy | **ACTIVE** | `InvalidateOnFalseSignal` — Unique semantic input retained. |
| 431 | `EnableSetupInvalidation` | `bool` | 16 · Accuracy | Accuracy | **ACTIVE** | `EnableSetupInvalidation` — Unique semantic input retained. |
| 432 | `InvalidationStructureAtr` | `double` | 16 · Accuracy | Accuracy | **ACTIVE** | `InvalidationStructureAtr` — Unique semantic input retained. |
| 433 | `InvalidationZoneCloseAtr` | `double` | 16 · Accuracy | Accuracy | **ACTIVE** | `InvalidationZoneCloseAtr` — Unique semantic input retained. |
| 434 | `InvalidationMaxAdverseR` | `double` | 16 · Accuracy | Accuracy | **ACTIVE** | `InvalidationMaxAdverseR` — Unique semantic input retained. |
| 435 | `RequireMtfFlipForInvalidation` | `bool` | 16 · Accuracy | Accuracy | **ACTIVE** | `RequireMtfFlipForInvalidation` — Unique semantic input retained. |
| 436 | `AllowReversalAgainstStaleHtf` | `bool` | 16 · Accuracy | Accuracy | **ACTIVE** | `AllowReversalAgainstStaleHtf` — Unique semantic input retained. |
| 437 | `EnableLiveStructuralReversal` | `bool` | 16 · Accuracy | Accuracy | **ACTIVE** | `EnableLiveStructuralReversal` — Unique semantic input retained. |
| 438 | `LiveReversalMinimumConfidence` | `int` | 16 · Accuracy | Accuracy | **ACTIVE** | `LiveReversalMinimumConfidence` — Unique semantic input retained. |
| 439 | `LiveReversalMinimumEvidence` | `int` | 16 · Accuracy | Accuracy | **ACTIVE** | `LiveReversalMinimumEvidence` — Unique semantic input retained. |
| 440 | `LiveReversalStructuralScore` | `int` | 16 · Accuracy | Accuracy | **ACTIVE** | `LiveReversalStructuralScore` — Unique semantic input retained. |
| 441 | `RequireReversalForce` | `bool` | 16 · Accuracy | Accuracy | **ACTIVE** | `RequireReversalForce` — Unique semantic input retained. |
| 442 | `OppositeSignalCooldownM5` | `int` | 16 · Accuracy | Accuracy | **ACTIVE** | `OppositeSignalCooldownM5` — Unique semantic input retained. |
| 443 | `PreventRapidDirectionFlip` | `bool` | 16 · Accuracy | Accuracy | **ACTIVE** | `PreventRapidDirectionFlip` — Unique semantic input retained. |
| 444 | `RequireM15ReversalForOpposite` | `bool` | 16 · Accuracy | Accuracy | **ACTIVE** | `RequireM15ReversalForOpposite` — Unique semantic input retained. |
| 445 | `AllowOppositeWhileActive` | `bool` | 16 · Accuracy | Accuracy | **ACTIVE** | `AllowOppositeWhileActive` — Unique semantic input retained. |
| 446 | `MinimumOppositeM5Structure` | `int` | 16 · Accuracy | Accuracy | **ACTIVE** | `MinimumOppositeM5Structure` — Unique semantic input retained. |
| 447 | `ExitReentryCooldownM5` | `int` | 16 · Accuracy | Accuracy | **ACTIVE** | `ExitReentryCooldownM5` — Unique semantic input retained. |
| 448 | `UseStructuralSequenceGate` | `bool` | 16 · Accuracy | Accuracy | **ACTIVE** | `UseStructuralSequenceGate` — Unique semantic input retained. |
| 449 | `MinimumStructuralSequence` | `int` | 16 · Accuracy | Accuracy | **ACTIVE** | `MinimumStructuralSequence` — Unique semantic input retained. |
| 450 | `RequireEntryLocationConfluence` | `bool` | 16 · Accuracy | Accuracy | **ACTIVE** | `RequireEntryLocationConfluence` — Unique semantic input retained. |
| 451 | `MinimumEntryLocationQuality` | `int` | 16 · Accuracy | Accuracy | **ACTIVE** | `MinimumEntryLocationQuality` — Unique semantic input retained. |
| 452 | `UseProxyExpectedValueGate` | `bool` | 16 · Accuracy | Accuracy | **ACTIVE** | `UseProxyExpectedValueGate` — Unique semantic input retained. |
| 453 | `MinimumProxyExpectedValue` | `double` | 16 · Accuracy | Accuracy | **ACTIVE** | `MinimumProxyExpectedValue` — Unique semantic input retained. |
| 454 | `EnableSmartDecisionEngine` | `bool` | 17 · Smart Engine | Smart Engine | **ACTIVE** | `EnableSmartDecisionEngine` — Unique semantic input retained. |
| 455 | `SmartMinimumTimeframeAgreement` | `int` | 17 · Smart Engine | Smart Engine | **ACTIVE** | `SmartMinimumTimeframeAgreement` — Unique semantic input retained. |
| 456 | `SmartTargetMinimumRR` | `double` | 17 · Smart Engine | Smart Engine | **ACTIVE** | `SmartTargetMinimumRR` — Unique semantic input retained. |
| 457 | `SmartTargetMaxCandidates` | `int` | 17 · Smart Engine | Smart Engine | **ACTIVE** | `SmartTargetMaxCandidates` — Unique semantic input retained. |
| 458 | `SmartRegimeQualityFloor` | `int` | 17 · Smart Engine | Smart Engine | **ACTIVE** | `SmartRegimeQualityFloor` — Unique semantic input retained. |
| 459 | `NoTradeMinimumSmartQuality` | `int` | 17 · Smart Engine | Smart Engine | **ACTIVE** | `NoTradeMinimumSmartQuality` — Unique semantic input retained. |
| 460 | `BlockCompressionRegime` | `bool` | 17 · Smart Engine | Smart Engine | **ACTIVE** | `BlockCompressionRegime` — Unique semantic input retained. |
| 461 | `BlockWeakRangeTransition` | `bool` | 17 · Smart Engine | Smart Engine | **ACTIVE** | `BlockWeakRangeTransition` — Unique semantic input retained. |
| 462 | `AlertOnLiveReaction` | `bool` | 12 · ALERTS — ADVANCED | Alerts | **ACTIVE** | `AlertOnLiveReaction` — Unique semantic input retained. |
| 463 | `AlertOnSmartDecision` | `bool` | 12 · ALERTS — ADVANCED | Alerts | **ACTIVE** | `AlertOnSmartDecision` — Unique semantic input retained. |
| 464 | `EnableLevelHitAlerts` | `bool` | 12 · ALERTS — ADVANCED | Alerts | **ACTIVE** | `EnableLevelHitAlerts` — Unique semantic input retained. |
| 465 | `AlertOnTp1` | `bool` | 12 · ALERTS — ADVANCED | Alerts | **ACTIVE** | `AlertOnTp1` — Unique semantic input retained. |
| 466 | `AlertOnTp2` | `bool` | 12 · ALERTS — ADVANCED | Alerts | **ACTIVE** | `AlertOnTp2` — Unique semantic input retained. |
| 467 | `AlertOnTp3` | `bool` | 12 · ALERTS — ADVANCED | Alerts | **ACTIVE** | `AlertOnTp3` — Unique semantic input retained. |
| 468 | `AlertOnTp4` | `bool` | 12 · ALERTS — ADVANCED | Alerts | **ACTIVE** | `AlertOnTp4` — Unique semantic input retained. |
| 469 | `AlertOnSl` | `bool` | 12 · ALERTS — ADVANCED | Alerts | **ACTIVE** | `AlertOnSl` — Unique semantic input retained. |
| 470 | `AlertOnFalseSignalRisk` | `bool` | 12 · ALERTS — ADVANCED | Alerts | **ACTIVE** | `AlertOnFalseSignalRisk` — Unique semantic input retained. |
| 471 | `AlertOnEntryRestriction` | `bool` | 12 · ALERTS — ADVANCED | Alerts | **ACTIVE** | `AlertOnEntryRestriction` — Unique semantic input retained. |
| 472 | `AlertOnHighConfidenceEntry` | `bool` | 12 · ALERTS — ADVANCED | Alerts | **ACTIVE** | `AlertOnHighConfidenceEntry` — Unique semantic input retained. |
| 473 | `HighConfidenceThreshold` | `int` | 12 · ALERTS — ADVANCED | Alerts | **ACTIVE** | `HighConfidenceThreshold` — Unique semantic input retained. |
| 474 | `SoundFilePath` | `string` | 12 · ALERTS — ADVANCED | Alerts | **ACTIVE** | `SoundFilePath` — Unique semantic input retained. |
| 475 | `PopupPosition` | `CFIPClean75PanelCorner` | 12 · ALERTS — ADVANCED | Alerts | **ACTIVE** | `PopupPosition` — Unique semantic input retained. |
| 476 | `PopupWidth` | `int` | 12 · ALERTS — ADVANCED | Alerts | **ACTIVE** | `PopupWidth` — Unique semantic input retained. |
| 477 | `KeepPopupUntilNextAlert` | `bool` | 12 · ALERTS — ADVANCED | Alerts | **ACTIVE** | `KeepPopupUntilNextAlert` — Unique semantic input retained. |
| 478 | `ShowPopupCloseButton` | `bool` | 12 · ALERTS — ADVANCED | Alerts | **ACTIVE** | `ShowPopupCloseButton` — Unique semantic input retained. |
| 479 | `PopupBackgroundColor` | `Color` | 12 · ALERTS — ADVANCED | Alerts | **ACTIVE** | `PopupBackgroundColor` — Unique semantic input retained. |
| 480 | `PopupBackgroundAlpha` | `int` | 12 · ALERTS — ADVANCED | Alerts | **ACTIVE** | `PopupBackgroundAlpha` — Unique semantic input retained. |
| 481 | `PopupBorderColor` | `Color` | 12 · ALERTS — ADVANCED | Alerts | **ACTIVE** | `PopupBorderColor` — Unique semantic input retained. |
| 482 | `PopupBorderThickness` | `int` | 12 · ALERTS — ADVANCED | Alerts | **ACTIVE** | `PopupBorderThickness` — Unique semantic input retained. |
| 483 | `PopupCornerRadius` | `int` | 12 · ALERTS — ADVANCED | Alerts | **ACTIVE** | `PopupCornerRadius` — Unique semantic input retained. |
| 484 | `PopupPadding` | `int` | 12 · ALERTS — ADVANCED | Alerts | **ACTIVE** | `PopupPadding` — Unique semantic input retained. |
| 485 | `PopupTextColor` | `Color` | 12 · ALERTS — ADVANCED | Alerts | **ACTIVE** | `PopupTextColor` — Unique semantic input retained. |
| 486 | `LineLengthBars` | `int` | 14 · DISPLAY — ADVANCED | Display | **ACTIVE** | `LineLengthBars` — Unique semantic input retained. |
| 487 | `LineForwardBars` | `int` | 14 · DISPLAY — ADVANCED | Display | **ACTIVE** | `LineForwardBars` — Unique semantic input retained. |
| 488 | `ShowSignalLabels` | `bool` | 14 · DISPLAY — ADVANCED | Display | **ACTIVE** | `ShowSignalLabels` — Unique semantic input retained. |
| 489 | `ShowLevelPriceLabels` | `bool` | 14 · DISPLAY — ADVANCED | Display | **ACTIVE** | `ShowLevelPriceLabels` — Unique semantic input retained. |
| 490 | `ShowContextEventMarker` | `bool` | 14 · DISPLAY — ADVANCED | Display | **ACTIVE** | `ShowContextEventMarker` — Unique semantic input retained. |
| 491 | `LabelLeftOffsetBars` | `int` | 14 · DISPLAY — ADVANCED | Display | **ACTIVE** | `LabelLeftOffsetBars` — Unique semantic input retained. |
| 492 | `ShowPredictionObjects` | `bool` | 14 · DISPLAY — ADVANCED | Display | **ACTIVE** | `ShowPredictionObjects` — Unique semantic input retained. |
| 493 | `ArrowOffsetAtr` | `double` | 14 · DISPLAY — ADVANCED | Display | **ACTIVE** | `ArrowOffsetAtr` — Unique semantic input retained. |
| 494 | `MinimumArrowOffsetPips` | `double` | 14 · DISPLAY — ADVANCED | Display | **ACTIVE** | `MinimumArrowOffsetPips` — Unique semantic input retained. |
| 495 | `ShowEarlyArrow` | `bool` | 14 · DISPLAY — ADVANCED | Display | **ACTIVE** | `ShowEarlyArrow` — Unique semantic input retained. |
| 496 | `ShowPanelToggleButton` | `bool` | 14 · DISPLAY — PANEL | Display | **ACTIVE** | `ShowPanelToggleButton` — Unique semantic input retained. |
| 497 | `PanelToggleWidth` | `int` | 14 · DISPLAY — PANEL | Display | **ACTIVE** | `PanelToggleWidth` — Unique semantic input retained. |
| 498 | `PanelToggleHeight` | `int` | 14 · DISPLAY — PANEL | Display | **ACTIVE** | `PanelToggleHeight` — Unique semantic input retained. |
| 499 | `ActionButtonWidth` | `int` | 13 · AUTO TRADING | Automation | **ACTIVE** | `ActionButtonWidth` — Unique semantic input retained. |
| 500 | `ActionButtonHeight` | `int` | 13 · AUTO TRADING | Automation | **ACTIVE** | `ActionButtonHeight` — Unique semantic input retained. |
| 501 | `AutoProtectBrokerPositions` | `bool` | 13 · AUTO TRADING | Automation | **DEPRECATED** | `AutoBrokerProtection` — Duplicate broker-protection control; canonical strategy-level protection switch is AutoBrokerProtection. |
| 502 | `ManagedPositionLabel` | `string` | 13 · AUTO TRADING | Automation | **ACTIVE** | `ManagedPositionLabel` — Unique semantic input retained. |
| 503 | `SyncBrokerTakeProfit` | `bool` | 13 · AUTO TRADING | Automation | **ACTIVE** | `SyncBrokerTakeProfit` — Unique semantic input retained. |
| 504 | `PreventBrokerTpBackwardMove` | `bool` | 13 · AUTO TRADING | Automation | **ACTIVE** | `PreventBrokerTpBackwardMove` — Unique semantic input retained. |
| 505 | `BrokerModifyCooldownMs` | `int` | 13 · AUTO TRADING | Automation | **ACTIVE** | `BrokerModifyCooldownMs` — Unique semantic input retained. |
| 506 | `EnableAggressiveAutoEntry` | `bool` | 13 · AUTO TRADING | Automation | **ACTIVE** | `EnableAggressiveAutoEntry` — Unique semantic input retained. |
| 507 | `AggressiveMinimumConfidence` | `int` | 13 · AUTO TRADING | Automation | **ACTIVE** | `AggressiveMinimumConfidence` — Unique semantic input retained. |
| 508 | `AggressiveMinimumEvidence` | `int` | 13 · AUTO TRADING | Automation | **ACTIVE** | `AggressiveMinimumEvidence` — Unique semantic input retained. |
| 509 | `AggressiveMinimumSmartQuality` | `int` | 13 · AUTO TRADING | Automation | **ACTIVE** | `AggressiveMinimumSmartQuality` — Unique semantic input retained. |
| 510 | `AggressiveRiskPercentEquity` | `double` | 13 · AUTO TRADING | Automation | **ACTIVE** | `AggressiveRiskPercentEquity` — Unique semantic input retained. |
| 511 | `AggressiveTpStage` | `CFIPClean75TargetStage` | 13 · AUTO TRADING | Automation | **ACTIVE** | `AggressiveTpStage` — Unique semantic input retained. |
| 512 | `AggressiveRequireSmartAgreement` | `bool` | 13 · AUTO TRADING | Automation | **ACTIVE** | `AggressiveRequireSmartAgreement` — Unique semantic input retained. |

## Semantic duplicate review

Only one confirmed duplicate-control pair is deprecated in Phase 2: `AutoProtectBrokerPositions` → `AutoBrokerProtection`. Other similarly named values are intentionally kept distinct when they operate at different layers. Examples include `MinimumConfidence` vs `MinimumAutoConfidence`, `MinimumSmartQuality` vs `MinimumAutoSmartQuality`, `MinimumTimeframeAgreement` vs `SmartMinimumTimeframeAgreement`, and `MinimumEntryQuality` vs `MinimumEntryLocationQuality`.

This distinction is intentional: collapsing thresholds across Decision, Entry, and Execution would create hidden coupling and could make a seemingly strong signal fail or pass for the wrong reason.

## Runtime/configuration separation

`EnableAutoTrading` and `EnableAutomaticOrders` are captured as configured intent. `CFIPClean75RuntimeAuthority` is the separate runtime switch holder. Later UI/control phases may change runtime authority without mutating the public configuration values.

## Manual-entry policy

`ShowTradeActionButtons` is marked REMOVED for the clean product behavior. The clean architecture does not expose manual BUY/SELL/STOP/LIMIT placement. Safety controls such as Close managed Positions and Cancel managed Pending Orders remain a separate concern and are not treated as trade-entry controls.

## Phase 2 acceptance

- all 512 v73 parameters are carried into v75 without silent omission;
- no duplicate public parameter property names;
- every parameter has a disposition;
- one immutable/effectively immutable configuration snapshot captures the complete public parameter surface;
- runtime auto-trading/order switches are separate from configuration;
- confirmed duplicate protection control has one future canonical authority;
- manual trade-entry control is excluded from the clean architecture authority.

## Next phase

**Phase 3 — Time, MTF and data pipeline.**
