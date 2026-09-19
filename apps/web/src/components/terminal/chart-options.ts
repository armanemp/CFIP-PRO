import { ColorType, CrosshairMode } from "lightweight-charts";
import type { ChartPreferences } from "./types";
import { TERMINAL_THEME } from "./terminal-theme";

export function terminalChartOptions(prefs: ChartPreferences) {
  return {
    autoSize: true,
    layout: {
      background: { type: ColorType.Solid, color: TERMINAL_THEME.background },
      textColor: TERMINAL_THEME.chartText,
      fontSize: 13,
      fontFamily: "Inter,Segoe UI,Arial,sans-serif",
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
      minimumWidth: 86,
    },
    timeScale: {
      borderColor: TERMINAL_THEME.borderSubtle,
      timeVisible: true,
      secondsVisible: false,
      rightOffset: 10,
      barSpacing: 9,
      minBarSpacing: 2,
      maxBarSpacing: 30,
    },
    crosshair: {
      mode: CrosshairMode.Normal,
      vertLine: { color: TERMINAL_THEME.crosshair, width: 1, style: 3, labelBackgroundColor: TERMINAL_THEME.crosshairLabel },
      horzLine: { color: TERMINAL_THEME.crosshair, width: 1, style: 3, labelBackgroundColor: TERMINAL_THEME.crosshairLabel },
    },
    handleScroll: { mouseWheel: true, pressedMouseMove: true, horzTouchDrag: true, vertTouchDrag: true },
    handleScale: { mouseWheel: true, pinch: true, axisPressedMouseMove: true, axisDoubleClickReset: true },
  };
}
