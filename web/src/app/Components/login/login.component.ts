import { Component } from '@angular/core';
import {LoginRequest} from "../../Requests/loginrequest";
import {FormControl, FormGroup, Validators} from "@angular/forms";
import {Router} from "@angular/router";
import {AuthService} from "../../Services/auth.service";
import {FloatLabelModule} from "primeng/floatlabel";
import {InputTextModule} from "primeng/inputtext";
import {PaginatorModule} from "primeng/paginator";
import {Button} from "primeng/button";
import {HeaderComponent} from "../header/header.component";
import {FooterComponent} from "../footer/footer.component";

/*
TODO:
Validiacia
Ak email nie je potvrdeny presmeruj ho na verifikaciu
Ak email je potvredy presmeruj ho na uzivatelske konto
 */

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    FloatLabelModule,
    InputTextModule,
    PaginatorModule,
    Button,
    HeaderComponent,
    FooterComponent
  ],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent {
  user: LoginRequest = new LoginRequest();

  loginForm: FormGroup = new FormGroup({
    email: new FormControl(null, [Validators.required, Validators.email]),
    password: new FormControl(null, [Validators.required])
  })

  constructor( private router: Router, private authService: AuthService) { }

  login(user: LoginRequest) {
    /*
    if (!this.loginForm.valid) {
      return;
    }
    */

    this.authService.login(user).subscribe({
      next: (response: { token: string, email_confirmation: string, role?: string }) => {
        localStorage.setItem('uiAppToken', response.token);
        localStorage.setItem('uiAppEmailConfirmation', JSON.stringify(response.email_confirmation));

        if (response.role) {
          localStorage.setItem('uiAppRole', response.role);
        }

        this.router.navigate(['/']);
      },
      error: () => {
        alert("Wrong email or password");
      }
    });
  }

}
