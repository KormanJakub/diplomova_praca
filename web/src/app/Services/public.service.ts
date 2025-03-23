import { Injectable } from '@angular/core';
import {HttpClient, HttpParams} from "@angular/common/http";
import {Observable} from "rxjs";
import {environment} from "../../Environments/environment";
import {Product} from "../Models/product.model";
import {Gallery} from "../Models/gallery.model";
import {Tag} from "primeng/tag";
import {Design} from "../Models/design.model";

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

  public allTags(): Observable<any> {
    return this.httpClient.get<Tag[]>(`${environment.apiUrl}/public/all-tags`);
  }

  public filterProducts(query: string): Observable<Product[]> {
    let params = new HttpParams();

    if (query && query.trim() !== '') {
      params = params.set('tagId', query);
    }

    return this.httpClient.get<Product[]>(`${environment.apiUrl}/public/filter-products`, { params });
  }

  public productDetail(id: string, color: string): Observable<Product> {
    const url = `${environment.apiUrl}/public/product/${id}?color=${encodeURIComponent(color)}`;
    return this.httpClient.get<Product>(url);
  }

  public getDesigns(): Observable<Design[]> {
    return this.httpClient.get<Design[]>(`${environment.apiUrl}/public/all-designs`);
  }
}
