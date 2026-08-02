import { Navigate, useParams } from 'react-router-dom';
import { useProjects } from '../../api/dataSource';
import { LoadingSkeleton } from '../../components/ui/States';

export function MyTasksPage() {
  const { workspaceId = '1' } = useParams();
  const projects = useProjects(Number(workspaceId));
  if (projects.isLoading) return <LoadingSkeleton rows={8} />;
  const project = projects.data?.items.find((item) => item.status === 'Active');
  return project ? <Navigate replace to={`/w/${workspaceId}/projects/${project.id}/tasks`} /> : <Navigate replace to={`/w/${workspaceId}/projects`} />;
}
