-- ============================================
-- AI CLINIC DATABASE SCHEMA
-- SQLite Database Setup
-- ============================================

-- SQLite does not support extensions or ENUM types
-- ENUM types are replaced with TEXT columns with CHECK constraints
-- Arrays are stored as JSON TEXT
-- BOOLEAN is replaced with INTEGER (0 = false, 1 = true)
-- UUID is replaced with TEXT using randomblob
-- TIMESTAMP WITH TIME ZONE is replaced with TEXT using datetime('now')

-- ============================================
-- USERS TABLE (Core Authentication)
-- ============================================

CREATE TABLE users (
    id TEXT PRIMARY KEY DEFAULT (lower(hex(randomblob(16)))),
    email TEXT UNIQUE NOT NULL,
    password_hash TEXT NOT NULL,
    phone TEXT,
    role TEXT NOT NULL DEFAULT 'patient' CHECK (role IN ('patient', 'doctor', 'admin')),
    is_active INTEGER DEFAULT 1,
    created_at TEXT DEFAULT (datetime('now')),
    updated_at TEXT DEFAULT (datetime('now')),
    last_login_at TEXT,
    data_sharing_enabled INTEGER DEFAULT 0,
    ai_analysis_enabled INTEGER DEFAULT 1,
    activity_tracking_enabled INTEGER DEFAULT 1,
    is_deactivated INTEGER DEFAULT 0,
    deactivated_at TEXT
);

-- ============================================
-- PATIENT PROFILES
-- ============================================

CREATE TABLE patient_profiles (
    id TEXT PRIMARY KEY DEFAULT (lower(hex(randomblob(16)))),
    user_id TEXT NOT NULL UNIQUE REFERENCES users(id) ON DELETE CASCADE,
    full_name TEXT,
    date_of_birth TEXT,
    gender TEXT,
    address TEXT,
    emergency_contact_name TEXT,
    emergency_contact_phone TEXT,
    blood_type TEXT,
    allergies TEXT, -- JSON array stored as TEXT
    chronic_conditions TEXT, -- JSON array stored as TEXT
    current_medications TEXT, -- JSON array stored as TEXT
    profile_photo BLOB, -- Profile photo stored as BLOB
    created_at TEXT DEFAULT (datetime('now')),
    updated_at TEXT DEFAULT (datetime('now'))
);

-- ============================================
-- DOCTOR PROFILES (with detailed tags for AI filtering)
-- ============================================

CREATE TABLE doctor_profiles (
    id TEXT PRIMARY KEY DEFAULT (lower(hex(randomblob(16)))),
    user_id TEXT NOT NULL UNIQUE REFERENCES users(id) ON DELETE CASCADE,    
    -- Basic Information
    full_name TEXT NOT NULL,
    title TEXT, -- Dr., Prof., etc.
    license_number TEXT UNIQUE NOT NULL,
    
    -- Specialization (Primary)
    primary_specialization TEXT NOT NULL,
    sub_specializations TEXT, -- JSON array stored as TEXT
    
    -- Detailed AI Filtering Tags
    medical_expertise_tags TEXT, -- JSON array stored as TEXT
    symptoms_expertise TEXT, -- JSON array stored as TEXT
    conditions_treated TEXT, -- JSON array stored as TEXT
    procedures_performed TEXT, -- JSON array stored as TEXT
    age_groups_treated TEXT, -- JSON array stored as TEXT
    languages_spoken TEXT, -- JSON array stored as TEXT
    
    -- Experience & Qualifications
    years_of_experience INTEGER,    
    -- Availability
    availability_status TEXT DEFAULT 'offline' CHECK (availability_status IN ('available', 'busy', 'offline')),
    working_hours TEXT, -- JSON stored as TEXT
    current_active_conversations INTEGER DEFAULT 0,
    
    -- Performance Metrics
    total_consultations INTEGER DEFAULT 0,
    average_rating REAL DEFAULT 0.00,
    total_ratings INTEGER DEFAULT 0,
    
    -- Contact & Profile
    profile_photo_url TEXT,
    profile_photo BLOB, -- Profile photo stored as BLOB
    
    -- Doctor Settings (from migration)
    auto_accept_appointments INTEGER DEFAULT 0,
    max_daily_patients INTEGER DEFAULT 30,
    notify_urgent_consultations INTEGER DEFAULT 1,
    notify_new_appointments INTEGER DEFAULT 1,
    notify_ai_assessments INTEGER DEFAULT 1,
    notify_email_summaries INTEGER DEFAULT 0,
    session_timeout_minutes INTEGER DEFAULT 30,
    
    -- Status
    is_verified INTEGER DEFAULT 0,
    is_active INTEGER DEFAULT 1,
    is_accepting_patients INTEGER DEFAULT 1,
    
    created_at TEXT DEFAULT (datetime('now')),
    updated_at TEXT DEFAULT (datetime('now')),
    
    CONSTRAINT rating_range CHECK (average_rating >= 0 AND average_rating <= 5),
    CONSTRAINT experience_positive CHECK (years_of_experience >= 0)
);

