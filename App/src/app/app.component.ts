import { Router } from '@angular/router';
import { Component } from '@angular/core';
import { AuthService } from "./services/auth.service";

@Component({
  selector: 'hc-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.less']
})
export class AppComponent {
  constructor(private auth: AuthService, private router: Router) {
    auth.handleAuthentication();

    if (!auth.isAuthenticated()) {
      router.navigate(['no-access']);
      return;
    }
  }

  title = 'hc';
}
