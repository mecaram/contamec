import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { User } from '../../models/user.model';

type SortableColumn = 'id' | 'name' | 'userRoleName' | 'isActive';

@Component({
  selector: 'app-user-list',
  templateUrl: './user-list.component.html',
  styleUrls: ['./user-list.component.scss']
})
export class UserListComponent implements OnChanges {
  sortColumn: SortableColumn = 'id';
  sortDirection: 'asc' | 'desc' = 'desc';

  @Input() users: User[] = [];
  @Input() sortResetToken = 0;
  @Input() loading = false;
  @Output() edit = new EventEmitter<User>();
  @Output() remove = new EventEmitter<User>();

  ngOnChanges(changes: SimpleChanges): void {
    if (changes.sortResetToken && !changes.sortResetToken.firstChange) {
      this.sortColumn = 'id';
      this.sortDirection = 'desc';
    }
  }

  get totalRecords(): number {
    return this.sortedUsers.length;
  }

  get sortedUsers(): User[] {
    const data = [...this.users];
    const direction = this.sortDirection === 'asc' ? 1 : -1;
    return data.sort((a, b) => {
      const left = this.getSortValue(a, this.sortColumn);
      const right = this.getSortValue(b, this.sortColumn);
      if (left < right) return -1 * direction;
      if (left > right) return 1 * direction;
      return 0;
    });
  }

  onEdit(user: User): void {
    this.edit.emit(user);
  }

  onDelete(user: User): void {
    this.remove.emit(user);
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

  private getSortValue(user: User, column: SortableColumn): number | string {
    switch (column) {
      case 'id':
        return user.id ?? 0;
      case 'name':
        return (user.name ?? '').toLocaleLowerCase();
      case 'userRoleName':
        return (user.userRoleName ?? '').toLocaleLowerCase();
      case 'isActive':
        return user.isActive ? 1 : 0;
      default:
        return 0;
    }
  }
}
