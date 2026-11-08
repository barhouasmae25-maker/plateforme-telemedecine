
import { CommonModule } from '@angular/common';
import {
  Component,
  OnInit,
  ChangeDetectorRef,
  inject
} from '@angular/core';

import {
  FormBuilder,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';

import { finalize, timeout } from 'rxjs';

import {
  PatientService,
  PatientProfile,
  UpdatePatientProfileRequest,
  UpdatePatientProfileResponse
} from '../../services/patient';

import {
  AppointmentService,
  AvailableDoctor,
  PatientAppointment,
  CreateAppointmentRequest,
  CreateAppointmentResponse
} from '../../services/appointment';

@Component({
  selector: 'app-patient',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './patient.html',
  styleUrl: './patient.scss'
})
export class PatientComponent implements OnInit {

  private readonly patientService = inject(PatientService);
  private readonly appointmentService = inject(AppointmentService);
  private readonly fb = inject(FormBuilder);
  private readonly cdr = inject(ChangeDetectorRef);

  profile: PatientProfile | null = null;

  // Profil
  isLoading = false;
  isEditing = false;
  isSaving = false;

  errorMessage = '';
  successMessage = '';

  patientForm = this.fb.group({
    firstName: ['', [Validators.required, Validators.minLength(2)]],
    lastName: ['', [Validators.required, Validators.minLength(2)]],
    phoneNumber: [''],
    dateOfBirth: ['']
  });

  // Rendez-vous
  doctors: AvailableDoctor[] = [];
  appointments: PatientAppointment[] = [];

  isLoadingDoctors = false;
  isLoadingAppointments = false;
  isBooking = false;
  cancellingAppointmentId: number | null = null;

  appointmentError = '';
  appointmentSuccess = '';

  appointmentForm = this.fb.group({
    doctorId: [null as number | null, Validators.required],
    appointmentDate: ['', Validators.required],
    appointmentTime: ['', Validators.required],
    reason: ['', Validators.maxLength(1000)]
  });

  ngOnInit(): void {
    this.loadProfile();
    this.loadAvailableDoctors();
    this.loadAppointments();
  }

  // -------------------------------
  // PROFIL PATIENT
  // -------------------------------

  loadProfile(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.cdr.detectChanges();

    this.patientService.getProfile()
      .pipe(
        timeout({ first: 15000 }),
        finalize(() => {
          this.isLoading = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: (profile: PatientProfile) => {
          this.profile = profile;
          this.fillForm(profile);
          this.cdr.detectChanges();
        },
        error: (error: unknown) => {
          this.errorMessage = this.getErrorMessage(
            error,
            'Impossible de charger votre profil.'
          );
          this.cdr.detectChanges();
        }
      });
  }

  private fillForm(profile: PatientProfile): void {
    this.patientForm.patchValue({
      firstName: profile.firstName ?? '',
      lastName: profile.lastName ?? '',
      phoneNumber: profile.phoneNumber ?? '',
      dateOfBirth: profile.dateOfBirth
        ? profile.dateOfBirth.substring(0, 10)
        : ''
    });
  }

  startEditing(): void {
    if (!this.profile) {
      return;
    }

    this.successMessage = '';
    this.errorMessage = '';

    this.fillForm(this.profile);
    this.isEditing = true;

    this.cdr.detectChanges();
  }

  cancelEditing(): void {
    if (this.profile) {
      this.fillForm(this.profile);
    }

    this.isEditing = false;
    this.errorMessage = '';
    this.successMessage = '';

    this.cdr.detectChanges();
  }

  saveProfile(): void {
    if (this.patientForm.invalid || this.isSaving) {
      this.patientForm.markAllAsTouched();
      return;
    }

    const formValue = this.patientForm.getRawValue();

    const data: UpdatePatientProfileRequest = {
      firstName: formValue.firstName?.trim() ?? '',
      lastName: formValue.lastName?.trim() ?? '',
      phoneNumber: formValue.phoneNumber?.trim() || null,
      dateOfBirth: formValue.dateOfBirth || null
    };

    this.isSaving = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.cdr.detectChanges();

    this.patientService.updateProfile(data)
      .pipe(
        timeout({ first: 15000 }),
        finalize(() => {
          this.isSaving = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: (response: UpdatePatientProfileResponse) => {
          const updatedProfile = this.extractProfile(response);

          if (updatedProfile) {
            this.profile = updatedProfile;
            this.fillForm(updatedProfile);
            this.isEditing = false;

            this.successMessage =
              'Votre profil a été mis à jour avec succès.';
          } else {
            this.isEditing = false;
            this.successMessage = 'Modification enregistrée.';
            this.loadProfile();
          }

          this.cdr.detectChanges();
        },
        error: (error: unknown) => {
          this.errorMessage = this.getErrorMessage(
            error,
            'Impossible de modifier votre profil.'
          );
          this.cdr.detectChanges();
        }
      });
  }

  private extractProfile(
    response: UpdatePatientProfileResponse
  ): PatientProfile | null {
    if ('profile' in response && response.profile) {
      return response.profile;
    }

    if ('id' in response && typeof response.id === 'number') {
      return response as PatientProfile;
    }

    return null;
  }

  // -------------------------------
  // MEDECINS DISPONIBLES
  // -------------------------------

  loadAvailableDoctors(): void {
    this.isLoadingDoctors = true;
    this.appointmentError = '';

    this.appointmentService.getAvailableDoctors()
      .pipe(
        timeout({ first: 15000 }),
        finalize(() => {
          this.isLoadingDoctors = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: (doctors: AvailableDoctor[]) => {
          this.doctors = doctors;
          this.cdr.detectChanges();
        },
        error: (error: unknown) => {
          this.appointmentError = this.getErrorMessage(
            error,
            'Impossible de charger la liste des médecins.'
          );
          this.cdr.detectChanges();
        }
      });
  }

  // -------------------------------
  // CREATION RENDEZ-VOUS
  // -------------------------------

  get minimumDate(): string {
    const now = new Date();
    const year = now.getFullYear();
    const month = String(now.getMonth() + 1).padStart(2, '0');
    const day = String(now.getDate()).padStart(2, '0');

    return `${year}-${month}-${day}`;
  }

  bookAppointment(): void {
    if (this.appointmentForm.invalid || this.isBooking) {
      this.appointmentForm.markAllAsTouched();
      return;
    }

    const value = this.appointmentForm.getRawValue();

    if (
      !value.doctorId ||
      !value.appointmentDate ||
      !value.appointmentTime
    ) {
      return;
    }

    const localStart = new Date(
      `${value.appointmentDate}T${value.appointmentTime}`
    );

    if (Number.isNaN(localStart.getTime()) || localStart <= new Date()) {
      this.appointmentError =
        'Veuillez choisir une date et une heure dans le futur.';
      return;
    }

    const localEnd = new Date(localStart.getTime() + 30 * 60 * 1000);

    const data: CreateAppointmentRequest = {
      doctorId: value.doctorId,
      startAt: localStart.toISOString(),
      endAt: localEnd.toISOString(),
      reason: value.reason?.trim() || null
    };

    this.isBooking = true;
    this.appointmentError = '';
    this.appointmentSuccess = '';

    this.appointmentService.createAppointment(data)
      .pipe(
        timeout({ first: 15000 }),
        finalize(() => {
          this.isBooking = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: (response: CreateAppointmentResponse) => {
          this.appointmentSuccess =
            response.message || 'Votre demande de rendez-vous a été envoyée.';

          this.appointmentForm.reset({
            doctorId: null,
            appointmentDate: '',
            appointmentTime: '',
            reason: ''
          });

          this.loadAppointments();
          this.cdr.detectChanges();
        },
        error: (error: unknown) => {
          this.appointmentError = this.getErrorMessage(
            error,
            'Impossible de réserver ce rendez-vous.'
          );
          this.cdr.detectChanges();
        }
      });
  }

  // -------------------------------
  // LISTE DES RENDEZ-VOUS
  // -------------------------------

  loadAppointments(): void {
    this.isLoadingAppointments = true;

    this.appointmentService.getMyAppointments()
      .pipe(
        timeout({ first: 15000 }),
        finalize(() => {
          this.isLoadingAppointments = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: (appointments: PatientAppointment[]) => {
          this.appointments = appointments;
          this.cdr.detectChanges();
        },
        error: (error: unknown) => {
          this.appointmentError = this.getErrorMessage(
            error,
            'Impossible de charger vos rendez-vous.'
          );
          this.cdr.detectChanges();
        }
      });
  }

  // -------------------------------
  // ANNULATION
  // -------------------------------

  cancelAppointment(appointment: PatientAppointment): void {
    if (
      appointment.status !== 'Pending' &&
      appointment.status !== 'Confirmed'
    ) {
      return;
    }

    const confirmed = window.confirm(
      `Voulez-vous vraiment annuler le rendez-vous avec Dr ${appointment.doctorFirstName} ${appointment.doctorLastName} ?`
    );

    if (!confirmed) {
      return;
    }

    this.cancellingAppointmentId = appointment.appointmentId;
    this.appointmentError = '';
    this.appointmentSuccess = '';

    this.appointmentService.cancelAppointment(
      appointment.appointmentId,
      'Annulation demandée par le patient'
    )
      .pipe(
        timeout({ first: 15000 }),
        finalize(() => {
          this.cancellingAppointmentId = null;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: (response) => {
          this.appointmentSuccess =
            response.message || 'Le rendez-vous a été annulé.';

          this.loadAppointments();
          this.cdr.detectChanges();
        },
        error: (error: unknown) => {
          this.appointmentError = this.getErrorMessage(
            error,
            'Impossible d’annuler ce rendez-vous.'
          );
          this.cdr.detectChanges();
        }
      });
  }

  // -------------------------------
  // AFFICHAGE DES STATUTS
  // -------------------------------

  getStatusLabel(status: string): string {
    switch (status) {
      case 'Pending':
        return 'En attente';
      case 'Confirmed':
        return 'Confirmé';
      case 'Rejected':
        return 'Refusé';
      case 'Cancelled':
        return 'Annulé';
      case 'Completed':
        return 'Terminé';
      default:
        return status;
    }
  }

  getStatusClass(status: string): string {
    switch (status) {
      case 'Pending':
        return 'status-pending';
      case 'Confirmed':
        return 'status-confirmed';
      case 'Rejected':
        return 'status-rejected';
      case 'Cancelled':
        return 'status-cancelled';
      case 'Completed':
        return 'status-completed';
      default:
        return '';
    }
  }

  // -------------------------------
  // GESTION DES ERREURS
  // -------------------------------

  private getErrorMessage(
    error: unknown,
    fallback: string
  ): string {
    if (
      typeof error === 'object' &&
      error !== null &&
      'name' in error &&
      error.name === 'TimeoutError'
    ) {
      return 'Le serveur met trop de temps à répondre. Réessayez.';
    }

    if (
      typeof error === 'object' &&
      error !== null &&
      'error' in error
    ) {
      const serverError = error.error;

      if (typeof serverError === 'string') {
        return serverError;
      }

      if (
        typeof serverError === 'object' &&
        serverError !== null &&
        'message' in serverError &&
        typeof serverError.message === 'string'
      ) {
        return serverError.message;
      }

      if (
        typeof serverError === 'object' &&
        serverError !== null &&
        'title' in serverError &&
        typeof serverError.title === 'string'
      ) {
        return serverError.title;
      }
    }

    return fallback;
  }
}
