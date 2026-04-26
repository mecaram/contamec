export interface Closure {
  id: number;
  openDate?: string | null;
  previousBalance?: number | null;
  incomes?: number | null;
  expenses?: number | null;
  inAccount?: number | null;
  closeDate?: string | null;
  isClosed?: boolean | null;
  result?: number | null;
}
