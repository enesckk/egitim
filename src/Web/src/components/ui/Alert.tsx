import React from 'react';
import { X } from 'lucide-react';
import { cn } from '@/lib/utils';

export interface AlertProps extends React.HTMLAttributes<HTMLDivElement> {
  variant?: 'success' | 'warning' | 'danger' | 'attention' | 'info';
  icon?: React.ReactNode;
  title?: string;
  onClose?: () => void;
}

export const Alert = React.forwardRef<HTMLDivElement, AlertProps>(
  ({ className, variant = 'info', icon, title, onClose, children, ...props }, ref) => {
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
          'flex items-start gap-3 px-4 py-3 rounded-xl border text-sm transition-all select-none',
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
        {onClose && (
          <button
            type="button"
            onClick={onClose}
            className="flex-shrink-0 p-1 -mr-1 -mt-1 rounded-lg hover:bg-black/5 transition-colors opacity-70 hover:opacity-100"
            aria-label="Kapat"
          >
            <X className="h-3.5 w-3.5" />
          </button>
        )}
      </div>
    );
  }
);

Alert.displayName = 'Alert';
