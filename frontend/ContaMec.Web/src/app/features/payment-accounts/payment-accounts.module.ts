import { CommonModule } from '@angular/common';
import { HttpClientModule } from '@angular/common/http';
import { NgModule } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

import { PaymentAccountFormComponent } from './components/payment-account-form/payment-account-form.component';
import { PaymentAccountListComponent } from './components/payment-account-list/payment-account-list.component';
import { PaymentAccountsRoutingModule } from './payment-accounts-routing.module';
import { PaymentAccountsComponent } from './payment-accounts.component';
import { PaymentAccountService } from './services/payment-account.service';

@NgModule({
  declarations: [PaymentAccountsComponent, PaymentAccountFormComponent, PaymentAccountListComponent],
  imports: [CommonModule, HttpClientModule, ReactiveFormsModule, MatButtonModule, MatIconModule, PaymentAccountsRoutingModule],
  providers: [PaymentAccountService]
})
export class PaymentAccountsModule {}
