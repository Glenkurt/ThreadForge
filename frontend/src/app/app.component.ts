import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { HealthService, HealthResponse } from './services/health.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent implements OnInit {
  title = 'Full-Stack Template';
  healthStatus: HealthResponse | null = null;
  loading = true;
  error: string | null = null;

  private healthService = inject(HealthService);

  ngOnInit(): void {
    this.checkApiHealth();
  }

  checkApiHealth(): void {
    this.loading = true;
    this.error = null;

    this.healthService.checkHealth().subscribe({
      next: response => {
        this.healthStatus = response;
        this.loading = false;
      },
      error: err => {
        this.error = 'Unable to connect to API';
        this.loading = false;
        console.error('Health check failed:', err);
      }
    });
  }
}
