import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { SubAccountsComponent } from './sub-accounts.component';

const routes: Routes = [{ path: '', component: SubAccountsComponent }];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class SubAccountsRoutingModule {}
