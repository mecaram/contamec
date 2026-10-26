import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { ServiceWorkerModule } from '@angular/service-worker';

import { environment } from '../environments/environment';
import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { LoginComponent } from './auth/login/login.component';
import { SessionExpiredInterceptor } from './auth/session-expired.interceptor';
import { PlaceholderPageComponent } from './shared/placeholder-page/placeholder-page.component';
import { PwaPromptsComponent } from './shared/pwa/pwa-prompts.component';

@NgModule({
  declarations: [AppComponent, LoginComponent, PlaceholderPageComponent, PwaPromptsComponent],
  // HttpClientModule solo acá: si un módulo lazy lo vuelve a importar, sus pedidos no pasan por el interceptor.
  imports: [
    BrowserModule,
    BrowserAnimationsModule,
    HttpClientModule,
    AppRoutingModule,
    // Solo en los builds de producción (Vercel y Plesk); con ng serve no hay service worker.
    ServiceWorkerModule.register('ngsw-worker.js', {
      enabled: environment.production,
      registrationStrategy: 'registerWhenStable:30000'
    })
  ],
  providers: [{ provide: HTTP_INTERCEPTORS, useClass: SessionExpiredInterceptor, multi: true }],
  bootstrap: [AppComponent]
})
export class AppModule { }
