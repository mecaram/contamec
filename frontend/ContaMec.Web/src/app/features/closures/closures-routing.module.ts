import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ClosuresComponent } from './closures.component';

const routes: Routes = [{ path: '', component: ClosuresComponent }];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class ClosuresRoutingModule {}
