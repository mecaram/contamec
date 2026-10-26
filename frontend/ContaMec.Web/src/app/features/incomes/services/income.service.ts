import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { VoucherApi, VoucherInfo, VoucherUrl } from '../../../shared/voucher/voucher-api';
import { Income } from '../models/income.model';
import { IncomeSearch } from '../models/income-search.model';

export interface ClosureLookup {
  id: number;
  isClosed?: boolean | number | null;
  openDate?: string | null;
  closureNumber?: number | null;
}

export interface IncomeAccountOption {
  id: number;
  name?: string | null;
  subAccountRequires?: boolean;
}

@Injectable()
export class IncomeService {
  private readonly apiBaseUrl = ((environment as any).apiUrl ?? environment.apiBaseUrl) as string;
  private readonly incomesUrl = `${this.apiBaseUrl}/incomes`;
  private readonly tokenKey = 'contamec_token';

  constructor(private readonly http: HttpClient) {}

  search(filters: IncomeSearch): Observable<Income[]> {
    return this.http.get<Income[]>(this.incomesUrl, { params: this.buildSearchParams(filters), headers: this.getAuthHeaders() });
  }

  /** Listado con los mismos filtros que la búsqueda, armado por el backend en PDF o Excel. */
  exportList(filters: IncomeSearch, format: 'pdf' | 'excel'): Observable<Blob> {
    return this.http.get(`${this.incomesUrl}/export/${format}`, {
      params: this.buildSearchParams(filters),
      headers: this.getAuthHeaders(),
      responseType: 'blob'
    });
  }

  private buildSearchParams(filters: IncomeSearch): HttpParams {
    let params = new HttpParams();

    if (filters.dateFrom) params = params.set('dateFrom', filters.dateFrom);
    if (filters.dateTo) params = params.set('dateTo', filters.dateTo);
    if (filters.closureId !== null && filters.closureId !== undefined) {
      params = params.set('closureId', String(filters.closureId));
    }
    if (filters.accountId !== null && filters.accountId !== undefined) {
      params = params.set('accountId', String(filters.accountId));
    }
    if (filters.paymentAccountId !== null && filters.paymentAccountId !== undefined) {
      params = params.set('paymentAccountId', String(filters.paymentAccountId));
    }
    if (filters.subAccountId !== null && filters.subAccountId !== undefined) {
      params = params.set('subAccountId', String(filters.subAccountId));
    }
    if (filters.amountFrom !== null && filters.amountFrom !== undefined) {
      params = params.set('amountFrom', String(filters.amountFrom));
    }
    if (filters.amountTo !== null && filters.amountTo !== undefined) {
      params = params.set('amountTo', String(filters.amountTo));
    }
    if (filters.detail) params = params.set('detail', filters.detail);
    return params;
  }

  getClosures(): Observable<ClosureLookup[]> {
    return this.http.get<ClosureLookup[]>(`${this.apiBaseUrl}/closures`, { headers: this.getAuthHeaders() });
  }

  /** Cuenta, subcuenta y cuenta de pago de los últimos ingresos que registró el usuario; null si todavía no registró ninguno. */
  getDefaults(): Observable<{ accountId: number | null; subAccountId: number | null; paymentAccountId: number | null }> {
    return this.http.get<{ accountId: number | null; subAccountId: number | null; paymentAccountId: number | null }>(`${this.incomesUrl}/last-payment-account`, { headers: this.getAuthHeaders() });
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
      amount: income.amount ?? null,
      paymentAccountId: income.paymentAccountId ?? null,
      subAccountId: income.subAccountId ?? null,
      occurrenceType: income.occurrenceType ?? null
    };
    return this.http.post<Income>(this.incomesUrl, payload, { headers: this.getAuthHeaders() });
  }

  update(id: number, income: Income): Observable<void> {
    const payload = {
      emissionDate: income.emissionDate ?? null,
      accountId: income.accountId ?? null,
      detail: income.detail ?? null,
      amount: income.amount ?? null,
      paymentAccountId: income.paymentAccountId ?? null,
      subAccountId: income.subAccountId ?? null,
      occurrenceType: income.occurrenceType ?? null
    };
    return this.http.put<void>(`${this.incomesUrl}/${id}`, payload, { headers: this.getAuthHeaders() });
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.incomesUrl}/${id}`, { headers: this.getAuthHeaders() });
  }

  /** Orden de Ingreso en PDF, armada por el backend con los datos actuales del ingreso. */
  downloadOrderPdf(id: number): Observable<Blob> {
    return this.http.get(`${this.incomesUrl}/${id}/pdf`, { headers: this.getAuthHeaders(), responseType: 'blob' });
  }

  /** Comprobante del ingreso: lo sube, lo borra y firma la URL el backend (bucket privado de Supabase Storage). */
  voucherApi(id: number): VoucherApi {
    const url = `${this.incomesUrl}/${id}/voucher`;
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
