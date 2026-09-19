import type { Candle, StudyDefinition } from "./types";

export type IndicatorPane = "overlay" | "oscillator";

export interface IndicatorParameter<T = number> {
  key: string;
  labelKey: string;
  defaultValue: T;
  min?: number;
  max?: number;
  step?: number;
}

export interface IndicatorDefinition extends StudyDefinition {
  pane: IndicatorPane;
  parameters: readonly IndicatorParameter[];
  descriptionKey: string;
}

const p = (key: string, labelKey: string, defaultValue: number, min?: number, max?: number, step?: number): IndicatorParameter => ({
  key, labelKey, defaultValue, min, max, step,
});

export const INDICATOR_REGISTRY = [
  { id: "EMA20", name: "EMA 20", group: "trend", pane: "overlay", parameters: [p("period","period",20,1,500,1)], descriptionKey: "indicatorEma" },
  { id: "EMA50", name: "EMA 50", group: "trend", pane: "overlay", parameters: [p("period","period",50,1,500,1)], descriptionKey: "indicatorEma" },
  { id: "EMA200", name: "EMA 200", group: "trend", pane: "overlay", parameters: [p("period","period",200,1,1000,1)], descriptionKey: "indicatorEma" },
  { id: "SMA20", name: "SMA 20", group: "trend", pane: "overlay", parameters: [p("period","period",20,1,500,1)], descriptionKey: "indicatorSma" },
  { id: "WMA20", name: "WMA 20", group: "trend", pane: "overlay", parameters: [p("period","period",20,1,500,1)], descriptionKey: "indicatorWma" },
  { id: "VWAP", name: "VWAP", group: "volume", pane: "overlay", parameters: [], descriptionKey: "indicatorVwap" },
  { id: "BB20", name: "Bollinger Bands", group: "volatility", pane: "overlay", parameters: [p("period","period",20,1,500,1), p("stdDev","stdDev",2,0.1,10,0.1)], descriptionKey: "indicatorBollinger" },
  { id: "RSI14", name: "RSI 14", group: "momentum", pane: "oscillator", parameters: [p("period","period",14,2,200,1)], descriptionKey: "indicatorRsi" },
  { id: "MACD", name: "MACD", group: "momentum", pane: "oscillator", parameters: [p("fast","fast",12,1,100,1), p("slow","slow",26,2,200,1), p("signal","signal",9,1,100,1)], descriptionKey: "indicatorMacd" },
  { id: "DMI14", name: "DMI / ADX 14", group: "momentum", pane: "oscillator", parameters: [p("period","period",14,2,200,1)], descriptionKey: "indicatorDmi" },
  { id: "STOCH14", name: "Stochastic 14", group: "momentum", pane: "oscillator", parameters: [p("period","period",14,2,200,1), p("smooth","smooth",3,1,50,1)], descriptionKey: "indicatorStochastic" },
  { id: "DONCHIAN20", name: "Donchian 20", group: "volatility", pane: "overlay", parameters: [p("period","period",20,1,500,1)], descriptionKey: "indicatorDonchian" },
  { id: "KELTNER20", name: "Keltner 20", group: "volatility", pane: "overlay", parameters: [p("period","period",20,1,500,1), p("atrPeriod","atrPeriod",14,2,200,1), p("multiplier","multiplier",1.5,0.1,10,0.1)], descriptionKey: "indicatorKeltner" },
  { id: "ICHIMOKU", name: "Ichimoku", group: "trend", pane: "overlay", parameters: [p("conversion","conversion",9,1,100,1), p("base","base",26,1,200,1), p("span","span",52,2,400,1)], descriptionKey: "indicatorIchimoku" },
] as const satisfies readonly IndicatorDefinition[];

export type IndicatorId = (typeof INDICATOR_REGISTRY)[number]["id"];

export const INDICATOR_IDS = INDICATOR_REGISTRY.map(x => x.id) as IndicatorId[];

export function getIndicatorDefinition(id: string): IndicatorDefinition | undefined {
  return INDICATOR_REGISTRY.find(x => x.id === id);
}

export function getIndicatorsByPane(pane: IndicatorPane): IndicatorDefinition[] {
  return INDICATOR_REGISTRY.filter(x => x.pane === pane);
}

export function defaultIndicatorParameters(id: IndicatorId): Record<string, number> {
  const definition = getIndicatorDefinition(id);
  return Object.fromEntries((definition?.parameters ?? []).map(x => [x.key, x.defaultValue]));
}

export function normalizeIndicatorParameters(
  id: IndicatorId,
  values: Record<string, number>,
): Record<string, number> {
  const definition = getIndicatorDefinition(id);
  if (!definition) return {};
  return Object.fromEntries(definition.parameters.map(parameter => {
    const raw = Number(values[parameter.key] ?? parameter.defaultValue);
    const value = Number.isFinite(raw) ? raw : parameter.defaultValue;
    const min = parameter.min ?? value;
    const max = parameter.max ?? value;
    return [parameter.key, Math.min(max, Math.max(min, value))];
  }));
}

export function indicatorCatalog(): readonly IndicatorDefinition[] {
  return INDICATOR_REGISTRY;
}

// Kept as a narrow domain seam for future plugin indicators.
// The chart renderer should consume registry definitions rather than hard-code indicator IDs.
export type IndicatorContext = {
  candles: Candle[];
  parameters: Record<string, number>;
};
