import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterModule } from '@angular/router';

import { ThreadHistoryService } from '../../services/thread-history.service';
import { ThreadHistoryListItem } from '../../models/thread-history.model';

@Component({
  selector: 'app-history-list',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './history-list.component.html',
  styleUrl: './history-list.component.css'
})
export class HistoryListComponent implements OnInit {
  private readonly history = inject(ThreadHistoryService);

  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);
  readonly items = signal<ThreadHistoryListItem[]>([]);

  readonly limit = 20;
  readonly offset = signal(0);

  readonly canLoadMore = computed(() => this.items().length > 0 && !this.isLoading());

  ngOnInit(): void {
    this.loadFirstPage();
  }

  loadFirstPage(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.offset.set(0);

    this.history.list(this.limit, 0).subscribe({
      next: items => {
        this.items.set(items);
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('Unable to load history. Please try again.');
        this.isLoading.set(false);
      }
    });
  }

  loadMore(): void {
    if (this.isLoading()) return;

    const nextOffset = this.offset() + this.limit;
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.history.list(this.limit, nextOffset).subscribe({
      next: items => {
        this.items.update(existing => [...existing, ...items]);
        this.offset.set(nextOffset);
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('Unable to load history. Please try again.');
        this.isLoading.set(false);
      }
    });
  }

  formatDate(value: string): string {
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return value;
    return date.toLocaleString();
  }
}
