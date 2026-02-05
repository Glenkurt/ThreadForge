import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ProfileAnalysisService } from '../../services/profile-analysis.service';
import { ProfileAnalysisResponse } from '../../models/profile-analysis.model';
import { ClipboardService } from '../../services/clipboard.service';

@Component({
  selector: 'app-profile-analysis',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './profile-analysis.component.html',
  styleUrl: './profile-analysis.component.css'
})
export class ProfileAnalysisComponent {
  private readonly profileService = inject(ProfileAnalysisService);
  private readonly clipboardService = inject(ClipboardService);

  username = signal('');
  isLoading = signal(false);
  error = signal<string | null>(null);
  result = signal<ProfileAnalysisResponse | null>(null);
  copySuccess = signal(false);
  downloadSuccess = signal(false);

  // Collapsible sections state
  expandedSections = signal<Set<string>>(new Set(['overview', 'brandVoice', 'targetAudience']));

  analyzeProfile(): void {
    const usernameValue = this.username().trim();
    
    if (!usernameValue) {
      this.error.set('Please enter a Twitter username');
      return;
    }

    this.isLoading.set(true);
    this.error.set(null);
    this.result.set(null);

    this.profileService.analyzeProfile({ username: usernameValue }).subscribe({
      next: (response) => {
        this.result.set(response);
        this.isLoading.set(false);
      },
      error: (err) => {
        this.isLoading.set(false);
        if (err.status === 400) {
          this.error.set(err.error?.message || 'Invalid username format');
        } else if (err.status === 404) {
          this.error.set(`Twitter profile @${usernameValue} not found`);
        } else if (err.status === 429) {
          this.error.set('Rate limit exceeded. Please try again in 10 minutes.');
        } else {
          this.error.set('Profile analysis failed. Please try again.');
        }
      }
    });
  }

  toggleSection(section: string): void {
    const current = this.expandedSections();
    const newSet = new Set(current);
    if (newSet.has(section)) {
      newSet.delete(section);
    } else {
      newSet.add(section);
    }
    this.expandedSections.set(newSet);
  }

  isSectionExpanded(section: string): boolean {
    return this.expandedSections().has(section);
  }

  async copyToClipboard(): Promise<void> {
    const result = this.result();
    if (!result) return;

    const text = this.generateMarkdownContent(result);
    const success = await this.clipboardService.copy(text);
    if (success) {
      this.copySuccess.set(true);
      setTimeout(() => this.copySuccess.set(false), 2000);
    }
  }

  downloadAnalysis(): void {
    const result = this.result();
    if (!result) return;

    const content = this.generateMarkdownContent(result);
    const date = new Date().toISOString().split('T')[0];
    const filename = `brand-analysis-${result.username}-${date}.md`;

    const blob = new Blob([content], { type: 'text/markdown' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);

    this.downloadSuccess.set(true);
    setTimeout(() => this.downloadSuccess.set(false), 2000);
  }

  private generateMarkdownContent(result: ProfileAnalysisResponse): string {
    const brand = result.brandDescription;
    const date = new Date(result.analyzedAt).toLocaleDateString();

    return `# Brand Analysis: @${result.username}
*Analyzed on ${date}*

## Overview
${brand.overview}

## Brand Voice
**Tone:** ${brand.brandVoice.tone}
**Style:** ${brand.brandVoice.style}
**Personality:** ${brand.brandVoice.personality}

## Target Audience
**Primary Audience:** ${brand.targetAudience.primary}
**Demographics:** ${brand.targetAudience.demographics}

**Pain Points:**
${brand.targetAudience.painPoints.map(p => `- ${p}`).join('\n')}

## Content Pillars
${brand.contentPillars.map(p => `- ${p}`).join('\n')}

## Content Patterns
**Format:** ${brand.contentPatterns.format}
**Length:** ${brand.contentPatterns.length}
**Structure:** ${brand.contentPatterns.structure}

## Engagement Insights
**Top Performing Content:**
${brand.engagementInsights.topPerformingContent.map(c => `- ${c}`).join('\n')}

**Call-to-Action Style:** ${brand.engagementInsights.callToActionStyle}
**Posting Frequency:** ${brand.engagementInsights.postingFrequency}

## Unique Differentiators
${brand.uniqueDifferentiators.map(d => `- ${d}`).join('\n')}

## Recommended Strategy

**Content Types:**
${brand.recommendedStrategy.contentTypes.map(t => `- ${t}`).join('\n')}

**Tone Guidance:** ${brand.recommendedStrategy.toneGuidance}

**Topics to Explore:**
${brand.recommendedStrategy.topicsToExplore.map(t => `- ${t}`).join('\n')}

---
*Generated by ThreadForge | https://threadforge.dev*
`;
  }
}
