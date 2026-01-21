import { Component, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

type ToneOption = {
  label: string;
  value: string | null;
};

interface GeneratorFormState {
  topic: string;
  audience: string | null;
  tone: string | null;
  tweetCount: number;
}

@Component({
  selector: 'app-generator',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './generator.component.html',
  styleUrl: './generator.component.css'
})
export class GeneratorComponent {
  // Form signals
  readonly topic = signal('');
  readonly audience = signal('');
  readonly selectedTone = signal<string | null>(null);
  readonly tweetCount = signal(7);

  // Validation signals
  readonly topicTouched = signal(false);
  readonly audienceTouched = signal(false);

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
  selectTone(value: string | null): void {
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

    const formState: GeneratorFormState = {
      topic: this.topic().trim(),
      audience: this.audience().trim() || null,
      tone: this.selectedTone(),
      tweetCount: this.tweetCount()
    };

    // Task 1: No API call - just emit form state for validation
    // eslint-disable-next-line no-console
    console.log('Generate Thread:', formState);
  }
}
