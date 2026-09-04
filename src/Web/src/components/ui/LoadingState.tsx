import React from 'react';
import { cn } from '@/lib/utils';

export interface LoadingStateProps {
  message?: string;
  className?: string;
}

export const LoadingState: React.FC<LoadingStateProps> = ({
  message = 'Yükleniyor...',
  className,
}) => {
  return (
    <div
      role="status"
      className={cn(
        'flex flex-col items-center justify-center p-12 text-center select-none',
        className
      )}
    >
      <div className="w-10 h-10 rounded-full border-2 border-[#17324D]/20 border-t-[#17324D] animate-spin mb-3" />
      <p className="text-xs font-medium text-[#66788A]">{message}</p>
      <span className="sr-only">{message}</span>
    </div>
  );
};
