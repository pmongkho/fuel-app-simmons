import { Component, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-root',
  standalone: true,
  template: `
    <main>
      <h1>Fuel App</h1>
      <button (click)="testConnection()">Test Backend</button>
      <p>{{ result() }}</p>
    </main>
  `,
  styles: `
    main {
      font-family: Arial, sans-serif;
      margin: 2rem;
    }

    button {
      margin-block: 1rem;
      padding: 0.5rem 0.75rem;
    }
  `,
})
export class App {
  private http = inject(HttpClient);
  result = signal('Frontend loaded. Click "Test Backend" to verify API connectivity.');

  testConnection() {
    this.http.get<{ message: string; time: string }>('http://localhost:5152/api/Test').subscribe({
      next: (res) => this.result.set(`${res.message} | ${res.time}`),
      error: (err) => this.result.set(`Backend call failed: ${err.status || 'network error'} ${err.statusText || ''}`.trim()),
    });
  }
}
