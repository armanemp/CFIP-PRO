export interface IntelligenceBrandConfig {
  name: string;
  tagline: string;
}

export const DEFAULT_INTELLIGENCE_BRAND: IntelligenceBrandConfig = {
  name: process.env.NEXT_PUBLIC_INTELLIGENCE_NAME?.trim() || "Cortex",
  tagline: process.env.NEXT_PUBLIC_INTELLIGENCE_TAGLINE?.trim() || "Financial Market Intelligence",
};

const STORAGE_KEY = "cfip-pro:intelligence-brand:v2";
const LEGACY_STORAGE_KEY = "cfip-pro:intelligence-brand:v1";

export function loadIntelligenceBrand(): IntelligenceBrandConfig {
  if (typeof window === "undefined") return DEFAULT_INTELLIGENCE_BRAND;
  try {
    const raw = window.localStorage.getItem(STORAGE_KEY) ?? window.localStorage.getItem(LEGACY_STORAGE_KEY);
    if (!raw) return DEFAULT_INTELLIGENCE_BRAND;
    const value = JSON.parse(raw) as Partial<IntelligenceBrandConfig>;
    const migratedName = value.name?.trim() === "MarketCortex" || value.name?.trim() === "Elyrava" ? DEFAULT_INTELLIGENCE_BRAND.name : value.name?.trim();
    const result = {
      name: typeof migratedName === "string" && migratedName ? migratedName : DEFAULT_INTELLIGENCE_BRAND.name,
      tagline: typeof value.tagline === "string" && value.tagline.trim() ? value.tagline.trim() : DEFAULT_INTELLIGENCE_BRAND.tagline,
    };
    window.localStorage.setItem(STORAGE_KEY, JSON.stringify(result));
    window.localStorage.removeItem(LEGACY_STORAGE_KEY);
    return result;
  } catch {
    return DEFAULT_INTELLIGENCE_BRAND;
  }
}

export function saveIntelligenceBrand(config: IntelligenceBrandConfig): void {
  if (typeof window === "undefined") return;
  try {
    window.localStorage.setItem(STORAGE_KEY, JSON.stringify({
      name: config.name.trim() || DEFAULT_INTELLIGENCE_BRAND.name,
      tagline: config.tagline.trim() || DEFAULT_INTELLIGENCE_BRAND.tagline,
    }));
  } catch {
    // Keep the active configuration usable if browser storage is unavailable.
  }
}
