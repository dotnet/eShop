import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ReactQueryDevtools } from '@tanstack/react-query-devtools';
import { RouterProvider } from '@tanstack/react-router';
import { AuthProvider, useAuth } from 'react-oidc-context';
import { oidcConfig, getStoredUser } from '@/lib/auth';
import { router } from '@/routes';
import { Login } from '@/pages/Login';
import { Loader2 } from 'lucide-react';
import { createContext, useContext, useState, useEffect, type ReactNode } from 'react';
import type { User } from 'oidc-client-ts';

interface DirectAuthContextType {
  user: User | null;
  setUser: (user: User | null) => void;
}

const DirectAuthContext = createContext<DirectAuthContextType>({
  user: null,
  setUser: () => {},
});

export const useDirectAuth = () => useContext(DirectAuthContext);

function DirectAuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(() => getStoredUser());

  useEffect(() => {
    const handleStorageChange = () => {
      setUser(getStoredUser());
    };
    window.addEventListener('storage', handleStorageChange);
    return () => window.removeEventListener('storage', handleStorageChange);
  }, []);

  return (
    <DirectAuthContext.Provider value={{ user, setUser }}>
      {children}
    </DirectAuthContext.Provider>
  );
}

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 1000 * 60 * 5, // 5 minutes
      retry: 1,
    },
  },
});

function AuthenticatedApp() {
  const auth = useAuth();
  const { user: directUser } = useDirectAuth();

  if (auth.isLoading) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <Loader2 className="h-8 w-8 animate-spin text-primary" />
      </div>
    );
  }

  // Allow access to callback and login routes
  if (window.location.pathname === '/callback' || window.location.pathname === '/login') {
    return <RouterProvider router={router} />;
  }

  // Check both OIDC auth and direct login
  const isAuthenticated = auth.isAuthenticated || (directUser && !directUser.expired);

  if (!isAuthenticated) {
    return <Login />;
  }

  return <RouterProvider router={router} />;
}

function App() {
  return (
    <DirectAuthProvider>
      <AuthProvider {...oidcConfig}>
        <QueryClientProvider client={queryClient}>
          <AuthenticatedApp />
          <ReactQueryDevtools initialIsOpen={false} />
        </QueryClientProvider>
      </AuthProvider>
    </DirectAuthProvider>
  );
}

export default App;
