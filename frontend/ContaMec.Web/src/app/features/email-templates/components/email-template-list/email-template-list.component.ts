import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { EmailTemplate } from '../../models/email-template.model';
import { templateTypeLabel } from '../../models/email-template-catalog';

type SortableColumn = 'id' | 'name' | 'subject' | 'createdAt';

@Component({
  selector: 'app-email-template-list',
  templateUrl: './email-template-list.component.html',
  styleUrls: ['./email-template-list.component.scss']
})
export class EmailTemplateListComponent implements OnChanges {
  sortColumn: SortableColumn = 'id';
  sortDirection: 'asc' | 'desc' = 'desc';
  /** En el celular no se ve el encabezado de la tabla: se ordena desde este selector. */
  readonly sortOptions: { column: SortableColumn; label: string }[] = [
    { column: 'id', label: 'ID' },
    { column: 'name', label: 'Nombre' },
    { column: 'subject', label: 'Asunto' },
    { column: 'createdAt', label: 'Fecha de alta' }
  ];
  currentPage = 1;
  readonly pageSize = 10;

  @Input() templates: EmailTemplate[] = [];
  @Input() sortResetToken = 0;
  @Input() loading = false;
  @Output() edit = new EventEmitter<EmailTemplate>();
  @Output() remove = new EventEmitter<EmailTemplate>();

  ngOnChanges(changes: SimpleChanges): void {
    if (changes.sortResetToken && !changes.sortResetToken.firstChange) {
      this.sortColumn = 'id';
      this.sortDirection = 'desc';
      this.currentPage = 1;
    }
    if (changes.templates && !changes.templates.firstChange) {
      this.ensureValidPage();
    }
  }

  get totalRecords(): number {
    return this.sortedTemplates.length;
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalRecords / this.pageSize));
  }

  get pagedTemplates(): EmailTemplate[] {
    const start = (this.currentPage - 1) * this.pageSize;
    return this.sortedTemplates.slice(start, start + this.pageSize);
  }

  get pageLabel(): string {
    return `Página ${this.currentPage} de ${this.totalPages}`;
  }

  get canGoPrevious(): boolean {
    return this.currentPage > 1;
  }

  get canGoNext(): boolean {
    return this.currentPage < this.totalPages;
  }

  get sortedTemplates(): EmailTemplate[] {
    const data = [...this.templates];
    const direction = this.sortDirection === 'asc' ? 1 : -1;
    return data.sort((a, b) => {
      const left = this.getSortValue(a, this.sortColumn);
      const right = this.getSortValue(b, this.sortColumn);
      if (left < right) return -1 * direction;
      if (left > right) return 1 * direction;
      return 0;
    });
  }

  nameLabel(name: string | null | undefined): string {
    return templateTypeLabel(name);
  }

  onEdit(template: EmailTemplate): void {
    this.edit.emit(template);
  }

  onDelete(template: EmailTemplate): void {
    this.remove.emit(template);
  }

  setSort(column: SortableColumn): void {
    if (this.sortColumn === column) {
      this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
      this.currentPage = 1;
      return;
    }
    this.sortColumn = column;
    this.sortDirection = 'asc';
    this.currentPage = 1;
  }

  sortBy(column: SortableColumn): void {
    this.sortColumn = column;
    this.currentPage = 1;
  }

  toggleSortDirection(): void {
    this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    this.currentPage = 1;
  }

  sortIndicator(column: SortableColumn): string {
    if (this.sortColumn !== column) return '';
    return this.sortDirection === 'asc' ? '▲' : '▼';
  }

  goToPreviousPage(): void {
    if (!this.canGoPrevious) return;
    this.currentPage -= 1;
  }

  goToNextPage(): void {
    if (!this.canGoNext) return;
    this.currentPage += 1;
  }

  private getSortValue(template: EmailTemplate, column: SortableColumn): number | string {
    switch (column) {
      case 'id':
        return template.id ?? 0;
      case 'name':
        return this.nameLabel(template.name).toLocaleLowerCase();
      case 'subject':
        return (template.subject ?? '').toLocaleLowerCase();
      case 'createdAt':
        return template.createdAt ? new Date(template.createdAt).getTime() : 0;
      default:
        return 0;
    }
  }

  private ensureValidPage(): void {
    if (this.currentPage > this.totalPages) {
      this.currentPage = this.totalPages;
    }
    if (this.currentPage < 1) {
      this.currentPage = 1;
    }
  }
}
