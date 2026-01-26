import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, catchError, throwError } from 'rxjs';
import { timeout } from 'rxjs/operators';

import { ThreadHistoryDetail, ThreadHistoryListItem } from '../models/thread-history.model';

@Injectable({
  providedIn: 'root'
})
export class ThreadHistoryService {
  private readonly http = inject(HttpClient);
  private readonly timeout_ms = 15000;

  list(limit: number = 20, offset: number = 0): Observable<ThreadHistoryListItem[]> {
    const url = `/api/v1/threads/history?limit=${encodeURIComponent(limit)}&offset=${encodeURIComponent(offset)}`;

    return this.http.get<ThreadHistoryListItem[]>(url).pipe(
      timeout(this.timeout_ms),
      catchError((error: HttpErrorResponse) => throwError(() => error))
    );
  }

  getById(id: string): Observable<ThreadHistoryDetail> {
    return this.http.get<ThreadHistoryDetail>(`/api/v1/threads/history/${encodeURIComponent(id)}`).pipe(
      timeout(this.timeout_ms),
      catchError((error: HttpErrorResponse) => throwError(() => error))
    );
  }
}
