import { Component, signal, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { HttpErrorResponse } from '@angular/common/http';
import { TimeoutError } from 'rxjs';

import { TweetImproverService } from '../../services/tweet-improver.service';
import { ClipboardService } from '../../services/clipboard.service';
import {
  ImproveTweetRequest,
  ImproveTweetResponse,
  ImprovementType,
  ToneValue,
  IMPROVEMENT_TYPES
} from '../../models/thread.model';

type ToneOption = {
  label: string;
  value: ToneValue;
};

@Component({
  selector: 'app-improve',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './improve.component.html',
  styleUrl: './improve.component.css'
})
export class ImproveComponent {
  private readonly improverService = inject(TweetImproverService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly clipboardService = inject(ClipboardService);

  // Expose Math for template
  readonly Math = Math;

  // Form signals
  readonly draft = signal('');
  readonly selectedImprovementType = signal<ImprovementType>('more_engaging');
  readonly selectedTone = signal<ToneValue>(null);
  readonly preserveElements = signal('');
  readonly additionalInstructions = signal('');

  // Validation signals
  readonly draftTouched = signal(false);

  // Generation state
  readonly isImproving = signal(false);
  readonly result = signal<ImproveTweetResponse | null>(null);
  readonly selectedVersion = signal<'improved' | 'alt1' | 'alt2'>('improved');

  // Copy state
  readonly copiedVersion = signal<string | null>(null);

  // Options
  readonly improvementTypes = IMPROVEMENT_TYPES;

  readonly toneOptions: ToneOption[] = [
    { label: 'Default', value: null },
    { label: 'Indie Hacker', value: 'indie_hacker' },
    { label: 'Professional', value: 'professional' },
    { label: 'Humorous', value: 'humorous' },
    { label: 'Motivational', value: 'motivational' },
    { label: 'Educational', value: 'educational' },
    { label: 'Provocative', value: 'provocative' },
    { label: 'Storytelling', value: 'storytelling' },
    { label: 'Clear & Practical', value: 'clear_practical' }
  ];

  // Computed validation
  readonly draftError = computed(() => {
    const value = this.draft().trim();
    if (!this.draftTouched()) return null;
    if (value.length === 0) return 'Please enter a tweet draft.';
    if (value.length > 500) return 'Draft must not exceed 500 characters.';
    return null;
  });

  readonly preserveError = computed(() => {
    const value = this.preserveElements();
    if (value.length > 200) return 'Must not exceed 200 characters.';
    return null;
  });

  readonly instructionsError = computed(() => {
    const value = this.additionalInstructions();
    if (value.length > 300) return 'Must not exceed 300 characters.';
    return null;
  });

  readonly isFormValid = computed(() => {
    const draftValue = this.draft().trim();
    return (
      draftValue.length >= 1 &&
      draftValue.length <= 500 &&
      this.preserveElements().length <= 200 &&
      this.additionalInstructions().length <= 300
    );
  });

  readonly charCount = computed(() => this.draft().length);
  readonly charCountClass = computed(() => {
    const count = this.charCount();
    if (count > 500) return 'over-limit';
    if (count > 400) return 'near-limit';
    return '';
  });

  readonly currentImprovedTweet = computed(() => {
    const res = this.result();
    if (!res) return null;

    const version = this.selectedVersion();
    if (version === 'improved') return res.improved;
    if (version === 'alt1' && res.alternatives[0]) return res.alternatives[0];
    if (version === 'alt2' && res.alternatives[1]) return res.alternatives[1];
    return res.improved;
  });

  readonly improvedCharCount = computed(() => {
    const tweet = this.currentImprovedTweet();
    return tweet ? tweet.length : 0;
  });

  readonly improvedCharClass = computed(() => {
    const count = this.improvedCharCount();
    if (count > 280) return 'over-limit';
    if (count > 260) return 'near-limit';
    return 'within-limit';
  });

  // Methods
  selectImprovementType(value: ImprovementType): void {
    this.selectedImprovementType.set(value);
  }

  selectTone(value: ToneValue): void {
    this.selectedTone.set(value);
  }

  onDraftBlur(): void {
    this.draftTouched.set(true);
  }

  selectVersion(version: 'improved' | 'alt1' | 'alt2'): void {
    this.selectedVersion.set(version);
  }

  async copyToClipboard(): Promise<void> {
    const tweet = this.currentImprovedTweet();
    if (!tweet) return;

    const success = await this.clipboardService.copy(tweet);
    if (success) {
      this.copiedVersion.set(this.selectedVersion());
      setTimeout(() => this.copiedVersion.set(null), 2000);
    } else {
      this.showErrorToast('Failed to copy to clipboard');
    }
  }

  useAsNewDraft(): void {
    const tweet = this.currentImprovedTweet();
    if (!tweet) return;

    this.draft.set(tweet);
    this.result.set(null);
    this.draftTouched.set(false);
  }

  clearAll(): void {
    this.draft.set('');
    this.preserveElements.set('');
    this.additionalInstructions.set('');
    this.result.set(null);
    this.draftTouched.set(false);
    this.selectedVersion.set('improved');
  }

  onImprove(): void {
    this.draftTouched.set(true);

    if (!this.isFormValid()) {
      return;
    }

    this.isImproving.set(true);

    const request: ImproveTweetRequest = {
      draft: this.draft().trim(),
      improvementType: this.selectedImprovementType(),
      tone: this.selectedTone(),
      preserveElements: this.preserveElements().trim() || null,
      additionalInstructions: this.additionalInstructions().trim() || null
    };

    this.improverService.improveTweet(request).subscribe({
      next: response => {
        this.result.set(response);
        this.isImproving.set(false);
        this.selectedVersion.set('improved');
      },
      error: error => {
        this.isImproving.set(false);
        this.handleError(error);
      }
    });
  }

  private handleError(error: HttpErrorResponse | Error): void {
    let message: string;

    if (error instanceof TimeoutError) {
      message = 'Request took too long. Please try again.';
    } else if (error instanceof HttpErrorResponse) {
      if (error.status === 429) {
        message = 'Daily limit reached. Try again tomorrow.';
      } else if (error.status === 500 || error.status === 503) {
        message = 'Improvement failed. Please try again.';
      } else if (error.status === 400) {
        message = error.error?.message || 'Invalid request. Please check your input.';
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
