
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';

const API_URL = 'http://localhost:5229/api/auth';

export interface RegisterRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  phoneNumber?: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface AuthResponse {
  token: string;
  expiresAt: string;
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string | null;
  roles: string[];
}

export interface UserProfile {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string | null;
  roles: string[];
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly http = inject(HttpClient);

  register(data: RegisterRequest): Observable<unknown> {
    return this.http.post(`${API_URL}/register`, data);
  }

  login(data: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(
      `${API_URL}/login`,
      data
    ).pipe(
      tap(response => {
        localStorage.setItem('auth_token', response.token);

        localStorage.setItem(
          'auth_user',
          JSON.stringify({
            id: response.id,
            firstName: response.firstName,
            lastName: response.lastName,
            email: response.email,
            roles: response.roles
          })
        );
      })
    );
  }

  getCurrentUser(): Observable<UserProfile> {
    const token = localStorage.getItem('auth_token');

    return this.http.get<UserProfile>(
      `${API_URL}/me`,
      {
        headers: {
          Authorization: `Bearer ${token ?? ''}`
        }
      }
    );
  }

 
logout(): void {
  localStorage.removeItem('auth_token');
  localStorage.removeItem('auth_user');
  localStorage.removeItem('role');
  localStorage.removeItem('email');
}

}
