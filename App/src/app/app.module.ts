import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';

import { AppComponent } from './app.component';
import { AppRoutingModule } from './app-routing.module';
import { GarageComponent } from './components/garage/garage.component';
import { NavBarComponent } from './components/nav-bar/nav-bar.component';
import { LoadingComponent } from './components/loading/loading.component';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import {
  MSAL_GUARD_CONFIG,
  MSAL_INSTANCE,
  MSAL_INTERCEPTOR_CONFIG,
  MsalBroadcastService,
  MsalGuard,
  MsalGuardConfiguration,
  MsalInterceptor,
  MsalInterceptorConfiguration,
  MsalModule,
  MsalService,
} from '@azure/msal-angular';
import {
  AuthenticationScheme,
  IPublicClientApplication,
  InteractionType,
  PublicClientApplication,
} from '@azure/msal-browser';
import { environment } from 'src/environments/environment';

export function MSALInstanceFactory(): IPublicClientApplication {
  return new PublicClientApplication({
    auth: {
      clientId: environment.adalConfig.clientId, // Application (client) ID from the app registration
      authority:
        'https://login.microsoftonline.com/' + environment.adalConfig.tenant, // The Azure cloud instance and the app's sign-in audience (tenant ID, common, organizations, or consumers)
      redirectUri: environment.adalConfig.redirectUri, // This is your redirect URI
    },
    cache: {
      cacheLocation: 'localStorage',
      storeAuthStateInCookie: false,
    },
  });
}

export function MSALGuardConfigFactory(): MsalGuardConfiguration {
  return {
    interactionType: InteractionType.Redirect,
    authRequest: {
      scopes: ['user.read'],
    },
  };
}

export function MSALInterceptorConfigFactory(): MsalInterceptorConfiguration {
  const protectedResourceMap = new Map<string, Array<string>>();

  environment.adalConfig.protectedResources.forEach((item) => {
    protectedResourceMap.set(item.resource, item.scopes);
  });

  return {
    interactionType: InteractionType.Redirect,
    authRequest: {
      authenticationScheme: AuthenticationScheme.BEARER,
    },
    protectedResourceMap,
  };
}

@NgModule({
  declarations: [
    AppComponent,
    GarageComponent,
    NavBarComponent,
    LoadingComponent,
  ],
  imports: [BrowserModule, AppRoutingModule, HttpClientModule, MsalModule],
  providers: [
    // { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true },
    {
      provide: HTTP_INTERCEPTORS, // Provides as HTTP Interceptor
      useClass: MsalInterceptor,
      multi: true,
    },
    {
      provide: MSAL_INSTANCE,
      useFactory: MSALInstanceFactory,
    },
    {
      provide: MSAL_GUARD_CONFIG,
      useFactory: MSALGuardConfigFactory,
    },
    {
      provide: MSAL_INTERCEPTOR_CONFIG,
      useFactory: MSALInterceptorConfigFactory,
    },
    MsalService,
    MsalGuard,
    MsalBroadcastService,
  ],
  bootstrap: [AppComponent],
})
export class AppModule {}
