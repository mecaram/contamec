import { Component, OnInit } from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { ClosureBalance } from './models/closure-balance.model';
import { Closure } from './models/closure.model';
import { ClosureSearch } from './models/closure-search.model';
import { ClosureBalanceService } from './services/closure-balance.service';
import { ClosureCloseResponse, ClosureExpenseDetailItem, ClosureIncomeDetailItem, ClosureService } from './services/closure.service';

type EditableClosureBalance = ClosureBalance & { amountInput: string };

@Component({
  selector: 'app-closures',
  templateUrl: './closures.component.html',
  styleUrls: ['./closures.component.scss']
})
export class ClosuresComponent implements OnInit {
  closures: Closure[] = [];
  loading = false;
  errorMessage: string | null = null;
  detailTarget: Closure | null = null;
  incomeDetailTarget: Closure | null = null;
  incomeDetailItems: ClosureIncomeDetailItem[] = [];
  incomeDetailTotal = 0;
  incomeDetailLoading = false;
  incomeDetailError: string | null = null;
  expenseDetailTarget: Closure | null = null;
  expenseDetailItems: ClosureExpenseDetailItem[] = [];
  expenseDetailTotal = 0;
  expenseDetailLoading = false;
  expenseDetailError: string | null = null;
  balanceValuesTarget: Closure | null = null;
  closureBalances: EditableClosureBalance[] = [];
  balanceValuesLoading = false;
  balanceValuesSaving = false;
  balanceValuesError: string | null = null;
  balanceValuesSuccess: string | null = null;
  applyAmountValue = '';
  closeTarget: Closure | null = null;
  closeConfirmTarget: Closure | null = null;
  closeLoading = false;
  closeError: string | null = null;

  readonly filterForm = this.fb.group({
    id: [null as number | null],
    isClosed: [''],
    openDateFrom: [''],
    openDateTo: ['']
  });

  constructor(
    private readonly fb: FormBuilder,
    private readonly closureService: ClosureService,
    private readonly closureBalanceService: ClosureBalanceService
  ) {}

  ngOnInit(): void {
    this.applyCurrentMonthDateRange();
    this.search();
  }

