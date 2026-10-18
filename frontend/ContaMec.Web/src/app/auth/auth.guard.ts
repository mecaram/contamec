import { Injectable } from '@angular/core';
import { CanActivate, Router, UrlTree } from '@angular/router';
import { AuthService } from './auth.service';

/** Exige una sesión vigente: sin token, o con el token vencido, se vuelve al Login. */
@Injectable({
  providedIn: 'root'
})
export class AuthGuard implements CanActivate {
  constructor(
    private readonly auth: AuthService,
    private readonly router: Router
  ) {}

  canActivate(): boolean | UrlTree {
    if (this.auth.isSessionValid()) return true;
    const hadSession = this.auth.getToken() !== null;
    this.auth.logout();
    return this.router.parseUrl(hadSession ? '/login?sesion=expirada' : '/login');
  }
}
