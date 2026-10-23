import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { AbstractControl, FormBuilder, ValidationErrors, Validators } from '@angular/forms';
import { argentinaMobileValidator, ARGENTINA_MOBILE_PREFIX, onlyDigits } from '../../../../shared/argentina-mobile';
import { documentNumberLengthMessage, documentNumberValidator, DOCUMENT_TYPES, keepDigits } from '../../../../shared/document';
import { Application } from '../../models/application.model';

/** Igual que Solicitar demo gratis: sin espacios en los extremos y sin distinguir mayúsculas. */
function emailsMatchValidator(group: AbstractControl): ValidationErrors | null {
  const email = (group.get('email')?.value ?? '').trim().toLowerCase();
  const confirmEmail = (group.get('confirmEmail')?.value ?? '').trim().toLowerCase();
  return email && confirmEmail && email !== confirmEmail ? { emailMismatch: true } : null;
}

@Component({
  selector: 'app-application-form',
  templateUrl: './application-form.component.html',
  styleUrls: ['./application-form.component.scss']
})
export class ApplicationFormComponent implements OnChanges {
  @Input() mode: 'create' | 'edit' = 'create';
  @Input() application: Application | null = null;
  @Output() cancelForm = new EventEmitter<void>();
  @Output() saveForm = new EventEmitter<Application>();

  // Mismos campos y reglas que Solicitar demo gratis. Las empresas anteriores sin documento lo cargan al editarlas.
  readonly form = this.fb.group(
    {
      name: ['' as string, [Validators.required, Validators.maxLength(100)]],
      documentType: ['' as string, [Validators.required]],
      documentNumber: ['' as string, [Validators.required, documentNumberValidator]],
      active: [true],
      whatsApp: ['', [argentinaMobileValidator]],
      email: ['', [Validators.required, Validators.email]],
      confirmEmail: ['', [Validators.required]],
      address: [''],
      locality: ['']
    },
    { validators: emailsMatchValidator }
  );

  readonly documentTypes = DOCUMENT_TYPES;

  constructor(private readonly fb: FormBuilder) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (changes.application || changes.mode) {
      this.form.reset({
        name: this.application?.name ?? '',
        documentType: this.application?.documentType ?? '',
        documentNumber: this.application?.documentNumber ?? '',
        active: this.application?.active ?? true,
        whatsApp: this.toLocalMobile(this.application?.whatsApp),
        email: this.application?.email ?? '',
        // Al editar ya está confirmado; si se cambia el correo hay que volver a escribir la repetición.
        confirmEmail: this.application?.email ?? '',
        address: this.application?.address ?? '',
        locality: this.application?.locality ?? ''
      });
    }
  }

  /** Al cambiar el tipo cambia la cantidad de dígitos esperada. */
  onDocumentTypeChange(): void {
    this.form.controls.documentNumber.updateValueAndValidity();
  }

  /** Solo números: se descarta cualquier otro carácter mientras se escribe o se pega. */
  onDocumentNumberInput(input: HTMLInputElement): void {
    this.form.controls.documentNumber.setValue(keepDigits(input));
  }

  get showEmailMismatch(): boolean {
    return this.form.hasError('emailMismatch') && !!this.form.controls.confirmEmail.touched;
  }

  showError(controlName: string, error: string): boolean {
    const control = this.form.get(controlName);
    return !!control && control.touched && control.hasError(error);
  }

  get documentNumberLengthMessage(): string {
    return documentNumberLengthMessage(this.form.controls.documentType.value);
  }

  get title(): string {
    return this.mode === 'create' ? 'Nueva aplicación' : 'Editar aplicación';
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    const payload: Application = {
      id: this.application?.id ?? 0,
      name: raw.name?.trim() || '',
      documentType: raw.documentType,
      documentNumber: raw.documentNumber,
      active: !!raw.active,
      whatsApp: onlyDigits(raw.whatsApp) ? ARGENTINA_MOBILE_PREFIX + onlyDigits(raw.whatsApp) : null,
      email: raw.email.trim(),
      address: raw.address?.trim() || null,
      locality: raw.locality?.trim() || null
    };
    this.saveForm.emit(payload);
  }

  cancel(): void {
    this.cancelForm.emit();
  }

  private toLocalMobile(whatsApp: string | null | undefined): string {
    const digits = onlyDigits(whatsApp);
    return digits.startsWith(ARGENTINA_MOBILE_PREFIX) ? digits.slice(ARGENTINA_MOBILE_PREFIX.length) : digits;
  }
}
