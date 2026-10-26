import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { VoucherApi, VoucherInfo, VoucherUrl } from '../../../shared/voucher/voucher-api';
import { ExpenseSearch } from '../models/expense-search.model';
import { Expense } from '../models/expense.model';

export interface ClosureLookup {
  id: number;
  isClosed?: boolean | number | null;
  openDate?: string | null;
  closureNumber?: number | null;
}

export interface ExpenseAccountOption {
  id: number;
  name?: string | null;
  subAccountRequires?: boolean;
}

@Injectable()
export class ExpenseService {
  private readonly apiBaseUrl = ((environment as any).apiUrl ?? environment.apiBaseUrl) as string;
  private readonly expensesUrl = `${this.apiBaseUrl}/expenses`;
  private readonly tokenKey = 'contamec_token';

  constructor(private readonly http: HttpClient) {}

  search(filters: ExpenseSearch): Observable<Expense[]> {
    return this.http.get<Expense[]>(this.expensesUrl, { params: this.buildSearchParams(filters), headers: this.getAuthHeaders() });
  }

  /** Listado con los mismos filtros que la búsqueda, armado por el backend en PDF o Excel. */
  exportList(filters: ExpenseSearch, format: 'pdf' | 'excel'): Observable<Blob> {
    return this.http.get(`${this.expensesUrl}/export/${format}`, {
      params: this.buildSearchParams(filters),
      headers: this.getAuthHeaders(),
      responseType: 'blob'
    });
  }

  private buildSearchParams(filters: ExpenseSearch): HttpParams {
    let params = new HttpParams();

    if (filters.dateFrom) params = params.set('dateFrom', filters.dateFrom);
    if (filters.dateTo) params = params.set('dateTo', filters.dateTo);
    if (filters.closureId !== null && filters.closureId !== undefined) params = params.set('closureId', String(filters.closureId));
    if (filters.accountId !== null && filters.accountId !== undefined) params = params.set('accountId', String(filters.accountId));
    if (filters.paymentAccountId !== null && filters.paymentAccountId !== undefined) params = params.set('paymentAccountId', String(filters.paymentAccountId));
    if (filters.subAccountId !== null && filters.subAccountId !== undefined) params = params.set('subAccountId', String(filters.subAccountId));
    if (filters.amountFrom !== null && filters.amountFrom !== undefined) params = params.set('amountFrom', String(filters.amountFrom));
    if (filters.amountTo !== null && filters.amountTo !== undefined) params = params.set('amountTo', String(filters.amountTo));
    if (filters.detail) params = params.set('detail', filters.detail);
    return params;
  }

  getClosures(): Observable<ClosureLookup[]> {
    return this.http.get<ClosureLookup[]>(`${this.apiBaseUrl}/closures`, { headers: this.getAuthHeaders() });
  }

  /** Cuenta, subcuenta y cuenta de pago de los últimos egresos que registró el usuario; null si todavía no registró ninguno. */
  getDefaults(): Observable<{ accountId: number | null; subAccountId: number | null; paymentAccountId: number | null }> {
    return this.http.get<{ accountId: number | null; subAccountId: number | null; paymentAccountId: number | null }>(`${this.expensesUrl}/last-payment-account`, { headers: this.getAuthHeaders() });
  }

  getExpenseAccounts(): Observable<ExpenseAccountOption[]> {
    return this.http.get<ExpenseAccountOption[]>(`${this.apiBaseUrl}/accounts/expense-options`, { headers: this.getAuthHeaders() });
  }

  create(expense: Expense): Observable<Expense> {
    const payload = {
      emissionDate: expense.emissionDate ?? null,
      accountId: expense.accountId ?? null,
      detail: expense.detail ?? null,
      amount: expense.amount ?? null,
      paymentAccountId: expense.paymentAccountId ?? null,
      subAccountId: expense.subAccountId ?? null,
      occurrenceType: expense.occurrenceType ?? null
    };
    return this.http.post<Expense>(this.expensesUrl, payload, { headers: this.getAuthHeaders() });
  }

  update(id: number, expense: Expense): Observable<void> {
    const payload = {
      emissionDate: expense.emissionDate ?? null,
      accountId: expense.accountId ?? null,
      detail: expense.detail ?? null,
      amount: expense.amount ?? null,
      paymentAccountId: expense.paymentAccountId ?? null,
      subAccountId: expense.subAccountId ?? null,
      occurrenceType: expense.occurrenceType ?? null
    };
    return this.http.put<void>(`${this.expensesUrl}/${id}`, payload, { headers: this.getAuthHeaders() });
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.expensesUrl}/${id}`, { headers: this.getAuthHeaders() });
  }

  /** Orden de Egreso en PDF, armada por el backend con los datos actuales del egreso. */
  downloadOrderPdf(id: number): Observable<Blob> {
    return this.http.get(`${this.expensesUrl}/${id}/pdf`, { headers: this.getAuthHeaders(), responseType: 'blob' });
  }

  /** Comprobante del egreso: lo sube, lo borra y firma la URL el backend (bucket privado de Supabase Storage). */
  voucherApi(id: number): VoucherApi {
    const url = `${this.expensesUrl}/${id}/voucher`;
    return {
      upload: (file: File) => {
        const form = new FormData();
        form.append('file', file, file.name);
        return this.http.post<VoucherInfo>(url, form, { headers: this.getAuthHeaders() });
      },
      remove: () => this.http.delete<void>(url, { headers: this.getAuthHeaders() }),
      getUrl: () => this.http.get<VoucherUrl>(url, { headers: this.getAuthHeaders() })
    };
  }

  private getAuthHeaders(): HttpHeaders {
    const token = localStorage.getItem(this.tokenKey);
    if (!token) return new HttpHeaders();
    return new HttpHeaders({ Authorization: `Bearer ${token}` });
  }
}
