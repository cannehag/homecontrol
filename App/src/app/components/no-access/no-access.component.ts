import { Component, OnInit } from '@angular/core';
import { AuthService } from "../../services/auth.service";

@Component({
  selector: 'hc-no-access',
  templateUrl: './no-access.component.html',
  styleUrls: ['./no-access.component.less']
})
export class NoAccessComponent implements OnInit {

  constructor(private auth: AuthService) { }

  ngOnInit() {
  }

  login() {
    this.auth.login();
  }
}
