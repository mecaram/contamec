export interface DashboardItem {
  name: string;
  icon: string;
  route: string;
  /** Modificador que reemplaza el color de icono del grupo (p. ej. 'incomes' → dashboard-card__icon--incomes). */
  iconTone?: 'incomes' | 'expenses';
  superAdminOnly?: boolean;
}

export interface DashboardCategory {
  title: string;
  /** Define los colores del grupo (dashboard__section--{tone}); no depende de la posición en el menú. */
  tone: 'daily' | 'files' | 'reports';
  items: DashboardItem[];
  /** Grupo definido pero oculto en el menú hasta que tenga contenido. */
  hidden?: boolean;
}
