import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-tweet-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './tweet-card.component.html',
  styleUrl: './tweet-card.component.css'
})
export class TweetCardComponent {
  @Input({ required: true }) tweet!: string;
  @Input({ required: true }) index!: number;

  get charCount(): number {
    return this.tweet.length;
  }

  get isOverLimit(): boolean {
    return this.charCount > 280;
  }
}
