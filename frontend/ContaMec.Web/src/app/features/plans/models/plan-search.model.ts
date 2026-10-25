export interface PlanSearch {
  applicationId?: number | null;
  applicationName?: string | null;
  locality?: string | null;
  active?: boolean | null;
  /** 'Demo', 'ProPlus' o 'none' (sin plan). */
  plan?: string | null;
  /** 'current' (vigentes), 'expired' (vencidos) o 'none' (sin vencimiento). */
  expiration?: string | null;
}
