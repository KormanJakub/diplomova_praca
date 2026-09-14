import {Component} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {AuthService} from '../../Services/auth.service';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './forgot-password.component.html',
  styleUrl: './forgot-password.component.css'
})
export class ForgotPasswordComponent {
  email = '';
  token = '';
  newPassword = '';
  repeatNewPassword = '';
  message = '';
  busy = false;

  constructor(private authService: AuthService) {}

  requestReset(): void {
    this.busy = true;
    this.authService.requestPasswordReset(this.email).subscribe({
      next: () => {
        this.message = 'Ak účet existuje, poslali sme kód na email.';
        this.busy = false;
      },
      error: () => {
        this.message = 'Žiadosť sa nepodarilo odoslať. Skúste to neskôr.';
        this.busy = false;
      }
    });
  }

  resetPassword(): void {
    if (this.newPassword !== this.repeatNewPassword) {
      this.message = 'Heslá sa nezhodujú.';
      return;
    }
    this.busy = true;
    this.authService.resetPassword(this.email, this.token, this.newPassword, this.repeatNewPassword)
      .subscribe({
        next: () => {
          this.message = 'Heslo bolo zmenené. Teraz sa môžete prihlásiť.';
          this.token = '';
          this.newPassword = '';
          this.repeatNewPassword = '';
          this.busy = false;
        },
        error: () => {
          this.message = 'Kód je neplatný alebo vypršal, prípadne heslo nespĺňa požiadavky.';
          this.busy = false;
        }
      });
  }
}
