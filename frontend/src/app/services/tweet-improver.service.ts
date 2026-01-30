import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, catchError, throwError } from 'rxjs';
import { timeout } from 'rxjs/operators';
import { ImproveTweetRequest, ImproveTweetResponse } from '../models/thread.model';

@Injectable({
  providedIn: 'root'
})
export class TweetImproverService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = '/api/v1/tweets';
  private readonly timeout_ms = 20000; // 20 second timeout

  improveTweet(request: ImproveTweetRequest): Observable<ImproveTweetResponse> {
    return this.http.post<ImproveTweetResponse>(`${this.apiUrl}/improve`, request).pipe(
      timeout(this.timeout_ms),
      catchError((error: HttpErrorResponse) => {
        return throwError(() => error);
      })
    );
  }

  getImprovementTypes(): Observable<Record<string, string>> {
    return this.http.get<Record<string, string>>(`${this.apiUrl}/improvement-types`);
  }
}
