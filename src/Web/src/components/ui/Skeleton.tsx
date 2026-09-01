import React from 'react';
import { cn } from '@/lib/utils';

export interface SkeletonProps extends React.HTMLAttributes<HTMLDivElement> {
  variant?: 'rectangular' | 'circular' | 'rounded' | 'card';
}

export const Skeleton = React.forwardRef<HTMLDivElement, SkeletonProps>(
  ({ className, variant = 'rounded', ...props }, ref) => {
    const variants = {
      rectangular: 'rounded-none',
      rounded: 'rounded-lg',
      circular: 'rounded-full',
      card: 'rounded-2xl',
    };

    return (
      <div
        ref={ref}
        className={cn(
          'animate-pulse bg-neutral-100',
          variants[variant],
          className
        )}
        {...props}
      />
    );
  }
);

Skeleton.displayName = 'Skeleton';
