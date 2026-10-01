export type AlertCondition =
  | { kind:"price-cross"; price:number; direction:"above"|"below" }
  | { kind:"indicator-threshold"; indicator:string; value:number; direction:"above"|"below" }
  | { kind:"analysis-recommendation"; recommendation:"long"|"short"|"wait" };

export interface AlertRule {
  id:string;
  symbol:string;
  timeframe:string;
  name:string;
  enabled:boolean;
  condition:AlertCondition;
}

export interface AlertEvent {
  ruleId:string;
  firedAt:number;
  message:string;
}

export function evaluateAlert(rule: AlertRule, context: { price:number; indicators:Record<string,number>; recommendation?: "long"|"short"|"wait" }, now=Date.now()): AlertEvent | null {
  if (!rule.enabled || !Number.isFinite(context.price)) return null;
  const c=rule.condition;
  if(c.kind==="price-cross" && ((c.direction==="above" && context.price>=c.price)||(c.direction==="below" && context.price<=c.price))) return {ruleId:rule.id,firedAt:now,message:`${rule.name}: price ${c.direction} ${c.price}`};
  if(c.kind==="indicator-threshold"){const value=context.indicators[c.indicator];if(!Number.isFinite(value))return null;if((c.direction==="above"&&value>=c.value)||(c.direction==="below"&&value<=c.value))return{ruleId:rule.id,firedAt:now,message:`${rule.name}: ${c.indicator} ${c.direction} ${c.value}`};}
  if(c.kind==="analysis-recommendation" && context.recommendation===c.recommendation)return{ruleId:rule.id,firedAt:now,message:`${rule.name}: analysis recommendation ${c.recommendation}`};
  return null;
}
