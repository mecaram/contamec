import { Component, OnInit } from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { EmailTemplate } from './models/email-template.model';
import { EmailTemplateSearch } from './models/email-template-search.model';
import { TEMPLATE_TYPES, templateTypeLabel } from './models/email-template-catalog';
import { EmailTemplateService } from './services/email-template.service';

@Component({
  selector: 'app-email-templates',
  templateUrl: './email-templates.component.html',
  styleUrls: ['./email-templates.component.scss']
})
export class EmailTemplatesComponent implements OnInit {
  readonly templateTypes = TEMPLATE_TYPES;
  templates: EmailTemplate[] = [];
  /** Nombres con plantilla, sin filtros: el formulario no ofrece repetirlos. */
  usedNames: string[] = [];
  loading = false;
  errorMessage: string | null = null;
  formVisible = false;
  formMode: 'create' | 'edit' = 'create';
  editingTemplate: EmailTemplate | null = null;
  notification: { message: string; type: 'success' | 'error' } | null = null;
  deleteTarget: EmailTemplate | null = null;
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
    private readonly templateService: EmailTemplateService
  ) {}

  ngOnInit(): void {
    this.search();
  }

  search(): void {
    this.loading = true;
    this.errorMessage = null;
    const filters = this.buildFilters();
    this.templateService
      .search(filters)
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (items) => {
          this.templates = items ?? [];
        },
        error: () => {
          this.errorMessage = 'No se pudieron consultar las plantillas de correo.';
          this.templates = [];
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
    this.loadUsedNames();
    this.formMode = 'create';
    this.editingTemplate = null;
    this.formVisible = true;
  }

  openEdit(template: EmailTemplate): void {
    this.loadUsedNames();
    this.formMode = 'edit';
    this.editingTemplate = template;
    this.formVisible = true;
  }

  cancelForm(): void {
    this.formVisible = false;
    this.editingTemplate = null;
  }

  submitForm(payload: EmailTemplate): void {
    if (this.formMode === 'create') {
      this.templateService.create(payload).subscribe({
        next: () => {
          this.notify('Plantilla creada correctamente.');
          this.formVisible = false;
          this.search();
        },
        error: (error) => this.notify(error?.error?.message ?? 'No se pudo crear la plantilla.', 'error')
      });
      return;
    }

    const id = this.editingTemplate?.id;
    if (!id) {
      this.notify('No se pudo identificar la plantilla a editar.', 'error');
      return;
    }

    this.templateService.update(id, payload).subscribe({
      next: () => {
        this.notify('Plantilla actualizada correctamente.');
        this.formVisible = false;
        this.editingTemplate = null;
        this.search();
      },
      error: (error) => this.notify(error?.error?.message ?? 'No se pudo actualizar la plantilla.', 'error')
    });
  }

  deleteTemplate(template: EmailTemplate): void {
    this.deleteTarget = template;
  }

  confirmDelete(): void {
    if (!this.deleteTarget) return;
    const target = this.deleteTarget;
    this.templateService.delete(target.id).subscribe({
      next: () => {
        this.deleteTarget = null;
        this.notify('Plantilla eliminada correctamente.');
        this.search();
      },
      error: (error) => {
        this.deleteTarget = null;
        this.notify(error?.error?.message ?? 'No se pudo eliminar la plantilla.', 'error');
      }
    });
  }

  cancelDelete(): void {
    this.deleteTarget = null;
  }

  nameLabel(name: string | null | undefined): string {
    return templateTypeLabel(name);
  }

  closeNotification(): void {
    if (this.notificationTimer) {
      clearTimeout(this.notificationTimer);
      this.notificationTimer = null;
    }
    this.notification = null;
  }

  private loadUsedNames(): void {
    this.templateService.search({}).subscribe({
      next: (items) => (this.usedNames = (items ?? []).map((t) => t.name ?? '').filter((name) => !!name)),
      // Sin la lista el formulario igual funciona: el backend rechaza los nombres repetidos.
      error: () => (this.usedNames = [])
    });
  }

  /** Exporta el listado con los filtros actuales. */
  exportList(format: 'pdf' | 'excel'): void {
    if (this.exporting) return;

    this.exporting = format;
    this.templateService
      .exportList(this.buildFilters(), format)
      .pipe(finalize(() => (this.exporting = null)))
      .subscribe({
        next: (blob) => this.saveFile(blob, `Listado_PlantillasDeCorreo_${this.fileTimestamp()}.${format === 'pdf' ? 'pdf' : 'xlsx'}`),
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

  private buildFilters(): EmailTemplateSearch {
    const raw = this.filterForm.getRawValue();
    return {
      id: raw.id,
      name: raw.name || null
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
