import { defineConfig } from "eslint/config";
import nextVitals from "eslint-config-next/core-web-vitals";
import nextTs from "eslint-config-next/typescript";

export default defineConfig(
  [...nextVitals, ...nextTs],
  {
    rules: {
      "react-hooks/set-state-in-effect": "off",
      "react-hooks/refs": "off",
    },
  },
  {
    files: ["src/app/page.tsx", "src/components/admin/admin-control-plane.tsx"],
    rules: {
      // These two shell surfaces use plain anchors because they are also valid
      // boundaries when the app is served behind a non-Next gateway.
      "@next/next/no-html-link-for-pages": "off",
    },
  },
);
