import { Component, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-root',
  standalone:true,
  template: `
    <h1>Fuel App</h1>
    <button (click)="testConnection()">Test Backend</button>

    <p>{{ result() }}</p>
  `,
})
export class App {
  private http = inject(HttpClient);
  result = signal('Not tested yet');

  testConnection() {
    this.http.get<any>('https://localhost:5152/api/Test').subscribe({
      next: (res) => this.result.set(res.message + ' | ' + res.time),
      error: (err) => this.result.set('Error: ' + JSON.stringify(err)),
    });
  }
}
