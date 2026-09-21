export type AdminSection =
  | "overview" | "platform" | "market-data" | "brokers" | "intelligence"
  | "risk" | "subscriptions" | "security" | "observability" | "research" | "experience";

export interface AdminModule {
  id: AdminSection;
  title: string;
  description: string;
  capabilities: readonly string[];
  maturity: "foundation" | "adapter" | "planned";
}

export const ADMIN_MODULES: readonly AdminModule[] = [
  { id:"overview", title:"Overview", description:"Runtime health, release posture and control-plane status.", capabilities:["runtime","release gates","health"], maturity:"foundation" },
  { id:"platform", title:"Platform", description:"Core application configuration and domain boundaries.", capabilities:["environment","contracts","feature flags","workspaces"], maturity:"foundation" },
  { id:"market-data", title:"Market Data", description:"Provider registry, normalization, quality and freshness.", capabilities:["providers","instruments","sessions","quality","lineage"], maturity:"adapter" },
  { id:"brokers", title:"Brokers & Accounts", description:"Broker adapters, accounts, symbols and execution rules.", capabilities:["accounts","symbol rules","tick values","margin","execution"], maturity:"adapter" },
  { id:"intelligence", title:"Finance Intelligence", description:"Analysis, knowledge, model adapters, provenance and governed promotion.", capabilities:["analysis","knowledge","AI adapters","evaluation","promotion","identity"], maturity:"foundation" },
  { id:"risk", title:"Risk", description:"Position sizing, exposure, limits and execution safeguards.", capabilities:["risk engine","limits","margin","portfolio","kill switch"], maturity:"foundation" },
  { id:"subscriptions", title:"Subscriptions", description:"Entitlements, plans, payment lifecycle and reconciliation.", capabilities:["plans","entitlements","checkout","settlement","renewal","audit"], maturity:"planned" },
  { id:"security", title:"Security", description:"Identity, OAuth, RBAC, secrets boundaries and audit.", capabilities:["OAuth","RBAC","sessions","audit","rate limits"], maturity:"foundation" },
  { id:"observability", title:"Observability", description:"Health, traces, metrics, events and operational diagnostics.", capabilities:["OpenTelemetry","logs","metrics","traces","incidents"], maturity:"foundation" },
  { id:"research", title:"Research Fabric", description:"External research, OSS registry, provenance and adoption gates.", capabilities:["OSS registry","research","license review","benchmarks"], maturity:"foundation" },
  { id:"experience", title:"Experience", description:"Terminal UX, localization, accessibility and product settings.", capabilities:["i18n","RTL","themes","workspaces","shortcuts"], maturity:"foundation" },
];
