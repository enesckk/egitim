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
  unreadCount?: number;
  onMenuToggle?: () => void;
  onNotificationClick?: () => void;
  onProfileClick?: () => void;
  actions?: React.ReactNode;
  isStudent?: boolean;
}

export const Header: React.FC<HeaderProps> = ({
  pageTitle = 'Bugün',
  brandTitle = 'Bilim Akademi',
  brandSubtitle,
  institutionName,
  userName = 'Ayşe Kaya',
  userRole,
  unreadCount,
  onMenuToggle,
  onNotificationClick,
  onProfileClick,
  actions,
  isStudent = false,
}) => {
  if (isStudent) {
    return (
      <header className="sticky top-0 z-30 bg-[#F8F7F3]/95 backdrop-blur-md border-b border-neutral-200/60 h-16 flex items-center justify-between px-4 sm:px-6 select-none transition-all">
        {/* Left: Avatar + "Hoş geldin," + Student Full Name */}
        <button
          type="button"
          onClick={onProfileClick}
          className="flex items-center gap-3 cursor-pointer group text-left focus:outline-none"
          aria-label="Profilimi Aç"
        >
          <div className="w-10 h-10 rounded-full bg-[#14233E] text-white flex items-center justify-center font-bold text-sm shadow-sm group-hover:scale-105 transition-transform">
            {userName ? userName.charAt(0).toUpperCase() : 'Ö'}
          </div>
          <div className="flex flex-col">
            <span className="text-[11px] text-neutral-500 font-medium leading-none">Hoş geldin,</span>
            <span className="text-sm sm:text-base font-bold text-[#14233E] mt-0.5 leading-tight group-hover:text-[#315FE8] transition-colors">
              {userName && !userName.includes('@') && !userName.includes('.demo') ? userName : 'Ahmet Yılmaz'}
            </span>
          </div>
        </button>

        {/* Right: Notification Bell */}
        <div className="flex items-center gap-2">
          {actions}
          <button
            type="button"
            onClick={onNotificationClick}
            className="relative p-2.5 rounded-full bg-white border border-neutral-200/70 text-neutral-600 hover:text-neutral-900 hover:bg-neutral-50 transition-all shadow-xs min-h-[44px] min-w-[44px] flex items-center justify-center"
            aria-label="Bildirimler"
          >
            <Bell className="h-4 w-4" />
            {typeof unreadCount === 'number' && unreadCount > 0 && (
              <span className="absolute top-1 right-1 w-2.5 h-2.5 bg-red-500 rounded-full ring-2 ring-white" />
            )}
          </button>
        </div>
      </header>
    );
  }
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
          {typeof unreadCount === 'number' && unreadCount > 0 && (
            <span className="absolute -top-0.5 -right-0.5 min-w-[18px] h-[18px] px-1 bg-danger text-white text-[10px] font-bold rounded-full flex items-center justify-center border-2 border-white">
              {unreadCount > 99 ? '99+' : unreadCount}
            </span>
          )}
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
