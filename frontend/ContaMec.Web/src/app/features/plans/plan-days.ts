/** Días antes del vencimiento en que se avisa en ámbar. */
export const EXPIRES_SOON_DAYS = 7;

/** Texto de los días hasta el vencimiento: 0 si vence hoy, negativo si venció. */
export function planDaysLabel(daysLeft: number): string {
  if (daysLeft < 0) return 'Vencido';
  if (daysLeft === 0) return 'Vence hoy';
  return daysLeft === 1 ? 'Falta 1 día' : `Faltan ${daysLeft} días`;
}

/** Color del aviso: vencido en rojo, por vencer en ámbar, el resto en verde. */
export function planDaysTone(daysLeft: number): 'expired' | 'soon' | 'ok' {
  if (daysLeft < 0) return 'expired';
  return daysLeft <= EXPIRES_SOON_DAYS ? 'soon' : 'ok';
}
