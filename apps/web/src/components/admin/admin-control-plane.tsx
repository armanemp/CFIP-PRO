"use client";

import { useEffect, useState, type ReactNode } from "react";
import { getAdminRuntime, getConfigDefaults, type AdminRuntime, type ConfigDefaults } from "@/lib/admin-api";
import { getProviderCatalog, type ProviderDescriptor } from "@/lib/api";
import { ADMIN_MODULES, type AdminSection } from "./admin-config";
import { settingsForSection } from "./admin-settings";

export function AdminControlPlane() {
  const [section, setSection] = useState<AdminSection>("overview");
  const [runtime, setRuntime] = useState<AdminRuntime | null>(null);
  const [config, setConfig] = useState<ConfigDefaults | null>(null);
  const [providers, setProviders] = useState<ProviderDescriptor[]>([]);
  const [error, setError] = useState<string | null>(null);
  useEffect(() => {
    getAdminRuntime().then(setRuntime).catch((e: unknown) => setError(e instanceof Error ? e.message : "Runtime unavailable"));
    getConfigDefaults().then(setConfig).catch(() => setConfig(null));
    getProviderCatalog().then(setProviders).catch(() => setProviders([]));
  }, []);
  const active = ADMIN_MODULES.find(x => x.id === section) ?? ADMIN_MODULES[0];
  return <main className="min-h-dvh bg-[var(--cfip-terminal-bg)] text-[var(--cfip-terminal-text)]">
    <header className="sticky top-0 z-20 flex h-12 items-center border-b border-[var(--cfip-terminal-border)] bg-[var(--cfip-terminal-surface)]/95 px-4 backdrop-blur">
      <a href="/" className="font-semibold tracking-[0.14em] text-white">CFIP-PRO</a><span className="mx-3 text-[var(--cfip-terminal-border-strong)]">/</span><span className="text-xs text-[var(--cfip-terminal-text-muted)]">CONTROL PLANE</span>
      <div className="ml-auto flex items-center gap-3 text-[10px] uppercase tracking-wider text-[var(--cfip-terminal-text-faint)]"><span>{config?.intelligence_identity.short_name ?? "INT"}</span><span>{runtime?.app.environment ?? "loading"}</span></div>
    </header>
    <div className="mx-auto flex max-w-[1500px] min-h-[calc(100dvh-3rem)]">
      <aside className="w-64 shrink-0 border-r border-[var(--cfip-terminal-border)] bg-[var(--cfip-terminal-surface)] p-3">
        <div className="mb-3 px-2 text-[10px] uppercase tracking-[0.18em] text-[var(--cfip-terminal-text-faint)]">Control domains</div>
        <nav className="space-y-1">{ADMIN_MODULES.map(item => <button key={item.id} onClick={()=>setSection(item.id)} className={`w-full rounded-md px-3 py-2 text-left text-xs transition ${section===item.id?"bg-[var(--cfip-terminal-surface-active)] text-white":"text-[var(--cfip-terminal-text-muted)] hover:bg-[var(--cfip-terminal-surface-active)] hover:text-white"}`}><div className="font-medium">{item.id === "intelligence" && config ? config.intelligence_identity.name : item.title}</div><div className="mt-0.5 text-[10px] opacity-60">{item.description}</div></button>)}</nav>
      </aside>
      <section className="min-w-0 flex-1 p-5 md:p-8">
        <div className="mb-6 flex items-start justify-between gap-4"><div><div className="text-[10px] uppercase tracking-[0.2em] text-[var(--cfip-terminal-text-faint)]">CFIP control plane</div><h1 className="mt-1 text-2xl font-semibold text-white">{active.id === "intelligence" && config ? config.intelligence_identity.name : active.title}</h1><p className="mt-1 max-w-3xl text-sm text-[var(--cfip-terminal-text-muted)]">{active.description}</p></div><span className="rounded-full border border-[var(--cfip-terminal-border-strong)] px-2.5 py-1 text-[10px] uppercase tracking-wider text-[var(--cfip-terminal-text-muted)]">{active.maturity}</span></div>
        {section==="overview" ? <Overview runtime={runtime} config={config} error={error} /> : <ModuleView module={active} providers={providers} config={config} />}
      </section>
    </div>
  </main>;
}

