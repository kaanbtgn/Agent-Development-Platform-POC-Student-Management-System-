import axiosInstance from './axiosInstance';
import type { InternshipPaymentDto, UpsertPaymentRequest, UpsertPaymentResult } from '@/types/payment.types';

export const paymentApi = {
  getByStudent: (studentId: string) =>
    axiosInstance
      .get<InternshipPaymentDto[]>(`/api/students/${studentId}/payments`)
      .then((r) => r.data),

  upsert: (studentId: string, year: number, month: number, payload: UpsertPaymentRequest) =>
    axiosInstance
      .put<UpsertPaymentResult>(
        `/api/students/${studentId}/payments/${year}/${month}`,
        payload
      )
      .then((r) => r.data),

  remove: (studentId: string, year: number, month: number) =>
    axiosInstance
      .delete(`/api/students/${studentId}/payments/${year}/${month}`),
};
