import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../../core';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <section class="container">
      <h1>Sign in</h1>
      <form (ngSubmit)="onSubmit()" #loginForm="ngForm">
        <label>
          Email
          <input type="email" name="email" [(ngModel)]="email" required />
        </label>
        <label>
          Password
          <input type="password" name="password" [(ngModel)]="password" required />
        </label>
        <button type="submit" [disabled]="auth.isLoading()">Sign in</button>
      </form>
    </section>
  `,
  styles: [
    `
      .container {
        max-width: 480px;
        margin: 2rem auto;
      }
      form {
        display: grid;
        gap: 1rem;
      }
      label {
        display: grid;
        gap: 0.5rem;
      }
    `
  ]
})
export class LoginComponent {
  email = '';
  password = '';

  readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  onSubmit(): void {
    this.auth.login(this.email, this.password).subscribe({
      next: () => this.router.navigate(['/dashboard']),
      error: () => {
        // Error handling is centralized in the interceptor
      }
    });
  }
}
