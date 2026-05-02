-- ============================================
-- AI CLINIC DATABASE SCHEMA
-- Supabase PostgreSQL Database Setup
-- ============================================

-- Enable required extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- ============================================
-- ENUMS
-- ============================================

CREATE TYPE user_role AS ENUM ('patient', 'doctor', 'admin');
CREATE TYPE conversation_status AS ENUM ('active', 'closed', 'archived', 'deactive');
CREATE TYPE message_sender_type AS ENUM ('patient', 'doctor', 'ai');
CREATE TYPE doctor_availability_status AS ENUM ('available', 'busy', 'offline');
CREATE TYPE document_type AS ENUM ('medical_record', 'lab_result', 'prescription', 'image', 'other');

-- ============================================
-- USERS TABLE (Core Authentication)
-- ============================================

CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    email VARCHAR(255) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    phone VARCHAR(20),
    role user_role NOT NULL DEFAULT 'patient',
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    last_login_at TIMESTAMP WITH TIME ZONE,
    data_sharing_enabled BOOLEAN DEFAULT FALSE,
    ai_analysis_enabled BOOLEAN DEFAULT TRUE,
    activity_tracking_enabled BOOLEAN DEFAULT TRUE,
    is_deactivated BOOLEAN DEFAULT FALSE,
    deactivated_at TIMESTAMP WITH TIME ZONE
);

-- ============================================
-- PATIENT PROFILES
-- ============================================

CREATE TABLE patient_profiles (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL UNIQUE REFERENCES users(id) ON DELETE CASCADE,
    full_name VARCHAR(255),
    date_of_birth DATE,
    gender VARCHAR(20),
    address TEXT,
    emergency_contact_name VARCHAR(255),
    emergency_contact_phone VARCHAR(20),
    blood_type VARCHAR(5),
    allergies TEXT[],
    chronic_conditions TEXT[],
    current_medications TEXT[],
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- ============================================
-- DOCTOR PROFILES (with detailed tags for AI filtering)
-- ============================================

CREATE TABLE doctor_profiles (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL UNIQUE REFERENCES users(id) ON DELETE CASCADE,    
    -- Basic Information
    full_name VARCHAR(255) NOT NULL,
    title VARCHAR(100), -- Dr., Prof., etc.
    license_number VARCHAR(100) UNIQUE NOT NULL,
    
    -- Specialization (Primary)
    primary_specialization VARCHAR(255) NOT NULL,
    sub_specializations TEXT[], -- Array of sub-specialties
    
    -- Detailed AI Filtering Tags
    medical_expertise_tags TEXT[], -- e.g., ['cardiology', 'hypertension', 'heart_failure']
    symptoms_expertise TEXT[], -- e.g., ['chest_pain', 'shortness_of_breath', 'palpitations']
    conditions_treated TEXT[], -- e.g., ['diabetes', 'asthma', 'arthritis']
    procedures_performed TEXT[], -- e.g., ['ecg', 'ultrasound', 'minor_surgery']
    age_groups_treated TEXT[], -- e.g., ['pediatric', 'adult', 'geriatric']
    languages_spoken TEXT[], -- e.g., ['english', 'spanish', 'mandarin']
    
    -- Experience & Qualifications
    years_of_experience INTEGER,    
    -- Availability
    availability_status doctor_availability_status DEFAULT 'offline',
    working_hours JSONB, -- {"monday": {"start": "09:00", "end": "17:00"}, ...}
    current_active_conversations INTEGER DEFAULT 0,
    
    -- Performance Metrics
    total_consultations INTEGER DEFAULT 0,
    average_rating DECIMAL(3,2) DEFAULT 0.00,
    total_ratings INTEGER DEFAULT 0,
    
    -- Contact & Profile
    profile_photo_url TEXT,
    
    -- Status
    is_verified BOOLEAN DEFAULT FALSE,
    is_active BOOLEAN DEFAULT TRUE,
    is_accepting_patients BOOLEAN DEFAULT TRUE,
    
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    
    CONSTRAINT rating_range CHECK (average_rating >= 0 AND average_rating <= 5),
    CONSTRAINT experience_positive CHECK (years_of_experience >= 0)
);

-- ============================================
-- ADMIN PROFILES
-- ============================================

CREATE TABLE admin_profiles (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL UNIQUE REFERENCES users(id) ON DELETE CASCADE,
    full_name VARCHAR(255) NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    manage_users BOOLEAN DEFAULT FALSE,
    manage_ai BOOLEAN DEFAULT FALSE,
    manage_doctors BOOLEAN DEFAULT FALSE,
    manage_tickets BOOLEAN DEFAULT FALSE,
    manage_permissions BOOLEAN DEFAULT FALSE
);

-- ============================================
-- CONVERSATIONS TABLE
-- ============================================

CREATE TABLE conversations (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    patient_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    assigned_doctor_id UUID REFERENCES users(id) ON DELETE SET NULL,
    
    title VARCHAR(255),
    status conversation_status DEFAULT 'active',
    
    -- AI Context
    initial_symptoms TEXT[],
    ai_suggested_specialization VARCHAR(255),
    
    -- Metadata
    started_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    closed_at TIMESTAMP WITH TIME ZONE,
    last_message_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    
    -- Tracking
    total_messages INTEGER DEFAULT 0,
    ai_messages_count INTEGER DEFAULT 0,
    doctor_messages_count INTEGER DEFAULT 0,
    
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),

    consultation_status VARCHAR(50) DEFAULT 'pending',
    diagnosis_completed BOOLEAN DEFAULT FALSE,
    prescription_generated BOOLEAN DEFAULT FALSE,
    required_specialization VARCHAR(255),
    ai_confidence_score DECIMAL(5,4)
);

