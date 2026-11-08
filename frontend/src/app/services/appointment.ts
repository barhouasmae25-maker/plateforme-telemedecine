
import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';

const API_URL = 'http://localhost:5229/api';

// ===============================
// DOCTOR
// ===============================

export interface AvailableDoctor {
  id: number;
  firstName: string;
  lastName: string;
  specialty: string;
  clinicAddress: string | null;
  description: string | null;
  consultationFee: number;
}

// ===============================
// PATIENT APPOINTMENT
// ===============================

export interface PatientAppointment {
  appointmentId: number;
  doctorId: number;
  doctorFirstName: string;
  doctorLastName: string;
  specialty: string;
  startAt: string;
  endAt: string;
  reason: string | null;
  status: string;
  createdAt: string;
}

// ===============================
// DOCTOR APPOINTMENT
// ===============================

export interface DoctorAppointment {
  appointmentId: number;
  patientId: number;
  patientFirstName: string;
  patientLastName: string;
  patientEmail: string;
  patientPhoneNumber: string | null;
  startAt: string;
  endAt: string;
  reason: string | null;
  status: string;
  createdAt: string;
}

// ===============================
// REQUESTS & RESPONSES
// ===============================

export interface CreateAppointmentRequest {
  doctorId: number;
  startAt: string;
  endAt: string;
  reason: string | null;
}

export interface CreateAppointmentResponse {
  message: string;
  appointment: PatientAppointment;
}

export interface UpdateAppointmentStatusRequest {
  status: 'Confirmed' | 'Rejected';
}

export interface ApiMessageResponse {
  message: string;
}

// ===============================
// APPOINTMENT SERVICE
// ===============================

@Injectable({
  providedIn: 'root'
})
export class AppointmentService {

  private readonly http = inject(HttpClient);

  // Authentication headers
  private getHeaders(): HttpHeaders {
    const token = localStorage.getItem('auth_token');

    return new HttpHeaders({
      Authorization: `Bearer ${token ?? ''}`,
      'Content-Type': 'application/json'
    });
  }

  // =================================
  // PATIENT METHODS
  // =================================

  // Liste des médecins disponibles
  getAvailableDoctors(): Observable<AvailableDoctor[]> {
    return this.http.get<AvailableDoctor[]>(
      `${API_URL}/doctors`,
      { headers: this.getHeaders() }
    );
  }

  // Réserver un rendez-vous
  createAppointment(
    data: CreateAppointmentRequest
  ): Observable<CreateAppointmentResponse> {
    return this.http.post<CreateAppointmentResponse>(
      `${API_URL}/patient/appointments`,
      data,
      { headers: this.getHeaders() }
    );
  }

  // Consulter mes rendez-vous
  getMyAppointments(): Observable<PatientAppointment[]> {
    return this.http.get<PatientAppointment[]>(
      `${API_URL}/patient/appointments`,
      { headers: this.getHeaders() }
    );
  }

  // Annuler un rendez-vous
  cancelAppointment(
    appointmentId: number,
    reason: string | null
  ): Observable<ApiMessageResponse> {
    return this.http.put<ApiMessageResponse>(
      `${API_URL}/patient/appointments/${appointmentId}/cancel`,
      { reason },
      { headers: this.getHeaders() }
    );
  }

  // =================================
  // DOCTOR METHODS
  // =================================

  // Consulter les rendez-vous reçus par le médecin
  getDoctorAppointments(): Observable<DoctorAppointment[]> {
    return this.http.get<DoctorAppointment[]>(
      `${API_URL}/doctor/appointments`,
      { headers: this.getHeaders() }
    );
  }

  // Accepter ou refuser un rendez-vous
  updateAppointmentStatus(
    appointmentId: number,
    status: 'Confirmed' | 'Rejected'
  ): Observable<ApiMessageResponse> {

    const request: UpdateAppointmentStatusRequest = {
      status
    };

    return this.http.put<ApiMessageResponse>(
      `${API_URL}/doctor/appointments/${appointmentId}/status`,
      request,
      { headers: this.getHeaders() }
    );
  }

}
