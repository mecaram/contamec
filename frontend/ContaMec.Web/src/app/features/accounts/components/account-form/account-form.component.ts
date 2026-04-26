import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { Account } from '../../models/account.model';

@Component({
  selector: 'app-account-form',
  templateUrl: './account-form.component.html',
  styleUrls: ['./account-form.component.scss']
})
export class AccountFormComponent implements OnChanges {
  @Input() mode: 'create' | 'edit' = 'create';
  @Input() account: Account | null = null;
  @Output() cancelForm = new EventEmitter<void>();
  @Output() saveForm = new EventEmitter<Account>();

  readonly form = this.fb.group({
    name: ['' as string, [Validators.required, Validators.maxLength(50)]],
    type: ['' as string, [Validators.required, Validators.maxLength(50)]]
  });

  constructor(private readonly fb: FormBuilder) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (changes.account || changes.mode) {
      this.form.reset({
        name: this.account?.name ?? '',
        type: this.normalizeType(this.account?.type)
      });
    }
  }

  get title(): string {
    return this.mode === 'create' ? 'Nueva cuenta' : 'Editar cuenta';
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const raw = this.form.getRawValue();
    const payload: Account = {
      id: this.account?.id ?? 0,
      name: raw.name?.trim() || '',
      type: raw.type?.trim() || ''
    };
    this.saveForm.emit(payload);
  }

  cancel(): void {
    this.cancelForm.emit();
  }

  private normalizeType(type: string | null | undefined): string {
    const value = (type ?? '').trim().toLowerCase();
    if (value.includes('egreso') || value.includes('gasto')) return 'Egreso';
    return 'Ingreso';
  }
}
