import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';

import { ExpenseFormComponent } from './components/expense-form/expense-form.component';
import { ExpenseListComponent } from './components/expense-list/expense-list.component';
import { ExpensesRoutingModule } from './expenses-routing.module';
import { ExpensesComponent } from './expenses.component';
import { ExpenseService } from './services/expense.service';
import { PaymentAccountService } from '../payment-accounts/services/payment-account.service';

@NgModule({
  declarations: [ExpensesComponent, ExpenseFormComponent, ExpenseListComponent],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatIconModule,
    MatSelectModule,
    ExpensesRoutingModule
  ],
  providers: [ExpenseService, PaymentAccountService]
})
export class ExpensesModule {}
