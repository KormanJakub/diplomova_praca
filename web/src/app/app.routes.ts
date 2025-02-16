import { Routes } from '@angular/router';
import {LoginComponent} from "./Components/login/login.component";
import {RegisterComponent} from "./Components/register/register.component";
import {ForgotPasswordComponent} from "./Components/forgot-password/forgot-password.component";
import {VerifyEmailComponent} from "./Components/verify-email/verify-email.component";
import {AuthGuard} from "./Middleware/auth.guard";
import {HomeComponent} from "./Components/home/home.component";
import {AboutMeComponent} from "./Components/information-components/about-me/about-me.component";
import {GalleryComponent} from "./Components/information-components/gallery/gallery.component";
import {ContactComponent} from "./Components/information-components/contact/contact.component";
import {ProductsComponent} from "./Components/products/products.component";

export const routes: Routes = [
  { path: "", component: HomeComponent },
  { path: "login", component: LoginComponent},
  { path: "register", component: RegisterComponent},
  { path: "about-me", component: AboutMeComponent},
  { path: "my-gallery", component: GalleryComponent},
  {path: "contact", component: ContactComponent},
  {path: "products", component: ProductsComponent},
  { path: "forgot-password", component: ForgotPasswordComponent},
  { path: "verify-email", component: VerifyEmailComponent, canActivate: [AuthGuard]},
];