-- ============================================
-- MESSAGES TABLE
-- ============================================

CREATE TABLE messages (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    conversation_id UUID NOT NULL REFERENCES conversations(id) ON DELETE CASCADE,
    sender_id UUID REFERENCES users(id) ON DELETE SET NULL,
    sender_type message_sender_type NOT NULL,
    
    content TEXT NOT NULL,
    
    -- AI specific fields
    ai_model_used VARCHAR(100),
    ai_confidence_score DECIMAL(5,4),
    document_references UUID[], -- Array of document IDs used for AI response
    
    -- Metadata
    is_read BOOLEAN DEFAULT FALSE,
    read_at TIMESTAMP WITH TIME ZONE,
    
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    
    CONSTRAINT content_not_empty CHECK (LENGTH(TRIM(content)) > 0)
);

-- ============================================
-- DOCUMENTS TABLE (Patient uploads in chatroom)
-- ============================================

CREATE TABLE documents (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    conversation_id UUID NOT NULL REFERENCES conversations(id) ON DELETE CASCADE,
    uploaded_by_user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    
    file_name VARCHAR(255) NOT NULL,
    file_type document_type NOT NULL,
    file_size_bytes BIGINT NOT NULL,
    file_url TEXT NOT NULL,
    mime_type VARCHAR(100),
    
    -- AI Processing
    is_processed BOOLEAN DEFAULT FALSE,
    extracted_text TEXT,
    
    -- Metadata
    description TEXT,
    tags TEXT[],
    
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    CONSTRAINT file_size_positive CHECK (file_size_bytes > 0)
);

-- ============================================
-- DOCTOR RATINGS & REVIEWS
-- ============================================

CREATE TABLE doctor_ratings (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    doctor_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    patient_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    conversation_id UUID NOT NULL REFERENCES conversations(id) ON DELETE CASCADE,
    
    rating INTEGER NOT NULL,
    review_text TEXT,
    
    -- Rating categories
    professionalism_rating INTEGER,
    communication_rating INTEGER,
    knowledge_rating INTEGER,
    response_time_rating INTEGER,
    
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    
    CONSTRAINT rating_value CHECK (rating >= 1 AND rating <= 5),
    CONSTRAINT unique_rating_per_conversation UNIQUE(conversation_id, patient_id)
);

-- ============================================
-- ACTIVITY LOGS (Audit Trail)
-- ============================================

CREATE TABLE activity_logs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID REFERENCES users(id) ON DELETE SET NULL,
    action VARCHAR(100) NOT NULL,
    entity_type VARCHAR(50),
    entity_id UUID,
    ip_address INET,
    user_agent TEXT,
    details JSONB,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- ============================================
-- SUPPORT TICKETS TABLE
-- ============================================

CREATE TABLE IF NOT EXISTS support_tickets (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    subject VARCHAR(500) NOT NULL,
    description TEXT NOT NULL,
    category VARCHAR(100), -- 'technical', 'billing', 'medical', 'account', 'other'
    priority VARCHAR(50) DEFAULT 'medium', -- 'low', 'medium', 'high', 'urgent'
    status VARCHAR(50) DEFAULT 'open', -- 'open', 'in_progress', 'resolved', 'closed'
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    resolved_at TIMESTAMP WITH TIME ZONE,
    closed_at TIMESTAMP WITH TIME ZONE,
    CONSTRAINT valid_priority CHECK (priority IN ('low', 'medium', 'high', 'urgent')),
    CONSTRAINT valid_status CHECK (status IN ('open', 'in_progress', 'resolved', 'closed'))
);

-- ============================================
-- SUPPORT TICKET ATTACHMENTS
-- ============================================

