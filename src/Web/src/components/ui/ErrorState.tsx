import React from 'react';
import { AlertCircle, RefreshCw } from 'lucide-react';
import { cn } from '@/lib/utils';
import { Button } from './Button';

export interface ErrorStateProps {
  title?: string;
  message?: string;
  onRetry?: () => void;
  className?: string;
}

export const ErrorState: React.FC<ErrorStateProps> = ({
  title = 'Bir Sorun Oluştu',
  message = 'İçerik yüklenirken bir hata meydana geldi. Lütfen tekrar deneyiniz.',
  onRetry,
  className,
}) => {
  return (
    <div
      role="alert"
      className={cn(
        'bg-white rounded-2xl border border-red-100 p-8 text-center max-w-md mx-auto shadow-sm',
        className
      )}
    >
      <div className="w-12 h-12 rounded-2xl bg-red-50 flex items-center justify-center mx-auto mb-4 text-red-600">
        <AlertCircle className="h-6 w-6" />
      </div>
      <h3 className="font-semibold text-base text-[#17212B] mb-1">{title}</h3>
      <p className="text-xs text-[#66788A] leading-relaxed mb-6">{message}</p>
      {onRetry && (
        <Button
          variant="secondary"
          size="sm"
          onClick={onRetry}
          leftIcon={<RefreshCw className="h-3.5 w-3.5" />}
        >
          Yeniden Dene
        </Button>
      )}
    </div>
  );
};
