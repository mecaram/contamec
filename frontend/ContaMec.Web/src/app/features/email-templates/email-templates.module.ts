import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';

import { EmailTemplateFormComponent } from './components/email-template-form/email-template-form.component';
import { EmailTemplateListComponent } from './components/email-template-list/email-template-list.component';
import { EmailTemplatesRoutingModule } from './email-templates-routing.module';
import { EmailTemplatesComponent } from './email-templates.component';
import { EmailTemplateService } from './services/email-template.service';

@NgModule({
  declarations: [EmailTemplatesComponent, EmailTemplateFormComponent, EmailTemplateListComponent],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    EmailTemplatesRoutingModule
  ],
  providers: [EmailTemplateService]
})
export class EmailTemplatesModule {}
