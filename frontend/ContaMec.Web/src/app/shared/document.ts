import { AbstractControl, ValidationErrors } from '@angular/forms';

/** Etiquetas del enum DocumentTypes de la base. */
export const DOCUMENT_TYPES = ['CUIL', 'CUIT', 'DNI'] as const;

/** Solo números; DNI: 7 u 8 dígitos; CUIT y CUIL: 11. El tipo se lee del control hermano documentType. */
export function documentNumberValidator(control: AbstractControl): ValidationErrors | null {
  const value: string = control.value ?? '';
  if (!value) return null;
  if (!/^\d+$/.test(value)) return { digits: true };
  const type = control.parent?.get('documentType')?.value;
  if (type === 'DNI' && (value.length < 7 || value.length > 8)) return { documentLength: true };
  if ((type === 'CUIT' || type === 'CUIL') && value.length !== 11) return { documentLength: true };
  return null;
}

/** Tipo y número de documento van juntos: si uno tiene valor, el otro es obligatorio. */
export function documentPairValidator(sibling: 'documentType' | 'documentNumber') {
  return (control: AbstractControl): ValidationErrors | null =>
    control.parent?.get(sibling)?.value && !control.value ? { required: true } : null;
}

export function documentNumberLengthMessage(type: string | null | undefined): string {
  return type === 'DNI' ? 'El DNI debe tener 7 u 8 dígitos.' : `El ${type} debe tener 11 dígitos.`;
}

/** Deja solo los dígitos en el input (al escribir o pegar) y devuelve el valor limpio. */
export function keepDigits(input: HTMLInputElement): string {
  const digits = input.value.replace(/\D/g, '');
  if (digits !== input.value) input.value = digits;
  return digits;
}
