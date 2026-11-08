
import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { finalize, timeout } from 'rxjs';

import {
  AppointmentService,
  DoctorAppointment
} from '../../services/appointment';

@Component({
  selector: 'app-medecin',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './medecin.html',
  styleUrl: './medecin.scss'
})
export class Medecin implements OnInit {

  private appointmentService = inject(AppointmentService);

  appointments: DoctorAppointment[] = [];

  isLoading = false;
  processingId: number | null = null;

  errorMessage = '';
  successMessage = '';

  selectedFilter = 'ALL';

  ngOnInit(): void {
    this.loadAppointments();
  }

  // Charger les rendez-vous depuis le Backend
  loadAppointments(): void {

    if (this.isLoading) {
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.appointmentService.getDoctorAppointments()
      .pipe(
        timeout(15000),
        finalize(() => {
          this.isLoading = false;
        })
      )
      .subscribe({

        next: (data) => {
          this.appointments = data;
        },

        error: (error) => {

          console.error('Erreur chargement rendez-vous:', error);

          if (error.name === 'TimeoutError') {
            this.errorMessage =
              'Le serveur ne répond pas. Veuillez réessayer.';
          } else if (error.status === 401) {
            this.errorMessage =
              'Session expirée. Veuillez vous reconnecter.';
          } else if (error.status === 403) {
            this.errorMessage =
              'Accès refusé. Votre compte médecin est nécessaire.';
          } else {
            this.errorMessage =
              'Impossible de charger les rendez-vous.';
          }
        }
      });
  }

  // Réinitialiser les filtres du tableau
  resetFilters(): void {
    this.selectedFilter = 'ALL';
    this.errorMessage = '';
    this.successMessage = '';
  }

  // Filtrer les rendez-vous
  get filteredAppointments(): DoctorAppointment[] {

    if (this.selectedFilter === 'ALL') {
      return this.appointments;
    }

    return this.appointments.filter(appointment =>
      appointment.status.toUpperCase() === this.selectedFilter
    );
  }

  // Statistiques
  get totalAppointments(): number {
    return this.appointments.length;
  }

  get pendingCount(): number {
    return this.appointments.filter(a =>
      a.status.toUpperCase() === 'PENDING'
    ).length;
  }

  get confirmedCount(): number {
    return this.appointments.filter(a =>
      a.status.toUpperCase() === 'CONFIRMED'
    ).length;
  }

  get rejectedCount(): number {
    return this.appointments.filter(a =>
      a.status.toUpperCase() === 'REJECTED'
    ).length;
  }

  // Vérifier si le rendez-vous est en attente
  isPending(status: string): boolean {
    return status.toUpperCase() === 'PENDING';
  }

  // Accepter un rendez-vous
  acceptAppointment(appointment: DoctorAppointment): void {
    this.updateStatus(appointment, 'Confirmed');
  }

  // Refuser un rendez-vous
  rejectAppointment(appointment: DoctorAppointment): void {

    const confirmed = window.confirm(
      'Voulez-vous vraiment refuser ce rendez-vous ?'
    );

    if (!confirmed) {
      return;
    }

    this.updateStatus(appointment, 'Rejected');
  }

  // Modifier le statut du rendez-vous
  private updateStatus(
    appointment: DoctorAppointment,
    status: 'Confirmed' | 'Rejected'
  ): void {

    if (this.processingId !== null) {
      return;
    }

    this.processingId = appointment.appointmentId;
    this.errorMessage = '';
    this.successMessage = '';

    this.appointmentService.updateAppointmentStatus(
      appointment.appointmentId,
      status
    )
    .pipe(
      timeout(15000),
      finalize(() => {
        this.processingId = null;
      })
    )
    .subscribe({

      next: (response) => {

        appointment.status = status;

        this.successMessage =
          response.message ||
          'Statut du rendez-vous modifié avec succès.';
      },

      error: (error) => {

        console.error('Erreur modification statut:', error);

        if (error.name === 'TimeoutError') {
          this.errorMessage =
            'Le serveur ne répond pas. Veuillez réessayer.';
        } else if (error.status === 401) {
          this.errorMessage =
            'Session expirée. Veuillez vous reconnecter.';
        } else if (error.status === 403) {
          this.errorMessage =
            'Vous n’avez pas le droit de modifier ce rendez-vous.';
        } else {
          this.errorMessage =
            error.error?.message ||
            'Impossible de modifier le statut du rendez-vous.';
        }
      }
    });
  }

  // Libellé du statut
  getStatusLabel(status: string): string {

    switch (status.toUpperCase()) {

      case 'PENDING':
        return 'En attente';

      case 'CONFIRMED':
        return 'Confirmé';

      case 'REJECTED':
        return 'Refusé';

      default:
        return status;
    }
  }

  // Classe CSS du statut
  getStatusClass(status: string): string {

    switch (status.toUpperCase()) {

      case 'PENDING':
        return 'status-pending';

      case 'CONFIRMED':
        return 'status-confirmed';

      case 'REJECTED':
        return 'status-rejected';

      default:
        return '';
    }
  }

  // Formater la date
  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('fr-FR', {
      day: '2-digit',
      month: 'long',
      year: 'numeric'
    });
  }

  // Formater l'heure
  formatTime(date: string): string {
    return new Date(date).toLocaleTimeString('fr-FR', {
      hour: '2-digit',
      minute: '2-digit'
    });
  }
}
