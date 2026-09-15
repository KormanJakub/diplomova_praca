import {Injectable} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable, finalize, of, tap} from 'rxjs';
import {environment} from '../../Environments/environment';
import {RegisterRequest} from '../Requests/registerrequest';
import {LoginRequest} from '../Requests/loginrequest';

export interface UiSession {
  role: 'admin' | 'user';
  firstName: string;
  email_confirmation: boolean;
}

@Injectable({providedIn: 'root'})
export class AuthService {
  private readonly sessionKey = 'waffl_ui_session';

  constructor(private httpClient: HttpClient) {}

  register(user: RegisterRequest): Observable<any> {
    return this.httpClient.post<any>(`${environment.apiUrl}/public/register`, user);
  }

  login(user: LoginRequest): Observable<UiSession> {
    return this.httpClient.post<UiSession>(`${environment.apiUrl}/public/login`, user).pipe(
      tap(session => this.saveSession(session))
    );
  }

  logout(): Observable<void> {
    return this.httpClient.post<void>(`${environment.apiUrl}/public/logout`, {}).pipe(
      finalize(() => localStorage.removeItem(this.sessionKey))
    );
  }

  refreshSession(): Observable<UiSession> {
    return this.httpClient.get<UiSession>(`${environment.apiUrl}/public/session`).pipe(
      tap(session => this.saveSession(session))
    );
  }

  requestPasswordReset(email: string): Observable<any> {
    return this.httpClient.put<any>(`${environment.apiUrl}/public/forgot-password`, null, {params: {email}});
  }

  resetPassword(email: string, token: string, newPassword: string, repeatNewPassword: string): Observable<any> {
    return this.httpClient.put<any>(`${environment.apiUrl}/public/new-password`,
      {Email: email, Token: token, NewPassword: newPassword, RepeatNewPassword: repeatNewPassword});
  }

  verifyEmailCode(email: string, verificationCode: number): Observable<any> {
    return this.httpClient.post<any>(`${environment.apiUrl}/public/verification-code`,
      {Email: email, VerificationCode: verificationCode});
  }

  resendEmailCode(email: string): Observable<any> {
    return this.httpClient.post<any>(`${environment.apiUrl}/public/new-verification-code`, {Email: email});
  }

  isLoggedIn(): boolean { return this.getSession() !== null; }
  isLoggedInUser(): Observable<boolean> { return of(this.isLoggedIn()); }
  isAdminLoggedIn(): boolean { return this.getSession()?.role === 'admin'; }
  isEmailConfirmed(): boolean { return this.getSession()?.email_confirmation === true; }
  getSession(): UiSession | null {
    try {
      const value = localStorage.getItem(this.sessionKey);
      return value ? JSON.parse(value) as UiSession : null;
    } catch {
      localStorage.removeItem(this.sessionKey);
      return null;
    }
  }

  getUserProfile(): Observable<any> {
    return this.httpClient.get<any>(`${environment.apiUrl}/user/profile`);
  }

  private saveSession(session: UiSession): void {
    localStorage.setItem(this.sessionKey, JSON.stringify(session));
  }
}
