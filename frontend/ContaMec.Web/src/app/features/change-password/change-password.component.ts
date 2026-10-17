import { Component } from '@angular/core';
import { AbstractControl, FormBuilder, ValidationErrors, Validators } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { passwordPolicyValidator, passwordRules, PasswordRule } from '../../auth/password-policy';
import { ChangePasswordService } from './change-password.service';

type PasswordField = 'currentPassword' | 'newPassword' | 'confirmPassword';

function passwordsMatchValidator(group: AbstractControl): ValidationErrors | null {
  const newPassword = group.get('newPassword')?.value ?? '';
  const confirmPassword = group.get('confirmPassword')?.value ?? '';
  return confirmPassword && newPassword !== confirmPassword ? { mismatch: true } : null;
}

@Component({
  selector: 'app-change-password',
  templateUrl: './change-password.component.html',
  styleUrls: ['./change-password.component.scss']
})
export class ChangePasswordComponent {
  readonly fields: { name: PasswordField; label: string; autocomplete: string }[] = [
    { name: 'currentPassword', label: 'Contraseña actual', autocomplete: 'current-password' },
    { name: 'newPassword', label: 'Contraseña nueva', autocomplete: 'new-password' },
    { name: 'confirmPassword', label: 'Repetir contraseña nueva', autocomplete: 'new-password' }
  ];

  readonly visible: Record<PasswordField, boolean> = {
    currentPassword: false,
    newPassword: false,
    confirmPassword: false
  };

  readonly form = this.fb.group(
    {
      currentPassword: ['', Validators.required],
      newPassword: ['', [Validators.required, passwordPolicyValidator]],
      confirmPassword: ['', Validators.required]
    },
    { validators: passwordsMatchValidator }
  );

  saving = false;
  errorMessage: string | null = null;
  successMessage: string | null = null;

  constructor(
    private readonly fb: FormBuilder,
    private readonly changePasswordService: ChangePasswordService
  ) {}

  get rules(): PasswordRule[] {
    return passwordRules(this.form.controls.newPassword.value);
  }

  get showMismatch(): boolean {
    return this.form.hasError('mismatch') && !!this.form.controls.confirmPassword.touched;
  }

  toggleVisible(field: PasswordField): void {
    this.visible[field] = !this.visible[field];
  }

  showRequired(field: PasswordField): boolean {
    const control = this.form.controls[field];
    return control.hasError('required') && control.touched;
  }

  save(): void {
    this.errorMessage = null;
    this.successMessage = null;
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving = true;
    const raw = this.form.getRawValue();
    this.changePasswordService
      .changePassword({
        currentPassword: raw.currentPassword,
        newPassword: raw.newPassword,
        confirmPassword: raw.confirmPassword
      })
      .pipe(finalize(() => (this.saving = false)))
      .subscribe({
        next: () => {
          this.successMessage = 'La contraseña se cambió correctamente.';
          this.form.reset({ currentPassword: '', newPassword: '', confirmPassword: '' });
        },
        error: (error) => {
          this.errorMessage = error?.error?.message ?? 'No se pudo cambiar la contraseña.';
        }
      });
  }
}
