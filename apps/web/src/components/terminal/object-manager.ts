export interface ManagedObject {
  id: string;
  kind: "drawing" | "indicator" | "analysis";
  name: string;
  visible: boolean;
  locked: boolean;
  order: number;
}

export function toggleObject(objects: readonly ManagedObject[], id: string, field: "visible" | "locked"): ManagedObject[] {
  return objects.map(object => object.id === id ? { ...object, [field]: !object[field] } : object);
}

export function removeObject(objects: readonly ManagedObject[], id: string): ManagedObject[] {
  return objects.filter(object => object.id !== id);
}

export function sortObjects(objects: readonly ManagedObject[]): ManagedObject[] {
  return [...objects].sort((a, b) => a.order - b.order);
}
