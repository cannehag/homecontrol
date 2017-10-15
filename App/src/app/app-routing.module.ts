import { AuthenticationGuard } from './guards/authentication.guard';
import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { GarageComponent } from './components/garage/garage.component';

const routes: Routes = [
  {
    path: '',
    component: GarageComponent,
    children: [],
    canActivate: [AuthenticationGuard]
  }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
