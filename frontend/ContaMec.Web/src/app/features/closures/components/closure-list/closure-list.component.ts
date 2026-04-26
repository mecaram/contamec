import { Component, EventEmitter, Input, Output } from '@angular/core';
import { Closure } from '../../models/closure.model';

type SortableColumn = 'id' | 'openDate' | 'previousBalance' | 'incomes' | 'expenses' | 'result';

@Component({
  selector: 'app-closure-list',
  templateUrl: './closure-list.component.html',
  styleUrls: ['./closure-list.component.scss']
})
export class ClosureListComponent {
  sortColumn: SortableColumn = 'id';
  sortDirection: 'asc' | 'desc' = 'desc';

  @Input() closures: Closure[] = [];
  @Input() loading = false;
  @Output() viewDetail = new EventEmitter<Closure>();
  @Output() viewIncomes = new EventEmitter<Closure>();
  @Output() viewExpenses = new EventEmitter<Closure>();
  @Output() viewBalanceValues = new EventEmitter<Closure>();
  @Output() closeClosure = new EventEmitter<Closure>();

  get sortedClosures(): Closure[] {
    const data = [...this.closures];
    const direction = this.sortDirection === 'asc' ? 1 : -1;
    return data.sort((a, b) => {
      const left = this.getSortValue(a, this.sortColumn);
      const right = this.getSortValue(b, this.sortColumn);
      if (left < right) return -1 * direction;
      if (left > right) return 1 * direction;
      return 0;
    });
  }

  get totalRecords(): number {
    return this.closures.length;
  }

  get totalPreviousBalance(): number {
    return this.closures.reduce((acc, row) => acc + (row.previousBalance ?? 0), 0);
  }

  get totalIncomes(): number {
    return this.closures.reduce((acc, row) => acc + (row.incomes ?? 0), 0);
  }

  get totalExpenses(): number {
    return this.closures.reduce((acc, row) => acc + (row.expenses ?? 0), 0);
  }

  get totalResultCalculated(): number {
    return this.totalPreviousBalance + this.totalIncomes - this.totalExpenses;
  }

  formatDate(value?: string | null): string {
    if (!value) return '-';
    const source = value.trim();
    const dateOnlyMatch = /^(\d{4})-(\d{2})-(\d{2})/.exec(source);
    if (dateOnlyMatch) {
      const [, yyyy, mm, dd] = dateOnlyMatch;
      return `${dd}/${mm}/${yyyy}`;
    }

    const date = new Date(source);
    if (Number.isNaN(date.getTime())) return '-';

    const dd = String(date.getDate()).padStart(2, '0');
    const mm = String(date.getMonth() + 1).padStart(2, '0');
    const yyyy = String(date.getFullYear());
    return `${dd}/${mm}/${yyyy}`;
  }

  formatResult(value?: number | null): string {
    if (value === null || value === undefined) return '-';
    return new Intl.NumberFormat('es-AR', {
      style: 'currency',
      currency: 'ARS',
      minimumFractionDigits: 2
    }).format(value);
  }

  getDifference(row: Closure): number {
    const inAccount = row.inAccount ?? 0;
    const result = row.result ?? 0;
    return inAccount - result;
  }

  canClose(row: Closure): boolean {
    if (row.isClosed) return false;
    return Math.abs(this.getDifference(row)) <= 0.009;
  }

  closeButtonTitle(row: Closure): string {
    if (row.isClosed) return 'El cierre ya está cerrado';
    if (!this.canClose(row)) return 'Solo se puede cerrar cuando la diferencia es igual a 0';
    return 'Cerrar';
  }

  setSort(column: SortableColumn): void {
    if (this.sortColumn === column) {
      this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
      return;
    }
    this.sortColumn = column;
    this.sortDirection = 'asc';
  }

  sortIndicator(column: SortableColumn): string {
    if (this.sortColumn !== column) return '';
    return this.sortDirection === 'asc' ? '▲' : '▼';
  }

  private getSortValue(row: Closure, column: SortableColumn): number {
    switch (column) {
      case 'id':
        return row.id ?? 0;
      case 'openDate':
        return this.toTimestamp(row.openDate);
      case 'previousBalance':
        return row.previousBalance ?? 0;
      case 'incomes':
        return row.incomes ?? 0;
      case 'expenses':
        return row.expenses ?? 0;
      case 'result':
        return row.result ?? 0;
      default:
        return 0;
    }
  }

  private toTimestamp(value?: string | null): number {
    if (!value) return 0;
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return 0;
    return date.getTime();
  }

  onViewIncomes(row: Closure): void {
    this.viewIncomes.emit(row);
  }

  onViewExpenses(row: Closure): void {
    this.viewExpenses.emit(row);
  }

  onViewDetail(row: Closure): void {
    this.viewDetail.emit(row);
  }

  onViewBalanceValues(row: Closure): void {
    this.viewBalanceValues.emit(row);
  }

  onCloseClosure(row: Closure): void {
    if (!this.canClose(row)) return;
    this.closeClosure.emit(row);
  }
}
