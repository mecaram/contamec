export interface PaymentAccountSearch {
  id?: number | null;
  name?: string | null;
  /** Solo las cuentas de pago activas, para los combos. */
  onlyActive?: boolean;
}
