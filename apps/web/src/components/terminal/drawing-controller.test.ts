import { describe, expect, it } from "vitest";
import type { UTCTimestamp } from "lightweight-charts";
import { canEditDrawing, createDrawing, deleteDrawing, moveDrawing, selectDrawingAt, updateDrawing } from "./drawing-controller";

const point = (time: number, price: number) => ({ time: time as UTCTimestamp, price });

const drawing = createDrawing({ tool: "trendline", a: point(100, 1.1), b: point(200, 1.2) }, "d1");

describe("drawing controller", () => {
  it("creates stable, editable drawing state", () => {
    expect(drawing).toMatchObject({ id: "d1", visible: true, locked: false });
    expect(canEditDrawing(drawing)).toBe(true);
  });

  it("moves and patches without mutating the source", () => {
    const moved = moveDrawing(drawing, point(110, 1.11), point(210, 1.21));
    const locked = updateDrawing(moved, { locked: true });
    expect(drawing.a.price).toBe(1.1);
    expect(moved.a.price).toBe(1.11);
    expect(canEditDrawing(locked)).toBe(false);
  });

  it("deletes and validates selections", () => {
    expect(deleteDrawing([drawing], "d1")).toEqual([]);
    expect(selectDrawingAt([drawing], "d1")).toBe("d1");
    expect(selectDrawingAt([drawing], "missing")).toBeNull();
  });
});
