import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, catchError, throwError } from 'rxjs';
import { timeout } from 'rxjs/operators';
import { ProfileAnalysisRequest, ProfileAnalysisResponse } from '../models/profile-analysis.model';

@Injectable({
  providedIn: 'root'
})
export class ProfileAnalysisService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = '/api/v1/profiles/analyze';
  private readonly timeout_ms = 60000; // 60 second timeout (analysis takes longer)

  analyzeProfile(request: ProfileAnalysisRequest): Observable<ProfileAnalysisResponse> {
    return this.http.post<ProfileAnalysisResponse>(this.apiUrl, request).pipe(
      timeout(this.timeout_ms),
      catchError((error: HttpErrorResponse) => {
        return throwError(() => error);
      })
    );
  }
}
