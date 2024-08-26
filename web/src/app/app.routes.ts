import { Routes } from '@angular/router';
import {LoginComponent} from "./Components/login/login.component";
import {RegisterComponent} from "./Components/register/register.component";
import {ForgotPasswordComponent} from "./Components/forgot-password/forgot-password.component";
import {VerifyEmailComponent} from "./Components/verify-email/verify-email.component";
import {AuthGuard} from "./Middleware/auth.guard";
import {HeaderComponent} from "./Components/header/header.component";
import {HomeComponent} from "./Components/home/home.component";

export const routes: Routes = [
  { path: "", component: HomeComponent },
  { path: "login", component: LoginComponent},
  { path: "register", component: RegisterComponent},
  { path: "forgot-password", component: ForgotPasswordComponent},
  { path: "header", component: HeaderComponent},
  { path: "verify-email", component: VerifyEmailComponent, canActivate: [AuthGuard]},
];
