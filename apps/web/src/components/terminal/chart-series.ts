import type { UTCTimestamp } from "lightweight-charts";

export interface LinePoint {
  time: UTCTimestamp;
  value: number;
}
