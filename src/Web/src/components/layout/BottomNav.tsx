import React from 'react';
import { cn } from '@/lib/utils';

export interface NavItem {
  id: string;
  label: string;
  icon: React.ReactNode;
  activeIcon?: React.ReactNode;
  badge?: string | number;
}

export interface BottomNavProps {
  items: NavItem[];
  activeId: string;
  onChange: (id: string) => void;
  className?: string;
}

export const BottomNav: React.FC<BottomNavProps> = ({
  items,
  activeId,
  onChange,
  className,
}) => {
  return (
    <nav
      className={cn(
        'fixed bottom-0 inset-x-0 z-40 flex items-center justify-around border-t border-neutral-200 bg-white px-1 pb-safe md:hidden shadow-soft-md',
        className
      )}
    >
      {items.map((item) => {
        const isActive = item.id === activeId;
        return (
          <button
            key={item.id}
            type="button"
            onClick={() => onChange(item.id)}
            className={cn(
              'flex flex-1 flex-col items-center justify-center py-2 px-1 min-h-[52px] transition-colors select-none relative',
              isActive
                ? 'text-primary-600 font-medium'
                : 'text-neutral-400 hover:text-neutral-600 font-normal'
            )}
            aria-current={isActive ? 'page' : undefined}
          >
            <div className="relative flex items-center justify-center">
              {isActive && item.activeIcon ? item.activeIcon : item.icon}
              {item.badge !== undefined && (
                <span className="absolute -top-1.5 -right-2.5 flex h-4 min-w-4 items-center justify-center rounded-full bg-danger px-1 text-[9px] font-bold text-white">
                  {item.badge}
                </span>
              )}
            </div>
            <span className="text-[10px] mt-1 leading-tight">{item.label}</span>
          </button>
        );
      })}
    </nav>
  );
};
