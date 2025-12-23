import { useEffect } from 'react';
import { useNavigate } from '@tanstack/react-router';
import { handleCallback } from '@/lib/auth';
import { Loader2 } from 'lucide-react';

export function Callback() {
  const navigate = useNavigate();

  useEffect(() => {
    handleCallback()
      .then(() => {
        navigate({ to: '/' });
      })
      .catch((error) => {
        console.error('Callback error:', error);
        navigate({ to: '/login' });
      });
  }, [navigate]);

  return (
    <div className="flex items-center justify-center min-h-screen">
      <Loader2 className="h-8 w-8 animate-spin text-primary" />
    </div>
  );
}
