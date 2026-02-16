import { useTranslation } from 'react-i18next';
import { AdminLayout } from '../layouts/AdminLayout';
import { AuditLogView } from '@/domains/admin/components/views/AuditLogView';

/** Admin audit log page — filterable, paginated audit trail viewer. */
export function AdminAuditPage() {
  const { t } = useTranslation();

  return (
    <AdminLayout>
      <div className="space-y-4">
        <div>
          <h2 className="text-2xl font-bold tracking-tight">{t('admin.audit.title')}</h2>
          <p className="text-sm text-muted-foreground">
            {t('admin.audit.subtitle')}
          </p>
        </div>
        <AuditLogView />
      </div>
    </AdminLayout>
  );
}
