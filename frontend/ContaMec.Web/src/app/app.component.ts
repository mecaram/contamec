import { Component } from '@angular/core';
import { NavigationEnd, NavigationError, Router } from '@angular/router';
import { AuthService } from './auth/auth.service';

const chunkReloadKey = 'contamec_chunk_reload';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent {
  title = 'contamec-web';

  constructor(auth: AuthService, router: Router) {
    // Al recargar o volver a abrir la app la sesión sigue en localStorage: se vuelve a programar su vencimiento.
    auth.watchSessionExpiry();
    auth.startActivityTracking();

    // Después de un deploy, una pestaña abierta pide archivos de pantallas que ya no existen:
    // se recarga una vez en la URL de destino para tomar la versión nueva.
    router.events.subscribe((event) => {
      if (event instanceof NavigationError && event.error?.name === 'ChunkLoadError') {
        if (sessionStorage.getItem(chunkReloadKey) === event.url) return;
        sessionStorage.setItem(chunkReloadKey, event.url);
        window.location.assign(event.url);
      } else if (event instanceof NavigationEnd) {
        // Navegación exitosa: si vuelve a fallar más adelante, se puede recargar de nuevo.
        sessionStorage.removeItem(chunkReloadKey);
      }
    });
  }
}
