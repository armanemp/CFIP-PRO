"use client";

import { useEffect, useState } from "react";
import { getHealth, getMarketObservations, type MarketObservation } from "@/lib/api";
import { getConfigDefaults, type ConfigDefaults } from "@/lib/admin-api";

const capabilities = [
  ["Professional terminal","Chart-first workspace with multi-pane indicators, drawings, replay, risk and analysis."],
  ["Intelligence","Evidence-grounded market intelligence with deterministic analysis, provenance and governed learning."],
  ["Research Fabric","OSS discovery, license boundaries, benchmarks and adapter contracts without vendor lock-in."],
  ["Risk & execution","Broker-aware sizing, account rules, margin and execution safeguards behind explicit authorization."],
  ["Realtime data","Normalized observations, provider abstraction, event-driven architecture and quality controls."],
  ["Control plane","Administration, security, subscriptions, observability and platform configuration in one governed surface."],
] as const;

export default function Home() {
  const [health,setHealth]=useState("checking");
  const [market,setMarket]=useState<MarketObservation|null>(null);
  const [config,setConfig]=useState<ConfigDefaults|null>(null);
  useEffect(()=>{
    getHealth().then(()=>setHealth("online")).catch(()=>setHealth("offline"));
    getMarketObservations("EUR/USD","reference",1).then(rows=>setMarket(rows.at(-1)??null)).catch(()=>{});
    getConfigDefaults().then(setConfig).catch(()=>setConfig(null));
  },[]);
  const price = market?.last ?? market?.bid ?? market?.ask;
  const intelligenceName = config?.intelligence_identity.name ?? "Finance Intelligence";
  return <main className="min-h-dvh overflow-hidden bg-[var(--cfip-terminal-bg)] text-[var(--cfip-terminal-text)]">
    <header className="flex h-12 items-center border-b border-[var(--cfip-terminal-border)] bg-[var(--cfip-terminal-surface)]/90 px-5 backdrop-blur">
      <a href="/" className="font-semibold tracking-[0.16em] text-white">CFIP-PRO</a>
      <span className="mx-3 text-[var(--cfip-terminal-border-strong)]">/</span><span className="text-[10px] uppercase tracking-[0.18em] text-[var(--cfip-terminal-text-muted)]">Financial Intelligence Platform</span>
      <nav className="ml-auto flex items-center gap-4 text-xs text-[var(--cfip-terminal-text-muted)]"><a href="/terminal" className="hover:text-white">Terminal</a><a href="/admin" className="hover:text-white">Control Plane</a></nav>
    </header>
    <section className="relative mx-auto max-w-7xl px-5 pb-16 pt-20 md:px-8 md:pt-28">
      <div className="pointer-events-none absolute -right-32 top-0 h-96 w-96 rounded-full bg-[var(--cfip-terminal-accent)]/10 blur-3xl" />
      <div className="max-w-4xl">
        <div className="mb-5 inline-flex rounded-full border border-[var(--cfip-terminal-border-strong)] bg-[var(--cfip-terminal-surface)] px-3 py-1 text-[10px] uppercase tracking-[0.18em] text-[var(--cfip-terminal-text-muted)]">AI-native • modular • governed</div>
        <h1 className="text-5xl font-semibold tracking-[-0.04em] text-white md:text-7xl">Market intelligence,<br/><span className="text-[var(--cfip-terminal-accent)]">built as a workstation.</span></h1>
        <p className="mt-6 max-w-2xl text-base leading-7 text-[var(--cfip-terminal-text-muted)] md:text-lg">CFIP-PRO combines professional charting, deterministic market analysis, broker-aware risk, realtime data and {intelligenceName} intelligence in a modular platform designed for evidence, reproducibility and control.</p>
        <div className="mt-8 flex flex-wrap gap-3">
          <a href="/terminal" className="rounded-lg bg-white px-5 py-3 text-sm font-medium text-[#071018] transition hover:bg-[var(--cfip-terminal-text-muted)]">Open terminal</a>
          <a href="/admin" className="rounded-lg border border-[var(--cfip-terminal-border-strong)] bg-[var(--cfip-terminal-surface)] px-5 py-3 text-sm font-medium text-white hover:bg-[var(--cfip-terminal-surface-active)]">Control plane</a>
        </div>
      </div>
      <div className="mt-14 grid gap-3 md:grid-cols-3">
        <Metric label="API" value={health.toUpperCase()} />
        <Metric label="Reference market" value={price ? Number(price).toFixed(5) : "—"} />
        <Metric label="Architecture" value="MODULAR" />
      </div>
      <div className="mt-10 grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
        {capabilities.map(([title,description])=><article key={title} className="rounded-xl border border-[var(--cfip-terminal-border)] bg-[var(--cfip-terminal-surface)]/80 p-5 hover:border-[var(--cfip-terminal-border-strong)]"><h2 className="text-sm font-semibold text-white">{title === "Intelligence" ? `${intelligenceName} Intelligence` : title}</h2><p className="mt-2 text-xs leading-5 text-[var(--cfip-terminal-text-muted)]">{description}</p></article>)}
      </div>
    </section>
    <footer className="border-t border-[var(--cfip-terminal-border)] px-5 py-5 text-center text-[10px] uppercase tracking-[0.15em] text-[var(--cfip-terminal-text-faint)]">CFIP-PRO • chart-first • evidence-first • governed</footer>
  </main>;
}

function Metric({label,value}:{label:string;value:string}) {
  return <div className="rounded-lg border border-[var(--cfip-terminal-border)] bg-[var(--cfip-terminal-surface)] p-4"><div className="text-[9px] uppercase tracking-[0.16em] text-[var(--cfip-terminal-text-faint)]">{label}</div><div className="mt-1 text-sm font-medium text-white">{value}</div></div>;
}
