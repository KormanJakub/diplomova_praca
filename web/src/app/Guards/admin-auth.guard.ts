import { Injectable } from '@angular/core';
import { CanActivate, Router } from '@angular/router';
import {CookieService} from "ngx-cookie-service";

@Injectable({
  providedIn: 'root',
})
export class AdminAuthGuard implements CanActivate {
  constructor(
    private router: Router,
    private cookieService: CookieService,
  ) {}

  isAdminLogged(): boolean {
    return this.cookieService.get('uiAppRole') === 'admin';
  }

  canActivate(): boolean {
    if (!this.isAdminLogged()) {
      this.router.navigate(['/']);
      return false;
    }

    return true;
  }
}
