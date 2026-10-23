import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { Closure } from '../models/closure.model';
import { ClosureSearch } from '../models/closure-search.model';

export interface ClosureIncomeDetailItem {
  accountId: number;
  accountName?: string | null;
  amount: number;
  percentage: number;
}

export interface ClosureIncomeDetailResponse {
  closureId: number;
  totalIncomes: number;
  items: ClosureIncomeDetailItem[];
}

export interface ClosureExpenseDetailItem {
  accountId: number;
  accountName?: string | null;
  amount: number;
  percentage: number;
}

export interface ClosureExpenseDetailResponse {
  closureId: number;
  totalExpenses: number;
  items: ClosureExpenseDetailItem[];
}

export interface ClosureCloseResponse {
  closedClosureId: number;
  closeDate: string;
  result: number;
  newClosureId: number;
  newOpenDate: string;
}

@Injectable()
export class ClosureService {
  private readonly apiBaseUrl = ((environment as any).apiUrl ?? environment.apiBaseUrl) as string;
  private readonly closuresUrl = `${this.apiBaseUrl}/closures`;
  private readonly tokenKey = 'contamec_token';

  constructor(private readonly http: HttpClient) {}

  search(filters: ClosureSearch): Observable<Closure[]> {
    return this.http.get<Closure[]>(this.closuresUrl, { params: this.buildSearchParams(filters), headers: this.getAuthHeaders() });
  }

  /** Listado con los mismos filtros que la búsqueda, armado por el backend en PDF o Excel. */
  exportList(filters: ClosureSearch, format: 'pdf' | 'excel'): Observable<Blob> {
    return this.http.get(`${this.closuresUrl}/export/${format}`, {
      params: this.buildSearchParams(filters),
      headers: this.getAuthHeaders(),
      responseType: 'blob'
    });
  }

  private buildSearchParams(filters: ClosureSearch): HttpParams {
    let params = new HttpParams();
    if (filters.closureNumber !== null && filters.closureNumber !== undefined) params = params.set('closureNumber', String(filters.closureNumber));
    if (filters.isClosed !== null && filters.isClosed !== undefined) params = params.set('isClosed', String(filters.isClosed));
    if (filters.openDateFrom) params = params.set('openDateFrom', filters.openDateFrom);
    if (filters.openDateTo) params = params.set('openDateTo', filters.openDateTo);
    return params;
  }

  getIncomeDetail(closureId: number): Observable<ClosureIncomeDetailResponse> {
    return this.http.get<ClosureIncomeDetailResponse>(`${this.closuresUrl}/${closureId}/income-detail`, {
      headers: this.getAuthHeaders()
    });
  }

  getExpenseDetail(closureId: number): Observable<ClosureExpenseDetailResponse> {
    return this.http.get<ClosureExpenseDetailResponse>(`${this.closuresUrl}/${closureId}/expense-detail`, {
      headers: this.getAuthHeaders()
    });
  }

  close(closureId: number): Observable<ClosureCloseResponse> {
    return this.http.post<ClosureCloseResponse>(`${this.closuresUrl}/${closureId}/close`, {}, { headers: this.getAuthHeaders() });
  }

  private getAuthHeaders(): HttpHeaders {
    const token = sessionStorage.getItem(this.tokenKey);
    if (!token) return new HttpHeaders();
    return new HttpHeaders({ Authorization: `Bearer ${token}` });
  }
}
