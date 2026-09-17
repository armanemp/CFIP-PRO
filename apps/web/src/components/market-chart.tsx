"use client";

import { useEffect, useRef } from "react";
import { CandlestickSeries, ColorType, createChart, type IChartApi } from "lightweight-charts";

export function MarketChart() {
  const containerRef = useRef<HTMLDivElement>(null);
  const chartRef = useRef<IChartApi | null>(null);

  useEffect(() => {
    const container = containerRef.current;
    if (!container) return;

    const chart = createChart(container, {
      autoSize: true,
      layout: { background: { type: ColorType.Solid, color: "#070a0f" }, textColor: "#8f9aaa" },
      grid: { vertLines: { color: "#111823" }, horzLines: { color: "#111823" } },
      rightPriceScale: { borderColor: "#1d2734" },
      timeScale: { borderColor: "#1d2734", timeVisible: true },
    });

    const series = chart.addSeries(CandlestickSeries, {
      upColor: "#22c55e",
      downColor: "#ef4444",
      borderVisible: false,
      wickUpColor: "#22c55e",
      wickDownColor: "#ef4444",
    });

    const now = Math.floor(Date.now() / 1000);
    const data = Array.from({ length: 72 }, (_, index) => {
      const close = 1.09 + Math.sin(index / 7) * 0.008 + index * 0.00008;
      const open = close - Math.sin(index) * 0.0015;
      return {
        time: (now - (72 - index) * 3600) as number,
        open,
        high: Math.max(open, close) + 0.0015,
        low: Math.min(open, close) - 0.0015,
        close,
      };
    });
    series.setData(data);
    chart.timeScale().fitContent();
    chartRef.current = chart;

    const observer = new ResizeObserver(() => chart.resize(container.clientWidth, container.clientHeight));
    observer.observe(container);

    return () => {
      observer.disconnect();
      chart.remove();
      chartRef.current = null;
    };
  }, []);

  return <div ref={containerRef} className="h-full min-h-0 w-full" aria-label="Market chart" />;
}
