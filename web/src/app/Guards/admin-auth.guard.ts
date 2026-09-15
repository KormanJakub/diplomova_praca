import { Injectable } from '@angular/core';
import { CanActivate, Router } from '@angular/router';
import {AuthService} from '../Services/auth.service';

@Injectable({
  providedIn: 'root',
})
export class AdminAuthGuard implements CanActivate {
  constructor(
    private router: Router,
    private authService: AuthService,
  ) {}

  isAdminLogged(): boolean {
    return this.authService.isAdminLoggedIn();
  }

  canActivate(): boolean {
    if (!this.isAdminLogged()) {
      this.router.navigate(['/']);
      return false;
    }

    return true;
  }
}
