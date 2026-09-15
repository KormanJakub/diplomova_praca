import { Injectable } from '@angular/core';
import {HttpClient, HttpHeaders} from "@angular/common/http";
import {environment} from "../../Environments/environment";
import {Observable} from "rxjs";

@Injectable({
  providedIn: 'root'
})
export class CheckoutService {

  private baseUrl = environment.apiUrl;

  constructor(private http: HttpClient) { }

  createCustomizations(customizations: any[]): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/user/make-customization`, customizations);
  }

  createOrder(
    customizationIds: string[],
    paymentMethod: string = 'Stripe',
    deliveryMethod: string = 'HomeDelivery',
    packetaPointId?: string,
    packetaPointName?: string,
    packetaPointAddress?: string
  ): Observable<any> {
    const headers = new HttpHeaders({
      'Content-Type': 'application/json'
    });

    let url = `${this.baseUrl}/user/make-order?paymentMethod=${encodeURIComponent(paymentMethod)}&deliveryMethod=${encodeURIComponent(deliveryMethod)}`;
    if (packetaPointId) {
      url += `&packetaPointId=${encodeURIComponent(packetaPointId)}`;
    }
    if (packetaPointName) {
      url += `&packetaPointName=${encodeURIComponent(packetaPointName)}`;
    }
    if (packetaPointAddress) {
      url += `&packetaPointAddress=${encodeURIComponent(packetaPointAddress)}`;
    }

    return this.http.post<any>(
      url,
      JSON.stringify(customizationIds),
      { headers }
    );
  }

  createGuestCustomizations(customizations: any): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/guest/make-customization-without-register`, customizations);
  }

  createGuestOrder(orderRequest: any): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/guest/make-order-without-register`, orderRequest);
  }

}
