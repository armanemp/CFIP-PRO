import type { Drawing, Point } from "./types";

export interface DrawingHandle {
  id: "a" | "b" | "entry" | "stop" | "target";
  point: Point;
  role: "anchor" | "entry" | "stop" | "target";
}
export interface DrawingGeometry {
  points: readonly Point[];
  levels?: readonly { id: string; price: number; label: string }[];
}
export function drawingHandles(drawing: Drawing): DrawingHandle[] {
  if (drawing.locked) return [];
  if (drawing.tool === "long" || drawing.tool === "short") {
    const direction = drawing.tool === "long" ? 1 : -1;
    const risk = Math.abs(drawing.b.price - drawing.a.price);
    const target = drawing.a.price + direction * risk * 2;
    return [
      { id:"entry", point:drawing.a, role:"entry" },
      { id:"stop", point:drawing.b, role:"stop" },
      { id:"target", point:{time:drawing.b.time,price:target}, role:"target" },
    ];
  }
  return [{id:"a",point:drawing.a,role:"anchor"},{id:"b",point:drawing.b,role:"anchor"}];
}
export function moveDrawing(drawing: Drawing, handle: DrawingHandle["id"], point: Point): Drawing {
  if (drawing.locked) return drawing;
  if (handle === "a" || handle === "entry") return {...drawing,a:point};
  if (handle === "b" || handle === "stop") return {...drawing,b:point};
  return drawing;
}
export function translateDrawing(drawing: Drawing, delta: Point): Drawing {
  if (drawing.locked) return drawing;
  return {...drawing,
    a:{time:(Number(drawing.a.time)+Number(delta.time)) as typeof drawing.a.time,price:drawing.a.price+delta.price},
    b:{time:(Number(drawing.b.time)+Number(delta.time)) as typeof drawing.b.time,price:drawing.b.price+delta.price}};
}
export function geometryForDrawing(drawing: Drawing): DrawingGeometry {
  if (drawing.tool === "fib") {
    const distance=drawing.b.price-drawing.a.price;
    const ratios=[0,0.236,0.382,0.5,0.618,0.786,1];
    return {points:[drawing.a,drawing.b],levels:ratios.map(ratio=>({id:String(ratio),price:drawing.a.price+distance*ratio,label:String(Math.round(ratio*1000)/10)+"%"}))};
  }
  if (drawing.tool === "long" || drawing.tool === "short") {
    const direction=drawing.tool==="long"?1:-1;
    const risk=Math.abs(drawing.b.price-drawing.a.price);
    return {points:[drawing.a,drawing.b,{...drawing.b,price:drawing.a.price+direction*risk*2}],
      levels:[{id:"entry",price:drawing.a.price,label:"Entry"},{id:"stop",price:drawing.b.price,label:"SL"},{id:"target",price:drawing.a.price+direction*risk*2,label:"TP"}]};
  }
  return {points:[drawing.a,drawing.b]};
}
export function cloneDrawingState(drawings: readonly Drawing[]): Drawing[] {
  return drawings.map(d=>({...d,a:{...d.a},b:{...d.b}}));
}
