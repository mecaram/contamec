import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface CompanyRegistrationRequest {
  name: string;
  documentType: string;
  documentNumber: string;
  whatsApp: string | null;
  email: string;
  confirmEmail: string;
  address: string | null;
  locality: string | null;
  username: string;
  confirmUsername: string;
  password: string;
  confirmPassword: string;
}

/** Endpoint público: no envía token ni inicia sesión. */
@Injectable()
export class RegisterCompanyService {
  private readonly apiBaseUrl = ((environment as any).apiUrl ?? environment.apiBaseUrl) as string;

  constructor(private readonly http: HttpClient) {}

  register(request: CompanyRegistrationRequest): Observable<void> {
    return this.http.post<void>(`${this.apiBaseUrl}/auth/register-company`, request);
  }
}
