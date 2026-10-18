import { CommonModule } from '@angular/common';
import { HttpClientModule } from '@angular/common/http';
import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';

import { ClosureService } from '../closures/services/closure.service';

import { DashboardRoutingModule } from './dashboard-routing.module';
import { DashboardComponent } from './dashboard.component';

@NgModule({
  declarations: [DashboardComponent],
  imports: [
    CommonModule,
    HttpClientModule,
    RouterModule,
    DashboardRoutingModule
  ],
  providers: [ClosureService]
})
export class DashboardModule {}
