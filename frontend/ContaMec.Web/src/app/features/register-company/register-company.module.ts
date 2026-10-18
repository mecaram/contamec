import { CommonModule } from '@angular/common';
import { HttpClientModule } from '@angular/common/http';
import { NgModule } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';

import { RegisterCompanyRoutingModule } from './register-company-routing.module';
import { RegisterCompanyComponent } from './register-company.component';
import { RegisterCompanyService } from './register-company.service';

@NgModule({
  declarations: [RegisterCompanyComponent],
  imports: [CommonModule, HttpClientModule, ReactiveFormsModule, RegisterCompanyRoutingModule],
  providers: [RegisterCompanyService]
})
export class RegisterCompanyModule {}
