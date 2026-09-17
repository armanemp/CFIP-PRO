"use client";

import { useEffect, useRef } from "react";
import {
  ColorType,
  LineSeries,
  createChart,
  type IChartApi,
  type UTCTimestamp,
} from "lightweight-charts";

import type { MarketObservation } from "@/lib/api";

interface MarketChartProps {
  observations: MarketObservation[];
}

export function MarketChart({ observations }: MarketChartProps) {
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

    const series = chart.addSeries(LineSeries, { lineWidth: 2 });
    const points = observations
      .map((observation) => {
        const value = observation.last ?? observation.bid ?? observation.ask;
        if (value === null) return null;
        return {
          time: Math.floor(new Date(observation.observed_at).getTime() / 1000) as UTCTimestamp,
          value: Number(value),
        };
      })
      .filter((point): point is { time: UTCTimestamp; value: number } => point !== null)
      .sort((a, b) => Number(a.time) - Number(b.time));

    series.setData(points);
    if (points.length > 0) chart.timeScale().fitContent();
    chartRef.current = chart;

    const observer = new ResizeObserver(() => chart.resize(container.clientWidth, container.clientHeight));
    observer.observe(container);

    return () => {
      observer.disconnect();
      chart.remove();
      chartRef.current = null;
    };
  }, [observations]);

  return <div ref={containerRef} className="h-full min-h-0 w-full" aria-label="Market observation chart" />;
}
