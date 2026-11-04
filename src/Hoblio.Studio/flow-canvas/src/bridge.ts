export type CanvasNodeDto = {
  id: string;
  kind: string;
  label: string;
  summary: string;
  x: number;
  y: number;
  isActive: boolean;
};

export type CanvasEdgeDto = {
  id: string;
  from: string;
  to: string;
  port: string;
  sourceHandle: string;
  targetHandle: string;
  label?: string;
  waypoints: { x: number; y: number }[];
};

export type SelectedEdgeDto = {
  id: string;
  sourceId: string;
  targetId: string;
  sourceLabel: string;
  targetLabel: string;
  label: string;
};

export type SetGraphPayload = {
  nodes: CanvasNodeDto[];
  edges: CanvasEdgeDto[];
  canEdit: boolean;
  canUndo?: boolean;
  canRedo?: boolean;
  selectedNodeIds: string[];
  selectedEdgeId: string | null;
  selectedEdge?: SelectedEdgeDto | null;
  fitViewToken?: number;
};

export type HostMessage =
  | { type: "setGraph"; payload: SetGraphPayload }
  | { type: "patchNodes"; payload: { nodes: Pick<CanvasNodeDto, "id" | "label" | "summary" | "isActive">[] } }
  | { type: "patchEdge"; payload: { id: string; waypoints: { x: number; y: number }[] } }
  | { type: "patchToolbar"; payload: { canUndo: boolean; canRedo: boolean } }
  | { type: "flashNode"; payload: { nodeId: string; durationMs?: number } };

export type CanvasMessage =
  | { type: "ready" }
  | { type: "nodesMoved"; payload: { positions: { id: string; x: number; y: number }[] } }
  | {
      type: "connect";
      payload: { from: string; to: string; sourceHandle?: string; targetHandle?: string };
    }
  | { type: "connectEnd"; payload: { ok: boolean; message?: string } }
  | {
      type: "reconnect";
      payload: {
        from: string;
        to: string;
        port: string;
        newFrom: string;
        newTo: string;
        sourceHandle?: string;
        targetHandle?: string;
      };
    }
  | { type: "deleteNodes"; payload: { ids: string[] } }
  | { type: "selection"; payload: { nodeIds: string[]; edgeId: string | null } }
  | { type: "removeEdge"; payload: { from: string; to: string; port: string } }
  | {
      type: "waypoints";
      payload: {
        from: string;
        to: string;
        port: string;
        waypoints: { x: number; y: number }[];
        history?: boolean;
      };
    }
  | {
      type: "shortcut";
      payload: { action: "selectAll" | "copy" | "cut" | "paste" | "delete" | "fitView" };
    }
  | {
      type: "toolbar";
      payload: {
        action:
          | "undo"
          | "redo"
          | "clearFlow"
          | "deleteSelection"
          | "setEditing"
          | "clearEdgeSelection"
          | "setEdgeLabel";
        editing?: boolean;
        edgeId?: string;
        label?: string;
      };
    };

declare global {
  interface Window {
    chrome?: {
      webview?: {
        postMessage: (message: string) => void;
        addEventListener: (type: "message", listener: (event: { data: unknown }) => void) => void;
        removeEventListener: (type: "message", listener: (event: { data: unknown }) => void) => void;
      };
    };
  }
}

export function postToHost(message: CanvasMessage) {
  const json = JSON.stringify(message);
  if (window.chrome?.webview?.postMessage) {
    window.chrome.webview.postMessage(json);
    return;
  }
  console.debug("[flow-canvas]", json);
}

export function subscribeHost(handler: (message: HostMessage) => void) {
  const listener = (event: { data: unknown }) => {
    try {
      const raw = typeof event.data === "string" ? event.data : JSON.stringify(event.data);
      const parsed = JSON.parse(raw) as HostMessage;
      if (parsed?.type) handler(parsed);
    } catch {
      /* ignore malformed host messages */
    }
  };

  window.chrome?.webview?.addEventListener("message", listener);
  return () => window.chrome?.webview?.removeEventListener("message", listener);
}
