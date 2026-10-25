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
  readonly planName = this.auth.getPlanName();
  /** Último día del plan, como fecha local sin hora; null si no vence. */
  readonly planExpiresOn = this.parseDate(this.auth.getPlanExpirationDate());
  /** Días que faltan hasta el último día del plan: 0 si vence hoy, negativo si ya venció (solo el SuperAdministrador entra igual). */
  readonly planDaysLeft = this.planExpiresOn ? this.daysFromToday(this.planExpiresOn) : null;
  readonly username = this.auth.getUsername();
  readonly previousLoginAt = this.auth.getPreviousLoginAt();

  /** En el celular las opciones del dashboard también se ofrecen en un menú lateral. */
  readonly showMenu = isPhone();
  menuOpen = false;

  /** Liquidación activa: el cierre abierto de la empresa, con sus totales calculados por el backend. */
  activeClosure: Closure | null = null;
  /** Todos los cierres de la empresa (con sus totales), para los gráficos de Estadísticas. */
  closures: Closure[] = [];
  indicatorsLoading = true;
  indicatorsError: string | null = null;

  constructor(
    private readonly auth: AuthService,
    private readonly router: Router,
    private readonly closureService: ClosureService
  ) {}

  ngOnInit(): void {
    // Una sola consulta alimenta los indicadores (cierre abierto) y las estadísticas (historial).
    this.closureService.search({}).subscribe({
      next: (closures) => {
        this.closures = closures;
        this.activeClosure = closures.find((closure) => !closure.isClosed) ?? null;
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

  /** Grupos de tarjetas contraídos. Al abrir el dashboard están todos expandidos. */
  private readonly collapsedSections = new Set<string>();

  isCollapsed(section: string): boolean {
    return this.collapsedSections.has(section);
  }

  toggleSection(section: string): void {
    if (!this.collapsedSections.delete(section)) this.collapsedSections.add(section);
  }

  logout(): void {
    this.auth.logout();
    void this.router.navigateByUrl('/login');
  }

  private parseDate(value: string | null): Date | null {
    const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value ?? '');
    return match ? new Date(Number(match[1]), Number(match[2]) - 1, Number(match[3])) : null;
  }

  private daysFromToday(date: Date): number {
    const now = new Date();
    const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());
    // Redondeo por los días de 23 o 25 horas en los cambios de horario.
    return Math.round((date.getTime() - today.getTime()) / 86_400_000);
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
        { name: 'Planes', icon: 'card_membership', route: '/planes', superAdminOnly: true },
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
