# Patient Settings Page Update

## Summary

Updated the `/patient/settings` page to focus on essential account settings only, removing unnecessary sections like Notifications, Appearance, and Connected Devices.

## Changes Made

### 1. Settings Page Simplification (`Pages/Patient/Settings.razor`)

**Removed Sections:**
- Notifications (appointment reminders, lab results, etc.)
- Appearance (theme, language, accessibility)
- Connected Devices (wearables integration)

**Kept Sections:**
- **Account Settings**
  - Email Address (with change button)
  
- **Privacy & Security**
  - Data Sharing (toggle)
  - AI Analysis (toggle)
  - Activity Tracking (toggle)
  - Download My Data (button)
  
- **Danger Zone**
  - Deactivate Account (button)
  - Delete Account (button)

**UI Changes:**
- Changed grid layout from 2 columns to single column (max-width: 900px)
- Removed device-related CSS styles
- Simplified responsive breakpoints

### 2. Database Migration (`supabase/migrations/20260417000001_add_user_settings_columns.sql`)

**New Columns Added to `users` Table:**

| Column Name | Type | Default | Description |
|------------|------|---------|-------------|
| `data_sharing_enabled` | BOOLEAN | FALSE | Data sharing preference |
| `ai_analysis_enabled` | BOOLEAN | TRUE | AI analysis permission |
| `activity_tracking_enabled` | BOOLEAN | TRUE | Activity tracking permission |
| `is_deactivated` | BOOLEAN | FALSE | Account deactivation status |
| `deactivated_at` | TIMESTAMP | NULL | Deactivation timestamp |

**Indexes Created:**
- `idx_users_is_active` - For active user queries
- `idx_users_is_deactivated` - For deactivated user queries

### 3. User Entity Update (`Core/Entities/User.cs`)

Added properties to match the new database columns:
- `DataSharingEnabled`
- `AiAnalysisEnabled`
- `ActivityTrackingEnabled`
- `IsDeactivated`
- `DeactivatedAt`

## How to Apply Migration

### Option 1: Supabase CLI
```bash
supabase db push
```

### Option 2: Supabase Dashboard
1. Go to SQL Editor in Supabase Dashboard
2. Copy contents from `supabase/migrations/20260417000001_add_user_settings_columns.sql`
3. Execute the SQL

### Option 3: Direct PostgreSQL
```bash
psql -h your-host -U postgres -d your-db -f supabase/migrations/20260417000001_add_user_settings_columns.sql
```

## Next Steps

To make the settings page fully functional, you'll need to:

1. **Create Service Methods** - Add methods to update user settings
2. **Add API Endpoints** - Create controller actions for settings updates
3. **Implement Frontend Logic** - Wire up buttons and toggles to API calls
4. **Add Validation** - Validate email changes
5. **Add Confirmation Dialogs** - For dangerous actions (deactivate/delete)
6. **Implement Data Export** - Create functionality to download user data

## Files Modified

- `Pages/Patient/Settings.razor` - Simplified UI
- `Core/Entities/User.cs` - Added new properties
- `supabase/migrations/20260417000001_add_user_settings_columns.sql` - New migration
- `supabase/migrations/README.md` - Migration documentation
