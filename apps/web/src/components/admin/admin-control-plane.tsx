"use client";

import { useEffect, useState, type ReactNode } from "react";
import { getAdminRuntime, type AdminRuntime } from "@/lib/admin-api";
import { getProviderCatalog, type ProviderDescriptor } from "@/lib/api";
import { ADMIN_MODULES, type AdminSection } from "./admin-config";
import { settingsForSection } from "./admin-settings";

export function AdminControlPlane() {
  const [section, setSection] = useState<AdminSection>("overview");
  const [runtime, setRuntime] = useState<AdminRuntime | null>(null);
  const [providers, setProviders] = useState<ProviderDescriptor[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    getAdminRuntime().then(setRuntime).catch((e: unknown) => setError(e instanceof Error ? e.message : "Runtime unavailable"));
    getProviderCatalog().then(setProviders).catch(() => setProviders([]));
  }, []);

  const active = ADMIN_MODULES.find(x => x.id === section) ?? ADMIN_MODULES[0];

  return <main className="min-h-dvh bg-[#070a0f] text-[#d8e0ea]">
    <header className="sticky top-0 z-20 flex h-12 items-center border-b border-[#1d2734] bg-[#0b1017]/95 px-4 backdrop-blur">
      <a href="/" className="font-semibold tracking-[0.14em] text-white">CFIP-PRO</a>
      <span className="mx-3 text-[#3d4a5a]">/</span><span className="text-xs text-[#8190a3]">CONTROL PLANE</span>
      <div className="ml-auto text-[10px] uppercase tracking-wider text-[#64748b]">{runtime?.app.environment ?? "loading"}</div>
    </header>
    <div className="mx-auto flex max-w-[1500px] min-h-[calc(100dvh-3rem)]">
      <aside className="w-64 shrink-0 border-r border-[#1d2734] bg-[#0a0f15] p-3">
        <div className="mb-3 px-2 text-[10px] uppercase tracking-[0.18em] text-[#566579]">Control domains</div>
        <nav className="space-y-1">{ADMIN_MODULES.map(item =>
          <button key={item.id} onClick={()=>setSection(item.id)} className={`w-full rounded-md px-3 py-2 text-left text-xs transition ${section===item.id?"bg-[#182536] text-white":"text-[#8391a4] hover:bg-[#101923] hover:text-white"}`}>
            <div className="font-medium">{item.title}</div><div className="mt-0.5 text-[10px] opacity-60">{item.description}</div>
          </button>
        )}</nav>
      </aside>
      <section className="min-w-0 flex-1 p-5 md:p-8">
        <div className="mb-6 flex items-start justify-between gap-4">
          <div><div className="text-[10px] uppercase tracking-[0.2em] text-[#536174]">CFIP control plane</div><h1 className="mt-1 text-2xl font-semibold text-white">{active.title}</h1><p className="mt-1 max-w-3xl text-sm text-[#7d8a9d]">{active.description}</p></div>
          <span className="rounded-full border border-[#273649] px-2.5 py-1 text-[10px] uppercase tracking-wider text-[#8190a3]">{active.maturity}</span>
        </div>
        {section==="overview" && <Overview runtime={runtime} error={error} />}
        {section!=="overview" && <ModuleView module={active} providers={providers} />}
      </section>
    </div>
  </main>;
}

