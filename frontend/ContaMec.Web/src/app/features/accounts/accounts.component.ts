import { Component, OnInit } from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { finalize } from 'rxjs/operators';
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
  sortResetToken = 0;
  private notificationTimer: ReturnType<typeof setTimeout> | null = null;

  readonly filterForm = this.fb.group({
    id: [null as number | null],
    name: [''],
    type: ['']
  });

  constructor(
    private readonly fb: FormBuilder,
    private readonly accountsService: AccountService
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

  closeNotification(): void {
    if (this.notificationTimer) {
      clearTimeout(this.notificationTimer);
      this.notificationTimer = null;
    }
    this.notification = null;
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
