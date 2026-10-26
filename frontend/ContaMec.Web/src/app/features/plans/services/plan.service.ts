import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { Plan, PlanActivationKind } from '../models/plan.model';
import { PlanHistory } from '../models/plan-history.model';
import { PlanSearch } from '../models/plan-search.model';

@Injectable()
export class PlanService {
  private readonly apiBaseUrl = ((environment as any).apiUrl ?? environment.apiBaseUrl) as string;
  private readonly plansUrl = `${this.apiBaseUrl}/plans`;
  private readonly tokenKey = 'contamec_token';

  constructor(private readonly http: HttpClient) {}

  search(filters: PlanSearch): Observable<Plan[]> {
    return this.http.get<Plan[]>(this.plansUrl, { params: this.buildSearchParams(filters), headers: this.getAuthHeaders() });
  }

  getLocalities(): Observable<string[]> {
    return this.http.get<string[]>(`${this.plansUrl}/localities`, { headers: this.getAuthHeaders() });
  }

  getHistory(applicationId: number): Observable<PlanHistory[]> {
    return this.http.get<PlanHistory[]>(`${this.plansUrl}/${applicationId}/history`, { headers: this.getAuthHeaders() });
  }

  deleteLastPlan(applicationId: number): Observable<Plan> {
    return this.http.delete<Plan>(`${this.plansUrl}/${applicationId}/last`, { headers: this.getAuthHeaders() });
  }

  activatePlan(applicationId: number, kind: PlanActivationKind): Observable<Plan> {
    const segment = kind === 'proPlus' ? 'pro-plus' : 'demo';
    return this.http.post<Plan>(`${this.plansUrl}/${applicationId}/${segment}`, null, { headers: this.getAuthHeaders() });
  }

  private buildSearchParams(filters: PlanSearch): HttpParams {
    let params = new HttpParams();
    if (filters.applicationId !== null && filters.applicationId !== undefined) params = params.set('applicationId', String(filters.applicationId));
    if (filters.applicationName) params = params.set('applicationName', filters.applicationName);
    if (filters.locality) params = params.set('locality', filters.locality);
    if (filters.active !== null && filters.active !== undefined) params = params.set('active', String(filters.active));
    if (filters.plan) params = params.set('plan', filters.plan);
    if (filters.expiration) params = params.set('expiration', filters.expiration);
    if (filters.expirationFrom) params = params.set('expirationFrom', filters.expirationFrom);
    if (filters.expirationTo) params = params.set('expirationTo', filters.expirationTo);
    return params;
  }

  private getAuthHeaders(): HttpHeaders {
    const token = localStorage.getItem(this.tokenKey);
    if (!token) return new HttpHeaders();
    return new HttpHeaders({ Authorization: `Bearer ${token}` });
  }
}
