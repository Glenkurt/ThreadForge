import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-unauthorized',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <section class="container">
      <h1>Access denied</h1>
      <p>You do not have permission to view this page.</p>
      <a routerLink="/">Go back home</a>
    </section>
  `,
  styles: [
    `
      .container {
        max-width: 480px;
        margin: 2rem auto;
      }
    `
  ]
})
export class UnauthorizedComponent {}
