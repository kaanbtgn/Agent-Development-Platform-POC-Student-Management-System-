import { useState } from 'react';
import type { StudentDto } from '@/types/student.types';
import { Button } from '@/components/atoms/Button';
import { ConfirmationDialog } from '@/components/molecules/ConfirmationDialog';
import { useStudentStore } from '@/store/studentStore';

interface StudentTableProps {
  onEdit: (student: StudentDto) => void;
  onDelete: (id: string) => Promise<void>;
  onSelect: (student: StudentDto) => void;
}

function initials(first: string, last: string) {
  return `${first[0] ?? ''}${last[0] ?? ''}`.toUpperCase();
}

const avatarColors = [
  'from-indigo-500 to-violet-600',
  'from-pink-500 to-rose-600',
  'from-teal-500 to-cyan-600',
  'from-amber-500 to-orange-600',
  'from-blue-500 to-indigo-600',
  'from-emerald-500 to-green-600',
];

export function StudentTable({ onEdit, onDelete, onSelect }: StudentTableProps) {
  const { students, selectedStudent } = useStudentStore();
  const [deletingId, setDeletingId] = useState<string | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);
  const [deleteError, setDeleteError] = useState<string | null>(null);

  return (
    <>
      <div className="overflow-hidden rounded-2xl border border-white/30 shadow-xl" style={{ background: 'rgba(255,255,255,0.9)', backdropFilter: 'blur(20px)' }}>
        <table className="min-w-full divide-y divide-gray-100/80 text-sm">
          <thead>
            <tr style={{ background: 'linear-gradient(135deg, rgba(99,102,241,0.08), rgba(139,92,246,0.05))' }}>
              {['Öğrenci No', 'Öğrenci', 'Bölüm', 'Telefon', 'Kayıt Tarihi', 'İşlemler'].map((h) => (
                <th key={h} className="px-4 py-3.5 text-left text-[10px] font-bold uppercase tracking-widest text-gray-500">
                  {h}
                </th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-100/80">
            {students.map((student, idx) => {
              const isSelected = selectedStudent?.id === student.id;
              const colorClass = avatarColors[idx % avatarColors.length];
              return (
                <tr
                  key={student.id}
                  className={`cursor-pointer transition-all duration-150 ${
                    isSelected
                      ? 'bg-indigo-50/80'
                      : 'hover:bg-indigo-50/40'
                  }`}
                  onClick={() => onSelect(student)}
                >
                  <td className="px-4 py-3">
                    <code className="rounded-lg bg-indigo-50 px-2 py-0.5 text-xs font-bold text-indigo-600">
                      {student.studentNumber}
                    </code>
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex items-center gap-3">
                      <div className={`flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-gradient-to-br ${colorClass} text-[11px] font-bold text-white shadow-md`}>
                        {initials(student.firstName, student.lastName)}
                      </div>
                      <span className="font-semibold text-gray-800">{student.firstName} {student.lastName}</span>
                    </div>
                  </td>
                  <td className="px-4 py-3">
                    <span className="rounded-full bg-gray-100 px-2.5 py-0.5 text-xs font-medium text-gray-600">
                      {student.department}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-gray-500 text-xs">{student.phone ?? '—'}</td>
                  <td className="px-4 py-3 text-gray-500 text-xs">{student.enrollmentDate}</td>
                  <td className="px-4 py-3">
                    <div className="flex gap-2" onClick={(e) => e.stopPropagation()}>
                      <Button size="sm" variant="secondary" onClick={() => onEdit(student)}>Düzenle</Button>
                      <Button size="sm" variant="danger" onClick={() => setDeletingId(student.id)}>Sil</Button>
                    </div>
                  </td>
                </tr>
              );
            })}
            {students.length === 0 && (
              <tr>
                <td colSpan={6} className="px-4 py-12 text-center">
                  <div className="flex flex-col items-center gap-2 text-gray-400">
                    <span className="text-3xl">🎓</span>
                    <p className="text-sm font-medium">Öğrenci bulunamadı.</p>
                  </div>
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {deletingId && (
        <ConfirmationDialog
          title="Öğrenciyi Sil"
          description="Bu öğrencinin kişisel verileri anonim hale getirilecek ve listeden kaldırılacak."
          confirmLabel={isDeleting ? 'Siliniyor…' : 'Evet, Sil'}
          confirmDisabled={isDeleting}
          error={deleteError}
          onConfirm={async () => {
            setIsDeleting(true);
            setDeleteError(null);
            try {
              await onDelete(deletingId);
              setDeletingId(null);
            } catch {
              setDeleteError('Silme işlemi başarısız. Lütfen tekrar deneyin.');
            } finally {
              setIsDeleting(false);
            }
          }}
          onCancel={() => { setDeletingId(null); setDeleteError(null); }}
        />
      )}
    </>
  );
}
