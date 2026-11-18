import { Component, inject, signal } from '@angular/core';
import { IonButton, IonContent, IonHeader, IonItem, IonLabel, IonList, IonTitle, IonToolbar } from '@ionic/angular';

import { ExploreContainerComponent } from '../explore-container/explore-container.component';
import { AuthService } from '../services/auth.service';

@Component({
  selector: 'app-tab3',
  templateUrl: 'tab3.page.html',
  styleUrls: ['tab3.page.scss'],
  imports: [IonHeader, IonToolbar, IonTitle, IonContent, IonList, IonItem, IonLabel, IonButton, ExploreContainerComponent],
})
export class Tab3Page {
  private readonly authService = inject(AuthService);

  readonly currentUser = this.authService.currentUser;
  readonly loggingOut = signal(false);

  async logout(): Promise<void> {
    if (this.loggingOut()) {
      return;
    }
    this.loggingOut.set(true);
    try {
      await this.authService.logout();
    } finally {
      this.loggingOut.set(false);
    }
  }
}
