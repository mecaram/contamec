import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { Plan } from '../../models/plan.model';

type SortableColumn = 'applicationId' | 'applicationName' | 'plan' | 'startDate' | 'expirationDate' | 'active';

/** Días antes del vencimiento en que se avisa en ámbar. */
const EXPIRES_SOON_DAYS = 7;

@Component({
  selector: 'app-plan-list',
  templateUrl: './plan-list.component.html',
  styleUrls: ['./plan-list.component.scss']
})
export class PlanListComponent implements OnChanges {
  sortColumn: SortableColumn = 'applicationName';
  sortDirection: 'asc' | 'desc' = 'asc';
  /** En el celular no se ve el encabezado de la tabla: se ordena desde este selector. */
  readonly sortOptions: { column: SortableColumn; label: string }[] = [
    { column: 'applicationId', label: 'ID' },
    { column: 'applicationName', label: 'Empresa' },
    { column: 'plan', label: 'Plan' },
    { column: 'startDate', label: 'Inicio del plan' },
    { column: 'expirationDate', label: 'Vence' },
    { column: 'active', label: 'Activa' }
  ];
  currentPage = 1;
  readonly pageSize = 10;

  @Input() plans: Plan[] = [];
  @Input() sortResetToken = 0;
  @Input() loading = false;

  @Output() activateDemo = new EventEmitter<Plan>();

  ngOnChanges(changes: SimpleChanges): void {
    if (changes.sortResetToken && !changes.sortResetToken.firstChange) {
      this.sortColumn = 'applicationName';
      this.sortDirection = 'asc';
      this.currentPage = 1;
    }
    if (changes.plans && !changes.plans.firstChange) {
      this.ensureValidPage();
    }
  }

  get totalRecords(): number {
    return this.plans.length;
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalRecords / this.pageSize));
  }

  get pagedPlans(): Plan[] {
    const start = (this.currentPage - 1) * this.pageSize;
    return this.sortedPlans.slice(start, start + this.pageSize);
  }

  get pageLabel(): string {
    return `Página ${this.currentPage} de ${this.totalPages}`;
  }

  get canGoPrevious(): boolean {
    return this.currentPage > 1;
  }

  get canGoNext(): boolean {
    return this.currentPage < this.totalPages;
  }

  get sortedPlans(): Plan[] {
    const data = [...this.plans];
    const direction = this.sortDirection === 'asc' ? 1 : -1;
    return data.sort((a, b) => {
      const left = this.getSortValue(a, this.sortColumn);
      const right = this.getSortValue(b, this.sortColumn);
      if (left < right) return -1 * direction;
      if (left > right) return 1 * direction;
      return 0;
    });
  }

  setSort(column: SortableColumn): void {
    if (this.sortColumn === column) {
      this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
      this.currentPage = 1;
      return;
    }
    this.sortColumn = column;
    this.sortDirection = 'asc';
    this.currentPage = 1;
  }

  sortBy(column: SortableColumn): void {
    this.sortColumn = column;
    this.currentPage = 1;
  }

  toggleSortDirection(): void {
    this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    this.currentPage = 1;
  }

  sortIndicator(column: SortableColumn): string {
    if (this.sortColumn !== column) return '';
    return this.sortDirection === 'asc' ? '▲' : '▼';
  }

  formatWhatsApp(whatsApp: string): string {
    return whatsApp.startsWith('549') ? `+54 9 ${whatsApp.slice(3)}` : whatsApp;
  }

  planClass(plan: Plan): string {
    if (plan.plan === 'Demo') return 'plan-list__badge--demo';
    if (plan.plan === 'Pro+') return 'plan-list__badge--pro';
    return '';
  }

  daysLabel(daysLeft: number): string {
    if (daysLeft < 0) return 'Vencido';
    if (daysLeft === 0) return 'Vence hoy';
    return daysLeft === 1 ? 'Falta 1 día' : `Faltan ${daysLeft} días`;
  }

  daysClass(daysLeft: number): string {
    if (daysLeft < 0) return 'plan-list__days--expired';
    return daysLeft <= EXPIRES_SOON_DAYS ? 'plan-list__days--soon' : '';
  }

  goToPreviousPage(): void {
    if (!this.canGoPrevious) return;
    this.currentPage -= 1;
  }

  goToNextPage(): void {
    if (!this.canGoNext) return;
    this.currentPage += 1;
  }

  private getSortValue(plan: Plan, column: SortableColumn): number | string {
    switch (column) {
      case 'applicationId':
        return plan.applicationId ?? 0;
      case 'applicationName':
        return (plan.applicationName ?? '').toLocaleLowerCase();
      case 'plan':
        return plan.plan ?? '';
      case 'startDate':
        return plan.startDate ? new Date(plan.startDate).getTime() : 0;
      case 'expirationDate':
        // Sin vencimiento va al final en orden ascendente.
        return plan.expirationDate ? new Date(plan.expirationDate).getTime() : Number.MAX_SAFE_INTEGER;
      case 'active':
        return plan.active ? 1 : 0;
      default:
        return 0;
    }
  }

  private ensureValidPage(): void {
    if (this.currentPage > this.totalPages) {
      this.currentPage = this.totalPages;
    }
    if (this.currentPage < 1) {
      this.currentPage = 1;
    }
  }
}
