import { HttpClient } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'hc-garage',
  templateUrl: './garage.component.html',
  styleUrls: ['./garage.component.less']
})
export class GarageComponent implements OnInit {
  portState = "open";
  loading = true;

  constructor(private http: HttpClient) { }

  togglePort() {
    this.loading = true;
    this.http.put('/api/garage/toggle', {}).subscribe(_ => {
      this.loading = false;
    })
  }

  refresh() {
    this.loading = true;
    this.http.get('/api/garage/status').subscribe((result: any) => {
      this.portState = result.state;
      this.loading = false;
    });
  }

  ngOnInit() {
    this.refresh();
  }

}
