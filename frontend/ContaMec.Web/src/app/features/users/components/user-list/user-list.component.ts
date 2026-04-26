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
  currentPage = 1;
  readonly pageSize = 10;

  @Input() users: User[] = [];
  @Input() sortResetToken = 0;
  @Input() loading = false;
  @Output() edit = new EventEmitter<User>();
  @Output() remove = new EventEmitter<User>();

  ngOnChanges(changes: SimpleChanges): void {
    if (changes.sortResetToken && !changes.sortResetToken.firstChange) {
      this.sortColumn = 'id';
      this.sortDirection = 'desc';
      this.currentPage = 1;
    }
    if (changes.users && !changes.users.firstChange) {
      this.ensureValidPage();
    }
  }

  get totalRecords(): number {
    return this.sortedUsers.length;
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalRecords / this.pageSize));
  }

  get pagedUsers(): User[] {
    const start = (this.currentPage - 1) * this.pageSize;
    return this.sortedUsers.slice(start, start + this.pageSize);
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

  private ensureValidPage(): void {
    if (this.currentPage > this.totalPages) {
      this.currentPage = this.totalPages;
    }
    if (this.currentPage < 1) {
      this.currentPage = 1;
    }
  }
}
