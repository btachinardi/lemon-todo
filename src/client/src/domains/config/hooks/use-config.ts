import { useQuery } from '@tanstack/react-query';
import { configApi } from '../api/config.api';

export const configKeys = {
  all: ['config'] as const,
  app: () => [...configKeys.all, 'app'] as const,
};

export function useDemoAccountsEnabled() {
  const query = useQuery({
    queryKey: configKeys.app(),
    queryFn: configApi.getConfig,
    staleTime: Infinity,
    select: (data) => data.enableDemoAccounts,
  });

  return query;
}
