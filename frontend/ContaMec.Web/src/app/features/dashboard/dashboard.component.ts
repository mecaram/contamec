import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../auth/auth.service';
import { isPhone } from '../../shared/amount-calculator/device';
import { Closure } from '../closures/models/closure.model';
import { ClosureService } from '../closures/services/closure.service';
import { DashboardCategory } from './dashboard.models';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss']
})
export class DashboardComponent implements OnInit {
  readonly applicationName = this.auth.getApplicationName();
  readonly username = this.auth.getUsername();
  readonly previousLoginAt = this.auth.getPreviousLoginAt();

  /** En el celular las opciones del dashboard también se ofrecen en un menú lateral. */
  readonly showMenu = isPhone();
  menuOpen = false;

  /** Liquidación activa: el cierre abierto de la empresa, con sus totales calculados por el backend. */
  activeClosure: Closure | null = null;
  indicatorsLoading = true;
  indicatorsError: string | null = null;

  /** Últimos cierres, del más viejo al más nuevo, para comparar ingresos y egresos entre cierres. */
  recentClosures: Closure[] = [];
  private readonly chartClosureCount = 12;

  constructor(
    private readonly auth: AuthService,
    private readonly router: Router,
    private readonly closureService: ClosureService
  ) {}

  ngOnInit(): void {
    // El backend devuelve los cierres del más nuevo al más viejo; una sola consulta alimenta los indicadores y el gráfico.
    this.closureService.search({}).subscribe({
      next: (closures) => {
        this.activeClosure = closures.find((closure) => !closure.isClosed) ?? null;
        this.recentClosures = closures.slice(0, this.chartClosureCount).reverse();
        this.indicatorsLoading = false;
      },
      error: () => {
        this.indicatorsError = 'No se pudieron cargar los indicadores.';
        this.indicatorsLoading = false;
      }
    });
  }

  toggleMenu(): void {
    this.menuOpen = !this.menuOpen;
  }

  closeMenu(): void {
    this.menuOpen = false;
  }

  logout(): void {
    this.auth.logout();
    void this.router.navigateByUrl('/login');
  }

  private readonly isSuperAdministrator = this.auth.isSuperAdministrator();

  readonly categories: DashboardCategory[] = ([
    {
      title: 'Proceso Diario',
      tone: 'daily',
      items: [
        { name: 'Ingresos', icon: 'trending_up', route: '/ingresos', iconTone: 'incomes' },
        { name: 'Egresos', icon: 'trending_down', route: '/egresos', iconTone: 'expenses' },
        { name: 'Cierres', icon: 'point_of_sale', route: '/cierres' }
      ]
    },
    {
      title: 'Archivos',
      tone: 'files',
      items: [
        { name: 'Cuentas', icon: 'account_balance', route: '/cuentas' },
        { name: 'Subcuentas', icon: 'account_tree', route: '/subcuentas' },
        { name: 'Cuentas de pago', icon: 'credit_card', route: '/cuentas-pago' },
        { name: 'Usuarios', icon: 'people', route: '/usuarios' },
        { name: 'Aplicaciones', icon: 'business', route: '/aplicaciones', superAdminOnly: true },
        { name: 'Plantillas de correo', icon: 'mail', route: '/plantillas-correo', superAdminOnly: true }
      ]
    },
    {
      title: 'Informes',
      tone: 'reports',
      items: [],
      hidden: true
    }
  ] as DashboardCategory[])
    .filter((category) => !category.hidden)
    .map((category) => ({
      ...category,
      items: category.items.filter((item) => !item.superAdminOnly || this.isSuperAdministrator)
    }));
}
