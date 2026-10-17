import { AbstractControl, ValidationErrors } from '@angular/forms';

/** Misma política que valida el backend (PasswordPolicy): 8 caracteres, 2 números y 1 carácter especial. */
export interface PasswordRule {
  key: 'minLength' | 'digits' | 'special';
  label: string;
  ok: boolean;
}

export function passwordRules(value: string | null | undefined): PasswordRule[] {
  const password = value ?? '';
  return [
    { key: 'minLength', label: 'Al menos 8 caracteres', ok: password.length >= 8 },
    { key: 'digits', label: 'Al menos 2 números', ok: (password.match(/\d/g) ?? []).length >= 2 },
    { key: 'special', label: 'Al menos un carácter especial', ok: /[^\p{L}\p{N}\s]/u.test(password) }
  ];
}

/** Un valor vacío es válido: la obligatoriedad se controla aparte (p. ej. al editar un usuario). */
export function passwordPolicyValidator(control: AbstractControl): ValidationErrors | null {
  const value: string = control.value ?? '';
  if (!value) return null;
  const errors: ValidationErrors = {};
  passwordRules(value)
    .filter((rule) => !rule.ok)
    .forEach((rule) => (errors[rule.key] = true));
  return Object.keys(errors).length > 0 ? errors : null;
}
