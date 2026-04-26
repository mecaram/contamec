import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { Expense } from '../../models/expense.model';

@Component({
  selector: 'app-expense-form',
  templateUrl: './expense-form.component.html',
  styleUrls: ['./expense-form.component.scss']
})
export class ExpenseFormComponent implements OnChanges {
  @Input() mode: 'create' | 'edit' = 'create';
  @Input() expense: Expense | null = null;
  @Input() accountOptions: Array<{ id: number; label: string }> = [];
  @Output() cancelForm = new EventEmitter<void>();
  @Output() saveForm = new EventEmitter<Expense>();
  amountDisplay = '';

  readonly form = this.fb.group({
    emissionDate: [null as Date | null, [Validators.required]],
    accountId: [null as number | null, [Validators.required, Validators.min(1)]],
    detail: ['' as string, [Validators.maxLength(80)]],
    amount: [null as number | null, [Validators.required, Validators.min(0.01)]]
  });

  constructor(private readonly fb: FormBuilder) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (changes.expense || changes.mode) {
      const defaultEmissionDate = this.mode === 'create' && !this.expense ? this.startOfDay(new Date()) : null;
      this.form.reset({
        emissionDate: this.expense?.emissionDate ? this.toDate(this.expense.emissionDate) : defaultEmissionDate,
        accountId: this.expense?.accountId ?? null,
        detail: this.expense?.detail ?? '',
        amount: this.expense?.amount ?? null
      });
      this.amountDisplay = this.formatAmount(this.form.controls.amount.value);
    }
  }

  get title(): string {
    return this.mode === 'create' ? 'Nuevo egreso' : 'Editar egreso';
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    const payload: Expense = {
      id: this.expense?.id ?? 0,
      emissionDate: this.toIsoDate(raw.emissionDate as Date),
      accountId: raw.accountId,
      detail: raw.detail?.trim() || '',
      amount: raw.amount
    };

    this.saveForm.emit(payload);
  }

  cancel(): void {
    this.cancelForm.emit();
  }

  onAmountInput(value: string): void {
    const parsed = this.parseAmount(value);
    this.form.controls.amount.setValue(parsed);
    this.amountDisplay = value;
  }

  onAmountBlur(): void {
    this.amountDisplay = this.formatAmount(this.form.controls.amount.value);
  }

  toIsoDate(date: Date): string {
    const local = new Date(date);
    local.setHours(0, 0, 0, 0);
    const year = local.getFullYear();
    const month = String(local.getMonth() + 1).padStart(2, '0');
    const day = String(local.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  toDate(value: string): Date {
    return new Date(value);
  }

  private startOfDay(date: Date): Date {
    const local = new Date(date);
    local.setHours(0, 0, 0, 0);
    return local;
  }

  private parseAmount(value: string): number | null {
    const raw = (value ?? '').replace(/\s/g, '').replace(/[^0-9,.-]/g, '');
    if (!raw) return null;

    const lastDot = raw.lastIndexOf('.');
    const lastComma = raw.lastIndexOf(',');
    const decimalIndex = Math.max(lastDot, lastComma);

    let normalized: string;
    if (decimalIndex >= 0) {
      const integerPart = raw.slice(0, decimalIndex).replace(/[.,]/g, '');
      const decimalPart = raw.slice(decimalIndex + 1).replace(/[.,]/g, '');
      normalized = `${integerPart}.${decimalPart}`;
    } else {
      normalized = raw.replace(/[.,]/g, '');
    }

    if (!normalized) return null;

    const amount = Number(normalized);
    return Number.isFinite(amount) ? amount : null;
  }

  private formatAmount(value: number | null): string {
    if (value === null || value === undefined || Number.isNaN(value)) return '';
    return new Intl.NumberFormat('es-AR', {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2
    }).format(value);
  }
}
