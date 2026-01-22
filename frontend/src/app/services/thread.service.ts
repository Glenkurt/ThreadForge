import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, catchError, throwError } from 'rxjs';
import { timeout } from 'rxjs/operators';
import { GenerateThreadRequest, GenerateThreadResponse } from '../models/thread.model';

@Injectable({
  providedIn: 'root'
})
export class ThreadService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = '/api/v1/threads/generate';
  private readonly timeout_ms = 15000; // 15 second timeout

  generateThread(request: GenerateThreadRequest): Observable<GenerateThreadResponse> {
    return this.http.post<GenerateThreadResponse>(this.apiUrl, request).pipe(
      timeout(this.timeout_ms),
      catchError((error: HttpErrorResponse) => {
        return throwError(() => error);
      })
    );
  }
}
