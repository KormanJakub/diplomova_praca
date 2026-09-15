import {Component, OnInit} from '@angular/core';
import {Router, RouterLink} from '@angular/router';
import {AuthService, UiSession} from '../../Services/auth.service';
import {Button} from 'primeng/button';
import {NgIf} from '@angular/common';
import {BadgeModule} from 'primeng/badge';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [Button, RouterLink, NgIf, BadgeModule],
  templateUrl: './header.component.html',
  styleUrl: './header.component.css'
})
export class HeaderComponent implements OnInit {
  session: UiSession | null = null;

  constructor(private router: Router, private authService: AuthService) {}

  ngOnInit(): void {
    this.session = this.authService.getSession();
    if (this.session) {
      this.authService.refreshSession().subscribe({
        next: session => this.session = session,
        error: () => {
          localStorage.removeItem('waffl_ui_session');
          this.session = null;
        }
      });
    }
  }

  logout(): void {
    this.authService.logout().subscribe({
      next: () => this.finishLogout(),
      error: () => this.finishLogout()
    });
  }

  private finishLogout(): void {
    this.session = null;
    this.router.navigate(['/']);
  }

  routeToUserProfile(): void { this.router.navigate(['/user']); }
  routeToAdminProfile(): void { this.router.navigate(['/admin']); }
  routeToShoppingCart(): void { this.router.navigate(['/shopping-cart']); }
  isLogged(): boolean { return this.session !== null; }
  isAdminLogged(): boolean { return this.session?.role === 'admin'; }
}
