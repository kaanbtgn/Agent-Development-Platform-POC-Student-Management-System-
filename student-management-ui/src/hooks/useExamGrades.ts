import { useState, useCallback } from 'react';
import { examApi } from '@/api/examApi';
import type { ExamGradeDto, UpsertExamGradeRequest, UpsertGradeResult } from '@/types/exam.types';

export function useExamGrades(studentId: string) {
  const [grades, setGrades] = useState<ExamGradeDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [upsertResult, setUpsertResult] = useState<UpsertGradeResult | null>(null);

  const fetchGrades = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await examApi.getByStudent(studentId);
      setGrades(data);
    } catch {
      setError('Sınav notları yüklenemedi.');
    } finally {
      setLoading(false);
    }
  }, [studentId]);

  const upsert = useCallback(
    async (courseName: string, payload: UpsertExamGradeRequest) => {
      const result = await examApi.upsert(studentId, courseName, payload);
      setUpsertResult(result);
      if (!result.needsHumanVerification) {
        await fetchGrades();
      }
      return result;
    },
    [studentId, fetchGrades]
  );

  const remove = useCallback(
    async (courseName: string) => {
      await examApi.remove(studentId, courseName);
      await fetchGrades();
    },
    [studentId, fetchGrades]
  );

  return { grades, loading, error, upsertResult, fetchGrades, upsert, remove, clearResult: () => setUpsertResult(null) };
}
