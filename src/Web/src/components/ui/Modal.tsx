import React, { useEffect, useRef, useId } from 'react';
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
  initialFocusRef?: React.RefObject<HTMLElement | null>;
}

const FOCUSABLE_ELEMENTS = [
  'a[href]',
  'button:not([disabled])',
  'textarea:not([disabled])',
  'input:not([disabled])',
  'select:not([disabled])',
  '[tabindex]:not([tabindex="-1"])',
].join(', ');

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
  initialFocusRef,
}) => {
  const dialogRef = useRef<HTMLDivElement>(null);
  const previouslyFocusedElementRef = useRef<HTMLElement | null>(null);
  const titleId = useId();
  const subtitleId = useId();

  useEffect(() => {
    if (!isOpen) return;

    // Save previous active element for restoration upon close
    previouslyFocusedElementRef.current = document.activeElement as HTMLElement | null;

    // Set initial focus
    const timer = setTimeout(() => {
      if (initialFocusRef?.current) {
        initialFocusRef.current.focus();
      } else if (dialogRef.current) {
        const focusable = dialogRef.current.querySelectorAll<HTMLElement>(FOCUSABLE_ELEMENTS);
        if (focusable.length > 0) {
          focusable[0].focus();
        } else {
          dialogRef.current.focus();
        }
      }
    }, 0);

    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        e.stopPropagation();
        e.preventDefault();
        onClose();
        return;
      }

      if (e.key === 'Tab') {
        if (!dialogRef.current) return;

        const focusables = Array.from(
          dialogRef.current.querySelectorAll<HTMLElement>(FOCUSABLE_ELEMENTS)
        ).filter((el) => el.offsetParent !== null || el.offsetWidth > 0 || el.offsetHeight > 0);

        if (focusables.length === 0) {
          e.preventDefault();
          dialogRef.current.focus();
          return;
        }

        const firstElement = focusables[0];
        const lastElement = focusables[focusables.length - 1];

        if (e.shiftKey) {
          if (document.activeElement === firstElement || document.activeElement === dialogRef.current) {
            e.preventDefault();
            lastElement.focus();
          }
        } else {
          if (document.activeElement === lastElement) {
            e.preventDefault();
            firstElement.focus();
          }
        }
      }
    };

    document.body.style.overflow = 'hidden';
    window.addEventListener('keydown', handleKeyDown);

    return () => {
      clearTimeout(timer);
      document.body.style.overflow = '';
      window.removeEventListener('keydown', handleKeyDown);
      if (previouslyFocusedElementRef.current && typeof previouslyFocusedElementRef.current.focus === 'function') {
        previouslyFocusedElementRef.current.focus();
      }
    };
  }, [isOpen, onClose, initialFocusRef]);

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
        ref={dialogRef}
        tabIndex={-1}
        role="dialog"
        aria-modal="true"
        aria-labelledby={title ? titleId : undefined}
        aria-describedby={subtitle ? subtitleId : undefined}
        aria-label={!title ? 'Pencere' : undefined}
        className={cn(
          'relative w-full rounded-t-2xl sm:rounded-2xl bg-white shadow-2xl overflow-hidden transition-all max-h-[90vh] flex flex-col z-10 focus:outline-none',
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
                <h3
                  id={titleId}
                  className={cn(
                    'font-semibold text-base leading-tight',
                    headerVariant === 'dark' ? 'text-white' : 'text-neutral-900'
                  )}
                >
                  {title}
                </h3>
              )}
              {subtitle && (
                <p
                  id={subtitleId}
                  className={cn(
                    'text-xs mt-0.5',
                    headerVariant === 'dark' ? 'text-navy-300' : 'text-neutral-400'
                  )}
                >
                  {subtitle}
                </p>
              )}
            </div>
            <IconButton
              variant="ghost"
              size="sm"
              ariaLabel="Pencereyi Kapat"
              onClick={onClose}
              className={
                headerVariant === 'dark'
                  ? 'text-navy-300 hover:text-white hover:bg-navy-800'
                  : 'text-neutral-400 hover:text-neutral-600'
              }
            >
              <X className="h-4 w-4" />
            </IconButton>
          </div>
        )}

        {/* Content Body */}
        <div className="p-6 overflow-y-auto space-y-4 flex-1">{children}</div>

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
