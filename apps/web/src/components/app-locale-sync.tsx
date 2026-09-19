"use client";

import { useEffect } from "react";
import type { Locale } from "@/components/terminal/types";

const STORAGE_KEY = "cfip-pro:app-locale:v1";
const supported: Locale[] = ["en","fa","de","ar","tr","fr","es","pt","ru","zh","ja"];

function normalize(value: string | null | undefined): Locale {
  const code = (value ?? "").toLowerCase().split("-")[0] as Locale;
  return supported.includes(code) ? code : "en";
}

export function detectOsLocale(): Locale {
  if (typeof navigator === "undefined") return "en";
  const stored = window.localStorage.getItem(STORAGE_KEY);
  return normalize(stored ?? navigator.language);
}

export function AppLocaleSync() {
  useEffect(() => {
    const apply = (locale: Locale) => {
      document.documentElement.lang = locale;
      document.documentElement.dir = locale === "fa" || locale === "ar" ? "rtl" : "ltr";
      try { window.localStorage.setItem(STORAGE_KEY, locale); } catch {}
      window.dispatchEvent(new CustomEvent("cfip:app-locale", { detail: locale }));
    };
    apply(detectOsLocale());
    const onLocale = (event: Event) => apply(normalize((event as CustomEvent<string>).detail));
    window.addEventListener("cfip:set-app-locale", onLocale);
    return () => window.removeEventListener("cfip:set-app-locale", onLocale);
  }, []);
  return null;
}
