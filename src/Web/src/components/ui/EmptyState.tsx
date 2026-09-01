import React from 'react';
import { cn } from '@/lib/utils';

export interface EmptyStateProps {
  icon: React.ReactNode;
  title: string;
  description?: string;
  action?: React.ReactNode;
  className?: string;
}

export const EmptyState: React.FC<EmptyStateProps> = ({
  icon,
  title,
  description,
  action,
  className,
}) => {
  return (
    <div
      className={cn(
        'bg-white rounded-2xl border border-neutral-100 py-12 px-6 text-center max-w-sm mx-auto',
        className
      )}
    >
      <div className="w-12 h-12 rounded-2xl bg-neutral-100 flex items-center justify-center mx-auto mb-4 text-neutral-400">
        {icon}
      </div>
      <h4 className="font-semibold text-sm text-neutral-800 mb-1">{title}</h4>
      {description && (
        <p className="text-xs text-neutral-400 leading-relaxed mb-4">{description}</p>
      )}
      {action && <div className="mt-2">{action}</div>}
    </div>
  );
};
