import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';

/**
 * Dashboard feature component - demonstrates lazy loading pattern.
 *
 * This component is lazy-loaded when the user navigates to /dashboard.
 * Best Practice: Keep feature modules isolated and lazy-loaded for better performance.
 */
@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="dashboard">
      <h1>📊 Dashboard</h1>
      <p>Welcome to your dashboard!</p>

      <section class="stats">
        <div class="stat-card">
          <h3>{{ stats().users }}</h3>
          <p>Total Users</p>
        </div>
        <div class="stat-card">
          <h3>{{ stats().requests }}</h3>
          <p>API Requests</p>
        </div>
        <div class="stat-card">
          <h3>{{ stats().uptime }}</h3>
          <p>Uptime</p>
        </div>
      </section>

      <button (click)="refreshStats()">🔄 Refresh Stats</button>
    </div>
  `,
  styles: [
    `
      .dashboard {
        padding: 2rem;
        max-width: 1200px;
        margin: 0 auto;
      }

      .stats {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
        gap: 1rem;
        margin: 2rem 0;
      }

      .stat-card {
        background: #f8f9fa;
        border-radius: 8px;
        padding: 1.5rem;
        text-align: center;
        box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
      }

      .stat-card h3 {
        font-size: 2rem;
        color: #4f46e5;
        margin: 0;
      }

      .stat-card p {
        color: #6b7280;
        margin: 0.5rem 0 0;
      }

      button {
        background: #4f46e5;
        color: white;
        border: none;
        padding: 0.75rem 1.5rem;
        border-radius: 6px;
        cursor: pointer;
        font-size: 1rem;
      }

      button:hover {
        background: #4338ca;
      }
    `
  ]
})
export class DashboardComponent {
  // Using signals for lightweight reactive state (Angular 17+)
  stats = signal({
    users: 1234,
    requests: '45.2K',
    uptime: '99.9%'
  });

  refreshStats(): void {
    // Simulate refreshing stats
    this.stats.set({
      users: Math.floor(Math.random() * 5000),
      requests: `${(Math.random() * 100).toFixed(1)}K`,
      uptime: `${(99 + Math.random()).toFixed(1)}%`
    });
  }
}
