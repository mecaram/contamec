import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { Income } from '../models/income.model';
import { IncomeSearch } from '../models/income-search.model';

export interface ClosureLookup {
  id: number;
  isClosed?: boolean | number | null;
}

export interface IncomeAccountOption {
  id: number;
  name?: string | null;
}

@Injectable()
export class IncomeService {
  private readonly apiBaseUrl = ((environment as any).apiUrl ?? environment.apiBaseUrl) as string;
  private readonly incomesUrl = `${this.apiBaseUrl}/incomes`;
  private readonly tokenKey = 'contamec_token';

  constructor(private readonly http: HttpClient) {}

  search(filters: IncomeSearch): Observable<Income[]> {
    let params = new HttpParams();

    if (filters.dateFrom) params = params.set('dateFrom', filters.dateFrom);
    if (filters.dateTo) params = params.set('dateTo', filters.dateTo);
    if (filters.closureId !== null && filters.closureId !== undefined) {
      params = params.set('closureId', String(filters.closureId));
    }
    if (filters.accountId !== null && filters.accountId !== undefined) {
      params = params.set('accountId', String(filters.accountId));
    }
    if (filters.amountFrom !== null && filters.amountFrom !== undefined) {
      params = params.set('amountFrom', String(filters.amountFrom));
    }
    if (filters.amountTo !== null && filters.amountTo !== undefined) {
      params = params.set('amountTo', String(filters.amountTo));
    }
    if (filters.detail) params = params.set('detail', filters.detail);

    return this.http.get<Income[]>(this.incomesUrl, { params, headers: this.getAuthHeaders() });
  }

  getClosures(): Observable<ClosureLookup[]> {
    return this.http.get<ClosureLookup[]>(`${this.apiBaseUrl}/closures`, { headers: this.getAuthHeaders() });
  }

  getIncomeAccounts(): Observable<IncomeAccountOption[]> {
    return this.http.get<IncomeAccountOption[]>(`${this.apiBaseUrl}/accounts/income-options`, { headers: this.getAuthHeaders() });
  }

  getById(id: number): Observable<Income> {
    return this.http.get<Income>(`${this.incomesUrl}/${id}`, { headers: this.getAuthHeaders() });
  }

  create(income: Income): Observable<Income> {
    const payload = {
      emissionDate: income.emissionDate ?? null,
      accountId: income.accountId ?? null,
      detail: income.detail ?? null,
      amount: income.amount ?? null
    };
    return this.http.post<Income>(this.incomesUrl, payload, { headers: this.getAuthHeaders() });
  }

  update(id: number, income: Income): Observable<void> {
    const payload = {
      emissionDate: income.emissionDate ?? null,
      accountId: income.accountId ?? null,
      detail: income.detail ?? null,
      amount: income.amount ?? null
    };
    return this.http.put<void>(`${this.incomesUrl}/${id}`, payload, { headers: this.getAuthHeaders() });
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.incomesUrl}/${id}`, { headers: this.getAuthHeaders() });
  }

  private getAuthHeaders(): HttpHeaders {
    const token = sessionStorage.getItem(this.tokenKey);
    if (!token) return new HttpHeaders();
    return new HttpHeaders({ Authorization: `Bearer ${token}` });
  }
}
