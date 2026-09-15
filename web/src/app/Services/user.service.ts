import { Injectable } from '@angular/core';
import {HttpClient} from "@angular/common/http";
import {Observable} from "rxjs";
import {User} from "../Models/user.model";
import {environment} from "../../Environments/environment";
import {Tag} from "../Models/tag.model";
import {Customization} from "../Models/customization.model";
import {Order} from "../Models/order.model";
import {Design} from "../Models/design.model";
import {Product} from "../Models/product.model";

@Injectable({
  providedIn: 'root'
})
export class UserService {

  constructor(private http: HttpClient) { }

  getUserProfile(): Observable<User> {
    const url = `${environment.apiUrl}/user/profile`;
    return this.http.get<User>(url);
  }

  getMyCustomizations(): Observable<Customization[]> {
    return this.http.get<Customization[]>(`${environment.apiUrl}/user/my-customizations`);
  }

  getOrders(): Observable<{
    orders: Order[],
    customizations: Customization[],
    designs: Design[],
    products: Product[]
  }> {
    return this.http.get<{
      orders: Order[],
      customizations: Customization[],
      designs: Design[],
      products: Product[]
    }>(`${environment.apiUrl}/user/orders`);
  }

  getOrdersById(id: number): Observable<{
    order: Order,
    customizations: Customization[],
    designs: Design[],
    products: Product[],
    user: User
  }> {
    return this.http.get<{
      order: Order,
      customizations: Customization[],
      designs: Design[],
      products: Product[],
      user: User
    }>(`${environment.apiUrl}/user/orders/${id}`);
  }


  makeOrder(customizationsIds: string[]): Observable<any> {
    return this.http.post(`${environment.apiUrl}/user/make-order`, customizationsIds);
  }

  cancelOrder(orderId: number): Observable<any> {
    return this.http.post(`${environment.apiUrl}/user/cancel-order/${orderId}`, {});
  }

  updateUserProfile(user: User): Observable<any> {
    return this.http.put(`${environment.apiUrl}/user/update`, user);
  }

  removeUser(): Observable<any> {
    return this.http.delete(`${environment.apiUrl}/user/remove`);
  }

  makeCustomization(requests: any[]): Observable<any> {
    return this.http.post(`${environment.apiUrl}/user/make-customization`, requests);
  }


}
