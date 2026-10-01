export type IntelligenceDecision="long"|"short"|"wait";
export interface EvidenceItem { id:string; source:string; observedAt?:string; publishedAt?:string; claim:string; relevance:number; provenance:string; }
export interface IntelligenceRequest { symbol:string; timeframe:string; question:string; evidence:readonly EvidenceItem[]; analysis:unknown; }
export interface IntelligenceResponse {
  decision:IntelligenceDecision; confidence:number; rationale:readonly string[]; evidenceIds:readonly string[];
  uncertainty:readonly string[]; model:string; modelVersion:string; generatedAt:string;
}
export interface IntelligenceProvider { id:string; version:string; reason(request:IntelligenceRequest,signal?:AbortSignal):Promise<IntelligenceResponse>; }
