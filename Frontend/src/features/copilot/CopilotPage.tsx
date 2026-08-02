import { useParams } from 'react-router-dom';
import { useProjects } from '../../api/dataSource';
import { CopilotPanel } from './CopilotPanel';

export function CopilotPage() {
  const { workspaceId = '1', projectId = '1' } = useParams();
  const project = useProjects(Number(workspaceId)).data?.items.find((item) => item.id === Number(projectId));
  return <CopilotPanel projectId={Number(projectId)} projectName={project?.name ?? 'Proje Copilot'} />;
}