  search(): void {
    this.loading = true;
    this.errorMessage = null;
    const filters = this.buildFilters();

    this.closureService
      .search(filters)
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (items) => {
          this.closures = items ?? [];
        },
        error: (error) => {
          this.errorMessage = error?.error?.message ?? 'No se pudieron consultar los cierres.';
          this.closures = [];
        }
      });
  }

  clearFilters(): void {
    this.filterForm.reset({
      id: null,
      isClosed: '',
      openDateFrom: null,
      openDateTo: null
    });
    this.applyCurrentMonthDateRange();
    this.search();
  }

  openDetail(closure: Closure): void {
    this.detailTarget = closure;
  }

  closeDetail(): void {
    this.detailTarget = null;
  }

  openIncomeDetail(closure: Closure): void {
    this.incomeDetailTarget = closure;
    this.incomeDetailItems = [];
    this.incomeDetailTotal = 0;
    this.incomeDetailError = null;
    this.incomeDetailLoading = true;

    this.closureService
      .getIncomeDetail(closure.id)
      .pipe(finalize(() => (this.incomeDetailLoading = false)))
      .subscribe({
        next: (response) => {
          this.incomeDetailItems = response?.items ?? [];
          this.incomeDetailTotal = response?.totalIncomes ?? 0;
        },
        error: () => {
          this.incomeDetailError = 'No se pudo cargar el detalle de ingresos.';
          this.incomeDetailItems = [];
          this.incomeDetailTotal = 0;
        }
      });
  }

  closeIncomeDetail(): void {
    this.incomeDetailTarget = null;
    this.incomeDetailItems = [];
    this.incomeDetailTotal = 0;
    this.incomeDetailError = null;
    this.incomeDetailLoading = false;
  }

  openExpenseDetail(closure: Closure): void {
    this.expenseDetailTarget = closure;
    this.expenseDetailItems = [];
    this.expenseDetailTotal = 0;
    this.expenseDetailError = null;
    this.expenseDetailLoading = true;

    this.closureService
      .getExpenseDetail(closure.id)
      .pipe(finalize(() => (this.expenseDetailLoading = false)))
      .subscribe({
        next: (response) => {
          this.expenseDetailItems = response?.items ?? [];
          this.expenseDetailTotal = response?.totalExpenses ?? 0;
        },
        error: () => {
          this.expenseDetailError = 'No se pudo cargar el detalle de egresos.';
          this.expenseDetailItems = [];
          this.expenseDetailTotal = 0;
        }
      });
  }

  closeExpenseDetail(): void {
    this.expenseDetailTarget = null;
    this.expenseDetailItems = [];
    this.expenseDetailTotal = 0;
    this.expenseDetailError = null;
    this.expenseDetailLoading = false;
  }

  openBalanceValues(closure: Closure): void {
    this.balanceValuesTarget = closure;
    this.closureBalances = [];
    this.balanceValuesError = null;
    this.balanceValuesSuccess = null;
    this.balanceValuesLoading = true;
    this.balanceValuesSaving = false;
    this.applyAmountValue = '';

    this.closureBalanceService
      .getByClosureId(closure.id)
      .pipe(finalize(() => (this.balanceValuesLoading = false)))
      .subscribe({
        next: (items) => {
          this.closureBalances = (items ?? []).map((x) => ({
            id: x.id,
            closureId: x.closureId,
            paymentAccountId: x.paymentAccountId,
            paymentAccountName: x.paymentAccountName,
            amount: this.normalizeAmount(x.amount),
            amountInput: this.formatAmountInput(x.amount)
          }));
        },
        error: (error) => {
          this.balanceValuesError = error?.error?.message ?? 'No se pudieron cargar los valores en cuenta.';
          this.closureBalances = [];
        }
      });
  }

  closeBalanceValues(): void {
    this.balanceValuesTarget = null;
    this.closureBalances = [];
    this.balanceValuesLoading = false;
    this.balanceValuesSaving = false;
    this.balanceValuesError = null;
    this.balanceValuesSuccess = null;
    this.applyAmountValue = '';
  }

  openCloseModal(closure: Closure): void {
    this.closeTarget = closure;
    this.closeLoading = false;
    this.closeError = null;
  }

  closeCloseModal(): void {
    if (this.closeLoading) return;
    this.closeTarget = null;
    this.closeConfirmTarget = null;
    this.closeError = null;
  }

  requestCloseConfirmation(): void {
    if (!this.closeTarget || this.closeLoading || !this.canCloseCurrentTarget) return;
    this.closeConfirmTarget = this.closeTarget;
  }

  cancelCloseConfirmation(): void {
    if (this.closeLoading) return;
    this.closeCloseModal();
  }

  confirmCloseClosure(): void {
    if (!this.closeTarget || !this.closeConfirmTarget || this.closeLoading || !this.canCloseCurrentTarget) return;

    this.closeLoading = true;
    this.closeError = null;

    this.closureService
      .close(this.closeTarget.id)
      .pipe(finalize(() => (this.closeLoading = false)))
      .subscribe({
        next: (_: ClosureCloseResponse) => {
          this.closeConfirmTarget = null;
          this.closeCloseModal();
          this.search();
        },
        error: (error) => {
          this.closeError = error?.error?.message ?? 'No se pudo cerrar el cierre seleccionado.';
        }
      });
  }

  onBalanceAmountInput(index: number, rawValue: string): void {
    const row = this.closureBalances[index];
    if (!row) return;

    row.amountInput = rawValue;
    const parsed = this.parseAmount(rawValue);
    if (parsed !== null) {
      row.amount = parsed;
    }
    this.balanceValuesSuccess = null;
  }

  onBalanceAmountBlur(index: number): void {
    const row = this.closureBalances[index];
    if (!row) return;

    const parsed = this.parseAmount(row.amountInput);
    row.amount = parsed ?? 0;
    row.amountInput = this.formatAmountInput(row.amount);
  }

  onBalanceAmountFocus(event: FocusEvent): void {
    const input = event.target as HTMLInputElement | null;
    if (!input) return;
    setTimeout(() => input.select(), 0);
  }

  applySameAmountToAll(): void {
    const amount = this.parseAmount(this.applyAmountValue) ?? 0;
    this.closureBalances = this.closureBalances.map((row) => ({
      ...row,
      amount,
      amountInput: this.formatAmountInput(amount)
    }));
    this.applyAmountValue = this.formatAmountInput(amount);
    this.balanceValuesSuccess = null;
  }

  saveBalanceValues(): void {
    if (!this.balanceValuesTarget || this.balanceValuesSaving) return;

    if (this.closureBalances.length === 0) {
      this.balanceValuesError = 'No hay cuentas de pago para guardar.';
      return;
    }

    if (this.closureBalances.some((x) => this.normalizeAmount(x.amount) < 0)) {
      this.balanceValuesError = 'El monto no puede ser negativo.';
      return;
    }

    this.balanceValuesError = null;
    this.balanceValuesSuccess = null;
    this.balanceValuesSaving = true;

    const closureId = this.balanceValuesTarget.id;
    const payload = this.closureBalances.map((row) => ({
      id: row.id,
      closureId,
      paymentAccountId: row.paymentAccountId,
      paymentAccountName: row.paymentAccountName,
      amount: this.normalizeAmount(row.amount)
    }));

    this.closureBalanceService
      .saveBulk(closureId, payload)
      .pipe(finalize(() => (this.balanceValuesSaving = false)))
      .subscribe({
        next: (items) => {
          this.closureBalances = (items ?? []).map((x) => ({
            id: x.id,
            closureId: x.closureId,
            paymentAccountId: x.paymentAccountId,
            paymentAccountName: x.paymentAccountName,
            amount: this.normalizeAmount(x.amount),
            amountInput: this.formatAmountInput(x.amount)
          }));
          this.search();
          this.closeBalanceValues();
        },
        error: (error) => {
          this.balanceValuesError = error?.error?.message ?? 'No se pudieron guardar los valores en cuenta.';
        }
      });
  }

  formatAmountInput(value: number | null | undefined): string {
    const amount = this.normalizeAmount(value);
    return amount.toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  trackByPaymentAccount(_: number, row: ClosureBalance): number {
    return row.paymentAccountId;
  }

  get totalBalanceAccounts(): number {
    return this.closureBalances.length;
  }

  get totalBalanceAmount(): number {
    return this.closureBalances.reduce((acc, row) => acc + this.normalizeAmount(row.amount), 0);
  }

  get balanceValuesResult(): number {
    return this.normalizeAmount(this.balanceValuesTarget?.result);
  }

  get balanceValuesDifference(): number {
    return this.totalBalanceAmount - this.balanceValuesResult;
  }

  get closeDifference(): number {
    if (!this.closeTarget) return 0;
    return this.normalizeAmount(this.closeTarget.inAccount) - this.normalizeAmount(this.closeTarget.result);
  }

  get canCloseCurrentTarget(): boolean {
    if (!this.closeTarget || this.closeTarget.isClosed) return false;
    return Math.abs(this.closeDifference) <= 0.009;
  }

  formatAccountId(value: number | null | undefined): string {
    const numeric = value ?? 0;
    return String(numeric).padStart(4, '0');
  }

  formatCurrency(value: number | null | undefined): string {
    const amount = value ?? 0;
    return new Intl.NumberFormat('es-AR', {
      style: 'currency',
      currency: 'ARS',
      minimumFractionDigits: 2
    }).format(amount);
  }

  formatPercentage(value: number | null | undefined): string {
    const percentage = value ?? 0;
    return `${percentage.toFixed(2)} %`;
  }

  formatDate(value?: string | null): string {
    if (!value) return '-';
    const source = value.trim();
    const dateOnlyMatch = /^(\d{4})-(\d{2})-(\d{2})/.exec(source);
    if (dateOnlyMatch) {
      const [, yyyy, mm, dd] = dateOnlyMatch;
      return `${dd}/${mm}/${yyyy}`;
    }

    const date = new Date(source);
    if (Number.isNaN(date.getTime())) return '-';
    const dd = String(date.getDate()).padStart(2, '0');
    const mm = String(date.getMonth() + 1).padStart(2, '0');
    const yyyy = String(date.getFullYear());
    return `${dd}/${mm}/${yyyy}`;
  }

  formatStatus(isClosed?: boolean | null): string {
    return isClosed ? 'Cerrado' : 'Abierto';
  }

  private buildFilters(): ClosureSearch {
    const raw = this.filterForm.getRawValue();
    let isClosed: boolean | null = null;
    if (raw.isClosed === 'true') isClosed = true;
    else if (raw.isClosed === 'false') isClosed = false;

    return {
      id: raw.id,
      isClosed,
      openDateFrom: raw.openDateFrom || null,
      openDateTo: raw.openDateTo || null
    };
  }

  private applyCurrentMonthDateRange(): void {
    const { from, to } = this.getCurrentMonthRange();
    this.filterForm.patchValue({
      openDateFrom: from,
      openDateTo: to
    });
  }

  private getCurrentMonthRange(): { from: string; to: string } {
    const now = new Date();
    const year = now.getFullYear();
    const month = now.getMonth();

    const firstDay = new Date(year, 0, 1);
    const lastDay = new Date(year, month + 1, 0);

    return {
      from: this.toDateInputValue(firstDay),
      to: this.toDateInputValue(lastDay)
    };
  }

  private toDateInputValue(value: Date): string {
    const yyyy = String(value.getFullYear());
    const mm = String(value.getMonth() + 1).padStart(2, '0');
    const dd = String(value.getDate()).padStart(2, '0');
    return `${yyyy}-${mm}-${dd}`;
  }

  private normalizeAmount(value: number | null | undefined): number {
    if (value === null || value === undefined || Number.isNaN(value)) return 0;
    return value;
  }

  private parseAmount(value: string | number | null | undefined): number | null {
    if (value === null || value === undefined) return 0;
    if (typeof value === 'number') return Number.isFinite(value) ? Math.max(0, value) : 0;

    const raw = value.trim().replace(/\s/g, '').replace(/[^0-9,.-]/g, '');
    if (!raw) return 0;

    const lastDot = raw.lastIndexOf('.');
    const lastComma = raw.lastIndexOf(',');
    const hasDot = lastDot >= 0;
    const hasComma = lastComma >= 0;

    let normalized = raw;

    if (hasDot && hasComma) {
      const decimalSeparator = lastDot > lastComma ? '.' : ',';
      const thousandsSeparator = decimalSeparator === '.' ? ',' : '.';
      normalized = normalized.split(thousandsSeparator).join('');
      normalized = normalized.replace(decimalSeparator, '.');
    } else if (hasComma) {
      const commaCount = (normalized.match(/,/g) ?? []).length;
      if (commaCount > 1) {
        normalized = normalized.replace(/,/g, '');
      } else {
        normalized = normalized.replace(',', '.');
      }
    } else if (hasDot) {
      const dotCount = (normalized.match(/\./g) ?? []).length;
      if (dotCount > 1) {
        normalized = normalized.replace(/\./g, '');
      }
    }

    if ((normalized.match(/\./g) ?? []).length > 1) {
      return null;
    }

    const amount = Number(normalized);
    if (!Number.isFinite(amount)) return null;
    return amount < 0 ? 0 : amount;
  }
}
