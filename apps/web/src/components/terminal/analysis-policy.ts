export const ANALYSIS_POLICY = Object.freeze({
  neutralScoreThreshold: 0.18,
  confluenceThreshold: 84,
  minimumPassedGates: 3,
  maximumEvidenceItems: 8,
  maximumTrendPoints: 6,
} as const);

export type AnalysisPolicy = typeof ANALYSIS_POLICY;
