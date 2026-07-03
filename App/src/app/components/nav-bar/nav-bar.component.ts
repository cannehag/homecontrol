import { MsalService } from '@azure/msal-angular';
import { Component, OnInit } from '@angular/core';

// Bootstrap's JS bundle is loaded globally via a <script> tag in index.html
// (not an ES import), so its Collapse API is only reachable as this global.
declare const bootstrap: any;

@Component({
  selector: 'home-nav-bar',
  templateUrl: './nav-bar.component.html',
  styleUrls: ['./nav-bar.component.less'],
})
export class NavBarComponent implements OnInit {
  constructor(private authService: MsalService) {}

  logout() {
    this.closeMenu();
    this.authService.logout();
  }

  // Bootstrap only auto-collapses the mobile menu when the toggler button
  // itself is clicked, not when a link inside it is - so clicking a nav link
  // on mobile otherwise leaves the expanded menu open underneath the new page.
  closeMenu() {
    const menu = document.getElementById('navbarSupportedContent');
    if (menu?.classList.contains('show')) {
      bootstrap.Collapse.getOrCreateInstance(menu).hide();
    }
  }

  ngOnInit() {}
}
