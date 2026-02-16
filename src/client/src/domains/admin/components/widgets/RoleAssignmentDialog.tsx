import { useState } from 'react';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/ui/dialog';
import { Button } from '@/ui/button';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/ui/select';
import type { AdminUser } from '../../types/admin.types';

const AVAILABLE_ROLES = ['User', 'Admin', 'SystemAdmin'] as const;

interface RoleAssignmentDialogProps {
  user: AdminUser | null;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onAssign: (userId: string, roleName: string) => void;
  isPending: boolean;
}

/** Dialog for assigning a role to a user. Shows only roles the user doesn't already have. */
export function RoleAssignmentDialog({
  user,
  open,
  onOpenChange,
  onAssign,
  isPending,
}: RoleAssignmentDialogProps) {
  const [selectedRole, setSelectedRole] = useState<string>('');

  const assignableRoles = AVAILABLE_ROLES.filter(
    (r) => !user?.roles.includes(r),
  );

  const handleAssign = () => {
    if (user && selectedRole) {
      onAssign(user.id, selectedRole);
      setSelectedRole('');
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Assign Role</DialogTitle>
          <DialogDescription>
            Assign a role to {user?.displayName ?? 'this user'}.
          </DialogDescription>
        </DialogHeader>
        <div className="py-4">
          <Select value={selectedRole} onValueChange={setSelectedRole}>
            <SelectTrigger>
              <SelectValue placeholder="Select a role" />
            </SelectTrigger>
            <SelectContent>
              {assignableRoles.map((role) => (
                <SelectItem key={role} value={role}>
                  {role}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
          {assignableRoles.length === 0 && (
            <p className="mt-2 text-sm text-muted-foreground">
              User already has all available roles.
            </p>
          )}
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Cancel
          </Button>
          <Button
            onClick={handleAssign}
            disabled={!selectedRole || isPending}
          >
            {isPending ? 'Assigning...' : 'Assign'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
