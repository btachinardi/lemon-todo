import { apiClient } from '@/lib/api-client';

interface AppConfig {
  enableDemoAccounts: boolean;
}

export const configApi = {
  getConfig: () => apiClient.get<AppConfig>('/api/config'),
};
