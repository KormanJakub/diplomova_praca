import { Component } from '@angular/core';
import {VerificateCodeRequest} from "../../Requests/verificatecoderequest";
import {RouterOutlet} from "@angular/router";
import {AuthService} from "../../Services/auth.service";

@Component({
  selector: 'app-verify-email',
  standalone: true,
  imports: [],
  templateUrl: './verify-email.component.html',
  styleUrl: './verify-email.component.css'
})
export class VerifyEmailComponent {
  user: VerificateCodeRequest = new VerificateCodeRequest();

  constructor(private authService: AuthService, private router: RouterOutlet) {}

  verifyCode(user: VerificateCodeRequest) {

  }

}
