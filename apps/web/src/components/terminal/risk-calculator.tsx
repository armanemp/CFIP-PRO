"use client";

import { useEffect, useState } from "react";
import type { Locale } from "./types";
import { calculateFxRisk } from "./risk-engine";

interface Props {
  locale: Locale;
  lastPrice: number;
}

const toNumber = (value: string) => {
  const n = Number(value);
  return Number.isFinite(n) ? n : 0;
};

export function RiskCalculator({ locale, lastPrice }: Props) {
  const [equity, setEquity] = useState("10000");
  const [riskPct, setRiskPct] = useState("1");
  const [leverage, setLeverage] = useState("30");
  const [contractSize, setContractSize] = useState("100000");
  const [tickSize, setTickSize] = useState("0.00001");
  const [tickValue, setTickValue] = useState("1");
  const [entry, setEntry] = useState(lastPrice ? String(lastPrice) : "");
  const [stop, setStop] = useState("");
  const [target, setTarget] = useState("");

  useEffect(() => {
    if (lastPrice > 0 && !entry) setEntry(String(lastPrice));
  }, [lastPrice, entry]);

  const result = calculateFxRisk({
    equity: toNumber(equity),
    riskPercent: toNumber(riskPct),
    leverage: toNumber(leverage),
    contractSize: toNumber(contractSize),
    tickSize: toNumber(tickSize),
    tickValue: toNumber(tickValue),
    entry: toNumber(entry),
    stopLoss: toNumber(stop),
    takeProfit: toNumber(target),
  });

  const fields = [
    ["Equity", equity, setEquity],
    ["Risk %", riskPct, setRiskPct],
    ["Leverage", leverage, setLeverage],
    ["Contract size", contractSize, setContractSize],
    ["Tick size", tickSize, setTickSize],
    ["Tick value / lot", tickValue, setTickValue],
    ["Entry", entry, setEntry],
    ["Stop loss", stop, setStop],
    ["Take profit", target, setTarget],
  ] as const;

  return <div className="space-y-3">
    <div className="rounded border border-[#263241] bg-[#0a0f16] p-3">
      <div className="text-xs uppercase text-[#64748b]">Risk engine</div>
      <div className="mt-1 text-[10px] leading-4 text-[#64748b]">
        Broker-aware calculation seam: tick size, tick value and account-currency conversion must come from the connected broker/account before live execution.
      </div>
    </div>
    <div className="grid grid-cols-2 gap-2">
      {fields.map(([label, value, setter]) => (
        <label key={label} className="text-[10px] text-[#718096]">
          {label}
          <input value={value} onChange={e => setter(e.target.value)} inputMode="decimal"
            className="mt-1 w-full rounded border border-[#334155] bg-[#0d131b] px-2 py-1.5 text-xs text-white outline-none focus:border-[#64748b]" />
        </label>
      ))}
    </div>
    <div className="grid grid-cols-2 gap-2">
      <Metric label="Risk cash" value={result.riskCash.toFixed(2)} />
      <Metric label="Stop distance / ticks" value={result.stopDistancePrice.toFixed(5)} suffix={result.valid ? result.stopDistanceTicks.toFixed(1) : undefined} />
      <Metric label="Units" value={result.valid ? Math.floor(result.units).toLocaleString() : "—"} suffix={result.valid ? `${result.lots.toFixed(2)} lots` : undefined} />
      <Metric label="R:R" value={result.valid && result.riskReward > 0 ? result.riskReward.toFixed(2) : "—"} />
      <Metric label="Margin estimate" value={result.valid ? result.marginEstimate.toFixed(2) : "—"} />
      <Metric label="Leverage cap" value={Number.isFinite(result.leverageCapUnits) ? Math.floor(result.leverageCapUnits).toLocaleString() : "—"} />
    </div>
    {!result.valid && result.reason && (
      <div className="rounded border border-amber-500/30 bg-amber-500/5 p-2 text-[10px] text-amber-300">
        Broker/symbol data required: {result.reason}
      </div>
    )}
    <div className="text-[10px] text-[#64748b]">
      {locale === "fa" ? "محاسبه تخمینی تا زمان اتصال قوانین واقعی بروکر." : "Estimate until live broker symbol/account rules are connected."}
    </div>
  </div>;
}

function Metric({ label, value, suffix }: { label: string; value: string; suffix?: string }) {
  return <div className="rounded border border-[#263241] bg-[#0a0f16] p-2">
    <div className="text-[10px] text-[#64748b]">{label}</div>
    <div className="mt-1 tabular-nums">{value}</div>
    {suffix && <div className="mt-1 text-[9px] text-[#64748b]">{suffix}</div>}
  </div>;
}
