import type { Drawing, Point, Tool } from "./types";

export interface DrawingDraft {
  tool: Exclude<Tool, "cursor" | "crosshair">;
  a: Point;
  b: Point;
}

export function createDrawingId(now = Date.now(), random = Math.random()): string {
  return `drawing-${now.toString(36)}-${Math.floor(random * 1_000_000).toString(36)}`;
}

export function createDrawing(draft: DrawingDraft, id = createDrawingId()): Drawing {
  return { ...draft, id, visible: true, locked: false };
}

export function moveDrawing(drawing: Drawing, a: Point, b: Point): Drawing {
  return { ...drawing, a, b };
}

export function updateDrawing(drawing: Drawing, patch: Partial<Pick<Drawing, "locked" | "visible">>): Drawing {
  return { ...drawing, ...patch };
}

export function deleteDrawing(drawings: Drawing[], id: string): Drawing[] {
  return drawings.filter(drawing => drawing.id !== id);
}

export function selectDrawingAt(drawings: Drawing[], id: string | null): string | null {
  if (id === null) return null;
  return drawings.some(drawing => drawing.id === id) ? id : null;
}

export function canEditDrawing(drawing: Drawing): boolean {
  return drawing.locked !== true && drawing.visible !== false;
}
