import { Component, ElementRef, EventEmitter, Input, OnChanges, Output, SimpleChanges, ViewChild } from '@angular/core';
import { evaluateAmountExpression, formatAmount, hasPendingOperation } from './amount-expression';

interface CalculatorKey {
  /** Lo que se agrega a la cuenta, o la acción: C (borrar todo), back (borrar uno), = (resolver). */
  value: string;
  text: string;
  label: string;
  kind: 'digit' | 'operator' | 'action' | 'equals';
}

const OPERATORS = ['+', '-', '*', '/'];

/**
 * Campo Monto con calculadora, para PC, tablet y notebook. Se puede escribir la cuenta con el teclado
 * (1500+230*2, 200+10%) o usar el teclado numérico que se abre arriba; el formulario recibe el resultado.
 */
@Component({
  selector: 'app-amount-calculator',
  templateUrl: './amount-calculator.component.html',
  styleUrls: ['./amount-calculator.component.scss']
})
export class AmountCalculatorComponent implements OnChanges {
  @Input() value: number | null = null;
  @Input() inputId = '';
  @Input() placeholder = '0,00';
  @Output() valueChange = new EventEmitter<number | null>();
  @ViewChild('input', { static: true }) private readonly input!: ElementRef<HTMLInputElement>;

  open = false;
  expression = '';
  private lastEmitted: number | null | undefined;
  /** Lo que se ve es un resultado: el próximo número que se marque empieza una cuenta nueva. */
  private showingResult = false;

  readonly keys: CalculatorKey[] = [
    { value: 'C', text: 'C', label: 'Borrar todo', kind: 'action' },
    { value: 'back', text: '⌫', label: 'Borrar último', kind: 'action' },
    { value: '%', text: '%', label: 'Porcentaje', kind: 'operator' },
    { value: '/', text: '÷', label: 'Dividir', kind: 'operator' },
    { value: '7', text: '7', label: '7', kind: 'digit' },
    { value: '8', text: '8', label: '8', kind: 'digit' },
    { value: '9', text: '9', label: '9', kind: 'digit' },
    { value: '*', text: '×', label: 'Multiplicar', kind: 'operator' },
    { value: '4', text: '4', label: '4', kind: 'digit' },
    { value: '5', text: '5', label: '5', kind: 'digit' },
    { value: '6', text: '6', label: '6', kind: 'digit' },
    { value: '-', text: '−', label: 'Restar', kind: 'operator' },
    { value: '1', text: '1', label: '1', kind: 'digit' },
    { value: '2', text: '2', label: '2', kind: 'digit' },
    { value: '3', text: '3', label: '3', kind: 'digit' },
    { value: '+', text: '+', label: 'Sumar', kind: 'operator' },
    { value: '00', text: '00', label: 'Doble cero', kind: 'digit' },
    { value: '0', text: '0', label: '0', kind: 'digit' },
    { value: ',', text: ',', label: 'Coma decimal', kind: 'digit' },
    { value: '=', text: '=', label: 'Resolver', kind: 'equals' }
  ];

  ngOnChanges(changes: SimpleChanges): void {
    // El formulario cambió el monto (se abrió otro registro): se muestra ese valor, salvo que sea el que acaba de calcular.
    if (changes.value && this.value !== this.lastEmitted) {
      this.lastEmitted = this.value;
      this.expression = formatAmount(this.value);
      this.showingResult = true;
    }
  }

  get result(): number | null {
    return evaluateAmountExpression(this.expression);
  }

  /** Hay algo escrito que todavía no da un resultado (por ejemplo "100+"). */
  get incomplete(): boolean {
    return this.expression.trim() !== '' && this.result === null;
  }

  get resultText(): string {
    if (this.incomplete) return 'Cálculo incompleto';
    return `= ${formatAmount(this.result ?? 0)}`;
  }

  openPanel(): void {
    this.open = true;
  }

  /** Al entrar al campo se selecciona el monto, así lo que se escribe lo reemplaza. */
  onFocus(): void {
    this.openPanel();
    this.input.nativeElement.select();
  }

  toggle(): void {
    if (this.open) {
      this.close();
      return;
    }
    this.input.nativeElement.focus();
    this.openPanel();
  }

  /** Al salir del campo se cierra y, si la cuenta da un resultado, queda el resultado. */
  close(): void {
    this.open = false;
    this.resolve();
  }

  onInput(text: string): void {
    this.expression = text;
    this.showingResult = false;
    this.emit();
  }

  onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Escape' && this.open) {
      event.preventDefault();
      this.open = false;
      return;
    }
    // Enter con una cuenta pendiente la resuelve en lugar de guardar el formulario.
    if ((event.key === 'Enter' || event.key === '=') && hasPendingOperation(this.expression)) {
      event.preventDefault();
      this.resolve();
      return;
    }
    if (event.key === '=') event.preventDefault();
  }

  press(key: string): void {
    const startsNumber = /^[0-9,]+$/.test(key);
    const base = this.showingResult && startsNumber ? '' : this.expression;
    this.showingResult = false;
    this.expression = this.apply(base, key);
    if (key === '=') this.resolve();
    this.emit();
    this.openPanel();
    this.input.nativeElement.focus();
  }

  private apply(expression: string, key: string): string {
    if (key === 'C') return '';
    if (key === 'back') return expression.slice(0, -1);
    if (key === '=') return expression;

    const last = expression.slice(-1);
    if (OPERATORS.includes(key)) {
      if (!expression) return key === '-' ? '-' : '';
      // Un operador después de otro lo reemplaza; después de "(" solo se acepta el signo menos.
      if (OPERATORS.includes(last)) return expression.slice(0, -1) + key;
      if (last === '(') return key === '-' ? expression + key : expression;
      return expression + key;
    }
    if (key === '%') {
      return /[0-9)]$/.test(expression) ? expression + key : expression;
    }
    if (key === ',') {
      // Una sola coma por número; si todavía no hay número, arranca en "0,".
      const currentNumber = /[0-9.,]*$/.exec(expression)?.[0] ?? '';
      if (/[.,]/.test(currentNumber)) return expression;
      return expression + (currentNumber ? ',' : '0,');
    }
    // Después de un % o de ")" no puede seguir un número sin operador.
    if (last === '%' || last === ')') return expression;
    return expression + key;
  }

  private resolve(): void {
    const result = this.result;
    if (result === null) return;
    this.expression = formatAmount(result);
    this.showingResult = true;
  }

  private emit(): void {
    const result = this.result;
    if (result === this.lastEmitted) return;
    this.lastEmitted = result;
    this.valueChange.emit(result);
  }
}
