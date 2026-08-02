import { Link2, PanelLeftClose, Save } from 'lucide-react';
import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useAppContext } from '../../app/AppProviders';
import { ApiError } from '../../api/apiClient';
import { isDemoMode, useFolders, useNote, useProjects, useUpdateNote } from '../../api/dataSource';
import { demoNotes } from '../../mocks/demoData';
import { Button } from '../../components/ui/Button';
import { DeveloperNotice, EmptyState, LoadingSkeleton, ProblemState } from '../../components/ui/States';
import { canEditKnowledge } from '../../lib/taskRules';
import { BacklinksPanel } from './BacklinksPanel';
import { MarkdownEditor } from './MarkdownEditor';
import { NoteConflictDialog } from './NoteConflictDialog';
import { NoteExplorer } from './NoteExplorer';

const isMobileViewport = () => typeof window !== 'undefined' && window.matchMedia('(max-width: 767px)').matches;

export function KnowledgePage() {
  const { workspaceId = '1', projectId = '1', noteId } = useParams();
  const navigate = useNavigate();
  const wid = Number(workspaceId), pid = Number(projectId);
  const folders = useFolders(wid, pid);
  const notes = isDemoMode ? demoNotes.filter((note) => !note.projectId || note.projectId === pid) : [];
  const selectedId = Number(noteId || notes[0]?.id || 0);
  const noteQuery = useNote(selectedId);
  const mutation = useUpdateNote(selectedId);
  const project = useProjects(wid).data?.items.find((item) => item.id === pid);
  const { user } = useAppContext();
  const [content, setContent] = useState('');
  const [title, setTitle] = useState('');
  const [mobile, setMobile] = useState(isMobileViewport);
  const [explorer, setExplorer] = useState(() => !isMobileViewport());
  const [links, setLinks] = useState(() => !isMobileViewport());
  const [conflict, setConflict] = useState(false);
  useEffect(() => { if (noteQuery.data) { setContent(noteQuery.data.content); setTitle(noteQuery.data.title); } }, [noteQuery.data]);
  useEffect(() => {
    const onKey = (event: KeyboardEvent) => { if ((event.metaKey || event.ctrlKey) && event.key.toLowerCase() === 's') { event.preventDefault(); document.getElementById('save-note')?.click(); } };
    window.addEventListener('keydown', onKey); return () => window.removeEventListener('keydown', onKey);
  }, []);
  useEffect(() => {
    const media = window.matchMedia('(max-width: 767px)');
    const onChange = (event: MediaQueryListEvent) => {
      setMobile(event.matches);
      if (event.matches) {
        setExplorer(false);
        setLinks(false);
      }
    };
    media.addEventListener('change', onChange);
    return () => media.removeEventListener('change', onChange);
  }, []);
  if (folders.isLoading || (selectedId && noteQuery.isLoading)) return <LoadingSkeleton rows={10} />;
  if (folders.error || noteQuery.error) return <ProblemState error={folders.error ?? noteQuery.error} onRetry={() => { void folders.refetch(); void noteQuery.refetch(); }} />;
  if (!selectedId || !noteQuery.data) return <div className="feature-empty"><EmptyState title="Henüz not yok" description="Bu kapsamda gösterilecek bir not bulunmuyor." />{import.meta.env.DEV ? <DeveloperNotice>workspace/project note listesi okuma ucu</DeveloperNotice> : null}</div>;
  const note = noteQuery.data;
  const readOnly = !canEditKnowledge(user.workspaceRole, project?.status === 'Archived');
  const dirty = content !== note.content || title !== note.title;
  const select = (id: number) => {
    navigate(`/w/${wid}/projects/${pid}/knowledge/${id}`);
    if (mobile) {
      setExplorer(false);
      setLinks(false);
    }
  };
  const save = () => mutation.mutate({ title, slug: note.slug, content, projectId: note.projectId, folderId: note.folderId, expectedVersion: note.version }, { onError: (error) => { if (error instanceof ApiError && error.status === 409) setConflict(true); } });
  return <div className="knowledge-layout">{explorer ? <NoteExplorer folders={folders.data ?? []} notes={notes} selectedId={selectedId} onSelect={select} onClose={() => setExplorer(false)} canManage={['Owner', 'Manager'].includes(user.workspaceRole)} /> : null}<section className="note-workspace"><header className="note-toolbar"><button className="icon-button" onClick={() => setExplorer((value) => !value)} aria-expanded={explorer} aria-label="Not gezginini aç veya kapat"><PanelLeftClose /></button><div><input aria-label="Not başlığı" value={title} onChange={(event) => setTitle(event.target.value)} readOnly={readOnly} /><span className="mono">/{note.slug} · v{note.version}</span></div><span className={dirty ? 'save-state save-state--dirty' : 'save-state'}>{dirty ? 'kaydedilmedi' : 'kaydedildi'}</span><Button id="save-note" variant={dirty ? 'primary' : 'secondary'} icon={<Save />} disabled={!dirty || readOnly || mutation.isPending} onClick={save}>{mutation.isPending ? 'Kaydediliyor…' : 'Kaydet'}</Button><button className="icon-button" onClick={() => setLinks((value) => !value)} aria-expanded={links} aria-label="Not bağlantılarını aç veya kapat"><Link2 /></button></header>{mutation.error && !(mutation.error instanceof ApiError && mutation.error.status === 409) ? <div className="inline-problem" role="alert">{mutation.error instanceof ApiError && mutation.error.status === 428 ? 'Güncel not sürümü olmadan kayıt yapılamadı. Notu yenileyip tekrar deneyin.' : mutation.error.message}</div> : null}<MarkdownEditor value={content} onChange={setContent} readOnly={readOnly} /></section>{links ? <BacklinksPanel note={note} notes={notes} onClose={() => setLinks(false)} onSelect={select} /> : null}<NoteConflictDialog open={conflict} local={content} server={note.content} onClose={() => setConflict(false)} onLoadServer={() => { setContent(note.content); setTitle(note.title); setConflict(false); }} /></div>;
}
