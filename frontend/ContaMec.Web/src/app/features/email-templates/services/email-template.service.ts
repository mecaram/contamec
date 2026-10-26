import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { EmailTemplate } from '../models/email-template.model';
import { EmailTemplateSearch } from '../models/email-template-search.model';

@Injectable()
export class EmailTemplateService {
  private readonly apiBaseUrl = ((environment as any).apiUrl ?? environment.apiBaseUrl) as string;
  private readonly templatesUrl = `${this.apiBaseUrl}/emailtemplates`;
  private readonly tokenKey = 'contamec_token';

  constructor(private readonly http: HttpClient) {}

  search(filters: EmailTemplateSearch): Observable<EmailTemplate[]> {
    return this.http.get<EmailTemplate[]>(this.templatesUrl, { params: this.buildSearchParams(filters), headers: this.getAuthHeaders() });
  }

  /** Listado con los mismos filtros que la búsqueda, armado por el backend en PDF o Excel. */
  exportList(filters: EmailTemplateSearch, format: 'pdf' | 'excel'): Observable<Blob> {
    return this.http.get(`${this.templatesUrl}/export/${format}`, {
      params: this.buildSearchParams(filters),
      headers: this.getAuthHeaders(),
      responseType: 'blob'
    });
  }

  private buildSearchParams(filters: EmailTemplateSearch): HttpParams {
    let params = new HttpParams();
    if (filters.id !== null && filters.id !== undefined) params = params.set('id', String(filters.id));
    if (filters.name) params = params.set('name', filters.name);
    return params;
  }

  create(template: EmailTemplate): Observable<EmailTemplate> {
    return this.http.post<EmailTemplate>(this.templatesUrl, this.buildPayload(template), { headers: this.getAuthHeaders() });
  }

  update(id: number, template: EmailTemplate): Observable<void> {
    return this.http.put<void>(`${this.templatesUrl}/${id}`, this.buildPayload(template), { headers: this.getAuthHeaders() });
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.templatesUrl}/${id}`, { headers: this.getAuthHeaders() });
  }

  private buildPayload(template: EmailTemplate) {
    return {
      name: template.name ?? null,
      subject: template.subject ?? null,
      body: template.body ?? null
    };
  }

  private getAuthHeaders(): HttpHeaders {
    const token = localStorage.getItem(this.tokenKey);
    if (!token) return new HttpHeaders();
    return new HttpHeaders({ Authorization: `Bearer ${token}` });
  }
}
