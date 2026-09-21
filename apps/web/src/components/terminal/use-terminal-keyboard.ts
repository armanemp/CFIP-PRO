import { useEffect } from "react";
import type { Drawing, Timeframe, Tool } from "./types";

export interface TerminalKeyboardActions {
  closePanels: () => void;
  setTool: (tool: Tool) => void;
  setPendingPoint: (point: Drawing["a"] | null) => void;
  setSelectedDrawingId: (id: string | null) => void;
  undoDrawing: () => void;
  redoDrawing: () => void;
  deleteSelectedDrawing: () => void;
  toggleFullscreen: () => void | Promise<void>;
  resetView: () => void;
  setTimeframe: (timeframe: Timeframe) => void;
  selectedDrawingId: string | null;
}

export function useTerminalKeyboard(actions: TerminalKeyboardActions): void {
  useEffect(() => {
    const onKey = (event: KeyboardEvent) => {
      if (event.target instanceof HTMLInputElement || event.target instanceof HTMLTextAreaElement) return;
      if (event.key === "Escape") {
        actions.closePanels();
        actions.setTool("cursor");
        actions.setPendingPoint(null);
        actions.setSelectedDrawingId(null);
        return;
      }
      if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "z") {
        event.preventDefault();
        if (event.shiftKey) actions.redoDrawing();
        else actions.undoDrawing();
        return;
      }
      if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "y") {
        event.preventDefault();
        actions.redoDrawing();
        return;
      }
      if ((event.key === "Delete" || event.key === "Backspace") && actions.selectedDrawingId) {
        actions.deleteSelectedDrawing();
        return;
      }
      if (event.key === "f" || event.key === "F") {
        void actions.toggleFullscreen();
        return;
      }
      if (event.key === "r" || event.key === "R") {
        actions.resetView();
        return;
      }
      const timeframes: Record<string, Timeframe> = {
        "1": "1m", "2": "5m", "3": "15m", "4": "30m",
        "5": "1H", "6": "4H", "7": "1D", "8": "1W", "9": "1M",
      };
      const timeframe = timeframes[event.key];
      if (timeframe) actions.setTimeframe(timeframe);
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [actions]);
}
