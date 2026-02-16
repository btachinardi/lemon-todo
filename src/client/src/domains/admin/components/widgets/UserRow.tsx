import { Badge } from '@/ui/badge';
import { Button } from '@/ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/ui/dropdown-menu';
import { MoreHorizontalIcon, ShieldIcon, ShieldOffIcon, UserXIcon, UserCheckIcon } from 'lucide-react';
import { TableCell, TableRow } from '@/ui/table';
import type { AdminUser } from '../../types/admin.types';

const roleBadgeVariant: Record<string, 'default' | 'secondary' | 'destructive' | 'outline'> = {
  User: 'secondary',
  Admin: 'default',
  SystemAdmin: 'destructive',
};

interface UserRowProps {
  user: AdminUser;
  isSystemAdmin: boolean;
  onAssignRole: (user: AdminUser) => void;
  onRemoveRole: (userId: string, roleName: string) => void;
  onDeactivate: (userId: string) => void;
  onReactivate: (userId: string) => void;
}

/** Single row in the admin user management table. */
export function UserRow({
  user,
  isSystemAdmin,
  onAssignRole,
  onRemoveRole,
  onDeactivate,
  onReactivate,
}: UserRowProps) {
  return (
    <TableRow className={!user.isActive ? 'opacity-60' : ''}>
      <TableCell className="font-mono text-xs">{user.id.slice(0, 8)}...</TableCell>
      <TableCell>{user.email}</TableCell>
      <TableCell>{user.displayName}</TableCell>
      <TableCell>
        <div className="flex flex-wrap gap-1">
          {user.roles.map((role) => (
            <Badge
              key={role}
              variant={roleBadgeVariant[role] ?? 'outline'}
              className="text-xs"
            >
              {role}
            </Badge>
          ))}
        </div>
      </TableCell>
      <TableCell>
        <Badge variant={user.isActive ? 'secondary' : 'destructive'} className="text-xs">
          {user.isActive ? 'Active' : 'Deactivated'}
        </Badge>
      </TableCell>
      <TableCell>
        {isSystemAdmin && (
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="icon" className="size-8">
                <MoreHorizontalIcon className="size-4" />
                <span className="sr-only">Actions</span>
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem onClick={() => onAssignRole(user)}>
                <ShieldIcon className="mr-2 size-4" />
                Assign Role
              </DropdownMenuItem>
              {user.roles.length > 1 && (
                <DropdownMenuItem
                  onClick={() => {
                    // Remove the highest role that isn't "User"
                    const removable = user.roles.filter((r) => r !== 'User');
                    if (removable.length > 0) {
                      onRemoveRole(user.id, removable[removable.length - 1]);
                    }
                  }}
                >
                  <ShieldOffIcon className="mr-2 size-4" />
                  Remove Role
                </DropdownMenuItem>
              )}
              {user.isActive ? (
                <DropdownMenuItem
                  onClick={() => onDeactivate(user.id)}
                  className="text-destructive focus:text-destructive"
                >
                  <UserXIcon className="mr-2 size-4" />
                  Deactivate
                </DropdownMenuItem>
              ) : (
                <DropdownMenuItem onClick={() => onReactivate(user.id)}>
                  <UserCheckIcon className="mr-2 size-4" />
                  Reactivate
                </DropdownMenuItem>
              )}
            </DropdownMenuContent>
          </DropdownMenu>
        )}
      </TableCell>
    </TableRow>
  );
}
