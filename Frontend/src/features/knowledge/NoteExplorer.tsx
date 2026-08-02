import { ChevronDown, ChevronRight, FileText, Folder, Plus, Search, X } from 'lucide-react';
import { useMemo, useState } from 'react';
import type { KnowledgeFolder, Note } from '../../types/domain';

export function NoteExplorer({ folders, notes, selectedId, onSelect, onClose, canManage }: { folders: KnowledgeFolder[]; notes: Note[]; selectedId: number | null; onSelect: (id: number) => void; onClose: () => void; canManage: boolean }) {
  const [query, setQuery] = useState('');
  const [open, setOpen] = useState(() => new Set(folders.map((folder) => folder.id)));
  const filtered = useMemo(() => notes.filter((note) => note.title.toLocaleLowerCase('tr').includes(query.toLocaleLowerCase('tr'))), [notes, query]);
  const toggle = (id: number) => setOpen((current) => { const next = new Set(current); if (next.has(id)) next.delete(id); else next.add(id); return next; });
  return <aside className="note-explorer" aria-label="Not gezgini"><header><strong>Notlar</strong><div className="note-explorer__actions">{canManage ? <button className="icon-button" aria-label="Yeni not"><Plus /></button> : null}<button className="icon-button note-explorer__close" onClick={onClose} aria-label="Not gezginini kapat"><X /></button></div></header><label className="search-field search-field--compact"><Search /><span className="sr-only">Not ara</span><input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Not ara…" /></label><div className="note-tree">{folders.map((folder) => { const children = filtered.filter((note) => note.folderId === folder.id); return <section key={folder.id}><button className="note-folder" onClick={() => toggle(folder.id)}>{open.has(folder.id) ? <ChevronDown /> : <ChevronRight />}<Folder />{folder.name}<span>{children.length}</span></button>{open.has(folder.id) ? children.map((note) => <button className={`note-link${selectedId === note.id ? ' note-link--active' : ''}`} onClick={() => onSelect(note.id)} key={note.id}><FileText />{note.title}</button>) : null}</section>; })}</div></aside>;
}
