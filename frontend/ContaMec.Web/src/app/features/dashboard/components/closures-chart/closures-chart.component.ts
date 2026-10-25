import { DatePipe, formatCurrency } from '@angular/common';
import { Component, Inject, Input, LOCALE_ID, OnChanges, ViewChild } from '@angular/core';
import { ChartDataSets, ChartOptions, ChartTooltipItem } from 'chart.js';
import { BaseChartDirective } from 'ng2-charts';
import { Closure } from '../../../closures/models/closure.model';
import {
  average,
  ChartSeries,
  closureLabel,
  closureTooltipTitle,
  EXPENSE_COLOR,
  INCOME_COLOR,
  MAX_CLOSURES,
  NET_FLOW_COLOR,
  closedClosures
} from '../closure-chart.utils';

/** Ingresos y egresos (barras) y flujo neto (línea) de los últimos cierres, del más viejo al más nuevo. */
@Component({
  selector: 'app-closures-chart',
  templateUrl: './closures-chart.component.html',
  styleUrls: ['./closures-chart.component.scss']
})
export class ClosuresChartComponent implements OnChanges {
  /** Cierres de la empresa en cualquier orden; se muestran los últimos MAX_CLOSURES cerrados. */
  @Input() closures: Closure[] = [];

  shown: Closure[] = [];
  labels: string[] = [];
  datasets: ChartDataSets[] = [];
  readonly options: ChartOptions;

  /** Todas las series, en el mismo orden que los datasets (la posición es el índice del dataset). */
  readonly series: ChartSeries[] = [
    { label: 'Flujo neto', color: NET_FLOW_COLOR, visible: true },
    { label: 'Ingresos', color: INCOME_COLOR, visible: true },
    { label: 'Egresos', color: EXPENSE_COLOR, visible: true },
    { label: 'Promedio ingresos', color: INCOME_COLOR, visible: true },
    { label: 'Promedio egresos', color: EXPENSE_COLOR, visible: true }
  ];
  /** Si hay promedios para mostrar (hace falta al menos un cierre cerrado). */
  hasReferences = false;

  @ViewChild(BaseChartDirective) private chart?: BaseChartDirective;

  constructor(@Inject(LOCALE_ID) private readonly locale: string) {
    const datePipe = new DatePipe(locale);
    this.options = {
      responsive: true,
      maintainAspectRatio: false,
      // La leyenda es la del template: cada serie con su casilla de verificación al lado.
      legend: { display: false },
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
    this.shown = closedClosures(this.closures).slice(-MAX_CLOSURES);
    this.labels = this.shown.map(closureLabel);

    const incomeAverage = average(this.shown.map((closure) => closure.incomes ?? 0));
    const expenseAverage = average(this.shown.map((closure) => closure.expenses ?? 0));

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
   * Promedios de ingresos y egresos, como líneas horizontales punteadas. Se ocultan con su casilla de verificación. Sin cierres cerrados no hay nada que promediar y no se agregan.
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

  private money(value: number, digitsInfo: string): string {
    return formatCurrency(value, this.locale, '$', 'USD', digitsInfo);
  }
}
