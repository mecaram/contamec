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
    let params = new HttpParams();
    if (filters.id !== null && filters.id !== undefined) params = params.set('id', String(filters.id));
    if (filters.name) params = params.set('name', filters.name);
    return this.http.get<PaymentAccount[]>(this.paymentAccountsUrl, { params, headers: this.getAuthHeaders() });
  }

  create(account: PaymentAccount): Observable<PaymentAccount> {
    const payload = {
      name: account.name ?? null
    };
    return this.http.post<PaymentAccount>(this.paymentAccountsUrl, payload, { headers: this.getAuthHeaders() });
  }

  update(id: number, account: PaymentAccount): Observable<void> {
    const payload = {
      name: account.name ?? null
    };
    return this.http.put<void>(`${this.paymentAccountsUrl}/${id}`, payload, { headers: this.getAuthHeaders() });
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.paymentAccountsUrl}/${id}`, { headers: this.getAuthHeaders() });
  }

  private getAuthHeaders(): HttpHeaders {
    const token = sessionStorage.getItem(this.tokenKey);
    if (!token) return new HttpHeaders();
    return new HttpHeaders({ Authorization: `Bearer ${token}` });
  }
}
