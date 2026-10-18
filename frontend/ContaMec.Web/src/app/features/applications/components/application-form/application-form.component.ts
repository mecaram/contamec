import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { Application } from '../../models/application.model';

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

  readonly form = this.fb.group({
    name: ['' as string, [Validators.required, Validators.maxLength(100)]],
    active: [true],
    whatsApp: [''],
    email: ['', [Validators.email]],
    address: [''],
    locality: ['']
  });

  constructor(private readonly fb: FormBuilder) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (changes.application || changes.mode) {
      this.form.reset({
        name: this.application?.name ?? '',
        active: this.application?.active ?? true,
        whatsApp: this.application?.whatsApp ?? '',
        email: this.application?.email ?? '',
        address: this.application?.address ?? '',
        locality: this.application?.locality ?? ''
      });
    }
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
      active: !!raw.active,
      whatsApp: raw.whatsApp?.trim() || null,
      email: raw.email?.trim() || null,
      address: raw.address?.trim() || null,
      locality: raw.locality?.trim() || null
    };
    this.saveForm.emit(payload);
  }

  cancel(): void {
    this.cancelForm.emit();
  }
}
