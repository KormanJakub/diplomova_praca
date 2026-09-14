import { Injectable } from '@angular/core';
import {environment} from "../../Environments/environment";
import {HttpClient, HttpParams} from "@angular/common/http";
import {Observable} from "rxjs";
import {PaymentRequestModel} from "../Requests/paymentrequest";

@Injectable({
  providedIn: 'root'
})
export class PaymentService {

  private baseUrl = environment.apiUrl;

  constructor(private http: HttpClient) { }

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

  verifyPayment(sessionId: string): Observable<any> {
    return this.http.post<any>(
      `${this.baseUrl}/payment/verify-payment?sessionId=${sessionId}`,
      {}
    );
  }
}
