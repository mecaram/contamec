import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { LoginComponent } from './auth/login/login.component';

const routes: Routes = [
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  { path: 'login', component: LoginComponent },
  {
    path: 'dashboard',
    loadChildren: () =>
      import('./features/dashboard/dashboard.module').then((m) => m.DashboardModule)
  },
  {
    path: 'ingresos',
    loadChildren: () =>
      import('./features/incomes/incomes.module').then((m) => m.IncomesModule)
  },
  {
    path: 'egresos',
    loadChildren: () =>
      import('./features/expenses/expenses.module').then((m) => m.ExpensesModule)
  },
  {
    path: 'cuentas',
    loadChildren: () =>
      import('./features/accounts/accounts.module').then((m) => m.AccountsModule)
  },
  {
    path: 'cuentas-pago',
    loadChildren: () =>
      import('./features/payment-accounts/payment-accounts.module').then((m) => m.PaymentAccountsModule)
  },
  {
    path: 'usuarios',
    loadChildren: () =>
      import('./features/users/users.module').then((m) => m.UsersModule)
  },
  {
    path: 'cierres',
    loadChildren: () =>
      import('./features/closures/closures.module').then((m) => m.ClosuresModule)
  }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule {}
