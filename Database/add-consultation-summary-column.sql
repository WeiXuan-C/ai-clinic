-- Migration: Add ai_generated_summary column to conversations table
-- Purpose: Store the medical summary generated during AI consultation for later doctor reference

-- Add column for storing AI-generated medical summary
ALTER TABLE conversations 
ADD COLUMN ai_generated_summary TEXT;

-- Add column for tracking which AI model was used
ALTER TABLE conversations 
ADD COLUMN ai_model_used VARCHAR(100);

-- Add comment
COMMENT ON COLUMN conversations.ai_generated_summary IS 'AI-generated medical summary for doctor reference when patient selects a doctor';
COMMENT ON COLUMN conversations.ai_model_used IS 'The AI model that was used for this conversation (from admin settings)';
