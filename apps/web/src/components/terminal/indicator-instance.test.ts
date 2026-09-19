import { createIndicatorInstance, updateIndicatorParameters } from "./indicator-instance";

describe("indicator instances", () => {
  it("creates registry-backed defaults", () => {
    const instance = createIndicatorInstance("RSI14", "rsi-1");
    expect(instance.id).toBe("rsi-1");
    expect(instance.parameters.period).toBe(14);
  });

  it("normalizes parameter updates through the registry", () => {
    const instance = createIndicatorInstance("RSI14", "rsi-1");
    const updated = updateIndicatorParameters(instance, { period: 21 });
    expect(updated.parameters.period).toBe(21);
  });
});
