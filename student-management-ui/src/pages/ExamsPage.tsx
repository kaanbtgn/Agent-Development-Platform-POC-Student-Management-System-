import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { useForm, type Resolver } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { ExamGradeTable } from '@/components/organisms/ExamGradeTable';
import { HumanInTheLoopModal } from '@/components/organisms/HumanInTheLoopModal';
import { Button } from '@/components/atoms/Button';
import { Input } from '@/components/atoms/Input';
import { Spinner } from '@/components/atoms/Spinner';
import { useExamGrades } from '@/hooks/useExamGrades';
import type { ExamGradeDto } from '@/types/exam.types';

const schema = z.object({
  courseName: z.string().min(1, 'Ders adı zorunludur'),
  exam1Grade: z.coerce.number().min(0).max(100).optional().or(z.literal('')),
  exam2Grade: z.coerce.number().min(0).max(100).optional().or(z.literal('')),
});
type FormData = z.infer<typeof schema>;

export function ExamsPage() {
  const { studentId } = useParams<{ studentId: string }>();
  const { grades, loading, upsertResult, fetchGrades, upsert, remove, clearResult } = useExamGrades(studentId ?? '');
  const [editing, setEditing] = useState<ExamGradeDto | null>(null);
  const [showForm, setShowForm] = useState(false);

  const form = useForm<FormData>({ resolver: zodResolver(schema) as Resolver<FormData> });

  useEffect(() => { if (studentId) fetchGrades(); }, [studentId, fetchGrades]);

  const handleSubmit = form.handleSubmit(async (data) => {
    const result = await upsert(data.courseName, {
      exam1Grade: data.exam1Grade === '' ? undefined : (data.exam1Grade as number | undefined),
      exam2Grade: data.exam2Grade === '' ? undefined : (data.exam2Grade as number | undefined),
    });
    setShowForm(false);
    form.reset();
    if (result.needsHumanVerification) setEditing(null);
  });

  const handleEdit = (g: ExamGradeDto) => {
    setEditing(g);
    form.setValue('courseName', g.courseName);
    form.setValue('exam1Grade', g.exam1Grade ?? '');
    form.setValue('exam2Grade', g.exam2Grade ?? '');
    setShowForm(true);
  };

  if (!studentId) return <p className="p-8 text-white/50">Öğrenci seçilmedi.</p>;

  return (
    <div className="p-6">
      <div className="mb-4 flex items-center justify-between">
        <h1 className="text-base font-semibold text-white">Sınav Notları</h1>
        <Button size="sm" onClick={() => { setEditing(null); form.reset(); setShowForm(true); }}>
          + Not Ekle
        </Button>
      </div>

      {loading ? <Spinner /> : <ExamGradeTable grades={grades} onEdit={handleEdit} onDelete={remove} />}

      {showForm && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
          <div className="w-full max-w-sm rounded-xl bg-white p-6 shadow-xl">
            <h2 className="mb-4 text-base font-semibold">{editing ? 'Not Düzenle' : 'Yeni Not'}</h2>
            <form onSubmit={handleSubmit} className="flex flex-col gap-3">
              <Input
                label="Ders Adı"
                {...form.register('courseName')}
                error={form.formState.errors.courseName?.message}
                disabled={!!editing}
              />
              <Input label="1. Sınav (opsiyonel)" type="number" step="0.5" {...form.register('exam1Grade')} />
              <Input label="2. Sınav (opsiyonel)" type="number" step="0.5" {...form.register('exam2Grade')} />
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
          onConfirm={() => { clearResult(); setShowForm(false); fetchGrades(); }}
          onCancel={() => { clearResult(); setShowForm(false); form.reset(); }}
        />
      )}
    </div>
  );
}
