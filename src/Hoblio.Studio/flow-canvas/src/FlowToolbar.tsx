import { useEffect, useState } from "react";
import { postToHost, type SelectedEdgeDto } from "./bridge";

function ToolButton({
  label,
  onClick,
  disabled,
  active,
  title,
}: {
  label: string;
  onClick: () => void;
  disabled?: boolean;
  active?: boolean;
  title?: string;
}) {
  return (
    <button
      type="button"
      title={title ?? label}
      disabled={disabled}
      onClick={onClick}
      className={`flow-tool-btn ${active ? "active" : ""}`}
    >
      {label}
    </button>
  );
}

type Props = {
  canEdit: boolean;
  canUndo: boolean;
  canRedo: boolean;
  selectedEdge: SelectedEdgeDto | null;
};

export function FlowToolbar({ canEdit, canUndo, canRedo, selectedEdge }: Props) {
  const [customOpen, setCustomOpen] = useState(false);
  const [labelDraft, setLabelDraft] = useState("");

  useEffect(() => {
    setCustomOpen(false);
    setLabelDraft(selectedEdge?.label ?? "");
  }, [selectedEdge?.id, selectedEdge?.label]);

  const applyDraft = () => {
    if (!selectedEdge) return;
    postToHost({
      type: "toolbar",
      payload: { action: "setEdgeLabel", edgeId: selectedEdge.id, label: labelDraft },
    });
  };

  const titleOf = (nodeId: string, fallback: string) => fallback || nodeId;

  return (
    <div className="flow-toolbar">
      <div className="flow-toolbar-row">
        <div className="flow-toolbar-group" role="group" aria-label="Modo do diagrama">
          <button
            type="button"
            aria-pressed={!canEdit}
            className={`flow-mode-btn ${!canEdit ? "active" : ""}`}
            onClick={() =>
              postToHost({ type: "toolbar", payload: { action: "setEditing", editing: false } })
            }
          >
            Exibição
          </button>
          <button
            type="button"
            aria-pressed={canEdit}
            className={`flow-mode-btn ${canEdit ? "active" : ""}`}
            onClick={() =>
              postToHost({ type: "toolbar", payload: { action: "setEditing", editing: true } })
            }
          >
            Edição
          </button>
        </div>

        {canEdit ? (
          <div className="flow-toolbar-actions">
            <ToolButton
              label="Desfazer"
              onClick={() => postToHost({ type: "toolbar", payload: { action: "undo" } })}
              disabled={!canUndo}
            />
            <ToolButton
              label="Refazer"
              onClick={() => postToHost({ type: "toolbar", payload: { action: "redo" } })}
              disabled={!canRedo}
            />
            <ToolButton
              label="Apagar seleção"
              onClick={() => postToHost({ type: "toolbar", payload: { action: "deleteSelection" } })}
              title="Remove nós ou conexões selecionados (Delete / Backspace)"
            />
            <ToolButton
              label="Limpar fluxo"
              onClick={() => postToHost({ type: "toolbar", payload: { action: "clearFlow" } })}
              title="Remove todo o diagrama e variáveis (canvas vazio)"
            />
          </div>
        ) : null}
      </div>

      {selectedEdge ? (
        <div className="flow-edge-bar">
          <span className="flow-edge-bar-label">Conexão</span>
          <span className="flow-edge-bar-path">
            {titleOf(selectedEdge.sourceId, selectedEdge.sourceLabel)} →{" "}
            {titleOf(selectedEdge.targetId, selectedEdge.targetLabel)}
          </span>
          {canEdit ? (
            <>
              <ToolButton
                label="Limpar texto"
                onClick={() =>
                  postToHost({
                    type: "toolbar",
                    payload: { action: "setEdgeLabel", edgeId: selectedEdge.id, label: "" },
                  })
                }
              />
              <ToolButton label="Custom" active={customOpen} onClick={() => setCustomOpen(true)} />
              {customOpen ? (
                <input
                  autoFocus
                  className="flow-edge-label-input"
                  value={labelDraft}
                  placeholder="Texto da conexão"
                  onChange={(event) => setLabelDraft(event.target.value)}
                  onBlur={applyDraft}
                  onKeyDown={(event) => {
                    if (event.key === "Enter") {
                      applyDraft();
                      setCustomOpen(false);
                    }
                    if (event.key === "Escape") {
                      setCustomOpen(false);
                    }
                  }}
                />
              ) : (
                <span className="flow-edge-current-label">
                  {selectedEdge.label || "(sem texto)"}
                </span>
              )}
            </>
          ) : (
            <span className="flow-edge-hint">Ative Edição para alterar esta conexão.</span>
          )}
          <ToolButton
            label="Fechar"
            onClick={() => postToHost({ type: "toolbar", payload: { action: "clearEdgeSelection" } })}
          />
        </div>
      ) : null}
    </div>
  );
}
