export interface FxRiskInput {
  equity: number;
  riskPercent: number;
  entry: number;
  stopLoss: number;
  takeProfit?: number;
  leverage?: number;
  contractSize: number;
  tickSize: number;
  tickValue: number;
  accountToQuoteRate?: number;
}

export interface FxRiskResult {
  valid: boolean;
  reason?: string;
  riskCash: number;
  stopDistancePrice: number;
  stopDistanceTicks: number;
  riskPerUnit: number;
  rawUnits: number;
  leverageCapUnits: number;
  units: number;
  lots: number;
  marginEstimate: number;
  rewardDistancePrice: number;
  riskReward: number;
}

const finitePositive = (value: number) => Number.isFinite(value) && value > 0;

export function calculateFxRisk(input: FxRiskInput): FxRiskResult {
  const {
    equity, riskPercent, entry, stopLoss, takeProfit = 0, leverage = 0,
    contractSize, tickSize, tickValue, accountToQuoteRate = 1,
  } = input;

  const base: FxRiskResult = {
    valid: false, riskCash: 0, stopDistancePrice: 0, stopDistanceTicks: 0,
    riskPerUnit: 0, rawUnits: 0, leverageCapUnits: 0, units: 0, lots: 0,
    marginEstimate: 0, rewardDistancePrice: 0, riskReward: 0,
  };

  if (!finitePositive(equity) || !finitePositive(riskPercent)) return { ...base, reason: "invalid-account-risk" };
  if (!finitePositive(entry) || !finitePositive(stopLoss)) return { ...base, reason: "invalid-prices" };
  if (!finitePositive(contractSize) || !finitePositive(tickSize) || !finitePositive(tickValue)) return { ...base, reason: "missing-broker-symbol-rules" };
  if (!finitePositive(accountToQuoteRate)) return { ...base, reason: "invalid-currency-conversion" };

  const stopDistancePrice = Math.abs(entry - stopLoss);
  const stopDistanceTicks = stopDistancePrice / tickSize;
  // tickValue is assumed to be account-currency value per tick for one standard lot.
  const riskPerLot = stopDistanceTicks * tickValue * accountToQuoteRate;
  const riskCash = equity * riskPercent / 100;
  if (!finitePositive(stopDistanceTicks) || !finitePositive(riskPerLot)) return { ...base, riskCash, stopDistancePrice, stopDistanceTicks, reason: "zero-risk-distance" };

  const rawLots = riskCash / riskPerLot;
  const rawUnits = rawLots * contractSize;
  const leverageCapUnits = leverage > 0 ? (equity * leverage) / entry : Number.POSITIVE_INFINITY;
  const units = Math.max(0, Math.min(rawUnits, leverageCapUnits));
  const lots = units / contractSize;
  const marginEstimate = leverage > 0 ? (units * entry) / leverage : 0;
  const rewardDistancePrice = takeProfit > 0 ? Math.abs(takeProfit - entry) : 0;
  const riskReward = rewardDistancePrice / stopDistancePrice;

  return {
    valid: true, riskCash, stopDistancePrice, stopDistanceTicks,
    riskPerUnit: riskPerLot / contractSize, rawUnits, leverageCapUnits,
    units, lots, marginEstimate, rewardDistancePrice, riskReward,
  };
}
