import { Routes } from '@angular/router';
import {LoginComponent} from "./Components/login/login.component";
import {RegisterComponent} from "./Components/register/register.component";
import {ForgotPasswordComponent} from "./Components/forgot-password/forgot-password.component";
import {AuthGuard} from "./Guards/auth.guard";
import {LoggedGuard} from "./Guards/logged.guard";
import {VerifyEmailComponent} from "./Components/verify-email/verify-email.component";

export const routes: Routes = [
  { path: "login", component: LoginComponent},
  { path: "register", component: RegisterComponent},
  { path: "forgot-password", component: ForgotPasswordComponent},
  { path: "verify-email", component: VerifyEmailComponent},
];
