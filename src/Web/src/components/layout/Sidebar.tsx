import React from 'react';
import { Layers, X } from 'lucide-react';
import { cn } from '@/lib/utils';
import { NavItem } from './BottomNav';

export interface SidebarProps {
  items: NavItem[];
  activeId: string;
  onChange: (id: string) => void;
  brandTitle?: string;
  brandSubtitle?: string;
  isOpen?: boolean;
  onClose?: () => void;
  className?: string;
  footerContent?: React.ReactNode;
}

export const Sidebar: React.FC<SidebarProps> = ({
  items,
  activeId,
  onChange,
  brandTitle = 'Bilim Akademi',
  brandSubtitle,
  isOpen = false,
  onClose,
  className,
  footerContent,
}) => {
  return (
    <>
      {/* Desktop Sidebar */}
      <aside
        className={cn(
          'hidden md:flex md:w-64 md:flex-col md:fixed md:inset-y-0 bg-navy-900 border-r border-navy-800 z-30 select-none',
          className
        )}
      >
        {/* Brand Header */}
        <div className="flex h-16 items-center px-5 gap-3 border-b border-navy-800">
          <div className="w-8 h-8 rounded-lg bg-primary-600 flex items-center justify-center text-white flex-shrink-0">
            <Layers className="h-4 w-4" />
          </div>
          <div className="flex flex-col min-w-0">
            <span className="text-sm font-bold text-white tracking-tight truncate">
              {brandTitle}
            </span>
            {brandSubtitle && (
              <span className="text-[10px] text-navy-400 truncate">
                {brandSubtitle}
              </span>
            )}
          </div>
        </div>

        {/* Navigation Links */}
        <nav className="flex-1 overflow-y-auto px-3 py-4 space-y-1">
          {items.map((item) => {
            const isActive = item.id === activeId;
            return (
              <button
                key={item.id}
                type="button"
                onClick={() => onChange(item.id)}
                className={cn(
                  'flex w-full items-center justify-between px-3 py-2.5 rounded-xl text-xs font-medium transition-colors text-left min-h-[44px]',
                  isActive
                    ? 'bg-navy-700 text-white font-semibold shadow-soft-sm'
                    : 'text-navy-200 hover:bg-navy-800 hover:text-white'
                )}
              >
                <div className="flex items-center gap-3 min-w-0">
                  <span className={cn('flex items-center justify-center flex-shrink-0', isActive ? 'text-primary-400' : 'text-navy-300')}>
                    {item.icon}
                  </span>
                  <span className="truncate">{item.label}</span>
                </div>
                {item.badge !== undefined && (
                  <span
                    className={cn(
                      'flex h-5 min-w-5 items-center justify-center rounded-full px-1.5 text-[10px] font-semibold',
                      isActive
                        ? 'bg-primary-600 text-white'
                        : 'bg-navy-800 text-navy-200 border border-navy-700'
                    )}
                  >
                    {item.badge}
                  </span>
                )}
              </button>
            );
          })}
        </nav>

        {/* Sidebar Footer */}
        {footerContent && (
          <div className="p-3 border-t border-navy-800 mt-auto bg-navy-950/50">
            {footerContent}
          </div>
        )}
      </aside>

      {/* Mobile Drawer (When Open) */}
      {isOpen && (
        <div className="md:hidden fixed inset-0 z-50 flex">
          <div className="w-64 bg-navy-900 flex flex-col shadow-2xl z-10 animate-in slide-in-from-left duration-200">
            <div className="flex items-center justify-between px-4 h-14 border-b border-navy-800">
              <div className="flex items-center gap-2.5">
                <div className="w-7 h-7 rounded-lg bg-primary-600 flex items-center justify-center text-white">
                  <Layers className="h-4 w-4" />
                </div>
                <span className="text-white font-semibold text-sm">{brandTitle}</span>
              </div>
              {onClose && (
                <button
                  type="button"
                  onClick={onClose}
                  className="p-1.5 rounded-lg text-navy-300 hover:text-white hover:bg-navy-800"
                  aria-label="Menüyü Kapat"
                >
                  <X className="h-5 w-5" />
                </button>
              )}
            </div>

            <nav className="flex-1 px-3 py-3 space-y-1 overflow-y-auto">
              {items.map((item) => {
                const isActive = item.id === activeId;
                return (
                  <button
                    key={item.id}
                    type="button"
                    onClick={() => {
                      onChange(item.id);
                      if (onClose) onClose();
                    }}
                    className={cn(
                      'flex items-center gap-3 px-3 py-2.5 rounded-xl w-full text-left min-h-[44px]',
                      isActive
                        ? 'bg-navy-700 text-white font-semibold'
                        : 'text-navy-200 hover:bg-navy-800 hover:text-white'
                    )}
                  >
                    <span className={isActive ? 'text-primary-400' : 'text-navy-300'}>
                      {item.icon}
                    </span>
                    <span className="text-sm font-medium">{item.label}</span>
                  </button>
                );
              })}
            </nav>

            {footerContent && (
              <div className="p-3 border-t border-navy-800 mt-auto">
                {footerContent}
              </div>
            )}
          </div>
          <div
            className="flex-1 bg-black/50 backdrop-blur-sm"
            onClick={onClose}
            aria-hidden="true"
          />
        </div>
      )}
    </>
  );
};
