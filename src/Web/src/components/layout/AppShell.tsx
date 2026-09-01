import React, { useState } from 'react';
import { cn } from '@/lib/utils';
import { Header, HeaderProps } from './Header';
import { BottomNav, NavItem } from './BottomNav';
import { Sidebar } from './Sidebar';

export type UserRole = 'student' | 'coach' | 'teacher' | 'parent' | 'admin';

export interface AppShellProps {
  children: React.ReactNode;
  role?: UserRole;
  navItems?: NavItem[];
  activeNavId?: string;
  onNavChange?: (id: string) => void;
  headerProps?: HeaderProps;
  showSidebar?: boolean;
  showBottomNav?: boolean;
  className?: string;
  sidebarFooter?: React.ReactNode;
}

export const AppShell: React.FC<AppShellProps> = ({
  children,
  role = 'student',
  navItems = [],
  activeNavId = '',
  onNavChange = () => {},
  headerProps,
  showSidebar = true,
  showBottomNav = true,
  className,
  sidebarFooter,
}) => {
  const [isDrawerOpen, setIsDrawerOpen] = useState(false);
  const hasNav = navItems.length > 0;

  // Student and Parent use mobile bottom nav; Coach/Teacher/Admin use drawer/sidebar
  const isMobileBottomNavRole = role === 'student' || role === 'parent';

  return (
    <div className="min-h-screen bg-surface flex flex-col text-neutral-900 font-sans">
      {/* Desktop Sidebar (and Mobile Drawer for non-student or when toggled) */}
      {showSidebar && hasNav && (
        <Sidebar
          items={navItems}
          activeId={activeNavId}
          onChange={onNavChange}
          isOpen={isDrawerOpen}
          onClose={() => setIsDrawerOpen(false)}
          brandTitle={headerProps?.brandTitle || 'Bilim Akademi'}
          brandSubtitle={headerProps?.institutionName || 'Eğitim Platformu'}
          footerContent={sidebarFooter}
        />
      )}

      {/* Main Content Area */}
      <div
        className={cn(
          'flex flex-1 flex-col min-w-0 transition-all',
          showSidebar && hasNav && 'md:pl-64'
        )}
      >
        {/* Header */}
        <Header
          {...headerProps}
          onMenuToggle={hasNav ? () => setIsDrawerOpen(true) : undefined}
        />

        {/* Page Content */}
        <main
          className={cn(
            'flex-1 w-full max-w-5xl mx-auto px-4 sm:px-6 py-3.5 sm:py-6',
            isMobileBottomNavRole && showBottomNav && hasNav && 'pb-16 md:pb-6',
            className
          )}
        >
          {children}
        </main>

        {/* Mobile Bottom Navigation for Student / Parent */}
        {isMobileBottomNavRole && showBottomNav && hasNav && (
          <BottomNav
            items={navItems}
            activeId={activeNavId}
            onChange={onNavChange}
          />
        )}
      </div>
    </div>
  );
};
