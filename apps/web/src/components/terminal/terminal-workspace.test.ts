import { describe, expect, it } from "vitest";
import { createReplayState } from "./replay-engine";
import { DEFAULT_PREFERENCES } from "./types";
import { createTerminalWorkspaceState, terminalWorkspaceReducer } from "./terminal-workspace";

describe("terminal workspace", () => {
  it("keeps workspace mutations pure and typed", () => {
    const initial = createTerminalWorkspaceState({ symbol: "EURUSD", preferences: DEFAULT_PREFERENCES, replay: createReplayState(100) });
    const next = terminalWorkspaceReducer(initial, { type: "timeframe/set", timeframe: "5m" });
    const withStudy = terminalWorkspaceReducer(next, { type: "studies/toggle", id: "RSI14" });

    expect(initial.timeframe).toBe("1m");
    expect(next.timeframe).toBe("5m");
    expect(withStudy.selectedStudies).toContain("RSI14");
  });
});
