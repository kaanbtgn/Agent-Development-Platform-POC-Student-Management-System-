import { useState } from 'react';
import type { InternshipPaymentDto } from '@/types/payment.types';
import { Badge, paymentStatusVariant } from '@/components/atoms/Badge';
import { ConfirmationDialog } from '@/components/molecules/ConfirmationDialog';

interface PaymentTableProps {
  payments: InternshipPaymentDto[];
  onEdit: (payment: InternshipPaymentDto) => void;
  onDelete: (year: number, month: number) => Promise<void>;
}

const MONTHS = [
  '', 'Ocak', 'Şubat', 'Mart', 'Nisan', 'Mayıs', 'Haziran',
  'Temmuz', 'Ağustos', 'Eylül', 'Ekim', 'Kasım', 'Aralık',
];

export function PaymentTable({ payments, onEdit, onDelete }: PaymentTableProps) {
  const [deleting, setDeleting] = useState<{ year: number; month: number } | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);
  const [deleteError, setDeleteError] = useState<string | null>(null);

  return (
    <>
      <div className="overflow-x-auto rounded-xl border border-white/40 shadow-md" style={{ background: 'rgba(255,255,255,0.85)', backdropFilter: 'blur(12px)' }}>
        <table className="w-full min-w-[680px] divide-y divide-gray-100 text-sm">
          <thead>
            <tr style={{ background: 'linear-gradient(135deg, rgba(99,102,241,0.07), rgba(139,92,246,0.04))' }}>
              {['Dönem', 'Tutar', 'Ödeme Tarihi', 'Durum', ''].map((h) => (
                <th key={h} className="px-4 py-3 text-left text-[10px] font-bold uppercase tracking-widest text-gray-500">
                  {h}
                </th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-100/80">
            {payments.map((p) => (
              <tr key={p.id} className="group transition-colors duration-150 hover:bg-indigo-50/50">
                <td className="px-4 py-3 font-semibold text-gray-700">
                  {MONTHS[p.periodMonth]} {p.periodYear}
                </td>
                <td className="px-4 py-3">
                  <span className="font-bold text-emerald-700">
                    {p.amount.toLocaleString('tr-TR', { style: 'currency', currency: 'TRY' })}
                  </span>
                </td>
                <td className="px-4 py-3 text-gray-500 text-xs">{p.paymentDate ?? '—'}</td>
                <td className="px-4 py-3">
                  <Badge label={p.status} variant={paymentStatusVariant(p.status)} />
                </td>
                <td className="px-4 py-3">
                  <div className="flex gap-1 opacity-0 transition-all group-hover:opacity-100">
                    <button
                      className="rounded-lg px-2.5 py-1 text-xs font-semibold text-indigo-600 hover:bg-indigo-50"
                      onClick={() => onEdit(p)}
                    >
                      Düzenle
                    </button>
                    <button
                      className="rounded-lg px-2.5 py-1 text-xs font-semibold text-red-600 hover:bg-red-50"
                      onClick={() => setDeleting({ year: p.periodYear, month: p.periodMonth })}
                    >
                      Sil
                    </button>
                  </div>
                </td>
              </tr>
            ))}
            {payments.length === 0 && (
              <tr>
                <td colSpan={5} className="px-4 py-10 text-center">
                  <div className="flex flex-col items-center gap-2 text-gray-400">
                    <span className="text-2xl">💳</span>
                    <p className="text-xs font-medium">Ödeme kaydı bulunamadı.</p>
                  </div>
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {deleting && (
        <ConfirmationDialog
          title="Ödemeyi Sil"
          description={`${MONTHS[deleting.month]} ${deleting.year} dönemine ait ödeme kaydı kalıcı olarak silinecek.`}
          confirmLabel={isDeleting ? 'Siliniyor…' : 'Evet, Sil'}
          confirmDisabled={isDeleting}
          error={deleteError}
          onConfirm={async () => {
            setIsDeleting(true);
            setDeleteError(null);
            try {
              await onDelete(deleting.year, deleting.month);
              setDeleting(null);
            } catch {
              setDeleteError('Silme işlemi başarısız. Lütfen tekrar deneyin.');
            } finally {
              setIsDeleting(false);
            }
          }}
          onCancel={() => { setDeleting(null); setDeleteError(null); }}
        />
      )}
    </>
  );
}
