import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { SubAccount } from '../models/sub-account.model';
import { SubAccountSearch } from '../models/sub-account-search.model';

@Injectable()
export class SubAccountService {
  private readonly apiBaseUrl = ((environment as any).apiUrl ?? environment.apiBaseUrl) as string;
  private readonly subAccountsUrl = `${this.apiBaseUrl}/subaccounts`;
  private readonly tokenKey = 'contamec_token';

  constructor(private readonly http: HttpClient) {}

  search(filters: SubAccountSearch): Observable<SubAccount[]> {
    return this.http.get<SubAccount[]>(this.subAccountsUrl, { params: this.buildSearchParams(filters), headers: this.getAuthHeaders() });
  }

  /** Listado con los mismos filtros que la búsqueda, armado por el backend en PDF o Excel. */
  exportList(filters: SubAccountSearch, format: 'pdf' | 'excel'): Observable<Blob> {
    return this.http.get(`${this.subAccountsUrl}/export/${format}`, {
      params: this.buildSearchParams(filters),
      headers: this.getAuthHeaders(),
      responseType: 'blob'
    });
  }

  private buildSearchParams(filters: SubAccountSearch): HttpParams {
    let params = new HttpParams();
    if (filters.id !== null && filters.id !== undefined) params = params.set('id', String(filters.id));
    if (filters.name) params = params.set('name', filters.name);
    if (filters.accountId !== null && filters.accountId !== undefined) params = params.set('accountId', String(filters.accountId));
    return params;
  }

  create(subAccount: SubAccount): Observable<SubAccount> {
    return this.http.post<SubAccount>(this.subAccountsUrl, this.toPayload(subAccount), { headers: this.getAuthHeaders() });
  }

  update(id: number, subAccount: SubAccount): Observable<void> {
    return this.http.put<void>(`${this.subAccountsUrl}/${id}`, this.toPayload(subAccount), { headers: this.getAuthHeaders() });
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.subAccountsUrl}/${id}`, { headers: this.getAuthHeaders() });
  }

  private toPayload(subAccount: SubAccount): object {
    return {
      name: subAccount.name ?? null,
      accountId: subAccount.accountId ?? null,
      behaviorType: subAccount.behaviorType ?? null,
      active: subAccount.active ?? true
    };
  }

  private getAuthHeaders(): HttpHeaders {
    const token = sessionStorage.getItem(this.tokenKey);
    if (!token) return new HttpHeaders();
    return new HttpHeaders({ Authorization: `Bearer ${token}` });
  }
}
