import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { Account } from '../models/account.model';
import { AccountSearch } from '../models/account-search.model';

@Injectable()
export class AccountService {
  private readonly apiBaseUrl = ((environment as any).apiUrl ?? environment.apiBaseUrl) as string;
  private readonly accountsUrl = `${this.apiBaseUrl}/accounts`;
  private readonly tokenKey = 'contamec_token';

  constructor(private readonly http: HttpClient) {}

  search(filters: AccountSearch): Observable<Account[]> {
    let params = new HttpParams();
    if (filters.id !== null && filters.id !== undefined) params = params.set('id', String(filters.id));
    if (filters.name) params = params.set('name', filters.name);
    if (filters.type) params = params.set('type', filters.type);
    return this.http.get<Account[]>(this.accountsUrl, { params, headers: this.getAuthHeaders() });
  }

  create(account: Account): Observable<Account> {
    const payload = {
      name: account.name ?? null,
      type: account.type ?? null
    };
    return this.http.post<Account>(this.accountsUrl, payload, { headers: this.getAuthHeaders() });
  }

  update(id: number, account: Account): Observable<void> {
    const payload = {
      name: account.name ?? null,
      type: account.type ?? null
    };
    return this.http.put<void>(`${this.accountsUrl}/${id}`, payload, { headers: this.getAuthHeaders() });
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.accountsUrl}/${id}`, { headers: this.getAuthHeaders() });
  }

  private getAuthHeaders(): HttpHeaders {
    const token = sessionStorage.getItem(this.tokenKey);
    if (!token) return new HttpHeaders();
    return new HttpHeaders({ Authorization: `Bearer ${token}` });
  }
}
