import { Injectable } from '@angular/core';
import {HttpClient, HttpParams} from "@angular/common/http";
import {environment} from "../../Environments/environment";
import {Observable} from "rxjs";
import {GuestOrderRequest} from "../Requests/guestorderrequest";

class GuestCustomizationRequest {
}

@Injectable({
  providedIn: 'root'
})
export class GuestService {
  private baseUrl = `${environment.apiUrl}/guest`;

  constructor(private http: HttpClient) { }

  makeCustomizationWithoutRegister(request: GuestCustomizationRequest): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/make-customization-without-register`, request);
  }

  makeOrderWithoutRegister(request: GuestOrderRequest): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/make-order-without-register`, request);
  }

  cancelOrderByToken(token: string): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/cancel-order-by-token`, {Token: token});
  }

  cancelOrderByCancellationToken(cancellationToken: string): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/cancel`, {Token: cancellationToken});
  }

  followOrder(followToken: string): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/follow-order`, {Token: followToken});
  }

  orderInformation(orderId: number, followToken: string): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/order/${orderId}`, {Token: followToken});
  }
}
