export interface TemplatePlaceholder {
  key: string;
  label: string;
  /** Valor de ejemplo para la vista previa. */
  sample: string;
}

export interface TemplateTypeOption {
  /** Etiqueta del enum TemplateTypes de la base. */
  value: string;
  label: string;
  /** Cuándo envía el sistema este correo; null si todavía no lo envía. */
  usage: string | null;
  placeholders: TemplatePlaceholder[];
}

const company: TemplatePlaceholder = { key: 'Empresa', label: 'Empresa', sample: 'Ferretería El Tornillo' };
const username: TemplatePlaceholder = { key: 'Usuario', label: 'Usuario', sample: 'eltornillo' };

/** Marcadores que completa el backend al enviar cada correo (ver BuildNewCompanyValues y BuildActivationValues). */
const demoRequestPlaceholders: TemplatePlaceholder[] = [
  company,
  { key: 'Direccion', label: 'Dirección', sample: 'Av. Costanera 1234' },
  { key: 'Localidad', label: 'Localidad', sample: 'Dolores' },
  { key: 'WhatsApp', label: 'WhatsApp', sample: '+5492245123456' },
  { key: 'Email', label: 'Email', sample: 'contacto@eltornillo.com.ar' },
  { ...username, label: 'Usuario administrador' },
  { key: 'FechaRegistro', label: 'Fecha de registro', sample: '22/10/2026 09:03' }
];

const demoApprovedPlaceholders: TemplatePlaceholder[] = [
  company,
  username,
  { key: 'DiasDemo', label: 'Días de demo', sample: '7' },
  { key: 'FechaInicio', label: 'Inicio de la demo', sample: '22/10/2026 10:15' },
  { key: 'FechaVencimiento', label: 'Vencimiento de la demo', sample: '29/10/2026 10:15' },
  { key: 'LoginUrl', label: 'Link de ingreso', sample: 'https://contamec.vercel.app' },
  { key: 'WhatsAppSoporte', label: 'WhatsApp de soporte', sample: '+54 9 2241 695973' },
  { key: 'WhatsAppSoporteNumero', label: 'WhatsApp de soporte (solo números)', sample: '5492241695973' }
];

/** Los pedidos de producción y renovación todavía no se envían: se ofrecen todos los marcadores para ir armándolos. */
const allPlaceholders: TemplatePlaceholder[] = [
  ...demoRequestPlaceholders,
  ...demoApprovedPlaceholders.filter((p) => !demoRequestPlaceholders.some((d) => d.key === p.key))
];

export const TEMPLATE_TYPES: TemplateTypeOption[] = [
  {
    value: 'PedidoDemo',
    label: 'Pedido de demo',
    usage: 'Se envía a los desarrolladores cuando una empresa usa "Solicitar demo gratis".',
    placeholders: demoRequestPlaceholders
  },
  {
    value: 'PedidoDemoAprobado',
    label: 'Pedido de demo aprobado',
    usage: 'Se envía a la empresa cuando se activa su aplicación desde Aplicaciones.',
    placeholders: demoApprovedPlaceholders
  },
  { value: 'PedidoProduccion', label: 'Pedido de producción', usage: null, placeholders: allPlaceholders },
  { value: 'PedidoProduccionAprobado', label: 'Pedido de producción aprobado', usage: null, placeholders: allPlaceholders },
  { value: 'PedidoRenovacion', label: 'Pedido de renovación', usage: null, placeholders: allPlaceholders },
  { value: 'PedidoRenovacionAprobado', label: 'Pedido de renovación aprobado', usage: null, placeholders: allPlaceholders }
];

export function findTemplateType(value: string | null | undefined): TemplateTypeOption | undefined {
  return TEMPLATE_TYPES.find((type) => type.value === value);
}

export function templateTypeLabel(value: string | null | undefined): string {
  return findTemplateType(value)?.label ?? value ?? '-';
}
