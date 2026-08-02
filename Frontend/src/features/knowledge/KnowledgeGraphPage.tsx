import { List, Maximize2, Minus, Plus, Search } from 'lucide-react';
import { useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useGraph } from '../../api/dataSource';
import { PageHeader } from '../../components/PageHeader';
import { Button } from '../../components/ui/Button';
import { EmptyState, LoadingSkeleton, ProblemState } from '../../components/ui/States';

export function KnowledgeGraphPage() {
  const { workspaceId = '1', projectId = '1' } = useParams();
  const wid = Number(workspaceId), pid = Number(projectId);
  const graph = useGraph(wid, pid);
  const navigate = useNavigate();
  const [selected, setSelected] = useState<number | null>(null);
  const [listMode, setListMode] = useState(false);
  const [query, setQuery] = useState('');
  const [zoom, setZoom] = useState(1);
  const positions = useMemo(() => new Map((graph.data?.nodes ?? []).map((node, index, all) => { const angle = (index / Math.max(1, all.length)) * Math.PI * 2; return [node.noteId, { x: 400 + Math.cos(angle) * 190, y: 220 + Math.sin(angle) * 120 }]; })), [graph.data?.nodes]);
  if (graph.isLoading) return <LoadingSkeleton rows={8} />;
  if (graph.error) return <ProblemState error={graph.error} onRetry={() => void graph.refetch()} />;
  if (!graph.data?.nodes.length) return <EmptyState title="Grafikte gösterilecek not yok" description="Notlar ve wiki-link bağlantıları oluştuğunda bilgi grafiği burada görünecek." />;
  const visible = graph.data.nodes.filter((node) => node.title.toLocaleLowerCase('tr').includes(query.toLocaleLowerCase('tr')));
  const selectedNode = graph.data.nodes.find((node) => node.noteId === selected);
  return <div className="page graph-page"><PageHeader title="Bilgi Grafiği" description="Notlar arasındaki bağlantıları keşfedin; erişilebilir liste görünümüne istediğiniz an geçin." /><div className="graph-toolbar"><label className="search-field"><Search /><span className="sr-only">Grafikte ara</span><input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Not ara…" /></label><Button icon={<List />} onClick={() => setListMode((value) => !value)}>{listMode ? 'Grafiği göster' : 'Liste görünümü'}</Button>{!listMode ? <div className="zoom-controls"><button className="icon-button" onClick={() => setZoom((value) => Math.min(1.6, value + .2))} aria-label="Yakınlaştır"><Plus /></button><button className="icon-button" onClick={() => setZoom((value) => Math.max(.6, value - .2))} aria-label="Uzaklaştır"><Minus /></button><button className="icon-button" onClick={() => setZoom(1)} aria-label="Grafiği sığdır"><Maximize2 /></button></div> : null}</div><div className="graph-layout">{listMode ? <div className="graph-list" role="list">{visible.map((node) => { const links = graph.data.edges.filter((edge) => edge.sourceNoteId === node.noteId || edge.targetNoteId === node.noteId).length; return <button role="listitem" key={node.noteId} onClick={() => navigate(`/w/${wid}/projects/${pid}/knowledge/${node.noteId}`)}><strong>{node.title}</strong><span>/{node.slug}</span><small>{links} bağlantı</small></button>; })}</div> : <div className="graph-canvas"><svg viewBox={`${400 - 400 / zoom} ${220 - 220 / zoom} ${800 / zoom} ${440 / zoom}`} role="img" aria-labelledby="graph-title graph-desc"><title id="graph-title">Bilgi notları bağlantı grafiği</title><desc id="graph-desc">{graph.data.nodes.length} not ve {graph.data.edges.length} bağlantı.</desc>{graph.data.edges.map((edge, index) => { const a = positions.get(edge.sourceNoteId), b = edge.targetNoteId ? positions.get(edge.targetNoteId) : undefined; return a && b ? <line key={index} x1={a.x} y1={a.y} x2={b.x} y2={b.y} className={selected && [edge.sourceNoteId, edge.targetNoteId].includes(selected) ? 'is-active' : ''} /> : null; })}{graph.data.nodes.map((node) => { const point = positions.get(node.noteId)!; return <g className={selected === node.noteId ? 'is-selected' : ''} key={node.noteId} transform={`translate(${point.x} ${point.y})`} onClick={() => setSelected(node.noteId)} role="button" tabIndex={0} onKeyDown={(event) => { if (event.key === 'Enter' || event.key === ' ') setSelected(node.noteId); }} aria-label={`${node.title} notunu seç`}><circle r={selected === node.noteId ? 24 : 18} /><text y="38" textAnchor="middle">{node.title}</text></g>; })}</svg></div>}<aside className="graph-inspector">{selectedNode ? <><span className="eyebrow">SEÇİLİ NOT</span><h2>{selectedNode.title}</h2><code>/{selectedNode.slug}</code><p>{graph.data.edges.filter((edge) => edge.sourceNoteId === selectedNode.noteId || edge.targetNoteId === selectedNode.noteId).length} doğrudan bağlantı</p><Button variant="primary" onClick={() => navigate(`/w/${wid}/projects/${pid}/knowledge/${selectedNode.noteId}`)}>Notu aç</Button></> : <p className="quiet-copy">Ayrıntıları görmek için bir düğüm seçin.</p>}</aside></div></div>;
}
