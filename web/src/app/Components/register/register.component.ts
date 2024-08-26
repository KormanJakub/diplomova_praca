import { Component } from '@angular/core';
import {FloatLabelModule} from "primeng/floatlabel";
import {InputTextModule} from "primeng/inputtext";
import { FormControl, FormsModule, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import {Router, RouterModule} from "@angular/router";
import {AuthService} from "../../Services/auth.service";
import {RegisterRequest} from "../../Requests/registerrequest";
import {Button} from "primeng/button";
import { PasswordModule } from 'primeng/password';
import {DividerModule} from "primeng/divider";
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import {CommonModule} from "@angular/common";
import {BrowserModule} from "@angular/platform-browser";
import {HeaderComponent} from "../header/header.component";

//TODO: Urob validaciu

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [
    FloatLabelModule,
    InputTextModule,
    FormsModule,
    Button,
    ReactiveFormsModule,
    RouterModule,
    CommonModule,
    DividerModule,
    PasswordModule,
    HeaderComponent
  ],
  templateUrl: './register.component.html',
  styleUrl: './register.component.css'
})

export class RegisterComponent {
  user: RegisterRequest = new RegisterRequest();

  registerForm: FormGroup = new FormGroup({
    email: new FormControl(null, [Validators.required, Validators.email]),
    firstName: new FormControl(null, [Validators.required]),
    lastName: new FormControl(null, [Validators.required]),
  });

  constructor(
    private router: Router,
    private authService: AuthService
  ) {}

  register() {
    if (this.registerForm.invalid) {
      return;
    }

    const registerRequest: RegisterRequest = this.registerForm.value;

    console.log(registerRequest);
    /*
    this.authService.register(registerRequest).subscribe({
      next: () => {
        alert('Registrácia prebehla úspešne. Prihlaste sa!');
        this.router.navigate(['/login']);
      },
      error: (err) => {
        alert('Registration failed: ' + err.error.error);
      }
    })
     */
  }
}
