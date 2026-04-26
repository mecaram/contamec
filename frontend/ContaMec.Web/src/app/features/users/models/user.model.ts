export interface User {
  id: number;
  name?: string | null;
  isActive: boolean;
  userRoleId?: number | null;
  userRoleName?: string | null;
  password?: string | null;
}
