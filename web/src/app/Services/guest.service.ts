import { Injectable } from '@angular/core';
import {HttpClient, HttpParams} from "@angular/common/http";
import {environment} from "../../Environments/environment";
import {Observable} from "rxjs";
import {GuestOrderRequest} from "../Requests/guestorderrequest";
import {CustomizationRequest} from "../Requests/customizationrequest";

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

  cancelOrder(orderId: number): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/cancel-order/${orderId}`, {});
  }

  cancelOrderByToken(token: string): Observable<any> {
    const params = new HttpParams().set('token', token);
    return this.http.post<any>(`${this.baseUrl}/cancel-order-by-token`, null, { params });
  }

  decrementProductQuantity(request: CustomizationRequest): Observable<any> {
    return this.http.delete<any>(`${this.baseUrl}/decrement-product-quantity`, { body: request });
  }

  cancelOrderByCancellationToken(cancellationToken: string): Observable<any> {
    const params = new HttpParams().set('cancellationToken', cancellationToken);
    return this.http.post<any>(`${this.baseUrl}/cancel`, null, { params });
  }

  confirmPayment(orderId: number): Observable<any> {
    const params = new HttpParams().set('orderId', orderId.toString());
    return this.http.post<any>(`${this.baseUrl}/confirm-payment`, null, { params });
  }

  followOrder(followToken: string): Observable<any> {
    const params = new HttpParams().set('followToken', followToken);
    return this.http.post<any>(`${this.baseUrl}/follow-order`, null, { params });
  }

  orderInformation(orderId: number): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/order/${orderId}`);
  }
}
