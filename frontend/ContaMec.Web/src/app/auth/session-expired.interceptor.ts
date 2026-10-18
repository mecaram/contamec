import { HttpErrorResponse, HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { AuthService } from './auth.service';

/**
 * Un 401 de la API significa que el token venció o dejó de ser válido: se cierra la sesión y se vuelve al Login.
 * El inicio de sesión usa fetch, así que un 401 por credenciales incorrectas no pasa por acá.
 */
@Injectable()
export class SessionExpiredInterceptor implements HttpInterceptor {
  constructor(private readonly auth: AuthService) {}

  intercept(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    return next.handle(request).pipe(
      catchError((error: unknown) => {
        if (error instanceof HttpErrorResponse && error.status === 401 && this.auth.getToken() !== null) {
          this.auth.expireSession();
        }
        return throwError(error);
      })
    );
  }
}
