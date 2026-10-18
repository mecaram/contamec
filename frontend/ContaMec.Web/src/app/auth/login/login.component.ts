import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../auth.service';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss']
})
export class LoginComponent implements OnInit {
  username = '';
  password = '';
  showPassword = false;
  loading = false;
  errorMessage: string | null = null;

  constructor(
    private readonly auth: AuthService,
    private readonly router: Router,
    private readonly route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    if (this.auth.isSessionValid() && this.auth.getUsername()) {
      void this.router.navigateByUrl('/dashboard');
      return;
    }
    // Un token vencido que quedó guardado no debe mandar al Dashboard.
    this.auth.logout();
    if (this.route.snapshot.queryParamMap.get('sesion') === 'expirada') {
      this.errorMessage = 'Su sesión expiró. Ingrese nuevamente.';
    }
    this.auth.warmUp();
  }

  toggleShowPassword(): void {
    this.showPassword = !this.showPassword;
  }

  onFormSubmit(event: Event): void {
    event.preventDefault();
    this.submit();
  }

  submit(): void {
    this.errorMessage = null;

    const u = this.username.trim();
    const p = this.password;
    if (!u || !p) {
      this.errorMessage = 'Ingrese usuario y contraseña.';
      return;
    }

    this.loading = true;
    this.auth.login({ username: u, password: p }).subscribe({
      next: (res) => {
        this.auth.saveSession(res);
        this.loading = false;
        this.password = '';
        this.showPassword = false;
        this.errorMessage = null;
        void this.router.navigateByUrl('/dashboard');
      },
      error: (err: Error) => {
        this.loading = false;
        this.errorMessage = err.message ?? 'Error al iniciar sesión.';
      }
    });
  }

}
