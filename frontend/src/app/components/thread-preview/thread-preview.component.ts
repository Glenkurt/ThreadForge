import { Component, Input, Output, EventEmitter, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TweetCardComponent } from '../tweet-card/tweet-card.component';

@Component({
  selector: 'app-thread-preview',
  standalone: true,
  imports: [CommonModule, TweetCardComponent],
  templateUrl: './thread-preview.component.html',
  styleUrl: './thread-preview.component.css'
})
export class ThreadPreviewComponent {
  private readonly snackBar = inject(MatSnackBar);

  @Input() tweets: string[] | null = null;
  @Input() isGenerating: boolean = false;
  @Output() tweetEdited = new EventEmitter<{ index: number; newText: string }>();

  // Copy all state
  allCopied = false;

  copyAllTweets(): void {
    if (!this.tweets) return;

    const formattedThread = this.tweets
      .map((tweet, index) => `${index + 1}/ ${tweet}`)
      .join('\n\n');

    navigator.clipboard.writeText(formattedThread).then(
      () => {
        this.showToast('Thread copied to clipboard');
        this.allCopied = true;
        setTimeout(() => this.allCopied = false, 1500);
      },
      () => {
        this.showToast('Failed to copy. Please try again.');
      }
    );
  }

  onTweetEdited(event: { index: number; newText: string }): void {
    this.tweetEdited.emit(event);
  }

  private showToast(message: string): void {
    this.snackBar.open(message, 'Close', {
      duration: 3000,
      horizontalPosition: 'center',
      verticalPosition: 'top'
    });
  }
}
