import { Component, OnInit } from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { Income } from './models/income.model';
import { IncomeSearch } from './models/income-search.model';
import { ClosureLookup, IncomeAccountOption, IncomeService } from './services/income.service';

@Component({
  selector: 'app-incomes',
  templateUrl: './incomes.component.html',
  styleUrls: ['./incomes.component.scss']
})
export class IncomesComponent implements OnInit {
  incomes: Income[] = [];
  closureOptions: ClosureLookup[] = [];
  accountOptions: Array<{ id: number; label: string }> = [];
  loading = false;
  errorMessage: string | null = null;
  formVisible = false;
  formMode: 'create' | 'edit' = 'create';
  editingIncome: Income | null = null;
  notification: { message: string; type: 'success' | 'error' } | null = null;
  deleteTarget: Income | null = null;
  sortResetToken = 0;
  private notificationTimer: ReturnType<typeof setTimeout> | null = null;
  private readonly today = this.startOfDay(new Date());
  dateFromInput = this.toDisplayDate(this.today);
  dateToInput = this.toDisplayDate(this.today);

  get closedClosureIds(): number[] {
    return this.closureOptions
      .filter((closure) => this.isClosedClosure(closure))
      .map((closure) => closure.id);
  }

  readonly filterForm = this.fb.group({
    dateFrom: [this.today as Date | null],
    dateTo: [this.today as Date | null],
    closureId: [null as number | null],
    accountId: [null as number | null],
    amountFrom: [null as number | null],
    amountTo: [null as number | null],
    detail: ['']
  });

  constructor(
    private readonly fb: FormBuilder,
    private readonly incomesService: IncomeService
  ) {}

  ngOnInit(): void {
    this.loadClosures();
    this.loadIncomeAccounts();
  }

