import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Observable, catchError, throwError } from 'rxjs';
import { timeout } from 'rxjs/operators';
import { GenerateThreadRequest, GenerateThreadResponse, RegenerateTweetRequest, RegenerateTweetResponse } from '../models/thread.model';
import { MockThreadDataService } from './mock-thread-data.service';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ThreadService {
  private readonly http = inject(HttpClient);
  private readonly mockService = inject(MockThreadDataService);
  private readonly apiUrl = '/api/v1/threads';
  private readonly timeout_ms = 15000; // 15 second timeout

  generateThread(request: GenerateThreadRequest): Observable<GenerateThreadResponse> {
    if (environment.useMockData) {
      return this.mockService.generateThread(request);
    }

    return this.http.post<GenerateThreadResponse>(`${this.apiUrl}/generate`, request).pipe(
      timeout(this.timeout_ms),
      catchError((error: HttpErrorResponse) => {
        return throwError(() => error);
      })
    );
  }

  regenerateTweet(
    tweets: string[],
    index: number,
    feedback?: string,
    tone?: string,
    maxChars: number = 260
  ): Observable<RegenerateTweetResponse> {
    let params = new HttpParams()
      .set('index', index.toString())
      .set('maxChars', maxChars.toString());

    // Add each tweet as a separate query param
    tweets.forEach(tweet => {
      params = params.append('tweets', tweet);
    });

    if (tone) {
      params = params.set('tone', tone);
    }

    const body: RegenerateTweetRequest = feedback ? { feedback } : {};

    return this.http.post<RegenerateTweetResponse>(
      `${this.apiUrl}/regenerate-tweet`,
      body,
      { params }
    ).pipe(
      timeout(this.timeout_ms),
      catchError((error: HttpErrorResponse) => {
        return throwError(() => error);
      })
    );
  }
}
