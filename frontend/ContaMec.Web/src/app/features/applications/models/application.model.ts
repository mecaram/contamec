export interface Application {
  id: number;
  name?: string | null;
  documentType?: string | null;
  documentNumber?: string | null;
  active: boolean;
  whatsApp?: string | null;
  email?: string | null;
  address?: string | null;
  locality?: string | null;
  guid?: string | null;
  createdAt?: string | null;
}

/** Resultado de eliminar una aplicación: registros borrados por tabla. */
export interface ApplicationDeleteResult {
  /** Tablas con registros borrados, en el orden en que se borraron; la aplicación va al final. */
  deleted: { table: string; label: string; count: number }[];
  /** Cotizaciones que cargaron sus usuarios: no se borran, solo quedan sin usuario. */
  unlinkedExchangeRates: number;
  total: number;
}

export interface ApplicationActivation {
  email?: string | null;
  emailSent: boolean;
}
