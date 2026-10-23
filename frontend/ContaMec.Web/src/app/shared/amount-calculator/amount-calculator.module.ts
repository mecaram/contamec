import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

import { AmountCalculatorComponent } from './amount-calculator.component';

@NgModule({
  declarations: [AmountCalculatorComponent],
  imports: [CommonModule, MatIconModule],
  exports: [AmountCalculatorComponent]
})
export class AmountCalculatorModule {}
