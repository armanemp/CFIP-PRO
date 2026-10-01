import type { Locale } from "./types";
import { en } from "./locales/en";
import { fa } from "./locales/fa";

export const localeNames: Record<Locale, string> = {
  en:"English", fa:"فارسی", de:"Deutsch", ar:"العربية", tr:"Türkçe", fr:"Français",
  es:"Español", pt:"Português", ru:"Русский", zh:"中文", ja:"日本語",
};
export const rtlLocales = new Set<Locale>(["fa","ar"]);
export type TranslationKey = keyof typeof en;

const dictionary: Record<Locale, Partial<Record<TranslationKey, string>>> = {
  en,
  fa,
  de:{}, ar:{}, tr:{}, fr:{}, es:{}, pt:{}, ru:{}, zh:{}, ja:{},
};

export function t(locale: Locale, key: TranslationKey): string {
  return dictionary[locale][key] ?? en[key];
}
