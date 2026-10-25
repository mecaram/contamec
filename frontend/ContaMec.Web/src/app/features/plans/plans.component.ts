import { Component, OnInit } from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { Plan, PlanActivationKind } from './models/plan.model';
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
