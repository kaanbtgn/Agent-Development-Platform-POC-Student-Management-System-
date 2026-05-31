import axiosInstance from './axiosInstance';
import type { ExamGradeDto, UpsertExamGradeRequest, UpsertGradeResult } from '@/types/exam.types';

export const examApi = {
  getByStudent: (studentId: string) =>
    axiosInstance
      .get<ExamGradeDto[]>(`/api/students/${studentId}/exam-grades`)
      .then((r) => r.data),

  upsert: (studentId: string, courseName: string, payload: UpsertExamGradeRequest) =>
    axiosInstance
      .put<UpsertGradeResult>(
        `/api/students/${studentId}/exam-grades/${encodeURIComponent(courseName)}`,
        payload
      )
      .then((r) => r.data),

  remove: (studentId: string, courseName: string) =>
    axiosInstance
      .delete(`/api/students/${studentId}/exam-grades/${encodeURIComponent(courseName)}`),
};
