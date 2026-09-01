import React from 'react';
import { cn } from '@/lib/utils';

export interface AlertProps extends React.HTMLAttributes<HTMLDivElement> {
  variant?: 'success' | 'warning' | 'danger' | 'attention' | 'info';
  icon?: React.ReactNode;
  title?: string;
}

export const Alert = React.forwardRef<HTMLDivElement, AlertProps>(
  ({ className, variant = 'info', icon, title, children, ...props }, ref) => {
    const variants = {
      success: 'bg-success-light border-green-200 text-success-dark',
      warning: 'bg-warning-light border-amber-200 text-warning-dark',
      danger: 'bg-danger-light border-red-200 text-danger-dark',
      attention: 'bg-attention-light border-purple-200 text-attention-dark',
      info: 'bg-primary-50 border-primary-100 text-primary-700',
    };

    return (
      <div
        ref={ref}
        role="alert"
        className={cn(
          'flex items-start gap-3 px-4 py-3 rounded-xl border text-sm transition-all',
          variants[variant],
          className
        )}
        {...props}
      >
        {icon && <span className="flex-shrink-0 mt-0.5">{icon}</span>}
        <div className="flex-1 min-w-0">
          {title && <h5 className="font-semibold text-sm mb-0.5 leading-tight">{title}</h5>}
          <div className="text-xs leading-relaxed opacity-95">{children}</div>
        </div>
      </div>
    );
  }
);

Alert.displayName = 'Alert';
