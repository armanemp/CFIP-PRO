import type { MarketObservation } from "@/lib/api";
import type { Candle, OrderBlock, StructureEvent, StructurePoint, Timeframe, Zone } from "./types";
import type { Displacement, LiquidityPool, LiquiditySweep, MTFStructureSummary, PremiumDiscountRange } from "./analysis-contracts";
import { timeframeSeconds } from "./types";

export function toCandles(rows: MarketObservation[], tf: Timeframe): Candle[] {
  const out = new Map<number, Candle>();
  for (const row of [...rows].sort((a,b)=>a.observed_at.localeCompare(b.observed_at))) {
    const timestamp = Math.floor(new Date(row.observed_at).getTime()/1000);
    const value = Number(row.last ?? row.bid ?? row.ask);
    if (!Number.isFinite(timestamp) || !Number.isFinite(value)) continue;
    const key = Math.floor(timestamp/timeframeSeconds[tf])*timeframeSeconds[tf];
    const volume = Math.max(0, Number(row.volume ?? 0)); const current = out.get(key);
    if (!current) out.set(key,{time:key as Candle["time"],open:value,high:value,low:value,close:value,volume:Number.isFinite(volume)?volume:0});
    else { current.high=Math.max(current.high,value); current.low=Math.min(current.low,value); current.close=value; if(Number.isFinite(volume)) current.volume+=volume; }
  }
  return [...out.values()];
}
export function sma(c:Candle[], p:number){return c.flatMap((x,i)=>i+1<p?[]:[{time:x.time,value:c.slice(i+1-p,i+1).reduce((s,q)=>s+q.close,0)/p}]);}
export function ema(c:Candle[],p:number){if(c.length<p)return [];let v=c.slice(0,p).reduce((s,x)=>s+x.close,0)/p;const k=2/(p+1);const out=[{time:c[p-1].time,value:v}];for(let i=p;i<c.length;i++){v+=(c[i].close-v)*k;out.push({time:c[i].time,value:v});}return out;}
export function wma(c:Candle[],p:number){const d=p*(p+1)/2;return c.flatMap((x,i)=>i+1<p?[]:[{time:x.time,value:c.slice(i+1-p,i+1).reduce((s,q,j)=>s+q.close*(j+1),0)/d}]);}
export function vwap(c:Candle[]){let pv=0,v=0;return c.map(x=>{const n=x.volume||1;pv+=((x.high+x.low+x.close)/3)*n;v+=n;return{time:x.time,value:pv/v};});}
export function bollinger(c:Candle[],p=20){return c.flatMap((x,i)=>{if(i+1<p)return [];const w=c.slice(i+1-p,i+1).map(q=>q.close),m=w.reduce((s,q)=>s+q,0)/p,sd=Math.sqrt(w.reduce((s,q)=>s+(q-m)**2,0)/p)*2;return[{time:x.time,upper:m+sd,mid:m,lower:m-sd}];});}
export function rsi(c:Candle[],p=14){if(c.length<=p)return[];let g=0,l=0;for(let i=1;i<=p;i++){const d=c[i].close-c[i-1].close;g+=Math.max(d,0);l+=Math.max(-d,0);}let ag=g/p,al=l/p;const out=[{time:c[p].time,value:al?100-100/(1+ag/al):100}];for(let i=p+1;i<c.length;i++){const d=c[i].close-c[i-1].close;ag=(ag*(p-1)+Math.max(d,0))/p;al=(al*(p-1)+Math.max(-d,0))/p;out.push({time:c[i].time,value:al?100-100/(1+ag/al):100});}return out;}
export function atr(c:Candle[],p=14){const tr=c.map((x,i)=>i?Math.max(x.high-x.low,Math.abs(x.high-c[i-1].close),Math.abs(x.low-c[i-1].close)):x.high-x.low);return tr.flatMap((_,i)=>i+1<p?[]:[{time:c[i].time,value:tr.slice(i+1-p,i+1).reduce((s,q)=>s+q,0)/p}]);}
export function obv(c:Candle[]){let v=0;return c.map((x,i)=>{if(i)v+=x.close>c[i-1].close?x.volume:x.close<c[i-1].close?-x.volume:0;return{time:x.time,value:v};});}
export function fvg(c:Candle[]):Zone[]{const z:Zone[]=[];const end=c.at(-1)?.time;if(end===undefined)return z;for(let i=2;i<c.length;i++){if(c[i-2].high<c[i].low)z.push({a:c[i-1].time,b:end,low:c[i-2].high,high:c[i].low,bullish:true});else if(c[i-2].low>c[i].high)z.push({a:c[i-1].time,b:end,low:c[i].high,high:c[i-2].low,bullish:false});}return z.slice(-12);}
export function pivots(c:Candle[]){const p:{time:Candle["time"];price:number;high:boolean}[]=[];for(let i=2;i<c.length-2;i++){if(c[i].high>c[i-1].high&&c[i].high>=c[i+1].high)p.push({time:c[i].time,price:c[i].high,high:true});if(c[i].low<c[i-1].low&&c[i].low<=c[i+1].low)p.push({time:c[i].time,price:c[i].low,high:false});}return p.slice(-20);}

