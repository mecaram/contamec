import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { User } from '../../models/user.model';
import { UserRoleOption } from '../../services/user.service';

@Component({
  selector: 'app-user-form',
  templateUrl: './user-form.component.html',
  styleUrls: ['./user-form.component.scss']
})
export class UserFormComponent implements OnChanges {
  @Input() mode: 'create' | 'edit' = 'create';
  @Input() user: User | null = null;
  @Input() roleOptions: UserRoleOption[] = [];
  @Output() cancelForm = new EventEmitter<void>();
  @Output() saveForm = new EventEmitter<User>();

  readonly form = this.fb.group({
    name: ['' as string, [Validators.required, Validators.maxLength(80)]],
    password: ['' as string],
    isActive: [true],
    userRoleId: [null as number | null]
  });

  constructor(private readonly fb: FormBuilder) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (changes.user || changes.mode) {
      this.form.reset({
        name: this.user?.name ?? '',
        password: '',
        isActive: this.user?.isActive ?? true,
        userRoleId: this.user?.userRoleId ?? null
      });
    }
  }

  get title(): string {
    return this.mode === 'create' ? 'Nuevo usuario' : 'Editar usuario';
  }

  save(): void {
    if (this.mode === 'create') {
      const password = this.form.controls.password.value?.trim() ?? '';
      if (password.length < 4) {
        this.form.controls.password.setErrors({ minlength: true });
      }
    }

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    const password = raw.password?.trim() || '';
    const payload: User = {
      id: this.user?.id ?? 0,
      name: raw.name?.trim() || '',
      isActive: !!raw.isActive,
      userRoleId: raw.userRoleId ?? null,
      password: password.length > 0 ? password : null
    };
    this.saveForm.emit(payload);
  }

  cancel(): void {
    this.cancelForm.emit();
  }
}
