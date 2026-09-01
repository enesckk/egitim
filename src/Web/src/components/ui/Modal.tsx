import React, { useEffect } from 'react';
import { X } from 'lucide-react';
import { cn } from '@/lib/utils';
import { IconButton } from './IconButton';

export interface ModalProps {
  isOpen: boolean;
  onClose: () => void;
  title?: string;
  subtitle?: string;
  children: React.ReactNode;
  footer?: React.ReactNode;
  headerVariant?: 'default' | 'dark';
  maxWidth?: 'sm' | 'md' | 'lg' | 'xl';
  className?: string;
}

export const Modal: React.FC<ModalProps> = ({
  isOpen,
  onClose,
  title,
  subtitle,
  children,
  footer,
  headerVariant = 'default',
  maxWidth = 'md',
  className,
}) => {
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose();
    };
    if (isOpen) {
      document.body.style.overflow = 'hidden';
      window.addEventListener('keydown', handleKeyDown);
    }
    return () => {
      document.body.style.overflow = '';
      window.removeEventListener('keydown', handleKeyDown);
    };
  }, [isOpen, onClose]);

  if (!isOpen) return null;

  const maxWidths = {
    sm: 'sm:max-w-sm',
    md: 'sm:max-w-md',
    lg: 'sm:max-w-lg',
    xl: 'sm:max-w-xl',
  };

  return (
    <div className="fixed inset-0 z-50 flex items-end sm:items-center justify-center p-0 sm:p-4">
      {/* Backdrop */}
      <div
        className="fixed inset-0 bg-black/40 backdrop-blur-sm transition-opacity"
        onClick={onClose}
        aria-hidden="true"
      />

      {/* Dialog Container */}
      <div
        role="dialog"
        aria-modal="true"
        className={cn(
          'relative w-full rounded-t-2xl sm:rounded-2xl bg-white shadow-2xl overflow-hidden transition-all max-h-[90vh] flex flex-col z-10',
          maxWidths[maxWidth],
          className
        )}
      >
        {/* Header */}
        {(title || subtitle) && (
          <div
            className={cn(
              'px-6 py-4 flex items-center justify-between border-b',
              headerVariant === 'dark'
                ? 'bg-navy-900 border-navy-800 text-white'
                : 'bg-white border-neutral-100 text-neutral-900'
            )}
          >
            <div>
              {title && (
                <h3 className={cn('font-semibold text-base leading-tight', headerVariant === 'dark' ? 'text-white' : 'text-neutral-900')}>
                  {title}
                </h3>
              )}
              {subtitle && (
                <p className={cn('text-xs mt-0.5', headerVariant === 'dark' ? 'text-navy-300' : 'text-neutral-400')}>
                  {subtitle}
                </p>
              )}
            </div>
            <IconButton
              variant="ghost"
              size="sm"
              ariaLabel="Kapat"
              onClick={onClose}
              className={headerVariant === 'dark' ? 'text-navy-300 hover:text-white hover:bg-navy-800' : 'text-neutral-400 hover:text-neutral-600'}
            >
              <X className="h-4 w-4" />
            </IconButton>
          </div>
        )}

        {/* Content Body */}
        <div className="p-6 overflow-y-auto space-y-4 flex-1">
          {children}
        </div>

        {/* Footer */}
        {footer && (
          <div className="px-6 py-4 border-t border-neutral-100 bg-surface flex items-center justify-end gap-2">
            {footer}
          </div>
        )}
      </div>
    </div>
  );
};
