import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-brand-guidelines',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './brand-guidelines.component.html',
  styleUrl: './brand-guidelines.component.css'
})
export class BrandGuidelinesComponent {
  @Input() value = '';
  @Input() error: string | null = null;
  @Input() maxLength = 1500;
  @Input() placeholder = 'Describe your brand voice, vocabulary, dos/don\'ts, favorite phrases...';
  @Input() rows = 4;

  @Output() valueChange = new EventEmitter<string>();

  onValueChange(value: string): void {
    this.valueChange.emit(value);
  }
}