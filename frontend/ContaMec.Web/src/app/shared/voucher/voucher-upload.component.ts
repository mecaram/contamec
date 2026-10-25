import { Component, EventEmitter, Input, OnDestroy, Output } from '@angular/core';
import { Subscription } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { VoucherApi } from './voucher-api';
import { formatFileSize, isImageFile, prepareVoucherFile, validateVoucherFile, VOUCHER_ACCEPT } from './voucher-file';

/**
 * Comprobante opcional de un registro ya guardado: elegir, ver la vista previa y subir; si ya tiene uno, verlo,
 * reemplazarlo o quitarlo. La subida y el borrado los hace el backend con el VoucherApi del módulo.
 */
@Component({
  selector: 'app-voucher-upload',
  templateUrl: './voucher-upload.component.html',
  styleUrls: ['./voucher-upload.component.scss']
})
export class VoucherUploadComponent implements OnDestroy {
  @Input() api: VoucherApi | null = null;
  /** El registro ya tiene comprobante. */
  @Input() hasVoucher = false;
  @Input() inputId = 'voucher-file';
  /** Hay una operación en curso: la pantalla que lo contiene no debería cerrarse. */
  @Output() busyChange = new EventEmitter<boolean>();
  @Output() hasVoucherChange = new EventEmitter<boolean>();

  readonly accept = VOUCHER_ACCEPT;
  /** Archivo elegido (ya comprimido si es imagen), pendiente de subir. */
  selectedFile: File | null = null;
  previewUrl: string | null = null;
  preparing = false;
  uploading = false;
  removing = false;
  opening = false;
  confirmRemove = false;
  errorMessage: string | null = null;
  successMessage: string | null = null;
  private request: Subscription | null = null;

  get busy(): boolean {
    return this.preparing || this.uploading || this.removing || this.opening;
  }

  get selectedSize(): string {
    return this.selectedFile ? formatFileSize(this.selectedFile.size) : '';
  }

  ngOnDestroy(): void {
    this.request?.unsubscribe();
    this.clearPreview();
  }

  async onFileSelected(input: HTMLInputElement): Promise<void> {
    const file = input.files?.[0] ?? null;
    // Se limpia para que volver a elegir el mismo archivo dispare el cambio.
    input.value = '';
    if (!file) return;

    this.resetMessages();
    this.discardSelection();
    const error = validateVoucherFile(file);
    if (error) {
      this.errorMessage = error;
      return;
    }

    this.setBusy(() => (this.preparing = true));
    try {
      const prepared = await prepareVoucherFile(file);
      if (validateVoucherFile(prepared)) {
        this.errorMessage = 'La imagen sigue superando los 10 MB después de comprimirla.';
        return;
      }
      this.selectedFile = prepared;
      this.previewUrl = isImageFile(prepared) ? URL.createObjectURL(prepared) : null;
    } catch {
      this.errorMessage = 'No se pudo leer el archivo. Pruebe con otro.';
    } finally {
      this.setBusy(() => (this.preparing = false));
    }
  }

  upload(): void {
    if (!this.api || !this.selectedFile || this.busy) return;

    const replacing = this.hasVoucher;
    this.resetMessages();
    this.setBusy(() => (this.uploading = true));
    this.request = this.api
      .upload(this.selectedFile)
      .pipe(finalize(() => this.setBusy(() => (this.uploading = false))))
      .subscribe({
        next: () => {
          this.discardSelection();
          this.setHasVoucher(true);
          this.successMessage = replacing ? 'Comprobante reemplazado correctamente.' : 'Comprobante adjuntado correctamente.';
        },
        error: (err) => (this.errorMessage = err?.error?.message ?? 'No se pudo subir el comprobante. Intente nuevamente.')
      });
  }

  cancelSelection(): void {
    if (this.uploading) return;
    this.discardSelection();
  }

  /** La ventana se abre antes de pedir la URL firmada para que el navegador no la bloquee. */
  open(): void {
    if (!this.api || this.busy) return;

    this.resetMessages();
    const target = window.open('', '_blank');
    this.setBusy(() => (this.opening = true));
    this.request = this.api
      .getUrl()
      .pipe(finalize(() => this.setBusy(() => (this.opening = false))))
      .subscribe({
        next: (voucher) => {
          if (target) target.location.href = voucher.url;
          else window.location.assign(voucher.url);
        },
        error: (err) => {
          target?.close();
          this.errorMessage = err?.error?.message ?? 'No se pudo abrir el comprobante. Intente nuevamente.';
        }
      });
  }

  askRemove(): void {
    if (this.busy) return;
    this.resetMessages();
    this.confirmRemove = true;
  }

  cancelRemove(): void {
    this.confirmRemove = false;
  }

  remove(): void {
    if (!this.api || this.busy) return;

    this.resetMessages();
    this.setBusy(() => (this.removing = true));
    this.request = this.api
      .remove()
      .pipe(finalize(() => this.setBusy(() => (this.removing = false))))
      .subscribe({
        next: () => {
          this.confirmRemove = false;
          this.setHasVoucher(false);
          this.successMessage = 'Comprobante quitado.';
        },
        error: (err) => {
          this.confirmRemove = false;
          this.errorMessage = err?.error?.message ?? 'No se pudo quitar el comprobante. Intente nuevamente.';
        }
      });
  }

  private setHasVoucher(value: boolean): void {
    this.hasVoucher = value;
    this.hasVoucherChange.emit(value);
  }

  private setBusy(change: () => void): void {
    const before = this.busy;
    change();
    if (before !== this.busy) this.busyChange.emit(this.busy);
  }

  private discardSelection(): void {
    this.selectedFile = null;
    this.clearPreview();
  }

  private clearPreview(): void {
    if (this.previewUrl) URL.revokeObjectURL(this.previewUrl);
    this.previewUrl = null;
  }

  private resetMessages(): void {
    this.errorMessage = null;
    this.successMessage = null;
  }
}