function Overview({ runtime, config, error }: { runtime: AdminRuntime | null; config: ConfigDefaults | null; error: string | null }) {
  const checks = runtime ? [["API runtime", runtime.platform.status === "ready", `${runtime.app.name} ${runtime.app.version}`],["PostgreSQL configured", runtime.dependencies.postgres, "connection boundary present"],["NATS configured", runtime.dependencies.nats, "event boundary present"],["Redis configured", runtime.dependencies.redis, "cache boundary present"],["Secrets exposed", !runtime.security.secrets_exposed, "safe introspection"],["Mutations", false, "authentication/authorization boundary required"]] as const : [];
  return <div className="space-y-5">
    {error && <Card title="Runtime"><div className="text-sm text-[var(--cfip-terminal-warning)]">{error}</div></Card>}
    <div className="grid gap-4 lg:grid-cols-3">{checks.map(([label,ok,detail])=><Card key={label} title={label}><div className={`text-xl font-semibold ${ok?"text-[var(--cfip-terminal-success)]":"text-[var(--cfip-terminal-warning)]"}`}>{ok?"READY":"GUARDED"}</div><div className="mt-1 text-[11px] text-[var(--cfip-terminal-text-muted)]">{detail}</div></Card>)}
      <Card title="Platform startup"><div className="text-xl font-semibold text-white">{runtime ? `${runtime.platform.ready_count}/${runtime.platform.component_count}` : "—"}</div><div className="mt-1 text-[11px] text-[var(--cfip-terminal-text-muted)]">Runtime components initialized in one application lifecycle.</div></Card>
      <Card title="Terminal"><div className="text-xl font-semibold text-white">Chart-first</div><div className="mt-1 text-[11px] text-[var(--cfip-terminal-text-muted)]">Indicators, analysis, risk and replay are modular domains.</div></Card>
      <Card title="Intelligence"><div className="text-xl font-semibold text-white">{config?.intelligence_identity.name ?? "—"}</div><div className="mt-1 text-[11px] text-[var(--cfip-terminal-text-muted)]">{config?.intelligence_identity.description ?? "Configurable evidence-grounded finance intelligence."}</div></Card>
    </div>
    {runtime && <section><div className="mb-2 text-[10px] uppercase tracking-[0.18em] text-[var(--cfip-terminal-text-faint)]">Runtime components</div><div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">{runtime.platform.components.map(component=><Card key={component.name} title={component.name}><div className={`text-sm font-semibold ${component.state==="ready"?"text-[var(--cfip-terminal-success)]":component.state==="degraded"?"text-[var(--cfip-terminal-warning)]":"text-[var(--cfip-terminal-text-muted)]"}`}>{component.state.toUpperCase()}</div><div className="mt-1 text-[11px] text-[var(--cfip-terminal-text-muted)]">{component.detail}</div><div className="mt-3 flex justify-between text-[10px] uppercase tracking-wider text-[var(--cfip-terminal-text-faint)]"><span>{component.required?"required":"optional"}</span><span>{component.duration_ms === null ? "—" : `${component.duration_ms}ms`}</span></div></Card>)}</div></section>}
  </div>;
}

function ModuleView({ module, providers, config }: { module: (typeof ADMIN_MODULES)[number]; providers: readonly ProviderDescriptor[]; config: ConfigDefaults | null }) {
  const settings=settingsForSection(module.id);
  const visibleProviders=providers.filter(p => module.id === "market-data" ? p.kind === "market-data" : module.id === "brokers" ? p.kind === "broker" : module.id === "intelligence" ? p.kind === "ai" : false);
  return <div className="space-y-5">
    {module.id === "intelligence" && config && <Card title="Configured identity"><div className="text-xl font-semibold text-white">{config.intelligence_identity.name}</div><div className="mt-1 text-xs text-[var(--cfip-terminal-text-muted)]">{config.intelligence_identity.domain} · {config.intelligence_identity.short_name}</div><div className="mt-2 text-xs text-[var(--cfip-terminal-text-muted)]">{config.intelligence_identity.description}</div><div className="mt-3 text-[10px] uppercase tracking-wider text-[var(--cfip-terminal-text-faint)]">Name changes are configuration, not executable behavior.</div></Card>}
    <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">{module.capabilities.map(capability=><Card key={capability} title={capability}><div className="text-xs text-[var(--cfip-terminal-text-muted)]">Domain capability boundary.</div><div className="mt-3 h-1 rounded bg-[var(--cfip-terminal-surface-active)]"><div className="h-1 w-1/3 rounded bg-[var(--cfip-terminal-border-strong)]" /></div></Card>)}</div>
    {visibleProviders.length > 0 && <section><div className="mb-2 text-[10px] uppercase tracking-[0.18em] text-[var(--cfip-terminal-text-faint)]">Provider catalog</div><div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">{visibleProviders.map(provider=><Card key={provider.id} title={provider.name}><div className="text-xs text-[var(--cfip-terminal-text-muted)]">{provider.capabilities.join(" · ")}</div><div className="mt-3 flex justify-between text-[10px] uppercase tracking-wider"><span className="text-[var(--cfip-terminal-text-faint)]">{provider.status}</span><span className="text-[var(--cfip-terminal-text-muted)]">{provider.credential_required ? "credentialed" : "public"}</span></div></Card>)}</div></section>}
    <section><div className="mb-2 text-[10px] uppercase tracking-[0.18em] text-[var(--cfip-terminal-text-faint)]">Settings contract</div><div className="grid gap-3 md:grid-cols-2">{settings.map(setting=><Card key={setting.id} title={setting.label}><div className="text-xs text-[var(--cfip-terminal-text-muted)]">{setting.description}</div><div className="mt-3 flex items-center justify-between text-[10px] uppercase tracking-wider"><span className="text-[var(--cfip-terminal-text-faint)]">{setting.type}{setting.sensitive?" · sensitive":""}</span><span className={setting.mutable?"text-[var(--cfip-terminal-success)]":"text-[var(--cfip-terminal-warning)]"}>{setting.mutable?"guarded mutation":"read-only"}</span></div></Card>)}</div>{!settings.length && <div className="rounded-lg border border-dashed border-[var(--cfip-terminal-border-strong)] p-5 text-xs text-[var(--cfip-terminal-text-faint)]">No mutable setting is exposed for this domain yet.</div>}</section>
  </div>;
}

function Card({ title, children }: { title: string; children: ReactNode }) { return <article className="rounded-xl border border-[var(--cfip-terminal-border)] bg-[var(--cfip-terminal-surface)] p-4 shadow-[0_12px_40px_rgba(0,0,0,.16)]"><div className="mb-3 text-[11px] uppercase tracking-[0.12em] text-[var(--cfip-terminal-text-faint)]">{title}</div>{children}</article>; }
