import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthGuard } from './auth/auth.guard';
import { LoginComponent } from './auth/login/login.component';

const routes: Routes = [
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  { path: 'login', component: LoginComponent },
  {
    path: 'registrar-empresa',
    loadChildren: () =>
      import('./features/register-company/register-company.module').then((m) => m.RegisterCompanyModule)
  },
  {
    path: 'dashboard',
    canActivate: [AuthGuard],
    loadChildren: () =>
      import('./features/dashboard/dashboard.module').then((m) => m.DashboardModule)
  },
  {
    path: 'ingresos',
    canActivate: [AuthGuard],
    loadChildren: () =>
      import('./features/incomes/incomes.module').then((m) => m.IncomesModule)
  },
  {
    path: 'egresos',
    canActivate: [AuthGuard],
    loadChildren: () =>
      import('./features/expenses/expenses.module').then((m) => m.ExpensesModule)
  },
  {
    path: 'cuentas',
    canActivate: [AuthGuard],
    loadChildren: () =>
      import('./features/accounts/accounts.module').then((m) => m.AccountsModule)
  },
  {
    path: 'cuentas-pago',
    canActivate: [AuthGuard],
    loadChildren: () =>
      import('./features/payment-accounts/payment-accounts.module').then((m) => m.PaymentAccountsModule)
  },
  {
    path: 'usuarios',
    canActivate: [AuthGuard],
    loadChildren: () =>
      import('./features/users/users.module').then((m) => m.UsersModule)
  },
  {
    path: 'aplicaciones',
    canActivate: [AuthGuard],
    loadChildren: () =>
      import('./features/applications/applications.module').then((m) => m.ApplicationsModule)
  },
  {
    path: 'cambiar-clave',
    canActivate: [AuthGuard],
    loadChildren: () =>
      import('./features/change-password/change-password.module').then((m) => m.ChangePasswordModule)
  },
  {
    path: 'cierres',
    canActivate: [AuthGuard],
    loadChildren: () =>
      import('./features/closures/closures.module').then((m) => m.ClosuresModule)
  }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule {}
