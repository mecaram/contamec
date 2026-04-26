import { Component, OnInit } from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { User } from './models/user.model';
import { UserSearch } from './models/user-search.model';
import { UserRoleOption, UserService } from './services/user.service';

@Component({
  selector: 'app-users',
  templateUrl: './users.component.html',
  styleUrls: ['./users.component.scss']
})
export class UsersComponent implements OnInit {
  users: User[] = [];
  roleOptions: UserRoleOption[] = [];
  loading = false;
  errorMessage: string | null = null;
  formVisible = false;
  formMode: 'create' | 'edit' = 'create';
  editingUser: User | null = null;
  notification: { message: string; type: 'success' | 'error' } | null = null;
  deleteTarget: User | null = null;
  sortResetToken = 0;
  private notificationTimer: ReturnType<typeof setTimeout> | null = null;

  readonly filterForm = this.fb.group({
    id: [null as number | null],
    name: [''],
    isActive: [''],
    userRoleId: [null as number | null]
  });

  constructor(
    private readonly fb: FormBuilder,
    private readonly userService: UserService
  ) {}

  ngOnInit(): void {
    this.loadRoleOptions();
    this.search();
  }

  search(): void {
    this.loading = true;
    this.errorMessage = null;
    const filters = this.buildFilters();
    this.userService
      .search(filters)
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (items) => {
          this.users = items ?? [];
        },
        error: () => {
          this.errorMessage = 'No se pudieron consultar los usuarios.';
          this.users = [];
        }
      });
  }

  clearFilters(): void {
    this.filterForm.reset({
      id: null,
      name: '',
      isActive: '',
      userRoleId: null
    });
    this.sortResetToken += 1;
    this.search();
  }

  openCreate(): void {
    this.formMode = 'create';
    this.editingUser = null;
    this.formVisible = true;
  }

  openEdit(user: User): void {
    this.formMode = 'edit';
    this.editingUser = user;
    this.formVisible = true;
  }

  cancelForm(): void {
    this.formVisible = false;
    this.editingUser = null;
  }

  submitForm(payload: User): void {
    if (this.formMode === 'create') {
      this.userService.create(payload).subscribe({
        next: () => {
          this.notify('Usuario creado correctamente.');
          this.formVisible = false;
          this.search();
        },
        error: (error) => this.notify(error?.error?.message ?? 'No se pudo crear el usuario.', 'error')
      });
      return;
    }

    const id = this.editingUser?.id;
    if (!id) {
      this.notify('No se pudo identificar el usuario a editar.', 'error');
      return;
    }

    this.userService.update(id, payload).subscribe({
      next: () => {
        this.notify('Usuario actualizado correctamente.');
        this.formVisible = false;
        this.editingUser = null;
        this.search();
      },
      error: (error) => this.notify(error?.error?.message ?? 'No se pudo actualizar el usuario.', 'error')
    });
  }

  deleteUser(user: User): void {
    this.deleteTarget = user;
  }

  confirmDelete(): void {
    if (!this.deleteTarget) return;
    const target = this.deleteTarget;
    this.userService.delete(target.id).subscribe({
      next: () => {
        this.deleteTarget = null;
        this.notify('Usuario eliminado correctamente.');
        this.search();
      },
      error: (error) => {
        this.deleteTarget = null;
        this.notify(error?.error?.message ?? 'No se pudo eliminar el usuario.', 'error');
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

  private loadRoleOptions(): void {
    this.userService.getRoleOptions().subscribe({
      next: (options) => {
        this.roleOptions = options ?? [];
      },
      error: () => {
        this.roleOptions = [];
      }
    });
  }

  private buildFilters(): UserSearch {
    const raw = this.filterForm.getRawValue();
    let isActive: boolean | null = null;
    if (raw.isActive === 'true') isActive = true;
    else if (raw.isActive === 'false') isActive = false;

    return {
      id: raw.id,
      name: raw.name?.trim() || null,
      isActive,
      userRoleId: raw.userRoleId
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
