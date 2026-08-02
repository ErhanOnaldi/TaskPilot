/* eslint-disable react-refresh/only-export-components */
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { createContext, useContext, useEffect, useMemo, useState, type PropsWithChildren } from 'react';
import type { UserContext } from '../types/domain';
import { isDemoMode } from '../api/dataSource';
import { getSessionUser, subscribeSession } from '../api/apiClient';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: { staleTime: 30_000, retry: (count, error) => !(error instanceof Error && 'status' in error && [401, 403, 404].includes(Number(error.status))) && count < 2 },
    mutations: { retry: false },
  },
});

interface AppContextValue {
  user: UserContext;
  theme: 'dark' | 'light';
  toggleTheme: () => void;
  setUserRoles: (workspaceRole: UserContext['workspaceRole'], projectRole: UserContext['projectRole']) => void;
  setAuthenticatedUser: (id: number, email: string) => void;
}

const AppContext = createContext<AppContextValue | null>(null);

function readTheme(): 'dark' | 'light' {
  const stored = localStorage.getItem('taskpilot-theme');
  if (stored === 'dark' || stored === 'light') return stored;
  return window.matchMedia('(prefers-color-scheme: light)').matches ? 'light' : 'dark';
}

export function AppProviders({ children }: PropsWithChildren) {
  const [theme, setTheme] = useState(readTheme);
  const sessionUser = getSessionUser();
  const [user, setUser] = useState<UserContext>(isDemoMode
    ? { id: 1, email: 'elif@northstar.studio', workspaceRole: 'Owner', projectRole: 'ProjectManager' }
    : { id: sessionUser?.id ?? 0, email: sessionUser?.email ?? '', workspaceRole: 'Guest', projectRole: 'Guest' });
  useEffect(() => subscribeSession(() => {
    const next = getSessionUser();
    setUser((current) => next
      ? { ...current, id: next.id, email: next.email }
      : { id: 0, email: '', workspaceRole: 'Guest', projectRole: 'Guest' });
  }), []);
  const value = useMemo<AppContextValue>(() => ({
    user,
    theme,
    toggleTheme: () => setTheme((current) => {
      const next = current === 'dark' ? 'light' : 'dark';
      localStorage.setItem('taskpilot-theme', next);
      return next;
    }),
    setUserRoles: (workspaceRole, projectRole) => setUser((current) => ({ ...current, workspaceRole, projectRole })),
    setAuthenticatedUser: (id, email) => setUser({ id, email, workspaceRole: 'Guest', projectRole: 'Guest' }),
  }), [theme, user]);

  return (
    <QueryClientProvider client={queryClient}>
      <AppContext.Provider value={value}>
        <div data-theme={theme}>{children}</div>
      </AppContext.Provider>
    </QueryClientProvider>
  );
}

export function useAppContext() {
  const value = useContext(AppContext);
  if (!value) throw new Error('useAppContext must be used inside AppProviders');
  return value;
}
