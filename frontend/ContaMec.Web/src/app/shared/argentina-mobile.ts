import { AbstractControl, ValidationErrors } from '@angular/forms';

// Prefijo internacional de celulares de Argentina; se guarda junto al número (formato de WhatsApp).
export const ARGENTINA_MOBILE_PREFIX = '549';

export function onlyDigits(value: string | null | undefined): string {
  return (value ?? '').replace(/\D/g, '');
}

// Código de área + número, sin 0 ni 15: siempre 10 dígitos. Ningún código de área empieza con 0 ni con 15.
export function argentinaMobileValidator(control: AbstractControl): ValidationErrors | null {
  const digits = onlyDigits(control.value);
  if (!digits) return null;
  if (digits.startsWith('0')) return { mobileLeadingZero: true };
  if (digits.startsWith('15')) return { mobileFifteen: true };
  if (digits.length !== 10) return { mobileLength: true };
  return null;
}
