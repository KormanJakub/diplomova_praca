import { Injectable } from '@angular/core';

export interface JwtPayload {
  UserId: string;
  UserEmail: string;
  FirstName: string;
  LastName: string;
}

@Injectable({
  providedIn: 'root'
})
export class DecodingTokenService {

  constructor() { }

  decodeToken(token: string): JwtPayload | null {
    if (!token) {
      return null;
    }

    const parts = token.split('.');
    if (parts.length !== 3) {
      return null;
    }

    try {
      const base64Payload = parts[1].replace(/-/g, '+').replace(/_/g, '/');
      const decodedPayload = atob(base64Payload);

      const payloadObject: JwtPayload = JSON.parse(decodedPayload);

      return payloadObject;
    } catch (error) {
      console.error('Chyba pri dekódovaní tokenu:', error);
      return null;
    }
  }

}
