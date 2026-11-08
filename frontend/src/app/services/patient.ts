import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';

const API_URL = 'http://localhost:5229/api/patient';

// Informations du patient
export interface PatientProfile {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string | null;
  dateOfBirth: string | null;
}

// Données envoyées au backend
export interface UpdatePatientProfileRequest {
  firstName: string;
  lastName: string;
  phoneNumber: string | null;
  dateOfBirth: string | null;
}

// Réponse possible du backend
export interface UpdatePatientProfileEnvelope {
  message?: string;
  profile?: PatientProfile;
}

// Accepte une réponse directe ou une réponse avec message et profile
export type UpdatePatientProfileResponse =
  | PatientProfile
  | UpdatePatientProfileEnvelope;

@Injectable({
  providedIn: 'root'
})
export class PatientService {

  private readonly http = inject(HttpClient);

  private getHeaders(): HttpHeaders {
    const token = localStorage.getItem('auth_token');

    return new HttpHeaders({
      Authorization: `Bearer ${token ?? ''}`,
      'Content-Type': 'application/json'
    });
  }

  // Récupérer le profil
  getProfile(): Observable<PatientProfile> {
    return this.http.get<PatientProfile>(
      `${API_URL}/profile`,
      { headers: this.getHeaders() }
    );
  }

  // Modifier le profil
  updateProfile(
    data: UpdatePatientProfileRequest
  ): Observable<UpdatePatientProfileResponse> {
    return this.http.put<UpdatePatientProfileResponse>(
      `${API_URL}/profile`,
      data,
      { headers: this.getHeaders() }
    );
  }
}