import { useState } from 'react';
import type { ExamGradeDto } from '@/types/exam.types';
import { ConfirmationDialog } from '@/components/molecules/ConfirmationDialog';

interface ExamGradeTableProps {
  grades: ExamGradeDto[];
  onEdit: (grade: ExamGradeDto) => void;
  onDelete: (courseName: string) => Promise<void>;
}

function gradeColor(grade: number | null | undefined) {
  if (grade == null) return 'text-gray-400';
  if (grade >= 85) return 'font-bold text-emerald-600';
  if (grade >= 70) return 'font-bold text-blue-600';
  if (grade >= 50) return 'font-bold text-amber-600';
  return 'font-bold text-red-600';
}

export function ExamGradeTable({ grades, onEdit, onDelete }: ExamGradeTableProps) {
  const [deletingCourseName, setDeletingCourseName] = useState<string | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);
  const [deleteError, setDeleteError] = useState<string | null>(null);

  return (
    <>
      <div className="overflow-hidden rounded-xl border border-white/40 shadow-md" style={{ background: 'rgba(255,255,255,0.85)', backdropFilter: 'blur(12px)' }}>
        <table className="min-w-full divide-y divide-gray-100 text-sm">
          <thead>
            <tr style={{ background: 'linear-gradient(135deg, rgba(99,102,241,0.07), rgba(139,92,246,0.04))' }}>
              {['Ders Adı', '1. Sınav', '2. Sınav', ''].map((h) => (
                <th key={h} className="px-4 py-3 text-left text-[10px] font-bold uppercase tracking-widest text-gray-500">
                  {h}
                </th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-100/80">
            {grades.map((g) => (
              <tr key={g.id} className="group transition-colors duration-150 hover:bg-indigo-50/50">
                <td className="px-4 py-3 font-semibold text-gray-800">{g.courseName}</td>
                <td className={`px-4 py-3 ${gradeColor(g.exam1Grade)}`}>{g.exam1Grade ?? '—'}</td>
                <td className={`px-4 py-3 ${gradeColor(g.exam2Grade)}`}>{g.exam2Grade ?? '—'}</td>
                <td className="px-4 py-3">
                  <div className="flex gap-1 opacity-0 transition-all group-hover:opacity-100">
                    <button
                      className="rounded-lg px-2.5 py-1 text-xs font-semibold text-indigo-600 hover:bg-indigo-50"
                      onClick={() => onEdit(g)}
                    >
                      Düzenle
                    </button>
                    <button
                      className="rounded-lg px-2.5 py-1 text-xs font-semibold text-red-600 hover:bg-red-50"
                      onClick={() => setDeletingCourseName(g.courseName)}
                    >
                      Sil
                    </button>
                  </div>
                </td>
              </tr>
            ))}
            {grades.length === 0 && (
              <tr>
                <td colSpan={4} className="px-4 py-10 text-center">
                  <div className="flex flex-col items-center gap-2 text-gray-400">
                    <span className="text-2xl">📝</span>
                    <p className="text-xs font-medium">Sınav notu bulunamadı.</p>
                  </div>
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {deletingCourseName && (
        <ConfirmationDialog
          title="Sınav Notunu Sil"
          description={`"${deletingCourseName}" dersine ait not kaydı kalıcı olarak silinecek.`}
          confirmLabel={isDeleting ? 'Siliniyor…' : 'Evet, Sil'}
          confirmDisabled={isDeleting}
          error={deleteError}
          onConfirm={async () => {
            setIsDeleting(true);
            setDeleteError(null);
            try {
              await onDelete(deletingCourseName);
              setDeletingCourseName(null);
            } catch {
              setDeleteError('Silme işlemi başarısız. Lütfen tekrar deneyin.');
            } finally {
              setIsDeleting(false);
            }
          }}
          onCancel={() => { setDeletingCourseName(null); setDeleteError(null); }}
        />
      )}
    </>
  );
}
