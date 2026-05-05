-- ============================================
-- Add Medical Document Fields to Documents Table
-- Extends documents table to support patient medical records
-- ============================================

-- Add new columns for medical document support
ALTER TABLE documents ADD COLUMN IF NOT EXISTS patient_id TEXT;
ALTER TABLE documents ADD COLUMN IF NOT EXISTS title TEXT;
ALTER TABLE documents ADD COLUMN IF NOT EXISTS document_type_string TEXT;
ALTER TABLE documents ADD COLUMN IF NOT EXISTS file_data BLOB;

-- Create index for patient_id lookups
CREATE INDEX IF NOT EXISTS idx_documents_patient_id ON documents(patient_id);

-- Make conversation_id nullable since medical documents may not be tied to conversations
-- Note: SQLite doesn't support ALTER COLUMN, so this is handled in the model layer
-- The application will allow NULL conversation_id for medical documents
