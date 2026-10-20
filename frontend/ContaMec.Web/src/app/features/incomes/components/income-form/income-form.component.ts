import { Component, EventEmitter, Input, OnChanges, OnDestroy, OnInit, Output, SimpleChanges } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { Subscription } from 'rxjs';
import { SubAccountService } from '../../../sub-accounts/services/sub-account.service';
import { Income } from '../../models/income.model';

@Component({
  selector: 'app-income-form',
  templateUrl: './income-form.component.html',
  styleUrls: ['./income-form.component.scss']
})
export class IncomeFormComponent implements OnInit, OnChanges, OnDestroy {
  @Input() mode: 'create' | 'edit' = 'create';
  @Input() income: Income | null = null;
  @Input() accountOptions: Array<{ id: number; label: string; subAccountRequires?: boolean }> = [];
  @Input() paymentAccountOptions: Array<{ id: number; label: string }> = [];
  /** En el alta se propone esta cuenta de pago (la del último ingreso registrado), si sigue disponible. */
  @Input() defaultPaymentAccountId: number | null = null;
  @Output() cancelForm = new EventEmitter<void>();
  @Output() saveForm = new EventEmitter<Income>();
  amountDisplay = '';
  subAccountOptions: Array<{ id: number; label: string }> = [];
  subAccountsLoading = false;
  private subAccountsRequest: Subscription | null = null;
  private accountChanges: Subscription | null = null;

  readonly form = this.fb.group({
    emissionDate: [null as Date | null, [Validators.required]],
    accountId: [null as number | null, [Validators.required, Validators.min(1)]],
    detail: ['' as string, [Validators.maxLength(80)]],
    amount: [null as number | null, [Validators.required, Validators.min(0.01)]],
    paymentAccountId: [null as number | null, [Validators.required]],
    subAccountId: [null as number | null],
    occurrenceType: ['' as string, [Validators.required]]
  });

  constructor(
    private readonly fb: FormBuilder,
    private readonly subAccountService: SubAccountService
  ) {}

  ngOnInit(): void {
    // Al cambiar la cuenta se descarta la subcuenta elegida y se cargan las de la nueva cuenta.
    this.accountChanges = this.form.controls.accountId.valueChanges.subscribe(() => {
      this.form.controls.subAccountId.setValue(null);
      this.refreshSubAccounts();
    });
  }

  ngOnDestroy(): void {
    this.accountChanges?.unsubscribe();
    this.subAccountsRequest?.unsubscribe();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes.income || changes.mode) {
      const defaultEmissionDate = this.mode === 'create' && !this.income ? this.startOfDay(new Date()) : null;
      this.form.reset({
        emissionDate: this.income?.emissionDate ? this.toDate(this.income.emissionDate) : defaultEmissionDate,
        accountId: this.income?.accountId ?? null,
        detail: this.income?.detail ?? '',
        amount: this.income?.amount ?? null,
        // Los ingresos anteriores pueden no tener cuenta de pago: el combo queda vacío y hay que elegir una.
        paymentAccountId: this.income?.paymentAccountId ?? null,
        subAccountId: this.income?.subAccountId ?? null,
        // Alta: Ordinaria por defecto. Los ingresos anteriores sin dato quedan sin seleccionar y hay que elegir una.
        occurrenceType: this.income ? this.income.occurrenceType ?? '' : 'Ordinaria'
      }, { emitEvent: false });
      this.amountDisplay = this.formatAmount(this.form.controls.amount.value);
      this.refreshSubAccounts();
    } else if (changes.accountOptions) {
      // Las cuentas llegan después de abrir el formulario: recién ahí se sabe si requiere subcuentas.
      this.refreshSubAccounts();
    }
    if (changes.income || changes.mode || changes.defaultPaymentAccountId || changes.paymentAccountOptions) {
      this.applyDefaultPaymentAccount();
    }
  }

  /** Solo en el alta y si todavía no se eligió una: la cuenta propuesta debe estar entre las opciones (activas). */
  private applyDefaultPaymentAccount(): void {
    const control = this.form.controls.paymentAccountId;
    if (this.mode !== 'create' || this.income || control.value !== null || control.dirty) return;
    const id = this.defaultPaymentAccountId;
    if (id !== null && this.paymentAccountOptions.some((p) => p.id === id)) {
      control.setValue(id);
    }
  }

  get subAccountPlaceholder(): string {
    if (!this.form.controls.accountId.value) return 'Seleccione primero una cuenta';
    if (!this.selectedAccountRequiresSubAccounts) return 'La cuenta no requiere subcuenta';
    if (this.subAccountsLoading) return 'Cargando subcuentas...';
    if (this.subAccountOptions.length === 0) return 'La cuenta no tiene subcuentas activas';
    return 'Seleccione una subcuenta';
  }

  private get selectedAccountRequiresSubAccounts(): boolean {
    const accountId = this.form.controls.accountId.value;
    return !!this.accountOptions.find((a) => a.id === accountId)?.subAccountRequires;
  }

  /** Habilita la subcuenta (obligatoria) solo si la cuenta elegida requiere subcuentas, y carga las activas de esa cuenta. */
  private refreshSubAccounts(): void {
    this.subAccountsRequest?.unsubscribe();
    this.subAccountsRequest = null;
    this.subAccountsLoading = false;
    this.subAccountOptions = [];

    const subAccount = this.form.controls.subAccountId;
    const accountId = this.form.controls.accountId.value;
    if (!accountId || !this.selectedAccountRequiresSubAccounts) {
      subAccount.clearValidators();
      subAccount.setValue(null, { emitEvent: false });
      subAccount.disable({ emitEvent: false });
      return;
    }

    subAccount.setValidators([Validators.required]);
    subAccount.enable({ emitEvent: false });
    this.subAccountsLoading = true;
    this.subAccountsRequest = this.subAccountService.search({ accountId }).subscribe({
      next: (items) => {
        this.subAccountsLoading = false;
        // Al editar se conserva la subcuenta del ingreso aunque se haya desactivado.
        const currentId = this.income?.accountId === accountId ? this.income?.subAccountId : null;
        this.subAccountOptions = (items ?? [])
          .filter((s) => (s.active ?? true) || s.id === currentId)
          .map((s) => ({ id: s.id, label: s.name?.trim() || `#${s.id}` }))
          .sort((a, b) => a.label.localeCompare(b.label, 'es', { sensitivity: 'base' }));
      },
      error: () => {
        this.subAccountsLoading = false;
        this.subAccountOptions = [];
      }
    });
  }

  get title(): string {
    return this.mode === 'create' ? 'Nuevo ingreso' : 'Editar ingreso';
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    const payload: Income = {
      id: this.income?.id ?? 0,
      emissionDate: this.toIsoDate(raw.emissionDate as Date),
      accountId: raw.accountId,
      detail: raw.detail?.trim() || '',
      amount: raw.amount,
      paymentAccountId: raw.paymentAccountId,
      subAccountId: raw.subAccountId,
      occurrenceType: raw.occurrenceType || null
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
