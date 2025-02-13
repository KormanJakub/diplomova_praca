import { Component } from '@angular/core';
import {Router, RouterLink} from "@angular/router";
import {FormsModule} from "@angular/forms";
import {AuthService} from "../../Services/auth.service";
import {PublicService} from "../../Services/public.service";

@Component({
  selector: 'app-footer',
  standalone: true,
  imports: [
    RouterLink,
    FormsModule
  ],
  templateUrl: './footer.component.html',
  styleUrl: './footer.component.css'
})
export class FooterComponent {
  email: string = "";

  constructor(
    private publicService: PublicService
  ) {}

  receiveNews() {
    this.publicService.receiveNews(this.email).subscribe({
      next: () => {
        alert('Úspešne budete príjmať novinky!');
      },
      error: (err) => {
        alert('Registration failed: ' + err.error.error);
      }
    })
  }
}
