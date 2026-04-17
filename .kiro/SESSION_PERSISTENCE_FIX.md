# Session Persistence Fix

## Problem
Users had to login again after every page refresh because session data was not persisted in browser storage.

## Root Cause
- Blazor Server maintains state on the server in a SignalR circuit
- When page refreshes, the circuit is lost and all server-side state is cleared
- AppState (Singleton) was cleared on circuit disconnect
- Supabase session was not being saved to browser storage

## Solution
Implemented browser localStorage persistence for authentication data.

### Architecture

```
User Login
    ↓
AuthController.VerifyOtpAsync()
    ↓
Save to AppState (server-side)
    ↓
Save to localStorage (browser-side)
    ├─ auth_token
    └─ user_data (JSON)

Page Refresh
    ↓
AuthGuard.CheckAuthentication()
    ↓
AuthController.CheckAndSyncAuthenticationAsync()
    ↓
Load from localStorage
    ↓
Restore to AppState
    ↓
User stays logged in ✅
```

## Files Created

### 1. `wwwroot/js/sessionStorage.js`
JavaScript helper for localStorage operations:
- `setItem(key, value)` - Save data
- `getItem(key)` - Retrieve data
- `removeItem(key)` - Delete data
- `clear()` - Clear all data

### 2. `Presentation/Services/BrowserStorageService.cs`
C# service that wraps JavaScript localStorage via JSInterop:
- Provides async methods for storage operations
- Handles errors gracefully
- Used by AuthController

## Files Modified

### 1. `Presentation/Controllers/AuthController.cs`
**Added:**
- `BrowserStorageService` dependency injection
- Constants for storage keys: `AUTH_TOKEN_KEY`, `USER_DATA_KEY`

**Updated Methods:**
- `VerifyOtpAsync()` - Saves token and user data to localStorage after successful login
- `LogoutAsync()` - Clears localStorage on logout
- `CheckAndSyncAuthenticationAsync()` - Loads from localStorage first, then checks Supabase

**Flow:**
1. Check localStorage for saved session
2. If found, restore to AppState
3. If not found, check Supabase session
4. If Supabase has session, save to localStorage
5. If no session anywhere, redirect to login

### 2. `DependencyInjection.cs`
- Registered `BrowserStorageService` as Scoped service

### 3. `Components/App.razor`
- Added `<script src="js/sessionStorage.js"></script>` to load storage helper

## How It Works

### Login Flow
1. User enters email and OTP
2. `AuthController.VerifyOtpAsync()` is called
3. Supabase verifies OTP and returns session
4. Session token and user data are saved to:
   - AppState (server-side, in-memory)
   - localStorage (browser-side, persistent)
5. User is logged in

### Page Refresh Flow
1. Page refreshes, Blazor circuit reconnects
2. `AuthGuard` checks authentication
3. `CheckAndSyncAuthenticationAsync()` is called
4. Checks localStorage for saved session
5. If found:
   - Deserializes user data
   - Restores to AppState
   - User stays logged in ✅
6. If not found:
   - Checks Supabase session
   - If valid, saves to localStorage
   - If invalid, redirects to login

### Logout Flow
1. User clicks logout
2. `AuthController.LogoutAsync()` is called
3. Clears:
   - Supabase session
   - AppState
   - localStorage
4. User is logged out

## Storage Keys

| Key | Value | Description |
|-----|-------|-------------|
| `auth_token` | JWT string | Supabase access token |
| `user_data` | JSON string | Serialized UserDto object |

## Security Considerations

### localStorage vs sessionStorage
- Using **localStorage** (persists across browser sessions)
- Alternative: Use **sessionStorage** (clears when browser closes)
- To use sessionStorage, change `localStorage` to `sessionStorage` in `sessionStorage.js`

### Token Security
- Tokens are stored in browser localStorage
- Vulnerable to XSS attacks
- Mitigation:
  - Sanitize all user inputs
  - Use Content Security Policy (CSP)
  - Set short token expiration times
  - Use HttpOnly cookies for production (requires server-side implementation)

### Best Practices for Production
1. **Use HttpOnly Cookies** instead of localStorage
2. **Implement CSRF protection**
3. **Use secure, SameSite cookies**
4. **Implement token refresh mechanism**
5. **Add session timeout**
6. **Log all authentication events**

## Testing

### Test Session Persistence
1. Sign in with OTP
2. Navigate to any page
3. Refresh the page (F5)
4. ✅ Should stay logged in
5. Check localStorage in DevTools:
   - Should see `auth_token` and `user_data`

### Test Logout
1. Click logout
2. Check localStorage in DevTools:
   - Should NOT see `auth_token` or `user_data`
3. Try to access protected page
4. ✅ Should redirect to sign-in

### Test Multiple Tabs
1. Sign in on Tab 1
2. Open Tab 2 to same site
3. ✅ Should be automatically logged in on Tab 2
4. Logout on Tab 1
5. Refresh Tab 2
6. ✅ Should redirect to sign-in (session cleared)

## Browser Compatibility
- localStorage is supported in all modern browsers
- IE 8+ support
- 5-10MB storage limit per domain

## Future Enhancements

1. **Token Refresh**
   - Implement automatic token refresh before expiration
   - Use Supabase `AutoRefreshToken` feature

2. **Session Timeout**
   - Add inactivity timeout
   - Clear session after X minutes of inactivity

3. **Remember Me**
   - Add checkbox to persist session longer
   - Use different expiration times

4. **Multi-Device Logout**
   - Implement server-side session management
   - Allow users to logout from all devices

5. **Secure Cookies (Production)**
   - Move from localStorage to HttpOnly cookies
   - Implement server-side session management
   - Add CSRF protection
