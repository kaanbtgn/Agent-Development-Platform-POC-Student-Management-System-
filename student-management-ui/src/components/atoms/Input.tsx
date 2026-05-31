import { cn } from '@/lib/utils';
import type { InputHTMLAttributes } from 'react';

interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  label?: string;
  error?: string;
}

export function Input({ label, error, className, id, ...props }: InputProps) {
  return (
    <div className="flex flex-col gap-1.5">
      {label && (
        <label htmlFor={id} className="text-xs font-semibold uppercase tracking-wide text-gray-500">
          {label}
        </label>
      )}
      <input
        id={id}
        className={cn(
          'h-10 w-full rounded-xl border bg-white/80 px-3.5 py-2 text-sm text-gray-900 placeholder-gray-400 shadow-sm backdrop-blur-sm transition-all duration-200 focus:bg-white focus:outline-none focus:ring-2 disabled:cursor-not-allowed disabled:opacity-60',
          error
            ? 'border-red-400 focus:border-red-400 focus:ring-red-400/30'
            : 'border-gray-200 focus:border-indigo-400 focus:ring-indigo-400/25',
          className
        )}
        {...props}
      />
      {error && (
        <p className="flex items-center gap-1 text-xs font-medium text-red-500">
          <span>⚠</span> {error}
        </p>
      )}
    </div>
  );
}
