import {Component, OnInit} from '@angular/core';
import {FloatLabelModule} from "primeng/floatlabel";
import {InputTextModule} from "primeng/inputtext";
import {FormControl, FormGroup, ReactiveFormsModule, Validators} from "@angular/forms";
import {LoginRequest} from "../../../Requests/loginrequest";
import {NgIf} from "@angular/common";
import {PublicService} from "../../../Services/public.service";
import {AuthService} from "../../../Services/auth.service";
import {Router} from "@angular/router";
import {CookieService} from "ngx-cookie-service";

@Component({
  selector: 'app-login-page',
  standalone: true,
  imports: [
    FloatLabelModule,
    InputTextModule,
    ReactiveFormsModule,
    NgIf
  ],
  templateUrl: './login-page.component.html',
  styleUrl: './login-page.component.css'
})
export class LoginPageComponent implements OnInit{

  loginForm: FormGroup = new FormGroup({});
  user: LoginRequest = new LoginRequest();

  constructor(
    private router: Router,
    private authService: AuthService,
    private cookieService: CookieService,
    ) {
  }

    ngOnInit(): void {
        this.loginForm = new FormGroup({
          email: new FormControl('', [
            Validators.required, Validators.email
          ]),
          password: new FormControl('', [
            Validators.required
          ])
        });
    }

    async onSubmit() {
      if (this.loginForm.valid) {
        this.login();
      } else {
      }
    }

    login() {
      this.user.password = this.loginForm.controls['password'].value;
      this.user.email = this.loginForm.controls['email'].value;

      this.authService.login(this.user).subscribe({
        next: (response: {
          token: string,
          email_confirmation: string,
          role?: string
        }) => {
          this.cookieService.set('uiAppToken', response.token);
          this.cookieService.set('uiAppEmailConfirmation', JSON.stringify(response.email_confirmation));

          if (response.role === 'admin') {
            this.cookieService.set('uiAppRole', 'admin');
          }

          this.router.navigate(['/']);
        },
        error: (error) => {
          alert("Error: " + error.message);
        }
      })
    }

}
