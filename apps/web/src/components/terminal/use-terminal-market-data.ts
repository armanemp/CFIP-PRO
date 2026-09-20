import { useEffect, useRef, useState } from "react";
import { getMarketObservations, type MarketObservation } from "@/lib/api";
import { TERMINAL_DATA_DEFAULTS } from "./terminal-config";

export interface TerminalMarketState {
  rows: MarketObservation[];
  live: boolean;
  error: boolean;
}

const versionOf = (rows: MarketObservation[]) =>
  `${rows.length}:${rows.at(-1)?.observed_at ?? ""}:${rows.at(-1)?.last ?? ""}`;

export function useTerminalMarketData(symbol: string, initialRows: MarketObservation[]): TerminalMarketState {
  const versionRef = useRef(versionOf(initialRows));
  const [state, setState] = useState<TerminalMarketState>({
    rows: initialRows,
    live: initialRows.length > 0,
    error: false,
  });

  useEffect(() => {
    let active = true;
    const load = async () => {
      try {
        const next = await getMarketObservations(
          symbol,
          TERMINAL_DATA_DEFAULTS.venue,
          TERMINAL_DATA_DEFAULTS.initialObservationLimit,
        );
        if (!active) return;
        const version = versionOf(next);
        if (version !== versionRef.current) {
          versionRef.current = version;
          setState({ rows: next, live: next.length > 0, error: false });
        } else {
          setState(current => ({ ...current, live: next.length > 0, error: false }));
        }
      } catch {
        if (active) setState(current => ({ ...current, live: false, error: true }));
      }
    };

    void load();
    const timer = window.setInterval(load, TERMINAL_DATA_DEFAULTS.refreshMs);
    return () => {
      active = false;
      window.clearInterval(timer);
    };
  }, [symbol]);

  return state;
}
