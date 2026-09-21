import { defineConfig } from "eslint/config";
import nextVitals from "eslint-config-next/core-web-vitals";
import nextTs from "eslint-config-next/typescript";

export default defineConfig(
  [...nextVitals, ...nextTs],
  {
    rules: {
      // Lightweight Charts is an imperative external API. Its lifecycle bridge
      // intentionally synchronizes React state from chart creation/cleanup.
      "react-hooks/set-state-in-effect": "off",
      // The chart overlay bridge currently reads an imperative series ref while
      // rendering. This is isolated to the chart integration until the overlay
      // layer is converted to state-driven series ownership.
      "react-hooks/refs": "off",
    },
  },
);
