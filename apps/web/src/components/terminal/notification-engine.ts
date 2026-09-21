export type NotificationChannel = "in-app" | "browser" | "email" | "webhook";
export interface NotificationEvent {
  id:string; kind:"alert"|"system"|"analysis"|"risk"|"execution"; title:string; body:string;
  createdAt:number; severity:"info"|"success"|"warning"|"critical"; read:boolean; channels:readonly NotificationChannel[];
}
export function createNotification(input:Omit<NotificationEvent,"id"|"createdAt"|"read">,now=Date.now()):NotificationEvent {
  return {...input,id:input.kind+"-"+now+"-"+Math.random().toString(36).slice(2,8),createdAt:now,read:false};
}
export function unreadCount(events:readonly NotificationEvent[]):number { return events.reduce((n,e)=>n+(e.read?0:1),0); }
export function markNotificationRead(events:readonly NotificationEvent[],id:string):NotificationEvent[] { return events.map(e=>e.id===id?{...e,read:true}:e); }
export function markAllNotificationsRead(events:readonly NotificationEvent[]):NotificationEvent[] { return events.map(e=>({...e,read:true})); }
