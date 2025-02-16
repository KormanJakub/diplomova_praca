import { Injectable } from '@angular/core';
import {HttpClient} from "@angular/common/http";
import {Observable} from "rxjs";
import {environment} from "../../Environments/environment";
import {Product} from "../Models/product.model";
import {Gallery} from "../Models/gallery.model";

@Injectable({
  providedIn: 'root'
})
export class PublicService {

  constructor(private httpClient: HttpClient) { }

  public receiveNews(email: string): Observable<any> {
    return this.httpClient.post<any>(`${environment.apiUrl}/public/receive-news`, { email });
  }

  public bestThreeProducts(product: Product): Observable<any> {
    return this.httpClient.get<Product[]>(`${environment.apiUrl}/public/best-three-products`);
  }

  public galleryShowCaseForHome(gallery: Gallery): Observable<any> {
    return this.httpClient.get<Gallery[]>(`${environment.apiUrl}/public/all-gallery`);
  }
}
