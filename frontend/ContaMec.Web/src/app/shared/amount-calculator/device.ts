/**
 * Celular: pantalla táctil chica, en cualquier orientación (acostado queda bajo de alto).
 * Tablets, notebooks y computadoras no entran, aunque la ventana se achique.
 */
export function isPhone(): boolean {
  if (typeof window === 'undefined' || typeof window.matchMedia !== 'function') return false;
  return window.matchMedia('(pointer: coarse) and (max-width: 599.98px), (pointer: coarse) and (max-height: 499.98px)').matches;
}
