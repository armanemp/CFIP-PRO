export type MarketDataQuality="verified"|"delayed"|"stale"|"degraded"|"unknown";
export interface MarketDataTick { provider:string; symbol:string; ts:number; bid?:number; ask?:number; last?:number; volume?:number; sequence?:number; }
export interface MarketDataBar { provider:string; symbol:string; timeframe:string; ts:number; open:number; high:number; low:number; close:number; volume:number; quality:MarketDataQuality; }
export interface MarketDataHealth { provider:string; symbol:string; connected:boolean; latencyMs?:number; lastTickAt?:number; quality:MarketDataQuality; reason?:string; }
export interface MarketDataProvider {
  id:string;
  capabilities:readonly ("ticks"|"bars"|"depth"|"trades"|"fundamentals")[];
  connect():Promise<void>; disconnect():Promise<void>;
  subscribe(symbol:string,timeframe:string,onTick:(tick:MarketDataTick)=>void):()=>void;
}
