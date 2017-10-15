import { Adal4Service } from 'adal-angular4';
import { Router } from '@angular/router';
import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';

@Injectable()
export class AuthService {

  constructor(private adalService: Adal4Service) { }

  init() {
    this.adalService.init(environment.adalConfig);
  }

  login() {
    this.adalService.login();
  }

  logout() {
    this.adalService.logOut();
  }

  handleCallback() {
    this.adalService.handleWindowCallback();
  }

  public get userInfo() {
    return this.adalService.userInfo;
  }

  public get accessToken() {
    return this.adalService.getCachedToken(environment.adalConfig.clientId);
  }

  public get isAuthenticated() {
    return this.userInfo && this.accessToken !== null;
  }
}