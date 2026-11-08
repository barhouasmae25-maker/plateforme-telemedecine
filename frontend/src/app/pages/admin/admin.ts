import { CommonModule } from '@angular/common';
import {
  HttpClient,
  HttpErrorResponse,
  HttpHeaders
} from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { TimeoutError } from 'rxjs';
import { finalize, timeout } from 'rxjs/operators';

interface AdminUser {
  id: string | number;
  firstName?: string;
  lastName?: string;
  fullName?: string;
  name?: string;
  userName?: string;
  email?: string;
  phoneNumber?: string;
  role?: string;
  roles?: string[];
  isActive: boolean;
  isProfessionalApproved?: boolean;
  createdAt?: string;
}

interface UsersResponse {
  users?: AdminUser[];
}

interface StatusResponse {
  message?: string;
  id?: string;
  isActive?: boolean;
}

interface ApprovalResponse {
  message?: string;
  isProfessionalApproved?: boolean;
}

@Component({
  selector: 'app-admin',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin.html',
  styleUrl: './admin.scss'
})
export class AdminComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  private readonly apiUrl = 'http://localhost:5229/api/admin/users';

  users: AdminUser[] = [];
  filteredUsers: AdminUser[] = [];

  isLoading = false;
  errorMessage = '';
  successMessage = '';

  searchTerm = '';
  selectedRole = 'ALL';
  selectedStatus = 'ALL';

  updatingUserId: string | number | null = null;
  approvingUserId: string | number | null = null;

  ngOnInit(): void {
    this.loadUsers();
  }

  private getHeaders(): HttpHeaders {
    const token =
      localStorage.getItem('auth_token') ||
      localStorage.getItem('token');

    let headers = new HttpHeaders();

    if (token) {
      headers = headers.set('Authorization', `Bearer ${token}`);
    }

    return headers;
  }

  loadUsers(): void {
    if (this.isLoading) {
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    this.http.get<AdminUser[] | UsersResponse>(this.apiUrl, {
      headers: this.getHeaders()
    })
    .pipe(
      timeout(15000),
      finalize(() => {
        this.isLoading = false;
      })
    )
    .subscribe({
      next: response => {
        let result: AdminUser[];

        if (Array.isArray(response)) {
          result = response;
        } else if (response && Array.isArray(response.users)) {
          result = response.users;
        } else {
          this.errorMessage =
            'Le format des données retournées par le serveur est invalide.';
          return;
        }

        this.users = result.map(user => ({ ...user }));
        this.applyFilters();

        console.log('Utilisateurs reçus :', this.users.length);
      },
      error: (error: unknown) => {
        console.error('Erreur chargement utilisateurs :', error);
        this.errorMessage = this.getErrorMessage(
          error,
          'Impossible de charger les utilisateurs.'
        );
      }
    });
  }

  applyFilters(): void {
    const search = this.searchTerm.trim().toLowerCase();

    this.filteredUsers = this.users.filter(user => {
      const name = this.getUserName(user).toLowerCase();
      const email = (user.email || '').toLowerCase();
      const phone = (user.phoneNumber || '').toLowerCase();
      const role = this.getUserRole(user);

      const matchesSearch =
        !search ||
        name.includes(search) ||
        email.includes(search) ||
        phone.includes(search);

      const matchesRole =
        this.selectedRole === 'ALL' ||
        role === this.selectedRole;

      const matchesStatus =
        this.selectedStatus === 'ALL' ||
        (this.selectedStatus === 'ACTIVE' && user.isActive === true) ||
        (this.selectedStatus === 'INACTIVE' && user.isActive === false);

      return matchesSearch && matchesRole && matchesStatus;
    });
  }

  getUserName(user: AdminUser): string {
    const firstLast =
      `${user.firstName || ''} ${user.lastName || ''}`.trim();

    return (
      user.fullName?.trim() ||
      user.name?.trim() ||
      firstLast ||
      user.userName ||
      user.email ||
      'Utilisateur sans nom'
    );
  }

  getUserInitials(user: AdminUser): string {
    const first = user.firstName?.trim().charAt(0) || '';
    const last = user.lastName?.trim().charAt(0) || '';

    return (
      `${first}${last}`.toUpperCase() ||
      user.email?.charAt(0).toUpperCase() ||
      '?'
    );
  }

  getUserRole(user: AdminUser): string {
    return (user.role || user.roles?.[0] || 'NON DÉFINI')
      .replace(/^ROLE_/, '')
      .toUpperCase();
  }

  getRoleLabel(role: string): string {
    const labels: Record<string, string> = {
      ADMIN: 'Administrateur',
      MEDECIN: 'Médecin',
      PHARMACIEN: 'Pharmacien',
      PATIENT: 'Patient',
      SECRETAIRE: 'Secrétaire'
    };

    return labels[role] || role;
  }

  isProfessional(user: AdminUser): boolean {
    const role = this.getUserRole(user);
    return role === 'MEDECIN' || role === 'PHARMACIEN';
  }

  get totalUsers(): number {
    return this.users.length;
  }

  get activeUsers(): number {
    return this.users.filter(user => user.isActive === true).length;
  }

  get inactiveUsers(): number {
    return this.users.filter(user => user.isActive === false).length;
  }

  get pendingProfessionals(): number {
    return this.users.filter(user =>
      this.isProfessional(user) &&
      user.isProfessionalApproved !== true
    ).length;
  }

  get isUpdating(): boolean {
    return this.updatingUserId !== null ||
      this.approvingUserId !== null;
  }

  resetFilters(): void {
    this.searchTerm = '';
    this.selectedRole = 'ALL';
    this.selectedStatus = 'ALL';
    this.applyFilters();
  }

  clearFilters(): void {
    this.resetFilters();
  }

  toggleUserStatus(user: AdminUser): void {
    if (this.isUpdating) {
      return;
    }

    const newStatus = !user.isActive;
    const action = newStatus ? 'activer' : 'désactiver';

    if (!window.confirm(
      `Voulez-vous vraiment ${action} le compte de ${this.getUserName(user)} ?`
    )) {
      return;
    }

    this.updatingUserId = user.id;
    this.errorMessage = '';
    this.successMessage = '';

    this.http.put<StatusResponse>(
      `${this.apiUrl}/${encodeURIComponent(String(user.id))}/status`,
      { isActive: newStatus },
      { headers: this.getHeaders() }
    )
    .pipe(
      timeout(15000),
      finalize(() => {
        this.updatingUserId = null;
      })
    )
    .subscribe({
      next: response => {
        const savedStatus = response.isActive ?? newStatus;

        // Mettre à jour l'utilisateur concerné dans la liste.
        this.users = this.users.map(currentUser =>
          String(currentUser.id) === String(user.id)
            ? { ...currentUser, isActive: savedStatus }
            : currentUser
        );

        // Recalculer les filtres et les statistiques.
        this.applyFilters();

        this.successMessage =
          response.message || 'Statut du compte modifié avec succès.';

        console.log('Réponse du backend :', response);
        console.log('Statut affiché :', savedStatus);

        // Vérifier le statut enregistré en rechargeant les données.
        this.refreshUsersAfterStatusChange();
      },
      error: (error: unknown) => {
        console.error('Erreur modification du compte :', error);
        this.errorMessage = this.getErrorMessage(
          error,
          'Impossible de modifier le statut du compte.'
        );
      }
    });
  }

  private refreshUsersAfterStatusChange(): void {
    this.http.get<AdminUser[] | UsersResponse>(this.apiUrl, {
      headers: this.getHeaders()
    })
    .pipe(timeout(15000))
    .subscribe({
      next: response => {
        let result: AdminUser[] | null = null;

        if (Array.isArray(response)) {
          result = response;
        } else if (response && Array.isArray(response.users)) {
          result = response.users;
        }

        if (result) {
          this.users = result.map(user => ({ ...user }));
          this.applyFilters();
        }
      },
      error: error => {
        // Le changement a déjà réussi ; signaler seulement
        // l'échec de vérification si nécessaire.
        console.error('Vérification du statut impossible :', error);
      }
    });
  }

  approveProfessional(user: AdminUser): void {
    if (
      !this.isProfessional(user) ||
      !user.isActive ||
      user.isProfessionalApproved === true ||
      this.isUpdating
    ) {
      return;
    }

    if (!window.confirm(
      `Approuver le compte de ${this.getUserName(user)} ?`
    )) {
      return;
    }

    this.approvingUserId = user.id;
    this.errorMessage = '';
    this.successMessage = '';

    this.http.put<ApprovalResponse>(
      `${this.apiUrl}/${encodeURIComponent(String(user.id))}/approve-professional`,
      {},
      { headers: this.getHeaders() }
    )
    .pipe(
      timeout(15000),
      finalize(() => {
        this.approvingUserId = null;
      })
    )
    .subscribe({
      next: response => {
        this.users = this.users.map(currentUser =>
          String(currentUser.id) === String(user.id)
            ? {
                ...currentUser,
                isProfessionalApproved:
                  response.isProfessionalApproved ?? true
              }
            : currentUser
        );

        this.applyFilters();

        this.successMessage =
          response.message || 'Compte professionnel approuvé avec succès.';
      },
      error: (error: unknown) => {
        console.error('Erreur approbation professionnel :', error);
        this.errorMessage = this.getErrorMessage(
          error,
          'Impossible d’approuver le compte professionnel.'
        );
      }
    });
  }

  private getErrorMessage(error: unknown, fallback: string): string {
    if (error instanceof TimeoutError) {
      return 'Le serveur met trop de temps à répondre.';
    }

    if (error instanceof HttpErrorResponse) {
      if (error.status === 401) {
        return 'Session expirée. Veuillez vous reconnecter.';
      }

      if (error.status === 403) {
        return 'Accès refusé : page réservée à l’administration.';
      }

      if (error.status === 0) {
        return 'Impossible de contacter le serveur. Vérifiez le backend.';
      }

      return error.error?.message ||
        `Erreur HTTP ${error.status}.`;
    }

    return fallback;
  }

  logout(): void {
    localStorage.removeItem('auth_token');
    localStorage.removeItem('token');
    localStorage.removeItem('role');
    localStorage.removeItem('email');

    this.router.navigate(['/login']);
  }

  trackByUserId(_index: number, user: AdminUser): string | number {
    return user.id;
  }
}