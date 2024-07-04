import { Component } from '@angular/core';
import {FloatLabelModule} from "primeng/floatlabel";
import {InputTextModule} from "primeng/inputtext";
import {FormControl, FormGroup, FormsModule, Validators} from "@angular/forms";
import {Router} from "@angular/router";
import {AuthService} from "../../Services/auth.service";
import {RegisterRequest} from "../../Requests/registerrequest";
import {Button} from "primeng/button";

//TODO: Urob validaciu

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [
    FloatLabelModule,
    InputTextModule,
    FormsModule,
    Button
  ],
  templateUrl: './register.component.html',
  styleUrl: './register.component.css'
})

export class RegisterComponent {
  user: RegisterRequest = new RegisterRequest();

  constructor(private router: Router, private authService: AuthService) { }

  register(user: RegisterRequest) {
    this.authService.register(user).subscribe({
      next: () => {
        alert('Registrácia prebehla úspešne. Na email vám bolo poslasné overujúci kód!');
        this.router.navigate(['/verify-email']);
      },
      error: (err) => {
        alert('Registration failed: ' + err.error.error);
      }
    })
  }
}
