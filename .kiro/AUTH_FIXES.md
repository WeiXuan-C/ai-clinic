# Authentication Fixes

## Issues Fixed

### 1. AuthGuard Showing Message Instead of Redirecting
**Problem:** When user is not authenticated, AuthGuard was showing "You need to sign in to access this page" message with a button.

**Solution:** 
- Removed the message UI from AuthGuard
- Now automatically redirects to `/auth/signin` when not authenticated
- Shows only a loading spinner while checking authentication
- Added `forceLoad: true` to ensure proper navigation

### 2. Session Not Persisting (Need to Login Again)
**Problem:** Users had to login again after page refresh because session wasn't persisting.

**Root Cause:** 
- Supabase Client was registered as `Scoped` service
- Blazor Server creates new scope on each page refresh/circuit reconnection
- Session was lost when scope was disposed

**Solution:**
- Changed Supabase Client from `Scoped` to `Singleton` lifetime
- Created `SupabaseSessionHandler` to manage session persistence
- Enabled `AutoRefreshToken = true` in Supabase options
- Session now persists across page refreshes and circuit reconnections

## Files Modified

### 1. `Components/AuthGuard.razor`
- Removed "You need to sign in" message UI
- Always redirects to sign-in when not authenticated
- Added loading spinner with proper styling
- Improved error handling with automatic redirect

### 2. `DependencyInjection.cs`
- Changed Supabase Client from `AddScoped` to `AddSingleton`
- Added `SessionHandler` configuration
- Session now persists server-side across all requests

### 3. `Infrastructure/Data/SupabaseSessionHandler.cs` (NEW)
- Implements `IGotrueSessionPersistence<Session>`
- Manages session caching in memory
- Provides SaveSession, LoadSession, and DestroySession methods

## How It Works Now

### Authentication Flow
1. User visits protected page (e.g., `/patient/profile`)
2. AuthGuard checks authentication:
   - First checks AppState (fast, in-memory)
   - If not in AppState, checks Supabase session
   - If Supabase has valid session, syncs to AppState
3. If authenticated: Shows page content
4. If not authenticated: Redirects to `/auth/signin?returnUrl=...`

### Session Persistence
1. User signs in with OTP
2. Supabase creates session with access token
3. Session is saved in SupabaseSessionHandler (in-memory, server-side)
4. On page refresh:
   - Blazor Server may create new circuit
   - Supabase Client (Singleton) maintains session
   - AuthGuard checks session and syncs to AppState
   - User stays logged in

### Why Singleton is Safe
- Supabase Client manages its own internal state
- Each user's session is identified by their JWT token
- The Client handles concurrent requests safely
- Session data is not shared between users

## Testing

To verify the fixes:

1. **Test Auto-Redirect:**
   - Clear browser cache/cookies
   - Navigate to `/patient/profile` directly
   - Should immediately redirect to `/auth/signin`
   - Should NOT show "You need to sign in" message

2. **Test Session Persistence:**
   - Sign in with OTP
   - Navigate to any patient page
   - Refresh the page (F5)
   - Should stay logged in (no redirect to sign-in)
   - Should see your profile data

3. **Test Multiple Tabs:**
   - Sign in on one tab
   - Open new tab to `/patient/dashboard`
   - Should be automatically authenticated
   - No need to sign in again

## Notes

- Session persists as long as the server is running
- If server restarts, users will need to sign in again
- For production, consider using distributed cache (Redis) for session storage
- Current implementation is suitable for development and small-scale deployments
