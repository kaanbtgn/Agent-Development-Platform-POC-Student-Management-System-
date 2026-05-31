import { useState, useCallback } from 'react';
import { paymentApi } from '@/api/paymentApi';
import type { InternshipPaymentDto, UpsertPaymentRequest, UpsertPaymentResult } from '@/types/payment.types';

export function usePayments(studentId: string) {
  const [payments, setPayments] = useState<InternshipPaymentDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [upsertResult, setUpsertResult] = useState<UpsertPaymentResult | null>(null);

  const fetchPayments = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await paymentApi.getByStudent(studentId);
      setPayments(data);
    } catch {
      setError('Ödemeler yüklenemedi.');
    } finally {
      setLoading(false);
    }
  }, [studentId]);

  const upsert = useCallback(
    async (year: number, month: number, payload: UpsertPaymentRequest) => {
      const result = await paymentApi.upsert(studentId, year, month, payload);
      setUpsertResult(result);
      if (!result.needsHumanVerification) {
        await fetchPayments();
      }
      return result;
    },
    [studentId, fetchPayments]
  );

  const remove = useCallback(
    async (year: number, month: number) => {
      await paymentApi.remove(studentId, year, month);
      await fetchPayments();
    },
    [studentId, fetchPayments]
  );

  return { payments, loading, error, upsertResult, fetchPayments, upsert, remove, clearResult: () => setUpsertResult(null) };
}
