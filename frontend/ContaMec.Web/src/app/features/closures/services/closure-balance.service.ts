import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ClosureBalance } from '../models/closure-balance.model';

@Injectable()
export class ClosureBalanceService {
  private readonly apiBaseUrl = ((environment as any).apiUrl ?? environment.apiBaseUrl) as string;
  private readonly closureBalancesUrl = `${this.apiBaseUrl}/closurebalances`;
  private readonly tokenKey = 'contamec_token';

  constructor(private readonly http: HttpClient) {}

  getByClosureId(closureId: number): Observable<ClosureBalance[]> {
    return this.http.get<ClosureBalance[]>(`${this.closureBalancesUrl}/by-closure/${closureId}`, {
      headers: this.getAuthHeaders()
    });
  }

  saveBulk(closureId: number, list: ClosureBalance[]): Observable<ClosureBalance[]> {
    const payload = list.map((row) => ({
      id: row.id,
      closureId: row.closureId,
      paymentAccountId: row.paymentAccountId,
      amount: row.amount
    }));

    return this.http.post<ClosureBalance[]>(`${this.closureBalancesUrl}/by-closure/${closureId}/bulk`, payload, {
      headers: this.getAuthHeaders()
    });
  }

  private getAuthHeaders(): HttpHeaders {
    const token = sessionStorage.getItem(this.tokenKey);
    if (!token) return new HttpHeaders();
    return new HttpHeaders({ Authorization: `Bearer ${token}` });
  }
}
