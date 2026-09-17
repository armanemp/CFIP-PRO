import type { MarketObservation } from "@/lib/api";
import type { Candle, Timeframe, Zone } from "./types";
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
