import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { Income } from '../../models/income.model';

type SortableColumn = 'id' | 'emissionDate' | 'accountName' | 'detail' | 'amount';

@Component({
  selector: 'app-income-list',
  templateUrl: './income-list.component.html',
  styleUrls: ['./income-list.component.scss']
})
export class IncomeListComponent implements OnChanges {
  readonly sortableColumns: SortableColumn[] = ['id', 'emissionDate', 'accountName', 'detail', 'amount'];
  sortColumn: SortableColumn = 'id';
  sortDirection: 'asc' | 'desc' = 'desc';

  @Input() incomes: Income[] = [];
  @Input() closedClosureIds: number[] = [];
  @Input() sortResetToken = 0;
  @Input() loading = false;
  @Output() edit = new EventEmitter<Income>();
  @Output() remove = new EventEmitter<Income>();

  ngOnChanges(changes: SimpleChanges): void {
    if (changes.sortResetToken && !changes.sortResetToken.firstChange) {
      this.sortColumn = 'id';
      this.sortDirection = 'desc';
    }
  }

  get totalRecords(): number {
    return this.sortedIncomes.length;
  }

  get totalAmount(): number {
    return this.sortedIncomes.reduce((acc, item) => acc + (item.amount ?? 0), 0);
  }

  get sortedIncomes(): Income[] {
    const data = [...this.incomes];
    const direction = this.sortDirection === 'asc' ? 1 : -1;

    return data.sort((a, b) => {
      const left = this.getSortValue(a, this.sortColumn);
      const right = this.getSortValue(b, this.sortColumn);
      if (left < right) return -1 * direction;
      if (left > right) return 1 * direction;
      return 0;
    });
  }

  onEdit(income: Income): void {
    this.edit.emit(income);
  }

  onDelete(income: Income): void {
    this.remove.emit(income);
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

  canManage(income: Income): boolean {
    if (income.closureId === null || income.closureId === undefined) return true;
    return !this.closedClosureIds.includes(income.closureId);
  }

  private getSortValue(income: Income, column: SortableColumn): number | string {
    switch (column) {
      case 'id':
        return income.id ?? 0;
      case 'emissionDate':
        return income.emissionDate ? new Date(income.emissionDate).getTime() : 0;
      case 'accountName':
        return (income.accountName ?? '').toLocaleLowerCase();
      case 'detail':
        return (income.detail ?? '').toLocaleLowerCase();
      case 'amount':
        return income.amount ?? 0;
      default:
        return 0;
    }
  }
}