-- ============================================
-- ADMIN PROFILES
-- ============================================

CREATE TABLE admin_profiles (
    id TEXT PRIMARY KEY DEFAULT (lower(hex(randomblob(16)))),
    user_id TEXT NOT NULL UNIQUE REFERENCES users(id) ON DELETE CASCADE,
    full_name TEXT NOT NULL,
    created_at TEXT DEFAULT (datetime('now')),
    updated_at TEXT DEFAULT (datetime('now')),
    manage_users INTEGER DEFAULT 0,
    manage_ai INTEGER DEFAULT 0,
    manage_doctors INTEGER DEFAULT 0,
    manage_tickets INTEGER DEFAULT 0,
    manage_permissions INTEGER DEFAULT 0
);

-- ============================================
-- CONVERSATIONS TABLE
-- ============================================

CREATE TABLE conversations (
    id TEXT PRIMARY KEY DEFAULT (lower(hex(randomblob(16)))),
    patient_id TEXT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    assigned_doctor_id TEXT REFERENCES users(id) ON DELETE SET NULL,
    
    title TEXT,
    status TEXT DEFAULT 'active' CHECK (status IN ('active', 'closed', 'archived', 'deactive')),
    
    -- AI Context
    initial_symptoms TEXT, -- JSON array stored as TEXT
    ai_suggested_specialization TEXT,
    
    -- Metadata
    started_at TEXT DEFAULT (datetime('now')),
    closed_at TEXT,
    last_message_at TEXT DEFAULT (datetime('now')),
    
    -- Tracking
    total_messages INTEGER DEFAULT 0,
    ai_messages_count INTEGER DEFAULT 0,
    doctor_messages_count INTEGER DEFAULT 0,
    
    created_at TEXT DEFAULT (datetime('now')),
    updated_at TEXT DEFAULT (datetime('now')),

    consultation_status TEXT DEFAULT 'pending',
    diagnosis_completed INTEGER DEFAULT 0,
    prescription_generated INTEGER DEFAULT 0,
    required_specialization TEXT,
    ai_confidence_score REAL
);

-- ============================================
-- MESSAGES TABLE
-- ============================================

CREATE TABLE messages (
    id TEXT PRIMARY KEY DEFAULT (lower(hex(randomblob(16)))),
    conversation_id TEXT NOT NULL REFERENCES conversations(id) ON DELETE CASCADE,
    sender_id TEXT REFERENCES users(id) ON DELETE SET NULL,
    sender_type TEXT NOT NULL CHECK (sender_type IN ('patient', 'doctor', 'ai')),
    
    content TEXT NOT NULL,
    
    -- AI specific fields
    ai_model_used TEXT,
    ai_confidence_score REAL,
    document_references TEXT, -- JSON array stored as TEXT
    
    -- Metadata
    is_read INTEGER DEFAULT 0,
    read_at TEXT,
    
    created_at TEXT DEFAULT (datetime('now')),
    
    CONSTRAINT content_not_empty CHECK (LENGTH(TRIM(content)) > 0)
);

-- ============================================
-- DOCUMENTS TABLE (Patient uploads in chatroom)
-- ============================================

