import { Component, OnInit } from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { Account } from '../accounts/models/account.model';
import { AccountService } from '../accounts/services/account.service';
import { SubAccount } from './models/sub-account.model';
import { SubAccountSearch } from './models/sub-account-search.model';
import { SubAccountService } from './services/sub-account.service';

@Component({
  selector: 'app-sub-accounts',
  templateUrl: './sub-accounts.component.html',
  styleUrls: ['./sub-accounts.component.scss']
})
export class SubAccountsComponent implements OnInit {
  subAccounts: SubAccount[] = [];
  /** Cuentas activas que requieren subcuentas, para los combos de filtro y formulario. */
  accountOptions: Account[] = [];
  loading = false;
  errorMessage: string | null = null;
  formVisible = false;
  formMode: 'create' | 'edit' = 'create';
  editingSubAccount: SubAccount | null = null;
  notification: { message: string; type: 'success' | 'error' } | null = null;
  deleteTarget: SubAccount | null = null;
  sortResetToken = 0;
  private notificationTimer: ReturnType<typeof setTimeout> | null = null;

  readonly filterForm = this.fb.group({
    id: [null as number | null],
    name: [''],
    accountId: [null as number | null]
  });

  constructor(
    private readonly fb: FormBuilder,
    private readonly subAccountsService: SubAccountService,
    private readonly accountsService: AccountService
  ) {}

  ngOnInit(): void {
    this.loadAccounts();
    this.search();
  }

  search(): void {
    this.loading = true;
    this.errorMessage = null;
    const filters = this.buildFilters();
    this.subAccountsService
      .search(filters)
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (items) => {
          this.subAccounts = items ?? [];
        },
        error: () => {
          this.errorMessage = 'No se pudieron consultar las subcuentas.';
          this.subAccounts = [];
        }
      });
  }

  clearFilters(): void {
    this.filterForm.reset({
      id: null,
      name: '',
      accountId: null
    });
    this.sortResetToken += 1;
    this.search();
  }

  openCreate(): void {
    this.formMode = 'create';
    this.editingSubAccount = null;
    this.formVisible = true;
  }

  openEdit(subAccount: SubAccount): void {
    this.formMode = 'edit';
    this.editingSubAccount = subAccount;
    this.formVisible = true;
  }

  cancelForm(): void {
    this.formVisible = false;
    this.editingSubAccount = null;
  }

  submitForm(payload: SubAccount): void {
    if (this.formMode === 'create') {
      this.subAccountsService.create(payload).subscribe({
        next: () => {
          this.notify('Subcuenta creada correctamente.');
          this.formVisible = false;
          this.search();
        },
        error: () => this.notify('No se pudo crear la subcuenta.', 'error')
      });
      return;
    }

    const id = this.editingSubAccount?.id;
    if (!id) {
      this.notify('No se pudo identificar la subcuenta a editar.', 'error');
      return;
    }

    this.subAccountsService.update(id, payload).subscribe({
      next: () => {
        this.notify('Subcuenta actualizada correctamente.');
        this.formVisible = false;
        this.editingSubAccount = null;
        this.search();
      },
      error: () => this.notify('No se pudo actualizar la subcuenta.', 'error')
    });
  }

  deleteSubAccount(subAccount: SubAccount): void {
    this.deleteTarget = subAccount;
  }

  confirmDelete(): void {
    if (!this.deleteTarget) return;
    const target = this.deleteTarget;
    this.subAccountsService.delete(target.id).subscribe({
      next: () => {
        this.deleteTarget = null;
        this.notify('Subcuenta eliminada correctamente.');
        this.search();
      },
      error: () => {
        this.deleteTarget = null;
        this.notify('No se puede eliminar la subcuenta porque tiene movimientos asociados.', 'error');
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

  private loadAccounts(): void {
    this.accountsService.search({}).subscribe({
      next: (items) => {
        // Solo las cuentas activas (sin dato cargado se consideran activas) que requieren subcuentas.
        this.accountOptions = (items ?? [])
          .filter((a) => (a.active ?? true) && a.subAccountRequires === true)
          .sort((a, b) => (a.name ?? '').localeCompare(b.name ?? '', 'es'));
      },
      error: () => {
        this.accountOptions = [];
        this.notify('No se pudieron cargar las cuentas.', 'error');
      }
    });
  }

  private buildFilters(): SubAccountSearch {
    const raw = this.filterForm.getRawValue();
    return {
      id: raw.id,
      name: raw.name?.trim() || null,
      accountId: raw.accountId
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
