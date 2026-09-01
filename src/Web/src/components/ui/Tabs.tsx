import React from 'react';
import { cn } from '@/lib/utils';

export interface TabItem {
  id: string;
  label: string;
  count?: number;
}

export interface TabsProps {
  tabs: TabItem[];
  activeId: string;
  onChange: (id: string) => void;
  className?: string;
}

export const Tabs: React.FC<TabsProps> = ({
  tabs,
  activeId,
  onChange,
  className,
}) => {
  return (
    <div className={cn('border-b border-neutral-200 flex gap-1 overflow-x-auto', className)}>
      {tabs.map((tab) => {
        const isActive = tab.id === activeId;
        return (
          <button
            key={tab.id}
            type="button"
            onClick={() => onChange(tab.id)}
            className={cn(
              'flex items-center gap-2 px-4 py-2.5 text-sm font-medium transition-colors whitespace-nowrap select-none min-h-[44px]',
              isActive
                ? 'text-primary-600 border-b-2 border-primary-500 -mb-px font-semibold'
                : 'text-neutral-500 hover:text-neutral-700'
            )}
          >
            <span>{tab.label}</span>
            {tab.count !== undefined && (
              <span
                className={cn(
                  'px-1.5 py-0.5 rounded-full text-[10px] font-semibold',
                  isActive
                    ? 'bg-primary-50 text-primary-600'
                    : 'bg-neutral-100 text-neutral-500'
                )}
              >
                {tab.count}
              </span>
            )}
          </button>
        );
      })}
    </div>
  );
};
