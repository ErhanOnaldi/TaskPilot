import { ExternalLink, X } from 'lucide-react';
import { useEffect, useRef } from 'react';
import { Link } from 'react-router-dom';
import { useAppContext } from '../../app/AppProviders';
import { PriorityBadge, StatusBadge } from '../../components/ui/Badge';
import { Button } from '../../components/ui/Button';
import { DeveloperNotice } from '../../components/ui/States';
import { formatDate, formatRelativeDue } from '../../lib/formatters';
import type { Project, TaskItem, TaskStatus } from '../../types/domain';
import { TaskStatusTransitionMenu } from './TaskStatusTransitionMenu';

export function TaskDetailDrawer({ task, project, open, onClose, onTransition, pending }: {
  task: TaskItem | null;
  project: Project | undefined;
  open: boolean;
  onClose: () => void;
  onTransition: (status: TaskStatus) => void;
  pending: boolean;
}) {
  const ref = useRef<HTMLDialogElement>(null);
  const { user } = useAppContext();
  useEffect(() => {
    const dialog = ref.current;
    if (open && dialog && !dialog.open) dialog.showModal();
    if (!open && dialog?.open) dialog.close();
  }, [open]);
  if (!task) return null;
  const due = formatRelativeDue(task);
  return (
    <dialog ref={ref} className="task-drawer" onClose={onClose} onCancel={(event) => { event.preventDefault(); onClose(); }}>
      <header className="task-drawer__header"><span className="mono">TP-{task.id}</span><div><Link to={`.`}>Tam sayfada aç <ExternalLink /></Link><button className="icon-button" onClick={onClose} aria-label="Görev detayını kapat"><X /></button></div></header>
      {project?.status === 'Archived' ? <div className="archive-banner">Arşivlenmiş proje · salt okunur</div> : null}
      <div className="task-drawer__body">
        <h1>{task.title}</h1>
        <div className="task-drawer__actions"><TaskStatusTransitionMenu task={task} user={user} projectArchived={project?.status === 'Archived'} onTransition={onTransition} disabled={pending} /></div>
        <dl className="task-fields">
          <div><dt>Durum</dt><dd><StatusBadge status={task.status} /></dd></div>
          <div><dt>Öncelik</dt><dd><PriorityBadge priority={task.priority} /></dd></div>
          <div><dt>Atanan</dt><dd>{task.assignedUserId ? `Kullanıcı ${task.assignedUserId}` : <span className="unassigned">Atanmamış</span>}</dd></div>
          <div><dt>Due date</dt><dd><time className={due.overdue ? 'is-overdue' : ''}>{due.label}</time></dd></div>
          <div><dt>Oluşturuldu</dt><dd>{formatDate(task.createdAt)}</dd></div>
          <div><dt>Güncellendi</dt><dd>{formatDate(task.updatedAt)}</dd></div>
          <div><dt>Tamamlandı</dt><dd>{formatDate(task.completedAt)}</dd></div>
        </dl>
        <section className="task-detail-section"><h2>Açıklama</h2><p>{task.description || 'Açıklama girilmemiş.'}</p></section>
        <section className="task-detail-section"><div className="section-header"><div><h2>Bağlı notlar</h2></div></div><p className="quiet-copy">Bu görev için bağlı not bulunmuyor.</p>{import.meta.env.DEV ? <DeveloperNotice>task-note listesi okuma ucu</DeveloperNotice> : null}</section>
        <section className="task-detail-section"><h2>Yorum ekle</h2><label className="sr-only" htmlFor="task-comment">Yorum</label><textarea id="task-comment" rows={4} maxLength={2000} placeholder="Bir güncelleme paylaşın…" /><footer><span>En fazla 2000 karakter</span><Button variant="primary">Yorum ekle</Button></footer></section>
      </div>
    </dialog>
  );
}
