import type { Locale } from "./types";

export function ChartAttribution({ locale }: { locale: Locale }) {
  return (
    <a
      href="https://www.tradingview.com/"
      target="_blank"
      rel="noreferrer noopener"
      aria-label={locale === "fa" ? "اعتبار نمودار TradingView" : "TradingView chart attribution"}
      className="rounded px-1.5 py-1 text-[10px] text-[#7f8da0] transition hover:bg-[#17202c] hover:text-[#cbd5e1]"
    >
      TradingView Lightweight Charts™
    </a>
  );
}