CREATE TABLE documents (
    id TEXT PRIMARY KEY DEFAULT (lower(hex(randomblob(16)))),
    conversation_id TEXT REFERENCES conversations(id) ON DELETE CASCADE,
    uploaded_by_user_id TEXT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    
    file_name TEXT NOT NULL,
    file_type TEXT NOT NULL CHECK (file_type IN ('medical_record', 'lab_result', 'prescription', 'image', 'other')),
    file_size_bytes INTEGER NOT NULL,
    file_url TEXT NOT NULL,
    mime_type TEXT,
    
    -- AI Processing
    is_processed INTEGER DEFAULT 0,
    extracted_text TEXT,
    
    -- Metadata
    description TEXT,
    tags TEXT, -- JSON array stored as TEXT
    
    -- Additional fields from migration
    patient_id TEXT,
    title TEXT,
    document_type_string TEXT,
    file_data BLOB,
    
    created_at TEXT DEFAULT (datetime('now')),
    CONSTRAINT file_size_positive CHECK (file_size_bytes > 0)
);

-- ============================================
-- DOCTOR RATINGS & REVIEWS
-- ============================================

CREATE TABLE doctor_ratings (
    id TEXT PRIMARY KEY DEFAULT (lower(hex(randomblob(16)))),
    doctor_id TEXT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    patient_id TEXT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    conversation_id TEXT NOT NULL REFERENCES conversations(id) ON DELETE CASCADE,
    
    rating INTEGER NOT NULL,
    review_text TEXT,
    
    -- Rating categories
    professionalism_rating INTEGER,
    communication_rating INTEGER,
    knowledge_rating INTEGER,
    response_time_rating INTEGER,
    
    created_at TEXT DEFAULT (datetime('now')),
    
    CONSTRAINT rating_value CHECK (rating >= 1 AND rating <= 5),
    CONSTRAINT unique_rating_per_conversation UNIQUE(conversation_id, patient_id)
);

-- ============================================
-- ACTIVITY LOGS (Audit Trail)
-- ============================================

CREATE TABLE activity_logs (
    id TEXT PRIMARY KEY DEFAULT (lower(hex(randomblob(16)))),
    user_id TEXT REFERENCES users(id) ON DELETE SET NULL,
    action TEXT NOT NULL,
    entity_type TEXT,
    entity_id TEXT,
    ip_address TEXT,
    user_agent TEXT,
    details TEXT, -- JSON stored as TEXT
    created_at TEXT DEFAULT (datetime('now'))
);

-- ============================================
-- SUPPORT TICKETS TABLE
-- ============================================

CREATE TABLE IF NOT EXISTS support_tickets (
    id TEXT PRIMARY KEY DEFAULT (lower(hex(randomblob(16)))),
    user_id TEXT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    subject TEXT NOT NULL,
    description TEXT NOT NULL,
    category TEXT, -- 'technical', 'billing', 'medical', 'account', 'other'
    priority TEXT DEFAULT 'medium', -- 'low', 'medium', 'high', 'urgent'
    status TEXT DEFAULT 'open', -- 'open', 'in_progress', 'resolved', 'closed'
    created_at TEXT DEFAULT (datetime('now')),
    updated_at TEXT DEFAULT (datetime('now')),
    resolved_at TEXT,
    closed_at TEXT,
    CONSTRAINT valid_priority CHECK (priority IN ('low', 'medium', 'high', 'urgent')),
    CONSTRAINT valid_status CHECK (status IN ('open', 'in_progress', 'resolved', 'closed'))
);

-- ============================================
-- SUPPORT TICKET ATTACHMENTS
-- ============================================

CREATE TABLE IF NOT EXISTS support_ticket_attachments (
    id TEXT PRIMARY KEY DEFAULT (lower(hex(randomblob(16)))),
    ticket_id TEXT NOT NULL REFERENCES support_tickets(id) ON DELETE CASCADE,
    file_name TEXT NOT NULL,
    file_url TEXT NOT NULL,
    file_size_bytes INTEGER NOT NULL,
    mime_type TEXT,
    uploaded_at TEXT DEFAULT (datetime('now')),
    CONSTRAINT file_size_positive CHECK (file_size_bytes > 0)
);

-- ============================================
-- SUPPORT TICKET RESPONSES
-- ============================================

CREATE TABLE IF NOT EXISTS support_ticket_responses (
    id TEXT PRIMARY KEY DEFAULT (lower(hex(randomblob(16)))),
    ticket_id TEXT NOT NULL REFERENCES support_tickets(id) ON DELETE CASCADE,
    responder_id TEXT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    message TEXT NOT NULL,
    is_internal_note INTEGER DEFAULT 0, -- Admin-only notes
    created_at TEXT DEFAULT (datetime('now')),
    CONSTRAINT message_not_empty CHECK (LENGTH(TRIM(message)) > 0)
);

