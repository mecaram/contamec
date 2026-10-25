import { Component, EventEmitter, Input, OnChanges, OnDestroy, OnInit, Output, SimpleChanges } from '@angular/core';
import { AbstractControl, FormBuilder, ValidationErrors, Validators } from '@angular/forms';
import { Subscription } from 'rxjs';
import { isPhone } from '../../../../shared/amount-calculator/device';
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
  /** Solo en el alta: datos que se proponen copiados de otro ingreso (salvo la fecha, que es la de hoy). */
  @Input() copyFrom: Income | null = null;
  @Input() accountOptions: Array<{ id: number; label: string; subAccountRequires?: boolean }> = [];
  @Input() paymentAccountOptions: Array<{ id: number; label: string }> = [];
  /** En el alta se propone esta cuenta de pago (la del último ingreso registrado), si sigue disponible. */
  @Input() defaultPaymentAccountId: number | null = null;
  /** Apertura del cierre abierto: la fecha de emisión va desde ese día hasta hoy. */
  @Input() minEmissionDate: string | null = null;
  /** Guardado en curso: Guardar y Cancelar quedan desactivados hasta que responde el backend. */
  @Input() saving = false;
  @Output() cancelForm = new EventEmitter<void>();
  @Output() saveForm = new EventEmitter<Income>();
  amountDisplay = '';
  /** En el celular queda la caja de texto: la calculadora es para PC, tablet y notebook. */
  readonly useCalculator = !isPhone();
  subAccountOptions: Array<{ id: number; label: string }> = [];
  subAccountsLoading = false;
  private subAccountsRequest: Subscription | null = null;
  private accountChanges: Subscription | null = null;

  readonly form = this.fb.group({
    emissionDate: [null as Date | null, [Validators.required, (control: AbstractControl) => this.emissionDateRangeValidator(control)]],
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
    if (changes.income || changes.mode || changes.copyFrom) {
      const defaultEmissionDate = this.mode === 'create' && !this.income ? this.startOfDay(new Date()) : null;
      // Al copiar, los datos salen del ingreso de origen; la fecha de emisión es la de hoy.
      const source = this.income ?? this.copySource;
      this.form.reset({
        emissionDate: this.income?.emissionDate ? this.toDate(this.income.emissionDate) : defaultEmissionDate,
        accountId: source?.accountId ?? null,
        detail: source?.detail ?? '',
        amount: source?.amount ?? null,
        // Los ingresos anteriores pueden no tener cuenta de pago: el combo queda vacío y hay que elegir una.
        paymentAccountId: source?.paymentAccountId ?? null,
        subAccountId: source?.subAccountId ?? null,
        // Alta: Ordinaria por defecto. Los ingresos anteriores sin dato quedan sin seleccionar y hay que elegir una.
        occurrenceType: source ? source.occurrenceType ?? '' : 'Ordinaria'
      }, { emitEvent: false });
      this.amountDisplay = this.formatAmount(this.form.controls.amount.value);
      this.discardInactiveCopiedOptions();
      this.refreshSubAccounts();
    } else if (changes.accountOptions || changes.paymentAccountOptions) {
      // Las cuentas llegan después de abrir el formulario: recién ahí se sabe si requiere subcuentas.
      this.discardInactiveCopiedOptions();
      if (changes.accountOptions) this.refreshSubAccounts();
    }
    if (changes.minEmissionDate) {
      this.form.controls.emissionDate.updateValueAndValidity();
    }
    if (changes.income || changes.mode || changes.defaultPaymentAccountId || changes.paymentAccountOptions) {
      this.applyDefaultPaymentAccount();
    }
  }

  private get copySource(): Income | null {
    return this.mode === 'create' && !this.income ? this.copyFrom : null;
  }

  /** Al copiar, una cuenta o cuenta de pago que ya no está activa queda vacía para elegir otra. */
  private discardInactiveCopiedOptions(): void {
    if (!this.copySource) return;
    const account = this.form.controls.accountId;
    if (account.value !== null && this.accountOptions.length > 0 && !this.accountOptions.some((a) => a.id === account.value)) {
      account.setValue(null, { emitEvent: false });
    }
    const payment = this.form.controls.paymentAccountId;
    if (payment.value !== null && this.paymentAccountOptions.length > 0 && !this.paymentAccountOptions.some((p) => p.id === payment.value)) {
      payment.setValue(null, { emitEvent: false });
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
    // Con las cuentas todavía sin cargar no se sabe si requiere subcuenta: se conserva la del ingreso hasta que lleguen.
    if (accountId && this.accountOptions.length === 0) return;
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
        // Al copiar, una subcuenta que ya no está activa queda vacía para elegir otra.
        if (subAccount.value !== null && !this.subAccountOptions.some((s) => s.id === subAccount.value)) {
          subAccount.setValue(null, { emitEvent: false });
        }
      },
      error: () => {
        this.subAccountsLoading = false;
        this.subAccountOptions = [];
      }
    });
  }

  /** Fechas en formato yyyy-MM-dd para los límites del calendario y los mensajes. */
  get minEmissionIsoDate(): string | null {
    return this.minEmissionDate ? this.minEmissionDate.substring(0, 10) : null;
  }

  get todayIsoDate(): string {
    return this.toIsoDate(new Date());
  }

  formatIsoDate(value: string | null): string {
    if (!value) return '';
    const [year, month, day] = value.split('-');
    return `${day}/${month}/${year}`;
  }

  private emissionDateRangeValidator(control: AbstractControl): ValidationErrors | null {
    if (!control.value) return null;
    const date = this.toIsoDate(control.value as Date);
    const min = this.minEmissionIsoDate;
    return (min && date < min) || date > this.todayIsoDate ? { range: true } : null;
  }

  get title(): string {
    if (this.mode === 'edit') return 'Editar ingreso';
    return this.copySource ? `Nuevo ingreso (copia del #${this.copySource.id})` : 'Nuevo ingreso';
  }

  save(): void {
    // Enter también envía el formulario: se ignora mientras se guarda.
    if (this.saving) return;
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

  onCalculatorAmount(amount: number | null): void {
    this.form.controls.amount.setValue(amount);
    this.form.controls.amount.markAsTouched();
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
    // "yyyy-MM-dd" se interpreta en UTC: se arma en hora local para no correrse un día.
    const dateOnly = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value);
    return dateOnly ? new Date(+dateOnly[1], +dateOnly[2] - 1, +dateOnly[3]) : new Date(value);
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
