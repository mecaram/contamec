export interface Plan {
  applicationId: number;
  applicationName?: string | null;
  address?: string | null;
  locality?: string | null;
  whatsApp?: string | null;
  active: boolean;
  /** "Demo" o "Pro+"; null si la empresa no tiene plan. */
  plan?: string | null;
  startDate?: string | null;
  /** Último día del plan, inclusive; null si no vence. */
  expirationDate?: string | null;
  /** Días hasta el vencimiento (fecha de Argentina): 0 si vence hoy, negativo si venció; null si no vence. */
  daysLeft?: number | null;
}
