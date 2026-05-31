import { useState } from 'react';
import type { AmbiguousMatchItem } from '@/types/payment.types';
import type { AmbiguousGradeItem } from '@/types/exam.types';
import { Button } from '@/components/atoms/Button';

type AmbiguousItem = AmbiguousMatchItem | AmbiguousGradeItem;

interface HumanInTheLoopModalProps {
  items: AmbiguousItem[];
  onConfirm: (selections: Record<string, string>) => void;
  onCancel: () => void;
}

export function HumanInTheLoopModal({ items, onConfirm, onCancel }: HumanInTheLoopModalProps) {
  const [selections, setSelections] = useState<Record<string, string>>(() =>
    Object.fromEntries(items.map((item) => [item.originalName, item.possibleMatches[0] ?? '']))
  );

  const handleConfirm = () => onConfirm(selections);

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center animate-fade-in" style={{ background: 'rgba(0,0,0,0.45)', backdropFilter: 'blur(8px)' }}>
      <div className="w-full max-w-lg overflow-hidden rounded-2xl shadow-2xl animate-slide-in-up" style={{ background: 'rgba(255,255,255,0.98)' }}>
        {/* Gradient header */}
        <div className="px-6 py-5" style={{ background: 'linear-gradient(135deg, #f59e0b, #ef4444)' }}>
          <div className="flex items-center gap-3">
            <div className="flex h-9 w-9 items-center justify-center rounded-xl bg-white/20 text-lg">⚠️</div>
            <div>
              <h3 className="text-sm font-bold text-white">Onay Gerekiyor</h3>
              <p className="text-xs text-amber-100/80">Belirsiz eşleşmeler tespit edildi</p>
            </div>
          </div>
        </div>
        {/* Content */}
        <div className="px-6 pt-5 pb-4">
          <p className="mb-4 text-sm text-gray-500">
            Aşağıdaki eşleşmeler belirsiz. Lütfen doğru kaydı seçin.
          </p>
          <div className="flex flex-col gap-4">
            {items.map((item) => (
              <div key={item.originalName}>
                <label className="mb-1.5 block text-xs font-bold uppercase tracking-wider text-gray-500">
                  {item.originalName}
                </label>
                <select
                  className="w-full rounded-xl border border-gray-200 bg-gray-50 px-3 py-2.5 text-sm text-gray-800 focus:border-amber-400 focus:outline-none focus:ring-2 focus:ring-amber-400/20 transition-all"
                  value={selections[item.originalName]}
                  onChange={(e) =>
                    setSelections((prev) => ({ ...prev, [item.originalName]: e.target.value }))
                  }
                >
                  {item.possibleMatches.map((match) => (
                    <option key={match} value={match}>
                      {match}
                    </option>
                  ))}
                </select>
              </div>
            ))}
          </div>
        </div>
        <div className="flex justify-end gap-3 border-t border-gray-100 px-6 py-4">
          <Button variant="secondary" onClick={onCancel}>
            İptal
          </Button>
          <Button variant="primary" onClick={handleConfirm}>
            Onayla ve Kaydet
          </Button>
        </div>
      </div>
    </div>
  );
}
