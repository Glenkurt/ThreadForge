import { Injectable } from '@angular/core';

/**
 * Service for clipboard operations with HTTP fallback.
 *
 * The modern Clipboard API requires a secure context (HTTPS or localhost).
 * This service provides a fallback using the legacy execCommand for HTTP deployments.
 */
@Injectable({ providedIn: 'root' })
export class ClipboardService {
  /**
   * Copy text to clipboard with automatic fallback for HTTP contexts.
   * @param text The text to copy
   * @returns Promise resolving to true if successful, false otherwise
   */
  async copy(text: string): Promise<boolean> {
    // Try modern Clipboard API first (requires secure context)
    if (navigator.clipboard && window.isSecureContext) {
      try {
        await navigator.clipboard.writeText(text);
        return true;
      } catch {
        // Fall through to legacy method
        return this.fallbackCopy(text);
      }
    }

    // Use legacy method for HTTP contexts
    return this.fallbackCopy(text);
  }

  /**
   * Legacy clipboard copy using execCommand.
   * Works in HTTP contexts but is deprecated.
   */
  private fallbackCopy(text: string): boolean {
    const textArea = document.createElement('textarea');
    textArea.value = text;

    // Prevent scrolling to bottom of page
    textArea.style.cssText = 'position:fixed;left:-9999px;top:-9999px;opacity:0';

    document.body.appendChild(textArea);
    textArea.focus();
    textArea.select();

    try {
      return document.execCommand('copy');
    } catch {
      return false;
    } finally {
      textArea.remove();
    }
  }
}
