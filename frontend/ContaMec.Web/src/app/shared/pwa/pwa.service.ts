import { Injectable, NgZone } from '@angular/core';
import { SwUpdate } from '@angular/service-worker';

/**
 * Cómo se instala ContaMEC en el navegador actual:
 * - prompt: el navegador ofrece su propio diálogo (Chrome, Edge, Opera, Brave, Samsung Internet).
 * - ios-safari / ios-other: Compartir › Agregar a pantalla de inicio (en iOS todos los navegadores usan Safari por debajo).
 * - mac-safari: Archivo › Agregar al Dock.
 * - firefox-android: menú › Instalar.
 * - unsupported: no se puede instalar; la app funciona igual desde el navegador.
 */
export type InstallGuide = 'prompt' | 'ios-safari' | 'ios-other' | 'mac-safari' | 'firefox-android' | 'unsupported';

/** Evento beforeinstallprompt de los navegadores Chromium (no está en los tipos del DOM). */
interface BeforeInstallPromptEvent extends Event {
  prompt(): Promise<void>;
  readonly userChoice: Promise<{ outcome: 'accepted' | 'dismissed' }>;
}

const dismissedKey = 'contamec_install_dismissed';
const outcomeKey = 'contamec_install_outcome';
/** Cada cuánto se busca una versión nueva mientras la app queda abierta. */
const updateCheckIntervalMs = 30 * 60 * 1000;

/** Instalación como PWA, aviso de versión nueva y estado de la conexión. */
@Injectable({
  providedIn: 'root'
})
export class PwaService {
  /** La app ya está instalada o se está usando como app instalada. */
  installed = isStandalone();
  updateAvailable = false;
  offline = !navigator.onLine;
  /** El usuario pidió ver la guía desde el menú, aunque antes la haya cerrado. */
  guideRequested = false;
  dismissed = readStorage(dismissedKey) === '1';

  private deferredPrompt: BeforeInstallPromptEvent | null = null;
  private readonly fallbackGuide = detectFallbackGuide();

  constructor(private readonly swUpdate: SwUpdate, zone: NgZone) {
    window.addEventListener('beforeinstallprompt', (event) => {
      // Se guarda para mostrarlo desde el botón propio en lugar de la barra del navegador.
      event.preventDefault();
      this.deferredPrompt = event as BeforeInstallPromptEvent;
    });
    window.addEventListener('appinstalled', () => {
      this.installed = true;
      this.deferredPrompt = null;
      this.guideRequested = false;
    });
    window.addEventListener('online', () => (this.offline = false));
    window.addEventListener('offline', () => (this.offline = true));

    if (!swUpdate.isEnabled) return;
    swUpdate.available.subscribe(() => (this.updateAvailable = true));
    // Si el service worker no puede servir la versión guardada, se recarga desde la red.
    swUpdate.unrecoverable.subscribe(() => window.location.reload());
    // Fuera de Angular: un intervalo pendiente impediría que la app quede "estable" y registre el service worker.
    zone.runOutsideAngular(() => {
      setInterval(() => void this.checkForUpdate(), updateCheckIntervalMs);
      document.addEventListener('visibilitychange', () => {
        if (document.visibilityState === 'visible') void this.checkForUpdate();
      });
    });
  }

  /** La forma de instalar que corresponde; null si no hay ninguna (p. ej. Chromium con la app ya instalada). */
  get guide(): InstallGuide | null {
    if (this.installed) return null;
    return this.deferredPrompt ? 'prompt' : this.fallbackGuide;
  }

  install(): void {
    const promptEvent = this.deferredPrompt;
    if (!promptEvent) {
      this.guideRequested = true;
      return;
    }
    // El diálogo del navegador se puede mostrar una sola vez por evento.
    this.deferredPrompt = null;
    void promptEvent.prompt();
    void promptEvent.userChoice.then(({ outcome }) => {
      writeStorage(outcomeKey, outcome);
      if (outcome === 'accepted') this.installed = true;
      else this.dismiss();
    });
  }

  /** Muestra la guía de instalación aunque el usuario la haya cerrado antes. */
  openGuide(): void {
    this.guideRequested = true;
  }

  /** Cierra la guía y no la vuelve a mostrar sola en las próximas visitas. */
  dismiss(): void {
    this.dismissed = true;
    this.guideRequested = false;
    writeStorage(dismissedKey, '1');
  }

  applyUpdate(): void {
    void this.swUpdate.activateUpdate().then(() => window.location.reload());
  }

  private async checkForUpdate(): Promise<void> {
    try {
      await this.swUpdate.checkForUpdate();
    } catch {
      // Sin conexión o servidor caído: se vuelve a intentar en la próxima revisión.
    }
  }
}

function isStandalone(): boolean {
  const iosStandalone = (navigator as Navigator & { standalone?: boolean }).standalone === true;
  return iosStandalone || window.matchMedia('(display-mode: standalone)').matches;
}

/**
 * Para los navegadores sin beforeinstallprompt, el user agent solo elige qué instrucciones mostrar.
 * En Chromium se espera el evento: si no llega, la app no se puede instalar ahí (o ya está instalada).
 */
function detectFallbackGuide(): InstallGuide | null {
  const ua = navigator.userAgent;
  const isIos = /iPad|iPhone|iPod/.test(ua) || (/Macintosh/.test(ua) && navigator.maxTouchPoints > 1);
  const isSafari = /Safari/.test(ua) && !/Chrome|CriOS|Chromium|Edg|OPR|FxiOS|Firefox|SamsungBrowser/.test(ua);
  if (isIos) return isSafari ? 'ios-safari' : 'ios-other';
  if ('onbeforeinstallprompt' in window) return null;
  if (/Android/.test(ua) && /Firefox/.test(ua)) return 'firefox-android';
  if (isSafari && /Macintosh/.test(ua)) return 'mac-safari';
  return 'unsupported';
}

function readStorage(key: string): string | null {
  try {
    return localStorage.getItem(key);
  } catch {
    return null;
  }
}

function writeStorage(key: string, value: string): void {
  try {
    localStorage.setItem(key, value);
  } catch {
    // Sin almacenamiento (modo privado o bloqueado): la elección vale solo para esta visita.
  }
}
