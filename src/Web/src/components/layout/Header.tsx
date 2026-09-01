import React from 'react';
import { Bell, Menu } from 'lucide-react';
import { Avatar } from '@/components/ui/Avatar';

export interface HeaderProps {
  pageTitle?: string;
  brandTitle?: string;
  brandSubtitle?: string;
  institutionName?: string;
  userName?: string;
  userRole?: string;
  onMenuToggle?: () => void;
  onNotificationClick?: () => void;
  onProfileClick?: () => void;
  actions?: React.ReactNode;
}

export const Header: React.FC<HeaderProps> = ({
  pageTitle = 'Bugün',
  brandTitle = 'Bilim Akademi',
  brandSubtitle,
  institutionName,
  userName = 'Ayşe Kaya',
  userRole,
  onMenuToggle,
  onNotificationClick,
  onProfileClick,
  actions,
}) => {
  return (
    <header className="sticky top-0 z-30 bg-white/95 backdrop-blur-sm border-b border-neutral-200 h-14 flex items-center px-4 gap-3 flex-shrink-0 select-none">
      {/* Mobile Hamburger */}
      {onMenuToggle && (
        <button
          type="button"
          onClick={onMenuToggle}
          className="md:hidden p-2 -ml-1 rounded-lg text-neutral-600 hover:bg-neutral-100 transition-colors min-h-[44px] min-w-[44px] flex items-center justify-center"
          aria-label="Menüyü Aç"
        >
          <Menu className="h-5 w-5" />
        </button>
      )}

      {/* Page Title on Mobile / Desktop */}
      <span className="md:hidden font-semibold text-neutral-800 text-sm truncate">
        {pageTitle}
      </span>

      {/* Desktop Brand Info */}
      <div className="hidden md:flex items-center gap-2 min-w-0">
        <h1 className="text-sm font-semibold text-neutral-900 truncate">
          {brandTitle}
        </h1>
        {institutionName && (
          <span className="rounded-md bg-navy-50 px-2 py-0.5 text-[10px] font-semibold text-navy-700 border border-navy-100 truncate">
            {institutionName}
          </span>
        )}
        {brandSubtitle && (
          <span className="text-[11px] text-neutral-400 truncate hidden lg:inline">
            • {brandSubtitle}
          </span>
        )}
      </div>

      {/* Right Controls: Notifications & Avatar */}
      <div className="ml-auto flex items-center gap-2">
        {actions}

        <button
          type="button"
          onClick={onNotificationClick}
          className="relative p-2 rounded-full text-neutral-500 hover:bg-neutral-100 transition-colors min-h-[44px] min-w-[44px] flex items-center justify-center"
          aria-label="Bildirimler"
        >
          <Bell className="h-[18px] w-[18px]" />
          <span className="absolute top-2.5 right-2.5 w-2 h-2 bg-danger rounded-full border-2 border-white" />
        </button>

        <button
          type="button"
          onClick={onProfileClick}
          className="flex items-center gap-2 cursor-pointer group focus:outline-none"
          aria-label="Profil"
        >
          <Avatar name={userName} size="sm" variant="primary" />
          <div className="hidden lg:flex flex-col text-left">
            <span className="text-xs font-semibold text-neutral-800 group-hover:text-primary-600 transition-colors leading-tight">
              {userName}
            </span>
            {userRole && (
              <span className="text-[10px] text-neutral-400 leading-tight">
                {userRole}
              </span>
            )}
          </div>
        </button>
      </div>
    </header>
  );
};
