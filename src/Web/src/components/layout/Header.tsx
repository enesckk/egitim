import React from 'react';
import { Bell, Menu } from 'lucide-react';
import { Avatar } from '@/components/ui/Avatar';
import { IconButton } from '@/components/ui/IconButton';

export interface HeaderProps {
  title?: string;
  subtitle?: string;
  institutionName?: string;
  userName?: string;
  userRole?: string;
  onMenuToggle?: () => void;
  onNotificationClick?: () => void;
  onProfileClick?: () => void;
  actions?: React.ReactNode;
}

export const Header: React.FC<HeaderProps> = ({
  title = 'Bilim Akademi',
  subtitle,
  institutionName,
  userName = 'Kullanıcı',
  userRole,
  onMenuToggle,
  onNotificationClick,
  onProfileClick,
  actions,
}) => {
  return (
    <header className="sticky top-0 z-30 flex h-14 md:h-16 w-full items-center justify-between border-b border-neutral-100 bg-white/95 px-4 backdrop-blur md:px-6">
      <div className="flex items-center gap-3 min-w-0">
        {onMenuToggle && (
          <button
            type="button"
            onClick={onMenuToggle}
            className="md:hidden p-2 rounded-lg text-neutral-600 hover:bg-neutral-100 min-h-[44px] min-w-[44px] flex items-center justify-center -ml-2"
            aria-label="Menüyü Aç"
          >
            <Menu className="h-5 w-5" />
          </button>
        )}

        <div className="min-w-0">
          <div className="flex items-center gap-2">
            <h1 className="text-sm md:text-base font-semibold text-neutral-900 truncate">
              {title}
            </h1>
            {institutionName && (
              <span className="hidden sm:inline-block rounded-md bg-navy-50 px-2 py-0.5 text-[10px] font-semibold text-navy-700 border border-navy-100">
                {institutionName}
              </span>
            )}
          </div>
          {subtitle && (
            <p className="text-[11px] text-neutral-400 truncate hidden sm:block">
              {subtitle}
            </p>
          )}
        </div>
      </div>

      <div className="flex items-center gap-2 md:gap-3">
        {actions}

        <IconButton
          variant="ghost"
          size="sm"
          ariaLabel="Bildirimler"
          onClick={onNotificationClick}
          className="relative text-neutral-500 hover:text-neutral-900"
        >
          <Bell className="h-4 w-4" />
          <span className="absolute top-2 right-2 h-2 w-2 rounded-full bg-danger border-2 border-white" />
        </IconButton>

        <button
          type="button"
          onClick={onProfileClick}
          className="flex items-center gap-2 pl-2 border-l border-neutral-100 cursor-pointer group focus:outline-none"
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
