import { MoreHorizontal, Plus, UsersRound } from 'lucide-react';
import { useParams } from 'react-router-dom';
import { useAppContext } from '../../app/AppProviders';
import { useMembers } from '../../api/dataSource';
import { PageHeader } from '../../components/PageHeader';
import { MemberAvatar } from '../../components/ui/Avatar';
import { Button } from '../../components/ui/Button';
import { LoadingSkeleton, ProblemState } from '../../components/ui/States';
import { formatDate } from '../../lib/formatters';

export function MembersPage() {
  const { workspaceId = '1' } = useParams();
  const { user } = useAppContext();
  const members = useMembers(Number(workspaceId));
  if (members.isLoading) return <LoadingSkeleton rows={8} />;
  if (members.error) return <ProblemState error={members.error} onRetry={() => void members.refetch()} />;
  const canInvite = ['Owner', 'Manager'].includes(user.workspaceRole);
  return <div className="page"><PageHeader title="Üyeler" description={`${members.data?.totalCount ?? 0} workspace üyesi`} actions={canInvite ? <Button variant="primary" icon={<Plus />}>Üye davet et</Button> : undefined} /><div className="member-table"><header><span>Üye</span><span>Workspace rolü</span><span>Proje rolü</span><span>Katıldı</span><span /></header>{members.data?.items.map((member) => <article key={member.userId}><div><MemberAvatar name={member.email} /><span><strong>{member.email.split('@')[0]}</strong><small>{member.email}</small></span></div><span>{member.role}</span><span>{member.projectRole ?? '—'}</span><time>{formatDate(member.joinedAt)}</time><button className="icon-button" aria-label={`${member.email} üye işlemleri`} disabled={user.workspaceRole !== 'Owner'}><MoreHorizontal /></button></article>)}</div><div className="permission-note"><UsersRound /><div><strong>Yetki backend tarafından doğrulanır</strong><p>Arayüz uygun olmayan aksiyonları gizler; nihai karar her zaman API’dedir.</p></div></div></div>;
}
