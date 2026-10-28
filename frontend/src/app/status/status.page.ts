import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import {
  IonBadge,
  IonButton,
  IonContent,
  IonHeader,
  IonItem,
  IonLabel,
  IonList,
  IonSpinner,
  IonTitle,
  IonToolbar,
} from '@ionic/angular';
import { Subscription, finalize } from 'rxjs';

import { HealthCheckError, HealthCheckResult, HealthService } from '../services/health.service';

type State = HealthCheckResult['api'] | HealthCheckResult['database'];

const STATE_LABELS: Record<State, string> = { ok: 'OK', ko: 'KO', unknown: 'Inconnu' };
const STATE_COLORS: Record<State, string> = { ok: 'success', ko: 'danger', unknown: 'medium' };
const ERROR_MESSAGES: Record<HealthCheckError, string> = {
  unreachable: 'API injoignable',
  timeout: 'Délai dépassé (5 s)',
  'invalid-response': "Réponse invalide de l'API",
};

@Component({
  selector: 'app-status',
  templateUrl: 'status.page.html',
  styleUrls: ['status.page.scss'],
  imports: [
    DatePipe,
    IonHeader,
    IonToolbar,
    IonTitle,
    IonContent,
    IonList,
    IonItem,
    IonLabel,
    IonBadge,
    IonButton,
    IonSpinner,
  ],
})
export class StatusPage {
  private readonly healthService = inject(HealthService);
  private subscription?: Subscription;

  readonly result = signal<HealthCheckResult | null>(null);
  readonly loading = signal(false);

  ionViewWillEnter(): void {
    this.refresh();
  }

  ionViewWillLeave(): void {
    this.subscription?.unsubscribe();
    this.subscription = undefined;
    this.loading.set(false);
  }

  refresh(): void {
    if (this.loading()) {
      return;
    }
    this.loading.set(true);
    this.subscription = this.healthService
      .check()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe((result) => this.result.set(result));
  }

  label(state: State | undefined): string {
    return state ? STATE_LABELS[state] : '…';
  }

  color(state: State | undefined): string {
    return state ? STATE_COLORS[state] : 'medium';
  }

  errorMessage(error: HealthCheckError | undefined): string | null {
    return error ? ERROR_MESSAGES[error] : null;
  }
}
