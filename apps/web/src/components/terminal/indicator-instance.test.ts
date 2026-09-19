import { createIndicatorInstance, updateIndicatorParameters } from "./indicator-instance";

describe("indicator instances", () => {
  it("creates registry-backed defaults", () => {
    const instance = createIndicatorInstance("RSI14", "rsi-1");
    expect(instance.id).toBe("rsi-1");
    expect(instance.parameters.period).toBe(14);
    expect(instance.visible).toBe(true);
  });

  it("normalizes instance parameters through registry constraints", () => {
    const instance = createIndicatorInstance("EMA20", "ema-1");
    const updated = updateIndicatorParameters(instance, { period: 9999 });
    expect(updated.parameters.period).toBe(500);
  });
});