export function macd(c: Candle[], fast=12, slow=26, signal=9) {
  const slowLine = ema(c, slow);
  const fastLine = ema(c, fast);
  const fastByTime = new Map(fastLine.map(x => [x.time, x.value]));
  const macdLine = slowLine.flatMap(x => {
    const f = fastByTime.get(x.time);
    return f === undefined ? [] : [{ time: x.time, value: f - x.value }];
  });
  const signalLine = ema(macdLine.map(x => ({ ...x, open: x.value, high: x.value, low: x.value, close: x.value, volume: 0 })), signal);
  return { macd: macdLine, signal: signalLine, histogram: macdLine.flatMap(x => {
    const s = signalLine.find(q => q.time === x.time)?.value;
    return s === undefined ? [] : [{ time: x.time, value: x.value - s }];
  }) };
}

export function sessionRange(c: Candle[], startHour=7, endHour=16) {
  const selected = c.filter(x => {
    const hour = new Date(Number(x.time) * 1000).getUTCHours();
    return hour >= startHour && hour < endHour;
  });
  if (!selected.length) return null;
  return {
    high: Math.max(...selected.map(x => x.high)),
    low: Math.min(...selected.map(x => x.low)),
    from: selected[0].time,
    to: selected.at(-1)?.time ?? selected[0].time,
  };
}

export function supportResistance(c: Candle[]) {
  const points = pivots(c);
  const buckets = new Map<number, { price: number; touches: number }>();
  for (const point of points) {
    const bucket = Math.round(point.price * 10000) / 10000;
    const current = buckets.get(bucket);
    if (current) current.touches += 1;
    else buckets.set(bucket, { price: point.price, touches: 1 });
  }
  return [...buckets.values()].filter(x => x.touches >= 2).sort((a, b) => b.touches - a.touches).slice(0, 8);
}

export function marketStructure(c: Candle[]) {
  const raw = pivots(c);
  const points: StructurePoint[] = [];
  let lastHigh: number | undefined;
  let lastLow: number | undefined;
  for (const p of raw) {
    if (p.high) {
      const label: StructurePoint["label"] = lastHigh === undefined ? "HH" : p.price > lastHigh ? "HH" : "LH";
      points.push({ ...p, label }); lastHigh = p.price;
    } else {
      const label: StructurePoint["label"] = lastLow === undefined ? "HL" : p.price > lastLow ? "HL" : "LL";
      points.push({ ...p, label }); lastLow = p.price;
    }
  }
  points.sort((a,b)=>Number(a.time)-Number(b.time));
  const events: StructureEvent[] = [];
  let trend: "bullish" | "bearish" | undefined;
  let brokenHigh: number | undefined;
  let brokenLow: number | undefined;
  for (let i=0;i<c.length;i++) {
    const candle=c[i];
    const priorHigh=[...raw].reverse().find(p=>p.high && Number(p.time)<Number(candle.time) && (brokenHigh===undefined || p.price!==brokenHigh));
    const priorLow=[...raw].reverse().find(p=>!p.high && Number(p.time)<Number(candle.time) && (brokenLow===undefined || p.price!==brokenLow));
    if (priorHigh && candle.close>priorHigh.price) { const bullish=true; events.push({time:candle.time,type:trend && trend!=="bullish"?"CHoCH":"BOS",bullish,price:priorHigh.price}); trend="bullish"; brokenHigh=priorHigh.price; }
    if (priorLow && candle.close<priorLow.price) { const bullish=false; events.push({time:candle.time,type:trend && trend!=="bearish"?"CHoCH":"BOS",bullish,price:priorLow.price}); trend="bearish"; brokenLow=priorLow.price; }
  }
  return { points: points.slice(-24), events: events.slice(-12) };
}

export function orderBlocks(c: Candle[]): OrderBlock[] {
  const out: OrderBlock[] = [];
  for (let i=2;i<c.length;i++) {
    const impulse=c[i]; const body=Math.abs(impulse.close-impulse.open); const range=impulse.high-impulse.low;
    if (range<=0 || body/range<0.6) continue;
    const base=c[i-1];
    if (impulse.close>impulse.open && base.close<base.open) out.push({time:base.time,end:c.at(-1)!.time,high:base.high,low:base.low,bullish:true,strength:Math.min(1,body/range)});
    if (impulse.close<impulse.open && base.close>base.open) out.push({time:base.time,end:c.at(-1)!.time,high:base.high,low:base.low,bullish:false,strength:Math.min(1,body/range)});
  }
  return out.slice(-10);
}


