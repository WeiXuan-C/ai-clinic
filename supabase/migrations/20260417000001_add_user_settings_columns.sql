-- ============================================
-- MIGRATION: Add User Settings Columns
-- Created: 2026-04-17
-- Description: Add columns for user privacy preferences and account status
-- ============================================

-- Add privacy and security settings
ALTER TABLE users
ADD COLUMN IF NOT EXISTS data_sharing_enabled BOOLEAN DEFAULT FALSE,
ADD COLUMN IF NOT EXISTS ai_analysis_enabled BOOLEAN DEFAULT TRUE,
ADD COLUMN IF NOT EXISTS activity_tracking_enabled BOOLEAN DEFAULT TRUE;

-- Add account status for deactivation
ALTER TABLE users
ADD COLUMN IF NOT EXISTS is_deactivated BOOLEAN DEFAULT FALSE,
ADD COLUMN IF NOT EXISTS deactivated_at TIMESTAMP WITH TIME ZONE;
