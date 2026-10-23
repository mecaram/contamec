import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { Account } from '../../models/account.model';

type SortableColumn = 'id' | 'name' | 'type' | 'subAccountRequires' | 'behaviorType' | 'active';

@Component({
  selector: 'app-account-list',
  templateUrl: './account-list.component.html',
  styleUrls: ['./account-list.component.scss']
})
export class AccountListComponent implements OnChanges {
  sortColumn: SortableColumn = 'id';
  sortDirection: 'asc' | 'desc' = 'desc';
  /** En el celular no se ve el encabezado de la tabla: se ordena desde este selector. */
  readonly sortOptions: { column: SortableColumn; label: string }[] = [
    { column: 'id', label: 'ID' },
    { column: 'name', label: 'Nombre' },
    { column: 'type', label: 'Tipo' },
    { column: 'subAccountRequires', label: 'Subcuentas' },
    { column: 'behaviorType', label: 'Comportamiento' },
    { column: 'active', label: 'Activo' }
  ];
  currentPage = 1;
  readonly pageSize = 10;

  @Input() accounts: Account[] = [];
  @Input() sortResetToken = 0;
  @Input() loading = false;
  @Output() edit = new EventEmitter<Account>();
  @Output() remove = new EventEmitter<Account>();
  /** Solo para cuentas que requieren subcuentas: muestra la cuenta con su listado de subcuentas. */
  @Output() viewDetail = new EventEmitter<Account>();

  ngOnChanges(changes: SimpleChanges): void {
    if (changes.sortResetToken && !changes.sortResetToken.firstChange) {
      this.sortColumn = 'id';
      this.sortDirection = 'desc';
      this.currentPage = 1;
    }
    if (changes.accounts && !changes.accounts.firstChange) {
      this.ensureValidPage();
    }
  }

  get totalRecords(): number {
    return this.sortedAccounts.length;
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalRecords / this.pageSize));
  }

  get pagedAccounts(): Account[] {
    const start = (this.currentPage - 1) * this.pageSize;
    return this.sortedAccounts.slice(start, start + this.pageSize);
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

  get sortedAccounts(): Account[] {
    const data = [...this.accounts];
    const direction = this.sortDirection === 'asc' ? 1 : -1;
    return data.sort((a, b) => {
      const left = this.getSortValue(a, this.sortColumn);
      const right = this.getSortValue(b, this.sortColumn);
      if (left < right) return -1 * direction;
      if (left > right) return 1 * direction;
      return 0;
    });
  }

  onEdit(account: Account): void {
    this.edit.emit(account);
  }

  onDelete(account: Account): void {
    this.remove.emit(account);
  }

  onViewDetail(account: Account): void {
    if (!account.subAccountRequires) return;
    this.viewDetail.emit(account);
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

  private getSortValue(account: Account, column: SortableColumn): number | string {
    switch (column) {
      case 'id':
        return account.id ?? 0;
      case 'name':
        return (account.name ?? '').toLocaleLowerCase();
      case 'type':
        return (account.type ?? '').toLocaleLowerCase();
      case 'subAccountRequires':
        return this.formatYesNo(account.subAccountRequires).toLocaleLowerCase();
      case 'behaviorType':
        return (account.behaviorType ?? '').toLocaleLowerCase();
      case 'active':
        return this.formatYesNo(account.active).toLocaleLowerCase();
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
