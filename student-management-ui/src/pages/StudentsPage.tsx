import { useEffect, useState } from 'react';
import { useForm, type Resolver } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { StudentTable } from '@/components/organisms/StudentTable';
import { AuditLogPanel } from '@/components/organisms/AuditLogPanel';
import { Input } from '@/components/atoms/Input';
import { Button } from '@/components/atoms/Button';
import { Spinner } from '@/components/atoms/Spinner';
import { useStudents } from '@/hooks/useStudents';
import { useStudentStore } from '@/store/studentStore';
import type { StudentDto, UpdateStudentRequest } from '@/types/student.types';
import { usePayments } from '@/hooks/usePayments';
import { useExamGrades } from '@/hooks/useExamGrades';
import { PaymentTable } from '@/components/organisms/PaymentTable';
import { ExamGradeTable } from '@/components/organisms/ExamGradeTable';
import { HumanInTheLoopModal } from '@/components/organisms/HumanInTheLoopModal';
import type { InternshipPaymentDto } from '@/types/payment.types';
import type { ExamGradeDto } from '@/types/exam.types';

const createSchema = z.object({
  firstName: z.string().min(1, 'Ad zorunludur'),
  lastName: z.string().min(1, 'Soyad zorunludur'),
  studentNumber: z.string().min(1, 'Öğrenci numarası zorunludur'),
  department: z.string().min(1, 'Bölüm zorunludur'),
  enrollmentDate: z.string().min(1, 'Kayıt tarihi zorunludur'),
  phone: z.string().optional(),
});
type CreateFormData = z.infer<typeof createSchema>;

const editSchema = z.object({
  firstName: z.string().min(1),
  lastName: z.string().min(1),
  department: z.string().min(1),
  phone: z.string().optional(),
});
type EditFormData = z.infer<typeof editSchema>;

type DetailTab = 'payments' | 'exams' | 'audit';

function useDebounce<T>(value: T, delay: number): T {
  const [debounced, setDebounced] = useState(value);
  useEffect(() => {
    const timer = setTimeout(() => setDebounced(value), delay);
    return () => clearTimeout(timer);
  }, [value, delay]);
  return debounced;
}

