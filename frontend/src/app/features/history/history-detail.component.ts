import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { Subscription } from 'rxjs';

import { ThreadHistoryService } from '../../services/thread-history.service';
import { ThreadHistoryDetail } from '../../models/thread-history.model';
import { ThreadPreviewComponent } from '../../components/thread-preview/thread-preview.component';

@Component({
  selector: 'app-history-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, ThreadPreviewComponent],
  templateUrl: './history-detail.component.html',
  styleUrl: './history-detail.component.css'
})
export class HistoryDetailComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly history = inject(ThreadHistoryService);

  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);
  readonly detail = signal<ThreadHistoryDetail | null>(null);
  readonly tweets = signal<string[] | null>(null);

  private sub: Subscription | null = null;

  ngOnInit(): void {
    this.sub = this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (!id) {
        this.errorMessage.set('Thread not found');
        this.isLoading.set(false);
        return;
      }

      this.load(id);
    });
  }

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
  }

  load(id: string): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.history.getById(id).subscribe({
      next: detail => {
        this.detail.set(detail);
        this.tweets.set(detail.tweets);
        this.isLoading.set(false);
      },
      error: (err: unknown) => {
        const status = err instanceof HttpErrorResponse ? err.status : null;
        if (status === 404) {
          this.errorMessage.set('Thread not found');
        } else {
          this.errorMessage.set('Unable to load history. Please try again.');
        }
        this.isLoading.set(false);
      }
    });
  }

  onTweetEdited(event: { index: number; newText: string }): void {
    const current = this.tweets();
    if (!current) return;

    const updated = [...current];
    updated[event.index - 1] = event.newText;
    this.tweets.set(updated);
  }

  formatDate(value: string): string {
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return value;
    return date.toLocaleString();
  }

  get topic(): string {
    const req = this.detail()?.request;
    const t = req?.['topic'];
    return typeof t === 'string' && t.trim().length > 0 ? t : 'Untitled topic';
  }
}
