import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';

import { IncomeFormComponent } from './components/income-form/income-form.component';
import { IncomeListComponent } from './components/income-list/income-list.component';
import { IncomesRoutingModule } from './incomes-routing.module';
import { IncomesComponent } from './incomes.component';
import { IncomeService } from './services/income.service';
import { PaymentAccountService } from '../payment-accounts/services/payment-account.service';

@NgModule({
  declarations: [IncomesComponent, IncomeFormComponent, IncomeListComponent],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatIconModule,
    MatSelectModule,
    IncomesRoutingModule
  ],
  providers: [IncomeService, PaymentAccountService]
})
export class IncomesModule {}
