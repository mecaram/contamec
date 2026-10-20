export interface Expense {
  id: number;
  closureId?: number | null;
  emissionDate?: string | null;
  accountId?: number | null;
  accountName?: string | null;
  detail?: string | null;
  amount?: number | null;
  paymentAccountId?: number | null;
  paymentAccountName?: string | null;
  subAccountId?: number | null;
  subAccountName?: string | null;
  occurrenceType?: string | null;
  createdByUserId?: number | null;
  createdAt?: string | null;
}
