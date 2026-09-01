import React from 'react';
import { ChevronDown } from 'lucide-react';
import { cn } from '@/lib/utils';

export interface SelectOption {
  value: string;
  label: string;
}

export interface SelectProps extends React.SelectHTMLAttributes<HTMLSelectElement> {
  label?: string;
  options: SelectOption[];
  placeholder?: string;
}

export const Select = React.forwardRef<HTMLSelectElement, SelectProps>(
  ({ className, label, options, placeholder = 'Tümü', value, id, ...props }, ref) => {
    const selectId = id || (label ? label.toLowerCase().replace(/\s+/g, '-') : undefined);
    const hasValue = Boolean(value);

    return (
      <div className="relative w-full">
        {label && (
          <label htmlFor={selectId} className="block text-xs font-medium text-neutral-500 mb-1">
            {label}
          </label>
        )}
        <div className="relative">
          <select
            id={selectId}
            ref={ref}
            value={value}
            className={cn(
              'w-full appearance-none pl-3 pr-8 py-2 rounded-lg border text-sm bg-white transition-colors cursor-pointer min-h-[38px]',
              'focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-primary-500',
              hasValue
                ? 'border-primary-300 text-primary-700 font-medium'
                : 'border-neutral-200 text-neutral-700',
              className
            )}
            {...props}
          >
            {placeholder && <option value="">{placeholder}</option>}
            {options.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
          <ChevronDown
            className="absolute right-2.5 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-neutral-400 pointer-events-none"
          />
        </div>
      </div>
    );
  }
);

Select.displayName = 'Select';
