import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, catchError, throwError } from 'rxjs';
import { BrandGuideline } from '../models/brand-guideline.model';

@Injectable({
  providedIn: 'root'
})
export class BrandGuidelineService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = '/api/v1/brand-guidelines';

  getBrandGuideline(): Observable<BrandGuideline> {
    return this.http.get<BrandGuideline>(this.apiUrl).pipe(
      catchError((error: HttpErrorResponse) => throwError(() => error))
    );
  }

  saveBrandGuideline(text: string): Observable<BrandGuideline> {
    return this.http.put<BrandGuideline>(this.apiUrl, { text }).pipe(
      catchError((error: HttpErrorResponse) => throwError(() => error))
    );
  }
}