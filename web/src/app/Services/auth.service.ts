import { Injectable } from '@angular/core';
import {HttpClient} from "@angular/common/http";
import {RegisterRequest} from "../Requests/registerrequest";
import {Observable} from "rxjs";
import {environment} from "../../Environments/environment";
import {LoginRequest} from "../Requests/loginrequest";
import {VerificateCodeRequest} from "../Requests/verificatecoderequest";
import {NewPasswordRequest} from "../Requests/newpasswordrequest";

@Injectable({
  providedIn: 'root'
})
export class AuthService {

  constructor(private httpClient: HttpClient) { }

  public register(user: RegisterRequest): Observable<any> {
    return this.httpClient.post<any>(`${environment.apiUrl}/public/register`, user);
  }

  public login(user: LoginRequest): Observable<any> {
    return this.httpClient.post<any>(`${environment.apiUrl}/public/login`, user);
  }

  public forgotPassword(email: string): Observable<any> {
    return this.httpClient.put<any>(`${environment.apiUrl}/public/forgot-password`, email);
  }

  public verificationCode(user: VerificateCodeRequest) : Observable<any> {
    return this.httpClient.post<any>(`${environment.apiUrl}/public/verificate-code`, user);
  }

  public newPassword(user: NewPasswordRequest) : Observable<any> {
    return this.httpClient.put<any>(`${environment.apiUrl}/public/new-password`, user);
  }

  public isLoggedIn() {
    return localStorage.getItem("uiAppToken") != null;
  }

  public isAdminLoggedIn() {
    return localStorage.getItem("uiAppAdmin") != null;
  }
}
