import { useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { z } from 'zod';
import { useCreateProject } from '../../api/dataSource';
import { Button } from '../../components/ui/Button';
import { Dialog } from '../../components/ui/Dialog';

const schema = z.object({ name: z.string().trim().min(3, 'Proje adı en az 3 karakter olmalıdır.') });

export function CreateProjectDialog({ open, onClose, workspaceId }: { open: boolean; onClose: () => void; workspaceId: number }) {
  const navigate = useNavigate();
  const { workspaceId: routeWorkspaceId } = useParams();
  const mutation = useCreateProject(workspaceId);
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [error, setError] = useState('');

  const submit = () => {
    const result = schema.safeParse({ name });
    if (!result.success) {
      setError(result.error.issues[0]?.message ?? 'Formu kontrol edin.');
      return;
    }
    mutation.mutate(
      { name: name.trim(), description: description.trim() || null },
      {
        onSuccess: (project) => {
          setName('');
          setDescription('');
          onClose();
          navigate(`/w/${routeWorkspaceId ?? workspaceId}/projects/${project.id}/overview`);
        },
      },
    );
  };

  return <Dialog open={open} onClose={onClose} title="Yeni proje" description="Çalışma alanınız için aktif bir proje oluşturun." footer={<><Button onClick={onClose}>Vazgeç</Button><Button variant="primary" disabled={mutation.isPending} onClick={submit}>{mutation.isPending ? 'Oluşturuluyor…' : 'Proje oluştur'}</Button></>}><div className="form-stack"><label>Proje adı<input autoFocus value={name} onChange={(event) => { setName(event.target.value); setError(''); }} /></label>{error ? <p className="field-error" role="alert">{error}</p> : null}<label>Açıklama<textarea rows={4} value={description} onChange={(event) => setDescription(event.target.value)} /></label>{mutation.error ? <p className="field-error" role="alert">{mutation.error.message}</p> : null}</div></Dialog>;
}
