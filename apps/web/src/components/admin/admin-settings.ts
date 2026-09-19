export type AdminValueType = "text" | "number" | "boolean" | "select" | "secret" | "json";

export interface AdminSettingDefinition {
  id: string;
  section: string;
  label: string;
  type: AdminValueType;
  mutable: boolean;
  sensitive: boolean;
  description: string;
  options?: readonly string[];
}

export const ADMIN_SETTINGS: readonly AdminSettingDefinition[] = [
  {id:"platform.app_env",section:"platform",label:"Environment",type:"select",mutable:false,sensitive:false,description:"Deployment environment.",options:["development","staging","production"]},
  {id:"platform.feature_flags",section:"platform",label:"Feature flags",type:"json",mutable:true,sensitive:false,description:"Governed rollout flags; changes require authenticated control-plane authorization."},
  {id:"market.default_provider",section:"market-data",label:"Default market-data provider",type:"select",mutable:true,sensitive:false,description:"Provider selected for normalized observations.", options:["eodhd","twelve-data","finnhub","polygon","alphavantage","dukascopy","truefx","ccxt"]},
  {id:"market.staleness_seconds",section:"market-data",label:"Staleness threshold",type:"number",mutable:true,sensitive:false,description:"Maximum accepted observation age before data is marked stale."},
  {id:"market.provider_credentials",section:"market-data",label:"Provider credentials",type:"secret",mutable:true,sensitive:true,description:"Secret references only; raw credentials must never be rendered."},
  {id:"broker.symbol_rules",section:"brokers",label:"Symbol rules",type:"json",mutable:true,sensitive:false,description:"Tick size/value, contract size, precision and trading constraints."},
  {id:"broker.execution_mode",section:"brokers",label:"Execution mode",type:"select",mutable:true,sensitive:false,description:"Paper, simulation or live adapter mode.",options:["paper","simulation","live"]},
  {id:"risk.max_account_risk",section:"risk",label:"Maximum account risk",type:"number",mutable:true,sensitive:false,description:"Hard risk boundary enforced before execution."},
  {id:"risk.kill_switch",section:"risk",label:"Kill switch",type:"boolean",mutable:true,sensitive:false,description:"Emergency execution gate."},
  {id:"intelligence.provider",section:"intelligence",label:"Intelligence provider",type:"select",mutable:true,sensitive:false,description:"Elyrava provider adapter.",options:["deterministic","local","remote","ollama","lm-studio","openai-compatible"]},
  {id:"intelligence.require_evidence",section:"intelligence",label:"Require evidence",type:"boolean",mutable:true,sensitive:false,description:"Reject intelligence responses without provenance."},
  {id:"intelligence.auto_promotion",section:"intelligence",label:"Auto promotion",type:"boolean",mutable:false,sensitive:false,description:"Reserved for governed low-risk promotion after evaluation and rollback gates."},
  {id:"security.oauth",section:"security",label:"OAuth providers",type:"json",mutable:true,sensitive:false,description:"Identity provider configuration without exposing secrets."},
  {id:"security.rbac",section:"security",label:"RBAC policy",type:"json",mutable:true,sensitive:false,description:"Role and permission policy."},
  {id:"security.session_ttl",section:"security",label:"Session TTL",type:"number",mutable:true,sensitive:false,description:"Authenticated session lifetime."},
  {id:"observability.otel",section:"observability",label:"OpenTelemetry",type:"boolean",mutable:true,sensitive:false,description:"Tracing and metrics export."},
  {id:"research.sources",section:"research",label:"Research sources",type:"json",mutable:true,sensitive:false,description:"Approved research sources with provenance and freshness policy."},
  {id:"research.oss_policy",section:"research",label:"OSS policy",type:"json",mutable:false,sensitive:false,description:"License, security, maintenance and vendor-lock-in gates."},
  {id:"experience.locales",section:"experience",label:"Enabled locales",type:"json",mutable:true,sensitive:false,description:"Enabled UI locales and RTL configuration."},
  {id:"experience.theme",section:"experience",label:"Theme",type:"select",mutable:true,sensitive:false,description:"Terminal visual theme.",options:["dark","light","system"]},
];

export function settingsForSection(section:string): AdminSettingDefinition[] {
  return ADMIN_SETTINGS.filter(x=>x.section===section);
}
