import type { SymbolDefinition } from "./types";

const majors = [
  ["EUR/USD", "EUR", "USD", "Euro / US Dollar", 5], ["GBP/USD", "GBP", "USD", "British Pound / US Dollar", 5],
  ["USD/JPY", "USD", "JPY", "US Dollar / Japanese Yen", 3], ["USD/CHF", "USD", "CHF", "US Dollar / Swiss Franc", 5],
  ["AUD/USD", "AUD", "USD", "Australian Dollar / US Dollar", 5], ["USD/CAD", "USD", "CAD", "US Dollar / Canadian Dollar", 5],
  ["NZD/USD", "NZD", "USD", "New Zealand Dollar / US Dollar", 5],
] as const;
const minors = [
  ["EUR/GBP", "EUR", "GBP", "Euro / British Pound"], ["EUR/JPY", "EUR", "JPY", "Euro / Japanese Yen"], ["EUR/CHF", "EUR", "CHF", "Euro / Swiss Franc"],
  ["EUR/AUD", "EUR", "AUD", "Euro / Australian Dollar"], ["EUR/CAD", "EUR", "CAD", "Euro / Canadian Dollar"], ["EUR/NZD", "EUR", "NZD", "Euro / New Zealand Dollar"],
  ["GBP/JPY", "GBP", "JPY", "British Pound / Japanese Yen"], ["GBP/CHF", "GBP", "CHF", "British Pound / Swiss Franc"], ["GBP/AUD", "GBP", "AUD", "British Pound / Australian Dollar"],
  ["GBP/CAD", "GBP", "CAD", "British Pound / Canadian Dollar"], ["GBP/NZD", "GBP", "NZD", "British Pound / New Zealand Dollar"],
  ["AUD/JPY", "AUD", "JPY", "Australian Dollar / Japanese Yen"], ["AUD/NZD", "AUD", "NZD", "Australian Dollar / New Zealand Dollar"], ["AUD/CAD", "AUD", "CAD", "Australian Dollar / Canadian Dollar"],
  ["NZD/JPY", "NZD", "JPY", "New Zealand Dollar / Japanese Yen"], ["CAD/JPY", "CAD", "JPY", "Canadian Dollar / Japanese Yen"], ["CHF/JPY", "CHF", "JPY", "Swiss Franc / Japanese Yen"],
] as const;
const exotics = [
  ["USD/TRY", "USD", "TRY", "US Dollar / Turkish Lira"], ["USD/ZAR", "USD", "ZAR", "US Dollar / South African Rand"], ["USD/MXN", "USD", "MXN", "US Dollar / Mexican Peso"],
  ["USD/SGD", "USD", "SGD", "US Dollar / Singapore Dollar"], ["USD/HKD", "USD", "HKD", "US Dollar / Hong Kong Dollar"], ["USD/NOK", "USD", "NOK", "US Dollar / Norwegian Krone"],
  ["USD/SEK", "USD", "SEK", "US Dollar / Swedish Krona"], ["USD/DKK", "USD", "DKK", "US Dollar / Danish Krone"], ["USD/PLN", "USD", "PLN", "US Dollar / Polish Zloty"],
  ["USD/HUF", "USD", "HUF", "US Dollar / Hungarian Forint"], ["USD/CNH", "USD", "CNH", "US Dollar / Offshore Yuan"], ["EUR/TRY", "EUR", "TRY", "Euro / Turkish Lira"],
  ["EUR/PLN", "EUR", "PLN", "Euro / Polish Zloty"], ["EUR/NOK", "EUR", "NOK", "Euro / Norwegian Krone"], ["EUR/SEK", "EUR", "SEK", "Euro / Swedish Krona"],
] as const;

function make(rows: readonly (readonly [string, string, string, string, number?])[], category: SymbolDefinition["category"]): SymbolDefinition[] {
  return rows.map(([symbol, base, quote, name, digits]) => ({ symbol, base, quote, name, digits: digits ?? (quote === "JPY" ? 3 : 5), category }));
}

export const forexSymbols = [...make(majors, "majors"), ...make(minors, "minors"), ...make(exotics, "exotics")];
export const defaultWatchlist = ["EUR/USD", "GBP/USD", "USD/JPY", "XAU/USD", "EUR/JPY", "AUD/USD", "USD/CAD", "USD/CHF"];
