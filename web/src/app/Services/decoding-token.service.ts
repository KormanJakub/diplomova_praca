import { Injectable } from '@angular/core';
import jwt_decode from 'jwt-decode';

@Injectable({
  providedIn: 'root'
})
export class DecodingTokenService {

  constructor() { }

  private decodeToken(token: string): any {
    
  }

  readFromToken() {
    const token = localStorage.getItem('uiAppToken');
    if (token) {
      const decodedToken = this.decodeToken(token);
      console.log(decodedToken);
    } else {
      console.log("No such token");
    }
  }
}
