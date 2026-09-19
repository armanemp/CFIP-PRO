"use client";

import { useEffect, useState } from "react";
import { getHealth, getMarketObservations, type MarketObservation } from "@/lib/api";

const capabilities = [
  ["Professional terminal","Chart-first workspace with multi-pane indicators, drawings, replay, risk and analysis."],
  ["Elyrava Intelligence","Evidence-grounded market intelligence with deterministic analysis, provenance and governed learning."],
  ["Research Fabric","OSS discovery, license boundaries, benchmarks and adapter contracts without vendor lock-in."],
  ["Risk & execution","Broker-aware sizing, account rules, margin and execution safeguards behind explicit authorization."],
  ["Realtime data","Normalized observations, provider abstraction, event-driven architecture and quality controls."],
  ["Control plane","Administration, security, subscriptions, observability and platform configuration in one governed surface."],
] as const;

export default function Home() {
  const [health,setHealth]=useState("checking");
  const [market,setMarket]=useState<MarketObservation|null>(null);
  useEffect(()=>{ getHealth().then(()=>setHealth("online")).catch(()=>setHealth("offline")); getMarketObservations("EUR/USD","reference",1).then(rows=>setMarket(rows.at(-1)??null)).catch(()=>{}); },[]);
  const price = market?.last ?? market?.bid ?? market?.ask;
  return <main className="min-h-dvh overflow-hidden bg-[#070a0f] text-[#d8e0ea]">
    <header className="flex h-12 items-center border-b border-[#1d2734] bg-[#0a0f16]/90 px-5 backdrop-blur">
      <a href="/" className="font-semibold tracking-[0.16em] text-white">CFIP-PRO</a>
      <span className="mx-3 text-[#3d4a5a]">/</span><span className="text-[10px] uppercase tracking-[0.18em] text-[#68778b]">Financial Intelligence Platform</span>
      <nav className="ml-auto flex items-center gap-4 text-xs text-[#7f8da0]"><a href="/terminal" className="hover:text-white">Terminal</a><a href="/admin" className="hover:text-white">Control Plane</a></nav>
    </header>
    <section className="relative mx-auto max-w-7xl px-5 pb-16 pt-20 md:px-8 md:pt-28">
      <div className="pointer-events-none absolute -right-32 top-0 h-96 w-96 rounded-full bg-[#17324a]/20 blur-3xl" />
      <div className="max-w-4xl">
        <div className="mb-5 inline-flex rounded-full border border-[#26364a] bg-[#0c141e] px-3 py-1 text-[10px] uppercase tracking-[0.18em] text-[#8a9ab0]">AI-native • modular • governed</div>
        <h1 className="text-5xl font-semibold tracking-[-0.04em] text-white md:text-7xl">Market intelligence,<br/><span className="text-[#8aa9c8]">built as a workstation.</span></h1>
        <p className="mt-6 max-w-2xl text-base leading-7 text-[#8492a5] md:text-lg">CFIP-PRO combines professional charting, deterministic market analysis, broker-aware risk, realtime data and Elyrava intelligence in a modular platform designed for evidence, reproducibility and control.</p>
        <div className="mt-8 flex flex-wrap gap-3">
          <a href="/terminal" className="rounded-lg bg-white px-5 py-3 text-sm font-medium text-[#071018] transition hover:bg-[#d9e5f0]">Open terminal</a>
          <a href="/admin" className="rounded-lg border border-[#314154] bg-[#0d141d] px-5 py-3 text-sm font-medium text-white hover:bg-[#131d28]">Control plane</a>
        </div>
      </div>
      <div className="mt-14 grid gap-3 md:grid-cols-3">
        <Metric label="API" value={health.toUpperCase()} />
        <Metric label="Reference market" value={price ? Number(price).toFixed(5) : "—"} />
        <Metric label="Architecture" value="MODULAR" />
      </div>
      <div className="mt-10 grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
        {capabilities.map(([title,description])=><article key={title} className="rounded-xl border border-[#1b2634] bg-[#0b1118]/80 p-5 hover:border-[#2a3a4d]"><h2 className="text-sm font-semibold text-white">{title}</h2><p className="mt-2 text-xs leading-5 text-[#748297]">{description}</p></article>)}
      </div>
    </section>
    <footer className="border-t border-[#1a2431] px-5 py-5 text-center text-[10px] uppercase tracking-[0.15em] text-[#526073]">CFIP-PRO • chart-first • evidence-first • governed</footer>
  </main>;
}

function Metric({label,value}:{label:string;value:string}) {
  return <div className="rounded-lg border border-[#1b2634] bg-[#0b1118] p-4"><div className="text-[9px] uppercase tracking-[0.16em] text-[#59687b]">{label}</div><div className="mt-1 text-sm font-medium text-white">{value}</div></div>;
}
