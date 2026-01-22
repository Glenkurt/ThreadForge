import { Component, signal, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { HttpErrorResponse } from '@angular/common/http';
import { TimeoutError } from 'rxjs';

import { ThreadService } from '../../services/thread.service';
import { GenerateThreadRequest } from '../../models/thread.model';
import { ThreadPreviewComponent } from '../../components/thread-preview/thread-preview.component';

type ToneValue = 'indie_hacker' | 'educational' | 'provocative' | 'direct' | null;

type ToneOption = {
  label: string;
  value: ToneValue;
};

@Component({
  selector: 'app-generator',
  standalone: true,
  imports: [CommonModule, FormsModule, ThreadPreviewComponent],
  templateUrl: './generator.component.html',
  styleUrl: './generator.component.css'
})
export class GeneratorComponent {
  private readonly threadService = inject(ThreadService);
  private readonly snackBar = inject(MatSnackBar);

  // Form signals
  readonly topic = signal('');
  readonly audience = signal('');
  readonly selectedTone = signal<ToneValue>(null);
  readonly tweetCount = signal(7);

  // Validation signals
  readonly topicTouched = signal(false);
  readonly audienceTouched = signal(false);

  // Generation state
  readonly isGenerating = signal(false);
  readonly generatedThread = signal<string[] | null>(null);

  // Tone options
  readonly toneOptions: ToneOption[] = [
    { label: 'Default', value: null },
    { label: 'Indie Hacker', value: 'indie_hacker' },
    { label: 'Educational', value: 'educational' },
    { label: 'Provocative', value: 'provocative' },
    { label: 'Direct', value: 'direct' }
  ];

  // Computed validation
  readonly topicError = computed(() => {
    const value = this.topic().trim();
    if (!this.topicTouched()) return null;
    if (value.length === 0) return 'Please enter a topic.';
    if (value.length > 120) return 'Topic must be 120 characters or less.';
    return null;
  });

  readonly audienceError = computed(() => {
    const value = this.audience().trim();
    if (!this.audienceTouched()) return null;
    if (value.length > 0 && value.length > 80) {
      return 'Audience must be 80 characters or less.';
    }
    return null;
  });

  readonly isFormValid = computed(() => {
    const topicValue = this.topic().trim();
    const audienceValue = this.audience().trim();

    const topicValid = topicValue.length >= 1 && topicValue.length <= 120;
    const audienceValid = audienceValue.length === 0 || audienceValue.length <= 80;

    return topicValid && audienceValid;
  });

  // Methods
  selectTone(value: ToneValue): void {
    this.selectedTone.set(value);
  }

  onTopicBlur(): void {
    this.topicTouched.set(true);
  }

  onAudienceBlur(): void {
    this.audienceTouched.set(true);
  }

  onGenerate(): void {
    // Mark fields as touched to show validation
    this.topicTouched.set(true);
    this.audienceTouched.set(true);

    if (!this.isFormValid()) {
      return;
    }

    this.isGenerating.set(true);
    const startTime = Date.now();

    const request: GenerateThreadRequest = {
      topic: this.topic().trim(),
      audience: this.audience().trim() || null,
      tone: this.selectedTone(),
      tweetCount: this.tweetCount()
    };

    this.threadService.generateThread(request).subscribe({
      next: response => {
        const duration = Date.now() - startTime;
        // eslint-disable-next-line no-console
        console.log(`Thread generation took ${duration}ms`);
        this.generatedThread.set(response.tweets);
        this.isGenerating.set(false);
      },
      error: error => {
        const duration = Date.now() - startTime;
        // eslint-disable-next-line no-console
        console.log(`Thread generation failed after ${duration}ms`);
        this.isGenerating.set(false);
        this.handleError(error);
      }
    });
  }

  private handleError(error: HttpErrorResponse | Error): void {
    let message: string;

    // Check if it's a timeout error
    if (error instanceof TimeoutError) {
      message = 'Request took too long. Please try again.';
    } else if (error instanceof HttpErrorResponse) {
      if (error.status === 429) {
        message = 'Daily limit reached. You can generate 20 threads per day. Try again tomorrow.';
      } else if (error.status === 500 || error.status === 503) {
        message = 'Thread generation failed. Please try again.';
      } else if (error.status === 400) {
        message = 'Invalid request. Please check your inputs.';
      } else if (error.status === 0) {
        message = 'Network error. Check your connection and try again.';
      } else {
        message = 'An unexpected error occurred. Please try again.';
      }
    } else {
      message = 'An unexpected error occurred. Please try again.';
    }

    this.showErrorToast(message);
  }

  private showErrorToast(message: string): void {
    this.snackBar.open(message, 'Close', {
      duration: 5000,
      horizontalPosition: 'center',
      verticalPosition: 'top',
      panelClass: ['error-toast']
    });
  }
}
