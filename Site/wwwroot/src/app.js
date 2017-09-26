import { inject } from 'aurelia-framework';
import { HttpClient } from 'aurelia-fetch-client';
import { Router } from 'aurelia-router';
import { tokenIsExpired } from './utils/tokenUtils';
import { Redirect } from 'aurelia-router';

@inject(HttpClient, Router)
export class App {
    // message = 'App';
    lock = new Auth0Lock(AUTH0_CLIENT_ID, AUTH0_DOMAIN);
    isAuthenticated = false;

    constructor(http, router) {
        this.http = http;
        this.router = router;

        this.router.configure(config => {
            config.title = "HomeController";
            config.map([
                {
                    route: 'noaccess',
                    name: 'noaccess',
                    title: 'noaccess',
                    nav: false,
                    moduleId: './noaccess/noaccess'
                },
                {
                    route: ['', 'garage-port'],
                    name: 'garage-port',
                    title: 'Garage',
                    nav: true,
                    moduleId: './garage-port/garage-port'
                }
            ]);
        });
        this.http.configure(config => {
            config.withDefaults({
                headers: {
                    'Authorization': `Bearer ${localStorage.getItem('token')}`,
                    'x-user': `${localStorage.getItem('userName')}`
                }
            });
        });

        if (tokenIsExpired()) {
            this.isAuthenticated = false;
        }
        else {
            this.isAuthenticated = true;
        }
    }

    login() {
        this.lock.show((err, profile, token) => {
            if (err) {
                console.log(err);
            }
            else {
                localStorage.setItem('profile', JSON.stringify(profile));
                localStorage.setItem('userName', profile.name);
                localStorage.setItem('token', token);
                this.isAuthenticated = true;

                this.http.configure(config => {
                    config.withDefaults({
                        headers: {
                            'Authorization': `Bearer ${localStorage.getItem('token')}`,
                            'x-user': `${localStorage.getItem('userName')}`
                        }
                    });
                });
                this.router.navigate('');
            }
        });
    }

    logout() {
        localStorage.removeItem('profile');
        localStorage.removeItem('token');
        localStorage.removeItem('userName');
        this.isAuthenticated = false;

        this.router.navigate('noaccess');
    }

}
