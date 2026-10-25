import { DatePipe, formatCurrency } from '@angular/common';
import { Component, Inject, Input, LOCALE_ID, OnChanges, ViewChild } from '@angular/core';
import { Chart, ChartDataSets, ChartLegendLabelItem, ChartOptions, ChartTooltipItem } from 'chart.js';
import { BaseChartDirective } from 'ng2-charts';
import { Closure } from '../../../closures/models/closure.model';
import {
  average,
  ChartSeries,
  closureLabel,
  closureTooltipTitle,
  EXPENSE_COLOR,
  INCOME_COLOR,
  legendLabelsInDatasetOrder,
  MAX_CLOSURES,
  NET_FLOW_COLOR,
  closedClosures
} from '../closure-chart.utils';

/** Cierres que promedia cada tendencia. */
const SHORT_TERM_WINDOW = 3;
const LONG_TERM_WINDOW = 6;

// Neutros, así se distinguen de las barras con cualquier indicador.
const SHORT_TERM_COLOR = '#7c3aed';
const LONG_TERM_COLOR = '#334155';

export type TrendMetric = 'incomes' | 'expenses' | 'result';

interface TrendMetricOption {
  key: TrendMetric;
  label: string;
  color: string;
  value: (closure: Closure) => number;
}

/**
 * Un indicador de los últimos cierres (barras) con sus tendencias de corto y largo plazo (medias móviles). Se elige
 * de a un indicador para no cargar el gráfico de líneas.
 */
@Component({
  selector: 'app-trends-chart',
  templateUrl: './trends-chart.component.html',
  styleUrls: ['./trends-chart.component.scss']
})
export class TrendsChartComponent implements OnChanges {
  /** Cierres de la empresa en cualquier orden; se muestran los últimos MAX_CLOSURES cerrados. */
  @Input() closures: Closure[] = [];

  readonly metrics: TrendMetricOption[] = [
    { key: 'incomes', label: 'Ingresos', color: INCOME_COLOR, value: (closure) => closure.incomes ?? 0 },
    { key: 'expenses', label: 'Egresos', color: EXPENSE_COLOR, value: (closure) => closure.expenses ?? 0 },
    { key: 'result', label: 'Resultado final', color: NET_FLOW_COLOR, value: (closure) => closure.result ?? 0 }
  ];
  metric: TrendMetricOption = this.metrics[0];

  /**
   * Series con casilla de verificación, en el orden de los datasets: el indicador elegido (toma su nombre y color) y las
   * dos tendencias. Lo que se oculta sigue oculto al cambiar de indicador.
   */
  readonly series: ChartSeries[] = [
    { label: this.metric.label, color: this.metric.color, swatch: 'bar', visible: true },
    { label: `Corto plazo (${SHORT_TERM_WINDOW} cierres)`, color: SHORT_TERM_COLOR, swatch: 'line', visible: true },
    { label: `Largo plazo (${LONG_TERM_WINDOW} cierres)`, color: LONG_TERM_COLOR, swatch: 'dashed', visible: true }
  ];

  shown: Closure[] = [];
  labels: string[] = [];
  datasets: ChartDataSets[] = [];
  readonly options: ChartOptions;

  /** Todos los cierres cerrados: las medias móviles de los primeros cierres mostrados usan también los anteriores. */
  private closed: Closure[] = [];

  @ViewChild(BaseChartDirective) private chart?: BaseChartDirective;

  constructor(@Inject(LOCALE_ID) private readonly locale: string) {
    const datePipe = new DatePipe(locale);
    const component = this;
    this.options = {
      responsive: true,
      maintainAspectRatio: false,
      legend: {
        position: 'bottom',
        labels: { usePointStyle: true, generateLabels: legendLabelsInDatasetOrder },
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
          title: (items: ChartTooltipItem[]) => closureTooltipTitle(this.shown[items[0]?.index ?? -1], datePipe),
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
    this.closed = closedClosures(this.closures);
    this.shown = this.closed.slice(-MAX_CLOSURES);
    this.labels = this.shown.map(closureLabel);
    this.buildDatasets();
  }

  selectMetric(metric: TrendMetricOption): void {
    if (metric === this.metric) return;
    this.metric = metric;
    this.buildDatasets();
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

  private buildDatasets(): void {
    const { label, color, value } = this.metric;
    const [metricSeries, shortTermSeries, longTermSeries] = this.series;
    metricSeries.label = label;
    metricSeries.color = color;
    // Las líneas van con order 0 para dibujarse por encima de las barras.
    this.datasets = [
      {
        label,
        hidden: !metricSeries.visible,
        data: this.shown.map(value),
        order: 1,
        backgroundColor: color,
        hoverBackgroundColor: color,
        borderWidth: 0,
        barPercentage: 0.8,
        categoryPercentage: 0.7
      },
      this.trendLine(shortTermSeries, this.movingAverage(SHORT_TERM_WINDOW, value), []),
      this.trendLine(longTermSeries, this.movingAverage(LONG_TERM_WINDOW, value), [6, 4])
    ];
  }

  private trendLine(series: ChartSeries, data: (number | null)[], borderDash: number[]): ChartDataSets {
    const color = series.color;
    return {
      type: 'line',
      label: series.label,
      hidden: !series.visible,
      data: data as number[],
      order: 0,
      fill: false,
      lineTension: 0,
      spanGaps: false,
      borderColor: color,
      backgroundColor: color,
      pointBackgroundColor: color,
      borderDash,
      borderWidth: 2,
      pointRadius: 2,
      pointHoverRadius: 4
    };
  }

  /**
   * Media móvil de cada cierre mostrado: el promedio de ese cierre y los anteriores hasta completar la ventana. Es null
   * para los cierres que todavía no tienen suficientes anteriores.
   */
  private movingAverage(window: number, value: (closure: Closure) => number): (number | null)[] {
    const offset = this.closed.length - this.shown.length;
    return this.shown.map((_, index) => {
      const end = offset + index + 1;
      return end >= window ? average(this.closed.slice(end - window, end).map(value)) : null;
    });
  }

  private money(value: number, digitsInfo: string): string {
    return formatCurrency(value, this.locale, '$', 'USD', digitsInfo);
  }
}
