import { useEffect } from 'react';
import { useNavigate } from '@tanstack/react-router';
import { useAuth } from 'react-oidc-context';
import { Loader2 } from 'lucide-react';

export function Callback() {
  const auth = useAuth();
  const navigate = useNavigate();

  useEffect(() => {
    if (!auth.isLoading && auth.isAuthenticated) {
      navigate({ to: '/' });
    }
  }, [auth.isLoading, auth.isAuthenticated, navigate]);

  if (auth.error) {
    return (
      <div className="flex flex-col items-center justify-center min-h-screen">
        <p className="text-destructive mb-4">Authentication failed: {auth.error.message}</p>
        <button
          onClick={() => navigate({ to: '/login' })}
          className="text-primary underline"
        >
          Go to login
        </button>
      </div>
    );
  }

  return (
    <div className="flex flex-col items-center justify-center min-h-screen">
      <Loader2 className="h-8 w-8 animate-spin text-primary mb-4" />
      <p className="text-muted-foreground">Completing sign in...</p>
    </div>
  );
}
