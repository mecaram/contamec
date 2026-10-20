import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

import { AccountService } from '../accounts/services/account.service';
import { SubAccountFormComponent } from './components/sub-account-form/sub-account-form.component';
import { SubAccountListComponent } from './components/sub-account-list/sub-account-list.component';
import { SubAccountsRoutingModule } from './sub-accounts-routing.module';
import { SubAccountsComponent } from './sub-accounts.component';
import { SubAccountService } from './services/sub-account.service';

@NgModule({
  declarations: [SubAccountsComponent, SubAccountFormComponent, SubAccountListComponent],
  imports: [CommonModule, ReactiveFormsModule, MatButtonModule, MatIconModule, SubAccountsRoutingModule],
  providers: [SubAccountService, AccountService]
})
export class SubAccountsModule {}
