import { Component, HostListener, OnInit } from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { SubAccount } from '../sub-accounts/models/sub-account.model';
import { SubAccountService } from '../sub-accounts/services/sub-account.service';
import { Account } from './models/account.model';
import { AccountSearch } from './models/account-search.model';
import { AccountService } from './services/account.service';

@Component({
  selector: 'app-accounts',
  templateUrl: './accounts.component.html',
  styleUrls: ['./accounts.component.scss']
})
export class AccountsComponent implements OnInit {
  accounts: Account[] = [];
  loading = false;
  errorMessage: string | null = null;
  formVisible = false;
  formMode: 'create' | 'edit' = 'create';
  editingAccount: Account | null = null;
  notification: { message: string; type: 'success' | 'error' } | null = null;
  deleteTarget: Account | null = null;
  /** Cuenta que se muestra en la ventana de detalle, con sus subcuentas. */
  detailTarget: Account | null = null;
  detailSubAccounts: SubAccount[] = [];
  detailLoading = false;
  detailError: string | null = null;
  /** Exportación del listado en curso. */
  exporting: 'pdf' | 'excel' | null = null;
  sortResetToken = 0;
  private notificationTimer: ReturnType<typeof setTimeout> | null = null;

  readonly filterForm = this.fb.group({
    id: [null as number | null],
    name: [''],
    type: ['']
  });

  constructor(
    private readonly fb: FormBuilder,
    private readonly accountsService: AccountService,
    private readonly subAccountService: SubAccountService
  ) {}

  ngOnInit(): void {
    this.search();
  }

  search(): void {
    this.loading = true;
    this.errorMessage = null;
    const filters = this.buildFilters();
    this.accountsService
      .search(filters)
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (items) => {
          this.accounts = items ?? [];
        },
        error: () => {
          this.errorMessage = 'No se pudieron consultar las cuentas.';
          this.accounts = [];
        }
      });
  }

  clearFilters(): void {
    this.filterForm.reset({
      id: null,
      name: '',
      type: ''
    });
    this.sortResetToken += 1;
    this.search();
  }

  openCreate(): void {
    this.formMode = 'create';
    this.editingAccount = null;
    this.formVisible = true;
  }

  openEdit(account: Account): void {
    this.formMode = 'edit';
    this.editingAccount = account;
    this.formVisible = true;
  }

  cancelForm(): void {
    this.formVisible = false;
    this.editingAccount = null;
  }

  submitForm(payload: Account): void {
    if (this.formMode === 'create') {
      this.accountsService.create(payload).subscribe({
        next: () => {
          this.notify('Cuenta creada correctamente.');
          this.formVisible = false;
          this.search();
        },
        error: () => this.notify('No se pudo crear la cuenta.', 'error')
      });
      return;
    }

    const id = this.editingAccount?.id;
    if (!id) {
      this.notify('No se pudo identificar la cuenta a editar.', 'error');
      return;
    }

    this.accountsService.update(id, payload).subscribe({
      next: () => {
        this.notify('Cuenta actualizada correctamente.');
        this.formVisible = false;
        this.editingAccount = null;
        this.search();
      },
      error: () => this.notify('No se pudo actualizar la cuenta.', 'error')
    });
  }

  deleteAccount(account: Account): void {
    this.deleteTarget = account;
  }

  confirmDelete(): void {
    if (!this.deleteTarget) return;
    const target = this.deleteTarget;
    this.accountsService.delete(target.id).subscribe({
      next: () => {
        this.deleteTarget = null;
        this.notify('Cuenta eliminada correctamente.');
        this.search();
      },
      error: () => {
        this.deleteTarget = null;
        this.notify('No se puede eliminar la cuenta porque tiene movimientos asociados.', 'error');
      }
    });
  }

  cancelDelete(): void {
    this.deleteTarget = null;
  }

  openDetail(account: Account): void {
    this.detailTarget = account;
    this.detailSubAccounts = [];
    this.detailError = null;
    this.detailLoading = true;
    this.subAccountService
      .search({ accountId: account.id })
      .pipe(finalize(() => {
        if (this.detailTarget === account) this.detailLoading = false;
      }))
      .subscribe({
        next: (items) => {
          // Si se cerró o se abrió otra cuenta mientras cargaba, la respuesta ya no corresponde.
          if (this.detailTarget !== account) return;
          this.detailSubAccounts = [...(items ?? [])].sort((a, b) =>
            (a.name ?? '').localeCompare(b.name ?? '', 'es', { sensitivity: 'base' })
          );
        },
        error: () => {
          if (this.detailTarget !== account) return;
          this.detailError = 'No se pudieron consultar las subcuentas.';
        }
      });
  }

  @HostListener('document:keydown.escape')
  closeDetail(): void {
    this.detailTarget = null;
    this.detailSubAccounts = [];
    this.detailLoading = false;
    this.detailError = null;
  }

  /** Sin dato en la base (registros anteriores) se muestra un guion. */
  formatYesNo(value: boolean | null | undefined): string {
    if (value === null || value === undefined) return '-';
    return value ? 'Sí' : 'No';
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
    this.accountsService
      .exportList(this.buildFilters(), format)
      .pipe(finalize(() => (this.exporting = null)))
      .subscribe({
        next: (blob) => this.saveFile(blob, `Listado_Cuentas_${this.fileTimestamp()}.${format === 'pdf' ? 'pdf' : 'xlsx'}`),
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

  private buildFilters(): AccountSearch {
    const raw = this.filterForm.getRawValue();
    return {
      id: raw.id,
      name: raw.name?.trim() || null,
      type: raw.type?.trim() || null
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
