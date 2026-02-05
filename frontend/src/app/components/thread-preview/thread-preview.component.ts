import { Component, Input, Output, EventEmitter, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TweetCardComponent } from '../tweet-card/tweet-card.component';
import { ClipboardService } from '../../services/clipboard.service';

@Component({
  selector: 'app-thread-preview',
  standalone: true,
  imports: [CommonModule, TweetCardComponent],
  templateUrl: './thread-preview.component.html',
  styleUrl: './thread-preview.component.css'
})
export class ThreadPreviewComponent {
  private readonly snackBar = inject(MatSnackBar);
  private readonly clipboardService = inject(ClipboardService);

  @Input() tweets: string[] | null = null;
  @Input() isGenerating: boolean = false;
  @Input() regeneratingIndex: number | null = null;
  @Output() tweetEdited = new EventEmitter<{ index: number; newText: string }>();
  @Output() regenerateRequested = new EventEmitter<{ index: number; feedback?: string }>();

  // Copy all state
  allCopied = false;

  async copyAllTweets(): Promise<void> {
    if (!this.tweets) return;

    const formattedThread = this.tweets
      .map((tweet, index) => `${index + 1}/ ${tweet}`)
      .join('\n\n');

    const success = await this.clipboardService.copy(formattedThread);
    if (success) {
      this.showToast('Thread copied to clipboard');
      this.allCopied = true;
      setTimeout(() => this.allCopied = false, 1500);
    } else {
      this.showToast('Failed to copy. Please try again.');
    }
  }

  onTweetEdited(event: { index: number; newText: string }): void {
    this.tweetEdited.emit(event);
  }

  onRegenerateRequested(event: { index: number; feedback?: string }): void {
    this.regenerateRequested.emit(event);
  }

  isRegeneratingTweet(index: number): boolean {
    return this.regeneratingIndex === index;
  }

  private showToast(message: string): void {
    this.snackBar.open(message, 'Close', {
      duration: 3000,
      horizontalPosition: 'center',
      verticalPosition: 'top'
    });
  }
}
