import { Injectable } from '@angular/core';
import {HttpClient, HttpHeaders} from "@angular/common/http";
import {RegisterRequest} from "../Requests/registerrequest";
import {catchError, Observable, of} from "rxjs";
import {environment} from "../../Environments/environment";
import {LoginRequest} from "../Requests/loginrequest";
import {VerificateCodeRequest} from "../Requests/verificatecoderequest";
import {NewPasswordRequest} from "../Requests/newpasswordrequest";
import {CookieService} from "ngx-cookie-service";

@Injectable({
  providedIn: 'root'
})
export class AuthService {

  constructor(
    private httpClient: HttpClient,
    private cookieService: CookieService
  ) { }

  public register(user: RegisterRequest): Observable<any> {
    return this.httpClient.post<any>(`${environment.apiUrl}/public/register`, user);
  }

  public login(user: LoginRequest): Observable<any> {
    return this.httpClient.post<any>(`${environment.apiUrl}/public/login`, user);
  }

  public isLoggedIn() {
    return this.cookieService.get("uiAppToken") != null;
  }

  isLoggedInUser(): Observable<boolean> {
    const token = this.cookieService.get('uiAppToken');
    return of(!!token);
  }

  getUserProfile(): Observable<any> {
    const token = this.cookieService.get('uiAppToken');

    const headers = new HttpHeaders()
      .set('Authorization', `Bearer ${token}`);

    return this.httpClient.get<any>(`${environment.apiUrl}/user/profile`, { headers }).pipe(
      catchError(err => of(null))
    );
  }

  public returnToken() {
    return this.cookieService.get("uiAppToken");
  }

  public isAdminLoggedIn() {
    return this.cookieService.get('uiAppAdmin') === 'admin';
  }

  public isEmailConfirmed() {
    return JSON.parse(this.cookieService.get("uiAppEmailConfirmation") || 'true');
  }
}
