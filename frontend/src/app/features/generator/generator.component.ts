import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { HttpErrorResponse } from '@angular/common/http';
import { TimeoutError } from 'rxjs';

import { ThreadService } from '../../services/thread.service';
import { GenerateThreadRequest, ToneValue, HookStrength, CtaType, StylePreferences, ThreadQuality, FEEDBACK_SUGGESTIONS } from '../../models/thread.model';
import { ThreadPreviewComponent } from '../../components/thread-preview/thread-preview.component';
import { BrandGuidelinesComponent } from '../../components/brand-guidelines/brand-guidelines.component';
import { BrandGuidelineService } from '../../services/brand-guideline.service';

type ToneOption = {
  label: string;
  value: ToneValue;
};

type HookOption = {
  label: string;
  value: HookStrength;
};

type CtaOption = {
  label: string;
  value: CtaType;
};

type TriState = true | false | null;

@Component({
  selector: 'app-generator',
  standalone: true,
  imports: [CommonModule, FormsModule, ThreadPreviewComponent, BrandGuidelinesComponent],
  templateUrl: './generator.component.html',
  styleUrl: './generator.component.css'
})
export class GeneratorComponent implements OnInit {
  private readonly threadService = inject(ThreadService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly brandGuidelineService = inject(BrandGuidelineService);

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
  readonly generatedThreadId = signal<string | null>(null);
  readonly threadQuality = signal<ThreadQuality | null>(null);
  readonly threadHashtags = signal<string[] | null>(null);

  // Feedback state for regeneration
  readonly showFeedback = signal(false);
  readonly feedback = signal('');
  readonly feedbackSuggestions = FEEDBACK_SUGGESTIONS;

  // Advanced options state
  readonly showAdvancedOptions = signal(false);
  readonly brandGuidelines = signal('');
  readonly exampleThreads = signal<string[]>([]);

  // Style preferences
  readonly useEmojis = signal<TriState>(null);
  readonly useNumbering = signal<TriState>(true);
  readonly maxCharsPerTweet = signal(260);
  readonly hookStrength = signal<HookStrength>(null);
  readonly ctaType = signal<CtaType>(null);

  // Tone options (expanded list)
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

  // Hook options
  readonly hookOptions: HookOption[] = [
    { label: 'Default', value: null },
    { label: 'Bold', value: 'bold' },
    { label: 'Question', value: 'question' },
    { label: 'Story', value: 'story' },
    { label: 'Statistic', value: 'stat' }
  ];

  // CTA options
  readonly ctaOptions: CtaOption[] = [
    { label: 'Default', value: null },
    { label: 'Soft', value: 'soft' },
    { label: 'Direct', value: 'direct' },
    { label: 'Question', value: 'question' }
  ];

  // Computed validation
  readonly topicError = computed(() => {
    const value = this.topic().trim();
    if (!this.topicTouched()) return null;
    if (value.length === 0) return 'Please enter a topic.';
    if (value.length > 1000) return 'Topic must not exceed 1000 characters.';
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

  readonly feedbackError = computed(() => {
    const value = this.feedback().trim();
    if (value.length > 1000) {
      return 'Feedback must be 1000 characters or less.';
    }
    return null;
  });

  readonly brandGuidelinesError = computed(() => {
    const value = this.brandGuidelines();
    if (value.length > 1500) {
      return 'Brand guidelines must be 1500 characters or less.';
    }
    return null;
  });

  readonly maxCharsError = computed(() => {
    const value = this.maxCharsPerTweet();
    if (value < 200) return 'Min 200 characters';
    if (value > 280) return 'Max 280 characters';
    return null;
  });

  readonly isFormValid = computed(() => {
    const topicValue = this.topic().trim();
    const audienceValue = this.audience().trim();

    const topicValid = topicValue.length >= 1 && topicValue.length <= 1000;
    const audienceValid = audienceValue.length === 0 || audienceValue.length <= 80;
    const brandGuidelinesValid = this.brandGuidelines().length <= 1500;
    const maxCharsValid = this.maxCharsPerTweet() >= 200 && this.maxCharsPerTweet() <= 280;
    const exampleThreadsValid = this.exampleThreads().every(e => e.length <= 5000);

    return topicValid && audienceValid && brandGuidelinesValid && maxCharsValid && exampleThreadsValid;
  });

  readonly isFeedbackValid = computed(() => {
    return this.feedback().trim().length <= 1000;
  });

  readonly canAddExampleThread = computed(() => {
    return this.exampleThreads().length < 3;
  });

  // Methods
  ngOnInit(): void {
    this.loadBrandGuideline();
  }
  selectTone(value: ToneValue): void {
    this.selectedTone.set(value);
  }

  onTopicBlur(): void {
    this.topicTouched.set(true);
  }

  onAudienceBlur(): void {
    this.audienceTouched.set(true);
  }

  toggleFeedback(): void {
    this.showFeedback.set(!this.showFeedback());
  }

  toggleAdvancedOptions(): void {
    this.showAdvancedOptions.set(!this.showAdvancedOptions());
  }

  // Emoji toggle cycling: null -> true -> false -> null
  cycleEmojis(): void {
    const current = this.useEmojis();
    if (current === null) {
      this.useEmojis.set(true);
    } else if (current === true) {
      this.useEmojis.set(false);
    } else {
      this.useEmojis.set(null);
    }
  }

  // Numbering toggle cycling: true -> false -> null -> true
  cycleNumbering(): void {
    const current = this.useNumbering();
    if (current === true) {
      this.useNumbering.set(false);
    } else if (current === false) {
      this.useNumbering.set(null);
    } else {
      this.useNumbering.set(true);
    }
  }

  getEmojiLabel(): string {
    const value = this.useEmojis();
    if (value === null) return 'Default';
    return value ? 'Yes' : 'No';
  }

  getNumberingLabel(): string {
    const value = this.useNumbering();
    if (value === null) return 'Default';
    return value ? 'Yes' : 'No';
  }

  addExampleThread(): void {
    if (this.canAddExampleThread()) {
      this.exampleThreads.update(threads => [...threads, '']);
    }
  }

  removeExampleThread(index: number): void {
    this.exampleThreads.update(threads => threads.filter((_, i) => i !== index));
  }

  updateExampleThread(index: number, value: string): void {
    this.exampleThreads.update(threads => {
      const updated = [...threads];
      updated[index] = value;
      return updated;
    });
  }

  getExampleThreadError(index: number): string | null {
    const thread = this.exampleThreads()[index];
    if (thread && thread.length > 5000) {
      return 'Example must be 5000 characters or less.';
    }
    return null;
  }

  clearAdvancedOptions(): void {
    this.brandGuidelines.set('');
    this.exampleThreads.set([]);
    this.useEmojis.set(null);
    this.useNumbering.set(true);
    this.maxCharsPerTweet.set(260);
    this.hookStrength.set(null);
    this.ctaType.set(null);
  }

  onTweetEdited(event: { index: number; newText: string }): void {
    const currentThread = this.generatedThread();
    if (!currentThread) return;

    const updatedThread = [...currentThread];
    updatedThread[event.index - 1] = event.newText;
    this.generatedThread.set(updatedThread);
  }

  onFeedbackKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.onRegenerate();
    }
  }

  addFeedbackSuggestion(suggestion: string): void {
    const current = this.feedback().trim();
    if (current.length === 0) {
      this.feedback.set(suggestion);
    } else if (!current.toLowerCase().includes(suggestion.toLowerCase())) {
      this.feedback.set(current + '. ' + suggestion);
    }
    // Show feedback section if hidden
    this.showFeedback.set(true);
  }

  getQualityColor(score: number): string {
    if (score >= 70) return 'quality-good';
    if (score >= 50) return 'quality-ok';
    return 'quality-weak';
  }

  onGenerate(): void {
    this.generateThread(false);
  }

  onRegenerate(): void {
    if (!this.isFeedbackValid()) return;
    this.generateThread(true);
  }

  private generateThread(isRegeneration: boolean): void {
    // Mark fields as touched to show validation
    this.topicTouched.set(true);
    this.audienceTouched.set(true);

    if (!this.isFormValid()) {
      return;
    }

    this.persistBrandGuideline();

    this.isGenerating.set(true);

    // Build style preferences
    const stylePreferences: StylePreferences = {
      useEmojis: this.useEmojis(),
      useNumbering: this.useNumbering(),
      maxCharsPerTweet: this.maxCharsPerTweet(),
      hookStrength: this.hookStrength(),
      ctaType: this.ctaType()
    };

    // Filter non-empty example threads
    const examples = this.exampleThreads().filter(e => e.trim().length > 0);

    const request: GenerateThreadRequest = {
      topic: this.topic().trim(),
      audience: this.audience().trim() || null,
      tone: this.selectedTone(),
      tweetCount: this.tweetCount(),
      brandGuidelines: this.brandGuidelines().trim() || null,
      exampleThreads: examples.length > 0 ? examples : null,
      stylePreferences
    };

    // Include feedback only for regeneration with non-empty feedback
    if (isRegeneration && this.feedback().trim()) {
      request.feedback = this.feedback().trim();
    }

    this.threadService.generateThread(request).subscribe({
      next: response => {
        this.generatedThread.set(response.tweets);
        this.generatedThreadId.set(response.id);
        this.threadQuality.set(response.quality ?? null);
        this.threadHashtags.set(response.hashtags ?? null);
        this.isGenerating.set(false);
        // Clear feedback after successful regeneration
        if (isRegeneration) {
          this.feedback.set('');
        }
      },
      error: error => {
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

  private loadBrandGuideline(): void {
    this.brandGuidelineService.getBrandGuideline().subscribe({
      next: response => {
        this.brandGuidelines.set(response.text || '');
      },
      error: () => {
        this.showErrorToast('Could not load brand guideline. Try again.');
      }
    });
  }

  private persistBrandGuideline(): void {
    const value = this.brandGuidelines().trim();

    this.brandGuidelineService.saveBrandGuideline(value).subscribe({
      error: () => {
        this.showErrorToast('Could not save brand guideline. Try again.');
      }
    });
  }
}