function Overview({ runtime, error }: { runtime: AdminRuntime | null; error: string | null }) {
  const checks = runtime ? [
    ["API runtime", runtime.platform.status === "ready", `${runtime.app.name} ${runtime.app.version}`],
    ["PostgreSQL configured", runtime.dependencies.postgres, "connection boundary present"],
    ["NATS configured", runtime.dependencies.nats, "event boundary present"],
    ["Redis configured", runtime.dependencies.redis, "cache boundary present"],
    ["Secrets exposed", !runtime.security.secrets_exposed, "safe introspection"],
    ["Mutations", false, "authentication/authorization boundary required"],
  ] as const : [];
  return <div className="space-y-5">
    {error && <Card title="Runtime"><div className="text-sm text-amber-300">{error}</div></Card>}
    <div className="grid gap-4 lg:grid-cols-3">
      {checks.map(([label,ok,detail])=><Card key={label} title={label}><div className={`text-xl font-semibold ${ok?"text-emerald-300":"text-amber-300"}`}>{ok?"READY":"GUARDED"}</div><div className="mt-1 text-[11px] text-[#718096]">{detail}</div></Card>)}
      <Card title="Platform startup"><div className="text-xl font-semibold text-white">{runtime ? `${runtime.platform.ready_count}/${runtime.platform.component_count}` : "—"}</div><div className="mt-1 text-[11px] text-[#718096]">Runtime components initialized in one application lifecycle.</div></Card>
      <Card title="Terminal"><div className="text-xl font-semibold text-white">Chart-first</div><div className="mt-1 text-[11px] text-[#718096]">Indicators, analysis, risk and replay are modular domains.</div></Card>
      <Card title="Intelligence"><div className="text-xl font-semibold text-white">Elyrava</div><div className="mt-1 text-[11px] text-[#718096]">Evidence, provenance, evaluation and promotion remain separate concerns.</div></Card>
    </div>
    {runtime && <section><div className="mb-2 text-[10px] uppercase tracking-[0.18em] text-[#566579]">Runtime components</div><div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">{runtime.platform.components.map(component=><Card key={component.name} title={component.name}><div className={`text-sm font-semibold ${component.state==="ready"?"text-emerald-300":component.state==="degraded"?"text-amber-300":"text-[#9aa8ba"}`}>{component.state.toUpperCase()}</div><div className="mt-1 text-[11px] text-[#718096]">{component.detail}</div><div className="mt-3 flex justify-between text-[10px] uppercase tracking-wider text-[#59687b]"><span>{component.required?"required":"optional"}</span><span>{component.duration_ms === null ? "—" : `${component.duration_ms}ms`}</span></div></Card>)}</div></section>}
  </div>;
}

function ModuleView({ module, providers }: { module: (typeof ADMIN_MODULES)[number]; providers: readonly ProviderDescriptor[] }) {
  const settings=settingsForSection(module.id);
  const visibleProviders=providers.filter(p => module.id === "market-data" ? p.kind === "market-data" : module.id === "brokers" ? p.kind === "broker" : module.id === "intelligence" ? p.kind === "ai" : false);
  return <div className="space-y-5">
    <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">{module.capabilities.map(capability =>
      <Card key={capability} title={capability}><div className="text-xs text-[#9aa8ba]">Domain capability boundary.</div><div className="mt-3 h-1 rounded bg-[#17212d]"><div className="h-1 w-1/3 rounded bg-[#42546a]" /></div></Card>
    )}</div>
    {visibleProviders.length > 0 && <section><div className="mb-2 text-[10px] uppercase tracking-[0.18em] text-[#566579]">Provider catalog</div><div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">{visibleProviders.map(provider => <Card key={provider.id} title={provider.name}><div className="text-xs text-[#8492a5]">{provider.capabilities.join(" · ")}</div><div className="mt-3 flex justify-between text-[10px] uppercase tracking-wider"><span className="text-[#59687b]">{provider.status}</span><span className="text-[#7f8da0]">{provider.credential_required ? "credentialed" : "public"}</span></div></Card>)}</div></section>}
    <section>
      <div className="mb-2 text-[10px] uppercase tracking-[0.18em] text-[#566579]">Settings contract</div>
      <div className="grid gap-3 md:grid-cols-2">{settings.map(setting=><Card key={setting.id} title={setting.label}><div className="text-xs text-[#8492a5]">{setting.description}</div><div className="mt-3 flex items-center justify-between text-[10px] uppercase tracking-wider"><span className="text-[#59687b]">{setting.type}{setting.sensitive?" · sensitive":""}</span><span className={setting.mutable?"text-emerald-300":"text-amber-300"}>{setting.mutable?"guarded mutation":"read-only"}</span></div></Card>)}</div>
      {!settings.length && <div className="rounded-lg border border-dashed border-[#293748] p-5 text-xs text-[#66758a]">No mutable setting is exposed for this domain yet.</div>}
    </section>
  </div>;
}

function Card({ title, children }: { title: string; children: ReactNode }) {
  return <article className="rounded-xl border border-[#1d2938] bg-[#0c121a] p-4 shadow-[0_12px_40px_rgba(0,0,0,.16)]"><div className="mb-3 text-[11px] uppercase tracking-[0.12em] text-[#68778b]">{title}</div>{children}</article>;
}
