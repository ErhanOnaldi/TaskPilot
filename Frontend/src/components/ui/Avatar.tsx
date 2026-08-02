import { initials } from '../../lib/formatters';

export function MemberAvatar({ name, size = 'md' }: { name: string; size?: 'sm' | 'md' }) {
  return <span className={`avatar avatar--${size}`} title={name} aria-label={name}>{initials(name)}</span>;
}
