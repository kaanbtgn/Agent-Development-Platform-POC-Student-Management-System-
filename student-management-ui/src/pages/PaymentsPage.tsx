import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { PaymentTable } from '@/components/organisms/PaymentTable';
import { HumanInTheLoopModal } from '@/components/organisms/HumanInTheLoopModal';
import { Button } from '@/components/atoms/Button';
import { Input } from '@/components/atoms/Input';
import { Spinner } from '@/components/atoms/Spinner';
import { usePayments } from '@/hooks/usePayments';
import type { InternshipPaymentDto } from '@/types/payment.types';

const schema = z.object({
  periodYear: z.coerce.number().min(2000).max(2100),
  periodMonth: z.coerce.number().min(1).max(12),
  amount: z.coerce.number().positive('Tutar pozitif olmalı'),
  paymentDate: z.string().optional(),
});
type FormData = z.infer<typeof schema>;

export function PaymentsPage() {
  const { studentId } = useParams<{ studentId: string }>();
  const { payments, loading, upsertResult, fetchPayments, upsert, remove, clearResult } = usePayments(studentId ?? '');
  const [editing, setEditing] = useState<InternshipPaymentDto | null>(null);
  const [showForm, setShowForm] = useState(false);

  const form = useForm<FormData>({ resolver: zodResolver(schema) as any });

  useEffect(() => { if (studentId) fetchPayments(); }, [studentId, fetchPayments]);

  const handleSubmit = form.handleSubmit(async (data) => {
    await upsert(data.periodYear, data.periodMonth, {
      amount: data.amount,
      paymentDate: data.paymentDate || undefined,
    });
    setShowForm(false);
    form.reset();
  });

  const handleEdit = (p: InternshipPaymentDto) => {
    setEditing(p);
    form.setValue('periodYear', p.periodYear);
    form.setValue('periodMonth', p.periodMonth);
    form.setValue('amount', p.amount);
    form.setValue('paymentDate', p.paymentDate ?? '');
    setShowForm(true);
  };

  if (!studentId) return <p className="p-8 text-white/50">Öğrenci seçilmedi.</p>;

  return (
    <div className="p-6">
      <div className="mb-4 flex items-center justify-between">
        <h1 className="text-base font-semibold text-white">Ödemeler</h1>
        <Button size="sm" onClick={() => { setEditing(null); form.reset(); setShowForm(true); }}>
          + Ödeme Ekle
        </Button>
      </div>

      {loading ? <Spinner /> : <PaymentTable payments={payments} onEdit={handleEdit} onDelete={remove} />}

      {showForm && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
          <div className="w-full max-w-sm rounded-xl bg-white p-6 shadow-xl">
            <h2 className="mb-4 text-base font-semibold">{editing ? 'Ödeme Düzenle' : 'Yeni Ödeme'}</h2>
            <form onSubmit={handleSubmit} className="flex flex-col gap-3">
              <Input label="Yıl" type="number" {...form.register('periodYear')} />
              <Input label="Ay (1-12)" type="number" {...form.register('periodMonth')} />
              <Input label="Tutar (₺)" type="number" step="0.01" {...form.register('amount')} error={form.formState.errors.amount?.message} />
              <Input label="Ödeme Tarihi (opsiyonel)" type="date" {...form.register('paymentDate')} />
              <div className="flex justify-end gap-2 pt-2">
                <Button variant="secondary" type="button" onClick={() => setShowForm(false)}>İptal</Button>
                <Button type="submit">Kaydet</Button>
              </div>
            </form>
          </div>
        </div>
      )}

      {upsertResult?.needsHumanVerification && (
        <HumanInTheLoopModal
          items={upsertResult.ambiguousItems}
          onConfirm={() => { clearResult(); setShowForm(false); fetchPayments(); }}
          onCancel={() => { clearResult(); setShowForm(false); form.reset(); }}
        />
      )}
    </div>
  );
}
