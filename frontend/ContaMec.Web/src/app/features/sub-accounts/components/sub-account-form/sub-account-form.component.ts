import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { Account } from '../../../accounts/models/account.model';
import { SubAccount } from '../../models/sub-account.model';

@Component({
  selector: 'app-sub-account-form',
  templateUrl: './sub-account-form.component.html',
  styleUrls: ['./sub-account-form.component.scss']
})
export class SubAccountFormComponent implements OnChanges {
  @Input() mode: 'create' | 'edit' = 'create';
  @Input() subAccount: SubAccount | null = null;
  /** Cuentas activas que requieren subcuentas, ordenadas por nombre. */
  @Input() accountOptions: Account[] = [];
  @Output() cancelForm = new EventEmitter<void>();
  @Output() saveForm = new EventEmitter<SubAccount>();

  readonly form = this.fb.group({
    name: ['' as string, [Validators.required, Validators.maxLength(50)]],
    accountId: [null as number | null, [Validators.required]],
    behaviorType: ['' as string, [Validators.required]],
    active: [true]
  });

  constructor(private readonly fb: FormBuilder) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (changes.subAccount || changes.mode) {
      this.form.reset({
        name: this.subAccount?.name ?? '',
        accountId: this.subAccount?.accountId ?? null,
        // Alta: Fijo por defecto. Las subcuentas anteriores sin dato quedan sin seleccionar y hay que elegir uno.
        behaviorType: this.subAccount ? this.subAccount.behaviorType ?? '' : 'Fijo',
        // Sin dato cargado (subcuentas anteriores o alta nueva) la subcuenta se considera activa.
        active: this.subAccount?.active ?? true
      });
    }
  }

  get title(): string {
    return this.mode === 'create' ? 'Nueva subcuenta' : 'Editar subcuenta';
  }

  /** Al editar, la cuenta actual se ofrece aunque ya no esté en la lista, para no perder la selección. */
  get formAccountOptions(): Account[] {
    const currentId = this.subAccount?.accountId;
    if (!currentId || this.accountOptions.some((a) => a.id === currentId)) return this.accountOptions;
    return [{ id: currentId, name: this.subAccount?.accountName }, ...this.accountOptions];
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const raw = this.form.getRawValue();
    const payload: SubAccount = {
      id: this.subAccount?.id ?? 0,
      name: raw.name?.trim() || '',
      accountId: raw.accountId,
      behaviorType: raw.behaviorType || null,
      active: !!raw.active
    };
    this.saveForm.emit(payload);
  }

  cancel(): void {
    this.cancelForm.emit();
  }
}
