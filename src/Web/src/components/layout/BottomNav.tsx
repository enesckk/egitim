import React, { useState } from 'react';
import { cn } from '@/lib/utils';
import {
  FileText,
  BookMarked,
  FolderArchive,
  MessageSquare,
  X,
  ChevronRight,
  MoreHorizontal
} from 'lucide-react';

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
  isStudent?: boolean;
}

export const BottomNav: React.FC<BottomNavProps> = ({
  items,
  activeId,
  onChange,
  className,
  isStudent = false,
}) => {
  const [isMoreOpen, setIsMoreOpen] = useState(false);

  // If not student, keep original standard bottom navigation
  if (!isStudent) {
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
  }

  // Final Approved Student Mobile Floating Capsule Navigation:
  // Exactly 4 tabs: Bugün | Planım | Gelişim | Diğer
  const studentPrimaryTabs = [
    { id: 'today', label: 'Bugün', iconName: 'today' },
    { id: 'plans', label: 'Planım', iconName: 'plans' },
    { id: 'academic', label: 'Gelişim', iconName: 'academic' },
    { id: 'more', label: 'Diğer', iconName: 'more' },
  ];

  const handleTabClick = (tabId: string) => {
    if (tabId === 'more') {
      setIsMoreOpen(true);
    } else {
      setIsMoreOpen(false);
      onChange(tabId);
    }
  };

  const moreMenuItems = [
    { id: 'exams', label: 'Denemeler & Sınavlar', desc: 'Sonuçlar, netler ve karneler', icon: FileText, color: 'text-blue-600 bg-blue-50' },
    { id: 'notebook', label: 'Ders Defterim', desc: 'Ders notları, özetler ve soru günlükleri', icon: BookMarked, color: 'text-amber-600 bg-amber-50' },
    { id: 'materials', label: 'Ders Materyalleri', desc: 'Öğretmenlerin paylaştığı fasikül ve dokümanlar', icon: FolderArchive, color: 'text-indigo-600 bg-indigo-50' },
    { id: 'messages', label: 'Mesajlar & İletişim', desc: 'Koç ve öğretmenlerle güvenli mesajlaşma', icon: MessageSquare, color: 'text-emerald-600 bg-emerald-50' },
  ];

  const isMoreActive = ['exams', 'notebook', 'materials', 'messages'].includes(activeId);

  return (
    <>
      {/* Floating Capsule Bottom Nav */}
      <div className="fixed bottom-4 inset-x-0 z-40 flex justify-center px-3 md:hidden pointer-events-none pb-safe">
        <nav
          className={cn(
            'pointer-events-auto bg-white/95 backdrop-blur-md rounded-full shadow-[0_8px_30px_rgb(0,0,0,0.12)] border border-neutral-200/80 p-1.5 flex items-center gap-1 max-w-[370px] w-full justify-between',
            className
          )}
        >
          {studentPrimaryTabs.map((tab) => {
            const isActive = tab.id === 'more' ? isMoreActive : activeId === tab.id;

            return (
              <button
                key={tab.id}
                type="button"
                onClick={() => handleTabClick(tab.id)}
                className={cn(
                  'flex-1 flex items-center justify-center gap-1.5 py-2 px-2.5 rounded-full text-xs font-medium transition-all select-none min-h-[44px]',
                  isActive
                    ? 'bg-[#14233E] text-white shadow-sm'
                    : 'text-neutral-500 hover:text-neutral-800 hover:bg-neutral-100/60'
                )}
                aria-current={isActive ? 'page' : undefined}
              >
                {tab.id === 'today' && (
                  <svg className="w-4 h-4 fill-current shrink-0" viewBox="0 0 24 24">
                    <path d="M10 20v-6h4v6h5v-8h3L12 3 2 12h3v8z" />
                  </svg>
                )}
                {tab.id === 'plans' && (
                  <svg className="w-4 h-4 fill-none stroke-current stroke-2 shrink-0" viewBox="0 0 24 24">
                    <rect x="3" y="4" width="18" height="18" rx="2" ry="2" />
                    <line x1="16" y1="2" x2="16" y2="6" />
                    <line x1="8" y1="2" x2="8" y2="6" />
                    <line x1="3" y1="10" x2="21" y2="10" />
                  </svg>
                )}
                {tab.id === 'academic' && (
                  <svg className="w-4 h-4 fill-none stroke-current stroke-2 shrink-0" viewBox="0 0 24 24">
                    <line x1="18" y1="20" x2="18" y2="10" />
                    <line x1="12" y1="20" x2="12" y2="4" />
                    <line x1="6" y1="20" x2="6" y2="14" />
                  </svg>
                )}
                {tab.id === 'more' && (
                  <MoreHorizontal className="w-4 h-4 shrink-0" />
                )}
                <span className="text-[11px] tracking-tight font-medium truncate">
                  {tab.label}
                </span>
              </button>
            );
          })}
        </nav>
      </div>

      {/* Diğer (More) Drawer Modal */}
      {isMoreOpen && (
        <div className="fixed inset-0 z-50 bg-black/40 backdrop-blur-xs flex items-end justify-center md:hidden animate-in fade-in">
          <div className="bg-white rounded-t-3xl w-full max-w-lg p-5 pb-8 shadow-2xl border-t border-neutral-200 animate-in slide-in-from-bottom duration-200">
            <div className="flex items-center justify-between pb-3 border-b border-neutral-100">
              <span className="font-semibold text-neutral-900 text-sm">Diğer Bölümler</span>
              <button
                type="button"
                onClick={() => setIsMoreOpen(false)}
                className="p-1 rounded-full text-neutral-400 hover:text-neutral-700 hover:bg-neutral-100"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            <div className="mt-3 space-y-2">
              {moreMenuItems.map((menu) => {
                const Icon = menu.icon;
                const isItemActive = activeId === menu.id;

                return (
                  <button
                    key={menu.id}
                    type="button"
                    onClick={() => {
                      setIsMoreOpen(false);
                      onChange(menu.id);
                    }}
                    className={cn(
                      'w-full flex items-center justify-between p-3 rounded-2xl transition-all text-left border',
                      isItemActive
                        ? 'border-primary-500 bg-primary-50/40 text-primary-900'
                        : 'border-neutral-100 hover:border-neutral-200 hover:bg-neutral-50/70 text-neutral-800'
                    )}
                  >
                    <div className="flex items-center gap-3">
                      <div className={cn('w-9 h-9 rounded-xl flex items-center justify-center shrink-0', menu.color)}>
                        <Icon className="w-4 h-4" />
                      </div>
                      <div>
                        <p className="text-xs font-semibold leading-tight">{menu.label}</p>
                        <p className="text-[11px] text-neutral-500 mt-0.5 leading-tight">{menu.desc}</p>
                      </div>
                    </div>
                    <ChevronRight className="w-4 h-4 text-neutral-400" />
                  </button>
                );
              })}
            </div>
          </div>
        </div>
      )}
    </>
  );
};
