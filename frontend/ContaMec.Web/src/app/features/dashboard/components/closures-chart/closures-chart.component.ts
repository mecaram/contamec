import { DatePipe, formatCurrency } from '@angular/common';
import { Component, Inject, Input, LOCALE_ID, OnChanges, ViewChild } from '@angular/core';
import { Chart, ChartDataSets, ChartLegendLabelItem, ChartOptions, ChartTooltipItem } from 'chart.js';
import { BaseChartDirective } from 'ng2-charts';
import { Closure } from '../../../closures/models/closure.model';

/** Cantidad de cierres que se comparan: el activo y los anteriores. */
const MAX_CLOSURES = 12;

// Mismos colores que _dashboard-colors.scss ($income-color, $expense-color) y que las tarjetas de resultado positivo.
const INCOME_COLOR = '#0284c7';
const EXPENSE_COLOR = '#ea580c';
const NET_FLOW_COLOR = '#15803d';

const MONTH_NAMES = [
  'Enero', 'Febrero', 'Marzo', 'Abril', 'Mayo', 'Junio',
  'Julio', 'Agosto', 'Septiembre', 'Octubre', 'Noviembre', 'Diciembre'
];

/** Una serie del gráfico que se muestra u oculta con su casilla de verificación o desde la leyenda. */
export interface ChartSeries {
  label: string;
  color: string;
  /** Muestra del trazo junto a la casilla: barra, línea continua o línea punteada. */
  swatch: 'bar' | 'line' | 'dashed';
  visible: boolean;
}

/** Ingresos y egresos (barras) y flujo neto (línea) de los últimos cierres, del más viejo al más nuevo. */
@Component({
  selector: 'app-closures-chart',
  templateUrl: './closures-chart.component.html',
  styleUrls: ['./closures-chart.component.scss']
})
export class ClosuresChartComponent implements OnChanges {
  /** Cierres de la empresa en cualquier orden; se toman los últimos MAX_CLOSURES por Id. */
  @Input() closures: Closure[] = [];

  shown: Closure[] = [];
  /** Si entre los cierres mostrados está el activo, que queda fuera de los promedios. */
  hasActive = false;
  labels: string[] = [];
  datasets: ChartDataSets[] = [];
  readonly options: ChartOptions;

  /** Todas las series, en el mismo orden que los datasets (la posición es el índice del dataset). */
  readonly series: ChartSeries[] = [
    { label: 'Flujo neto', color: NET_FLOW_COLOR, swatch: 'line', visible: true },
    { label: 'Ingresos', color: INCOME_COLOR, swatch: 'bar', visible: true },
    { label: 'Egresos', color: EXPENSE_COLOR, swatch: 'bar', visible: true },
    { label: 'Promedio ingresos', color: INCOME_COLOR, swatch: 'dashed', visible: true },
    { label: 'Promedio egresos', color: EXPENSE_COLOR, swatch: 'dashed', visible: true }
  ];
  /** Si hay promedios para mostrar (hace falta al menos un cierre cerrado). */
  hasReferences = false;

  @ViewChild(BaseChartDirective) private chart?: BaseChartDirective;

  constructor(@Inject(LOCALE_ID) private readonly locale: string) {
    const datePipe = new DatePipe(locale);
    const component = this;
    this.options = {
      responsive: true,
      maintainAspectRatio: false,
      legend: {
        position: 'bottom',
        labels: {
          usePointStyle: true,
          // Chart.js ordena la leyenda por `order` (el de dibujo); acá va en el orden de las series, como las casillas.
          generateLabels: (chart: Chart) =>
            (Chart.defaults.global.legend?.labels?.generateLabels?.(chart) ?? []).sort(
              (a, b) => (a.datasetIndex ?? 0) - (b.datasetIndex ?? 0)
            )
        },
        // Hace lo de siempre (mostrar u ocultar la serie) y deja las casillas de verificación en el mismo estado.
        onClick(this: any, event: MouseEvent, item: ChartLegendLabelItem) {
          Chart.defaults.global.legend?.onClick?.call(this, event, item);
          component.syncSeries();
        }
      },
      tooltips: {
        mode: 'index',
        intersect: false,
        itemSort: (a, b) => (a.datasetIndex ?? 0) - (b.datasetIndex ?? 0),
        callbacks: {
          // Primera línea: número y período del cierre. Segunda: fechas de apertura y cierre.
          title: (items: ChartTooltipItem[]) => {
            const closure = this.shown[items[0]?.index ?? -1];
            if (!closure) return '';
            const heading = `Cierre ${this.closureLabel(closure)}${closure.isClosed ? '' : ' (activo)'}`;
            const dates = [
              closure.openDate ? `abierto el ${datePipe.transform(closure.openDate, 'dd/MM/yyyy')}` : '',
              closure.isClosed && closure.closeDate ? `cerrado el ${datePipe.transform(closure.closeDate, 'dd/MM/yyyy')}` : ''
            ].filter((text) => text).join(', ');
            return dates ? [heading, dates.charAt(0).toUpperCase() + dates.slice(1)] : heading;
          },
          label: (item: ChartTooltipItem, data) =>
            `${data.datasets?.[item.datasetIndex ?? 0]?.label}: ${this.money(Number(item.value), '1.2-2')}`
        }
      },
      scales: {
        xAxes: [{ gridLines: { display: false } }],
        yAxes: [{ ticks: { beginAtZero: true, callback: (value) => this.money(Number(value), '1.0-0') } }]
      }
    };
  }