export function StudentsPage() {
  const { loading, error, fetchAll, search, create, update, remove } = useStudents();
  const { selectedStudent, setSelectedStudent } = useStudentStore();
  const [searchTerm, setSearchTerm] = useState('');
  const debouncedSearch = useDebounce(searchTerm, 300);
  const [showCreateForm, setShowCreateForm] = useState(false);
  const [editingStudent, setEditingStudent] = useState<StudentDto | null>(null);
  const [detailTab, setDetailTab] = useState<DetailTab>('payments');

  const createForm = useForm<CreateFormData>({ resolver: zodResolver(createSchema) });
  const editForm = useForm<EditFormData>({ resolver: zodResolver(editSchema) });

  useEffect(() => { fetchAll(); }, [fetchAll]);
  useEffect(() => {
    if (debouncedSearch) search(debouncedSearch);
    else fetchAll();
  }, [debouncedSearch, search, fetchAll]);

  const handleCreate = createForm.handleSubmit(async (data) => {
    await create(data);
    createForm.reset();
    setShowCreateForm(false);
  });

  const handleEdit = editForm.handleSubmit(async (data: UpdateStudentRequest) => {
    if (!editingStudent) return;
    await update(editingStudent.id, data);
    setEditingStudent(null);
  });

  const handleDelete = async (id: string) => {
    await remove(id);
    if (selectedStudent?.id === id) setSelectedStudent(null);
  };

  return (
    <div className="flex flex-1 overflow-hidden relative">
      {/* Main: student list */}
      <div className="flex flex-1 flex-col overflow-hidden">
        <header className="flex items-center justify-between px-6 py-4"
          style={{ background: 'rgba(255,255,255,0.07)', borderBottom: '1px solid rgba(255,255,255,0.1)', backdropFilter: 'blur(20px)' }}>
          <div className="flex items-center gap-3">
            <div className="flex h-8 w-8 items-center justify-center rounded-xl text-base"
              style={{ background: 'linear-gradient(135deg, rgba(99,102,241,0.3), rgba(139,92,246,0.2))', border: '1px solid rgba(99,102,241,0.3)' }}>
              🎓
            </div>
            <div>
              <h1 className="text-sm font-bold text-white">Öğrenciler</h1>
              <p className="text-[10px] text-indigo-300/50">Kayıtlı öğrenci listesi</p>
            </div>
          </div>
          <Button size="sm" onClick={() => setShowCreateForm(true)}>+ Yeni Öğrenci</Button>
        </header>

        <div className="px-6 py-3">
          <div className="relative">
            <span className="absolute left-3.5 top-1/2 -translate-y-1/2 text-sm text-white/30">🔍</span>
            <input
              className="h-10 w-full rounded-xl pl-9 pr-4 text-sm text-white placeholder-white/30 focus:outline-none focus:ring-2 focus:ring-indigo-500/50 transition-all"
              style={{ background: 'rgba(255,255,255,0.08)', border: '1px solid rgba(255,255,255,0.12)' }}
              placeholder="Öğrenci ara..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
            />
          </div>
        </div>

        {loading && (
          <div className="flex justify-center py-8"><Spinner /></div>
        )}
        {error && <p className="px-6 text-sm text-red-400">{error}</p>}

        <div className="flex-1 overflow-y-auto px-6 pb-6">
          <StudentTable
            onEdit={(s) => { setEditingStudent(s); editForm.reset(s); }}
            onDelete={handleDelete}
            onSelect={(s) => { setSelectedStudent(s); setDetailTab('payments'); }}
          />
        </div>
      </div>

      {/* Detail panel as slide-in modal overlay */}
      {selectedStudent && (
        <>
          {/* Backdrop */}
          <div
            className="absolute inset-0 z-20 animate-fade-in"
            style={{ background: 'rgba(0,0,0,0.35)', backdropFilter: 'blur(2px)' }}
            onClick={() => setSelectedStudent(null)}
          />
          {/* Slide-in panel */}
          <div className="absolute right-0 top-0 bottom-0 z-30 w-[480px] flex flex-col animate-slide-in-right shadow-2xl"
            style={{ background: 'rgba(255,255,255,0.97)', backdropFilter: 'blur(20px)' }}>
            <DetailPanel
              student={selectedStudent}
              tab={detailTab}
              onTabChange={setDetailTab}
              onClose={() => setSelectedStudent(null)}
            />
          </div>
        </>
      )}

      {/* Create form modal */}
      {showCreateForm && (
        <Modal title="Yeni Öğrenci" onClose={() => setShowCreateForm(false)}>
          <form onSubmit={handleCreate} className="flex flex-col gap-3">
            <Input label="Ad" {...createForm.register('firstName')} error={createForm.formState.errors.firstName?.message} />
            <Input label="Soyad" {...createForm.register('lastName')} error={createForm.formState.errors.lastName?.message} />
            <Input label="Öğrenci No" {...createForm.register('studentNumber')} error={createForm.formState.errors.studentNumber?.message} />
            <Input label="Bölüm" {...createForm.register('department')} error={createForm.formState.errors.department?.message} />
            <Input label="Kayıt Tarihi" type="date" {...createForm.register('enrollmentDate')} error={createForm.formState.errors.enrollmentDate?.message} />
            <Input label="Telefon (opsiyonel)" {...createForm.register('phone')} />
            <div className="flex justify-end gap-2 pt-2">
              <Button variant="secondary" type="button" onClick={() => setShowCreateForm(false)}>İptal</Button>
              <Button type="submit">Kaydet</Button>
            </div>
          </form>
        </Modal>
      )}

      {/* Edit form modal */}
      {editingStudent && (
        <Modal title="Öğrenci Düzenle" onClose={() => setEditingStudent(null)}>
          <form onSubmit={handleEdit} className="flex flex-col gap-3">
            <Input label="Ad" {...editForm.register('firstName')} />
            <Input label="Soyad" {...editForm.register('lastName')} />
            <Input label="Bölüm" {...editForm.register('department')} />
            <Input label="Telefon" {...editForm.register('phone')} />
            <div className="flex justify-end gap-2 pt-2">
              <Button variant="secondary" type="button" onClick={() => setEditingStudent(null)}>İptal</Button>
              <Button type="submit">Güncelle</Button>
            </div>
          </form>
        </Modal>
      )}
    </div>
  );
}

