export function CitationChip({ kind, id, onClick }: { kind: 'Task' | 'Note'; id: number; onClick: () => void }) {
  return <button className="citation-chip" onClick={onClick}>{kind === 'Task' ? `TP-${id}` : `Not ${id}`}</button>;
}
