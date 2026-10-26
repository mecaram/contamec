import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { PaymentAccount } from '../models/payment-account.model';
import { PaymentAccountSearch } from '../models/payment-account-search.model';

@Injectable()
export class PaymentAccountService {
  private readonly apiBaseUrl = ((environment as any).apiUrl ?? environment.apiBaseUrl) as string;
  private readonly paymentAccountsUrl = `${this.apiBaseUrl}/paymentaccounts`;
  private readonly tokenKey = 'contamec_token';

  constructor(private readonly http: HttpClient) {}

  search(filters: PaymentAccountSearch): Observable<PaymentAccount[]> {
    return this.http.get<PaymentAccount[]>(this.paymentAccountsUrl, { params: this.buildSearchParams(filters), headers: this.getAuthHeaders() });
  }

  /** Listado con los mismos filtros que la búsqueda, armado por el backend en PDF o Excel. */
  exportList(filters: PaymentAccountSearch, format: 'pdf' | 'excel'): Observable<Blob> {
    return this.http.get(`${this.paymentAccountsUrl}/export/${format}`, {
      params: this.buildSearchParams(filters),
      headers: this.getAuthHeaders(),
      responseType: 'blob'
    });
  }

  private buildSearchParams(filters: PaymentAccountSearch): HttpParams {
    let params = new HttpParams();
    if (filters.id !== null && filters.id !== undefined) params = params.set('id', String(filters.id));
    if (filters.name) params = params.set('name', filters.name);
    if (filters.onlyActive) params = params.set('onlyActive', 'true');
    return params;
  }

  create(account: PaymentAccount): Observable<PaymentAccount> {
    const payload = {
      name: account.name ?? null,
      active: account.active ?? true
    };
    return this.http.post<PaymentAccount>(this.paymentAccountsUrl, payload, { headers: this.getAuthHeaders() });
  }

  update(id: number, account: PaymentAccount): Observable<void> {
    const payload = {
      name: account.name ?? null,
      active: account.active ?? true
    };
    return this.http.put<void>(`${this.paymentAccountsUrl}/${id}`, payload, { headers: this.getAuthHeaders() });
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.paymentAccountsUrl}/${id}`, { headers: this.getAuthHeaders() });
  }

  private getAuthHeaders(): HttpHeaders {
    const token = localStorage.getItem(this.tokenKey);
    if (!token) return new HttpHeaders();
    return new HttpHeaders({ Authorization: `Bearer ${token}` });
  }
}
