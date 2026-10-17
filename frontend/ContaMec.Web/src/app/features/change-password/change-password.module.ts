import { CommonModule } from '@angular/common';
import { HttpClientModule } from '@angular/common/http';
import { NgModule } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

import { ChangePasswordRoutingModule } from './change-password-routing.module';
import { ChangePasswordComponent } from './change-password.component';
import { ChangePasswordService } from './change-password.service';

@NgModule({
  declarations: [ChangePasswordComponent],
  imports: [CommonModule, HttpClientModule, ReactiveFormsModule, MatButtonModule, MatIconModule, ChangePasswordRoutingModule],
  providers: [ChangePasswordService]
})
export class ChangePasswordModule {}
