import { Injectable, NgZone } from '@angular/core';
import { Router } from '@angular/router';
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
  role: string;
  previousLoginAt: string | null;
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
  private readonly roleKey = 'contamec_role';
  private readonly previousLoginAtKey = 'contamec_previous_login_at';
  private readonly superAdministratorRole = 'SuperAdministrador';
  private expiryTimer: ReturnType<typeof setTimeout> | null = null;
  /** Momento en que se obtuvo el token actual; 0 si se desconoce (p. ej. tras recargar la página). */
  private tokenObtainedAt = 0;
  private refreshing = false;
  private activityTrackingStarted = false;
  /** Con actividad, el token se renueva como mucho una vez por este intervalo. */
  private readonly refreshIntervalMs = 2 * 60 * 1000;

  constructor(
    private readonly router: Router,
    private readonly zone: NgZone
  ) {}

  /**
   * Despierta la API (Render la suspende sin tráfico) y su conexión a la base mientras se escriben las credenciales.
   * No espera la respuesta ni informa errores: si falla, el login funciona igual.
   */
  warmUp(): void {
    fetch(`${environment.apiBaseUrl}/health`).catch(() => undefined);
  }

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
    sessionStorage.setItem(this.roleKey, response.role ?? '');
    // Vacío significa "primer acceso"; la clave ausente, una sesión iniciada antes de existir este dato.
    sessionStorage.setItem(this.previousLoginAtKey, response.previousLoginAt ?? '');
    this.tokenObtainedAt = Date.now();
    this.watchSessionExpiry();
  }

  /**
   * La sesión vence por inactividad: mientras el usuario usa la pantalla (teclado, mouse, toques) el token
   * se renueva, y si deja de usarla vence al cumplirse Jwt:ExpiresInMinutes desde la última renovación.
   */
  startActivityTracking(): void {
    if (this.activityTrackingStarted) return;
    this.activityTrackingStarted = true;
    // Fuera de la zona de Angular: estos eventos son muy frecuentes y no deben disparar la detección de cambios.
    this.zone.runOutsideAngular(() => {
      for (const eventName of ['click', 'keydown', 'mousemove', 'touchstart', 'wheel']) {
        document.addEventListener(eventName, () => this.onUserActivity(), { passive: true, capture: true });
      }
    });
  }

  /** Hay token y todavía no venció (claim exp del JWT). */
  isSessionValid(): boolean {
    const expiresAt = this.getTokenExpiration();
    return expiresAt !== null && Date.now() < expiresAt;
  }

  /**
   * Programa la salida al Login para el momento en que vence el token, así una pantalla abierta sin uso
   * no queda mostrando datos vacíos. Se llama al iniciar sesión y al arrancar la aplicación.
   */
  watchSessionExpiry(): void {
    this.clearExpiryTimer();
    const expiresAt = this.getTokenExpiration();
    if (expiresAt === null) return;
    this.expiryTimer = setTimeout(() => this.expireSession(), Math.max(0, expiresAt - Date.now()));
  }

  /** Cierra la sesión vencida y vuelve al Login avisando el motivo. */
  expireSession(): void {
    this.logout();
    this.zone.run(() => void this.router.navigate(['/login'], { queryParams: { sesion: 'expirada' } }));
  }

  private onUserActivity(): void {
    if (this.refreshing || Date.now() - this.tokenObtainedAt < this.refreshIntervalMs) return;
    if (!this.isSessionValid()) return;
    void this.refreshToken();
  }

  private async refreshToken(): Promise<void> {
    const token = this.getToken();
    if (!token) return;
    this.refreshing = true;
    try {
      const res = await fetch(`${environment.apiBaseUrl}/auth/refresh`, {
        method: 'POST',
        headers: { Authorization: `Bearer ${token}` }
      });
      // Se ignora la respuesta si mientras tanto se cerró la sesión o se inició otra.
      if (this.getToken() !== token) return;
      if (res.status === 401) {
        this.expireSession();
        return;
      }
      if (!res.ok) return;
      const data = (await res.json()) as { token?: string };
      if (!data.token) return;
      sessionStorage.setItem(this.tokenKey, data.token);
      this.tokenObtainedAt = Date.now();
      this.watchSessionExpiry();
    } catch {
      // Sin red el token sigue vigente hasta su vencimiento; se reintenta con la próxima actividad.
    } finally {
      this.refreshing = false;
    }
  }

  private getTokenExpiration(): number | null {
    const payload = this.getToken()?.split('.')[1];
    if (!payload) return null;
    try {
      const json = atob(payload.replace(/-/g, '+').replace(/_/g, '/'));
      const exp = (JSON.parse(json) as { exp?: unknown }).exp;
      return typeof exp === 'number' ? exp * 1000 : null;
    } catch {
      return null;
    }
  }

  private clearExpiryTimer(): void {
    if (this.expiryTimer !== null) {
      clearTimeout(this.expiryTimer);
      this.expiryTimer = null;
    }
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

  /** Fecha ISO del inicio de sesión anterior: '' si es el primero, null si no se conoce. */
  getPreviousLoginAt(): string | null {
    return sessionStorage.getItem(this.previousLoginAtKey);
  }

  /** Solo para la interfaz: el backend autoriza por el rol del JWT. */
  isSuperAdministrator(): boolean {
    return sessionStorage.getItem(this.roleKey) === this.superAdministratorRole;
  }

  logout(): void {
    this.clearExpiryTimer();
    this.tokenObtainedAt = 0;
    sessionStorage.removeItem(this.tokenKey);
    sessionStorage.removeItem(this.usernameKey);
    sessionStorage.removeItem(this.applicationIdKey);
    sessionStorage.removeItem(this.applicationNameKey);
    sessionStorage.removeItem(this.roleKey);
    sessionStorage.removeItem(this.previousLoginAtKey);
  }
}
