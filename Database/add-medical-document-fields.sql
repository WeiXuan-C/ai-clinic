-- ============================================
-- Add Medical Document Fields to Documents Table (SQLite)
-- Extends documents table to support patient medical records
-- ============================================

-- Add new columns for medical document support
ALTER TABLE documents ADD COLUMN patient_id TEXT;
ALTER TABLE documents ADD COLUMN title TEXT;
ALTER TABLE documents ADD COLUMN document_type_string TEXT;
ALTER TABLE documents ADD COLUMN file_data BLOB;

-- Create index for patient_id lookups
CREATE INDEX IF NOT EXISTS idx_documents_patient_id ON documents(patient_id);

-- Note: conversation_id is already nullable in the main schema
-- The application will allow NULL conversation_id for medical documents
