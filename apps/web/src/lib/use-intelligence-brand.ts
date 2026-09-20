"use client";

import { useCallback, useEffect, useState } from "react";
import {
  DEFAULT_INTELLIGENCE_BRAND,
  loadIntelligenceBrand,
  saveIntelligenceBrand,
  type IntelligenceBrandConfig,
} from "@/lib/brand-config";

const EVENT_NAME = "cfip:intelligence-brand-changed";

export function useIntelligenceBrand() {
  const [brand, setBrand] = useState<IntelligenceBrandConfig>(DEFAULT_INTELLIGENCE_BRAND);

  useEffect(() => {
    const sync = () => setBrand(loadIntelligenceBrand());
    sync();
    window.addEventListener(EVENT_NAME, sync);
    window.addEventListener("storage", sync);
    return () => {
      window.removeEventListener(EVENT_NAME, sync);
      window.removeEventListener("storage", sync);
    };
  }, []);

  const updateBrand = useCallback((next: IntelligenceBrandConfig) => {
    const value = {
      name: next.name.trim().slice(0, 64) || DEFAULT_INTELLIGENCE_BRAND.name,
      tagline: next.tagline.trim().slice(0, 120) || DEFAULT_INTELLIGENCE_BRAND.tagline,
    };
    saveIntelligenceBrand(value);
    setBrand(value);
    window.dispatchEvent(new CustomEvent(EVENT_NAME, { detail: value }));
  }, []);

  return { brand, updateBrand };
}
