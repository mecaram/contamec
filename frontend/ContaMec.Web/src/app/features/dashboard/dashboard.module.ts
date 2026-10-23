import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';

import { ClosureService } from '../closures/services/closure.service';

import { DashboardRoutingModule } from './dashboard-routing.module';
import { DashboardComponent } from './dashboard.component';
import { DashboardMenuComponent } from './components/dashboard-menu/dashboard-menu.component';

@NgModule({
  declarations: [DashboardComponent, DashboardMenuComponent],
  imports: [
    CommonModule,
    RouterModule,
    DashboardRoutingModule
  ],
  providers: [ClosureService]
})
export class DashboardModule {}
