import { Injectable } from '@angular/core';
import {HttpClient} from "@angular/common/http";
import {Observable} from "rxjs";
import {environment} from "../../Environments/environment";

@Injectable({
  providedIn: 'root'
})
export class PublicService {

  constructor(private httpClient: HttpClient) { }

  public receiveNews(email: string): Observable<any> {
    return this.httpClient.post<any>(`${environment.apiUrl}/public/receive-news`, { email });
  }
}
