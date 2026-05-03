-- Add profile_photo column to patient_profiles table
-- SQLite Migration Script

ALTER TABLE patient_profiles ADD COLUMN profile_photo BLOB;
