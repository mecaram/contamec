import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../auth/auth.service';
import { DashboardCategory } from './dashboard.models';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss']
})
export class DashboardComponent {
  constructor(
    private readonly auth: AuthService,
    private readonly router: Router
  ) {}

  logout(): void {
    this.auth.logout();
    void this.router.navigateByUrl('/login');
  }

  readonly categories: DashboardCategory[] = [
    {
      title: 'Archivos',
      items: [
        { name: 'Cuentas', icon: 'account_balance', route: '/cuentas' },
        { name: 'Cuentas de pago', icon: 'credit_card', route: '/cuentas-pago' },
        { name: 'Usuarios', icon: 'people', route: '/usuarios' }
      ]
    },
    {
      title: 'Proceso Diario',
      items: [
        { name: 'Ingresos', icon: 'trending_up', route: '/ingresos' },
        { name: 'Egresos', icon: 'trending_down', route: '/egresos' },
        { name: 'Cierres', icon: 'point_of_sale', route: '/cierres' }
      ]
    },
    {
      title: 'Informes',
      items: []
    }
  ];
}
