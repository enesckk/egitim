import React from 'react';
import { cn } from '@/lib/utils';

export interface CardProps extends React.HTMLAttributes<HTMLDivElement> {
  variant?: 'default' | 'dark' | 'tinted' | 'interactive' | 'muted' | 'outline';
}

export const Card = React.forwardRef<HTMLDivElement, CardProps>(
  ({ className, variant = 'default', ...props }, ref) => {
    const variants = {
      default: 'bg-white border border-neutral-100 text-neutral-800',
      dark: 'bg-navy-900 text-white',
      tinted: 'bg-primary-50 border border-primary-100 text-primary-900',
      interactive:
        'bg-white border border-neutral-100 hover:shadow-md hover:border-neutral-200 transition-all cursor-pointer active:scale-[0.99] text-neutral-800',
      muted: 'bg-surface-alt border border-neutral-100 text-neutral-800',
      outline: 'bg-transparent border border-neutral-200 text-neutral-800',
    };

    return (
      <div
        ref={ref}
        className={cn('rounded-2xl p-4 sm:p-5', variants[variant], className)}
        {...props}
      />
    );
  }
);

Card.displayName = 'Card';

export const CardHeader = React.forwardRef<
  HTMLDivElement,
  React.HTMLAttributes<HTMLDivElement>
>(({ className, ...props }, ref) => (
  <div
    ref={ref}
    className={cn('flex flex-col space-y-1 pb-3', className)}
    {...props}
  />
));
CardHeader.displayName = 'CardHeader';

export const CardTitle = React.forwardRef<
  HTMLParagraphElement,
  React.HTMLAttributes<HTMLHeadingElement>
>(({ className, ...props }, ref) => (
  <h3
    ref={ref}
    className={cn('font-semibold text-sm leading-none tracking-tight text-neutral-800', className)}
    {...props}
  />
));
CardTitle.displayName = 'CardTitle';

export const CardDescription = React.forwardRef<
  HTMLParagraphElement,
  React.HTMLAttributes<HTMLParagraphElement>
>(({ className, ...props }, ref) => (
  <p
    ref={ref}
    className={cn('text-xs text-neutral-400 mt-0.5', className)}
    {...props}
  />
));
CardDescription.displayName = 'CardDescription';

export const CardContent = React.forwardRef<
  HTMLDivElement,
  React.HTMLAttributes<HTMLDivElement>
>(({ className, ...props }, ref) => (
  <div ref={ref} className={cn('pt-0', className)} {...props} />
));
CardContent.displayName = 'CardContent';

export const CardFooter = React.forwardRef<
  HTMLDivElement,
  React.HTMLAttributes<HTMLDivElement>
>(({ className, ...props }, ref) => (
  <div
    ref={ref}
    className={cn('flex items-center pt-3 border-t border-neutral-100 mt-3', className)}
    {...props}
  />
));
CardFooter.displayName = 'CardFooter';
