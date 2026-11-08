
import { Component, ChangeDetectorRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize, timeout } from 'rxjs';
import { AuthService } from '../../services/auth';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './login.html',
  styleUrl: './login.scss'
})
export class Login {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly cdr = inject(ChangeDetectorRef);

  email = '';
  password = '';
  errorMessage = '';
  isLoading = false;

  onSubmit(): void {
    this.errorMessage = '';

    if (!this.email.trim() || !this.password) {
      this.errorMessage = 'Veuillez remplir tous les champs.';
      return;
    }

    if (this.isLoading) {
      return;
    }

    this.isLoading = true;
    this.cdr.detectChanges();

    this.authService.login({
      email: this.email.trim(),
      password: this.password
    }).pipe(
      timeout(15000),
      finalize(() => {
        this.isLoading = false;
        this.cdr.detectChanges();
        console.log('Chargement terminé :', this.isLoading);
      })
    ).subscribe({
      next: (response) => {
        if (!response?.token) {
          this.authService.logout();
          this.errorMessage = 'Le serveur n’a pas retourné de token.';
          this.cdr.detectChanges();
          return;
        }

        const roles = (response.roles ?? []).map(role =>
          role.replace(/^ROLE_/, '').toUpperCase()
        );

        let destination = '';

        if (roles.includes('ADMIN')) {
          destination = '/admin';
        } else if (roles.includes('MEDECIN')) {
          destination = '/medecin';
        } else if (roles.includes('PHARMACIEN')) {
          destination = '/pharmacien';
        } else if (roles.includes('PATIENT')) {
          destination = '/patient';
        }

        if (!destination) {
          this.authService.logout();
          this.errorMessage =
            'Aucun rôle valide n’a été trouvé pour cet utilisateur.';
          this.cdr.detectChanges();
          return;
        }

        this.router.navigateByUrl(destination).then(
          navigated => {
            if (!navigated) {
              this.errorMessage =
                'La connexion a réussi, mais la navigation a échoué.';
              this.cdr.detectChanges();
            }
          },
          error => {
            console.error('Erreur de navigation :', error);
            this.errorMessage =
              'Une erreur est survenue pendant la navigation.';
            this.cdr.detectChanges();
          }
        );
      },

      error: (error: any) => {
        console.error('Erreur pendant la connexion :', error);

        if (error.name === 'TimeoutError') {
          this.errorMessage =
            'Le serveur met trop de temps à répondre. Réessayez.';
        } else if (error.status === 400 || error.status === 401) {
          this.errorMessage = 'Email ou mot de passe incorrect.';
        } else if (error.status === 0) {
          this.errorMessage =
            'Impossible de contacter le serveur. Vérifiez que le backend est démarré.';
        } else {
          this.errorMessage =
            error.error?.message ??
            'Une erreur est survenue pendant la connexion.';
        }

        this.cdr.detectChanges();
      }
    });
  }
}
