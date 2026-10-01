import { useEffect } from "react";

export function useOutsideClick(
  refs: Array<React.RefObject<HTMLElement | null>>,
  onOutside: () => void,
  enabled = true,
) {
  useEffect(() => {
    if (!enabled) return;

    const handlePointerDown = (event: PointerEvent) => {
      const target = event.target;
      if (!(target instanceof Node)) return;

      const inside = refs.some((ref) => ref.current?.contains(target));
      if (!inside) onOutside();
    };

    document.addEventListener("pointerdown", handlePointerDown);
    return () => document.removeEventListener("pointerdown", handlePointerDown);
  }, [enabled, onOutside, refs]);
}
