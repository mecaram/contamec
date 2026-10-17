import { Injectable } from '@angular/core';
import { CanActivate, Router, UrlTree } from '@angular/router';
import { AuthService } from './auth.service';

/** Restringe rutas a usuarios con rol SuperAdministrador. El backend valida lo mismo con el JWT. */
@Injectable({
  providedIn: 'root'
})
export class SuperAdminGuard implements CanActivate {
  constructor(
    private readonly auth: AuthService,
    private readonly router: Router
  ) {}

  canActivate(): boolean | UrlTree {
    return this.auth.isSuperAdministrator() ? true : this.router.parseUrl('/dashboard');
  }
}
