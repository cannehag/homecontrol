import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { NoAccessComponent } from "./components/no-access/no-access.component";
import { GarageComponent } from "./components/garage/garage.component";

const routes: Routes = [
  {
    path: '',
    component: GarageComponent,
    children: []
  },
  {
    path: 'no-access',
    component: NoAccessComponent,
    children: []
  }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
