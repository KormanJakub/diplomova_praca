import {Component, OnInit} from '@angular/core';
import {UserService} from "../../../Services/user.service";
import {User} from "../../../Models/user.model";
import {CurrencyPipe, DatePipe} from "@angular/common";
import {Button} from "primeng/button";
import {Footer} from "primeng/api";
import {FormsModule} from "@angular/forms";
import {DialogModule} from "primeng/dialog";

@Component({
  selector: 'app-user-information',
  standalone: true,
  imports: [
    CurrencyPipe,
    DatePipe,
    Button,
    Footer,
    FormsModule,
    DialogModule
  ],
  templateUrl: './user-information.component.html',
  styleUrl: './user-information.component.css'
})
export class UserInformationComponent implements OnInit {

  user!: User;
  userDialog: boolean = false;

  constructor(
    private userService: UserService
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