export function liquidityAnalysis(c: Candle[], toleranceRatio = 0.00015) {
  const pools: LiquidityPool[] = [];
  const sweeps: LiquiditySweep[] = [];
  const raw = pivots(c);
  const highs = raw.filter(p => p.high);
  const lows = raw.filter(p => !p.high);
  const tolerance = Math.max(Number.EPSILON, (c.at(-1)?.close ?? 1) * toleranceRatio);
  const cluster = (points: typeof highs, kind: LiquidityPool["kind"]) => {
    for (let i = 0; i < points.length; i++) {
      const group = points.filter(p => Math.abs(p.price - points[i].price) <= tolerance);
      if (group.length < 2) continue;
      const unique = new Set(group.map(p => String(p.time)));
      if (unique.size < 2) continue;
      const price = group.reduce((s, p) => s + p.price, 0) / group.length;
      const start = group.reduce((a, p) => Number(p.time) < Number(a.time) ? p : a).time;
      const end = group.reduce((a, p) => Number(p.time) > Number(a.time) ? p : a).time;
      if (!pools.some(x => x.kind === kind && Math.abs(x.price - price) <= tolerance && x.start === start)) {
        pools.push({ kind, price, touches: unique.size, start, end, swept: false });
      }
    }
  };
  cluster(highs, "equal_high");
  cluster(lows, "equal_low");

  for (const pool of pools) {
    const candidates = c.filter(x => Number(x.time) > Number(pool.end));
    for (const candle of candidates) {
      const breach = pool.kind === "equal_high" ? candle.high > pool.price + tolerance : candle.low < pool.price - tolerance;
      const reclaimed = pool.kind === "equal_high" ? candle.close < pool.price : candle.close > pool.price;
      if (breach) {
        sweeps.push({
          time: candle.time,
          price: pool.price,
          kind: pool.kind === "equal_high" ? "buy_side" : "sell_side",
          reclaimed,
          strength: Math.min(1, Math.abs((pool.kind === "equal_high" ? candle.high - pool.price : pool.price - candle.low)) / Math.max(candle.high - candle.low, tolerance)),
        });
        pool.swept = true;
        break;
      }
    }
  }
  return { pools: pools.slice(-12), sweeps: sweeps.slice(-12) };
}

export function displacementAnalysis(c: Candle[], period = 14): Displacement[] {
  const a = atr(c, period);
  const atrByTime = new Map(a.map(x => [x.time, x.value]));
  return c.flatMap((x) => {
    const range = x.high - x.low;
    const atrValue = atrByTime.get(x.time);
    if (!atrValue || range <= 0) return [];
    const bodyRatio = Math.abs(x.close - x.open) / range;
    const atrMultiple = range / Math.max(atrValue, Number.EPSILON);
    if (bodyRatio < 0.65 || atrMultiple < 1.25) return [];
    return [{ time: x.time, bullish: x.close > x.open, bodyRatio, range, atrMultiple, strength: Math.min(1, bodyRatio * 0.55 + Math.min(2, atrMultiple) / 2 * 0.45) }];
  }).slice(-12);
}

export function premiumDiscount(c: Candle[], lookback = 80): PremiumDiscountRange | null {
  const window = c.slice(-lookback);
  if (window.length < 5) return null;
  const highCandle = window.reduce((a, x) => x.high > a.high ? x : a);
  const lowCandle = window.reduce((a, x) => x.low < a.low ? x : a);
  if (highCandle.high <= lowCandle.low) return null;
  const equilibrium = (highCandle.high + lowCandle.low) / 2;
  const current = c.at(-1)!.close;
  const span = highCandle.high - lowCandle.low;
  const zone = current > equilibrium + span * 0.02 ? "premium" : current < equilibrium - span * 0.02 ? "discount" : "equilibrium";
  return { high: highCandle.high, low: lowCandle.low, equilibrium, current, zone, sourceHigh: highCandle.time, sourceLow: lowCandle.time };
}

