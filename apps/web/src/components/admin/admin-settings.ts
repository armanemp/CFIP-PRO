export type AdminValueType = "text" | "number" | "boolean" | "select" | "secret" | "json";

export interface AdminSettingDefinition {
  id: string; section: string; label: string; type: AdminValueType;
  mutable: boolean; sensitive: boolean; description: string; options?: readonly string[];
}

export const ADMIN_SETTINGS: readonly AdminSettingDefinition[] = [
  {id:"platform.app_env",section:"platform",label:"Environment",type:"select",mutable:false,sensitive:false,description:"Deployment environment.",options:["development","staging","production"]},
  {id:"platform.feature_flags",section:"platform",label:"Feature flags",type:"json",mutable:true,sensitive:false,description:"Governed rollout flags; every mutation requires authorization and audit."},
  {id:"platform.event_schema_version",section:"platform",label:"Event schema version",type:"number",mutable:false,sensitive:false,description:"Active cross-domain event contract version."},
  {id:"market.default_provider",section:"market-data",label:"Default market-data provider",type:"select",mutable:true,sensitive:false,description:"Provider selected for normalized observations.",options:["eodhd","twelve-data","finnhub","polygon","alphavantage","dukascopy","truefx","ccxt"]},
  {id:"market.provider_priority",section:"market-data",label:"Provider priority",type:"json",mutable:true,sensitive:false,description:"Ordered failover policy for normalized market data."},
  {id:"market.staleness_seconds",section:"market-data",label:"Staleness threshold",type:"number",mutable:true,sensitive:false,description:"Maximum accepted observation age before data is marked stale."},
  {id:"market.provider_credentials",section:"market-data",label:"Provider credentials",type:"secret",mutable:true,sensitive:true,description:"Secret references only; raw credentials must never be rendered."},
  {id:"broker.enabled",section:"brokers",label:"Enabled brokers",type:"json",mutable:true,sensitive:false,description:"Broker adapter/account enablement."},
  {id:"broker.symbol_rules",section:"brokers",label:"Symbol rules",type:"json",mutable:true,sensitive:false,description:"Tick size/value, contract size, precision and trading constraints."},
  {id:"broker.execution_mode",section:"brokers",label:"Execution mode",type:"select",mutable:true,sensitive:false,description:"Paper, simulation or live adapter mode.",options:["paper","simulation","live"]},
  {id:"risk.max_account_risk",section:"risk",label:"Maximum account risk",type:"number",mutable:true,sensitive:false,description:"Hard risk boundary enforced before execution."},
  {id:"risk.max_open_positions",section:"risk",label:"Maximum open positions",type:"number",mutable:true,sensitive:false,description:"Portfolio-level position-count limit."},
  {id:"risk.kill_switch",section:"risk",label:"Kill switch",type:"boolean",mutable:true,sensitive:false,description:"Emergency execution gate."},
  {id:"intelligence.provider",section:"intelligence",label:"Intelligence provider",type:"select",mutable:true,sensitive:false,description:"Pipvara provider adapter.",options:["deterministic","openai","anthropic","gemini","deepseek","groq","openrouter","ollama","lm-studio"]},
  {id:"intelligence.require_evidence",section:"intelligence",label:"Require evidence",type:"boolean",mutable:true,sensitive:false,description:"Reject intelligence responses without provenance."},
  {id:"intelligence.min_confidence",section:"intelligence",label:"Minimum confidence",type:"number",mutable:true,sensitive:false,description:"Governance threshold for surfaced intelligence."},
  {id:"intelligence.learning_enabled",section:"intelligence",label:"Learning pipeline",type:"boolean",mutable:true,sensitive:false,description:"Enable governed outcome learning and calibration workflows."},
  {id:"intelligence.auto_promotion",section:"intelligence",label:"Auto promotion",type:"boolean",mutable:false,sensitive:false,description:"Reserved for governed low-risk promotion after evaluation and rollback gates."},
  {id:"security.oauth",section:"security",label:"OAuth providers",type:"json",mutable:true,sensitive:false,description:"Identity provider configuration without exposing secrets."},
  {id:"security.rbac",section:"security",label:"RBAC policy",type:"json",mutable:true,sensitive:false,description:"Role and permission policy."},
  {id:"security.session_ttl",section:"security",label:"Session TTL",type:"number",mutable:true,sensitive:false,description:"Authenticated session lifetime."},
  {id:"security.audit_retention",section:"security",label:"Audit retention",type:"number",mutable:true,sensitive:false,description:"Retention policy for immutable control-plane audit events."},
  {id:"observability.otel",section:"observability",label:"OpenTelemetry",type:"boolean",mutable:true,sensitive:false,description:"Tracing and metrics export."},
  {id:"observability.log_level",section:"observability",label:"Log level",type:"select",mutable:true,sensitive:false,description:"Runtime logging threshold.",options:["debug","info","warning","error"]},
  {id:"research.sources",section:"research",label:"Research sources",type:"json",mutable:true,sensitive:false,description:"Approved research sources with provenance and freshness policy."},
  {id:"research.search_providers",section:"research",label:"Research search providers",type:"json",mutable:true,sensitive:false,description:"Configured research/search adapters."},
  {id:"research.oss_policy",section:"research",label:"OSS policy",type:"json",mutable:false,sensitive:false,description:"License, security, maintenance, API-fit and vendor-lock-in gates."},
  {id:"experience.locales",section:"experience",label:"Enabled locales",type:"json",mutable:true,sensitive:false,description:"Enabled UI locales and RTL configuration."},
  {id:"experience.theme",section:"experience",label:"Theme",type:"select",mutable:true,sensitive:false,description:"Terminal visual theme.",options:["dark","light","system"]},
  {id:"experience.chart_defaults",section:"experience",label:"Chart defaults",type:"json",mutable:true,sensitive:false,description:"Chart-first defaults such as timeframe, panes and display preferences."},
  {id:"terminal.alerts",section:"experience",label:"Alert preferences",type:"json",mutable:true,sensitive:false,description:"Alert channels, cooldowns and user notification preferences."},
  {id:"terminal.workspace_layouts",section:"experience",label:"Workspace layouts",type:"json",mutable:true,sensitive:false,description:"Saved terminal layouts, panes, chart synchronization and object visibility."},
  {id:"git.governance",section:"platform",label:"Git governance",type:"json",mutable:false,sensitive:false,description:"Protected paths, approval rules, validation and rollback requirements."},
  {id:"git.proposal_queue",section:"intelligence",label:"AI change proposals",type:"json",mutable:true,sensitive:false,description:"Evidence-backed repository changes awaiting authorization and verification."},
  {id:"research.oss_adapters",section:"research",label:"OSS adapters",type:"json",mutable:true,sensitive:false,description:"Installed and evaluated OSS adapters with license, security and compatibility evidence."},
];

export function settingsForSection(section:string): AdminSettingDefinition[] {
  return ADMIN_SETTINGS.filter(x=>x.section===section);
}
