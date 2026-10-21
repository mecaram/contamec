import { HttpErrorResponse } from '@angular/common/http';
import { Component } from '@angular/core';
import { AbstractControl, FormBuilder, ValidationErrors, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { passwordPolicyValidator, passwordRules, PasswordRule } from '../../auth/password-policy';
import { argentinaMobileValidator, ARGENTINA_MOBILE_PREFIX, onlyDigits } from '../../shared/argentina-mobile';
import { documentNumberLengthMessage, documentNumberValidator, DOCUMENT_TYPES, keepDigits } from '../../shared/document';
import { RegisterCompanyService } from './register-company.service';

function passwordsMatchValidator(group: AbstractControl): ValidationErrors | null {
  const password = group.get('password')?.value ?? '';
  const confirmPassword = group.get('confirmPassword')?.value ?? '';
  return password && password !== confirmPassword ? { mismatch: true } : null;
}

@Component({
  selector: 'app-register-company',
  templateUrl: './register-company.component.html',
  styleUrls: ['./register-company.component.scss']
})
export class RegisterCompanyComponent {
  // Mismas reglas que los formularios de administración de aplicaciones y usuarios.
  readonly form = this.fb.group(
    {
      name: ['', [Validators.required, Validators.maxLength(100)]],
      documentType: ['', [Validators.required]],
      documentNumber: ['', [Validators.required, documentNumberValidator]],
      whatsApp: ['', [argentinaMobileValidator]],
      email: ['', [Validators.email]],
      address: [''],
      locality: [''],
      username: ['', [Validators.required, Validators.maxLength(80), Validators.pattern(/^\S*$/)]],
      password: ['', [Validators.required, passwordPolicyValidator]],
      confirmPassword: ['', [Validators.required]]
    },
    { validators: passwordsMatchValidator }
  );

  readonly visible: Record<'password' | 'confirmPassword', boolean> = {
    password: false,
    confirmPassword: false
  };

  readonly documentTypes = DOCUMENT_TYPES;

  submitting = false;
  registered = false;
  errorMessage: string | null = null;

  constructor(
    private readonly fb: FormBuilder,
    private readonly registerCompanyService: RegisterCompanyService,
    private readonly router: Router
  ) {}

  /** Al cambiar el tipo cambia la cantidad de dígitos esperada. */
  onDocumentTypeChange(): void {
    this.form.controls.documentNumber.updateValueAndValidity();
  }

  /** Solo números: se descarta cualquier otro carácter mientras se escribe o se pega. */
  onDocumentNumberInput(input: HTMLInputElement): void {
    this.form.controls.documentNumber.setValue(keepDigits(input));
  }

  get documentNumberLengthMessage(): string {
    return documentNumberLengthMessage(this.form.controls.documentType.value);
  }

  get passwordRules(): PasswordRule[] {
    return passwordRules(this.form.controls.password.value);
  }

  get showMismatch(): boolean {
    return this.form.hasError('mismatch') && !!this.form.controls.confirmPassword.touched;
  }

  showError(controlName: string, error: string): boolean {
    const control = this.form.get(controlName);
    return !!control && control.touched && control.hasError(error);
  }

  toggleVisible(field: 'password' | 'confirmPassword'): void {
    this.visible[field] = !this.visible[field];
  }

  isVisible(field: 'password' | 'confirmPassword'): boolean {
    return this.visible[field];
  }

  submit(): void {
    if (this.submitting) return;
    this.errorMessage = null;

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    const whatsApp = onlyDigits(raw.whatsApp);
    this.submitting = true;
    this.form.disable();
    this.registerCompanyService
      .register({
        name: raw.name.trim(),
        documentType: raw.documentType,
        documentNumber: raw.documentNumber,
        whatsApp: whatsApp ? ARGENTINA_MOBILE_PREFIX + whatsApp : null,
        email: raw.email?.trim() || null,
        address: raw.address?.trim() || null,
        locality: raw.locality?.trim() || null,
        username: raw.username.trim(),
        // Igual que el formulario de usuarios: la contraseña se guarda sin espacios en los extremos.
        password: raw.password.trim(),
        confirmPassword: raw.confirmPassword.trim()
      })
      .subscribe({
        next: () => {
          this.submitting = false;
          this.registered = true;
        },
        error: (err: HttpErrorResponse) => {
          this.submitting = false;
          this.form.enable();
          this.errorMessage = this.toErrorMessage(err);
        }
      });
  }

  backToLogin(): void {
    void this.router.navigateByUrl('/login');
  }

  /** Solo se muestran los mensajes de validación del backend; cualquier otro error, genérico. */
  private toErrorMessage(err: HttpErrorResponse): string {
    if (err.status === 400 && typeof err.error?.message === 'string') {
      return err.error.message === 'Ya existe un usuario con ese nombre.'
        ? 'El nombre de usuario no está disponible. Elija otro.'
        : err.error.message;
    }
    if (err.status === 429) {
      return 'Se realizaron demasiados intentos de registro. Espere unos minutos y vuelva a intentarlo.';
    }
    if (err.status === 0) {
      return 'No hay respuesta del servidor. Verifique su conexión e intente nuevamente.';
    }
    return 'No fue posible registrar la empresa. Intente nuevamente más tarde.';
  }
}
