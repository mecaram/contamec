import { Component, OnInit } from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { Plan } from './models/plan.model';
import { PlanSearch } from './models/plan-search.model';
import { PlanService } from './services/plan.service';

/** Días que suma Activar Plan Demo; el backend usa el mismo valor. */
const DEMO_DAYS = 7;

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

  readonly demoDays = DEMO_DAYS;
  /** Empresa del modal Activar Plan Demo; null si está cerrado. */
  demoTarget: Plan | null = null;
  demoSaving = false;
  demoError: string | null = null;

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
  get demoExtendsCurrent(): boolean {
    const daysLeft = this.demoTarget?.daysLeft;
    return !!this.demoTarget?.expirationDate && daysLeft !== null && daysLeft !== undefined && daysLeft >= 0;
  }

  /** Vencimiento que va a guardar el backend, para mostrarlo antes de confirmar. */
  get demoExpiration(): Date {
    const from = this.demoExtendsCurrent ? this.parseDate(this.demoTarget!.expirationDate!) : new Date();
    from.setDate(from.getDate() + DEMO_DAYS);
    return from;
  }

  openDemoModal(plan: Plan): void {
    this.demoTarget = plan;
    this.demoError = null;
  }

  closeDemoModal(): void {
    if (this.demoSaving) return;
    this.demoTarget = null;
    this.demoError = null;
  }

  confirmDemo(): void {
    if (!this.demoTarget || this.demoSaving) return;
    this.demoSaving = true;
    this.demoError = null;
    this.planService
      .activateDemo(this.demoTarget.applicationId)
      .pipe(finalize(() => (this.demoSaving = false)))
      .subscribe({
        next: () => {
          this.demoTarget = null;
          this.search();
        },
        error: () => {
          this.demoError = 'No se pudo activar el plan Demo.';
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
