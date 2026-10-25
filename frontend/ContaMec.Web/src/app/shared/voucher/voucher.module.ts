import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { VoucherUploadComponent } from './voucher-upload.component';

@NgModule({
  declarations: [VoucherUploadComponent],
  imports: [CommonModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  exports: [VoucherUploadComponent]
})
export class VoucherModule {}
