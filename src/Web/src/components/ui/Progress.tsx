import React from 'react';
import { cn } from '@/lib/utils';

export interface ProgressProps extends React.HTMLAttributes<HTMLDivElement> {
  value: number; // 0 to 100
  max?: number;
  variant?: 'primary' | 'success' | 'warning' | 'danger' | 'attention';
  size?: 'sm' | 'md' | 'lg';
  showLabel?: boolean;
  label?: string;
}

export const Progress = React.forwardRef<HTMLDivElement, ProgressProps>(
  ({ className, value, max = 100, variant = 'primary', size = 'sm', showLabel = false, label = 'İlerleme', ...props }, ref) => {
    const percentage = Math.min(Math.max(Math.round((value / max) * 100), 0), 100);

    const barColors = {
      primary: 'bg-primary-500',
      success: 'bg-success',
      warning: 'bg-warning',
      danger: 'bg-danger',
      attention: 'bg-attention',
    };

    const trackSizes = {
      sm: 'h-1.5',
      md: 'h-2',
      lg: 'h-3',
    };

    return (
      <div ref={ref} className={cn('w-full space-y-1.5', className)} {...props}>
        {showLabel && (
          <div className="flex justify-between items-center text-xs font-medium text-neutral-600">
            <span>{label}</span>
            <span className="font-mono font-semibold text-neutral-700">%{percentage}</span>
          </div>
        )}
        <div
          className={cn(
            'w-full overflow-hidden rounded-full bg-neutral-100',
            trackSizes[size]
          )}
          role="progressbar"
          aria-valuenow={percentage}
          aria-valuemin={0}
          aria-valuemax={100}
        >
          <div
            className={cn('h-full transition-all duration-500 ease-out rounded-full', barColors[variant])}
            style={{ width: `${percentage}%` }}
          />
        </div>
      </div>
    );
  }
);

Progress.displayName = 'Progress';
