import React from 'react';
import { cn } from '@/lib/utils';

export interface IconButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'secondary' | 'outline' | 'ghost' | 'destructive' | 'subtle';
  size?: 'sm' | 'md' | 'lg';
  ariaLabel: string;
  isLoading?: boolean;
}

export const IconButton = React.forwardRef<HTMLButtonElement, IconButtonProps>(
  (
    {
      className,
      variant = 'ghost',
      size = 'md',
      ariaLabel,
      isLoading = false,
      disabled,
      children,
      ...props
    },
    ref
  ) => {
    const baseStyles =
      'inline-flex items-center justify-center font-medium transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 focus-visible:ring-offset-2 disabled:opacity-50 disabled:pointer-events-none active:scale-[0.95] select-none';

    const variants = {
      primary: 'bg-primary-600 text-white hover:bg-primary-700 active:bg-primary-800 shadow-soft-sm',
      secondary: 'bg-white text-neutral-700 hover:bg-neutral-50 border border-neutral-200',
      outline: 'bg-transparent border border-neutral-300 text-neutral-700 hover:bg-neutral-50',
      ghost: 'bg-transparent text-neutral-500 hover:text-neutral-900 hover:bg-neutral-100',
      destructive: 'bg-danger text-white hover:bg-danger-dark shadow-soft-sm',
      subtle: 'bg-primary-50 text-primary-600 hover:bg-primary-100 border border-primary-100',
    };

    const sizes = {
      sm: 'h-8 w-8 rounded-lg text-xs min-h-[32px] min-w-[32px]',
      md: 'h-10 w-10 rounded-xl text-sm min-h-[40px] min-w-[40px]',
      lg: 'h-12 w-12 rounded-xl text-base min-h-[48px] min-w-[48px]',
    };

    return (
      <button
        ref={ref}
        aria-label={ariaLabel}
        disabled={disabled || isLoading}
        className={cn(baseStyles, variants[variant], sizes[size], className)}
        {...props}
      >
        {isLoading ? (
          <svg
            className="animate-spin h-4 w-4 text-current"
            xmlns="http://www.w3.org/2000/svg"
            fill="none"
            viewBox="0 0 24 24"
          >
            <circle
              className="opacity-25"
              cx="12"
              cy="12"
              r="10"
              stroke="currentColor"
              strokeWidth="4"
            />
            <path
              className="opacity-75"
              fill="currentColor"
              d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
            />
          </svg>
        ) : (
          children
        )}
      </button>
    );
  }
);

IconButton.displayName = 'IconButton';
