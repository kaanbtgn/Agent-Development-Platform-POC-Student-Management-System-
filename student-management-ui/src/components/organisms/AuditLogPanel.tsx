import { useEffect, useState } from 'react';
import { auditApi } from '@/api/auditApi';
import type { AuditEntry } from '@/types/audit.types';
import { Spinner } from '@/components/atoms/Spinner';

interface AuditLogPanelProps {
  studentId: string;
}

export function AuditLogPanel({ studentId }: AuditLogPanelProps) {
  const [entries, setEntries] = useState<AuditEntry[]>([]);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (!studentId) return;
    setLoading(true);
    auditApi
      .getByStudent(studentId)
      .then(setEntries)
      .finally(() => setLoading(false));
  }, [studentId]);

  if (loading) {
    return (
      <div className="flex justify-center py-8">
        <Spinner />
      </div>
    );
  }

  const parsePayload = (value: AuditEntry['newValues']) => {
    if (!value) return null;
    if (typeof value !== 'string') return value;
    try {
      return JSON.parse(value) as Record<string, unknown>;
    } catch {
      return value;
    }
  };

  return (
    <div className="flex flex-col gap-2">
      {entries.length === 0 && (
        <p className="py-8 text-center text-sm text-gray-400">Audit kaydı bulunamadı.</p>
      )}
      {entries.map((entry) => (
        <div key={entry.id} className="rounded-lg border border-gray-200 bg-gray-50 p-3 text-xs">
          <div className="mb-1 flex items-center justify-between gap-2">
            <span className="font-semibold uppercase text-gray-700">{entry.action}</span>
            <span className="text-gray-400">
              {new Date(entry.timestamp).toLocaleString('tr-TR')}
            </span>
          </div>
          <p className="mb-1 text-[11px] uppercase tracking-wide text-gray-500">{entry.entityType}</p>
          {entry.newValues && (
            <>
              <p className="mb-1 font-medium text-emerald-700">Yeni Değerler</p>
              <pre className="overflow-x-auto text-gray-600">
                {JSON.stringify(parsePayload(entry.newValues), null, 2)}
              </pre>
            </>
          )}
          {entry.oldValues && (
            <>
              <p className="mb-1 mt-2 font-medium text-amber-700">Eski Değerler</p>
              <pre className="overflow-x-auto text-gray-600">
                {JSON.stringify(parsePayload(entry.oldValues), null, 2)}
              </pre>
            </>
          )}
        </div>
      ))}
    </div>
  );
}
