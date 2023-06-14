import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { GarageComponent } from './components/garage/garage.component';
import { RouterModule, Routes } from '@angular/router';
import { MsalGuard } from '@azure/msal-angular';

const routes: Routes = [
  {
    path: '',
    component: GarageComponent,
    children: [],
    canActivate: [MsalGuard],
  },
];

@NgModule({
  declarations: [],
  imports: [RouterModule.forRoot(routes), CommonModule],
  exports: [RouterModule],
})
export class AppRoutingModule {}
