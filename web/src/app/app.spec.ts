import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { App } from './app';
import { environment } from '../environments/environment';

const HEALTH_CHECK_TIMEOUT_MS = 15_000;

const healthUrl = `${environment.apiBaseUrl}/health`;

describe('App', () => {
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
    httpMock.expectOne(healthUrl).flush({ status: 'healthy', timestamp: '2026-09-03T00:00:00Z' });
  });

  it('should render title', async () => {
    const fixture = TestBed.createComponent(App);
    httpMock.expectOne(healthUrl).flush({ status: 'healthy', timestamp: '2026-09-03T00:00:00Z' });
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h1')?.textContent).toContain('Hello, web');
  });

  it('should show a loading state before the API responds', () => {
    const fixture = TestBed.createComponent(App);
    // Render síncrono deliberado: queremos observar el estado 'loading'
    // *antes* de resolver la request mockeada -- await fixture.whenStable()
    // aquí bloquearía hasta que la request (nunca flusheada todavía) se
    // resuelva.
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.health')?.textContent).toContain('Verificando');
    httpMock.expectOne(healthUrl).flush({ status: 'healthy', timestamp: '2026-09-03T00:00:00Z' });
  });

  it('should show the health status once the API responds', async () => {
    const fixture = TestBed.createComponent(App);
    httpMock.expectOne(healthUrl).flush({ status: 'healthy', timestamp: '2026-09-03T00:00:00Z' });
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.health')?.textContent).toContain('healthy');
  });

  it('should show an explicit error state when the API call fails (no unhandled rejection)', async () => {
    const fixture = TestBed.createComponent(App);
    httpMock.expectOne(healthUrl).error(new ProgressEvent('error'));
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.health')?.textContent).toContain('No se pudo conectar');
  });

  it('should show an explicit error state when the API never responds within the cold-start timeout', async () => {
    // Sin zone.js (app zoneless) no hay fakeAsync/tick -- se usan los fake
    // timers de vitest, que sí interceptan el asyncScheduler de RxJS detrás
    // de timeout().
    vi.useFakeTimers();
    try {
      const fixture = TestBed.createComponent(App);
      fixture.detectChanges();
      httpMock.expectOne(healthUrl); // deliberadamente sin flush -- simula un cold-start colgado
      await vi.advanceTimersByTimeAsync(HEALTH_CHECK_TIMEOUT_MS + 1);
      await fixture.whenStable();
      const compiled = fixture.nativeElement as HTMLElement;
      expect(compiled.querySelector('.health')?.textContent).toContain('No se pudo conectar');
    } finally {
      vi.useRealTimers();
    }
  });
});
