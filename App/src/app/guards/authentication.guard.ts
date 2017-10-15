import { AuthService } from './../services/auth.service';
import { Injectable } from '@angular/core';
import { NavigationExtras, CanActivate, ActivatedRouteSnapshot, RouterStateSnapshot, Router } from '@angular/router';
import { Observable } from 'rxjs/Observable';

@Injectable()
export class AuthenticationGuard implements CanActivate {
  constructor(private router: Router, private authService: AuthService) { }

  canActivate(
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot): Observable<boolean> | Promise<boolean> | boolean {

    const navigationExtras: NavigationExtras = {
      queryParams: { 'redirectUrl': route.url }
    };

    if (!this.authService.isAuthenticated) {
      this.authService.login();
      return false;
    }
    return true;
  }
}