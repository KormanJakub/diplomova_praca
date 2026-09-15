import {Component, OnInit} from '@angular/core';
import {FloatLabelModule} from "primeng/floatlabel";
import {InputTextModule} from "primeng/inputtext";
import {FormControl, FormGroup, ReactiveFormsModule, Validators} from "@angular/forms";
import {LoginRequest} from "../../../Requests/loginrequest";
import {NgIf} from "@angular/common";
import {PublicService} from "../../../Services/public.service";
import {AuthService} from "../../../Services/auth.service";
import {Router} from "@angular/router";
import {DialogModule} from "primeng/dialog";
import {ButtonDirective} from "primeng/button";

@Component({
  selector: 'app-login-page',
  standalone: true,
  imports: [
    FloatLabelModule,
    InputTextModule,
    ReactiveFormsModule,
    NgIf,
    DialogModule,
    ButtonDirective
  ],
  templateUrl: './login-page.component.html',
  styleUrl: './login-page.component.css'
})
export class LoginPageComponent implements OnInit{

  loginForm: FormGroup = new FormGroup({});
  user: LoginRequest = new LoginRequest();

  errorDialogVisible = false;
  errorDialogMessage = '';

  constructor(
    private router: Router,
    private authService: AuthService,
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
        next: () => {
          this.router.navigate(['/']);
        },
        error: () => {
          this.errorDialogMessage = 'Uživateľ nie je registrovaný, alebo ste zadali zlé heslo! Skuste to znova';
          this.errorDialogVisible = true;
        }
      })
    }

}
