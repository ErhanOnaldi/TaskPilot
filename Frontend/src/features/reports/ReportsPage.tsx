import { CalendarClock, Plus } from 'lucide-react';
import { useState } from 'react';
import { useMutation } from '@tanstack/react-query';
import { reportsApi, type WeeklyReportResponse } from '../../api/endpoints';
import { isDemoMode } from '../../api/dataSource';
import { useAppContext } from '../../app/AppProviders';
import { PageHeader } from '../../components/PageHeader';
import { Button } from '../../components/ui/Button';
import { EmptyState } from '../../components/ui/States';
import { WeeklyReportViewer } from './WeeklyReportViewer';
import { useParams } from 'react-router-dom';

const demoReport: WeeklyReportResponse = { id: 1, projectId: 1, status: 'PendingReview', createdAtUtc: '2026-07-27T09:02:00Z', reviewedAtUtc: null, content: '# Atlas Web Platform — Haftalık Rapor\n\n## İlerleme\nBu hafta 5 görev tamamlandı, 3 görev In Review durumunda bekliyor.\n\n## Risk\nPassword recovery e-posta teslim hataları hâlâ kritik ve açık. Authentication rate limiting atanmamış durumda.\n\n## Öneri\nTP-151 için sahip atayın ve TP-142 üzerinde günlük kontrol noktası tanımlayın.' };

export function ReportsPage() {
  const { projectId = '1' } = useParams();
  const { user } = useAppContext();
  const [report, setReport] = useState<WeeklyReportResponse | null>(isDemoMode ? demoReport : null);
  const create = useMutation({ mutationFn: () => isDemoMode ? Promise.resolve(demoReport) : reportsApi.create(Number(projectId), new Date().toISOString().slice(0, 10)), onSuccess: setReport });
  const review = useMutation({ mutationFn: (approve: boolean) => isDemoMode ? Promise.resolve({ ...report!, status: approve ? 'Approved' as const : 'Rejected' as const, reviewedAtUtc: new Date().toISOString() }) : reportsApi.review(report!.id, approve), onSuccess: setReport });
  return <div className="page"><PageHeader title="Haftalık Raporlar" description="İlerleme, risk ve iş yükü özeti; insan onayı olmadan yayımlanmaz." actions={<><Button icon={<CalendarClock />}>Takvim ayarı</Button><Button variant="primary" icon={<Plus />} onClick={() => create.mutate()} disabled={create.isPending}>{create.isPending ? 'Hazırlanıyor…' : 'Rapor oluştur'}</Button></>} />{report ? <WeeklyReportViewer report={report} canReview={['Owner', 'Manager'].includes(user.workspaceRole)} onReview={(approve) => review.mutate(approve)} /> : <EmptyState title="Rapor geçmişi henüz kullanılamıyor" description="Bir rapor oluşturabilir veya bildirimdeki rapor bağlantısından bu ekrana gelebilirsiniz." />}</div>;
}
