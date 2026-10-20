import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { SubAccount } from '../../models/sub-account.model';

type SortableColumn = 'id' | 'name' | 'accountName' | 'behaviorType' | 'active';

@Component({
  selector: 'app-sub-account-list',
  templateUrl: './sub-account-list.component.html',
  styleUrls: ['./sub-account-list.component.scss']
})
export class SubAccountListComponent implements OnChanges {
  sortColumn: SortableColumn = 'id';
  sortDirection: 'asc' | 'desc' = 'desc';
  /** En el celular no se ve el encabezado de la tabla: se ordena desde este selector. */
  readonly sortOptions: { column: SortableColumn; label: string }[] = [
    { column: 'id', label: 'ID' },
    { column: 'name', label: 'Nombre' },
    { column: 'accountName', label: 'Cuenta' },
    { column: 'behaviorType', label: 'Comportamiento' },
    { column: 'active', label: 'Activo' }
  ];
  currentPage = 1;
  readonly pageSize = 10;

  @Input() subAccounts: SubAccount[] = [];
  @Input() sortResetToken = 0;
  @Input() loading = false;
  @Output() edit = new EventEmitter<SubAccount>();
  @Output() remove = new EventEmitter<SubAccount>();

  ngOnChanges(changes: SimpleChanges): void {
    if (changes.sortResetToken && !changes.sortResetToken.firstChange) {
      this.sortColumn = 'id';
      this.sortDirection = 'desc';
      this.currentPage = 1;
    }
    if (changes.subAccounts && !changes.subAccounts.firstChange) {
      this.ensureValidPage();
    }
  }

  get totalRecords(): number {
    return this.sortedSubAccounts.length;
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalRecords / this.pageSize));
  }

  get pagedSubAccounts(): SubAccount[] {
    const start = (this.currentPage - 1) * this.pageSize;
    return this.sortedSubAccounts.slice(start, start + this.pageSize);
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

  get sortedSubAccounts(): SubAccount[] {
    const data = [...this.subAccounts];
    const direction = this.sortDirection === 'asc' ? 1 : -1;
    return data.sort((a, b) => {
      const left = this.getSortValue(a, this.sortColumn);
      const right = this.getSortValue(b, this.sortColumn);
      if (left < right) return -1 * direction;
      if (left > right) return 1 * direction;
      return 0;
    });
  }

  onEdit(subAccount: SubAccount): void {
    this.edit.emit(subAccount);
  }

  onDelete(subAccount: SubAccount): void {
    this.remove.emit(subAccount);
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

  /** Sin dato en la base (registros anteriores) se muestra un guion. */
  formatYesNo(value: boolean | null | undefined): string {
    if (value === null || value === undefined) return '-';
    return value ? 'Sí' : 'No';
  }

  goToPreviousPage(): void {
    if (!this.canGoPrevious) return;
    this.currentPage -= 1;
  }

  goToNextPage(): void {
    if (!this.canGoNext) return;
    this.currentPage += 1;
  }

  private getSortValue(subAccount: SubAccount, column: SortableColumn): number | string {
    switch (column) {
      case 'id':
        return subAccount.id ?? 0;
      case 'name':
        return (subAccount.name ?? '').toLocaleLowerCase();
      case 'accountName':
        return (subAccount.accountName ?? '').toLocaleLowerCase();
      case 'behaviorType':
        return (subAccount.behaviorType ?? '').toLocaleLowerCase();
      case 'active':
        return this.formatYesNo(subAccount.active).toLocaleLowerCase();
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
