import {Component, OnInit} from '@angular/core';
import {UserService} from "../../../Services/user.service";
import {User} from "../../../Models/user.model";
import {CurrencyPipe, DatePipe} from "@angular/common";
import {Button, ButtonDirective} from "primeng/button";
import {FormsModule} from "@angular/forms";
import {DialogModule} from "primeng/dialog";
import {Router} from '@angular/router';

@Component({
  selector: 'app-user-information',
  standalone: true,
  imports: [
    CurrencyPipe,
    DatePipe,
    Button,
    FormsModule,
    DialogModule,
    ButtonDirective
  ],
  templateUrl: './user-information.component.html',
  styleUrl: './user-information.component.css'
})
export class UserInformationComponent implements OnInit {

  user!: User;
  userDialog: boolean = false;

  constructor(
    private userService: UserService,
    private router: Router
  ) {}

  ngOnInit() {
    this.refreshData();
  }

  refreshData() {
    this.userService.getUserProfile().subscribe({
      next: (user: User) => {
        this.user = user;
      }
    })
  }

  showUserDialog() {
    this.userDialog = true;
  }

  hideUserDialog() {
    this.userDialog = false;
  }

  updateUserProfile() {
    this.userService.updateUserProfile(this.user).subscribe({
      next: () => {
        localStorage.removeItem('waffl_ui_session');
        this.router.navigate(['/']);
      }
    })
  }

  removeUser() {
    this.userService.removeUser().subscribe({
      next: () => {

      }
    })
  }
}
