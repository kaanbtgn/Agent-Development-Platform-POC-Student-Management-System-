import { Button } from '@/components/atoms/Button';

interface ConfirmationDialogProps {
  title: string;
  description: string;
  confirmLabel?: string;
  cancelLabel?: string;
  confirmDisabled?: boolean;
  error?: string | null;
  onConfirm: () => void;
  onCancel: () => void;
}

export function ConfirmationDialog({
  title,
  description,
  confirmLabel = 'Onayla',
  cancelLabel = 'İptal',
  confirmDisabled = false,
  error,
  onConfirm,
  onCancel,
}: ConfirmationDialogProps) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-sm">
      <div className="w-full max-w-sm rounded-xl bg-white p-6 shadow-xl">
        <h3 className="mb-2 text-base font-semibold text-gray-900">{title}</h3>
        <p className="mb-4 text-sm text-gray-600">{description}</p>
        {error && (
          <p className="mb-4 rounded-lg bg-red-50 px-3 py-2 text-xs font-medium text-red-600">{error}</p>
        )}
        <div className="flex justify-end gap-3">
          <Button variant="secondary" onClick={onCancel} disabled={confirmDisabled}>
            {cancelLabel}
          </Button>
          <Button variant="danger" onClick={onConfirm} disabled={confirmDisabled}>
            {confirmLabel}
          </Button>
        </div>
      </div>
    </div>
  );
}
