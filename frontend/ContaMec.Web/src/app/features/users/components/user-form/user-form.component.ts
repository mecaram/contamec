import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { passwordPolicyValidator, passwordRules, PasswordRule } from '../../../../auth/password-policy';
import { User } from '../../models/user.model';
import { UserApplicationOption, UserRoleOption } from '../../services/user.service';

@Component({
  selector: 'app-user-form',
  templateUrl: './user-form.component.html',
  styleUrls: ['./user-form.component.scss']
})
export class UserFormComponent implements OnChanges {
  @Input() mode: 'create' | 'edit' = 'create';
  @Input() user: User | null = null;
  @Input() roleOptions: UserRoleOption[] = [];
  /** Solo llega con datos para un SuperAdministrador; vacío oculta el selector de empresa. */
  @Input() applicationOptions: UserApplicationOption[] = [];
  @Input() currentApplicationId: number | null = null;
  @Output() cancelForm = new EventEmitter<void>();
  @Output() saveForm = new EventEmitter<User>();

  readonly form = this.fb.group({
    name: ['' as string, [Validators.required, Validators.maxLength(80)]],
    password: ['' as string, passwordPolicyValidator],
    isActive: [true],
    userRoleId: [null as number | null],
    applicationId: [null as number | null]
  });

  constructor(private readonly fb: FormBuilder) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (changes.user || changes.mode || changes.currentApplicationId) {
      this.form.reset({
        name: this.user?.name ?? '',
        password: '',
        isActive: this.user?.isActive ?? true,
        userRoleId: this.user?.userRoleId ?? null,
        applicationId: this.currentApplicationId
      });
    }
  }

  get passwordRules(): PasswordRule[] {
    return passwordRules(this.form.controls.password.value);
  }

  /** Al crear siempre se muestran los requisitos; al editar, solo si se está escribiendo una nueva. */
  get showPasswordRules(): boolean {
    return this.mode === 'create' || !!this.form.controls.password.value;
  }

  get showApplication(): boolean {
    return this.mode === 'create' && this.applicationOptions.length > 0;
  }

  get title(): string {
    return this.mode === 'create' ? 'Nuevo usuario' : 'Editar usuario';
  }

  save(): void {
    if (this.mode === 'create' && !this.form.controls.password.value?.trim()) {
      this.form.controls.password.setErrors({ required: true });
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
      password: password.length > 0 ? password : null,
      applicationId: this.showApplication ? raw.applicationId ?? null : null
    };
    this.saveForm.emit(payload);
  }

  cancel(): void {
    this.cancelForm.emit();
  }
}
