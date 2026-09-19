"use client";

import { useEffect, useState } from "react";
import type { Locale } from "./types";

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
  const [entry, setEntry] = useState(lastPrice ? String(lastPrice) : "");
  const [stop, setStop] = useState("");
  const [target, setTarget] = useState("");

  useEffect(() => {
    if (lastPrice > 0 && !entry) setEntry(String(lastPrice));
  }, [lastPrice, entry]);

  const eq = toNumber(equity);
  const risk = Math.max(toNumber(riskPct), 0);
  const lev = toNumber(leverage);
  const cs = toNumber(contractSize);
  const ep = toNumber(entry);
  const sl = toNumber(stop);
  const tp = toNumber(target);

  const riskCash = eq * risk / 100;
  const stopDistance = Math.abs(ep - sl);
  const rawUnits = stopDistance > 0 ? riskCash / stopDistance : 0;
  const maxUnits = lev > 0 && ep > 0 ? (eq * lev) / ep : 0;
  const units = maxUnits > 0 ? Math.min(rawUnits, maxUnits) : rawUnits;
  const reward = tp > 0 ? Math.abs(tp - ep) : 0;
  const rr = stopDistance > 0 ? reward / stopDistance : 0;
  const valid = eq > 0 && risk > 0 && ep > 0 && sl > 0 && stopDistance > 0 && cs > 0;

  const fields = [
    ["Equity", equity, setEquity],
    ["Risk %", riskPct, setRiskPct],
    ["Leverage", leverage, setLeverage],
    ["Contract size", contractSize, setContractSize],
    ["Entry", entry, setEntry],
    ["Stop loss", stop, setStop],
    ["Take profit", target, setTarget],
  ] as const;

  return <div className="space-y-3">
    <div className="rounded border border-[#263241] bg-[#0a0f16] p-3">
      <div className="text-xs uppercase text-[#64748b]">Risk engine</div>
      <div className="mt-1 text-[10px] leading-4 text-[#64748b]">
        Offline sizing estimate. Live execution must supply broker contract rules, account currency conversion, tick value and margin rules.
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
      <Metric label="Risk cash" value={riskCash.toFixed(2)} />
      <Metric label="Stop distance" value={stopDistance.toFixed(5)} />
      <Metric label="Units" value={valid ? Math.floor(units).toLocaleString() : "—"} suffix={valid ? `${(units / cs).toFixed(2)} lots` : undefined} />
      <Metric label="R:R" value={rr > 0 ? rr.toFixed(2) : "—"} />
    </div>
    <div className="text-[10px] text-[#64748b]">
      Max leverage cap: {maxUnits > 0 ? Math.floor(maxUnits).toLocaleString() : "—"} · {locale === "fa" ? "محاسبه تخمینی" : "Estimate only"}
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
