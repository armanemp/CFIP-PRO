import type { MarketObservation } from "@/lib/api";
import type { Candle, OrderBlock, StructureEvent, StructurePoint, Timeframe, Zone } from "./types";
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
