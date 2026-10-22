import { Component, ElementRef, EventEmitter, Input, OnChanges, OnDestroy, Output, SimpleChanges, ViewChild } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { Subscription } from 'rxjs';
import { debounceTime } from 'rxjs/operators';
import { EmailTemplate } from '../../models/email-template.model';
import { TEMPLATE_TYPES, TemplatePlaceholder, TemplateTypeOption, findTemplateType } from '../../models/email-template-catalog';

type EditableField = 'subject' | 'body';

/** Mismo patrón que el backend (EmailTemplateService): {{Clave}}, con espacios opcionales. */
const PLACEHOLDER_PATTERN = /\{\{\s*(\w+)\s*\}\}/g;

@Component({
  selector: 'app-email-template-form',
  templateUrl: './email-template-form.component.html',
  styleUrls: ['./email-template-form.component.scss']
})
export class EmailTemplateFormComponent implements OnChanges, OnDestroy {
  readonly subjectMaxLength = 200;
  readonly templateTypes = TEMPLATE_TYPES;

  @Input() mode: 'create' | 'edit' = 'create';
  @Input() template: EmailTemplate | null = null;
  /** Nombres que ya tienen plantilla: el envío usa una sola por nombre. */
  @Input() usedNames: string[] = [];
  @Output() cancelForm = new EventEmitter<void>();
  @Output() saveForm = new EventEmitter<EmailTemplate>();

  @ViewChild('subjectInput') subjectInput?: ElementRef<HTMLInputElement>;
  @ViewChild('bodyInput') bodyInput?: ElementRef<HTMLTextAreaElement>;

  readonly form = this.fb.group({
    name: ['' as string, [Validators.required]],
    subject: ['' as string, [Validators.required, Validators.maxLength(this.subjectMaxLength)]],
    body: ['' as string, [Validators.required]]
  });

  /** En el celular se ve una pestaña a la vez; en pantallas anchas, código y vista previa lado a lado. */
  bodyTab: 'code' | 'preview' = 'code';
  previewSubject = '';
  previewBody: SafeHtml = '';
  unknownPlaceholders: string[] = [];
  private lastField: EditableField = 'body';
  private readonly previewSubscription: Subscription;

