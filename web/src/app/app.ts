import { Component, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { timeout } from 'rxjs';
import { environment } from '../environments/environment';

// Si el Container App tarda en salir de un cold-start (min_replicas=0) o
// cuelga, este timeout dispara el mismo callback `error` de subscribe en
// vez de dejar healthStatus atorado en 'loading' para siempre.
const HEALTH_CHECK_TIMEOUT_MS = 15_000;

interface HealthResponse {
  status: string;
  timestamp: string;
}

type HealthStatus = 'loading' | 'ok' | 'error';

@Component({
  selector: 'app-root',
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('web');

  // Estado de /health, expuesto como signal -- prueba visual de la cadena
  // código -> CI -> Terraform -> Azure -> app corriendo (spec 1.5). Arranca
  // en 'loading' para que el shell nunca muestre una pantalla en blanco,
  // ni siquiera durante un cold-start del Container App (min_replicas=0).
  protected readonly healthStatus = signal<HealthStatus>('loading');
  protected readonly healthData = signal<HealthResponse | null>(null);

  private readonly http = inject(HttpClient);

  constructor() {
    // Manejo de error explícito (callback `error` de subscribe) -- nunca un
    // unhandled rejection, ni siquiera si el Container App está en cero.
    this.http.get<HealthResponse>(`${environment.apiBaseUrl}/health`).pipe(
      timeout(HEALTH_CHECK_TIMEOUT_MS)
    ).subscribe({
      next: (data) => {
        this.healthData.set(data);
        this.healthStatus.set('ok');
      },
      error: () => {
        this.healthStatus.set('error');
      }
    });
  }
}
