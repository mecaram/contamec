export interface Closure {
  id: number;
  closureNumber?: number | null;
  openDate?: string | null;
  previousBalance?: number | null;
  incomes?: number | null;
  expenses?: number | null;
  netFlow?: number | null;
  inAccount?: number | null;
  closeDate?: string | null;
  isClosed?: boolean | null;
  result?: number | null;
  /** Mes (yyyy-MM) en que el cierre tuvo más ingresos y egresos; null si no tiene movimientos. */
  predominantMonth?: string | null;
}