  constructor(
    private readonly fb: FormBuilder,
    private readonly sanitizer: DomSanitizer
  ) {
    // La vista previa se recalcula al dejar de escribir: rearmar el iframe en cada tecla lo hace parpadear.
    this.previewSubscription = this.form.valueChanges.pipe(debounceTime(250)).subscribe(() => this.updatePreview());
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes.template || changes.mode) {
      this.form.reset({
        name: this.template?.name ?? '',
        subject: this.template?.subject ?? '',
        body: this.template?.body ?? ''
      });
      this.bodyTab = 'code';
      this.updatePreview();
    }
  }

  ngOnDestroy(): void {
    this.previewSubscription.unsubscribe();
  }

  get title(): string {
    return this.mode === 'create' ? 'Nueva plantilla de correo' : 'Editar plantilla de correo';
  }

  get selectedType(): TemplateTypeOption | undefined {
    return findTemplateType(this.form.controls.name.value);
  }

  get placeholders(): TemplatePlaceholder[] {
    return this.selectedType?.placeholders ?? [];
  }

  get nameTaken(): boolean {
    const name = this.form.controls.name.value;
    return !!name && name !== this.template?.name && this.usedNames.includes(name);
  }

  isNameDisabled(value: string): boolean {
    return value !== this.template?.name && this.usedNames.includes(value);
  }

  rememberField(field: EditableField): void {
    this.lastField = field;
  }

  token(key: string): string {
    return `{{${key}}}`;
  }

  /** Inserta el marcador donde está el cursor del último campo usado (asunto o cuerpo). */
  insertPlaceholder(placeholder: TemplatePlaceholder): void {
    this.insertText(this.lastField, this.token(placeholder.key));
  }

  /** Enlace al ingreso de ContaMec si la plantilla tiene ese dato; si no, con la dirección vacía para completarla. */
  insertLink(): void {
    const href = this.placeholders.some((p) => p.key === 'LoginUrl') ? this.token('LoginUrl') : '';
    this.wrapBody(`<a href="${href}" style="color:#0c4a6e;font-weight:600">`, '</a>');
  }

  /** Envuelve la selección del cuerpo con la etiqueta HTML (o la inserta vacía si no hay selección). */
  wrapBody(open: string, close: string): void {
    const textarea = this.bodyInput?.nativeElement;
    if (!textarea) return;
    this.bodyTab = 'code';
    const value = this.form.controls.body.value ?? '';
    const start = textarea.selectionStart ?? value.length;
    const end = textarea.selectionEnd ?? value.length;
    const selected = value.slice(start, end);
    this.setFieldValue('body', value.slice(0, start) + open + selected + close + value.slice(end), start + open.length, start + open.length + selected.length);
  }

  /** Tab inserta sangría en lugar de salir del editor. */
  onBodyKeydown(event: KeyboardEvent): void {
    if (event.key !== 'Tab' || event.shiftKey) return;
    event.preventDefault();
    this.insertText('body', '  ');
  }

  save(): void {
    if (this.form.invalid || this.nameTaken) {
      this.form.markAllAsTouched();
      return;
    }
    const raw = this.form.getRawValue();
    const payload: EmailTemplate = {
      id: this.template?.id ?? 0,
      name: raw.name || null,
      subject: raw.subject?.trim() || '',
      body: raw.body?.trim() || ''
    };
    this.saveForm.emit(payload);
  }

  cancel(): void {
    this.cancelForm.emit();
  }

  private insertText(field: EditableField, text: string): void {
    const element = field === 'subject' ? this.subjectInput?.nativeElement : this.bodyInput?.nativeElement;
    const value = this.form.controls[field].value ?? '';
    const start = element?.selectionStart ?? value.length;
    const end = element?.selectionEnd ?? value.length;
    const caret = start + text.length;
    if (field === 'body') this.bodyTab = 'code';
    this.setFieldValue(field, value.slice(0, start) + text + value.slice(end), caret, caret);
  }

  private setFieldValue(field: EditableField, value: string, selectionStart: number, selectionEnd: number): void {
    const control = this.form.controls[field];
    control.setValue(value);
    control.markAsDirty();
    // El elemento se actualiza en el próximo ciclo; recién ahí se puede ubicar el cursor.
    setTimeout(() => {
      const element = field === 'subject' ? this.subjectInput?.nativeElement : this.bodyInput?.nativeElement;
      if (!element) return;
      element.focus();
      element.setSelectionRange(selectionStart, selectionEnd);
    });
  }

  private updatePreview(): void {
    const raw = this.form.getRawValue();
    const samples = new Map(this.placeholders.map((p) => [p.key, p.sample]));
    const unknown = new Set<string>();
    const fill = (text: string, encode: boolean) =>
      text.replace(PLACEHOLDER_PATTERN, (match: string, key: string) => {
        const sample = samples.get(key);
        if (sample === undefined) {
          unknown.add(key);
          return match;
        }
        return encode ? this.escapeHtml(sample) : sample;
      });

    this.previewSubject = fill(raw.subject ?? '', false);
    const body = fill(raw.body ?? '', true);
    this.unknownPlaceholders = this.selectedType ? [...unknown] : [];
    // El iframe tiene sandbox sin permisos: el HTML se muestra pero no puede ejecutar scripts.
    this.previewBody = this.sanitizer.bypassSecurityTrustHtml(
      `<!doctype html><html><head><meta charset="utf-8"><style>body{margin:12px;background:#fff}</style></head><body>${body}</body></html>`
    );
  }

  private escapeHtml(value: string): string {
    return value.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
  }
}
