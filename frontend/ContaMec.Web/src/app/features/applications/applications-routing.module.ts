import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { SuperAdminGuard } from '../../auth/super-admin.guard';
import { ApplicationsComponent } from './applications.component';

const routes: Routes = [{ path: '', component: ApplicationsComponent, canActivate: [SuperAdminGuard] }];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class ApplicationsRoutingModule {}
