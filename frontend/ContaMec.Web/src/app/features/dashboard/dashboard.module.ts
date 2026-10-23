import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { ChartsModule } from 'ng2-charts';

import { ClosureService } from '../closures/services/closure.service';

import { DashboardRoutingModule } from './dashboard-routing.module';
import { DashboardComponent } from './dashboard.component';
import { DashboardMenuComponent } from './components/dashboard-menu/dashboard-menu.component';
import { ClosuresChartComponent } from './components/closures-chart/closures-chart.component';

@NgModule({
  declarations: [DashboardComponent, DashboardMenuComponent, ClosuresChartComponent],
  imports: [
    CommonModule,
    RouterModule,
    ChartsModule,
    DashboardRoutingModule
  ],
  providers: [ClosureService]
})
export class DashboardModule {}
