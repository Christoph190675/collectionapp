import { Component, inject, input, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
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
} from '@ionic/angular';
import { finalize } from 'rxjs';

import { AuthService } from '../../services/auth.service';
import { FormErrors, toFormErrors } from '../auth-errors';

export const HOME_URL = '/tabs/tab1';
export const SESSION_EXPIRED_MESSAGE = 'Votre session a expiré. Reconnectez-vous.';

@Component({
  selector: 'app-login',
  templateUrl: 'login.page.html',
  styleUrls: ['login.page.scss'],
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
export class LoginPage {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  /** `?expired=1`: set by the interceptor when the session could not be renewed (US4-AC3) */
  readonly expired = input<string | undefined>();

  readonly form = inject(FormBuilder).nonNullable.group({
    email: ['', [Validators.required]],
    password: ['', [Validators.required]],
  });

  readonly submitting = signal(false);
  readonly errors = signal<FormErrors>({ global: null, fields: {} });

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
      .login(this.form.getRawValue())
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: () => {
          this.form.reset();
          void this.router.navigateByUrl(HOME_URL);
        },
        error: (error: unknown) => {
          this.errors.set(toFormErrors(error));
          this.form.controls.password.setValue('');
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
    return control.touched && control.hasError('required') ? 'Ce champ est obligatoire' : null;
  }

  sessionExpired(): boolean {
    return this.expired() === '1' && !this.errors().global;
  }

  readonly sessionExpiredMessage = SESSION_EXPIRED_MESSAGE;
}
