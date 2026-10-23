import { Component, OnInit } from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { PaymentAccount } from './models/payment-account.model';
import { PaymentAccountSearch } from './models/payment-account-search.model';
import { PaymentAccountService } from './services/payment-account.service';

@Component({
  selector: 'app-payment-accounts',
  templateUrl: './payment-accounts.component.html',
  styleUrls: ['./payment-accounts.component.scss']
})
export class PaymentAccountsComponent implements OnInit {
  paymentAccounts: PaymentAccount[] = [];
  loading = false;
  errorMessage: string | null = null;
  formVisible = false;
  formMode: 'create' | 'edit' = 'create';
  editingPaymentAccount: PaymentAccount | null = null;
  notification: { message: string; type: 'success' | 'error' } | null = null;
  deleteTarget: PaymentAccount | null = null;
  /** Exportación del listado en curso. */
  exporting: 'pdf' | 'excel' | null = null;
  sortResetToken = 0;
  private notificationTimer: ReturnType<typeof setTimeout> | null = null;

  readonly filterForm = this.fb.group({
    id: [null as number | null],
    name: ['']
  });

  constructor(
    private readonly fb: FormBuilder,
    private readonly paymentAccountsService: PaymentAccountService
  ) {}

  ngOnInit(): void {
    this.search();
  }

  search(): void {
    this.loading = true;
    this.errorMessage = null;
    const filters = this.buildFilters();
    this.paymentAccountsService
      .search(filters)
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (items) => {
          this.paymentAccounts = items ?? [];
        },
        error: () => {
          this.errorMessage = 'No se pudieron consultar las cuentas de pago.';
          this.paymentAccounts = [];
        }
      });
  }

  clearFilters(): void {
    this.filterForm.reset({
      id: null,
      name: ''
    });
    this.sortResetToken += 1;
    this.search();
  }

  openCreate(): void {
    this.formMode = 'create';
    this.editingPaymentAccount = null;
    this.formVisible = true;
  }

  openEdit(account: PaymentAccount): void {
    this.formMode = 'edit';
    this.editingPaymentAccount = account;
    this.formVisible = true;
  }

  cancelForm(): void {
    this.formVisible = false;
    this.editingPaymentAccount = null;
  }

  submitForm(payload: PaymentAccount): void {
    if (this.formMode === 'create') {
      this.paymentAccountsService.create(payload).subscribe({
        next: () => {
          this.notify('Cuenta de pago creada correctamente.');
          this.formVisible = false;
          this.search();
        },
        error: () => this.notify('No se pudo crear la cuenta de pago.', 'error')
      });
      return;
    }

    const id = this.editingPaymentAccount?.id;
    if (!id) {
      this.notify('No se pudo identificar la cuenta de pago a editar.', 'error');
      return;
    }

    this.paymentAccountsService.update(id, payload).subscribe({
      next: () => {
        this.notify('Cuenta de pago actualizada correctamente.');
        this.formVisible = false;
        this.editingPaymentAccount = null;
        this.search();
      },
      error: () => this.notify('No se pudo actualizar la cuenta de pago.', 'error')
    });
  }

  deletePaymentAccount(account: PaymentAccount): void {
    this.deleteTarget = account;
  }

  confirmDelete(): void {
    if (!this.deleteTarget) return;
    const target = this.deleteTarget;
    this.paymentAccountsService.delete(target.id).subscribe({
      next: () => {
        this.deleteTarget = null;
        this.notify('Cuenta de pago eliminada correctamente.');
        this.search();
      },
      error: () => {
        this.deleteTarget = null;
        this.notify('No se puede eliminar la cuenta de pago porque tiene movimientos asociados.', 'error');
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

  /** Exporta el listado con los filtros actuales. */
  exportList(format: 'pdf' | 'excel'): void {
    if (this.exporting) return;

    this.exporting = format;
    this.paymentAccountsService
      .exportList(this.buildFilters(), format)
      .pipe(finalize(() => (this.exporting = null)))
      .subscribe({
        next: (blob) => this.saveFile(blob, `Listado_CuentasDePago_${this.fileTimestamp()}.${format === 'pdf' ? 'pdf' : 'xlsx'}`),
        error: () => this.notify(`No fue posible exportar el listado a ${format === 'pdf' ? 'PDF' : 'Excel'}. Intente nuevamente.`, 'error')
      });
  }

  private fileTimestamp(): string {
    const now = new Date();
    const pad = (value: number) => String(value).padStart(2, '0');
    return `${now.getFullYear()}${pad(now.getMonth() + 1)}${pad(now.getDate())}_${pad(now.getHours())}${pad(now.getMinutes())}`;
  }

  private saveFile(blob: Blob, fileName: string): void {
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    link.remove();
    // Se libera cuando el navegador ya tomó el archivo.
    setTimeout(() => URL.revokeObjectURL(url), 1000);
  }

  private buildFilters(): PaymentAccountSearch {
    const raw = this.filterForm.getRawValue();
    return {
      id: raw.id,
      name: raw.name?.trim() || null
    };
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
}
