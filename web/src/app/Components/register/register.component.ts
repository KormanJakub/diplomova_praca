import {Component, OnInit} from '@angular/core';
import {FloatLabelModule} from "primeng/floatlabel";
import {InputTextModule} from "primeng/inputtext";
import {FormControl, FormsModule, FormGroup, Validators, ReactiveFormsModule, FormBuilder} from '@angular/forms';
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
import {FooterComponent} from "../footer/footer.component";

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
    HeaderComponent,
    FooterComponent
  ],
  templateUrl: './register.component.html',
  styleUrl: './register.component.css'
})

export class RegisterComponent implements OnInit{
  user: RegisterRequest = new RegisterRequest();
  registerForm!: FormGroup;

  constructor(
    private router: Router,
    private authService: AuthService,
    private fb: FormBuilder,
  ) {}

  ngOnInit(): void {
    this.registerForm = this.fb.group({
      email: new FormControl('', [Validators.required, Validators.email]),
      first_name: [''],
      last_name: [''],
      password: ['', [Validators.required, Validators.minLength(6)]],
      repeatPassword: ['', [Validators.required, Validators.minLength(6)]],
    },
      {
        validators: [this.passwordsMatchValidator]
      }
      );
  }

  passwordsMatchValidator(formGroup: FormGroup) {
    const passwordControl = formGroup.get('password');
    const repeatPasswordControl = formGroup.get('repeatPassword');

    if (!passwordControl || !repeatPasswordControl) {
      return null;
    }

    if (repeatPasswordControl.errors && !repeatPasswordControl.errors['mismatch']) {
      return null;
    }

    if (passwordControl.value !== repeatPasswordControl.value) {
      repeatPasswordControl.setErrors({ mismatch: true });
    } else {
      repeatPasswordControl.setErrors(null);
    }

    return null;
  }

  onSubmit() {
    if (this.registerForm.valid) {
      console.log(this.registerForm.value);
    } else {
      this.registerForm.markAllAsTouched();
    }
  }

  register() {
    this.user.email = this.registerForm.controls['email'].value;
    this.user.password = this.registerForm.controls['password'].value;
    this.user.repeatPassword = this.registerForm.controls['repeatPassword'].value;
    this.user.firstName = this.registerForm.controls['first_name'].value;
    this.user.lastName = this.registerForm.controls['last_name'].value;

    this.authService.register(this.user).subscribe({
      next: () => {
        alert('Registrácia prebehla úspešne. Prihlaste sa!');
        this.router.navigate(['/login']);
      },
      error: (err) => {
        alert('Registration failed: ' + err.error.error);
      }
    })
  }
}
