import type { PropsWithChildren, ReactNode } from 'react';

export function PermissionGate({ allowed, fallback = null, children }: PropsWithChildren<{ allowed: boolean; fallback?: ReactNode }>) {
  return allowed ? children : fallback;
}
