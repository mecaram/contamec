import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { Account } from '../../models/account.model';

type SortableColumn = 'id' | 'name' | 'type';

@Component({
  selector: 'app-account-list',
  templateUrl: './account-list.component.html',
  styleUrls: ['./account-list.component.scss']
})
export class AccountListComponent implements OnChanges {
  sortColumn: SortableColumn = 'id';
  sortDirection: 'asc' | 'desc' = 'desc';

  @Input() accounts: Account[] = [];
  @Input() sortResetToken = 0;
  @Input() loading = false;
  @Output() edit = new EventEmitter<Account>();
  @Output() remove = new EventEmitter<Account>();

  ngOnChanges(changes: SimpleChanges): void {
    if (changes.sortResetToken && !changes.sortResetToken.firstChange) {
      this.sortColumn = 'id';
      this.sortDirection = 'desc';
    }
  }

  get totalRecords(): number {
    return this.sortedAccounts.length;
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

  private getSortValue(account: Account, column: SortableColumn): number | string {
    switch (column) {
      case 'id':
        return account.id ?? 0;
      case 'name':
        return (account.name ?? '').toLocaleLowerCase();
      case 'type':
        return (account.type ?? '').toLocaleLowerCase();
      default:
        return 0;
    }
  }
}
