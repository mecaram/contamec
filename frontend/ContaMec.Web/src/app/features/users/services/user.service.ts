import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { User } from '../models/user.model';
import { UserSearch } from '../models/user-search.model';

export interface UserRoleOption {
  id: number;
  name?: string | null;
}

@Injectable()
export class UserService {
  private readonly apiBaseUrl = ((environment as any).apiUrl ?? environment.apiBaseUrl) as string;
  private readonly usersUrl = `${this.apiBaseUrl}/users`;
  private readonly tokenKey = 'contamec_token';

  constructor(private readonly http: HttpClient) {}

  search(filters: UserSearch): Observable<User[]> {
    let params = new HttpParams();
    if (filters.id !== null && filters.id !== undefined) params = params.set('id', String(filters.id));
    if (filters.name) params = params.set('name', filters.name);
    if (filters.isActive !== null && filters.isActive !== undefined) params = params.set('isActive', String(filters.isActive));
    if (filters.userRoleId !== null && filters.userRoleId !== undefined) params = params.set('userRoleId', String(filters.userRoleId));
    return this.http.get<User[]>(this.usersUrl, { params, headers: this.getAuthHeaders() });
  }

  getRoleOptions(): Observable<UserRoleOption[]> {
    return this.http.get<UserRoleOption[]>(`${this.usersUrl}/role-options`, { headers: this.getAuthHeaders() });
  }

  create(user: User): Observable<User> {
    const payload = {
      name: user.name ?? null,
      password: user.password ?? null,
      isActive: user.isActive,
      userRoleId: user.userRoleId ?? null
    };
    return this.http.post<User>(this.usersUrl, payload, { headers: this.getAuthHeaders() });
  }

  update(id: number, user: User): Observable<void> {
    const payload = {
      name: user.name ?? null,
      password: user.password ?? null,
      isActive: user.isActive,
      userRoleId: user.userRoleId ?? null
    };
    return this.http.put<void>(`${this.usersUrl}/${id}`, payload, { headers: this.getAuthHeaders() });
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.usersUrl}/${id}`, { headers: this.getAuthHeaders() });
  }

  private getAuthHeaders(): HttpHeaders {
    const token = sessionStorage.getItem(this.tokenKey);
    if (!token) return new HttpHeaders();
    return new HttpHeaders({ Authorization: `Bearer ${token}` });
  }
}
