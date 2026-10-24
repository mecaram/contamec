import { DatePipe, formatCurrency } from '@angular/common';
import { Component, Inject, Input, LOCALE_ID, OnChanges } from '@angular/core';
import { ChartDataSets, ChartOptions, ChartTooltipItem } from 'chart.js';
import { Closure } from '../../../closures/models/closure.model';

/** Cantidad de cierres que se comparan: el activo y los anteriores. */
const MAX_CLOSURES = 12;

// Mismos colores que _dashboard-colors.scss ($income-color, $expense-color) y que las tarjetas de resultado positivo.
const INCOME_COLOR = '#0284c7';
const EXPENSE_COLOR = '#ea580c';
const NET_FLOW_COLOR = '#15803d';

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
  labels: string[] = [];
  datasets: ChartDataSets[] = [];
  readonly options: ChartOptions;

  constructor(@Inject(LOCALE_ID) private readonly locale: string) {
    const datePipe = new DatePipe(locale);
    this.options = {
      responsive: true,
      maintainAspectRatio: false,
      legend: { position: 'bottom', labels: { usePointStyle: true } },
      tooltips: {
        mode: 'index',
        intersect: false,
        callbacks: {
          title: (items: ChartTooltipItem[]) => {
            const closure = this.shown[items[0]?.index ?? -1];
            if (!closure) return '';
            const opened = closure.openDate ? `, abierto el ${datePipe.transform(closure.openDate, 'dd/MM/yyyy')}` : '';
            return `Cierre ${this.closureLabel(closure)}${opened}${closure.isClosed ? '' : ' (activo)'}`;
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
    this.shown = [...(this.closures ?? [])].sort((a, b) => b.id - a.id).slice(0, MAX_CLOSURES).reverse();
    this.labels = this.shown.map((closure) => this.closureLabel(closure));

    // La línea va con order 0 para dibujarse por encima de las barras.
    this.datasets = [
      {
        type: 'line',
        label: 'Flujo neto',
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
        label: 'Ingresos',
        data: this.shown.map((closure) => closure.incomes ?? 0),
        order: 1,
        backgroundColor: INCOME_COLOR,
        hoverBackgroundColor: INCOME_COLOR,
        borderWidth: 0,
        barPercentage: 0.8,
        categoryPercentage: 0.7
      },
      {
        label: 'Egresos',
        data: this.shown.map((closure) => closure.expenses ?? 0),
        order: 1,
        backgroundColor: EXPENSE_COLOR,
        hoverBackgroundColor: EXPENSE_COLOR,
        borderWidth: 0,
        barPercentage: 0.8,
        categoryPercentage: 0.7
      }
    ];
  }

  private closureLabel(closure: Closure): string {
    return `${closure.closureNumber ?? '#' + closure.id}`;
  }

  private money(value: number, digitsInfo: string): string {
    return formatCurrency(value, this.locale, '$', 'USD', digitsInfo);
  }
}
