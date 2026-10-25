/** Un plan activado para la empresa (registro de la tabla Plans). */
export interface PlanHistory {
  id: number;
  /** Cuándo se activó el plan. */
  createdAt: string;
  /** "Demo" o "Pro+". */
  plan?: string | null;
  /** Desde cuándo rige el período. */
  startDate?: string | null;
  /** Último día del período, inclusive. */
  expirationDate?: string | null;
  /** Días hasta el vencimiento (fecha de Argentina): 0 si vence hoy, negativo si venció; null si no vence. */
  daysLeft?: number | null;
}
