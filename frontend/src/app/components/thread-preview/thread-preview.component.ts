import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TweetCardComponent } from '../tweet-card/tweet-card.component';

@Component({
  selector: 'app-thread-preview',
  standalone: true,
  imports: [CommonModule, TweetCardComponent],
  templateUrl: './thread-preview.component.html',
  styleUrl: './thread-preview.component.css'
})
export class ThreadPreviewComponent {
  @Input() tweets: string[] | null = null;
  @Input() isGenerating: boolean = false;
}
