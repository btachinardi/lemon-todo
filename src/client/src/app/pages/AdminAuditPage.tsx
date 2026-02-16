import { AdminLayout } from '../layouts/AdminLayout';
import { AuditLogView } from '@/domains/admin/components/views/AuditLogView';

/** Admin audit log page — filterable, paginated audit trail viewer. */
export function AdminAuditPage() {
  return (
    <AdminLayout>
      <div className="space-y-4">
        <div>
          <h2 className="text-2xl font-bold tracking-tight">Audit Log</h2>
          <p className="text-sm text-muted-foreground">
            View security-relevant actions and system events.
          </p>
        </div>
        <AuditLogView />
      </div>
    </AdminLayout>
  );
}