const MONTHS = ['', 'Ocak', 'Şubat', 'Mart', 'Nisan', 'Mayıs', 'Haziran', 'Temmuz', 'Ağustos', 'Eylül', 'Ekim', 'Kasım', 'Aralık'];

const paymentSchema = z.object({
  periodYear: z.coerce.number().int().min(2000).max(2100),
  periodMonth: z.coerce.number().int().min(1).max(12),
  amount: z.coerce.number().positive('Tutar 0\'dan büyük olmalıdır'),
  paymentDate: z.string().optional(),
});
type PaymentFormData = z.infer<typeof paymentSchema>;

const gradeSchema = z.object({
  courseName: z.string().min(1, 'Ders adı zorunludur'),
  exam1Grade: z.coerce.number().min(0).max(100).optional().or(z.literal('')),
  exam2Grade: z.coerce.number().min(0).max(100).optional().or(z.literal('')),
});
type GradeFormData = z.infer<typeof gradeSchema>;

function DetailPanel({
  student,
  tab,
  onTabChange,
  onClose,
}: {
  student: StudentDto;
  tab: DetailTab;
  onTabChange: (t: DetailTab) => void;
  onClose: () => void;
}) {
  const { payments, loading: pLoading, fetchPayments, upsert: upsertPayment, remove: removePayment, upsertResult: paymentUpsertResult, clearResult: clearPaymentResult } = usePayments(student.id);
  const { grades, loading: gLoading, fetchGrades, upsert: upsertGrade, remove: removeGrade, upsertResult: gradeUpsertResult, clearResult: clearGradeResult } = useExamGrades(student.id);

  const [editingPayment, setEditingPayment] = useState<InternshipPaymentDto | null>(null);
  const [showAddPayment, setShowAddPayment] = useState(false);
  const paymentForm = useForm<PaymentFormData>({ resolver: zodResolver(paymentSchema) as Resolver<PaymentFormData> });

  const [editingGrade, setEditingGrade] = useState<ExamGradeDto | null>(null);
  const [showAddGrade, setShowAddGrade] = useState(false);
  const gradeForm = useForm<GradeFormData>({ resolver: zodResolver(gradeSchema) as Resolver<GradeFormData> });

  useEffect(() => { if (tab === 'payments') fetchPayments(); }, [tab, fetchPayments]);
  useEffect(() => { if (tab === 'exams') fetchGrades(); }, [tab, fetchGrades]);

  const openEditPayment = (payment: InternshipPaymentDto) => {
    setEditingPayment(payment);
    setShowAddPayment(false);
    paymentForm.reset({
      periodYear: payment.periodYear,
      periodMonth: payment.periodMonth,
      amount: payment.amount,
      paymentDate: payment.paymentDate ?? '',
    });
  };

  const openAddPayment = () => {
    setEditingPayment(null);
    setShowAddPayment(true);
    paymentForm.reset({
      periodYear: new Date().getFullYear(),
      periodMonth: new Date().getMonth() + 1,
      amount: 0,
      paymentDate: '',
    });
  };

  const closePaymentModal = () => { setEditingPayment(null); setShowAddPayment(false); };

  const handlePaymentSubmit = paymentForm.handleSubmit(async (data: PaymentFormData) => {
    await upsertPayment(data.periodYear, data.periodMonth, {
      amount: data.amount,
      paymentDate: data.paymentDate || undefined,
    });
    closePaymentModal();
  });

  const openEdit = (grade: ExamGradeDto) => {
    setEditingGrade(grade);
    gradeForm.reset({
      courseName: grade.courseName,
      exam1Grade: grade.exam1Grade ?? '',
      exam2Grade: grade.exam2Grade ?? '',
    });
  };

  const openAdd = () => {
    setEditingGrade(null);
    setShowAddGrade(true);
    gradeForm.reset({ courseName: '', exam1Grade: '', exam2Grade: '' });
  };

  const closeGradeModal = () => {
    setEditingGrade(null);
    setShowAddGrade(false);
  };

  const handleGradeSubmit = gradeForm.handleSubmit(async (data) => {
    await upsertGrade(data.courseName, {
      exam1Grade: data.exam1Grade === '' ? undefined : Number(data.exam1Grade),
      exam2Grade: data.exam2Grade === '' ? undefined : Number(data.exam2Grade),
    });
    closeGradeModal();
  });

  const TABS: { key: DetailTab; label: string }[] = [
    { key: 'payments', label: 'Ödemeler' },
    { key: 'exams', label: 'Sınav Notları' },
    { key: 'audit', label: 'Audit' },
  ];

  return (
    <>
      <div className="flex h-full flex-col overflow-hidden">
        {/* Panel header */}
        <div className="flex items-center justify-between px-5 py-4" style={{ background: 'linear-gradient(135deg, #6366f1, #8b5cf6)' }}>
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-white/20 text-base font-bold text-white">
              {student.firstName[0]}{student.lastName[0]}
            </div>
            <div>
              <p className="text-sm font-bold text-white">
                {student.firstName} {student.lastName}
              </p>
              <p className="text-xs text-indigo-200">{student.studentNumber} · {student.department}</p>
            </div>
          </div>
          <button
            className="flex h-7 w-7 items-center justify-center rounded-lg bg-white/20 text-sm text-white transition-colors hover:bg-white/30"
            onClick={onClose}
          >
            ✕
          </button>
        </div>
        {/* Tabs */}
        <div className="flex gap-0" style={{ background: 'rgba(248,248,252,1)', borderBottom: '1px solid #e5e7eb' }}>
          {TABS.map(({ key, label }) => (
            <button
              key={key}
              className={`flex-1 py-2.5 text-xs font-semibold transition-all duration-150 ${
                tab === key
                  ? 'border-b-2 border-indigo-600 text-indigo-700 bg-white'
                  : 'text-gray-400 hover:text-gray-700 hover:bg-gray-50/50'
              }`}
              onClick={() => onTabChange(key)}
            >
              {label}
            </button>
          ))}
        </div>
        <div className="flex-1 overflow-y-auto p-4">
          {tab === 'payments' && (
            <>
              <div className="mb-3 flex justify-end">
                <Button size="sm" onClick={openAddPayment}>+ Yeni Ödeme</Button>
              </div>
              {pLoading ? <Spinner /> : <PaymentTable payments={payments} onEdit={openEditPayment} onDelete={removePayment} />}
            </>
          )}
          {tab === 'exams' && (
            <>
              <div className="mb-3 flex justify-end">
                <Button size="sm" onClick={openAdd}>+ Yeni Not</Button>
              </div>
              {gLoading ? <Spinner /> : <ExamGradeTable grades={grades} onEdit={openEdit} onDelete={removeGrade} />}
            </>
          )}
          {tab === 'audit' && <AuditLogPanel studentId={student.id} />}
        </div>
      </div>

      {/* Ödeme ekle / düzenle modal */}
      {(editingPayment !== null || showAddPayment) && (
        <Modal
          title={editingPayment ? 'Ödeme Düzenle' : 'Yeni Ödeme'}
          onClose={closePaymentModal}
        >
          <form onSubmit={handlePaymentSubmit} className="flex flex-col gap-3">
            <div className="flex gap-2">
              <Input
                label="Yıl"
                type="number"
                className={editingPayment ? 'bg-gray-100 cursor-not-allowed' : ''}
                readOnly={!!editingPayment}
                {...paymentForm.register('periodYear')}
              />
              <div className="flex flex-1 flex-col gap-1">
                <label className="text-xs font-medium text-gray-700">Ay</label>
                <select
                  {...paymentForm.register('periodMonth')}
                  disabled={!!editingPayment}
                  className="rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-1 focus:ring-indigo-500 disabled:bg-gray-100 disabled:cursor-not-allowed"
                >
                  {MONTHS.slice(1).map((name, i) => (
                    <option key={i + 1} value={i + 1}>{name}</option>
                  ))}
                </select>
              </div>
            </div>
            <Input
              label="Tutar (₺)"
              type="number"
              step="0.01"
              {...paymentForm.register('amount')}
              error={paymentForm.formState.errors.amount?.message}
            />
            <Input
              label="Ödeme Tarihi (opsiyonel)"
              type="date"
              {...paymentForm.register('paymentDate')}
            />
            <div className="flex justify-end gap-2 pt-2">
              <Button variant="secondary" type="button" onClick={closePaymentModal}>İptal</Button>
              <Button type="submit">Kaydet</Button>
            </div>
          </form>
        </Modal>
      )}

      {/* Sınav notu ekle / düzenle modal */}
      {(editingGrade !== null || showAddGrade) && (
        <Modal
          title={editingGrade ? 'Sınav Notunu Düzenle' : 'Yeni Sınav Notu'}
          onClose={closeGradeModal}
        >
          <form onSubmit={handleGradeSubmit} className="flex flex-col gap-3">
            <Input
              label="Ders Adı"
              {...gradeForm.register('courseName')}
              readOnly={!!editingGrade}
              className={editingGrade ? 'bg-gray-100 cursor-not-allowed' : ''}
              error={gradeForm.formState.errors.courseName?.message}
            />
            <Input
              label="1. Sınav (0-100)"
              type="number"
              min={0}
              max={100}
              {...gradeForm.register('exam1Grade')}
              error={gradeForm.formState.errors.exam1Grade?.message}
            />
            <Input
              label="2. Sınav (0-100)"
              type="number"
              min={0}
              max={100}
              {...gradeForm.register('exam2Grade')}
              error={gradeForm.formState.errors.exam2Grade?.message}
            />
            <div className="flex justify-end gap-2 pt-2">
              <Button variant="secondary" type="button" onClick={closeGradeModal}>İptal</Button>
              <Button type="submit">Kaydet</Button>
            </div>
          </form>
        </Modal>
      )}

      {paymentUpsertResult?.needsHumanVerification && (
        <HumanInTheLoopModal
          items={paymentUpsertResult.ambiguousItems}
          onConfirm={() => { clearPaymentResult(); fetchPayments(); }}
          onCancel={() => clearPaymentResult()}
        />
      )}
      {gradeUpsertResult?.needsHumanVerification && (
        <HumanInTheLoopModal
          items={gradeUpsertResult.ambiguousItems}
          onConfirm={() => { clearGradeResult(); fetchGrades(); }}
          onCancel={() => clearGradeResult()}
        />
      )}
    </>
  );
}

function Modal({ title, children, onClose }: { title: string; children: React.ReactNode; onClose: () => void }) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center animate-fade-in" style={{ background: 'rgba(0,0,0,0.4)', backdropFilter: 'blur(6px)' }}>
      <div className="w-full max-w-md overflow-hidden rounded-2xl shadow-2xl animate-slide-in-up" style={{ background: 'rgba(255,255,255,0.97)' }}>
        <div className="flex items-center justify-between px-5 py-4" style={{ background: 'linear-gradient(135deg, #6366f1, #8b5cf6)' }}>
          <h2 className="text-sm font-bold text-white">{title}</h2>
          <button
            className="flex h-6 w-6 items-center justify-center rounded-lg bg-white/20 text-xs text-white hover:bg-white/30 transition-colors"
            onClick={onClose}
          >✕</button>
        </div>
        <div className="p-5">{children}</div>
      </div>
    </div>
  );
}
