import { cn } from '@/lib/utils';
import type { ButtonHTMLAttributes } from 'react';

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'secondary' | 'danger' | 'ghost';
  size?: 'sm' | 'md' | 'lg';
}

export function Button({
  variant = 'primary',
  size = 'md',
  className,
  children,
  disabled,
  ...props
}: ButtonProps) {
  return (
    <button
      className={cn(
        'relative inline-flex items-center justify-center overflow-hidden rounded-xl font-semibold transition-all duration-200 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-40 active:scale-[0.97]',
        {
          'text-white shadow-lg shadow-indigo-500/25 hover:shadow-indigo-500/40 hover:-translate-y-0.5 focus-visible:ring-indigo-500':
            variant === 'primary',
          'bg-white/10 text-gray-800 border border-gray-200 hover:bg-white/20 hover:border-gray-300 shadow-sm focus-visible:ring-gray-400 backdrop-blur-sm':
            variant === 'secondary',
          'text-white shadow-lg shadow-red-500/25 hover:shadow-red-500/40 hover:-translate-y-0.5 focus-visible:ring-red-500':
            variant === 'danger',
          'text-gray-600 hover:bg-gray-100/80 hover:text-gray-900': variant === 'ghost',
          'h-8 px-3 text-xs': size === 'sm',
          'h-10 px-4 text-sm': size === 'md',
          'h-11 px-6 text-base': size === 'lg',
        },
        className
      )}
      style={{
        ...(variant === 'primary' ? { background: 'linear-gradient(135deg, #6366f1, #8b5cf6)' } : {}),
        ...(variant === 'danger' ? { background: 'linear-gradient(135deg, #ef4444, #dc2626)' } : {}),
      }}
      disabled={disabled}
      {...props}
    >
      {/* Shimmer overlay */}
      {(variant === 'primary' || variant === 'danger') && (
        <span className="pointer-events-none absolute inset-0 -translate-x-full group-hover:translate-x-full transition-transform duration-700 bg-gradient-to-r from-transparent via-white/10 to-transparent" />
      )}
      {children}
    </button>
  );
}
