export interface Application {
  id: number;
  name?: string | null;
  active: boolean;
  whatsApp?: string | null;
  email?: string | null;
  address?: string | null;
  locality?: string | null;
  guid?: string | null;
  createdAt?: string | null;
}
