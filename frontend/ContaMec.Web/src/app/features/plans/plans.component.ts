import { Component, OnInit } from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { Plan } from './models/plan.model';
import { PlanSearch } from './models/plan-search.model';
import { PlanService } from './services/plan.service';

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
