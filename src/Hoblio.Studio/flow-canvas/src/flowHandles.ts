export const MAIN_SOURCE_HANDLE = "right";

const MAIN_OUT = new Set([
  "right",
  "top",
  "bottom",
  "left",
  "out-top",
  "out-bottom",
  "main",
]);

const CONTROL_PORTS = new Set(["then", "else", "body", "after"]);

export function normalizeSourceHandle(value: string | undefined): string | undefined {
  if (value === "out-top") return "top";
  if (value === "out-bottom") return "bottom";
  return value;
}

export function portFromSourceHandle(kind: string, handle: string | null | undefined): string {
  const h = normalizeSourceHandle(handle ?? undefined) ?? MAIN_SOURCE_HANDLE;
  if (CONTROL_PORTS.has(h)) return h;
  if (MAIN_OUT.has(h)) return "main";
  return "main";
}

export function sourceHandleKey(kind: string, handle: string | null | undefined): string {
  const k = kind.toLowerCase();
  const h = handle ?? "main";
  if (k === "if" || k === "for_each" || k === "while_loop" || k === "repeat") {
    return CONTROL_PORTS.has(h) ? h : "main";
  }
  return "main";
}

export function connectionRejectReason(
  sourceId: string | null | undefined,
  targetId: string | null | undefined,
  sourceKind: string | undefined,
  targetKind: string | undefined,
): string | null {
  if (!sourceId || !targetId) return null;
  if (sourceKind === "end") return "Cannot connect from an End node";
  if (targetKind === "start") return "Cannot connect to Start";
  if (sourceId === targetId) return "Cannot connect a node to itself";
  return null;
}

export function edgeDisplayLabel(port: string): string | undefined {
  return port === "main" ? undefined : port;
}

export function parseEdgeId(id: string): { from: string; port: string; to: string } | null {
  const parts = id.split("|");
  if (parts.length !== 3) return null;
  return { from: parts[0], port: parts[1], to: parts[2] };
}
