import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { LoginComponent } from './auth/login/login.component';
import { SessionExpiredInterceptor } from './auth/session-expired.interceptor';
import { PlaceholderPageComponent } from './shared/placeholder-page/placeholder-page.component';

@NgModule({
  declarations: [AppComponent, LoginComponent, PlaceholderPageComponent],
  // HttpClientModule solo acá: si un módulo lazy lo vuelve a importar, sus pedidos no pasan por el interceptor.
  imports: [BrowserModule, BrowserAnimationsModule, HttpClientModule, AppRoutingModule],
  providers: [{ provide: HTTP_INTERCEPTORS, useClass: SessionExpiredInterceptor, multi: true }],
  bootstrap: [AppComponent]
})
export class AppModule { }
