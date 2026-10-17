import { Injectable } from '@angular/core';
import { defer, from, Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../environments/environment';

export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  username: string;
  applicationId: number;
  applicationName: string;
}

interface FetchLoginError {
  status: number;
  message: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly tokenKey = 'contamec_token';
  private readonly usernameKey = 'contamec_username';
  private readonly applicationIdKey = 'contamec_application_id';
  private readonly applicationNameKey = 'contamec_application_name';

  login(body: LoginRequest): Observable<LoginResponse> {
    const url = `${environment.apiBaseUrl}/auth/login`;
    return defer(() => from(this.postLoginJson(url, body))).pipe(
      catchError((err: FetchLoginError | { status?: number; message?: string }) => {
        const status = 'status' in err && err.status !== undefined ? err.status : 0;
        if (status === 401) {
          return throwError(new Error('Usuario o contraseña incorrectos.'));
        }
        if (status === 403) {
          return throwError(
            new Error(('message' in err && err.message) || 'No es posible iniciar sesión. Comuníquese con el administrador del sistema.')
          );
        }
        if (status === 0) {
          return throwError(
            new Error(
              'No hay respuesta de la API (red, CORS o certificado SSL). ' +
                'Ejecute el front con: ng serve (usa proxy.conf.json hacia la API). ' +
                'Compruebe que ContaMec.Api esté en ejecución.'
            )
          );
        }
        return throwError(
          new Error(
            `Error al contactar la API (${status}). ${('message' in err && err.message) || 'Revise que la API esté en ejecución.'}`
          )
        );
      })
    );
  }

  private async postLoginJson(url: string, body: LoginRequest): Promise<LoginResponse> {
    let res: Response;
    try {
      res = await fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
      });
    } catch {
      const err: FetchLoginError = { status: 0, message: 'Unknown Error' };
      throw err;
    }

    if (res.status === 401) {
      const err: FetchLoginError = { status: 401, message: res.statusText };
      throw err;
    }
    if (res.status === 403) {
      // El backend rechaza el login con { message } (aplicación inactiva o inexistente).
      const data = (await res.json().catch(() => null)) as { message?: string } | null;
      const err: FetchLoginError = { status: 403, message: data?.message ?? '' };
      throw err;
    }
    if (!res.ok) {
      const err: FetchLoginError = {
        status: res.status,
        message: `Http failure response for ${url}: ${res.status} ${res.statusText}`
      };
      throw err;
    }
    return (await res.json()) as LoginResponse;
  }

  saveSession(response: LoginResponse): void {
    sessionStorage.setItem(this.tokenKey, response.token);
    sessionStorage.setItem(this.usernameKey, response.username);
    sessionStorage.setItem(this.applicationIdKey, String(response.applicationId));
    sessionStorage.setItem(this.applicationNameKey, response.applicationName);
  }

  getToken(): string | null {
    return sessionStorage.getItem(this.tokenKey);
  }

  getUsername(): string | null {
    return sessionStorage.getItem(this.usernameKey);
  }

  /**
   * Aplicación (empresa) del usuario autenticado, informada por el backend al iniciar sesión.
   * Solo para uso en la interfaz: el backend toma el ApplicationId del JWT, no de este valor.
   */
  getApplicationId(): number | null {
    const value = Number(sessionStorage.getItem(this.applicationIdKey));
    return Number.isInteger(value) && value > 0 ? value : null;
  }

  getApplicationName(): string | null {
    return sessionStorage.getItem(this.applicationNameKey);
  }

  logout(): void {
    sessionStorage.removeItem(this.tokenKey);
    sessionStorage.removeItem(this.usernameKey);
    sessionStorage.removeItem(this.applicationIdKey);
    sessionStorage.removeItem(this.applicationNameKey);
  }
}
