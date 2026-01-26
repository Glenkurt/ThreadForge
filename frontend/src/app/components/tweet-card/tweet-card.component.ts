import { Component, Input, Output, EventEmitter, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';

@Component({
  selector: 'app-tweet-card',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './tweet-card.component.html',
  styleUrl: './tweet-card.component.css'
})
export class TweetCardComponent {
  private readonly snackBar = inject(MatSnackBar);

  @Input({ required: true }) tweet!: string;
  @Input({ required: true }) index!: number;
  @Output() tweetEdited = new EventEmitter<{ index: number; newText: string }>();

  // Copy state
  copied = false;

  // Edit state
  isEditing = false;
  editedText = '';

  get charCount(): number {
    return this.isEditing ? this.editedText.length : this.tweet.length;
  }

  get isOverLimit(): boolean {
    return this.charCount > 280;
  }

  // Copy functionality
  copyTweet(text: string): void {
    navigator.clipboard.writeText(text).then(
      () => {
        this.showToast('Tweet copied to clipboard');
        this.copied = true;
        setTimeout(() => this.copied = false, 1500);
      },
      () => {
        this.showToast('Failed to copy. Please try again.');
      }
    );
  }

  // Edit functionality
  startEditing(): void {
    this.editedText = this.tweet;
    this.isEditing = true;
  }

  saveEdit(): void {
    const trimmed = this.editedText.trim();
    if (trimmed.length > 0 && trimmed !== this.tweet) {
      this.tweetEdited.emit({ index: this.index, newText: trimmed });
    }
    this.isEditing = false;
  }

  cancelEdit(): void {
    this.isEditing = false;
  }

  onKeydown(event: KeyboardEvent): void {
    if ((event.metaKey || event.ctrlKey) && event.key === 'Enter') {
      event.preventDefault();
      this.saveEdit();
      return;
    }
    if (event.key === 'Escape') {
      this.cancelEdit();
    }
  }

  private showToast(message: string): void {
    this.snackBar.open(message, 'Close', {
      duration: 3000,
      horizontalPosition: 'center',
      verticalPosition: 'top'
    });
  }
}
