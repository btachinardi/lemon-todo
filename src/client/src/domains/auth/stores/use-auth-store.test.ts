import { describe, it, expect, beforeEach } from 'vitest';
import { useAuthStore } from './use-auth-store';

describe('use-auth-store', () => {
  beforeEach(() => {
    useAuthStore.setState({
      accessToken: null,
      user: null,
      isAuthenticated: false,
    });
  });

  it('should store roles in user profile via setAuth', () => {
    const user = {
      id: '1',
      email: 'admin@test.com',
      displayName: 'Admin',
      roles: ['User', 'Admin'],
    };
    useAuthStore.getState().setAuth('token', user);

    const stored = useAuthStore.getState().user;
    expect(stored).not.toBeNull();
    expect(stored!.roles).toEqual(['User', 'Admin']);
  });

  it('should preserve roles through state updates', () => {
    const user = {
      id: '1',
      email: 'sysadmin@test.com',
      displayName: 'SysAdmin',
      roles: ['User', 'SystemAdmin'],
    };
    useAuthStore.getState().setAuth('token', user);
    useAuthStore.getState().setAccessToken('new-token');

    const stored = useAuthStore.getState().user;
    expect(stored!.roles).toEqual(['User', 'SystemAdmin']);
  });

  it('should clear roles on logout', () => {
    useAuthStore.getState().setAuth('token', {
      id: '1',
      email: 'admin@test.com',
      displayName: 'Admin',
      roles: ['User', 'Admin'],
    });
    useAuthStore.getState().logout();

    expect(useAuthStore.getState().user).toBeNull();
  });
});
