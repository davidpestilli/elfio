import { Handle, Position, type NodeProps } from "@xyflow/react";
import type { CSSProperties } from "react";
import { HOBLIO_ROW_Y } from "../nodeLayout";

export type FlowNodeData = {
  kind: string;
  label: string;
  summary: string;
  isActive: boolean;
  isFlashing?: boolean;
};

type SideSpec = {
  sideId: string;
  position: Position;
  sourceId?: string;
  sourceClassName?: string;
  target: boolean;
  source: boolean;
};

/**
 * Only override left/right (logical row for straight horizontal edges).
 * Top/bottom use XYFlow defaults (bottom: 0 + translate(-50%, 50%), etc.).
 */
function handleStyle(position: Position): CSSProperties | undefined {
  switch (position) {
    case Position.Left:
      return { top: HOBLIO_ROW_Y, transform: "translate(-50%, -50%)" };
    case Position.Right:
      return { top: HOBLIO_ROW_Y, transform: "translate(50%, -50%)" };
    default:
      return undefined;
  }
}

function sideSpecs(kind: string, showTarget: boolean, showSource: boolean): SideSpec[] {
  const io = (sideId: string, position: Position): SideSpec => ({
    sideId,
    position,
    sourceId: sideId,
    target: showTarget,
    source: showSource,
  });

  switch (kind.toLowerCase()) {
    case "if":
    case "case":
      return [
        { sideId: "top", position: Position.Top, target: showTarget, source: false },
        { sideId: "left", position: Position.Left, target: showTarget, source: false },
        {
          sideId: "right",
          position: Position.Right,
          sourceId: "then",
          sourceClassName: "port-then",
          target: false,
          source: showSource,
        },
        {
          sideId: "bottom",
          position: Position.Bottom,
          sourceId: "else",
          sourceClassName: "port-else",
          target: false,
          source: showSource,
        },
      ];
    case "switch":
      return [
        { sideId: "top", position: Position.Top, target: showTarget, source: false },
        { sideId: "left", position: Position.Left, target: showTarget, source: false },
        {
          sideId: "right",
          position: Position.Right,
          sourceId: "right",
          sourceClassName: "port-then port-switch-out",
          target: false,
          source: showSource,
        },
      ];
    case "for_each":
    case "while_loop":
    case "repeat":
      return [
        { sideId: "top", position: Position.Top, target: showTarget, source: false },
        { sideId: "left", position: Position.Left, target: showTarget, source: false },
        {
          sideId: "right",
          position: Position.Right,
          sourceId: "body",
          sourceClassName: "port-body",
          target: false,
          source: showSource,
        },
        {
          sideId: "bottom",
          position: Position.Bottom,
          sourceId: "after",
          sourceClassName: "port-after",
          target: false,
          source: showSource,
        },
      ];
    default:
      return [
        io("top", Position.Top),
        io("right", Position.Right),
        io("bottom", Position.Bottom),
        io("left", Position.Left),
      ];
  }
}

function SideHandles({ spec }: { spec: SideSpec }) {
  const style = handleStyle(spec.position);
  return (
    <>
      {spec.target ? (
        <Handle
          key={`target-${spec.sideId}`}
          id={spec.sideId}
          type="target"
          position={spec.position}
          style={style}
          className="hoblio-handle hoblio-handle-target"
        />
      ) : null}
      {spec.source ? (
        <Handle
          key={`source-${spec.sourceId}`}
          id={spec.sourceId!}
          type="source"
          position={spec.position}
          style={style}
          className={`hoblio-handle hoblio-handle-source ${spec.sourceClassName ?? ""}`}
        />
      ) : null}
    </>
  );
}

export function FlowNode({ id, data, selected }: NodeProps) {
  const d = data as FlowNodeData;
  const kind = d.kind.toLowerCase();
  const showTarget = kind !== "start";
  const showSource = kind !== "end";
  const sides = sideSpecs(kind, showTarget, showSource);
  const cls = [
    "hoblio-node",
    selected ? "selected" : "",
    d.isActive ? "active" : "",
    d.isFlashing ? "flash" : "",
    `kind-${kind}`,
  ]
    .filter(Boolean)
    .join(" ");

  return (
    <div className={cls}>
      {sides.map((spec) => (
        <SideHandles key={spec.sideId} spec={spec} />
      ))}
      <div className="hoblio-node-body">
        <div className="hoblio-node-title">{d.label}</div>
        <div className="hoblio-node-sub">{d.summary || kind}</div>
        <div className="hoblio-node-id">{id}</div>
        {(kind === "if" || kind === "case") && (
          <div className="hoblio-node-ports">
            <span className="port-label port-then-label">then →</span>
            <span className="port-label port-else-label">else ↓</span>
          </div>
        )}
        {kind === "switch" && (
          <div className="hoblio-node-ports">
            <span className="port-label port-then-label">cases →</span>
          </div>
        )}
        {(kind === "for_each" || kind === "while_loop" || kind === "repeat") && (
          <div className="hoblio-node-ports">
            <span className="port-label port-body-label">body →</span>
            <span className="port-label port-after-label">after ↓</span>
          </div>
        )}
      </div>
    </div>
  );
}
