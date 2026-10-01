import type { Candle } from "./types";

export type ReplayStatus = "idle" | "paused" | "playing" | "finished";

export interface ReplayState {
  status: ReplayStatus;
  cursor: number;
  start: number;
  end: number;
  speed: number;
}

export const DEFAULT_REPLAY_SPEEDS = [0.25, 0.5, 1, 2, 4, 8, 16] as const;

export function createReplayState(candleCount: number, start = 0, end = Math.max(0, candleCount - 1)): ReplayState {
  const safeEnd = Math.max(start, Math.min(end, Math.max(0, candleCount - 1)));
  return { status: "idle", cursor: Math.max(0, Math.min(start, safeEnd)), start: Math.max(0, start), end: safeEnd, speed: 1 };
}

export function replaySlice(candles: Candle[], state: ReplayState): Candle[] {
  if (!candles.length) return [];
  return candles.slice(0, Math.min(candles.length, state.cursor + 1));
}

export function replayStep(state: ReplayState, delta = 1): ReplayState {
  const cursor = Math.max(state.start, Math.min(state.end, state.cursor + delta));
  return { ...state, cursor, status: cursor >= state.end ? "finished" : "paused" };
}

export function replayPlay(state: ReplayState): ReplayState {
  if (state.cursor >= state.end) return { ...state, status: "finished" };
  return { ...state, status: "playing" };
}

export function replayPause(state: ReplayState): ReplayState {
  return { ...state, status: "paused" };
}

export function replayReset(state: ReplayState): ReplayState {
  return { ...state, cursor: state.start, status: "paused" };
}

export function replaySetSpeed(state: ReplayState, speed: number): ReplayState {
  const next = DEFAULT_REPLAY_SPEEDS.reduce((best, candidate) =>
    Math.abs(candidate - speed) < Math.abs(best - speed) ? candidate : best, DEFAULT_REPLAY_SPEEDS[0]);
  return { ...state, speed: next };
}
