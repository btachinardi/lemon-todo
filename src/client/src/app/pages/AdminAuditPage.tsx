import { AdminLayout } from '../layouts/AdminLayout';

/** Admin audit log page — placeholder for CP4.6. */
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
        <div className="flex flex-col items-center justify-center gap-3 rounded-md border border-dashed py-20">
          <p className="text-muted-foreground">Audit log viewer coming in CP4.6.</p>
        </div>
      </div>
    </AdminLayout>
  );
}
