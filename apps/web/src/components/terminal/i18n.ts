import type { Locale } from "./types";

export const localeNames: Record<Locale, string> = { en: "English", fa: "فارسی", de: "Deutsch", ar: "العربية", tr: "Türkçe", fr: "Français", es: "Español", pt: "Português", ru: "Русский", zh: "中文", ja: "日本語" };

export const rtlLocales = new Set<Locale>(["fa", "ar"]);

const en = {
  chart: "Chart", market: "Market", watchlist: "Watchlist", structure: "Structure", intelligence: "Intelligence", risk: "Risk", objects: "Objects",
  indicators: "Indicators", settings: "Settings", language: "Language", symbol: "Symbol", timeframe: "Timeframe", chartType: "Chart type", replay: "Replay", alerts: "Alerts", fullscreen: "Fullscreen",
  reset: "Reset", screenshot: "Screenshot", autoFit: "Auto fit", live: "Live", loading: "Loading market data", noData: "No market observations for this symbol", dataOffline: "Data unavailable", search: "Search symbols", majors: "Majors", minors: "Minors", exotics: "Exotics",
  candlestick: "Candlestick", bars: "Bars", line: "Line", area: "Area", baseline: "Baseline", crosshair: "Crosshair", cursor: "Cursor", trendline: "Trend line", ray: "Ray", horizontal: "Horizontal line", vertical: "Vertical line", rectangle: "Rectangle", fibonacci: "Fibonacci", measure: "Measure", longPosition: "Long position", shortPosition: "Short position",
  marketData: "Normalized market data", bid: "Bid", ask: "Ask", spread: "Spread", open: "Open", high: "High", low: "Low", close: "Close", volume: "Volume", session: "Session", source: "Source", price: "Price", change: "Change",
  hideSidebar: "Hide sidebar", showSidebar: "Show sidebar", hideRail: "Hide tools", showRail: "Show tools", grid: "Grid", sessions: "Sessions", magnet: "Magnet", bidAsk: "Bid / Ask lines", dark: "Dark", light: "Light",
};

export type TranslationKey = keyof typeof en;
const dictionary: Record<Locale, Partial<Record<TranslationKey, string>>> = { en, fa: {}, de: {}, ar: {}, tr: {}, fr: {}, es: {}, pt: {}, ru: {}, zh: {}, ja: {} };

dictionary.fa = { ...en, chart: "نمودار", market: "بازار", watchlist: "دیده‌بان", structure: "ساختار", intelligence: "هوشمندی", risk: "ریسک", objects: "اشیاء", indicators: "اندیکاتورها", settings: "تنظیمات", language: "زبان", symbol: "نماد", timeframe: "تایم‌فریم", chartType: "نوع نمودار", replay: "بازپخش", alerts: "هشدارها", fullscreen: "تمام‌صفحه", reset: "بازنشانی", screenshot: "تصویر نمودار", autoFit: "تنظیم خودکار", live: "زنده", loading: "در حال دریافت داده بازار", noData: "برای این نماد داده بازار وجود ندارد", dataOffline: "داده در دسترس نیست", search: "جستجوی نماد", majors: "اصلی", minors: "فرعی", exotics: "اگزوتیک", candlestick: "کندل‌استیک", bars: "بار", line: "خطی", area: "ناحیه‌ای", baseline: "مبنای صفر", crosshair: "صلیب‌نما", cursor: "نشانگر", trendline: "خط روند", ray: "نیم‌خط", horizontal: "خط افقی", vertical: "خط عمودی", rectangle: "مستطیل", fibonacci: "فیبوناچی", measure: "اندازه‌گیری", longPosition: "پوزیشن خرید", shortPosition: "پوزیشن فروش", marketData: "داده نرمال‌شده بازار", bid: "بید", ask: "اسک", spread: "اسپرد", open: "باز", high: "بیشینه", low: "کمینه", close: "بسته", volume: "حجم", session: "سشن", source: "منبع", price: "قیمت", change: "تغییر", hideSidebar: "مخفی‌کردن سایدبار", showSidebar: "نمایش سایدبار", hideRail: "مخفی‌کردن ابزارها", showRail: "نمایش ابزارها", grid: "گرید", sessions: "سشن‌ها", magnet: "مگنت", bidAsk: "خطوط بید/اسک", dark: "تیره", light: "روشن" };
dictionary.de = { ...en, chart: "Chart", market: "Markt", watchlist: "Beobachtungsliste", structure: "Struktur", intelligence: "Intelligenz", risk: "Risiko", indicators: "Indikatoren", settings: "Einstellungen", language: "Sprache", symbol: "Symbol", timeframe: "Zeitrahmen", chartType: "Charttyp", alerts: "Alarme", fullscreen: "Vollbild", reset: "Zurücksetzen", screenshot: "Screenshot", live: "Live", noData: "Keine Marktdaten für dieses Symbol", search: "Symbole suchen", hideSidebar: "Seitenleiste ausblenden", showSidebar: "Seitenleiste anzeigen" };
dictionary.ar = { ...en, chart: "الرسم البياني", market: "السوق", watchlist: "قائمة المراقبة", structure: "الهيكل", intelligence: "الذكاء", risk: "المخاطر", indicators: "المؤشرات", settings: "الإعدادات", language: "اللغة", symbol: "الرمز", timeframe: "الإطار الزمني", chartType: "نوع الرسم", alerts: "التنبيهات", fullscreen: "ملء الشاشة", reset: "إعادة ضبط", search: "بحث عن الرموز", hideSidebar: "إخفاء الشريط الجانبي", showSidebar: "إظهار الشريط الجانبي" };

export function t(locale: Locale, key: TranslationKey): string { return dictionary[locale][key] ?? en[key]; }
