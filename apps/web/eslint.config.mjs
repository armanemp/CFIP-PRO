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
    },
  },
);
