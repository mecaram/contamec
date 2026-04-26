export interface Income {
  id: number;
  closureId?: number | null;
  emissionDate?: string | null;
  accountId?: number | null;
  accountName?: string | null;
  detail?: string | null;
  amount?: number | null;
  createdByUserId?: number | null;
  createdAt?: string | null;
}
