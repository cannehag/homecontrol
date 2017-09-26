import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { NoAccessComponent } from './components/no-access/no-access.component';
import { AuthService } from "./services/auth.service";
import { JwtHelper } from "angular2-jwt/angular2-jwt";
import { GarageComponent } from './components/garage/garage.component';
import { NavBarComponent } from './components/nav-bar/nav-bar.component';
import { LoadingComponent } from './components/loading/loading.component';
import { AuthInterceptor } from "../authInterceptor";

@NgModule({
  declarations: [
    AppComponent,
    NoAccessComponent,
    GarageComponent,
    NavBarComponent,
    LoadingComponent
  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    HttpClientModule
  ],
  providers: [
    AuthService,
    JwtHelper,
    { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true, }
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
