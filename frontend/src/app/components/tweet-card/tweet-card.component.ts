import { Component, Input, Output, EventEmitter, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ClipboardService } from '../../services/clipboard.service';

@Component({
  selector: 'app-tweet-card',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './tweet-card.component.html',
  styleUrl: './tweet-card.component.css'
})
export class TweetCardComponent {
  private readonly snackBar = inject(MatSnackBar);
  private readonly clipboardService = inject(ClipboardService);

  @Input({ required: true }) tweet!: string;
  @Input({ required: true }) index!: number;
  @Input() isRegenerating = false;
  @Output() tweetEdited = new EventEmitter<{ index: number; newText: string }>();
  @Output() regenerateRequested = new EventEmitter<{ index: number; feedback?: string }>();

  // Copy state
  copied = false;

  // Edit state
  isEditing = false;
  editedText = '';

  // Regenerate state
  showRegenerateInput = signal(false);
  regenerateFeedback = signal('');

  get charCount(): number {
    return this.isEditing ? this.editedText.length : this.tweet.length;
  }

  get isOverLimit(): boolean {
    return this.charCount > 280;
  }

  // Copy functionality
  async copyTweet(text: string): Promise<void> {
    const success = await this.clipboardService.copy(text);
    if (success) {
      this.showToast('Tweet copied to clipboard');
      this.copied = true;
      setTimeout(() => this.copied = false, 1500);
    } else {
      this.showToast('Failed to copy. Please try again.');
    }
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

  // Regenerate functionality
  toggleRegenerateInput(): void {
    this.showRegenerateInput.update(v => !v);
    if (!this.showRegenerateInput()) {
      this.regenerateFeedback.set('');
    }
  }

  requestRegenerate(): void {
    const feedback = this.regenerateFeedback().trim() || undefined;
    this.regenerateRequested.emit({ index: this.index, feedback });
    this.showRegenerateInput.set(false);
    this.regenerateFeedback.set('');
  }

  onRegenerateKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.requestRegenerate();
    }
    if (event.key === 'Escape') {
      this.toggleRegenerateInput();
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