-- ============================================
-- MEDICAL RECORDS TABLE (Patient's medical history)
-- ============================================

CREATE TABLE IF NOT EXISTS medical_records (
    id TEXT PRIMARY KEY DEFAULT (lower(hex(randomblob(16)))),
    patient_id TEXT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    conversation_id TEXT REFERENCES conversations(id) ON DELETE SET NULL,
    created_by_doctor_id TEXT REFERENCES users(id) ON DELETE SET NULL,
    record_type TEXT NOT NULL, -- 'diagnosis', 'prescription', 'lab_result', 'consultation_note', 'other'
    title TEXT NOT NULL,
    content TEXT NOT NULL,
    diagnosis_code TEXT, -- ICD-10 code
    diagnosis_description TEXT,
    medications TEXT, -- JSON stored as TEXT
    record_date TEXT NOT NULL DEFAULT (date('now')),
    is_exported INTEGER DEFAULT 0,
    export_count INTEGER DEFAULT 0,
    last_exported_at TEXT,
    created_at TEXT DEFAULT (datetime('now')),
    updated_at TEXT DEFAULT (datetime('now'))
);

-- ============================================
-- CONSULTATION NOTES (Doctor's notes during consultation)
-- ============================================

CREATE TABLE IF NOT EXISTS consultation_notes (
    id TEXT PRIMARY KEY DEFAULT (lower(hex(randomblob(16)))),
    conversation_id TEXT NOT NULL REFERENCES conversations(id) ON DELETE CASCADE,
    doctor_id TEXT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    patient_id TEXT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    symptoms TEXT, -- JSON array stored as TEXT
    physical_examination TEXT,
    diagnosis TEXT NOT NULL,
    treatment_plan TEXT,
    follow_up_instructions TEXT,
    prescription_id TEXT REFERENCES medical_records(id) ON DELETE SET NULL,
    is_finalized INTEGER DEFAULT 0,
    finalized_at TEXT,
    created_at TEXT DEFAULT (datetime('now')),
    updated_at TEXT DEFAULT (datetime('now'))
);

-- ============================================
-- PRESCRIPTIONS TABLE (Separate detailed prescriptions)
-- ============================================

CREATE TABLE IF NOT EXISTS prescriptions (
    id TEXT PRIMARY KEY DEFAULT (lower(hex(randomblob(16)))),
    consultation_note_id TEXT REFERENCES consultation_notes(id) ON DELETE CASCADE,
    patient_id TEXT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    doctor_id TEXT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    medication_name TEXT NOT NULL,
    dosage TEXT NOT NULL,
    frequency TEXT NOT NULL, -- 'once daily', 'twice daily', 'as needed', etc.
    duration TEXT, -- '7 days', '2 weeks', 'ongoing'
    instructions TEXT,
    is_active INTEGER DEFAULT 1,
    created_at TEXT DEFAULT (datetime('now')),
    updated_at TEXT DEFAULT (datetime('now'))
);

-- ============================================
-- AI ASSISTANT SETTINGS (Admin configurable)
-- ============================================

CREATE TABLE IF NOT EXISTS ai_assistant_settings (
    id TEXT PRIMARY KEY DEFAULT (lower(hex(randomblob(16)))),
    model_name TEXT NOT NULL, -- 'gpt-4', 'claude-3', etc.
    is_active INTEGER DEFAULT 1,
    system_prompt TEXT,
    enable_document_analysis INTEGER DEFAULT 1,
    enable_symptom_checker INTEGER DEFAULT 1,
    enable_doctor_recommendation INTEGER DEFAULT 1,
    created_by_admin_id TEXT REFERENCES users(id) ON DELETE SET NULL,
    created_at TEXT DEFAULT (datetime('now')),
    updated_at TEXT DEFAULT (datetime('now'))
);

-- ============================================
-- USER SUSPENSION LOG
-- ============================================

CREATE TABLE IF NOT EXISTS user_suspensions (
    id TEXT PRIMARY KEY DEFAULT (lower(hex(randomblob(16)))),
    user_id TEXT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    suspended_by_admin_id TEXT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    reason TEXT NOT NULL,
    suspension_start TEXT DEFAULT (datetime('now')),
    suspension_end TEXT, -- NULL for indefinite
    is_active INTEGER DEFAULT 1,
    lifted_at TEXT,
    lifted_by_admin_id TEXT REFERENCES users(id) ON DELETE SET NULL,
    created_at TEXT DEFAULT (datetime('now'))
);

-- ============================================
-- TRIGGERS FOR UPDATED_AT (SQLite version)
-- ============================================

-- Trigger for users table
CREATE TRIGGER IF NOT EXISTS update_users_updated_at 
AFTER UPDATE ON users
FOR EACH ROW
BEGIN
    UPDATE users SET updated_at = datetime('now') WHERE id = NEW.id;
END;

-- Trigger for patient_profiles table
CREATE TRIGGER IF NOT EXISTS update_patient_profiles_updated_at 
AFTER UPDATE ON patient_profiles
FOR EACH ROW
BEGIN
    UPDATE patient_profiles SET updated_at = datetime('now') WHERE id = NEW.id;
END;

-- Trigger for doctor_profiles table
CREATE TRIGGER IF NOT EXISTS update_doctor_profiles_updated_at 
AFTER UPDATE ON doctor_profiles
FOR EACH ROW
BEGIN
    UPDATE doctor_profiles SET updated_at = datetime('now') WHERE id = NEW.id;
END;

-- Trigger for admin_profiles table
CREATE TRIGGER IF NOT EXISTS update_admin_profiles_updated_at 
AFTER UPDATE ON admin_profiles
FOR EACH ROW
BEGIN
    UPDATE admin_profiles SET updated_at = datetime('now') WHERE id = NEW.id;
END;

-- Trigger for conversations table
CREATE TRIGGER IF NOT EXISTS update_conversations_updated_at 
AFTER UPDATE ON conversations
FOR EACH ROW
BEGIN
    UPDATE conversations SET updated_at = datetime('now') WHERE id = NEW.id;
END;

-- Trigger for support_tickets table
CREATE TRIGGER IF NOT EXISTS update_support_tickets_updated_at 
AFTER UPDATE ON support_tickets
FOR EACH ROW
BEGIN
    UPDATE support_tickets SET updated_at = datetime('now') WHERE id = NEW.id;
END;

-- Trigger for medical_records table
CREATE TRIGGER IF NOT EXISTS update_medical_records_updated_at 
AFTER UPDATE ON medical_records
FOR EACH ROW
BEGIN
    UPDATE medical_records SET updated_at = datetime('now') WHERE id = NEW.id;
END;

-- Trigger for consultation_notes table
CREATE TRIGGER IF NOT EXISTS update_consultation_notes_updated_at 
AFTER UPDATE ON consultation_notes
FOR EACH ROW
BEGIN
    UPDATE consultation_notes SET updated_at = datetime('now') WHERE id = NEW.id;
END;

-- Trigger for prescriptions table
CREATE TRIGGER IF NOT EXISTS update_prescriptions_updated_at 
AFTER UPDATE ON prescriptions
FOR EACH ROW
BEGIN
    UPDATE prescriptions SET updated_at = datetime('now') WHERE id = NEW.id;
END;

-- Trigger for ai_assistant_settings table
CREATE TRIGGER IF NOT EXISTS update_ai_assistant_settings_updated_at 
AFTER UPDATE ON ai_assistant_settings
FOR EACH ROW
BEGIN
    UPDATE ai_assistant_settings SET updated_at = datetime('now') WHERE id = NEW.id;
END;

-- ============================================
-- TRIGGER: Update conversation last_message_at
-- ============================================

CREATE TRIGGER IF NOT EXISTS trigger_update_conversation_last_message
AFTER INSERT ON messages
FOR EACH ROW
BEGIN
    UPDATE conversations
    SET last_message_at = NEW.created_at,
        total_messages = total_messages + 1,
        ai_messages_count = CASE WHEN NEW.sender_type = 'ai' THEN ai_messages_count + 1 ELSE ai_messages_count END,
        doctor_messages_count = CASE WHEN NEW.sender_type = 'doctor' THEN doctor_messages_count + 1 ELSE doctor_messages_count END
    WHERE id = NEW.conversation_id;
END;

-- ============================================
-- TRIGGER: Update doctor rating average
-- ============================================

CREATE TRIGGER IF NOT EXISTS trigger_update_doctor_rating_insert
AFTER INSERT ON doctor_ratings
FOR EACH ROW
BEGIN
    UPDATE doctor_profiles
    SET average_rating = (
            SELECT AVG(rating)
            FROM doctor_ratings
            WHERE doctor_id = NEW.doctor_id
        ),
        total_ratings = (
            SELECT COUNT(*)
            FROM doctor_ratings
            WHERE doctor_id = NEW.doctor_id
        )
    WHERE user_id = NEW.doctor_id;
END;

CREATE TRIGGER IF NOT EXISTS trigger_update_doctor_rating_update
AFTER UPDATE ON doctor_ratings
FOR EACH ROW
BEGIN
    UPDATE doctor_profiles
    SET average_rating = (
            SELECT AVG(rating)
            FROM doctor_ratings
            WHERE doctor_id = NEW.doctor_id
        ),
        total_ratings = (
            SELECT COUNT(*)
            FROM doctor_ratings
            WHERE doctor_id = NEW.doctor_id
        )
    WHERE user_id = NEW.doctor_id;
END;

-- ============================================
-- TRIGGER: Update conversation consultation status
-- ============================================

CREATE TRIGGER IF NOT EXISTS trigger_update_conversation_status
AFTER INSERT ON consultation_notes
FOR EACH ROW
WHEN NEW.is_finalized = 1
BEGIN
    UPDATE conversations
    SET diagnosis_completed = 1,
        consultation_status = 'completed'
    WHERE id = NEW.conversation_id;
END;

-- ============================================
-- TRIGGER: Track active doctor conversations (INSERT)
-- ============================================

CREATE TRIGGER IF NOT EXISTS trigger_update_doctor_conversations_insert
AFTER INSERT ON conversations
FOR EACH ROW
WHEN NEW.assigned_doctor_id IS NOT NULL
BEGIN
    UPDATE doctor_profiles
    SET current_active_conversations = current_active_conversations + 1
    WHERE user_id = NEW.assigned_doctor_id;
END;

-- ============================================
-- TRIGGER: Track active doctor conversations (UPDATE - assign doctor)
-- ============================================

CREATE TRIGGER IF NOT EXISTS trigger_update_doctor_conversations_assign
AFTER UPDATE ON conversations
FOR EACH ROW
WHEN NEW.assigned_doctor_id IS NOT NULL AND OLD.assigned_doctor_id IS NULL
BEGIN
    UPDATE doctor_profiles
    SET current_active_conversations = current_active_conversations + 1
    WHERE user_id = NEW.assigned_doctor_id;
END;

-- ============================================
-- TRIGGER: Track active doctor conversations (UPDATE - close conversation)
-- ============================================

CREATE TRIGGER IF NOT EXISTS trigger_update_doctor_conversations_close
AFTER UPDATE ON conversations
FOR EACH ROW
WHEN NEW.status IN ('closed', 'archived') AND OLD.status NOT IN ('closed', 'archived')
BEGIN
    UPDATE doctor_profiles
    SET current_active_conversations = MAX(current_active_conversations - 1, 0)
    WHERE user_id = NEW.assigned_doctor_id;
END;

-- ============================================
-- INDEXES for better query performance
-- ============================================

CREATE INDEX IF NOT EXISTS idx_conversations_patient_id ON conversations(patient_id);
CREATE INDEX IF NOT EXISTS idx_conversations_doctor_id ON conversations(assigned_doctor_id);
CREATE INDEX IF NOT EXISTS idx_conversations_status ON conversations(status);
CREATE INDEX IF NOT EXISTS idx_messages_conversation_id ON messages(conversation_id);
CREATE INDEX IF NOT EXISTS idx_messages_sender_id ON messages(sender_id);
CREATE INDEX IF NOT EXISTS idx_documents_conversation_id ON documents(conversation_id);
CREATE INDEX IF NOT EXISTS idx_documents_patient_id ON documents(patient_id);
CREATE INDEX IF NOT EXISTS idx_doctor_ratings_doctor_id ON doctor_ratings(doctor_id);
CREATE INDEX IF NOT EXISTS idx_activity_logs_user_id ON activity_logs(user_id);
CREATE INDEX IF NOT EXISTS idx_support_tickets_user_id ON support_tickets(user_id);
CREATE INDEX IF NOT EXISTS idx_medical_records_patient_id ON medical_records(patient_id);
CREATE INDEX IF NOT EXISTS idx_consultation_notes_conversation_id ON consultation_notes(conversation_id);
CREATE INDEX IF NOT EXISTS idx_prescriptions_patient_id ON prescriptions(patient_id);
