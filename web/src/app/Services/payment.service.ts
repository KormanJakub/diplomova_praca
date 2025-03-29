import { Injectable } from '@angular/core';
import {environment} from "../../Environments/environment";
import {HttpClient, HttpHeaders, HttpParams} from "@angular/common/http";
import {Observable} from "rxjs";
import {CookieService} from "ngx-cookie-service";
import {PaymentRequestModel} from "../Requests/paymentrequest";

export interface PaymentRequest {
  productName: string;
  amount: number;
  quantity: number;
}

@Injectable({
  providedIn: 'root'
})
export class PaymentService {

  private baseUrl = environment.apiUrl;

  constructor(private http: HttpClient, private cookieService: CookieService) { }

  createStripeSession(request: PaymentRequestModel): Observable<any> {

    return this.http.post<any>(
      `${this.baseUrl}/payment/create-checkout-session`,
      request
    );
  }

  cancelOrder(cancellationToken: string): Observable<any> {
    const params = new HttpParams().set('cancellationToken', cancellationToken);

    return this.http.post<any>(`${this.baseUrl}/guest/cancel`, {}, {params: params});
  }

  confirmPayment(orderId: string): Observable<any> {
    const params = new HttpParams().set('orderId', orderId);

    return this.http.post<any>(`${this.baseUrl}/guest/confirm-payment`, {}, {params: params});
  }

  verifyPayment(sessionId: string): Observable<any> {
    const token = this.cookieService.get('uiAppToken');
    const headers = new HttpHeaders({
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    });

    return this.http.post<any>(
      `${this.baseUrl}/payment/verify-payment?sessionId=${sessionId}`,
      {},
      { headers }
    );
  }
}
