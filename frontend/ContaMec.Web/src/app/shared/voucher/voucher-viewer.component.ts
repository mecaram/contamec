import { Component, EventEmitter, HostListener, Input, OnDestroy, OnInit, Output } from '@angular/core';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { Subscription } from 'rxjs';
import { VoucherApi } from './voucher-api';

/**
 * Ventana modal con el comprobante de un registro: la imagen o el PDF dentro de la ventana.
 * El archivo se baja con la URL firmada y se muestra desde una URL local (blob), así no depende de que
 * Storage permita mostrarse dentro de otra página; si no se puede bajar, se usa la URL firmada directamente.
 */
@Component({
  selector: 'app-voucher-viewer',
  templateUrl: './voucher-viewer.component.html',
  styleUrls: ['./voucher-viewer.component.scss']
})
export class VoucherViewerComponent implements OnInit, OnDestroy {
  @Input() api: VoucherApi | null = null;
  @Input() title = 'Comprobante';
  @Input() subtitle: string | null = null;
  /** Nombre del archivo al descargar, sin extensión. */
  @Input() downloadName = 'Comprobante';
  @Output() closed = new EventEmitter<void>();

  loading = true;
  errorMessage: string | null = null;
  isImage = false;
  /** URL que se muestra y se descarga: local (blob) o, si no se pudo bajar, la firmada. */
  fileUrl: string | null = null;
  safeFileUrl: SafeResourceUrl | null = null;
  private extension = '';
  private objectUrl: string | null = null;
  private request: Subscription | null = null;
  private destroyed = false;

  constructor(private readonly sanitizer: DomSanitizer) {}

  get downloadFileName(): string {
    return `${this.downloadName}${this.extension}`;
  }

  ngOnInit(): void {
    if (!this.api) {
      this.fail('No se pudo abrir el comprobante.');
      return;
    }

    this.request = this.api.getUrl().subscribe({
      next: (voucher) => {
        this.isImage = voucher.isImage;
        const dot = voucher.fileName.lastIndexOf('.');
        this.extension = dot >= 0 ? voucher.fileName.substring(dot) : '';
        this.loadFile(voucher.url);
      },
      error: (err) => this.fail(err?.error?.message ?? 'No se pudo abrir el comprobante. Intente nuevamente.')
    });
  }

  ngOnDestroy(): void {
    this.destroyed = true;
    this.request?.unsubscribe();
    if (this.objectUrl) URL.revokeObjectURL(this.objectUrl);
  }

  @HostListener('document:keydown.escape')
  close(): void {
    this.closed.emit();
  }

  openInNewTab(): void {
    if (this.fileUrl) window.open(this.fileUrl, '_blank', 'noopener');
  }

  download(): void {
    if (!this.fileUrl) return;
    const link = document.createElement('a');
    link.href = this.fileUrl;
    link.download = this.downloadFileName;
    link.rel = 'noopener';
    document.body.appendChild(link);
    link.click();
    link.remove();
  }

  private async loadFile(signedUrl: string): Promise<void> {
    let url = signedUrl;
    try {
      const response = await fetch(signedUrl);
      if (!response.ok) throw new Error(`HTTP ${response.status}`);
      const blob = await response.blob();
      if (this.destroyed) return;
      this.objectUrl = URL.createObjectURL(blob);
      url = this.objectUrl;
    } catch {
      // Sin el archivo local se muestra desde Storage; la URL firmada vence a los pocos minutos.
    }
    if (this.destroyed) return;
    this.fileUrl = url;
    this.safeFileUrl = this.sanitizer.bypassSecurityTrustResourceUrl(url);
    this.loading = false;
  }

  private fail(message: string): void {
    this.loading = false;
    this.errorMessage = message;
  }
}