export function mtfStructure(c: Candle[], currentTf: Timeframe): MTFStructureSummary[] {
  const order: Timeframe[] = ["1m","5m","15m","30m","1H","4H","1D","1W","1M"];
  const index = order.indexOf(currentTf);
  const targets = order.slice(Math.max(0, index - 2), Math.min(order.length, index + 3));
  return targets.map(timeframe => {
    const candles = toCandles(c.map(x => ({ observed_at: new Date(Number(x.time) * 1000).toISOString(), last: x.close, bid: null, ask: null, volume: x.volume } as never)), timeframe);
    const structure = marketStructure(candles);
    const points = structure.points.slice(-6);
    const bull = points.filter(p => p.label === "HH" || p.label === "HL").length;
    const bear = points.filter(p => p.label === "LH" || p.label === "LL").length;
    return {
      timeframe,
      bias: bull > bear ? "bullish" : bear > bull ? "bearish" : "neutral",
      structure: points.length < 2 ? "insufficient" : bull > bear + 1 ? "HH_HL" : bear > bull + 1 ? "LH_LL" : "mixed",
      lastEvent: structure.events.at(-1),
    };
  });
}

export function dmi(c: Candle[], p = 14) {
  if (c.length <= p) return [];
  let trSum = 0, plusSum = 0, minusSum = 0;
  const out: { time: Candle["time"]; plus: number; minus: number; adx: number }[] = [];
  for (let i = 1; i < c.length; i++) {
    const up = c[i].high - c[i - 1].high;
    const down = c[i - 1].low - c[i].low;
    const tr = Math.max(c[i].high - c[i].low, Math.abs(c[i].high - c[i - 1].close), Math.abs(c[i].low - c[i - 1].close));
    trSum += tr; plusSum += up > down && up > 0 ? up : 0; minusSum += down > up && down > 0 ? down : 0;
    if (i >= p) {
      const plus = trSum ? 100 * plusSum / trSum : 0;
      const minus = trSum ? 100 * minusSum / trSum : 0;
      const dx = plus + minus ? 100 * Math.abs(plus - minus) / (plus + minus) : 0;
      out.push({ time: c[i].time, plus, minus, adx: dx });
      const old = i - p;
      const oldUp = c[old + 1].high - c[old].high;
      const oldDown = c[old].low - c[old + 1].low;
      const oldTr = Math.max(c[old + 1].high - c[old + 1].low, Math.abs(c[old + 1].high - c[old].close), Math.abs(c[old + 1].low - c[old].close));
      trSum -= oldTr; plusSum -= oldUp > oldDown && oldUp > 0 ? oldUp : 0; minusSum -= oldDown > oldUp && oldDown > 0 ? oldDown : 0;
    }
  }
  return out;
}

export function stochastic(c: Candle[], p = 14, smooth = 3) {
  const raw = c.flatMap((x, i) => {
    if (i + 1 < p) return [];
    const w = c.slice(i + 1 - p, i + 1);
    const high = Math.max(...w.map(q => q.high)), low = Math.min(...w.map(q => q.low));
    return [{ time: x.time, value: high === low ? 50 : ((x.close - low) / (high - low)) * 100 }];
  });
  return raw.map((x, i) => ({ time: x.time, value: raw.slice(Math.max(0, i + 1 - smooth), i + 1).reduce((s, q) => s + q.value, 0) / Math.min(smooth, i + 1) }));
}

export function donchian(c: Candle[], p = 20) {
  return c.flatMap((x, i) => {
    if (i + 1 < p) return [];
    const w = c.slice(i + 1 - p, i + 1);
    return [{ time: x.time, upper: Math.max(...w.map(q => q.high)), middle: (Math.max(...w.map(q => q.high)) + Math.min(...w.map(q => q.low))) / 2, lower: Math.min(...w.map(q => q.low)) }];
  });
}

export function keltner(c: Candle[], emaPeriod = 20, atrPeriod = 14, multiplier = 1.5) {
  const mid = ema(c, emaPeriod);
  const atrValues = new Map(atr(c, atrPeriod).map(x => [x.time, x.value]));
  return mid.flatMap(x => {
    const a = atrValues.get(x.time);
    return a === undefined ? [] : [{ time: x.time, middle: x.value, upper: x.value + a * multiplier, lower: x.value - a * multiplier }];
  });
}

export function ichimoku(c: Candle[], conversion = 9, base = 26, span = 52) {
  const midpoint = (w: Candle[]) => (Math.max(...w.map(x => x.high)) + Math.min(...w.map(x => x.low))) / 2;
  return c.flatMap((x, i) => {
    if (i + 1 < span) return [];
    const tenkan = midpoint(c.slice(i + 1 - conversion, i + 1));
    const kijun = midpoint(c.slice(i + 1 - base, i + 1));
    const senkouA = (tenkan + kijun) / 2;
    const senkouB = midpoint(c.slice(i + 1 - span, i + 1));
    return [{ time: x.time, tenkan, kijun, senkouA, senkouB }];
  });
}
