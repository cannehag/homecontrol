import { MsalService } from '@azure/msal-angular';
import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'home-nav-bar',
  templateUrl: './nav-bar.component.html',
  styleUrls: ['./nav-bar.component.less'],
})
export class NavBarComponent implements OnInit {
  constructor(private authService: MsalService) {}

  logout() {
    this.authService.logout();
  }

  ngOnInit() {}
}
