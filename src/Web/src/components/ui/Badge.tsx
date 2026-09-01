import React from 'react';
import { cn } from '@/lib/utils';

export interface BadgeProps extends React.HTMLAttributes<HTMLDivElement> {
  variant?: 'default' | 'secondary' | 'success' | 'warning' | 'danger' | 'attention' | 'info' | 'outline';
  size?: 'sm' | 'md';
}

export const Badge = React.forwardRef<HTMLDivElement, BadgeProps>(
  ({ className, variant = 'default', size = 'md', ...props }, ref) => {
    const variants = {
      default: 'bg-navy-900 text-white',
      secondary: 'bg-neutral-100 text-neutral-600 border border-neutral-200',
      success: 'bg-success-light text-success-dark border border-green-200',
      warning: 'bg-warning-light text-warning-dark border border-amber-200',
      danger: 'bg-danger-light text-danger border border-red-200',
      attention: 'bg-attention-light text-attention-dark border border-purple-200',
      info: 'bg-primary-50 text-primary-700 border border-primary-100',
      outline: 'bg-transparent border border-neutral-300 text-neutral-600',
    };

    const sizes = {
      sm: 'px-2 py-0.5 text-[10px] font-semibold',
      md: 'px-2.5 py-0.5 text-xs font-medium',
    };

    return (
      <div
        ref={ref}
        className={cn(
          'inline-flex items-center rounded-full transition-colors select-none',
          variants[variant],
          sizes[size],
          className
        )}
        {...props}
      />
    );
  }
);

Badge.displayName = 'Badge';
