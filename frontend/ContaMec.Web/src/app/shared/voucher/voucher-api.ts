import { Observable } from 'rxjs';

export interface VoucherInfo {
  fileName: string;
  isImage: boolean;
}

export interface VoucherUrl extends VoucherInfo {
  /** URL firmada: vence a los pocos minutos, se pide cada vez que se abre. */
  url: string;
}

/** Operaciones del comprobante de un registro (egreso, más adelante ingreso); las arma el servicio de cada módulo. */
export interface VoucherApi {
  upload(file: File): Observable<VoucherInfo>;
  remove(): Observable<void>;
  getUrl(): Observable<VoucherUrl>;
}
