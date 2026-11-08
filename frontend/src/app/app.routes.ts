import { Routes } from '@angular/router';

import { Login } from './pages/login/login';
import { Register } from './pages/register/register';

import { PatientComponent } from './pages/patient/patient';
import { AdminComponent } from './pages/admin/admin';
import { Medecin } from './pages/medecin/medecin';
import { Pharmacien } from './pages/pharmacien/pharmacien';

export const routes: Routes = [
  { path: 'login', component: Login },
  { path: 'register', component: Register },

  { path: 'patient', component: PatientComponent },
  { path: 'admin', component: AdminComponent },
  { path: 'medecin', component: Medecin },
  { path: 'pharmacien', component: Pharmacien },

  { path: '', redirectTo: 'login', pathMatch: 'full' },
  { path: '**', redirectTo: 'login' }
];