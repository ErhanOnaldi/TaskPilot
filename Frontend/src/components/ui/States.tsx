import type { ReactNode } from 'react';
import { AlertTriangle, Inbox } from 'lucide-react';
import { Button } from './Button';

export function LoadingSkeleton({ rows = 5 }: { rows?: number }) {
  return <div className="skeleton-list" aria-label="Yükleniyor" aria-busy="true">{Array.from({ length: rows }, (_, index) => <div className="skeleton-row" key={index} />)}</div>;
}

export function EmptyState({ title, description, action }: { title: string; description: string; action?: { label: string; onClick: () => void } | undefined }) {
  return (
    <div className="empty-state">
      <Inbox aria-hidden="true" />
      <h2>{title}</h2>
      <p>{description}</p>
      {action ? <Button onClick={action.onClick}>{action.label}</Button> : null}
    </div>
  );
}

export function DeveloperNotice({ children }: { children: ReactNode }) {
  if (!import.meta.env.DEV) return null;
  return <p className="developer-notice" role="note">backend dependency: {children}</p>;
}

export function ProblemState({ error, onRetry }: { error: unknown; onRetry?: () => void }) {
  const message = error instanceof Error ? error.message : 'İçerik yüklenemedi.';
  const correlation = error && typeof error === 'object' && 'correlationId' in error ? String(error.correlationId ?? '') : '';
  return (
    <div className="problem-state" role="alert">
      <AlertTriangle aria-hidden="true" />
      <div><h2>Bir sorun oluştu</h2><p>{message}</p>{correlation ? <code>correlation-id: {correlation}</code> : null}</div>
      {onRetry ? <Button onClick={onRetry}>Yeniden dene</Button> : null}
    </div>
  );
}
