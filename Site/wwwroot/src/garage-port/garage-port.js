import {inject} from 'aurelia-framework';
import {Redirect} from 'aurelia-router';
import {tokenIsExpired} from '../utils/tokenUtils';
import {HttpClient} from 'aurelia-fetch-client';

@inject(HttpClient)
export class GaragePort {
   portState = "open";
   loading = true;

  constructor(http){
     this.http = http;
     var self = this;

    this.http.configure(config => {
      config.withDefaults({
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('token')}`,
          'x-user': `${localStorage.getItem('userName')}`
        }
      }).withInterceptor({
         request(request) {
            self.loading = true;
            return request;
         },
         response(response) {
            self.loading = false;
            return response;
         }
      });;
    });
  }

  togglePort(){
    this.http.fetch('/api/garage/toggle', {method:'put',
      headers: {
        'Authorization': `Bearer ${localStorage.getItem('token')}`
      }
    })
  }

  refreshState(){
    this.http.fetch('/api/garage/status', {
      headers: {
        'Authorization': `Bearer ${localStorage.getItem('token')}`
      }
    })
    .then(response => response.text())
    .then(data => this.portState = data);
  }

  activate(){
    this.refreshState();
  }

  canActivate() {
    if(tokenIsExpired()) {
      return new Redirect('noaccess');
    }
    else {
      return true;
    }
  }
}