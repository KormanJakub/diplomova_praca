import {Component, OnInit} from '@angular/core';
import {Router, RouterLink} from "@angular/router";
import {AuthService} from "../../Services/auth.service";
import {DecodingTokenService, JwtPayload} from "../../Services/decoding-token.service";
import {Button} from "primeng/button";
import {NgIf} from "@angular/common";
import {BadgeModule} from "primeng/badge";
import {CookieService} from "ngx-cookie-service";

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [
    Button,
    RouterLink,
    NgIf,
    BadgeModule
  ],
  templateUrl: './header.component.html',
  styleUrl: './header.component.css'
})
export class HeaderComponent implements OnInit{
  decoded: JwtPayload | null = null;

  constructor(
    private router: Router,
    private authService: AuthService,
    private decoding: DecodingTokenService,
    private cookieService: CookieService
  ) {}

  ngOnInit(): void {
    const token = this.authService.returnToken();
    this.decoded = this.decoding.decodeToken(token);
  }

  logout(): void {
    this.cookieService.delete('uiAppToken');
    this.cookieService.delete('uiAppRole');
    this.cookieService.delete('uiAppEmailConfirmation');

    this.router.navigate(['/']);
  }

  routeToUserProfile () {
    this.router.navigate(['/user']);
  }

  routeToAdminProfile() {
    this.router.navigate(['/admin']);
  }

  isLogged(): boolean {
    const token = this.cookieService.get("uiAppToken");
    return !!token;
  }

  isAdminLogged(): boolean {
    return this.cookieService.get('uiAppRole') === 'admin';
  }
}
