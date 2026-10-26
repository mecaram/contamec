import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';

import { AmountCalculatorModule } from '../../shared/amount-calculator/amount-calculator.module';
import { VoucherModule } from '../../shared/voucher/voucher.module';
import { IncomeFormComponent } from './components/income-form/income-form.component';
import { IncomeListComponent } from './components/income-list/income-list.component';
import { IncomesRoutingModule } from './incomes-routing.module';
import { IncomesComponent } from './incomes.component';
import { IncomeService } from './services/income.service';
import { PaymentAccountService } from '../payment-accounts/services/payment-account.service';
import { SubAccountService } from '../sub-accounts/services/sub-account.service';

@NgModule({
  declarations: [IncomesComponent, IncomeFormComponent, IncomeListComponent],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatTooltipModule,
    AmountCalculatorModule,
    VoucherModule,
    IncomesRoutingModule
  ],
  providers: [IncomeService, PaymentAccountService, SubAccountService]
})
export class IncomesModule {}