  ngOnChanges(): void {
    this.shown = [...(this.closures ?? [])].sort((a, b) => a.id - b.id).slice(-MAX_CLOSURES);
    this.hasActive = this.shown.some((closure) => !closure.isClosed);
    this.labels = this.shown.map((closure) => this.closureLabel(closure));

    // El cierre activo tiene importes parciales: no entra en los promedios.
    const closed = this.shown.filter((closure) => closure.isClosed);
    const incomeAverage = this.average(closed.map((closure) => closure.incomes ?? 0));
    const expenseAverage = this.average(closed.map((closure) => closure.expenses ?? 0));

    // La línea va con order 0 para dibujarse por encima de las barras.
    const [netFlowSeries, incomeSeries, expenseSeries] = this.series;
    this.datasets = [
      {
        type: 'line',
        label: netFlowSeries.label,
        hidden: !netFlowSeries.visible,
        data: this.shown.map((closure) => closure.netFlow ?? 0),
        order: 0,
        fill: false,
        lineTension: 0,
        borderColor: NET_FLOW_COLOR,
        backgroundColor: NET_FLOW_COLOR,
        pointBackgroundColor: NET_FLOW_COLOR,
        pointRadius: 4,
        pointHoverRadius: 6,
        borderWidth: 2
      },
      {
        label: incomeSeries.label,
        hidden: !incomeSeries.visible,
        data: this.shown.map((closure) => closure.incomes ?? 0),
        order: 1,
        backgroundColor: INCOME_COLOR,
        hoverBackgroundColor: INCOME_COLOR,
        borderWidth: 0,
        barPercentage: 0.8,
        categoryPercentage: 0.7
      },
      {
        label: expenseSeries.label,
        hidden: !expenseSeries.visible,
        data: this.shown.map((closure) => closure.expenses ?? 0),
        order: 1,
        backgroundColor: EXPENSE_COLOR,
        hoverBackgroundColor: EXPENSE_COLOR,
        borderWidth: 0,
        barPercentage: 0.8,
        categoryPercentage: 0.7
      },
      ...this.referenceLines(incomeAverage, expenseAverage)
    ];
  }

  /**
   * Promedios de ingresos y egresos, como líneas horizontales punteadas. Se ocultan con su casilla de verificación o
   * desde la leyenda. Sin cierres cerrados no hay nada que promediar y no se agregan.
   */
  private referenceLines(incomeAverage: number | null, expenseAverage: number | null): ChartDataSets[] {
    this.hasReferences = incomeAverage !== null && expenseAverage !== null;
    if (incomeAverage === null || expenseAverage === null) return [];
    const [, , , incomeAverageLine, expenseAverageLine] = this.series;
    return [this.averageLine(incomeAverageLine, incomeAverage), this.averageLine(expenseAverageLine, expenseAverage)];
  }

  /** Series con casilla de verificación: las que están en el gráfico (los promedios solo si hay cierres cerrados). */
  get toggles(): ChartSeries[] {
    return this.series.slice(0, this.datasets.length);
  }

  /** Casilla de verificación de una serie. */
  toggleSeries(item: ChartSeries): void {
    item.visible = !item.visible;
    const index = this.series.indexOf(item);
    const dataset = this.datasets[index];
    if (dataset) dataset.hidden = !item.visible;
    this.chart?.hideDataset(index, !item.visible);
  }

  /** Copia a las casillas lo que se mostró u ocultó desde la leyenda del gráfico. */
  private syncSeries(): void {
    const chart = this.chart?.chart;
    if (!chart) return;
    this.series.forEach((item, index) => {
      if (index < (chart.data.datasets?.length ?? 0)) item.visible = chart.isDatasetVisible(index);
    });
  }

  private averageLine(reference: ChartSeries, value: number): ChartDataSets {
    const color = reference.color;
    return {
      type: 'line',
      label: reference.label,
      data: this.shown.map(() => value),
      order: 0,
      hidden: !reference.visible,
      fill: false,
      lineTension: 0,
      borderColor: color,
      backgroundColor: color,
      borderDash: [6, 4],
      borderWidth: 1.5,
      pointRadius: 0,
      pointHoverRadius: 0,
      pointHitRadius: 0
    };
  }

  private average(values: number[]): number | null {
    if (values.length === 0) return null;
    return values.reduce((sum, value) => sum + value, 0) / values.length;
  }

  /** Número de cierre y el mes al que corresponde, por ejemplo "12 - Septiembre/2026". */
  private closureLabel(closure: Closure): string {
    const number = `${closure.closureNumber ?? '#' + closure.id}`;
    const month = this.closureMonth(closure);
    return month ? `${number} - ${month}` : number;
  }

  /**
   * Mes en que el cierre tuvo más ingresos y egresos (un cierre abierto el 31/08 con casi todo en septiembre es
   * "Septiembre/2026"). Sin movimientos, el mes de apertura.
   */
  private closureMonth(closure: Closure): string | null {
    const match = /^(\d{4})-(\d{2})/.exec(closure.predominantMonth ?? closure.openDate ?? '');
    if (!match) return null;
    const name = MONTH_NAMES[Number(match[2]) - 1];
    return name ? `${name}/${match[1]}` : null;
  }

  private money(value: number, digitsInfo: string): string {
    return formatCurrency(value, this.locale, '$', 'USD', digitsInfo);
  }
}
