import { Component, inject, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import {
  IonButton,
  IonContent,
  IonHeader,
  IonInput,
  IonItem,
  IonList,
  IonNote,
  IonSpinner,
  IonText,
  IonTitle,
  IonToolbar,
  ToastController,
} from '@ionic/angular';
import { finalize } from 'rxjs';

import { AuthService } from '../../services/auth.service';
import { FormErrors, toFormErrors } from '../auth-errors';

function sameAsPassword(control: AbstractControl): ValidationErrors | null {
  const password = control.parent?.get('password')?.value;
  return control.value === password ? null : { mismatch: true };
}

@Component({
  selector: 'app-register',
  templateUrl: 'register.page.html',
  styleUrls: ['register.page.scss'],
  imports: [
    ReactiveFormsModule,
    RouterLink,
    IonHeader,
    IonToolbar,
    IonTitle,
    IonContent,
    IonList,
    IonItem,
    IonInput,
    IonNote,
    IonText,
    IonButton,
    IonSpinner,
  ],
})
export class RegisterPage {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly toastController = inject(ToastController);

  // Ergonomic checks only: the API rules prevail (constitution VI)
  readonly form = inject(FormBuilder).nonNullable.group({
    lastName: ['', [Validators.required, Validators.maxLength(100)]],
    firstName: ['', [Validators.required, Validators.maxLength(100)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
    passwordConfirmation: ['', [Validators.required, sameAsPassword]],
  });

  readonly submitting = signal(false);
  readonly errors = signal<FormErrors>({ global: null, fields: {} });

  constructor() {
    this.form.controls.password.valueChanges.subscribe(() =>
      this.form.controls.passwordConfirmation.updateValueAndValidity({ emitEvent: false }),
    );
  }

  submit(): void {
    if (this.submitting()) {
      return;
    }
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.submitting.set(true);
    this.errors.set({ global: null, fields: {} });
    this.authService
      .register(this.form.getRawValue())
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: async () => {
          this.form.reset();
          const toast = await this.toastController.create({
            message: 'Compte créé. Vous pouvez vous connecter.',
            duration: 3000,
            color: 'success',
          });
          await toast.present();
          await this.router.navigateByUrl('/login');
        },
        error: (error: unknown) => {
          this.errors.set(toFormErrors(error));
          this.form.patchValue({ password: '', passwordConfirmation: '' });
        },
      });
  }

  /** Message under a field: API error first, then the local check once the field was touched. */
  fieldError(name: keyof typeof this.form.controls): string | null {
    const apiError = this.errors().fields[name];
    if (apiError) {
      return apiError;
    }
    const control = this.form.controls[name];
    if (!control.touched || control.valid) {
      return null;
    }
    if (control.hasError('required')) {
      return 'Ce champ est obligatoire';
    }
    if (control.hasError('email')) {
      return "L'adresse e-mail n'est pas valide";
    }
    if (control.hasError('minlength')) {
      return 'Au moins 8 caractères, dont une lettre et un chiffre';
    }
    if (control.hasError('maxlength')) {
      return '100 caractères maximum';
    }
    if (control.hasError('mismatch')) {
      return 'La confirmation ne correspond pas au mot de passe';
    }
    return null;
  }
}
