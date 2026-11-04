import { useEffect, useRef } from "react";
import { postToHost } from "./bridge";

export function useCanvasShortcuts(canEdit: boolean) {
  const canEditRef = useRef(canEdit);
  canEditRef.current = canEdit;

  useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => {
      const target = event.target as HTMLElement | null;
      if (target?.closest("input, textarea, select, [contenteditable='true']")) return;

      const mod = event.ctrlKey || event.metaKey;
      if (mod && event.key.toLowerCase() === "a") {
        event.preventDefault();
        postToHost({ type: "shortcut", payload: { action: "selectAll" } });
        return;
      }
      if (mod && event.key.toLowerCase() === "c") {
        event.preventDefault();
        postToHost({ type: "shortcut", payload: { action: "copy" } });
        return;
      }
      if (mod && event.key.toLowerCase() === "x") {
        if (!canEditRef.current) return;
        event.preventDefault();
        postToHost({ type: "shortcut", payload: { action: "cut" } });
        return;
      }
      if (mod && event.key.toLowerCase() === "v") {
        if (!canEditRef.current) return;
        event.preventDefault();
        postToHost({ type: "shortcut", payload: { action: "paste" } });
      }
    };

    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, []);
}
