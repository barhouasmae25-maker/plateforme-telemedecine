
import { ComponentFixture, TestBed } from '@angular/core/testing';
import {
  HttpClientTestingModule,
  HttpTestingController
} from '@angular/common/http/testing';

import { AdminComponent } from './admin';

describe('AdminComponent', () => {
  let component: AdminComponent;
  let fixture: ComponentFixture<AdminComponent>;
  let httpMock: HttpTestingController;

  const apiUrl = 'http://localhost:5229/api/admin/users';

  const mockUsers = [
    {
      id: 'user-1',
      firstName: 'Ahmed',
      lastName: 'Benali',
      email: 'ahmed@example.com',
      userName: 'ahmed',
      phoneNumber: '0600000001',
      role: 'MEDECIN',
      roles: ['MEDECIN'],
      isActive: true,
      isProfessionalApproved: false,
      createdAt: '2026-10-20T10:00:00Z'
    },
    {
      id: 'user-2',
      firstName: 'Sara',
      lastName: 'Amrani',
      email: 'sara@example.com',
      userName: 'sara',
      phoneNumber: '0600000002',
      role: 'PATIENT',
      roles: ['PATIENT'],
      isActive: false,
      isProfessionalApproved: false,
      createdAt: '2026-10-21T10:00:00Z'
    }
  ];

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        AdminComponent,
        HttpClientTestingModule
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(AdminComponent);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.removeItem('auth_token');
    localStorage.removeItem('token');
  });

  it('should create the component', () => {
    expect(component).toBeTruthy();

    component.loadUsers();
    httpMock.expectOne(apiUrl).flush([]);
  });

  it('should load users and stop loading', () => {
    component.loadUsers();

    const request = httpMock.expectOne(apiUrl);

    expect(request.request.method).toBe('GET');

    request.flush(mockUsers);

    expect(component.users.length).toBe(2);
    expect(component.filteredUsers.length).toBe(2);
    expect(component.isLoading).toBe(false);
  });

  it('should display users in the template', () => {
    component.loadUsers();

    httpMock.expectOne(apiUrl).flush(mockUsers);

    fixture.detectChanges();

    const pageText = fixture.nativeElement.textContent;

    expect(pageText).toContain('Ahmed Benali');
    expect(pageText).toContain('Sara Amrani');
    expect(pageText).toContain('Chargement : Terminé');
  });

  it('should filter users by role', () => {
    component.users = [...mockUsers];
    component.selectedRole = 'MEDECIN';

    component.applyFilters();

    expect(component.filteredUsers.length).toBe(1);
    expect(component.filteredUsers[0].id).toBe('user-1');
  });

  it('should filter users by active status', () => {
    component.users = [...mockUsers];
    component.selectedStatus = 'ACTIVE';

    component.applyFilters();

    expect(component.filteredUsers.length).toBe(1);
    expect(component.filteredUsers[0].id).toBe('user-1');
  });

  it('should search users by email', () => {
    component.users = [...mockUsers];
    component.searchTerm = 'sara@example.com';

    component.applyFilters();

    expect(component.filteredUsers.length).toBe(1);
    expect(component.filteredUsers[0].id).toBe('user-2');
  });

  it('should clear filters', () => {
    component.users = [...mockUsers];
    component.searchTerm = 'Ahmed';
    component.selectedRole = 'MEDECIN';
    component.selectedStatus = 'ACTIVE';

    component.clearFilters();

    expect(component.searchTerm).toBe('');
    expect(component.selectedRole).toBe('ALL');
    expect(component.selectedStatus).toBe('ALL');
    expect(component.filteredUsers.length).toBe(2);
  });

  it('should display an error if loading users fails', () => {
    component.loadUsers();

    const request = httpMock.expectOne(apiUrl);

    request.flush(
      { message: 'Accès refusé' },
      { status: 403, statusText: 'Forbidden' }
    );

    expect(component.isLoading).toBe(false);
    expect(component.users.length).toBe(0);
    expect(component.errorMessage).toContain('Accès refusé');
  });

  it('should update user status after a successful response', () => {
    const user = { ...mockUsers[0] };

    component.users = [user];
    component.applyFilters();

    const originalConfirm = window.confirm;
    window.confirm = () => true;

    try {
      component.toggleUserStatus(user);

      const request = httpMock.expectOne(
        `${apiUrl}/${user.id}/status`
      );

      expect(request.request.method).toBe('PUT');
      expect(request.request.body).toEqual({ isActive: false });

      request.flush({
        message: 'Compte désactivé avec succès.',
        id: user.id,
        isActive: false
      });

      expect(user.isActive).toBe(false);
      expect(component.successMessage).toContain('désactivé');
    } finally {
      window.confirm = originalConfirm;
    }
  });
});
