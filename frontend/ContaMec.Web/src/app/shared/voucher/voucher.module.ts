import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';

import { VoucherUploadComponent } from './voucher-upload.component';
import { VoucherViewerComponent } from './voucher-viewer.component';

@NgModule({
  declarations: [VoucherUploadComponent, VoucherViewerComponent],
  imports: [CommonModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule, MatTooltipModule],
  exports: [VoucherUploadComponent, VoucherViewerComponent]
})
export class VoucherModule {}
