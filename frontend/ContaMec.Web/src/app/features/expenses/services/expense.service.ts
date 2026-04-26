import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ExpenseSearch } from '../models/expense-search.model';
import { Expense } from '../models/expense.model';

export interface ClosureLookup {
  id: number;
  isClosed?: boolean | number | null;
}

export interface ExpenseAccountOption {
  id: number;
  name?: string | null;
}

@Injectable()
export class ExpenseService {
  private readonly apiBaseUrl = ((environment as any).apiUrl ?? environment.apiBaseUrl) as string;
  private readonly expensesUrl = `${this.apiBaseUrl}/expenses`;
  private readonly tokenKey = 'contamec_token';

  constructor(private readonly http: HttpClient) {}

  search(filters: ExpenseSearch): Observable<Expense[]> {
    let params = new HttpParams();

    if (filters.dateFrom) params = params.set('dateFrom', filters.dateFrom);
    if (filters.dateTo) params = params.set('dateTo', filters.dateTo);
    if (filters.closureId !== null && filters.closureId !== undefined) params = params.set('closureId', String(filters.closureId));
    if (filters.accountId !== null && filters.accountId !== undefined) params = params.set('accountId', String(filters.accountId));
    if (filters.amountFrom !== null && filters.amountFrom !== undefined) params = params.set('amountFrom', String(filters.amountFrom));
    if (filters.amountTo !== null && filters.amountTo !== undefined) params = params.set('amountTo', String(filters.amountTo));
    if (filters.detail) params = params.set('detail', filters.detail);

    return this.http.get<Expense[]>(this.expensesUrl, { params, headers: this.getAuthHeaders() });
  }

  getClosures(): Observable<ClosureLookup[]> {
    return this.http.get<ClosureLookup[]>(`${this.apiBaseUrl}/closures`, { headers: this.getAuthHeaders() });
  }

  getExpenseAccounts(): Observable<ExpenseAccountOption[]> {
    return this.http.get<ExpenseAccountOption[]>(`${this.apiBaseUrl}/accounts/expense-options`, { headers: this.getAuthHeaders() });
  }

  create(expense: Expense): Observable<Expense> {
    const payload = {
      emissionDate: expense.emissionDate ?? null,
      accountId: expense.accountId ?? null,
      detail: expense.detail ?? null,
      amount: expense.amount ?? null
    };
    return this.http.post<Expense>(this.expensesUrl, payload, { headers: this.getAuthHeaders() });
  }

  update(id: number, expense: Expense): Observable<void> {
    const payload = {
      emissionDate: expense.emissionDate ?? null,
      accountId: expense.accountId ?? null,
      detail: expense.detail ?? null,
      amount: expense.amount ?? null
    };
    return this.http.put<void>(`${this.expensesUrl}/${id}`, payload, { headers: this.getAuthHeaders() });
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.expensesUrl}/${id}`, { headers: this.getAuthHeaders() });
  }

  private getAuthHeaders(): HttpHeaders {
    const token = sessionStorage.getItem(this.tokenKey);
    if (!token) return new HttpHeaders();
    return new HttpHeaders({ Authorization: `Bearer ${token}` });
  }
}
