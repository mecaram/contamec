import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';

import { ApplicationFormComponent } from './components/application-form/application-form.component';
import { ApplicationListComponent } from './components/application-list/application-list.component';
import { ApplicationService } from './services/application.service';
import { ApplicationsRoutingModule } from './applications-routing.module';
import { ApplicationsComponent } from './applications.component';

@NgModule({
  declarations: [ApplicationsComponent, ApplicationFormComponent, ApplicationListComponent],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    ApplicationsRoutingModule
  ],
  providers: [ApplicationService]
})
export class ApplicationsModule {}
