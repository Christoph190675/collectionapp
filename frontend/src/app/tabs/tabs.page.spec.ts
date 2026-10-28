import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { TabsPage } from './tabs.page';

describe('TabsPage', () => {
  let component: TabsPage;
  let fixture: ComponentFixture<TabsPage>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TabsPage],
      providers: [provideRouter([])]
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(TabsPage);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('US1-AC2 : onglet Statut présent', () => {
    const tab: HTMLElement | null = fixture.nativeElement.querySelector('ion-tab-button[tab="status"]');

    expect(tab).not.toBeNull();
    expect(tab!.getAttribute('href')).toBe('/tabs/status');
    expect(tab!.textContent?.trim()).toBe('Statut');
  });
});
