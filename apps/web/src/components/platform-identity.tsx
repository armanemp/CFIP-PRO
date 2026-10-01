"use client";

import { useEffect } from "react";
import { getPlatformIdentity } from "@/lib/identity";

export function PlatformIdentityRuntime() {
  useEffect(() => {
    let active = true;
    const sync = async () => {
      try {
        const identity = await getPlatformIdentity();
        if (!active) return;
        document.title = identity.name;
        document.documentElement.dataset.platformName = identity.name;
        window.dispatchEvent(new CustomEvent("platform-identity-change", { detail: identity }));
      } catch {
        // The terminal remains usable if the control plane is temporarily unavailable.
      }
    };
    void sync();
    const interval = window.setInterval(() => void sync(), 30_000);
    return () => { active = false; window.clearInterval(interval); };
  }, []);
  return null;
}
