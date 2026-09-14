import {Component} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {ActivatedRoute, Router} from '@angular/router';
import {AuthService} from '../../Services/auth.service';

@Component({
  selector: 'app-verify-email',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './verify-email.component.html',
  styleUrl: './verify-email.component.css'
})
export class VerifyEmailComponent {
  email = '';
  code = '';
  message = '';
  busy = false;

  constructor(private auth: AuthService, private route: ActivatedRoute, private router: Router) {
    this.email = this.route.snapshot.queryParamMap.get('email') ?? '';
  }

  verifyCode(): void {
    if (!/^\d{6}$/.test(this.code)) {
      this.message = 'Zadajte šesťmiestny kód.';
      return;
    }
    this.busy = true;
    this.auth.verifyEmailCode(this.email, Number(this.code)).subscribe({
      next: () => this.router.navigate(['/login']),
      error: () => {
        this.message = 'Kód je neplatný alebo vypršal.';
        this.busy = false;
      }
    });
  }

  resendCode(): void {
    this.busy = true;
    this.auth.resendEmailCode(this.email).subscribe({
      next: () => {
        this.message = 'Nový kód bol odoslaný.';
        this.busy = false;
      },
      error: () => {
        this.message = 'Kód sa nepodarilo odoslať.';
        this.busy = false;
      }
    });
  }
}
