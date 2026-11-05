import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import {
  ReactFlow,
  Background,
  BackgroundVariant,
  Controls,
  MiniMap,
  MarkerType,
  applyEdgeChanges,
  applyNodeChanges,
  reconnectEdge,
  useReactFlow,
  useUpdateNodeInternals,
  type Connection,
  type Edge,
  type EdgeChange,
  type FinalConnectionState,
  type Node,
  type NodeChange,
  type OnBeforeDelete,
  type OnNodeDrag,
  type OnSelectionChangeParams,
} from "@xyflow/react";
import "@xyflow/react/dist/style.css";
import {
  postToHost,
  subscribeHost,
  type CanvasNodeDto,
  type HostMessage,
  type SetGraphPayload,
} from "./bridge";
import { FlowNode, type FlowNodeData } from "./nodes/FlowNode";
import { WaypointEdge } from "./edges/WaypointEdge";
import { useCanvasShortcuts } from "./useCanvasShortcuts";
import { computeSnap, guidesEqual, type AlignmentGuide } from "./flowAlignment";
import { FlowAlignmentGuides } from "./FlowAlignmentGuides";
import { FlowToolbar } from "./FlowToolbar";
import type { SelectedEdgeDto } from "./bridge";
import { connectionRejectReason, edgeDisplayLabel, normalizeSourceHandle, portFromSourceHandle } from "./flowHandles";
import { HOBLIO_FALLBACK_HEIGHT, HOBLIO_FALLBACK_WIDTH } from "./nodeLayout";

const nodeTypes = { flowNode: FlowNode };
const edgeTypes = { waypoint: WaypointEdge };
const FALLBACK_W = HOBLIO_FALLBACK_WIDTH;
const FALLBACK_H = HOBLIO_FALLBACK_HEIGHT;

const markerEnd = { type: MarkerType.ArrowClosed, width: 18, height: 18 };

function toFlowNodes(dtos: CanvasNodeDto[]): Node<FlowNodeData>[] {
  return dtos.map((n) => ({
    id: n.id,
    type: "flowNode",
    position: { x: n.x, y: n.y },
    data: {
      kind: n.kind,
      label: n.label,
      summary: n.summary,
      isActive: n.isActive,
      isFlashing: false,
    },
    selected: false,
  }));
}

function toFlowEdges(payload: SetGraphPayload, canEdit: boolean): Edge[] {
  return payload.edges.map((e) => ({
    id: e.id,
    source: e.from,
    target: e.to,
    sourceHandle: e.sourceHandle,
    targetHandle: e.targetHandle,
    type: "waypoint",
    label: e.label ?? edgeDisplayLabel(e.port),
    markerEnd,
    data: { waypoints: e.waypoints, canEdit },
    selected: payload.selectedEdgeId === e.id,
  }));
}

function applySelection(nodes: Node[], edges: Edge[], nodeIds: string[], edgeId: string | null) {
  const nodeSet = new Set(nodeIds);
  return {
    nodes: nodes.map((n) => ({ ...n, selected: nodeSet.has(n.id) })),
    edges: edges.map((e) => ({ ...e, selected: edgeId !== null && e.id === edgeId })),
  };
}

