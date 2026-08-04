import { Check, ChevronDown, Plus } from 'lucide-react';
import { useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useCreateWorkspace, useWorkspaces } from '../../api/dataSource';
import { useAppContext } from '../../app/AppProviders';
import { Button } from '../../components/ui/Button';
import { Dialog } from '../../components/ui/Dialog';

export function WorkspaceSwitcher({ workspaceId, onNavigate }: { workspaceId: number; onNavigate: () => void }) {
  const navigate = useNavigate();
  const { user } = useAppContext();
  const workspaces = useWorkspaces().data?.items ?? [];
  const active = workspaces.find((workspace) => workspace.id === workspaceId);
  const createWorkspace = useCreateWorkspace();
  const [open, setOpen] = useState(false);
  const [createOpen, setCreateOpen] = useState(false);
  const [name, setName] = useState('');
  const [error, setError] = useState('');
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!open) return;
    const onPointerDown = (event: MouseEvent) => {
      if (!containerRef.current?.contains(event.target as Node)) setOpen(false);
    };
    const onKey = (event: KeyboardEvent) => {
      if (event.key === 'Escape') setOpen(false);
    };
    document.addEventListener('mousedown', onPointerDown);
    document.addEventListener('keydown', onKey);
    return () => {
      document.removeEventListener('mousedown', onPointerDown);
      document.removeEventListener('keydown', onKey);
    };
  }, [open]);

  const switchTo = (id: number) => {
    setOpen(false);
    if (id !== workspaceId) navigate(`/w/${id}`);
    onNavigate();
  };

  const create = async (event: React.FormEvent) => {
    event.preventDefault();
    const trimmed = name.trim();
    if (trimmed.length < 3) { setError('Çalışma alanı adı en az 3 karakter olmalıdır.'); return; }
    setError('');
    try {
      const workspace = await createWorkspace.mutateAsync(trimmed);
      setCreateOpen(false);
      setName('');
      navigate(`/w/${workspace.id}`);
      onNavigate();
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Çalışma alanı oluşturulamadı.');
    }
  };

  const label = active?.name ?? 'Workspace';

  return (
    <div className="workspace-switcher" ref={containerRef}>
      <button
        type="button"
        className="sidebar-workspace"
        onClick={() => setOpen((value) => !value)}
        aria-expanded={open}
        aria-haspopup="menu"
      >
        <span className="workspace-avatar">{label.slice(0, 2).toLocaleUpperCase('tr-TR')}</span>
        <span><strong>{label}</strong><span>{user.workspaceRole}</span></span>
        <ChevronDown aria-hidden="true" />
      </button>
      {open ? (
        <div className="workspace-menu" role="menu">
          <div className="workspace-menu__label">Çalışma alanları</div>
          {workspaces.map((workspace) => (
            <button type="button" role="menuitem" key={workspace.id} onClick={() => switchTo(workspace.id)}>
              <span>{workspace.name}</span>
              {workspace.id === workspaceId ? <Check aria-label="Seçili" /> : null}
            </button>
          ))}
          {workspaces.length ? null : <p className="workspace-menu__empty">Henüz çalışma alanı yok.</p>}
          <button
            type="button"
            role="menuitem"
            className="workspace-menu__create"
            onClick={() => { setOpen(false); setError(''); setCreateOpen(true); }}
          >
            <Plus aria-hidden="true" />Yeni çalışma alanı
          </button>
        </div>
      ) : null}
      <Dialog
        open={createOpen}
        onClose={() => setCreateOpen(false)}
        title="Yeni çalışma alanı"
        description="Ekip ve yetki sınırınızı belirleyen yeni bir alan oluşturun."
        size="sm"
      >
        <form className="form-stack" id="create-workspace-form" onSubmit={create}>
          <label>Çalışma alanı adı
            <input autoFocus value={name} onChange={(event) => setName(event.target.value)} placeholder="Örn. Northstar Studio" />
          </label>
          {error ? <div className="auth-error" role="alert">{error}</div> : null}
          <Button variant="primary" disabled={createWorkspace.isPending}>
            {createWorkspace.isPending ? 'Oluşturuluyor…' : 'Oluştur'}
          </Button>
        </form>
      </Dialog>
    </div>
  );
}
