import { Router } from '@angular/router';
import { Component } from '@angular/core';
import { AuthService } from './services/auth.service';

@Component({
  selector: 'hc-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.less']
})
export class AppComponent {
  constructor(public authService: AuthService) {
    this.authService.init();

    //http://www.iconarchive.com/show/colorful-long-shadow-icons-by-graphicloads/Home-icon.html

    this.authService.handleCallback();
  }
}
