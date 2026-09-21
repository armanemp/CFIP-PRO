import { ColorType, CrosshairMode } from "lightweight-charts";
import type { ChartPreferences } from "./types";
import { TERMINAL_THEME } from "./terminal-theme";
import { TERMINAL_CHART_LAYOUT } from "./terminal-config";

export function terminalChartOptions(prefs: ChartPreferences) {
  return {
    autoSize: true,
    layout: {
      background: { type: ColorType.Solid, color: TERMINAL_THEME.background },
      textColor: TERMINAL_THEME.chartText,
      fontSize: TERMINAL_CHART_LAYOUT.fontSize,
      fontFamily: TERMINAL_CHART_LAYOUT.fontFamily,
      attributionLogo: true,
    },
    grid: {
      vertLines: { color: prefs.showGrid ? TERMINAL_THEME.grid : "transparent" },
      horzLines: { color: prefs.showGrid ? TERMINAL_THEME.grid : "transparent" },
    },
    rightPriceScale: {
      borderColor: TERMINAL_THEME.borderSubtle,
      autoScale: true,
      alignLabels: true,
      minimumWidth: TERMINAL_CHART_LAYOUT.priceScaleMinWidth,
    },
    timeScale: {
      borderColor: TERMINAL_THEME.borderSubtle,
      timeVisible: true,
      secondsVisible: false,
      rightOffset: TERMINAL_CHART_LAYOUT.rightOffset,
      barSpacing: TERMINAL_CHART_LAYOUT.barSpacing,
      minBarSpacing: TERMINAL_CHART_LAYOUT.minBarSpacing,
      maxBarSpacing: TERMINAL_CHART_LAYOUT.maxBarSpacing,
    },
    crosshair: {
      mode: CrosshairMode.Normal,
      vertLine: { color: TERMINAL_THEME.crosshair, width: 1 as const, style: 3 as const, labelBackgroundColor: TERMINAL_THEME.crosshairLabel },
      horzLine: { color: TERMINAL_THEME.crosshair, width: 1 as const, style: 3 as const, labelBackgroundColor: TERMINAL_THEME.crosshairLabel },
    },
    handleScroll: { mouseWheel: true, pressedMouseMove: true, horzTouchDrag: true, vertTouchDrag: true },
    handleScale: { mouseWheel: true, pinch: true, axisPressedMouseMove: true, axisDoubleClickReset: true },
  };
}
