import React from 'react';
import { cn } from '@/lib/utils';

export interface AvatarProps extends React.HTMLAttributes<HTMLDivElement> {
  src?: string;
  alt?: string;
  name?: string;
  size?: 'sm' | 'md' | 'lg' | 'xl';
  variant?: 'navy' | 'primary';
}

export const Avatar = React.forwardRef<HTMLDivElement, AvatarProps>(
  ({ className, src, alt = 'Avatar', name, size = 'md', variant = 'navy', ...props }, ref) => {
    const [imageError, setImageError] = React.useState(false);

    const sizes = {
      sm: 'h-8 w-8 text-xs',
      md: 'h-9 w-9 text-xs',
      lg: 'h-11 w-11 text-sm font-medium',
      xl: 'h-14 w-14 text-base font-semibold',
    };

    const variants = {
      navy: 'bg-navy-50 text-navy-700 border border-neutral-100',
      primary: 'bg-primary-600 text-white',
    };

    const getInitials = (n?: string) => {
      if (!n) return '?';
      const parts = n.trim().split(/\s+/);
      if (parts.length === 1) return parts[0].substring(0, 2).toUpperCase();
      return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
    };

    return (
      <div
        ref={ref}
        className={cn(
          'relative inline-flex shrink-0 items-center justify-center rounded-full font-semibold overflow-hidden select-none',
          sizes[size],
          variants[variant],
          className
        )}
        {...props}
      >
        {src && !imageError ? (
          <img
            src={src}
            alt={alt}
            onError={() => setImageError(true)}
            className="h-full w-full object-cover"
          />
        ) : (
          <span>{getInitials(name || alt)}</span>
        )}
      </div>
    );
  }
);

Avatar.displayName = 'Avatar';
