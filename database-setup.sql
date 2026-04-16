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
    phone VARCHAR(20),
    role user_role NOT NULL DEFAULT 'patient',
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    last_login_at TIMESTAMP WITH TIME ZONE
);

-- ============================================
-- GUEST CREDITS TABLE (Free AI Usage Tracking)
-- ============================================

CREATE TABLE guest_credits (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    ip_address INET NOT NULL UNIQUE,
    credits_used INTEGER DEFAULT 0,
    credits_limit INTEGER DEFAULT 10,
    first_access_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    last_access_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    is_blocked BOOLEAN DEFAULT FALSE,
    CONSTRAINT credits_positive CHECK (credits_used >= 0),
    CONSTRAINT credits_limit_valid CHECK (credits_limit > 0)
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
-- ORGANIZATIONS TABLE
-- ============================================

CREATE TABLE organizations (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(255) NOT NULL,
    description TEXT,
    address TEXT,
    phone VARCHAR(20),
    email VARCHAR(255),
    logo_url TEXT,
    is_verified BOOLEAN DEFAULT FALSE,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- ============================================
-- DOCTOR PROFILES (with detailed tags for AI filtering)
-- ============================================

CREATE TABLE doctor_profiles (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL UNIQUE REFERENCES users(id) ON DELETE CASCADE,
    organization_id UUID REFERENCES organizations(id) ON DELETE SET NULL,
    
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
    education JSONB, -- Array of degrees: [{"degree": "MD", "institution": "...", "year": 2010}]
    certifications JSONB, -- Array of certifications
    
    -- Availability
    availability_status doctor_availability_status DEFAULT 'offline',
    working_hours JSONB, -- {"monday": {"start": "09:00", "end": "17:00"}, ...}
    max_concurrent_patients INTEGER DEFAULT 5,
    current_active_conversations INTEGER DEFAULT 0,
    
    -- Performance Metrics
    total_consultations INTEGER DEFAULT 0,
    average_response_time_minutes DECIMAL(10,2),
    average_rating DECIMAL(3,2) DEFAULT 0.00,
    total_ratings INTEGER DEFAULT 0,
    
    -- Contact & Profile
    bio TEXT,
    profile_photo_url TEXT,
    consultation_fee DECIMAL(10,2),
    
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
    department VARCHAR(100),
    permissions JSONB DEFAULT '{"manage_users": true, "manage_doctors": true, "view_analytics": true}'::jsonb,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
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
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
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
    message_id UUID REFERENCES messages(id) ON DELETE SET NULL,
    
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
-- MEDICAL KNOWLEDGE BASE (For AI Training)
-- ============================================

CREATE TABLE medical_knowledge_base (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    title VARCHAR(500) NOT NULL,
    content TEXT NOT NULL,
    source VARCHAR(255),
    category VARCHAR(100),
    tags TEXT[],
    
    is_verified BOOLEAN DEFAULT FALSE,
    verified_by_doctor_id UUID REFERENCES users(id),
    
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
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

CREATE TRIGGER update_organizations_updated_at BEFORE UPDATE ON organizations
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