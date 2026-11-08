
import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
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

@Component({
  selector: 'app-patient',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule
  ],
  templateUrl: './patient.html',
  styleUrl: './patient.scss'
})
export class PatientComponent implements OnInit {

  private readonly patientService = inject(PatientService);
  private readonly fb = inject(FormBuilder);

  profile: PatientProfile | null = null;

  isLoading = false;
  isSaving = false;
  isEditing = false;

  successMessage = '';
  errorMessage = '';

  patientForm = this.fb.group({
    firstName: ['', [
      Validators.required,
      Validators.maxLength(50)
    ]],
    lastName: ['', [
      Validators.required,
      Validators.maxLength(50)
    ]],
    phoneNumber: [''],
    dateOfBirth: ['']
  });

  ngOnInit(): void {
    this.loadProfile();
  }

  loadProfile(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.patientService.getProfile()
      .pipe(
        timeout({ first: 15000 }),
        finalize(() => {
          this.isLoading = false;
        })
      )
      .subscribe({
        next: (profile: PatientProfile) => {
          this.profile = profile;
          this.fillForm(profile);
        },
        error: (error: unknown) => {
          this.errorMessage = this.getErrorMessage(
            error,
            'Impossible de charger votre profil.'
          );
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

    this.fillForm(this.profile);
    this.isEditing = true;
    this.successMessage = '';
    this.errorMessage = '';
  }

  cancelEditing(): void {
    if (this.profile) {
      this.fillForm(this.profile);
    }

    this.isEditing = false;
    this.successMessage = '';
    this.errorMessage = '';
  }

  saveProfile(): void {
    if (this.isSaving) {
      return;
    }

    if (this.patientForm.invalid) {
      this.patientForm.markAllAsTouched();
      return;
    }

    const values = this.patientForm.getRawValue();

    const request: UpdatePatientProfileRequest = {
      firstName: values.firstName?.trim() ?? '',
      lastName: values.lastName?.trim() ?? '',
      phoneNumber: values.phoneNumber?.trim() || null,
      dateOfBirth: values.dateOfBirth || null
    };

    if (!request.firstName || !request.lastName) {
      this.errorMessage = 'Le prénom et le nom sont obligatoires.';
      return;
    }

    this.isSaving = true;
    this.successMessage = '';
    this.errorMessage = '';

    this.patientService.updateProfile(request)
      .pipe(
        timeout({ first: 15000 }),
        finalize(() => {
          this.isSaving = false;
        })
      )
      .subscribe({
        next: (response: UpdatePatientProfileResponse) => {
          let updatedProfile: PatientProfile;
          let message = 'Profil mis à jour avec succès.';

          if ('profile' in response && response.profile) {
            updatedProfile = response.profile;
            message = response.message || message;
          } else if ('id' in response) {
            updatedProfile = response;
          } else {
            this.errorMessage = 'Réponse du serveur non reconnue.';
            return;
          }

          this.profile = updatedProfile;
          this.fillForm(updatedProfile);
          this.isEditing = false;
          this.successMessage = message;
        },
        error: (error: unknown) => {
          this.errorMessage = this.getErrorMessage(
            error,
            'Erreur pendant la modification du profil.'
          );
        }
      });
  }

  hasError(fieldName: string): boolean {
    const field = this.patientForm.get(fieldName);

    return !!field &&
      field.invalid &&
      (field.touched || field.dirty);
  }

  private getErrorMessage(
    error: unknown,
    defaultMessage: string
  ): string {
    const err = error as {
      error?: {
        message?: string;
        title?: string;
      };
      message?: string;
      name?: string;
    };

    if (err?.name === 'TimeoutError') {
      return 'Le serveur ne répond pas. Vérifiez le backend.';
    }

    return (
      err?.error?.message ||
      err?.error?.title ||
      err?.message ||
      defaultMessage
    );
  }
}
