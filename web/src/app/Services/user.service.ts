import { Injectable } from '@angular/core';
import {HttpClient, HttpHeaders} from "@angular/common/http";
import {CookieService} from "ngx-cookie-service";
import {Observable} from "rxjs";
import {User} from "../Models/user.model";
import {environment} from "../../Environments/environment";
import {Tag} from "../Models/tag.model";

@Injectable({
  providedIn: 'root'
})
export class UserService {

  constructor(
    private http: HttpClient,
    private cookieService: CookieService
  ) { }

  getUserProfile(): Observable<User> {
    const token = this.cookieService.get('uiAppToken');
    const url = `${environment.apiUrl}/user/profile`;

    const headers = new HttpHeaders()
      .set('Authorization', `Bearer ${token}`)
      .set('Content-Type', 'application/json');

    return this.http.get<User>(url, {
      headers: headers
    });
  }
}
