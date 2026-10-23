/**
 * Cuentas del campo Monto: + − × ÷, paréntesis y %. No usa eval.
 * El % funciona como en una calculadora: 200 + 10% = 220; 200 × 10% = 20.
 */

type Token = { kind: 'number'; value: number } | { kind: 'op'; value: string };

interface Operand {
  value: number;
  /** El operando terminó en %: el que suma o resta lo toma como porcentaje del valor anterior. */
  percent: boolean;
}

const OPERATOR_ALIASES: Record<string, string> = {
  '+': '+',
  '-': '-',
  '−': '-',
  '*': '*',
  x: '*',
  X: '*',
  '×': '*',
  '/': '/',
  '÷': '/',
  '(': '(',
  ')': ')',
  '%': '%'
};

/** Resultado redondeado a 2 decimales; null si la cuenta está vacía, incompleta o divide por cero. */
export function evaluateAmountExpression(text: string | null | undefined): number | null {
  const tokens = tokenize(text ?? '');
  if (!tokens || tokens.length === 0) return null;

  let position = 0;
  const peek = (): Token | undefined => tokens[position];
  const takeOp = (...ops: string[]): string | null => {
    const token = peek();
    if (token?.kind === 'op' && ops.includes(token.value)) {
      position++;
      return token.value;
    }
    return null;
  };

  const parseExpression = (): Operand | null => {
    const first = parseTerm();
    if (!first) return null;
    let value = first.percent ? first.value / 100 : first.value;
    let op: string | null;
    while ((op = takeOp('+', '-'))) {
      const right = parseTerm();
      if (!right) return null;
      const amount = right.percent ? (value * right.value) / 100 : right.value;
      value = op === '+' ? value + amount : value - amount;
    }
    return { value, percent: false };
  };

  const parseTerm = (): Operand | null => {
    const first = parseFactor();
    if (!first) return null;
    const next = peek();
    if (next?.kind !== 'op' || !['*', '/'].includes(next.value)) return first;

    let value = first.percent ? first.value / 100 : first.value;
    let op: string | null;
    while ((op = takeOp('*', '/'))) {
      const right = parseFactor();
      if (!right) return null;
      const operand = right.percent ? right.value / 100 : right.value;
      if (op === '/' && operand === 0) return null;
      value = op === '*' ? value * operand : value / operand;
    }
    return { value, percent: false };
  };

  const parseFactor = (): Operand | null => {
    const sign = takeOp('+', '-');
    if (sign) {
      const operand = parseFactor();
      return operand && { value: sign === '-' ? -operand.value : operand.value, percent: operand.percent };
    }

    let value: number;
    const token = peek();
    if (token?.kind === 'number') {
      position++;
      value = token.value;
    } else if (takeOp('(')) {
      const inner = parseExpression();
      if (!inner || !takeOp(')')) return null;
      value = inner.value;
    } else {
      return null;
    }

    return { value, percent: takeOp('%') !== null };
  };

  const result = parseExpression();
  if (!result || position !== tokens.length || !Number.isFinite(result.value)) return null;
  return Math.round((result.value + Number.EPSILON) * 100) / 100;
}

/** Hay alguna cuenta para resolver (no es solo un número, con o sin signo). */
export function hasPendingOperation(text: string | null | undefined): boolean {
  return /[+\-−*xX×/÷()%]/.test((text ?? '').trim().replace(/^[-−]/, ''));
}

export function formatAmount(value: number | null | undefined): string {
  if (value === null || value === undefined || Number.isNaN(value)) return '';
  return new Intl.NumberFormat('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(value);
}

function tokenize(text: string): Token[] | null {
  const tokens: Token[] = [];
  let index = 0;
  while (index < text.length) {
    const char = text[index];
    if (/\s|\$/.test(char)) {
      index++;
      continue;
    }
    if (/[0-9.,]/.test(char)) {
      let end = index;
      while (end < text.length && /[0-9.,]/.test(text[end])) end++;
      const value = parseNumber(text.slice(index, end));
      if (value === null) return null;
      tokens.push({ kind: 'number', value });
      index = end;
      continue;
    }
    const op = OPERATOR_ALIASES[char];
    if (!op) return null;
    tokens.push({ kind: 'op', value: op });
    index++;
  }
  return tokens;
}

// Igual que la caja de texto del Monto: el último punto o coma es el separador decimal.
function parseNumber(raw: string): number | null {
  const decimalIndex = Math.max(raw.lastIndexOf('.'), raw.lastIndexOf(','));
  const normalized = decimalIndex >= 0
    ? `${raw.slice(0, decimalIndex).replace(/[.,]/g, '')}.${raw.slice(decimalIndex + 1)}`
    : raw;
  if (!/\d/.test(normalized)) return null;
  const value = Number(normalized.startsWith('.') ? `0${normalized}` : normalized);
  return Number.isFinite(value) ? value : null;
}
