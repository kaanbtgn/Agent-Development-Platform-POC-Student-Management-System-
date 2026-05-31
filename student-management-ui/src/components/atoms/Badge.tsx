import { cn } from '@/lib/utils';

interface BadgeProps {
  label: string;
  variant?: 'green' | 'yellow' | 'red' | 'gray' | 'blue';
}

const variantClasses: Record<string, string> = {
  green: 'bg-emerald-50 text-emerald-700 border border-emerald-200 shadow-sm shadow-emerald-100',
  yellow: 'bg-amber-50 text-amber-700 border border-amber-200 shadow-sm shadow-amber-100',
  red: 'bg-red-50 text-red-700 border border-red-200 shadow-sm shadow-red-100',
  gray: 'bg-gray-100 text-gray-600 border border-gray-200',
  blue: 'bg-indigo-50 text-indigo-700 border border-indigo-200 shadow-sm shadow-indigo-100',
};

const dotColors: Record<string, string> = {
  green: 'bg-emerald-400',
  yellow: 'bg-amber-400',
  red: 'bg-red-400',
  gray: 'bg-gray-400',
  blue: 'bg-indigo-400',
};

export function Badge({ label, variant = 'gray' }: BadgeProps) {
  return (
    <span
      className={cn(
        'inline-flex items-center gap-1.5 rounded-full px-2.5 py-0.5 text-xs font-semibold',
        variantClasses[variant]
      )}
    >
      <span className={cn('h-1.5 w-1.5 rounded-full', dotColors[variant])} />
      {label}
    </span>
  );
}

export function paymentStatusVariant(status: string): BadgeProps['variant'] {
  switch (status.toLowerCase()) {
    case 'paid':
      return 'green';
    case 'pending':
      return 'yellow';
    case 'overdue':
      return 'red';
    default:
      return 'gray';
  }
}
