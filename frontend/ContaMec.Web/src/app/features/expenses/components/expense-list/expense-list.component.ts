import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { Expense } from '../../models/expense.model';

type SortableColumn = 'id' | 'emissionDate' | 'accountName' | 'detail' | 'amount';

@Component({
  selector: 'app-expense-list',
  templateUrl: './expense-list.component.html',
  styleUrls: ['./expense-list.component.scss']
})
export class ExpenseListComponent implements OnChanges {
  readonly sortableColumns: SortableColumn[] = ['id', 'emissionDate', 'accountName', 'detail', 'amount'];
  sortColumn: SortableColumn = 'id';
  sortDirection: 'asc' | 'desc' = 'desc';
  currentPage = 1;
  readonly pageSize = 10;

  @Input() expenses: Expense[] = [];
  @Input() closedClosureIds: number[] = [];
  @Input() sortResetToken = 0;
  @Input() loading = false;
  @Output() edit = new EventEmitter<Expense>();
  @Output() remove = new EventEmitter<Expense>();

  ngOnChanges(changes: SimpleChanges): void {
    if (changes.sortResetToken && !changes.sortResetToken.firstChange) {
      this.sortColumn = 'id';
      this.sortDirection = 'desc';
      this.currentPage = 1;
    }
    if (changes.expenses && !changes.expenses.firstChange) {
      this.ensureValidPage();
    }
  }

  get totalRecords(): number {
    return this.sortedExpenses.length;
  }

  get totalAmount(): number {
    return this.sortedExpenses.reduce((acc, item) => acc + (item.amount ?? 0), 0);
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalRecords / this.pageSize));
  }

  get pagedExpenses(): Expense[] {
    const start = (this.currentPage - 1) * this.pageSize;
    return this.sortedExpenses.slice(start, start + this.pageSize);
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

  get sortedExpenses(): Expense[] {
    const data = [...this.expenses];
    const direction = this.sortDirection === 'asc' ? 1 : -1;
    return data.sort((a, b) => {
      const left = this.getSortValue(a, this.sortColumn);
      const right = this.getSortValue(b, this.sortColumn);
      if (left < right) return -1 * direction;
      if (left > right) return 1 * direction;
      return 0;
    });
  }

  onEdit(expense: Expense): void {
    this.edit.emit(expense);
  }

  onDelete(expense: Expense): void {
    this.remove.emit(expense);
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

  canManage(expense: Expense): boolean {
    if (expense.closureId === null || expense.closureId === undefined) return true;
    return !this.closedClosureIds.includes(expense.closureId);
  }

  private getSortValue(expense: Expense, column: SortableColumn): number | string {
    switch (column) {
      case 'id':
        return expense.id ?? 0;
      case 'emissionDate':
        return expense.emissionDate ? new Date(expense.emissionDate).getTime() : 0;
      case 'accountName':
        return (expense.accountName ?? '').toLocaleLowerCase();
      case 'detail':
        return (expense.detail ?? '').toLocaleLowerCase();
      case 'amount':
        return expense.amount ?? 0;
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
