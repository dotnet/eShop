import { Link, useLocation } from '@tanstack/react-router';
import { LayoutDashboard, Package, ClipboardList, History, LogOut, Truck } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { logoutDirect } from '@/lib/auth';
import { useDirectAuth } from '@/App';

const navItems = [
  { path: '/', label: 'Dashboard', icon: LayoutDashboard },
  { path: '/assigned', label: 'Assigned Orders', icon: Package },
  { path: '/available', label: 'Available Orders', icon: ClipboardList },
  { path: '/history', label: 'History', icon: History },
];

export function Sidebar() {
  const location = useLocation();
  const { setUser } = useDirectAuth();

  const handleLogout = () => {
    logoutDirect();
    setUser(null);
  };

  return (
    <aside className="fixed left-0 top-0 h-full w-64 bg-card border-r flex flex-col">
      <div className="p-6 border-b">
        <div className="flex items-center gap-2">
          <Truck className="h-8 w-8 text-primary" />
          <div>
            <h1 className="text-xl font-bold">eShop</h1>
            <p className="text-xs text-muted-foreground">Shipper Portal</p>
          </div>
        </div>
      </div>

      <nav className="flex-1 p-4 space-y-1">
        {navItems.map((item) => {
          const isActive = location.pathname === item.path;
          const Icon = item.icon;

          return (
            <Link
              key={item.path}
              to={item.path}
              className={`flex items-center gap-3 px-3 py-2 rounded-md transition-colors ${
                isActive
                  ? 'bg-primary text-primary-foreground'
                  : 'hover:bg-muted'
              }`}
            >
              <Icon className="h-5 w-5" />
              {item.label}
            </Link>
          );
        })}
      </nav>

      <div className="p-4 border-t">
        <Button
          variant="ghost"
          className="w-full justify-start gap-3"
          onClick={handleLogout}
        >
          <LogOut className="h-5 w-5" />
          Sign Out
        </Button>
      </div>
    </aside>
  );
}
