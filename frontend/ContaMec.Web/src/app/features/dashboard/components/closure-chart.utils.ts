import { DatePipe } from '@angular/common';
import { Closure } from '../../closures/models/closure.model';

// Lo comparten los gráficos de cierres del grupo Estadisticas, para que rotulen y expliquen cada cierre igual.

/** Cantidad de cierres que se comparan: los últimos cerrados. */
export const MAX_CLOSURES = 12;

// Mismos colores que _dashboard-colors.scss ($income-color, $expense-color) y que las tarjetas de resultado positivo.
export const INCOME_COLOR = '#0284c7';
export const EXPENSE_COLOR = '#ea580c';
export const NET_FLOW_COLOR = '#15803d';

const MONTH_NAMES = [
  'Enero', 'Febrero', 'Marzo', 'Abril', 'Mayo', 'Junio',
  'Julio', 'Agosto', 'Septiembre', 'Octubre', 'Noviembre', 'Diciembre'
];

/** Una serie del gráfico que se muestra u oculta con su casilla de verificación. */
export interface ChartSeries {
  label: string;
  color: string;
  visible: boolean;
}

/** Cierres cerrados, del más viejo al más nuevo (por Id). El activo queda afuera porque sus importes son parciales. */
export function closedClosures(closures: Closure[] | null | undefined): Closure[] {
  return (closures ?? []).filter((closure) => closure.isClosed).sort((a, b) => a.id - b.id);
}

/** Número de cierre y el mes al que corresponde, por ejemplo "12 - Septiembre/2026". */
export function closureLabel(closure: Closure): string {
  const number = `${closure.closureNumber ?? '#' + closure.id}`;
  const month = closureMonth(closure);
  return month ? `${number} - ${month}` : number;
}

/**
 * Título del tooltip. Primera línea: número y período del cierre. Segunda: fechas de apertura y cierre.
 */
export function closureTooltipTitle(closure: Closure | undefined, datePipe: DatePipe): string | string[] {
  if (!closure) return '';
  const heading = `Cierre ${closureLabel(closure)}${closure.isClosed ? '' : ' (activo)'}`;
  const dates = [
    closure.openDate ? `abierto el ${datePipe.transform(closure.openDate, 'dd/MM/yyyy')}` : '',
    closure.isClosed && closure.closeDate ? `cerrado el ${datePipe.transform(closure.closeDate, 'dd/MM/yyyy')}` : ''
  ].filter((text) => text).join(', ');
  return dates ? [heading, dates.charAt(0).toUpperCase() + dates.slice(1)] : heading;
}

export function average(values: number[]): number | null {
  if (values.length === 0) return null;
  return values.reduce((sum, value) => sum + value, 0) / values.length;
}

/**
 * Mes en que el cierre tuvo más ingresos y egresos (un cierre abierto el 31/08 con casi todo en septiembre es
 * "Septiembre/2026"). Sin movimientos, el mes de apertura.
 */
function closureMonth(closure: Closure): string | null {
  const match = /^(\d{4})-(\d{2})/.exec(closure.predominantMonth ?? closure.openDate ?? '');
  if (!match) return null;
  const name = MONTH_NAMES[Number(match[2]) - 1];
  return name ? `${name}/${match[1]}` : null;
}
