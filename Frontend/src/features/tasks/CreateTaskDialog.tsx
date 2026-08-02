import { useState } from 'react';
import { z } from 'zod';
import { useCreateTask } from '../../api/dataSource';
import { Button } from '../../components/ui/Button';
import { Dialog } from '../../components/ui/Dialog';
import type { TaskPriority } from '../../types/domain';

const schema = z.object({ title: z.string().trim().min(6, 'Başlık en az 6 karakter olmalıdır.'), dueDate: z.string().refine((value) => !value || new Date(`${value}T23:59:59`) >= new Date(), 'Geçmiş bir tarih seçilemez.') });

export function CreateTaskDialog({ open, onClose, projectId }: { open: boolean; onClose: () => void; projectId: number }) {
  const mutation = useCreateTask(projectId);
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [priority, setPriority] = useState<TaskPriority>('Medium');
  const [dueDate, setDueDate] = useState('');
  const [error, setError] = useState('');
  const submit = () => {
    const result = schema.safeParse({ title, dueDate });
    if (!result.success) { setError(result.error.issues[0]?.message ?? 'Formu kontrol edin.'); return; }
    mutation.mutate({ title: title.trim(), description: description.trim() || null, priority, dueDate: dueDate ? new Date(`${dueDate}T12:00:00`).toISOString() : null, assignedUserId: null }, { onSuccess: () => { setTitle(''); setDescription(''); setDueDate(''); onClose(); } });
  };
  return <Dialog open={open} onClose={onClose} title="Yeni görev" description="Görev Todo durumunda oluşturulur." size="lg" footer={<><Button onClick={onClose}>Vazgeç</Button><Button variant="primary" disabled={mutation.isPending} onClick={submit}>{mutation.isPending ? 'Oluşturuluyor…' : 'Görev oluştur'}</Button></>}><div className="form-stack"><label>Başlık<input value={title} onChange={(event) => { setTitle(event.target.value); setError(''); }} autoFocus /></label>{error ? <p className="field-error" role="alert">{error}</p> : null}<label>Açıklama<textarea rows={5} value={description} onChange={(event) => setDescription(event.target.value)} /></label><div className="form-grid"><label>Öncelik<select value={priority} onChange={(event) => setPriority(event.target.value as TaskPriority)}><option value="Low">Düşük</option><option value="Medium">Orta</option><option value="High">Yüksek</option><option value="Critical">Kritik</option></select></label><label>Due date<input type="date" value={dueDate} onChange={(event) => setDueDate(event.target.value)} /></label></div>{mutation.error ? <p className="field-error" role="alert">{mutation.error.message}</p> : null}</div></Dialog>;
}
