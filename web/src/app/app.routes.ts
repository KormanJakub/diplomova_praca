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
import {NoAuthGuard} from "./Guards/no-auth.guard";
import {AdminHomePageComponent} from "./Components/admin-page/admin-home-page/admin-home-page.component";
import {AdminAuthGuard} from "./Guards/admin-auth.guard";
import {AllTagsComponent} from "./Components/admin-page/tagy/all-tags/all-tags.component";
import {AddTagsComponent} from "./Components/admin-page/tagy/add-tags/add-tags.component";
import {
  AdminImportantInformationsComponent
} from "./Components/admin-page/admin-important-informations/admin-important-informations.component";
import {AllProductsComponent} from "./Components/admin-page/products/all-products/all-products.component";
import {AllDesignsComponent} from "./Components/admin-page/designs/all-designs/all-designs.component";
import {
  AllPairedDesignsComponent
} from "./Components/admin-page/paired-designs/all-paired-designs/all-paired-designs.component";
import {AllGalleryComponent} from "./Components/admin-page/gallery/all-gallery/all-gallery.component";
import {AllCustomizationsComponent} from "./Components/admin-page/all-customizations/all-customizations.component";
import {AllOrdersComponent} from "./Components/admin-page/all-orders/all-orders.component";
import {OrderInformationComponent} from "./Components/admin-page/order-information/order-information.component";
import {UserHomePageComponent} from "./Components/user-page/user-home-page/user-home-page.component";
import {UserInformationComponent} from "./Components/user-page/user-information/user-information.component";
import {UserOrdersComponent} from "./Components/user-page/user-orders/user-orders.component";
import {UserOrderInfoComponent} from "./Components/user-page/user-order-info/user-order-info.component";

export const routes: Routes = [
  { path: "", component: HomeComponent },
  {
    path: "login",
    component: LoginComponent,
    canActivate: [NoAuthGuard]
  },
  {
    path: "register",
    component: RegisterComponent,
    canActivate: [NoAuthGuard]
  },
  { path: "about-me", component: AboutMeComponent},
  { path: "my-gallery", component: GalleryComponent},
  { path: "contact", component: ContactComponent},
  { path: "products", component: ProductsComponent},
  { path: "forgot-password", component: ForgotPasswordComponent},
  {
    path: "verify-email",
    component: VerifyEmailComponent,
    canActivate: [AuthGuard]
  },
  {
    path: "admin",
    component: AdminHomePageComponent,
    canActivate: [AuthGuard, AdminAuthGuard],
    children: [
      { path: "", component: AdminImportantInformationsComponent},
      { path: "tags", component: AllTagsComponent},
      { path: "products", component: AllProductsComponent},
      { path: "designs", component: AllDesignsComponent},
      { path: "paired-designs", component: AllPairedDesignsComponent},
      { path: "gallery", component: AllGalleryComponent},
      { path: "customizations", component: AllCustomizationsComponent},
      { path: "orders", component: AllOrdersComponent},
      {path: "orders/:id", component: OrderInformationComponent}
    ]
  },
  {
    path: "user",
    component: UserHomePageComponent,
    canActivate: [AuthGuard],
    children: [
      {path:"", component: UserInformationComponent},
      {path:"my-orders", component: UserOrdersComponent},
      {path:"my-order/:id", component: UserOrderInfoComponent}
    ]
  }
];
