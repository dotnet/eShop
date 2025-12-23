import { Link, useLocation, useNavigate } from '@tanstack/react-router';
import { Warehouse, Package, LayoutDashboard, LogOut } from 'lucide-react';
import { useAuth } from 'react-oidc-context';
import { cn } from '@/lib/utils';
import { useDirectAuth } from '@/App';
import { logoutDirect } from '@/lib/auth';

const navigation = [
  { name: 'Dashboard', href: '/', icon: LayoutDashboard },
  { name: 'Warehouses', href: '/warehouses', icon: Warehouse },
  { name: 'Inventory', href: '/inventory', icon: Package },
];

export function Sidebar() {
  const location = useLocation();
  const navigate = useNavigate();
  const auth = useAuth();
  const { user: directUser, setUser } = useDirectAuth();

  const currentUser = directUser || auth.user;

  const handleSignOut = () => {
    if (directUser) {
      logoutDirect();
      setUser(null);
      navigate({ to: '/login' });
    } else {
      auth.signoutRedirect();
    }
  };

  return (
    <div className="flex h-full w-64 flex-col bg-card border-r">
      <div className="flex h-16 items-center px-6 border-b">
        <h1 className="text-xl font-bold">eShop Admin</h1>
      </div>

      <nav className="flex-1 space-y-1 px-3 py-4">
        {navigation.map((item) => {
          const isActive = location.pathname === item.href;
          return (
            <Link
              key={item.name}
              to={item.href}
              className={cn(
                'flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition-colors',
                isActive
                  ? 'bg-primary text-primary-foreground'
                  : 'text-muted-foreground hover:bg-accent hover:text-accent-foreground'
              )}
            >
              <item.icon className="h-5 w-5" />
              {item.name}
            </Link>
          );
        })}
      </nav>

      <div className="border-t p-4">
        <div className="flex items-center gap-3 mb-3">
          <div className="h-8 w-8 rounded-full bg-primary flex items-center justify-center text-primary-foreground text-sm font-medium">
            {currentUser?.profile.name?.charAt(0).toUpperCase() || 'A'}
          </div>
          <div className="flex-1 min-w-0">
            <p className="text-sm font-medium truncate">
              {currentUser?.profile.name || 'Admin'}
            </p>
            <p className="text-xs text-muted-foreground truncate">
              {currentUser?.profile.email}
            </p>
          </div>
        </div>
        <button
          onClick={handleSignOut}
          className="flex w-full items-center gap-2 rounded-md px-3 py-2 text-sm text-muted-foreground hover:bg-accent hover:text-accent-foreground transition-colors"
        >
          <LogOut className="h-4 w-4" />
          Sign out
        </button>
      </div>
    </div>
  );
}
