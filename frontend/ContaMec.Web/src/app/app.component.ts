import { Component } from '@angular/core';
import { AuthService } from './auth/auth.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent {
  title = 'contamec-web';

  constructor(auth: AuthService) {
    // Al recargar la página la sesión sigue en sessionStorage: se vuelve a programar su vencimiento.
    auth.watchSessionExpiry();
    auth.startActivityTracking();
  }
}
