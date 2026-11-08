
import { Component, ChangeDetectorRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthService } from '../../services/auth';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './register.html',
  styleUrl: './register.scss'
})
export class Register {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly cdr = inject(ChangeDetectorRef);

  firstName = '';
  lastName = '';
  email = '';
  phoneNumber = '';
  password = '';
  confirmPassword = '';

  errorMessage = '';
  validationErrors: string[] = [];
  successMessage = '';
  isLoading = false;

  onSubmit(): void {
    this.errorMessage = '';
    this.validationErrors = [];
    this.successMessage = '';

    if (
      !this.firstName.trim() ||
      !this.lastName.trim() ||
      !this.email.trim() ||
      !this.password
    ) {
      this.errorMessage =
        'Veuillez remplir tous les champs obligatoires.';
      return;
    }

    if (this.password.length < 8) {
      this.errorMessage =
        'Le mot de passe doit contenir au moins 8 caractères.';
      return;
    }

    if (this.password !== this.confirmPassword) {
      this.errorMessage =
        'Les mots de passe ne correspondent pas.';
      return;
    }

    this.isLoading = true;

    this.authService.register({
      firstName: this.firstName.trim(),
      lastName: this.lastName.trim(),
      email: this.email.trim(),
      password: this.password,
      phoneNumber: this.phoneNumber.trim() || undefined
    }).subscribe({
      next: () => {
        this.isLoading = false;
        this.successMessage =
          'Compte créé avec succès ! Vous pouvez vous connecter.';

        this.cdr.detectChanges();

        setTimeout(() => {
          this.router.navigate(['/login']);
        }, 1500);
      },

      error: (error: HttpErrorResponse) => {
        this.isLoading = false;
        this.errorMessage = '';
        this.validationErrors = [];

        const response = error.error;

        console.log('Register API response:', response);

        if (response && typeof response === 'object') {
          this.errorMessage =
            response.message || 'Impossible de créer le compte.';

          if (Array.isArray(response.errors)) {
            this.validationErrors = response.errors.map(
              (message: unknown) => this.translateError(String(message))
            );
          } else if (
            response.errors &&
            typeof response.errors === 'object'
          ) {
            this.validationErrors = Object.values(response.errors)
              .flat()
              .map((message: unknown) =>
                this.translateError(String(message))
              );
          }
        } else {
          this.errorMessage =
            error.status === 0
              ? 'Impossible de contacter le serveur.'
              : 'Une erreur est survenue lors de la création du compte.';
        }

        this.cdr.detectChanges();
      }
    });
  }

  private translateError(message: string): string {
    if (message.includes('non alphanumeric')) {
      return 'Le mot de passe doit contenir au moins un caractère spécial (@, #, !, etc.).';
    }

    if (message.includes('at least one digit')) {
      return 'Le mot de passe doit contenir au moins un chiffre.';
    }

    if (message.includes('at least one uppercase')) {
      return 'Le mot de passe doit contenir au moins une lettre majuscule.';
    }

    if (message.includes('at least one lowercase')) {
      return 'Le mot de passe doit contenir au moins une lettre minuscule.';
    }

    if (message.toLowerCase().includes('email')) {
      return 'L’adresse e-mail saisie est invalide ou déjà utilisée.';
    }

    return message;
  }
}
