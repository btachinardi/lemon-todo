import { AdminLayout } from '../layouts/AdminLayout';
import { UserManagementView } from '@/domains/admin/components/views/UserManagementView';

/** Admin users page with user management table. */
export function AdminUsersPage() {
  return (
    <AdminLayout>
      <div className="space-y-4">
        <div>
          <h2 className="text-2xl font-bold tracking-tight">User Management</h2>
          <p className="text-sm text-muted-foreground">
            View and manage user accounts, roles, and activation status.
          </p>
        </div>
        <UserManagementView />
      </div>
    </AdminLayout>
  );
}
