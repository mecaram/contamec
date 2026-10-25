import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { SuperAdminGuard } from '../../auth/super-admin.guard';
import { PlansComponent } from './plans.component';

const routes: Routes = [{ path: '', component: PlansComponent, canActivate: [SuperAdminGuard] }];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class PlansRoutingModule {}
