export interface AuditEntry {
  id: string;
  studentId?: string;
  entityId: string;
  entityType: string;
  action: string;
  timestamp: string;
  oldValues?: string | Record<string, unknown>;
  newValues?: string | Record<string, unknown>;
}