export function App() {
  const { fitView, setCenter, getNode } = useReactFlow();
  const updateNodeInternals = useUpdateNodeInternals();
  const [nodes, setNodes] = useState<Node<FlowNodeData>[]>([]);
  const [edges, setEdges] = useState<Edge[]>([]);
  const [canEdit, setCanEdit] = useState(true);
  const [canUndo, setCanUndo] = useState(false);
  const [canRedo, setCanRedo] = useState(false);
  const [selectedEdge, setSelectedEdge] = useState<SelectedEdgeDto | null>(null);
  const [guides, setGuides] = useState<AlignmentGuide[]>([]);
  const applyingRemote = useRef(false);
  const lastFitToken = useRef(0);
  const canEditRef = useRef(canEdit);
  const guidesRef = useRef<AlignmentGuide[]>([]);
  const nodesRef = useRef(nodes);
  const flashTimers = useRef<Map<string, number>>(new Map());
  nodesRef.current = nodes;
  canEditRef.current = canEdit;

  const publishGuides = useCallback((next: AlignmentGuide[]) => {
    if (guidesEqual(guidesRef.current, next)) return;
    guidesRef.current = next;
    setGuides(next);
  }, []);

  useCanvasShortcuts(canEdit);

  const onHostMessage = useCallback(
    (message: HostMessage) => {
      if (message.type === "setGraph") {
        applyingRemote.current = true;
        try {
          const baseNodes = toFlowNodes(message.payload.nodes);
          const baseEdges = toFlowEdges(message.payload, message.payload.canEdit);
          const { nodes: selNodes, edges: selEdges } = applySelection(
            baseNodes,
            baseEdges,
            message.payload.selectedNodeIds,
            message.payload.selectedEdgeId,
          );
          setCanEdit(message.payload.canEdit);
          setCanUndo(message.payload.canUndo ?? false);
          setCanRedo(message.payload.canRedo ?? false);
          setSelectedEdge(message.payload.selectedEdge ?? null);
          setNodes(selNodes);
          setEdges(selEdges);
          publishGuides([]);
          requestAnimationFrame(() => {
            for (const n of message.payload.nodes) updateNodeInternals(n.id);
          });
          if (
            message.payload.fitViewToken &&
            message.payload.fitViewToken > lastFitToken.current
          ) {
            lastFitToken.current = message.payload.fitViewToken;
            requestAnimationFrame(() => {
              void fitView({ padding: 0.12, minZoom: 0.15, maxZoom: 1, duration: 200 });
            });
          }
        } finally {
          applyingRemote.current = false;
        }
        return;
      }

      if (message.type === "patchNodes") {
        applyingRemote.current = true;
        try {
          const patch = new Map(message.payload.nodes.map((n) => [n.id, n]));
          setNodes((prev) =>
            prev.map((node) => {
              const p = patch.get(node.id);
              if (!p) return node;
              return {
                ...node,
                data: {
                  ...node.data,
                  label: p.label,
                  summary: p.summary,
                  isActive: p.isActive,
                  isFlashing: node.data.isFlashing ?? false,
                },
              };
            }),
          );
        } finally {
          applyingRemote.current = false;
        }
        return;
      }

      if (message.type === "flashNode") {
        const { nodeId, durationMs = 2600 } = message.payload;
        applyingRemote.current = true;
        try {
          setNodes((prev) =>
            prev.map((node) =>
              node.id === nodeId
                ? { ...node, selected: true, data: { ...node.data, isFlashing: true } }
                : node,
            ),
          );
          const target = getNode(nodeId) ?? nodesRef.current.find((n) => n.id === nodeId);
          if (target) {
            const cx = target.position.x + FALLBACK_W / 2;
            const cy = target.position.y + FALLBACK_H / 2;
            void setCenter(cx, cy, { duration: 280 });
          }
          const prior = flashTimers.current.get(nodeId);
          if (prior !== undefined) window.clearTimeout(prior);
          const timer = window.setTimeout(() => {
            setNodes((prev) =>
              prev.map((node) =>
                node.id === nodeId
                  ? { ...node, data: { ...node.data, isFlashing: false } }
                  : node,
              ),
            );
            flashTimers.current.delete(nodeId);
          }, durationMs);
          flashTimers.current.set(nodeId, timer);
        } finally {
          applyingRemote.current = false;
        }
        return;
      }

      if (message.type === "patchToolbar") {
        setCanUndo(message.payload.canUndo);
        setCanRedo(message.payload.canRedo);
        return;
      }

      if (message.type === "patchEdge") {
        applyingRemote.current = true;
        try {
          setEdges((prev) =>
            prev.map((e) =>
              e.id === message.payload.id
                ? { ...e, data: { ...(e.data as object), waypoints: message.payload.waypoints } }
                : e,
            ),
          );
        } finally {
          applyingRemote.current = false;
        }
      }
    },
    [fitView, getNode, publishGuides, setCenter, updateNodeInternals],
  );

  useEffect(() => {
    const unsub = subscribeHost(onHostMessage);
    postToHost({ type: "ready" });
    return unsub;
  }, [onHostMessage]);

  useEffect(() => {
    setEdges((prev) =>
      prev.map((e) => ({ ...e, data: { ...(e.data as object), canEdit } })),
    );
  }, [canEdit]);

  const nodeKind = useCallback(
    (id: string | null | undefined) =>
      id ? nodesRef.current.find((n) => n.id === id)?.data.kind : undefined,
    [],
  );

  const isValidConnection = useCallback(
    (connection: Connection | Edge) => {
      const source = connection.source;
      const target = connection.target;
      return (
        connectionRejectReason(
          source,
          target,
          nodeKind(source),
          nodeKind(target),
        ) === null
      );
    },
    [nodeKind],
  );

  const onNodesChange = useCallback(
    (changes: NodeChange<Node<FlowNodeData>>[]) => {
      if (canEditRef.current) {
        let dragging = false;
        let nextGuides: AlignmentGuide[] = [];
        for (const change of changes) {
          if (change.type !== "position" || !change.dragging || !change.position) continue;
          dragging = true;
          const others = nodesRef.current
            .filter((n) => n.id !== change.id)
            .map((n) => ({
              id: n.id,
              rect: { x: n.position.x, y: n.position.y, width: FALLBACK_W, height: FALLBACK_H },
            }));
          const snapped = computeSnap(
            { x: change.position.x, y: change.position.y, width: FALLBACK_W, height: FALLBACK_H },
            others,
          );
          change.position = snapped.position;
          nextGuides = snapped.guides;
        }
        if (dragging) publishGuides(nextGuides);
      }
      setNodes((prev) => applyNodeChanges(changes, prev));
    },
    [publishGuides],
  );

  const snapDraggedNodes = useCallback(
    (current: Node<FlowNodeData>[], group: FlowNode[]) => {
      const groupIds = new Set(group.map((g) => g.id));
      return current.map((n) => {
        if (!groupIds.has(n.id)) return n;
        const others = current
          .filter((o) => !groupIds.has(o.id))
          .map((o) => ({
            id: o.id,
            rect: { x: o.position.x, y: o.position.y, width: FALLBACK_W, height: FALLBACK_H },
          }));
        const snapped = computeSnap(
          { x: n.position.x, y: n.position.y, width: FALLBACK_W, height: FALLBACK_H },
          others,
        );
        return { ...n, position: snapped.position };
      });
    },
    [],
  );

  const onNodeDragStop: OnNodeDrag<Node<FlowNodeData>> = useCallback(
    (_event, node, draggedNodes) => {
      publishGuides([]);
      if (applyingRemote.current || !canEditRef.current) return;
      const group =
        draggedNodes && draggedNodes.length > 0 ? draggedNodes : [node];
      setNodes((current) => {
        const next = canEditRef.current ? snapDraggedNodes(current, group) : current;
        postToHost({
          type: "nodesMoved",
          payload: {
            positions: group.map((g) => {
              const placed = next.find((n) => n.id === g.id) ?? g;
              return { id: placed.id, x: placed.position.x, y: placed.position.y };
            }),
          },
        });
        return next;
      });
    },
    [publishGuides, snapDraggedNodes],
  );

  const onConnect = useCallback(
    (connection: Connection) => {
      if (applyingRemote.current || !canEditRef.current) return;
      if (!connection.source || !connection.target) return;
      const reason = connectionRejectReason(
        connection.source,
        connection.target,
        nodeKind(connection.source),
        nodeKind(connection.target),
      );
      if (reason) {
        postToHost({ type: "connectEnd", payload: { ok: false, message: reason } });
        return;
      }
      postToHost({
        type: "connect",
        payload: {
          from: connection.source,
          to: connection.target,
          sourceHandle: normalizeSourceHandle(connection.sourceHandle ?? undefined) ?? "right",
          targetHandle: connection.targetHandle ?? "left",
        },
      });
    },
    [nodeKind],
  );

  const onConnectEnd = useCallback(
    (_event: MouseEvent | TouchEvent, state: FinalConnectionState) => {
      if (!canEditRef.current || state.isValid !== false) return;
      const from = state.fromNode;
      const to = state.toNode;
      if (!from || !to) return;
      const reason = connectionRejectReason(from.id, to.id, from.data?.kind, to.data?.kind);
      postToHost({
        type: "connectEnd",
        payload: { ok: false, message: reason ?? "Conexão inválida." },
      });
    },
    [],
  );

  const onReconnect = useCallback(
    (oldEdge: Edge, connection: Connection) => {
      if (!canEditRef.current || !isValidConnection(connection)) return;
      const parts = oldEdge.id.split("|");
      if (parts.length !== 3) return;
      setEdges((eds) => reconnectEdge(oldEdge, connection, eds, { shouldReplaceId: false }));
      postToHost({
        type: "reconnect",
        payload: {
          from: parts[0],
          to: parts[2],
          port: parts[1],
          newFrom: connection.source!,
          newTo: connection.target!,
          sourceHandle: normalizeSourceHandle(connection.sourceHandle ?? undefined) ?? "right",
          targetHandle: connection.targetHandle ?? "left",
        },
      });
    },
    [isValidConnection],
  );

  const onSelectionChange = useCallback(({ nodes: selNodes, edges: selEdges }: OnSelectionChangeParams) => {
    if (applyingRemote.current) return;
    postToHost({
      type: "selection",
      payload: {
        nodeIds: selNodes.map((n) => n.id),
        edgeId: selEdges[0]?.id ?? null,
      },
    });
  }, []);

  const onBeforeDelete = useCallback<OnBeforeDelete<Node, Edge>>(
    async ({ nodes: delNodes, edges: delEdges }) => ({
      nodes: delNodes.filter((n) => n.data.kind !== "start"),
      edges: delEdges,
    }),
    [],
  );

  const onNodesDelete = useCallback((deleted: Node[]) => {
    if (applyingRemote.current || !canEditRef.current) return;
    const ids = deleted.map((n) => n.id).filter((id) => {
      const k = nodesRef.current.find((x) => x.id === id)?.data.kind;
      return k !== "start";
    });
    if (ids.length > 0) postToHost({ type: "deleteNodes", payload: { ids } });
  }, []);

  const onEdgesDelete = useCallback((deleted: Edge[]) => {
    if (applyingRemote.current || !canEditRef.current) return;
    for (const edge of deleted) {
      const parts = edge.id.split("|");
      if (parts.length !== 3) continue;
      postToHost({
        type: "removeEdge",
        payload: { from: parts[0], to: parts[2], port: parts[1] },
      });
    }
  }, []);

  const proOptions = useMemo(() => ({ hideAttribution: true }), []);

  return (
    <div className="flow-canvas-wrap">
      <FlowToolbar
        canEdit={canEdit}
        canUndo={canUndo}
        canRedo={canRedo}
        selectedEdge={selectedEdge}
      />
      <div className="flow-canvas-inner">
        <ReactFlow
          nodes={nodes}
          edges={edges}
          nodeTypes={nodeTypes}
          edgeTypes={edgeTypes}
          onNodesChange={onNodesChange}
          onEdgesChange={(changes: EdgeChange[]) =>
            setEdges((prev) => applyEdgeChanges(changes, prev))
          }
          onNodeDragStop={onNodeDragStop}
          onConnect={onConnect}
          onConnectEnd={onConnectEnd}
          onReconnect={onReconnect}
          onSelectionChange={onSelectionChange}
          onBeforeDelete={onBeforeDelete}
          onNodesDelete={onNodesDelete}
          onEdgesDelete={onEdgesDelete}
          onPaneClick={() =>
            postToHost({ type: "selection", payload: { nodeIds: [], edgeId: null } })
          }
          onEdgeClick={(_, edge) =>
            postToHost({ type: "selection", payload: { nodeIds: [], edgeId: edge.id } })
          }
          isValidConnection={isValidConnection}
          nodesDraggable={canEdit}
          nodesConnectable={canEdit}
          edgesReconnectable={canEdit}
          elementsSelectable
          edgesFocusable={canEdit}
          deleteKeyCode={canEdit ? ["Backspace", "Delete"] : null}
          snapToGrid={canEdit}
          snapGrid={[10, 10]}
          panOnScroll
          zoomOnDoubleClick={false}
          minZoom={0.1}
          maxZoom={2}
          fitView
          fitViewOptions={{ padding: 0.12, minZoom: 0.15, maxZoom: 1 }}
          proOptions={proOptions}
        >
          <Background variant={BackgroundVariant.Dots} gap={20} size={1} color="#cbd5e1" />
          <FlowAlignmentGuides guides={guides} />
          <Controls showInteractive={false} position="bottom-left" />
          <MiniMap pannable zoomable position="bottom-right" />
        </ReactFlow>
      </div>
    </div>
  );
}
