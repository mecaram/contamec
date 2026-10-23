import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';

import { AccountFormComponent } from './components/account-form/account-form.component';
import { AccountListComponent } from './components/account-list/account-list.component';
import { AccountsRoutingModule } from './accounts-routing.module';
import { AccountsComponent } from './accounts.component';
import { AccountService } from './services/account.service';

@NgModule({
  declarations: [AccountsComponent, AccountFormComponent, AccountListComponent],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    AccountsRoutingModule
  ],
  providers: [AccountService]
})
export class AccountsModule {}
