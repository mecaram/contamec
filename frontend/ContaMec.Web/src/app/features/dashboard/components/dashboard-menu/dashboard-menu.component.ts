import { Component, EventEmitter, HostListener, Input, Output } from '@angular/core';
import { DashboardCategory } from '../../dashboard.models';

/** Menú lateral del dashboard en el celular: las mismas opciones que las tarjetas, más las del usuario. */
@Component({
  selector: 'app-dashboard-menu',
  templateUrl: './dashboard-menu.component.html',
  styleUrls: ['./dashboard-menu.component.scss']
})
export class DashboardMenuComponent {
  @Input() categories: DashboardCategory[] = [];
  @Input() open = false;
  @Output() readonly closed = new EventEmitter<void>();
  @Output() readonly logout = new EventEmitter<void>();

  @HostListener('document:keydown.escape')
  close(): void {
    if (this.open) this.closed.emit();
  }
}
