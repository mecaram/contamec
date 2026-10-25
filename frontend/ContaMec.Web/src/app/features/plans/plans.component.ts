import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { Subscription } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { Plan, PlanActivationKind } from './models/plan.model';
import { PlanHistory } from './models/plan-history.model';
import { planDaysLabel, planDaysTone } from './plan-days';
import { PlanSearch } from './models/plan-search.model';
import { PlanService } from './services/plan.service';

/** Nombre y días que suma cada acción Activar Plan; el backend usa los mismos días. */
const PLAN_ACTIVATIONS: Record<PlanActivationKind, { label: string; days: number }> = {
  demo: { label: 'Demo', days: 7 },
  proPlus: { label: 'Pro+', days: 30 }
};

@Component({
  selector: 'app-plans',
  templateUrl: './plans.component.html',
  styleUrls: ['./plans.component.scss']
})
export class PlansComponent implements OnInit {
  plans: Plan[] = [];
  loading = false;
  errorMessage: string | null = null;
  sortResetToken = 0;
  /** Opciones del filtro Localidad: las localidades distintas de las empresas. */
  localities: string[] = [];

  /** Empresa del modal Activar Plan; null si está cerrado. */
  activationTarget: Plan | null = null;
  activationKind: PlanActivationKind = 'demo';
  activationSaving = false;
  activationError: string | null = null;

  /** Empresa del modal Eliminar último plan; null si está cerrado. */
  deleteTarget: Plan | null = null;
  deleteSaving = false;
  deleteError: string | null = null;

  /** Empresa del modal Historial de planes; null si está cerrado. */
  historyTarget: Plan | null = null;
  historyItems: PlanHistory[] = [];
  historyLoading = false;
  historyError: string | null = null;
  private historySubscription?: Subscription;

  readonly filterForm = this.fb.group({
    applicationId: [null as number | null],
    applicationName: [''],
    locality: [''],
    plan: [''],
    active: [''],
    expiration: ['']
  });

  constructor(
    private readonly fb: FormBuilder,
    private readonly planService: PlanService
  ) {}

  ngOnInit(): void {
    this.search();
    // Si no cargan, el combo queda solo con "Todas" y el listado funciona igual.
    this.planService.getLocalities().subscribe({
      next: (localities) => (this.localities = localities ?? []),
      error: () => (this.localities = [])
    });
  }

  search(): void {
    this.loading = true;
    this.errorMessage = null;
    this.planService
      .search(this.buildFilters())
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (items) => {
          this.plans = items ?? [];
        },
        error: () => {
          this.errorMessage = 'No se pudieron consultar los planes.';
          this.plans = [];
        }
      });
  }

  clearFilters(): void {
    this.filterForm.reset({
      applicationId: null,
      applicationName: '',
      locality: '',
      plan: '',
      active: '',
      expiration: ''
    });
    this.sortResetToken += 1;
    this.search();
  }

  /** El plan vigente (vence hoy o después) se extiende desde su vencimiento; sin vencimiento o vencido, desde hoy. */
  get activationExtendsCurrent(): boolean {
    const daysLeft = this.activationTarget?.daysLeft;
    return !!this.activationTarget?.expirationDate && daysLeft !== null && daysLeft !== undefined && daysLeft >= 0;
  }

  get activationLabel(): string {
    return PLAN_ACTIVATIONS[this.activationKind].label;
  }

  get activationDays(): number {
    return PLAN_ACTIVATIONS[this.activationKind].days;
  }

  /** Vencimiento que va a guardar el backend, para mostrarlo antes de confirmar. */
  get activationExpiration(): Date {
    const from = this.activationExtendsCurrent ? this.parseDate(this.activationTarget!.expirationDate!) : new Date();
    from.setDate(from.getDate() + this.activationDays);
    return from;
  }

  openActivationModal(event: { plan: Plan; kind: PlanActivationKind }): void {
    this.activationTarget = event.plan;
    this.activationKind = event.kind;
    this.activationError = null;
  }

  closeActivationModal(): void {
    if (this.activationSaving) return;
    this.activationTarget = null;
    this.activationError = null;
  }

  confirmActivation(): void {
    if (!this.activationTarget || this.activationSaving) return;
    this.activationSaving = true;
    this.activationError = null;
    this.planService
      .activatePlan(this.activationTarget.applicationId, this.activationKind)
      .pipe(finalize(() => (this.activationSaving = false)))
      .subscribe({
        next: () => {
          this.activationTarget = null;
          this.search();
        },
        error: () => {
          this.activationError = `No se pudo activar el plan ${this.activationLabel}.`;
        }
      });
  }

  openDeleteModal(plan: Plan): void {
    this.deleteTarget = plan;
    this.deleteError = null;
  }

  closeDeleteModal(): void {
    if (this.deleteSaving) return;
    this.deleteTarget = null;
    this.deleteError = null;
  }

  confirmDeleteLastPlan(): void {
    if (!this.deleteTarget || this.deleteSaving) return;
    this.deleteSaving = true;
    this.deleteError = null;
    this.planService
      .deleteLastPlan(this.deleteTarget.applicationId)
      .pipe(finalize(() => (this.deleteSaving = false)))
      .subscribe({
        next: () => {
          this.deleteTarget = null;
          this.search();
        },
        error: (err: HttpErrorResponse) => {
          // Las reglas las valida el backend: se muestra su mensaje (por ejemplo, que no hay planes registrados).
          this.deleteError = err?.error?.message || 'No se pudo eliminar el último plan.';
        }
      });
  }

  daysLabel(daysLeft: number): string {
    return planDaysLabel(daysLeft);
  }

  daysTone(daysLeft: number): string {
    return planDaysTone(daysLeft);
  }

  openHistory(plan: Plan): void {
    this.historySubscription?.unsubscribe();
    this.historyTarget = plan;
    this.historyItems = [];
    this.historyError = null;
    this.historyLoading = true;
    this.historySubscription = this.planService
      .getHistory(plan.applicationId)
      .pipe(finalize(() => (this.historyLoading = false)))
      .subscribe({
        next: (items) => (this.historyItems = items ?? []),
        error: () => (this.historyError = 'No se pudo consultar el historial de planes.')
      });
  }

  closeHistory(): void {
    // Si la consulta sigue en curso, su respuesta ya no se muestra.
    this.historySubscription?.unsubscribe();
    this.historyTarget = null;
    this.historyItems = [];
    this.historyError = null;
    this.historyLoading = false;
  }

  /** Solo la parte de fecha: así no se corre de día por la zona horaria. */
  private parseDate(value: string): Date {
    const [year, month, day] = value.slice(0, 10).split('-').map(Number);
    return new Date(year, month - 1, day);
  }

  private buildFilters(): PlanSearch {
    const raw = this.filterForm.getRawValue();
    let active: boolean | null = null;
    if (raw.active === 'true') active = true;
    else if (raw.active === 'false') active = false;

    return {
      applicationId: raw.applicationId,
      applicationName: raw.applicationName?.trim() || null,
      locality: raw.locality || null,
      active,
      plan: raw.plan || null,
      expiration: raw.expiration || null
    };
  }
}
