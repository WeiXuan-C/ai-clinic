# Authentication Implementation

## Overview
Converted the authentication system from email OTP to email/password authentication with proper password hashing using BCrypt.

## Changes Made

### 1. UI Updates

#### Signin Page (`UI/Pages/Auth/Signin.razor`)
- Added password input field with show/hide toggle
- Integrated with `AuthFacade` for authentication
- Added error message display
- Added loading state during authentication
- Redirects to appropriate dashboard based on user role (Patient/Doctor/Admin)

#### Signup Page (`UI/Pages/Auth/Signup.razor`)
- Added password and confirm password fields with show/hide toggles
- Added role selector (Patient/Doctor)
- Integrated with `AuthFacade` for registration
- Added validation for password length (minimum 8 characters)
- Added password match validation
- Shows success message before redirecting
- Redirects to appropriate dashboard based on selected role

### 2. Backend Implementation

#### AuthFacade (`Services/AuthFacade.cs`)
Implements the **Facade Pattern** to coordinate authentication operations across multiple services:

**Services Coordinated:**
- `UserService` - User account management and password verification
- `PatientProfileService` - Patient profile creation
- `DoctorProfileService` - Doctor profile creation
- `ActivityLogService` - Activity logging for security audit

**Key Methods:**

1. **RegisterAsync(email, password, role, ipAddress)**
   - Validates email and password
   - Checks for existing accounts
   - Creates user with hashed password (BCrypt)
   - Creates corresponding profile (Patient or Doctor)
   - Logs registration activity
   - Returns `AuthResult` with success/failure status

2. **SignInAsync(email, password, ipAddress)**
   - Validates credentials
   - Authenticates user using BCrypt verification
   - Checks account status (active/deactivated)
   - Updates last login timestamp
   - Logs login activity (success or failure)
   - Returns `AuthResult` with user data

3. **SignOutAsync(userId, ipAddress)**
   - Logs logout activity

4. **GetUserWithProfileAsync(userId)**
   - Retrieves user with profile information

**AuthResult Class:**
- `IsSuccess` - Boolean indicating operation success
- `ErrorMessage` - Error description if failed
- `User` - User object if successful

### 3. Service Updates

#### DoctorProfileService
- Added `CreateAsync` method to create new doctor profiles

#### DependencyInjection
- Registered `AuthFacade` as a scoped service

## Security Features

1. **Password Hashing**: Uses BCrypt for secure password storage
2. **Activity Logging**: All authentication events are logged with IP addresses
3. **Account Status Checks**: Prevents login for deactivated accounts
4. **Failed Login Tracking**: Logs failed authentication attempts
5. **Input Validation**: Validates email and password requirements

## User Flow

### Registration
1. User enters email, password, and selects role (Patient/Doctor)
2. Frontend validates password length and match
3. `AuthFacade.RegisterAsync` is called
4. User account is created with hashed password
5. Profile is created based on role:
   - **Patient**: Basic profile with empty optional fields
   - **Doctor**: Profile with `IsActive=false` and `IsVerified=false` (requires admin verification)
6. Activity is logged
7. User is redirected to appropriate dashboard

### Sign In
1. User enters email and password
2. `AuthFacade.SignInAsync` is called
3. Password is verified using BCrypt
4. Account status is checked
5. Last login timestamp is updated
6. Activity is logged
7. User is redirected to role-specific dashboard

## Next Steps (TODO)

1. **Session Management**: Implement authentication cookies/JWT tokens
2. **Remember Me**: Add persistent login option
3. **Password Reset**: Implement forgot password flow
4. **Email Verification**: Add email verification for new accounts
5. **Two-Factor Authentication**: Optional 2FA for enhanced security
6. **Rate Limiting**: Prevent brute force attacks
7. **Doctor Verification**: Admin workflow to verify doctor credentials
8. **Profile Completion**: Prompt users to complete their profiles after registration

## Database Schema

The system uses the existing `users` table with:
- `password_hash` column for BCrypt hashed passwords
- `role` column for user type (Patient/Doctor/Admin)
- `is_active` and `is_deactivated` for account status
- `last_login_at` for tracking login activity

## Design Pattern: Facade

The `AuthFacade` implements the **Facade Pattern** by:
- Providing a simplified interface for complex authentication operations
- Coordinating multiple subsystems (User, Profile, Activity services)
- Encapsulating business logic and validation
- Reducing coupling between UI and service layers
- Making the authentication flow easier to maintain and test
