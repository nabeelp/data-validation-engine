import { createContext, useContext, useEffect, useState, type ReactNode } from 'react';
import { getMe, type UserInfo } from '../services/api';

interface AuthState {
  user: UserInfo | null;
  loading: boolean;
  error: boolean;
}

const AuthContext = createContext<AuthState>({ user: null, loading: true, error: false });

export function AuthProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<AuthState>({ user: null, loading: true, error: false });

  useEffect(() => {
    getMe()
      .then((user) => setState({ user, loading: false, error: false }))
      .catch(() => setState({ user: null, loading: false, error: true }));
  }, []);

  return <AuthContext.Provider value={state}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  return useContext(AuthContext);
}
