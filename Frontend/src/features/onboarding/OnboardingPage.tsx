import { useState } from 'react';
import { Navigate, useNavigate } from 'react-router-dom';
import { z } from 'zod';
import { hasSession } from '../../api/apiClient';
import { projectApi, workspaceApi } from '../../api/endpoints';
import { Button } from '../../components/ui/Button';

const schema = z.object({
  workspaceName: z.string().trim().min(3, 'Çalışma alanı adı en az 3 karakter olmalıdır.'),
  projectName: z.string().trim().min(3, 'İlk proje adı en az 3 karakter olmalıdır.'),
});

export function OnboardingPage() {
  const navigate = useNavigate();
  const [workspaceName, setWorkspaceName] = useState('');
  const [projectName, setProjectName] = useState('');
  const [pending, setPending] = useState(false);
  const [error, setError] = useState('');
  if (!hasSession()) return <Navigate replace to="/login" />;

  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    const parsed = schema.safeParse({ workspaceName, projectName });
    if (!parsed.success) {
      setError(parsed.error.issues[0]?.message ?? 'Formu kontrol edin.');
      return;
    }
    setPending(true);
    setError('');
    try {
      const workspace = await workspaceApi.create(workspaceName.trim());
      const project = await projectApi.create(workspace.id, projectName.trim(), null);
      navigate(`/w/${workspace.id}/projects/${project.id}/overview`, { replace: true });
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Çalışma alanı oluşturulamadı.');
    } finally {
      setPending(false);
    }
  };

  return <div className="auth-page"><section className="auth-form"><div className="auth-brand"><img className="brand-mark-image" src="/brand-mark.png" alt="" width="28" height="28" loading="eager" decoding="async" />TaskPilot</div><form onSubmit={submit}><span className="eyebrow">İLK KURULUM</span><h1>Çalışma alanınızı kurun</h1><p>İlk çalışma alanınızı ve projenizi oluşturarak başlayın.</p><label>Çalışma alanı adı<input autoFocus value={workspaceName} onChange={(event) => setWorkspaceName(event.target.value)} placeholder="Örn. Northstar Studio" /></label><label>İlk proje adı<input value={projectName} onChange={(event) => setProjectName(event.target.value)} placeholder="Örn. Atlas Web Platform" /></label>{error ? <div className="auth-error" role="alert">{error}</div> : null}<Button variant="primary" disabled={pending}>{pending ? 'Kuruluyor…' : 'Çalışma alanını oluştur'}</Button></form></section><aside className="auth-preview"><span className="eyebrow">BAĞLAMINIZLA BİRLİKTE BÜYÜR</span><h2>Görevler, notlar ve AI çalışma akışları tek projede buluşur.</h2><div className="onboarding-steps"><span>01</span><div><strong>Çalışma alanı</strong><small>Ekip ve yetki sınırınız</small></div><span>02</span><div><strong>Proje</strong><small>Görev ve bilgi bağlamınız</small></div><span>03</span><div><strong>Copilot</strong><small>Kaynaklı, onay kontrollü AI</small></div></div></aside></div>;
}
