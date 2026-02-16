import { useTranslation } from 'react-i18next';
import { AdminLayout } from '../layouts/AdminLayout';
import { UserManagementView } from '@/domains/admin/components/views/UserManagementView';

/** Admin users page with user management table. */
export function AdminUsersPage() {
  const { t } = useTranslation();

  return (
    <AdminLayout>
      <div className="space-y-4">
        <div>
          <h2 className="text-2xl font-bold tracking-tight">{t('admin.users.title')}</h2>
          <p className="text-sm text-muted-foreground">
            {t('admin.users.subtitle')}
          </p>
        </div>
        <UserManagementView />
      </div>
    </AdminLayout>
  );
}
