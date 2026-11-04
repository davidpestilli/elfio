import {
  BaseEdge,
  EdgeLabelRenderer,
  getSmoothStepPath,
  useReactFlow,
  type EdgeProps,
} from "@xyflow/react";
import { useCallback, useMemo, type PointerEvent as ReactPointerEvent } from "react";
import { postToHost } from "../bridge";
import { parseEdgeId } from "../flowHandles";
import { pathMidpoint } from "../flowLayout";
import type { WaypointEdgeData } from "./waypointTypes";

const NO_WAYPOINTS: { x: number; y: number }[] = [];

export function WaypointEdge(props: EdgeProps) {
  const {
    id,
    sourceX,
    sourceY,
    targetX,
    targetY,
    sourcePosition,
    targetPosition,
    markerEnd,
    label,
    selected,
    data,
  } = props;
  const edgeData = (data ?? {}) as WaypointEdgeData;
  const waypoints = edgeData.waypoints ?? NO_WAYPOINTS;
  const canEdit = edgeData.canEdit ?? false;
  const { screenToFlowPosition } = useReactFlow();

  const [path, labelX, labelY] = useMemo(() => {
    if (waypoints.length === 0) {
      return getSmoothStepPath({
        sourceX,
        sourceY,
        sourcePosition,
        targetX,
        targetY,
        targetPosition,
        borderRadius: 14,
      });
    }
    const points = [{ x: sourceX, y: sourceY }, ...waypoints, { x: targetX, y: targetY }];
    const pathData = points.map((p, i) => `${i === 0 ? "M" : "L"} ${p.x},${p.y}`).join(" ");
    const middle = pathMidpoint(points);
    return [pathData, middle.x, middle.y] as const;
  }, [sourceX, sourceY, sourcePosition, targetX, targetY, targetPosition, waypoints]);

  const points = useMemo(
    () => [{ x: sourceX, y: sourceY }, ...waypoints, { x: targetX, y: targetY }],
    [sourceX, sourceY, targetX, targetY, waypoints],
  );

  const pushWaypoints = useCallback(
    (next: { x: number; y: number }[], history = false) => {
      const parsed = parseEdgeId(id);
      if (!parsed) return;
      postToHost({
        type: "waypoints",
        payload: { from: parsed.from, to: parsed.to, port: parsed.port, waypoints: next, history },
      });
    },
    [id],
  );

  const dragWaypoint = useCallback(
    (index: number, event: ReactPointerEvent<HTMLDivElement>) => {
      if (!canEdit) return;
      event.stopPropagation();
      event.preventDefault();
      const snapshot = [...waypoints];
      pushWaypoints(snapshot, true);
      const move = (moveEvent: PointerEvent) => {
        const position = screenToFlowPosition({ x: moveEvent.clientX, y: moveEvent.clientY });
        pushWaypoints(
          snapshot.map((point, pointIndex) => (pointIndex === index ? position : point)),
          false,
        );
      };
      const stop = () => {
        window.removeEventListener("pointermove", move);
        window.removeEventListener("pointerup", stop);
      };
      window.addEventListener("pointermove", move);
      window.addEventListener("pointerup", stop);
    },
    [canEdit, pushWaypoints, screenToFlowPosition, waypoints],
  );

  const showTools = canEdit && Boolean(selected);

  return (
    <>
      {selected ? (
        <path
          d={path}
          fill="none"
          stroke="#2563eb"
          strokeOpacity={0.3}
          strokeWidth={8}
          strokeLinecap="round"
          strokeLinejoin="round"
          pointerEvents="none"
        />
      ) : null}
      <BaseEdge id={id} path={path} markerEnd={markerEnd} interactionWidth={22} />
      <EdgeLabelRenderer>
        {label ? (
          <div
            className={`flow-edge-label ${selected ? "selected" : ""}`}
            style={{ transform: `translate(-50%, -50%) translate(${labelX}px, ${labelY}px)` }}
          >
            {label}
          </div>
        ) : null}
        {showTools
          ? points.map((point, index) =>
              index === 0 || index === points.length - 1 ? null : (
                <div
                  key={`waypoint-${id}-${index}`}
                  role="button"
                  tabIndex={-1}
                  title="Arraste · duplo clique remove"
                  onPointerDown={(e) => dragWaypoint(index - 1, e)}
                  onDoubleClick={(e) => {
                    e.stopPropagation();
                    pushWaypoints(
                      waypoints.filter((_, pointIndex) => pointIndex !== index - 1),
                      true,
                    );
                  }}
                  className="waypoint-handle"
                  style={{ transform: `translate(-50%, -50%) translate(${point.x}px, ${point.y}px)` }}
                />
              ),
            )
          : null}
        {showTools
          ? points.map((point, index) => {
              if (index >= points.length - 1) return null;
              const next = points[index + 1];
              const middle = { x: (point.x + next.x) / 2, y: (point.y + next.y) / 2 };
              return (
                <button
                  key={`insert-${id}-${index}`}
                  type="button"
                  className="waypoint-add"
                  title="Criar ponto de passagem"
                  style={{ transform: `translate(-50%, -50%) translate(${middle.x}px, ${middle.y}px)` }}
                  onClick={(e) => {
                    e.stopPropagation();
                    const nextWaypoints = [...waypoints];
                    nextWaypoints.splice(index, 0, middle);
                    pushWaypoints(nextWaypoints, true);
                  }}
                >
                  +
                </button>
              );
            })
          : null}
      </EdgeLabelRenderer>
    </>
  );
}
