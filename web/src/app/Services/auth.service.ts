import { Injectable } from '@angular/core';
import {HttpClient} from "@angular/common/http";
import {RegisterRequest} from "../Requests/registerrequest";
import {Observable} from "rxjs";
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
