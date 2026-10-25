import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

import { PlanListComponent } from './components/plan-list/plan-list.component';
import { PlanService } from './services/plan.service';
import { PlansRoutingModule } from './plans-routing.module';
import { PlansComponent } from './plans.component';

@NgModule({
  declarations: [PlansComponent, PlanListComponent],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatIconModule,
    PlansRoutingModule
  ],
  providers: [PlanService]
})
export class PlansModule {}