  search(): void {
    this.loading = true;
    this.errorMessage = null;

    const filters = this.buildFilters();
    this.incomesService
      .search(filters)
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (items) => {
          this.incomes = items ?? [];
          this.hydrateFilterOptions(this.incomes);
        },
        error: () => {
          this.errorMessage = 'No se pudieron consultar los ingresos.';
          this.incomes = [];
        }
      });
  }

  clearFilters(): void {
    const pendingClosure = this.closureOptions.find((c) => this.isOpenClosure(c));
    this.filterForm.reset({
      dateFrom: this.today,
      dateTo: this.today,
      closureId: pendingClosure ? pendingClosure.id : null,
      accountId: null,
      amountFrom: null,
      amountTo: null,
      detail: ''
    });
    this.dateFromInput = this.toDisplayDate(this.today);
    this.dateToInput = this.toDisplayDate(this.today);
    this.sortResetToken += 1;
    this.search();
  }

  onDateFromChange(value: string): void {
    this.dateFromInput = value;
    const parsed = this.parseInputDate(value);
    if (!parsed) return;
    this.filterForm.controls.dateFrom.setValue(parsed);
    this.search();
  }

  onDateToChange(value: string): void {
    this.dateToInput = value;
    const parsed = this.parseInputDate(value);
    if (!parsed) return;
    this.filterForm.controls.dateTo.setValue(parsed);
    this.search();
  }

  selectDateSegment(event: Event): void {
    const input = event.target as HTMLInputElement | null;
    if (!input) return;

    const value = input.value ?? '';
    if (value.length < 10) return;

    const caret = input.selectionStart ?? 0;
    let start = 0;
    let end = 2;

    if (caret >= 3 && caret <= 5) {
      start = 3;
      end = 5;
    } else if (caret >= 6) {
      start = 6;
      end = 10;
    }

    input.setSelectionRange(start, end);
  }

  normalizeDateInputs(): void {
    const dateFrom = this.filterForm.controls.dateFrom.value;
    const dateTo = this.filterForm.controls.dateTo.value;
    this.dateFromInput = dateFrom ? this.toDisplayDate(dateFrom) : '';
    this.dateToInput = dateTo ? this.toDisplayDate(dateTo) : '';
  }

  openCreate(): void {
    this.formMode = 'create';
    this.editingIncome = null;
    this.formVisible = true;
  }

  openEdit(income: Income): void {
    this.formMode = 'edit';
    this.editingIncome = income;
    this.formVisible = true;
  }

  cancelForm(): void {
    this.formVisible = false;
    this.editingIncome = null;
  }

  submitForm(payload: Income): void {
    if (this.formMode === 'create') {
      this.incomesService.create(payload).subscribe({
        next: () => {
          this.notify('Ingreso creado correctamente.');
          this.formVisible = false;
          this.search();
        },
        error: () => this.notify('No se pudo crear el ingreso.')
      });
      return;
    }

    const id = this.editingIncome?.id;
    if (!id) {
      this.notify('No se pudo identificar el ingreso a editar.');
      return;
    }

    this.incomesService.update(id, payload).subscribe({
      next: () => {
        this.notify('Ingreso actualizado correctamente.');
        this.formVisible = false;
        this.editingIncome = null;
        this.search();
      },
      error: () => this.notify('Solo se puede modificar un ingreso con cierre abierto.', 'error')
    });
  }

  deleteIncome(income: Income): void {
    this.deleteTarget = income;
  }

  confirmDelete(): void {
    if (!this.deleteTarget) return;
    const target = this.deleteTarget;

    this.incomesService.delete(target.id).subscribe({
      next: () => {
        this.deleteTarget = null;
        this.notify('Ingreso eliminado correctamente.');
        this.search();
      },
      error: () => {
        this.deleteTarget = null;
        this.notify('Solo se puede eliminar un ingreso con cierre abierto.', 'error');
      }
    });
  }

  cancelDelete(): void {
    this.deleteTarget = null;
  }

  closeNotification(): void {
    if (this.notificationTimer) {
      clearTimeout(this.notificationTimer);
      this.notificationTimer = null;
    }
    this.notification = null;
  }

  private buildFilters(): IncomeSearch {
    const raw = this.filterForm.getRawValue();
    return {
      dateFrom: raw.dateFrom ? this.toIsoDate(raw.dateFrom) : null,
      dateTo: raw.dateTo ? this.toIsoDate(raw.dateTo) : null,
      closureId: raw.closureId,
      accountId: raw.accountId,
      amountFrom: raw.amountFrom,
      amountTo: raw.amountTo,
      detail: raw.detail?.trim() || null
    };
  }

  private hydrateFilterOptions(items: Income[]): void {
    // No sobrescribir cuentas cargadas desde /api/accounts/income-options.
    if (this.accountOptions.length > 0) return;

    const accountMap = new Map<number, string>();
    for (const item of items) {
      if (item.accountId !== null && item.accountId !== undefined) {
        accountMap.set(item.accountId, item.accountName?.trim() || `#${item.accountId}`);
      }
    }

    this.accountOptions = Array.from(accountMap.entries())
      .map(([id, label]) => ({ id, label }))
      .sort((a, b) => a.label.localeCompare(b.label, 'es', { sensitivity: 'base' }));
  }

  private loadClosures(): void {
    this.incomesService.getClosures().subscribe({
      next: (closures) => {
        const ordered = (closures ?? []).slice().sort((a, b) => b.id - a.id);
        this.closureOptions = ordered;

        const pending = ordered.find((c) => this.isOpenClosure(c));
        if (pending) {
          this.filterForm.patchValue({ closureId: pending.id }, { emitEvent: false });
        }

        this.search();
      },
      error: () => {
        this.closureOptions = [];
        this.search();
      }
    });
  }

  private loadIncomeAccounts(): void {
    this.incomesService.getIncomeAccounts().subscribe({
      next: (accounts: IncomeAccountOption[]) => {
        this.accountOptions = (accounts ?? [])
          .map((a) => ({
            id: a.id,
            label: a.name?.trim() || `#${a.id}`
          }))
          .sort((a, b) => a.label.localeCompare(b.label, 'es', { sensitivity: 'base' }));
      },
      error: () => {
        this.accountOptions = [];
      }
    });
  }

  toIsoDate(value: Date): string {
    const local = new Date(value);
    local.setHours(0, 0, 0, 0);
    const year = local.getFullYear();
    const month = String(local.getMonth() + 1).padStart(2, '0');
    const day = String(local.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  toDisplayDate(value: Date): string {
    const local = new Date(value);
    local.setHours(0, 0, 0, 0);
    const day = String(local.getDate()).padStart(2, '0');
    const month = String(local.getMonth() + 1).padStart(2, '0');
    const year = local.getFullYear();
    return `${day}-${month}-${year}`;
  }

  private startOfDay(date: Date): Date {
    const local = new Date(date);
    local.setHours(0, 0, 0, 0);
    return local;
  }

  private notify(message: string, type: 'success' | 'error' = 'success'): void {
    if (this.notificationTimer) {
      clearTimeout(this.notificationTimer);
      this.notificationTimer = null;
    }

    this.notification = { message, type };
    this.notificationTimer = setTimeout(() => {
      this.notification = null;
      this.notificationTimer = null;
    }, 4000);
  }

  private isOpenClosure(closure: ClosureLookup): boolean {
    const value = closure.isClosed as unknown;
    return value === 0 || value === false || value === '0';
  }

  private isClosedClosure(closure: ClosureLookup): boolean {
    const value = closure.isClosed as unknown;
    return value === 1 || value === true || value === '1';
  }

  private parseInputDate(value: string): Date | null {
    const raw = (value ?? '').trim();
    if (!raw) return null;

    let day = 0;
    let month = 0;
    let year = 0;

    const dash = raw.match(/^(\d{2})-(\d{2})-(\d{4})$/);
    const iso = raw.match(/^(\d{4})-(\d{2})-(\d{2})$/);

    if (dash) {
      day = Number(dash[1]);
      month = Number(dash[2]);
      year = Number(dash[3]);
    } else if (iso) {
      year = Number(iso[1]);
      month = Number(iso[2]);
      day = Number(iso[3]);
    } else {
      return null;
    }

    if (month < 1 || month > 12 || day < 1 || day > 31) return null;
    const date = new Date(year, month - 1, day);
    if (date.getFullYear() !== year || date.getMonth() !== month - 1 || date.getDate() !== day) return null;
    date.setHours(0, 0, 0, 0);
    return date;
  }
}
