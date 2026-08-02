import { Bell, CheckCheck } from 'lucide-react';
import { useState } from 'react';
import { useMarkAllNotificationsRead, useNotifications } from '../../api/dataSource';
import { PageHeader } from '../../components/PageHeader';
import { Button } from '../../components/ui/Button';
import { EmptyState, LoadingSkeleton, ProblemState } from '../../components/ui/States';
import { formatDate } from '../../lib/formatters';

export function NotificationCenter() {
  const query = useNotifications();
  const markAll = useMarkAllNotificationsRead();
  const [unreadOnly, setUnreadOnly] = useState(false);
  if (query.isLoading) return <LoadingSkeleton rows={8} />;
  if (query.error) return <ProblemState error={query.error} onRetry={() => void query.refetch()} />;
  const items = (query.data?.items ?? []).filter((item) => !unreadOnly || !item.isRead);
  return <div className="page"><PageHeader title="Bildirimler" description="Görev, yorum, AI ve rapor güncellemeleri." actions={<Button icon={<CheckCheck />} onClick={() => markAll.mutate()} disabled={markAll.isPending}>Tümünü okundu işaretle</Button>} /><div className="filter-bar"><div className="segmented-control"><button className={!unreadOnly ? 'is-active' : ''} onClick={() => setUnreadOnly(false)}>Tümü</button><button className={unreadOnly ? 'is-active' : ''} onClick={() => setUnreadOnly(true)}>Okunmamış</button></div><span className="result-count">{items.length} bildirim</span></div>{items.length ? <div className="notification-list">{items.map((item) => <article className={item.isRead ? '' : 'is-unread'} key={item.id}><span className="notification-icon"><Bell /></span><div><span className="eyebrow">{item.type.replace(/([a-z])([A-Z])/g, '$1 $2').toUpperCase()}</span><h2>{item.title}</h2><p>{item.message}</p></div><time>{formatDate(item.createdAt, { day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' })}</time>{!item.isRead ? <span className="unread-dot" aria-label="Okunmamış" /> : null}</article>)}</div> : <EmptyState title="Burada yeni bir şey yok" description={unreadOnly ? 'Tüm bildirimlerinizi okudunuz.' : 'Bildirimler oluştuğunda burada görünecek.'} action={unreadOnly ? { label: 'Tümünü göster', onClick: () => setUnreadOnly(false) } : undefined} />}</div>;
}
