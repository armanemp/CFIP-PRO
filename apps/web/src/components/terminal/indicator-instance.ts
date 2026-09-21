import { defaultIndicatorParameters, getIndicatorDefinition, normalizeIndicatorParameters, type IndicatorId } from "./indicator-registry";

export type IndicatorLineStyle = "solid" | "dashed" | "dotted";

export interface IndicatorInstance {
  id: string;
  indicatorId: IndicatorId;
  parameters: Record<string, number>;
  visible: boolean;
  locked: boolean;
  pane: "overlay" | "oscillator" | "volume";
  order: number;
  lineWidth: number;
  lineStyle: IndicatorLineStyle;
}

export function createIndicatorInstance(indicatorId: IndicatorId, id = crypto.randomUUID()): IndicatorInstance {
  const definition = getIndicatorDefinition(indicatorId);
  return {
    id,
    indicatorId,
    parameters: defaultIndicatorParameters(indicatorId),
    visible: true,
    locked: false,
    pane: definition?.pane ?? "overlay",
    order: 0,
    lineWidth: definition?.visual?.lineWidth ?? 2,
    lineStyle: definition?.visual?.style ?? "solid",
  };
}

export function normalizeIndicatorInstance(instance: IndicatorInstance): IndicatorInstance {
  return {
    ...instance,
    parameters: normalizeIndicatorParameters(instance.indicatorId, instance.parameters),
    lineWidth: Math.min(6, Math.max(1, Math.round(instance.lineWidth))),
  };
}

export function updateIndicatorParameters(instance: IndicatorInstance, patch: Record<string, number>): IndicatorInstance {
  return normalizeIndicatorInstance({
    ...instance,
    parameters: {...instance.parameters, ...patch},
  });
}

export function cloneIndicatorInstance(instance: IndicatorInstance): IndicatorInstance {
  return {...instance, parameters: {...instance.parameters}};
}
