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

/** Igual que el backend: sin espacios en los extremos y sin distinguir mayúsculas. */
function repeatedValueValidator(field: string, confirmField: string, error: string) {
  return (group: AbstractControl): ValidationErrors | null => {
    const value = (group.get(field)?.value ?? '').trim().toLowerCase();
    const confirmValue = (group.get(confirmField)?.value ?? '').trim().toLowerCase();
    return value && confirmValue && value !== confirmValue ? { [error]: true } : null;
  };
}

const emailsMatchValidator = repeatedValueValidator('email', 'confirmEmail', 'emailMismatch');
// El login no distingue mayúsculas en el usuario.
const usernamesMatchValidator = repeatedValueValidator('username', 'confirmUsername', 'usernameMismatch');

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
      email: ['', [Validators.required, Validators.email]],
      confirmEmail: ['', [Validators.required]],
      address: [''],
      locality: [''],
      username: ['', [Validators.required, Validators.maxLength(80), Validators.pattern(/^\S*$/)]],
      confirmUsername: ['', [Validators.required]],
      password: ['', [Validators.required, passwordPolicyValidator]],
      confirmPassword: ['', [Validators.required]]
    },
    { validators: [passwordsMatchValidator, emailsMatchValidator, usernamesMatchValidator] }
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

  get showEmailMismatch(): boolean {
    return this.form.hasError('emailMismatch') && !!this.form.controls.confirmEmail.touched;
  }

  get showUsernameMismatch(): boolean {
    return this.form.hasError('usernameMismatch') && !!this.form.controls.confirmUsername.touched;
  }

  /** La repetición se escribe a mano: pegar el valor copiado no detectaría un error de tipeo. */
  preventPaste(event: ClipboardEvent): void {
    event.preventDefault();
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
        email: raw.email.trim(),
        confirmEmail: raw.confirmEmail.trim(),
        address: raw.address?.trim() || null,
        locality: raw.locality?.trim() || null,
        username: raw.username.trim(),
        confirmUsername: raw.confirmUsername.trim(),
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
