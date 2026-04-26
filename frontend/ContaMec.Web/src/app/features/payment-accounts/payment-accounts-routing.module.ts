import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { PaymentAccountsComponent } from './payment-accounts.component';

const routes: Routes = [{ path: '', component: PaymentAccountsComponent }];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class PaymentAccountsRoutingModule {}
