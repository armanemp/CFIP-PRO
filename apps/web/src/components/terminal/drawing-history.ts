import type { Drawing } from "./types";

export interface DrawingHistory {
  undo: Drawing[][];
  redo: Drawing[][];
  limit: number;
}

export function createDrawingHistory(limit = 50): DrawingHistory {
  return { undo: [], redo: [], limit: Math.max(1, limit) };
}

function equalDrawings(left: readonly Drawing[], right: readonly Drawing[]): boolean {
  return JSON.stringify(left) === JSON.stringify(right);
}

export function recordDrawingChange(
  history: DrawingHistory,
  before: Drawing[],
  after: Drawing[],
): DrawingHistory {
  if (equalDrawings(before, after)) return history;
  return {
    ...history,
    undo: [...history.undo.slice(-(history.limit - 1)), before],
    redo: [],
  };
}

export function undoDrawingChange(
  history: DrawingHistory,
  current: Drawing[],
): { history: DrawingHistory; drawings: Drawing[] } {
  const previous = history.undo.at(-1);
  if (!previous) return { history, drawings: current };
  return {
    history: {
      ...history,
      undo: history.undo.slice(0, -1),
      redo: [...history.redo, current].slice(-history.limit),
    },
    drawings: previous,
  };
}

export function redoDrawingChange(
  history: DrawingHistory,
  current: Drawing[],
): { history: DrawingHistory; drawings: Drawing[] } {
  const next = history.redo.at(-1);
  if (!next) return { history, drawings: current };
  return {
    history: {
      ...history,
      undo: [...history.undo, current].slice(-history.limit),
      redo: history.redo.slice(0, -1),
    },
    drawings: next,
  };
}
