import { Component } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs/operators';
import { InstallGuide, PwaService } from './pwa.service';

/** Pantallas donde la guía de instalación aparece sola; en las demás taparía los botones de los formularios. */
const installRoutes = ['/login', '/dashboard'];

/** Avisos de la PWA para toda la app: instalación, versión nueva y sin conexión. */
@Component({
  selector: 'app-pwa-prompts',
  templateUrl: './pwa-prompts.component.html',
  styleUrls: ['./pwa-prompts.component.scss']
})
export class PwaPromptsComponent {
  private onInstallRoute = false;

  constructor(readonly pwa: PwaService, router: Router) {
    router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe((event) => {
        const path = event.urlAfterRedirects.split(/[?#]/)[0];
        this.onInstallRoute = installRoutes.includes(path);
      });
  }

  get guide(): InstallGuide | null {
    const guide = this.pwa.guide;
    if (!guide) return null;
    if (this.pwa.guideRequested) return guide;
    return this.onInstallRoute && !this.pwa.dismissed ? guide : null;
  }

  retry(): void {
    window.location.reload();
  }
}
