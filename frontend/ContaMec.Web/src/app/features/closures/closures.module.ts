import { CommonModule } from '@angular/common';
import { HttpClientModule } from '@angular/common/http';
import { NgModule } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

import { ClosureListComponent } from './components/closure-list/closure-list.component';
import { ClosuresRoutingModule } from './closures-routing.module';
import { ClosuresComponent } from './closures.component';
import { ClosureBalanceService } from './services/closure-balance.service';
import { ClosureService } from './services/closure.service';

@NgModule({
  declarations: [ClosuresComponent, ClosureListComponent],
  imports: [CommonModule, HttpClientModule, ReactiveFormsModule, MatButtonModule, MatIconModule, ClosuresRoutingModule],
  providers: [ClosureService, ClosureBalanceService]
})
export class ClosuresModule {}
