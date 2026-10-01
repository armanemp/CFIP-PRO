export type PluginFamily =
  | "chart"
  | "indicator"
  | "analysis"
  | "data"
  | "execution"
  | "backtest"
  | "ai"
  | "alerts"
  | "portfolio"
  | "research";

export type IntegrationMode = "native-adapter" | "plugin" | "reference";

export interface OssModuleDescriptor {
  id: string;
  family: PluginFamily;
  mode: IntegrationMode;
  license: string;
  capabilities: readonly string[];
  status: "planned" | "adapter-ready" | "integrated";
  notes?: string;
}

/**
 * OSS is integrated through stable CFIP contracts. We do not copy vendor-specific
 * UI/state into the terminal. Adapters isolate licensing, update cadence and APIs.
 */
export const OSS_MODULE_CATALOG: readonly OssModuleDescriptor[] = [
  {
    id: "lightweight-charts",
    family: "chart",
    mode: "native-adapter",
    license: "Apache-2.0",
    capabilities: ["candles","bars","line","area","baseline","multi-pane","primitives","markers","screenshot"],
    status: "integrated",
  },
  {
    id: "pairlens-fast-financial-charts",
    family: "chart",
    mode: "reference",
    license: "MIT",
    capabilities: ["90 indicators","42 drawing tools","multi-pane","live tick streaming","AI control surface"],
    status: "planned",
    notes: "Evaluate as a chart-engine benchmark/adapter candidate; do not add until performance, API fit and dependency policy pass.",
  },
  {
    id: "open-terminal-ui-reference",
    family: "research",
    mode: "reference",
    license: "MIT",
    capabilities: ["terminal-shell","screening","portfolio","backtesting","alerts","AI research","plugin architecture"],
    status: "planned",
    notes: "Reference architecture only; CFIP keeps its own contracts and UI.",
  },
  {
    id: "pairlens-reference",
    family: "research",
    mode: "reference",
    license: "FSL/Apache-2 transition",
    capabilities: ["AI-native terminal","WebGL charts","indicators","drawings","workspaces","guarded automation","plugins"],
    status: "planned",
    notes: "Reference only until licensing and feature boundaries are independently reviewed.",
  },
];

export interface CfipPluginContext {
  symbol: string;
  timeframe: string;
  locale: string;
  abortSignal?: AbortSignal;
}

export interface IndicatorPlugin<TParams extends Record<string, unknown> = Record<string, unknown>> {
  readonly id: string;
  readonly version: string;
  readonly parameters: readonly string[];
  calculate(input: { candles: readonly unknown[]; params: TParams }, context: CfipPluginContext): unknown;
}

export interface AnalysisPlugin<TInput = unknown, TResult = unknown> {
  readonly id: string;
  readonly version: string;
  analyze(input: TInput, context: CfipPluginContext): Promise<TResult> | TResult;
}

export interface IntelligencePlugin<TInput = unknown, TResult = unknown> {
  readonly id: string;
  readonly version: string;
  reason(input: TInput, context: CfipPluginContext): Promise<TResult> | TResult;
}

export interface DataProviderPlugin {
  readonly id: string;
  readonly version: string;
  subscribe(request: { symbol: string; timeframe: string }, onEvent: (event: unknown) => void, context: CfipPluginContext): () => void;
}

export interface AlertPlugin<TEvent = unknown> {
  readonly id: string;
  readonly version: string;
  evaluate(event: TEvent, context: CfipPluginContext): Promise<boolean> | boolean;
}
