import { Component, OnInit } from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { Application, ApplicationDeleteResult } from './models/application.model';
import { ApplicationSearch } from './models/application-search.model';
import { ApplicationService } from './services/application.service';

@Component({
  selector: 'app-applications',
  templateUrl: './applications.component.html',
  styleUrls: ['./applications.component.scss']
})
export class ApplicationsComponent implements OnInit {
  applications: Application[] = [];
  loading = false;
  errorMessage: string | null = null;
  formVisible = false;
  formMode: 'create' | 'edit' = 'create';
  editingApplication: Application | null = null;
  notification: { message: string; type: 'success' | 'error' } | null = null;
  deleteTarget: Application | null = null;
  deleting = false;
  /** Motivo por el que el backend no eliminó la aplicación (por ejemplo, que está activa). */
  deleteError: string | null = null;
  /** Resumen de la eliminación: queda abierto hasta que el operador lo cierra. */
  deleteResult: { applicationId: number; name: string; result: ApplicationDeleteResult } | null = null;
  /** Exportación del listado en curso. */
  exporting: 'pdf' | 'excel' | null = null;
  activateTarget: Application | null = null;
  sortResetToken = 0;
  private notificationTimer: ReturnType<typeof setTimeout> | null = null;

  readonly filterForm = this.fb.group({
    id: [null as number | null],
    name: [''],
    active: ['']
  });

  constructor(
    private readonly fb: FormBuilder,
    private readonly applicationService: ApplicationService
  ) {}

  ngOnInit(): void {
    this.search();
  }

  search(): void {
    this.loading = true;
    this.errorMessage = null;
    const filters = this.buildFilters();
    this.applicationService
      .search(filters)
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (items) => {
          this.applications = items ?? [];
        },
        error: () => {
          this.errorMessage = 'No se pudieron consultar las aplicaciones.';
          this.applications = [];
        }
      });
  }

  clearFilters(): void {
    this.filterForm.reset({
      id: null,
      name: '',
      active: ''
    });
    this.sortResetToken += 1;
    this.search();
  }

  openCreate(): void {
    this.formMode = 'create';
    this.editingApplication = null;
    this.formVisible = true;
  }

  openEdit(application: Application): void {
    this.formMode = 'edit';
    this.editingApplication = application;
    this.formVisible = true;
  }

  cancelForm(): void {
    this.formVisible = false;
    this.editingApplication = null;
  }

  submitForm(payload: Application): void {
    if (this.formMode === 'create') {
      this.applicationService.create(payload).subscribe({
        next: () => {
          this.notify('Aplicación creada correctamente.');
          this.formVisible = false;
          this.search();
        },
        error: (error) => this.notify(error?.error?.message ?? 'No se pudo crear la aplicación.', 'error')
      });
      return;
    }

    const id = this.editingApplication?.id;
    if (!id) {
      this.notify('No se pudo identificar la aplicación a editar.', 'error');
      return;
    }

    this.applicationService.update(id, payload).subscribe({
      next: () => {
        this.notify('Aplicación actualizada correctamente.');
        this.formVisible = false;
        this.editingApplication = null;
        this.search();
      },
      error: (error) => this.notify(error?.error?.message ?? 'No se pudo actualizar la aplicación.', 'error')
    });
  }

  deleteApplication(application: Application): void {
    this.deleteTarget = application;
    this.deleteError = null;
  }

  confirmDelete(): void {
    if (!this.deleteTarget || this.deleting) return;
    const target = this.deleteTarget;
    this.deleting = true;
    this.deleteError = null;
    // Las reglas (activa, datos asociados) las valida el backend.
    this.applicationService
      .delete(target.id)
      .pipe(finalize(() => (this.deleting = false)))
      .subscribe({
        next: (result) => {
          this.deleteTarget = null;
          this.deleteResult = { applicationId: target.id, name: target.name || '-', result };
          this.search();
        },
        error: (error) => {
          this.deleteError = error?.error?.message ?? 'No se pudo eliminar la aplicación.';
        }
      });
  }

  cancelDelete(): void {
    if (this.deleting) return;
    this.deleteTarget = null;
    this.deleteError = null;
  }

  closeDeleteResult(): void {
    this.deleteResult = null;
  }

  activateApplication(application: Application): void {
    this.activateTarget = application;
  }

  confirmActivate(): void {
    if (!this.activateTarget) return;
    const target = this.activateTarget;
    this.applicationService.activate(target.id).subscribe({
      next: (result) => {
        this.activateTarget = null;
        if (!result?.email) {
          this.notify('Aplicación y usuarios activados. La empresa no tiene correo para avisarle.');
        } else if (result.emailSent) {
          this.notify(`Aplicación y usuarios activados. Se envió el aviso a ${result.email}.`);
        } else {
          this.notify(`Aplicación y usuarios activados, pero no se pudo enviar el aviso a ${result.email}.`, 'error');
        }
        this.search();
      },
      error: (error) => {
        this.activateTarget = null;
        this.notify(error?.error?.message ?? 'No se pudo activar la aplicación.', 'error');
      }
    });
  }

  cancelActivate(): void {
    this.activateTarget = null;
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
    this.applicationService
      .exportList(this.buildFilters(), format)
      .pipe(finalize(() => (this.exporting = null)))
      .subscribe({
        next: (blob) => this.saveFile(blob, `Listado_Aplicaciones_${this.fileTimestamp()}.${format === 'pdf' ? 'pdf' : 'xlsx'}`),
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

  private buildFilters(): ApplicationSearch {
    const raw = this.filterForm.getRawValue();
    let active: boolean | null = null;
    if (raw.active === 'true') active = true;
    else if (raw.active === 'false') active = false;

    return {
      id: raw.id,
      name: raw.name?.trim() || null,
      active
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