CREATE TABLE IF NOT EXISTS support_ticket_attachments (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    ticket_id UUID NOT NULL REFERENCES support_tickets(id) ON DELETE CASCADE,
    file_name VARCHAR(255) NOT NULL,
    file_url TEXT NOT NULL,
    file_size_bytes BIGINT NOT NULL,
    mime_type VARCHAR(100),
    uploaded_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    CONSTRAINT file_size_positive CHECK (file_size_bytes > 0)
);

-- ============================================
-- SUPPORT TICKET RESPONSES
-- ============================================

CREATE TABLE IF NOT EXISTS support_ticket_responses (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    ticket_id UUID NOT NULL REFERENCES support_tickets(id) ON DELETE CASCADE,
    responder_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    message TEXT NOT NULL,
    is_internal_note BOOLEAN DEFAULT FALSE, -- Admin-only notes
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    CONSTRAINT message_not_empty CHECK (LENGTH(TRIM(message)) > 0)
);

-- ============================================
-- MEDICAL RECORDS TABLE (Patient's medical history)
-- ============================================

CREATE TABLE IF NOT EXISTS medical_records (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    patient_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    conversation_id UUID REFERENCES conversations(id) ON DELETE SET NULL,
    created_by_doctor_id UUID REFERENCES users(id) ON DELETE SET NULL,
    record_type VARCHAR(100) NOT NULL, -- 'diagnosis', 'prescription', 'lab_result', 'consultation_note', 'other'
    title VARCHAR(500) NOT NULL,
    content TEXT NOT NULL,
    diagnosis_code VARCHAR(50), -- ICD-10 code
    diagnosis_description TEXT,
    medications JSONB, -- [{"name": "...", "dosage": "...", "frequency": "...", "duration": "..."}]
    record_date DATE NOT NULL DEFAULT CURRENT_DATE,
    is_exported BOOLEAN DEFAULT FALSE,
    export_count INTEGER DEFAULT 0,
    last_exported_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- ============================================
-- CONSULTATION NOTES (Doctor's notes during consultation)
-- ============================================

CREATE TABLE IF NOT EXISTS consultation_notes (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    conversation_id UUID NOT NULL REFERENCES conversations(id) ON DELETE CASCADE,
    doctor_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    patient_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    symptoms TEXT[],
    physical_examination TEXT,
    diagnosis TEXT NOT NULL,
    treatment_plan TEXT,
    follow_up_instructions TEXT,
    prescription_id UUID REFERENCES medical_records(id) ON DELETE SET NULL,
    is_finalized BOOLEAN DEFAULT FALSE,
    finalized_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- ============================================
-- PRESCRIPTIONS TABLE (Separate detailed prescriptions)
-- ============================================

CREATE TABLE IF NOT EXISTS prescriptions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    consultation_note_id UUID REFERENCES consultation_notes(id) ON DELETE CASCADE,
    patient_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    doctor_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    medication_name VARCHAR(255) NOT NULL,
    dosage VARCHAR(100) NOT NULL,
    frequency VARCHAR(100) NOT NULL, -- 'once daily', 'twice daily', 'as needed', etc.
    duration VARCHAR(100), -- '7 days', '2 weeks', 'ongoing'
    instructions TEXT,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- ============================================
-- AI ASSISTANT SETTINGS (Admin configurable)
-- ============================================

CREATE TABLE IF NOT EXISTS ai_assistant_settings (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    model_name VARCHAR(100) NOT NULL, -- 'gpt-4', 'claude-3', etc.
    is_active BOOLEAN DEFAULT TRUE,
    system_prompt TEXT,
    enable_document_analysis BOOLEAN DEFAULT TRUE,
    enable_symptom_checker BOOLEAN DEFAULT TRUE,
    enable_doctor_recommendation BOOLEAN DEFAULT TRUE,
    created_by_admin_id UUID REFERENCES users(id) ON DELETE SET NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
);

-- ============================================
-- USER SUSPENSION LOG
-- ============================================

CREATE TABLE IF NOT EXISTS user_suspensions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    suspended_by_admin_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    reason TEXT NOT NULL,
    suspension_start TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    suspension_end TIMESTAMP WITH TIME ZONE, -- NULL for indefinite
    is_active BOOLEAN DEFAULT TRUE,
    lifted_at TIMESTAMP WITH TIME ZONE,
    lifted_by_admin_id UUID REFERENCES users(id) ON DELETE SET NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- ============================================
-- TRIGGERS FOR UPDATED_AT
-- ============================================

CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER update_users_updated_at BEFORE UPDATE ON users
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_patient_profiles_updated_at BEFORE UPDATE ON patient_profiles
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_doctor_profiles_updated_at BEFORE UPDATE ON doctor_profiles
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_admin_profiles_updated_at BEFORE UPDATE ON admin_profiles
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_conversations_updated_at BEFORE UPDATE ON conversations
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- ============================================
-- FUNCTION: Update conversation last_message_at
-- ============================================

CREATE OR REPLACE FUNCTION update_conversation_last_message()
RETURNS TRIGGER AS $$
BEGIN
    UPDATE conversations
    SET last_message_at = NEW.created_at,
        total_messages = total_messages + 1,
        ai_messages_count = CASE WHEN NEW.sender_type = 'ai' THEN ai_messages_count + 1 ELSE ai_messages_count END,
        doctor_messages_count = CASE WHEN NEW.sender_type = 'doctor' THEN doctor_messages_count + 1 ELSE doctor_messages_count END
    WHERE id = NEW.conversation_id;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_update_conversation_last_message
    AFTER INSERT ON messages
    FOR EACH ROW EXECUTE FUNCTION update_conversation_last_message();

-- ============================================
-- FUNCTION: Update doctor rating average
-- ============================================

CREATE OR REPLACE FUNCTION update_doctor_rating_average()
RETURNS TRIGGER AS $$
BEGIN
    UPDATE doctor_profiles
    SET average_rating = (
            SELECT AVG(rating)::DECIMAL(3,2)
            FROM doctor_ratings
            WHERE doctor_id = NEW.doctor_id
        ),
        total_ratings = (
            SELECT COUNT(*)
            FROM doctor_ratings
            WHERE doctor_id = NEW.doctor_id
        )
    WHERE user_id = NEW.doctor_id;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_update_doctor_rating
    AFTER INSERT OR UPDATE ON doctor_ratings
    FOR EACH ROW EXECUTE FUNCTION update_doctor_rating_average();


-- ============================================
-- TRIGGERS FOR UPDATED_AT
-- ============================================

CREATE TRIGGER update_support_tickets_updated_at BEFORE UPDATE ON support_tickets
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_medical_records_updated_at BEFORE UPDATE ON medical_records
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_consultation_notes_updated_at BEFORE UPDATE ON consultation_notes
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_prescriptions_updated_at BEFORE UPDATE ON prescriptions
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_doctor_availability_updated_at BEFORE UPDATE ON doctor_availability
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_ai_assistant_settings_updated_at BEFORE UPDATE ON ai_assistant_settings
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- ============================================
-- FUNCTION: Update medical record export count
-- ============================================

CREATE OR REPLACE FUNCTION update_medical_record_export()
RETURNS TRIGGER AS $$
BEGIN
    -- Update export count for all records in the export
    UPDATE medical_records
    SET export_count = export_count + 1,
        last_exported_at = NEW.created_at,
        is_exported = TRUE
    WHERE id = ANY(NEW.records_included);
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_update_medical_record_export
    AFTER INSERT ON document_exports
    FOR EACH ROW EXECUTE FUNCTION update_medical_record_export();

-- ============================================
-- FUNCTION: Update conversation consultation status
-- ============================================

CREATE OR REPLACE FUNCTION update_conversation_consultation_status()
RETURNS TRIGGER AS $$
BEGIN
    UPDATE conversations
    SET diagnosis_completed = TRUE,
        consultation_status = 'completed'
    WHERE id = NEW.conversation_id;
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_update_conversation_status
    AFTER INSERT ON consultation_notes
    FOR EACH ROW 
    WHEN (NEW.is_finalized = TRUE)
    EXECUTE FUNCTION update_conversation_consultation_status();

-- ============================================
-- FUNCTION: Track active doctor conversations
-- ============================================

CREATE OR REPLACE FUNCTION update_doctor_active_conversations()
RETURNS TRIGGER AS $$
BEGIN
    IF TG_OP = 'INSERT' OR (TG_OP = 'UPDATE' AND NEW.assigned_doctor_id IS NOT NULL AND OLD.assigned_doctor_id IS NULL) THEN
        -- Increment when doctor is assigned
        UPDATE doctor_profiles
        SET current_active_conversations = current_active_conversations + 1
        WHERE user_id = NEW.assigned_doctor_id;
    ELSIF TG_OP = 'UPDATE' AND NEW.status IN ('closed', 'archived') AND OLD.status NOT IN ('closed', 'archived') THEN
        -- Decrement when conversation is closed
        UPDATE doctor_profiles
        SET current_active_conversations = GREATEST(current_active_conversations - 1, 0)
        WHERE user_id = NEW.assigned_doctor_id;
    END IF;
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_update_doctor_conversations
    AFTER INSERT OR UPDATE ON conversations
    FOR EACH ROW EXECUTE FUNCTION update_doctor_active_conversations();