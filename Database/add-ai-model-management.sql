-- Migration: Add AI Model Management Features
-- Description: Adds fields to manage multiple AI models and their availability for patients
-- Date: 2026-06-17

-- Add new columns to ai_assistant_settings table
ALTER TABLE ai_assistant_settings 
ADD COLUMN model_type INTEGER NOT NULL DEFAULT 0,
ADD COLUMN is_available_for_patients BOOLEAN NOT NULL DEFAULT TRUE,
ADD COLUMN display_order INTEGER NOT NULL DEFAULT 0,
ADD COLUMN description TEXT;

-- Add index for better query performance
CREATE INDEX IF NOT EXISTS idx_ai_assistant_settings_available 
ON ai_assistant_settings(is_available_for_patients, is_active, display_order);

-- Add current AI model type to conversations table
ALTER TABLE conversations 
ADD COLUMN current_ai_model_type INTEGER;

-- Insert default AI models if they don't exist
INSERT INTO ai_assistant_settings (id, model_name, model_type, is_active, is_available_for_patients, display_order, description, system_prompt, created_at, updated_at)
VALUES 
    (
        lower(hex(randomblob(4))) || '-' || lower(hex(randomblob(2))) || '-4' || substr(lower(hex(randomblob(2))),2) || '-' || substr('89ab',abs(random()) % 4 + 1, 1) || substr(lower(hex(randomblob(2))),2) || '-' || lower(hex(randomblob(6))),
        'Gemma 4',
        0,
        TRUE,
        TRUE,
        1,
        'Google Gemma 4 - Advanced medical analysis and consultation',
        'You are a professional medical AI assistant powered by Gemma 4. Provide accurate, helpful, and empathetic medical guidance.',
        datetime('now'),
        datetime('now')
    ),
    (
        lower(hex(randomblob(4))) || '-' || lower(hex(randomblob(2))) || '-4' || substr(lower(hex(randomblob(2))),2) || '-' || substr('89ab',abs(random()) % 4 + 1, 1) || substr(lower(hex(randomblob(2))),2) || '-' || lower(hex(randomblob(6))),
        'MiniMax',
        1,
        TRUE,
        TRUE,
        2,
        'MiniMax - Efficient and fast medical consultations',
        'You are a professional medical AI assistant powered by MiniMax. Provide quick, accurate, and helpful medical guidance.',
        datetime('now'),
        datetime('now')
    ),
    (
        lower(hex(randomblob(4))) || '-' || lower(hex(randomblob(2))) || '-4' || substr(lower(hex(randomblob(2))),2) || '-' || substr('89ab',abs(random()) % 4 + 1, 1) || substr(lower(hex(randomblob(2))),2) || '-' || lower(hex(randomblob(6))),
        'Nemotron',
        2,
        TRUE,
        TRUE,
        3,
        'NVIDIA Nemotron - Specialized medical diagnostics',
        'You are a professional medical AI assistant powered by Nemotron. Provide specialized diagnostic support and medical guidance.',
        datetime('now'),
        datetime('now')
    ),
    (
        lower(hex(randomblob(4))) || '-' || lower(hex(randomblob(2))) || '-4' || substr(lower(hex(randomblob(2))),2) || '-' || substr('89ab',abs(random()) % 4 + 1, 1) || substr(lower(hex(randomblob(2))),2) || '-' || lower(hex(randomblob(6))),
        'Owlapha',
        3,
        TRUE,
        TRUE,
        4,
        'Owlapha - Comprehensive medical knowledge assistant',
        'You are a professional medical AI assistant powered by Owlapha. Provide comprehensive medical knowledge and patient guidance.',
        datetime('now'),
        datetime('now')
    )
ON CONFLICT(model_name) DO NOTHING;

-- Update existing conversations to use Gemma4 as default if ai_model_used is set
UPDATE conversations 
SET current_ai_model_type = 0 
WHERE ai_model_used IS NOT NULL AND current_ai_model_type IS NULL;

COMMIT;
