import { useState, useEffect } from 'react';
import { useNavigate } from '@tanstack/react-router';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { LogIn, Loader2, AlertCircle } from 'lucide-react';
import { login, getStoredUser } from '@/lib/auth';
import { useAuth } from 'react-oidc-context';

export function Login() {
  const navigate = useNavigate();
  const auth = useAuth();
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    // Check if already logged in via direct storage
    const storedUser = getStoredUser();
    if (storedUser && !storedUser.expired) {
      navigate({ to: '/' });
      return;
    }

    // Check if logged in via OIDC
    if (auth.isAuthenticated && auth.user) {
      navigate({ to: '/' });
    }
  }, [navigate, auth.isAuthenticated, auth.user]);

  // Handle OIDC errors
  useEffect(() => {
    if (auth.error) {
      setError(auth.error.message || 'Authentication failed');
      setIsLoading(false);
    }
  }, [auth.error]);

  const handleLogin = async () => {
    setError(null);
    setIsLoading(true);

    try {
      // Use OIDC redirect flow
      await login();
    } catch (err) {
      console.error('Login error:', err);
      setError(err instanceof Error ? err.message : 'An unexpected error occurred');
      setIsLoading(false);
    }
  };

  return (
    <div className="flex items-center justify-center min-h-screen bg-muted/50">
      <Card className="w-full max-w-md">
        <CardHeader className="text-center">
          <CardTitle className="text-2xl">eShop Admin</CardTitle>
          <CardDescription>
            Sign in to access the admin dashboard
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          {error && (
            <div className="flex items-center gap-2 p-3 text-sm text-destructive bg-destructive/10 rounded-md">
              <AlertCircle className="h-4 w-4" />
              {error}
            </div>
          )}

          <Button
            onClick={handleLogin}
            className="w-full"
            size="lg"
            disabled={isLoading || auth.isLoading}
          >
            {isLoading || auth.isLoading ? (
              <>
                <Loader2 className="h-5 w-5 mr-2 animate-spin" />
                Redirecting to login...
              </>
            ) : (
              <>
                <LogIn className="h-5 w-5 mr-2" />
                Sign in with Identity Server
              </>
            )}
          </Button>

          <p className="text-xs text-center text-muted-foreground mt-4">
            You will be redirected to the secure login page
          </p>
        </CardContent>
      </Card>
    </div>
  );
}
