-- ============================================
-- Add Doctor Settings Columns Migration (SQLite)
-- Adds missing columns for doctor profile settings
-- ============================================

-- Add auto_accept_appointments column (INTEGER for BOOLEAN)
ALTER TABLE doctor_profiles 
ADD COLUMN auto_accept_appointments INTEGER DEFAULT 0;

-- Add max_daily_patients column
ALTER TABLE doctor_profiles 
ADD COLUMN max_daily_patients INTEGER DEFAULT 30;

-- Add notification preference columns (INTEGER for BOOLEAN)
ALTER TABLE doctor_profiles 
ADD COLUMN notify_urgent_consultations INTEGER DEFAULT 1;

ALTER TABLE doctor_profiles 
ADD COLUMN notify_new_appointments INTEGER DEFAULT 1;

ALTER TABLE doctor_profiles 
ADD COLUMN notify_ai_assessments INTEGER DEFAULT 1;

ALTER TABLE doctor_profiles 
ADD COLUMN notify_email_summaries INTEGER DEFAULT 0;

-- Add session timeout column
ALTER TABLE doctor_profiles 
ADD COLUMN session_timeout_minutes INTEGER DEFAULT 30;

-- Add profile_photo BLOB column for SQLite
ALTER TABLE doctor_profiles 
ADD COLUMN profile_photo BLOB;

-- Update existing records to have default values
UPDATE doctor_profiles
SET 
    auto_accept_appointments = 0,
    max_daily_patients = 30,
    notify_urgent_consultations = 1,
    notify_new_appointments = 1,
    notify_ai_assessments = 1,
    notify_email_summaries = 0,
    session_timeout_minutes = 30
WHERE auto_accept_appointments IS NULL;
