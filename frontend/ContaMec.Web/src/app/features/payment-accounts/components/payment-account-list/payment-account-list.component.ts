import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { PaymentAccount } from '../../models/payment-account.model';

type SortableColumn = 'id' | 'name';

@Component({
  selector: 'app-payment-account-list',
  templateUrl: './payment-account-list.component.html',
  styleUrls: ['./payment-account-list.component.scss']
})
export class PaymentAccountListComponent implements OnChanges {
  sortColumn: SortableColumn = 'id';
  sortDirection: 'asc' | 'desc' = 'desc';

  @Input() paymentAccounts: PaymentAccount[] = [];
  @Input() sortResetToken = 0;
  @Input() loading = false;
  @Output() edit = new EventEmitter<PaymentAccount>();
  @Output() remove = new EventEmitter<PaymentAccount>();

  ngOnChanges(changes: SimpleChanges): void {
    if (changes.sortResetToken && !changes.sortResetToken.firstChange) {
      this.sortColumn = 'id';
      this.sortDirection = 'desc';
    }
  }

  get totalRecords(): number {
    return this.sortedPaymentAccounts.length;
  }

  get sortedPaymentAccounts(): PaymentAccount[] {
    const data = [...this.paymentAccounts];
    const direction = this.sortDirection === 'asc' ? 1 : -1;
    return data.sort((a, b) => {
      const left = this.getSortValue(a, this.sortColumn);
      const right = this.getSortValue(b, this.sortColumn);
      if (left < right) return -1 * direction;
      if (left > right) return 1 * direction;
      return 0;
    });
  }

  onEdit(paymentAccount: PaymentAccount): void {
    this.edit.emit(paymentAccount);
  }

  onDelete(paymentAccount: PaymentAccount): void {
    this.remove.emit(paymentAccount);
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

  private getSortValue(paymentAccount: PaymentAccount, column: SortableColumn): number | string {
    switch (column) {
      case 'id':
        return paymentAccount.id ?? 0;
      case 'name':
        return (paymentAccount.name ?? '').toLocaleLowerCase();
      default:
        return 0;
    }
  }
}
