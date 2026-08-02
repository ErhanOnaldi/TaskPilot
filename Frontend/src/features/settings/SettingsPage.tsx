import { Archive, Save, Tags, TriangleAlert } from 'lucide-react';
import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { useAppContext } from '../../app/AppProviders';
import { useProjects, useWorkspace } from '../../api/dataSource';
import { PageHeader } from '../../components/PageHeader';
import { Button } from '../../components/ui/Button';

export function SettingsPage() {
  const { workspaceId = '1', projectId } = useParams();
  const { user } = useAppContext();
  const workspace = useWorkspace(Number(workspaceId)).data;
  const project = useProjects(Number(workspaceId)).data?.items.find((item) => item.id === Number(projectId));
  const [name, setName] = useState('');
  useEffect(() => {
    if (workspace?.name) setName(workspace.name);
  }, [workspace?.name]);
  const owner = user.workspaceRole === 'Owner';
  return <div className="page settings-page"><PageHeader title="Ayarlar" description="Workspace, proje ve label politikalarını yönetin." /><section className="settings-section"><header><div><h2>Workspace</h2><p>Ad ve genel çalışma alanı ayarları.</p></div></header><div className="settings-form"><label>Workspace adı<input value={name} onChange={(event) => setName(event.target.value)} disabled={!owner} /></label><Button icon={<Save />} disabled={!owner}>Değişiklikleri kaydet</Button></div></section>{project ? <section className="settings-section"><header><div><h2>Proje</h2><p>{project.name}</p></div></header><dl className="settings-summary"><div><dt>Durum</dt><dd>{project.status}</dd></div><div><dt>Son güncelleme</dt><dd>{new Intl.DateTimeFormat('tr-TR').format(new Date(project.updatedAt))}</dd></div></dl></section> : null}<section className="settings-section"><header><div><h2><Tags /> Label yönetimi</h2><p>Label oluşturma ve düzenleme yalnız Manager/Owner için açıktır.</p></div><Button disabled={!['Owner', 'Manager'].includes(user.workspaceRole)}>Label’ları yönet</Button></header></section><section className="settings-section danger-zone"><header><div><h2><TriangleAlert /> Tehlikeli alan</h2><p>Arşivleme geri alınabilir; kalıcı silme farklı bir aksiyondur.</p></div></header><Button icon={<Archive />} disabled={!owner}>Workspace’i arşivle</Button></section></div>;
}
