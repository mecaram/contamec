import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { PaymentAccount } from '../../models/payment-account.model';

@Component({
  selector: 'app-payment-account-form',
  templateUrl: './payment-account-form.component.html',
  styleUrls: ['./payment-account-form.component.scss']
})
export class PaymentAccountFormComponent implements OnChanges {
  @Input() mode: 'create' | 'edit' = 'create';
  @Input() paymentAccount: PaymentAccount | null = null;
  @Output() cancelForm = new EventEmitter<void>();
  @Output() saveForm = new EventEmitter<PaymentAccount>();

  readonly form = this.fb.group({
    name: ['' as string, [Validators.required, Validators.maxLength(50)]]
  });

  constructor(private readonly fb: FormBuilder) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (changes.paymentAccount || changes.mode) {
      this.form.reset({
        name: this.paymentAccount?.name ?? ''
      });
    }
  }

  get title(): string {
    return this.mode === 'create' ? 'Nueva cuenta de pago' : 'Editar cuenta de pago';
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const raw = this.form.getRawValue();
    const payload: PaymentAccount = {
      id: this.paymentAccount?.id ?? 0,
      name: raw.name?.trim() || ''
    };
    this.saveForm.emit(payload);
  }

  cancel(): void {
    this.cancelForm.emit();
  }
}
