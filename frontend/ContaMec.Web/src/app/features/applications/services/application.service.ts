import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { Application, ApplicationActivation, ApplicationDeleteResult } from '../models/application.model';
import { ApplicationSearch } from '../models/application-search.model';

@Injectable()
export class ApplicationService {
  private readonly apiBaseUrl = ((environment as any).apiUrl ?? environment.apiBaseUrl) as string;
  private readonly applicationsUrl = `${this.apiBaseUrl}/applications`;
  private readonly tokenKey = 'contamec_token';

  constructor(private readonly http: HttpClient) {}

  search(filters: ApplicationSearch): Observable<Application[]> {
    return this.http.get<Application[]>(this.applicationsUrl, { params: this.buildSearchParams(filters), headers: this.getAuthHeaders() });
  }

  /** Listado con los mismos filtros que la búsqueda, armado por el backend en PDF o Excel. */
  exportList(filters: ApplicationSearch, format: 'pdf' | 'excel'): Observable<Blob> {
    return this.http.get(`${this.applicationsUrl}/export/${format}`, {
      params: this.buildSearchParams(filters),
      headers: this.getAuthHeaders(),
      responseType: 'blob'
    });
  }

  private buildSearchParams(filters: ApplicationSearch): HttpParams {
    let params = new HttpParams();
    if (filters.id !== null && filters.id !== undefined) params = params.set('id', String(filters.id));
    if (filters.name) params = params.set('name', filters.name);
    if (filters.active !== null && filters.active !== undefined) params = params.set('active', String(filters.active));
    return params;
  }

  create(application: Application): Observable<Application> {
    return this.http.post<Application>(this.applicationsUrl, this.buildPayload(application), { headers: this.getAuthHeaders() });
  }

  update(id: number, application: Application): Observable<void> {
    return this.http.put<void>(`${this.applicationsUrl}/${id}`, this.buildPayload(application), { headers: this.getAuthHeaders() });
  }

  activate(id: number): Observable<ApplicationActivation> {
    return this.http.post<ApplicationActivation>(`${this.applicationsUrl}/${id}/activate`, null, { headers: this.getAuthHeaders() });
  }

  delete(id: number): Observable<ApplicationDeleteResult> {
    return this.http.delete<ApplicationDeleteResult>(`${this.applicationsUrl}/${id}`, { headers: this.getAuthHeaders() });
  }

  private buildPayload(application: Application) {
    return {
      name: application.name ?? null,
      documentType: application.documentType ?? null,
      documentNumber: application.documentNumber ?? null,
      active: application.active,
      whatsApp: application.whatsApp ?? null,
      email: application.email ?? null,
      address: application.address ?? null,
      locality: application.locality ?? null
    };
  }

  private getAuthHeaders(): HttpHeaders {
    const token = localStorage.getItem(this.tokenKey);
    if (!token) return new HttpHeaders();
    return new HttpHeaders({ Authorization: `Bearer ${token}` });
  }
}
