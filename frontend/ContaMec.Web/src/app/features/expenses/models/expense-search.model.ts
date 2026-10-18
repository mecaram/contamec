export interface ExpenseSearch {
  dateFrom?: string | null;
  dateTo?: string | null;
  closureId?: number | null;
  accountId?: number | null;
  paymentAccountId?: number | null;
  amountFrom?: number | null;
  amountTo?: number | null;
  detail?: string | null;
}
