import type { ReactNode } from 'react';

export function MetricCard({ label, value, icon, tone = 'default', hint }: { label: string; value: string; icon?: ReactNode; tone?: string; hint?: string }) {
  return <article className={`metric-card metric-card--${tone}`} title={hint}><div><span>{label}</span><strong>{value}</strong></div>{icon ? <span className="metric-card__icon">{icon}</span> : null}</article>;
}
