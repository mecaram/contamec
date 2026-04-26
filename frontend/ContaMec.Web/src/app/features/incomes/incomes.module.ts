import { CommonModule } from '@angular/common';
import { HttpClientModule } from '@angular/common/http';
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

@NgModule({
  declarations: [IncomesComponent, IncomeFormComponent, IncomeListComponent],
  imports: [
    CommonModule,
    HttpClientModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatIconModule,
    MatSelectModule,
    IncomesRoutingModule
  ],
  providers: [IncomeService]
})
export class IncomesModule {}
