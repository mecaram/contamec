import { Component, Input, OnChanges } from '@angular/core';
import { ChartDataSets, ChartOptions, ChartTooltipItem } from 'chart.js';
import { Label } from 'ng2-charts';
import { Closure } from '../../../closures/models/closure.model';

// Mismos colores que las tarjetas de indicadores (_dashboard-colors.scss y .indicator-card--positive).
const INCOME_COLOR = '#0284c7';
const EXPENSE_COLOR = '#ea580c';
const NET_FLOW_COLOR = '#15803d';

const amountFormat = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', minimumFractionDigits: 2 });
const axisFormat = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 });

/** Ingresos y egresos de los últimos cierres en barras, con el flujo neto en una línea. */
@Component({
  selector: 'app-closures-chart',
  templateUrl: './closures-chart.component.html',
  styleUrls: ['./closures-chart.component.scss']
})
export class ClosuresChartComponent implements OnChanges {
  /** Cierres ordenados del más viejo al más nuevo. */
  @Input() closures: Closure[] = [];

  labels: Label[] = [];
  datasets: ChartDataSets[] = [];

  readonly options: ChartOptions = {
    responsive: true,
    maintainAspectRatio: false,
    legend: { position: 'bottom', labels: { usePointStyle: true } },
    tooltips: {
      mode: 'index',
      intersect: false,
      callbacks: {
        label: (item: ChartTooltipItem, data) =>
          `${data.datasets?.[item.datasetIndex ?? 0]?.label}: ${amountFormat.format(Number(item.value))}`
      }
    },
    scales: {
      xAxes: [{ gridLines: { display: false } }],
      yAxes: [{ ticks: { beginAtZero: true, callback: (value) => axisFormat.format(Number(value)) } }]
    }
  };

  ngOnChanges(): void {
    this.labels = this.closures.map((closure) => {
      const number = `N° ${closure.closureNumber ?? '#' + closure.id}`;
      return closure.isClosed ? number : [number, '(activo)'];
    });

    this.datasets = [
      {
        label: 'Ingresos',
        data: this.closures.map((closure) => closure.incomes ?? 0),
        backgroundColor: INCOME_COLOR,
        hoverBackgroundColor: INCOME_COLOR,
        borderColor: INCOME_COLOR
      },
      {
        label: 'Egresos',
        data: this.closures.map((closure) => closure.expenses ?? 0),
        backgroundColor: EXPENSE_COLOR,
        hoverBackgroundColor: EXPENSE_COLOR,
        borderColor: EXPENSE_COLOR
      },
      {
        type: 'line',
        label: 'Flujo neto',
        data: this.closures.map((closure) => closure.netFlow ?? 0),
        borderColor: NET_FLOW_COLOR,
        backgroundColor: NET_FLOW_COLOR,
        pointBackgroundColor: NET_FLOW_COLOR,
        pointBorderColor: '#ffffff',
        pointRadius: 4,
        pointHoverRadius: 6,
        fill: false,
        lineTension: 0
      }
    ];
  }
}
