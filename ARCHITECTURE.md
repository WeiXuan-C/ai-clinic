# AI Clinic - System Architecture & Design Documentation

## Executive Summary

AI Clinic is a healthtech platform that combines AI-powered medical assistance with real doctor consultations. The system enables patients to get instant answers through an AI chatbot trained on official medical documents, while also providing access to real doctors from various organizations for personalized care.

## Core Features

1. **AI Chatbot with Document Embedding**
   - Admins/doctors upload official medical documents
   - System performs vector embedding for semantic search
   - Patients receive accurate, document-backed answers

2. **OTP-Based Authentication with Auto-Registration**
   - Passwordless login via OTP (email/SMS)
   - Automatic user registration on first login
   - Role-based access (Patient, Doctor, Admin)

3. **Multi-Organization Doctor Network**
   - Doctors from different organizations can join
   - Real-time availability status
   - Smart routing to available doctors

4. **Hybrid Chat System**
   - AI chatbot for instant responses
   - Automatic escalation to human doctors when available
   - Seamless handoff between AI and human support

---

## Design Patterns

### 1. Repository Pattern

**Purpose**: Abstracts data access logic and provides a clean separation between business logic and data access layers.

**Implementation in AI Clinic**:
```
Backend/
├── Repositories/
│   ├── IRepository.cs (Generic interface)
│   ├── IPatientRepository.cs
│   ├── IDoctorRepository.cs
│   ├── IChatMessageRepository.cs
│   ├── IDocumentRepository.cs
│   └── Implementations/
│       ├── PatientRepository.cs
│       ├── DoctorRepository.cs
│       ├── ChatMessageRepository.cs
│       └── DocumentRepository.cs
```

**Benefits**:
- Centralized data access logic
- Easy to mock for unit testing
- Database-agnostic business logic
- Simplified maintenance

**Usage Example**:
```csharp
// In Service layer
public class ChatService
{
    private readonly IChatMessageRepository _chatRepo;
    private readonly IDoctorRepository _doctorRepo;
    
    public async Task<ChatMessage> SendMessage(string patientId, string message)
    {
        var availableDoctor = await _doctorRepo.GetAvailableDoctorAsync();
        // Business logic here
    }
}
```

### 2. Strategy Pattern

**Purpose**: Defines a family of algorithms, encapsulates each one, and makes them interchangeable.

**Implementation in AI Clinic**:
```
Backend/
├── Strategies/
│   ├── IResponseStrategy.cs
│   ├── AIResponseStrategy.cs
│   ├── DoctorResponseStrategy.cs
│   └── HybridResponseStrategy.cs
├── Strategies/
│   ├── IAuthenticationStrategy.cs
│   ├── EmailOTPStrategy.cs
│   └── SMSOTPStrategy.cs
```

**Benefits**:
- Flexible message routing (AI vs Doctor)
- Easy to add new authentication methods
- Runtime strategy selection
- Open/Closed principle compliance

**Usage Example**:
```csharp
// Chat routing strategy
public interface IResponseStrategy
{
    Task<string> GetResponseAsync(string message, string patientId);
}

public class ChatOrchestrator
{
    public async Task<string> ProcessMessage(string message, string patientId)
    {
        IResponseStrategy strategy = await DetermineStrategy();
        return await strategy.GetResponseAsync(message, patientId);
    }
}
```

### 3. Observer Pattern (Event-Driven Architecture)

**Purpose**: Defines a one-to-many dependency between objects so that when one object changes state, all its dependents are notified.

**Implementation in AI Clinic**:
```
Backend/
├── Events/
│   ├── IEventPublisher.cs
│   ├── IEventSubscriber.cs
│   ├── Events/
│   │   ├── DocumentUploadedEvent.cs
│   │   ├── DoctorAvailabilityChangedEvent.cs
│   │   ├── NewMessageEvent.cs
│   │   └── OTPGeneratedEvent.cs
│   └── Handlers/
│       ├── DocumentEmbeddingHandler.cs
│       ├── NotificationHandler.cs
│       └── ChatRoutingHandler.cs
```

**Benefits**:
- Decoupled components
- Real-time notifications
- Scalable event processing
- Easy to add new event handlers

**Usage Example**:
```csharp
// When document is uploaded
await _eventPublisher.PublishAsync(new DocumentUploadedEvent
{
    DocumentId = doc.Id,
    UploadedBy = doctorId,
    Timestamp = DateTime.UtcNow
});

// Handler processes embedding asynchronously
public class DocumentEmbeddingHandler : IEventHandler<DocumentUploadedEvent>
{
    public async Task HandleAsync(DocumentUploadedEvent evt)
    {
        await _embeddingService.ProcessDocumentAsync(evt.DocumentId);
    }
}
```

---

## System Architecture

### Technology Stack

#### Backend
- **Framework**: ASP.NET Core 10.0 (Blazor Server)
- **Language**: C# 12
- **Database**: Supabase (PostgreSQL)
- **Vector Database**: Supabase pgvector extension
- **Real-time**: SignalR for chat
- **Authentication**: Custom OTP with JWT tokens

#### AI & ML
- **Embedding Model**: OpenAI text-embedding-3-small or Azure OpenAI
- **LLM**: OpenAI GPT-4 or Azure OpenAI
- **Vector Search**: Supabase pgvector with cosine similarity

#### Frontend
- **Framework**: Blazor Server Components
- **UI Library**: Bootstrap 5
- **Real-time**: SignalR client

#### Infrastructure
- **Hosting**: Azure App Service / AWS / Self-hosted
- **File Storage**: Supabase Storage or Azure Blob Storage
- **Email**: SendGrid / AWS SES
- **SMS**: Twilio / AWS SNS

### Database Schema

```sql
-- Users (base table)
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    phone_number VARCHAR(20) UNIQUE,
    email VARCHAR(255) UNIQUE,
    full_name VARCHAR(255) NOT NULL,
    role VARCHAR(20) NOT NULL, -- 'Patient', 'Doctor', 'Admin'
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

-- Patients
CREATE TABLE patients (
    id UUID PRIMARY KEY REFERENCES users(id),
    date_of_birth DATE,
    medical_history TEXT,
    allergies TEXT[]
);

-- Doctors
CREATE TABLE doctors (
    id UUID PRIMARY KEY REFERENCES users(id),
    organization_name VARCHAR(255),
    specialization VARCHAR(100),
    license_number VARCHAR(100) UNIQUE,
    is_available BOOLEAN DEFAULT false,
    rating DECIMAL(3,2) DEFAULT 0.00
);

-- OTP Tokens
CREATE TABLE otp_tokens (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    identifier VARCHAR(255) NOT NULL, -- email or phone
    otp_code VARCHAR(6) NOT NULL,
    expires_at TIMESTAMP NOT NULL,
    is_used BOOLEAN DEFAULT false,
    created_at TIMESTAMP DEFAULT NOW()
);

-- Medical Documents
CREATE TABLE medical_documents (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    title VARCHAR(500) NOT NULL,
    content TEXT NOT NULL,
    file_url TEXT,
    uploaded_by UUID REFERENCES users(id),
    category VARCHAR(100),
    created_at TIMESTAMP DEFAULT NOW()
);

-- Document Embeddings
CREATE TABLE document_embeddings (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    document_id UUID REFERENCES medical_documents(id),
    chunk_text TEXT NOT NULL,
    embedding VECTOR(1536), -- OpenAI embedding dimension
    chunk_index INTEGER,
    created_at TIMESTAMP DEFAULT NOW()
);

-- Chat Conversations
CREATE TABLE conversations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    patient_id UUID REFERENCES patients(id),
    doctor_id UUID REFERENCES doctors(id), -- NULL if AI-only
    status VARCHAR(20) DEFAULT 'active', -- 'active', 'closed'
    started_at TIMESTAMP DEFAULT NOW(),
    ended_at TIMESTAMP
);

-- Chat Messages
CREATE TABLE chat_messages (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    conversation_id UUID REFERENCES conversations(id),
    sender_id UUID REFERENCES users(id),
    sender_type VARCHAR(20) NOT NULL, -- 'Patient', 'Doctor', 'AI'
    message_text TEXT NOT NULL,
    created_at TIMESTAMP DEFAULT NOW()
);

-- Indexes for performance
CREATE INDEX idx_embeddings_vector ON document_embeddings 
USING ivfflat (embedding vector_cosine_ops);

CREATE INDEX idx_conversations_patient ON conversations(patient_id);
CREATE INDEX idx_conversations_doctor ON conversations(doctor_id);
CREATE INDEX idx_messages_conversation ON chat_messages(conversation_id);
```

---

## Project Structure

```
ai-clinic/
├── Backend/
│   ├── Controllers/
│   │   ├── AuthController.cs
│   │   ├── ChatController.cs
│   │   ├── DocumentController.cs
│   │   ├── DoctorController.cs
│   │   └── PatientController.cs
│   ├── Services/
│   │   ├── Interfaces/
│   │   │   ├── IAuthService.cs
│   │   │   ├── IChatService.cs
│   │   │   ├── IEmbeddingService.cs
│   │   │   ├── IOTPService.cs
│   │   │   └── IDocumentService.cs
│   │   ├── AuthService.cs
│   │   ├── ChatService.cs
│   │   ├── EmbeddingService.cs
│   │   ├── OTPService.cs
│   │   ├── DocumentService.cs
│   │   └── SupabaseService.cs
│   ├── Repositories/
│   │   ├── Interfaces/
│   │   │   ├── IRepository.cs
│   │   │   ├── IPatientRepository.cs
│   │   │   ├── IDoctorRepository.cs
│   │   │   ├── IChatRepository.cs
│   │   │   └── IDocumentRepository.cs
│   │   └── Implementations/
│   │       ├── PatientRepository.cs
│   │       ├── DoctorRepository.cs
│   │       ├── ChatRepository.cs
│   │       └── DocumentRepository.cs
│   ├── Strategies/
│   │   ├── ResponseStrategies/
│   │   │   ├── IResponseStrategy.cs
│   │   │   ├── AIResponseStrategy.cs
│   │   │   ├── DoctorResponseStrategy.cs
│   │   │   └── HybridResponseStrategy.cs
│   │   └── AuthStrategies/
│   │       ├── IAuthenticationStrategy.cs
│   │       ├── EmailOTPStrategy.cs
│   │       └── SMSOTPStrategy.cs
│   ├── Events/
│   │   ├── IEventPublisher.cs
│   │   ├── IEventHandler.cs
│   │   ├── EventPublisher.cs
│   │   ├── Events/
│   │   │   ├── DocumentUploadedEvent.cs
│   │   │   ├── DoctorAvailabilityChangedEvent.cs
│   │   │   └── NewMessageEvent.cs
│   │   └── Handlers/
│   │       ├── DocumentEmbeddingHandler.cs
│   │       ├── NotificationHandler.cs
│   │       └── ChatRoutingHandler.cs
│   ├── Models/
│   │   ├── BaseEntity.cs
│   │   ├── User.cs
│   │   ├── Patient.cs
│   │   ├── Doctor.cs
│   │   ├── Conversation.cs
│   │   ├── ChatMessage.cs
│   │   ├── MedicalDocument.cs
│   │   └── DocumentEmbedding.cs
│   ├── DTOs/
│   │   ├── Auth/
│   │   │   ├── LoginRequestDto.cs
│   │   │   ├── VerifyOTPDto.cs
│   │   │   └── AuthResponseDto.cs
│   │   ├── Chat/
│   │   │   ├── SendMessageDto.cs
│   │   │   ├── MessageResponseDto.cs
│   │   │   └── ConversationDto.cs
│   │   └── Document/
│   │       ├── UploadDocumentDto.cs
│   │       └── DocumentDto.cs
│   ├── Hubs/
│   │   └── ChatHub.cs
│   └── Middleware/
│       ├── JwtMiddleware.cs
│       └── ExceptionMiddleware.cs
├── Components/
│   ├── Pages/
│   │   ├── Auth/
│   │   │   ├── Login.razor
│   │   │   └── VerifyOTP.razor
│   │   ├── Patient/
│   │   │   ├── Dashboard.razor
│   │   │   └── Chat.razor
│   │   ├── Doctor/
│   │   │   ├── Dashboard.razor
│   │   │   ├── Patients.razor
│   │   │   └── Chat.razor
│   │   └── Admin/
│   │       ├── Dashboard.razor
│   │       ├── Documents.razor
│   │       └── Users.razor
│   ├── Shared/
│   │   ├── ChatComponent.razor
│   │   ├── DocumentUpload.razor
│   │   └── DoctorCard.razor
│   └── Layout/
│       ├── MainLayout.razor
│       ├── NavMenu.razor
│       └── AuthLayout.razor
├── wwwroot/
│   ├── css/
│   ├── js/
│   └── assets/
├── Program.cs
├── appsettings.json
└── ai-clinic.csproj
```

---

## Implementation Workflow

### Phase 1: Foundation (Week 1-2)
1. Set up Supabase database with schema
2. Implement Repository Pattern for data access
3. Create base models and DTOs
4. Set up authentication infrastructure

### Phase 2: Authentication (Week 2-3)
1. Implement OTP service (email/SMS)
2. Create Strategy Pattern for auth methods
3. Build JWT token generation
4. Auto-registration flow
5. Role-based authorization

### Phase 3: Document Management (Week 3-4)
1. Document upload API
2. Implement embedding service (OpenAI)
3. Vector storage in Supabase
4. Event-driven embedding processing
5. Semantic search functionality

### Phase 4: Chat System (Week 4-6)
1. SignalR hub setup
2. AI chatbot integration
3. Strategy Pattern for response routing
4. Doctor availability management
5. Conversation persistence

### Phase 5: UI Development (Week 6-8)
1. Blazor components for chat
2. Patient dashboard
3. Doctor dashboard
4. Admin panel
5. Real-time updates

### Phase 6: Testing & Deployment (Week 8-10)
1. Unit tests
2. Integration tests
3. Performance optimization
4. Security audit
5. Production deployment

---

## Business Logic & Rules

### 1. Chat Routing Logic
```
IF patient sends message:
    CHECK if conversation exists
    IF conversation has assigned doctor AND doctor is available:
        ROUTE to doctor
    ELSE IF no available doctors:
        ROUTE to AI chatbot
        SEARCH relevant documents using vector similarity
        GENERATE response with document citations
    ELSE:
        FIND available doctor
        ASSIGN doctor to conversation
        NOTIFY doctor
        ROUTE message to doctor
```

### 2. Doctor Assignment Algorithm
```
Priority factors:
1. Specialization match (if patient has specific condition)
2. Current workload (number of active conversations)
3. Doctor rating
4. Response time history

Algorithm:
- Filter available doctors
- Score each doctor based on factors
- Assign highest-scoring doctor
- Update doctor availability if at capacity
```

### 3. Document Embedding Process
```
ON document upload:
1. Extract text content
2. Split into chunks (500-1000 tokens)
3. Generate embeddings for each chunk
4. Store in vector database
5. Index for fast retrieval
6. Publish DocumentUploadedEvent
```

### 4. OTP Flow
```
Login Request:
1. User enters email/phone
2. System checks if user exists
3. Generate 6-digit OTP
4. Store OTP with 5-minute expiration
5. Send via email/SMS
6. Return success response

Verification:
1. User enters OTP
2. Validate OTP and expiration
3. IF valid AND user exists:
     Generate JWT token
   ELSE IF valid AND new user:
     Create user account (auto-registration)
     Assign default role (Patient)
     Generate JWT token
   ELSE:
     Return error
4. Mark OTP as used
```

---

## Creative Features

### 1. Smart Document Suggestions
- AI analyzes patient questions
- Suggests relevant documents to doctors
- Highlights key sections for quick reference

### 2. Symptom Checker Integration
- Pre-chat symptom assessment
- Routes to appropriate specialist
- Provides preliminary information

### 3. Multi-language Support
- Translate documents automatically
- Support patient queries in multiple languages
- Maintain accuracy across translations

### 4. Doctor Performance Analytics
- Response time tracking
- Patient satisfaction scores
- Specialization effectiveness

### 5. Appointment Scheduling
- Book follow-up appointments
- Calendar integration
- Automated reminders

---

## Security Considerations

1. **Data Encryption**
   - TLS for data in transit
   - Encryption at rest for sensitive data
   - Secure key management

2. **HIPAA Compliance**
   - Audit logging
   - Access controls
   - Data retention policies

3. **Rate Limiting**
   - OTP request limits
   - API rate limiting
   - DDoS protection

4. **Input Validation**
   - Sanitize all user inputs
   - Prevent SQL injection
   - XSS protection

---

## API Endpoints

### Authentication
```
POST /api/auth/request-otp
POST /api/auth/verify-otp
POST /api/auth/refresh-token
POST /api/auth/logout
```

### Chat
```
GET  /api/chat/conversations
GET  /api/chat/conversations/{id}/messages
POST /api/chat/send-message
PUT  /api/chat/conversations/{id}/close
```

### Documents
```
GET  /api/documents
POST /api/documents/upload
GET  /api/documents/{id}
DELETE /api/documents/{id}
POST /api/documents/search
```

### Doctors
```
GET  /api/doctors/available
PUT  /api/doctors/availability
GET  /api/doctors/{id}/profile
PUT  /api/doctors/{id}/profile
```

### Patients
```
GET  /api/patients/{id}/profile
PUT  /api/patients/{id}/profile
GET  /api/patients/{id}/history
```

---

## Configuration

### appsettings.json
```json
{
  "Supabase": {
    "Url": "your-supabase-url",
    "Key": "your-supabase-key"
  },
  "OpenAI": {
    "ApiKey": "your-openai-key",
    "Model": "gpt-4",
    "EmbeddingModel": "text-embedding-3-small"
  },
  "JWT": {
    "Secret": "your-jwt-secret",
    "Issuer": "ai-clinic",
    "Audience": "ai-clinic-users",
    "ExpirationMinutes": 60
  },
  "OTP": {
    "ExpirationMinutes": 5,
    "Length": 6
  },
  "Email": {
    "Provider": "SendGrid",
    "ApiKey": "your-sendgrid-key",
    "FromEmail": "noreply@ai-clinic.com"
  },
  "SMS": {
    "Provider": "Twilio",
    "AccountSid": "your-twilio-sid",
    "AuthToken": "your-twilio-token",
    "FromNumber": "+1234567890"
  }
}
```

---

## Next Steps

1. Review and approve this architecture document
2. Set up development environment
3. Create Supabase project and configure database
4. Obtain API keys (OpenAI, SendGrid, Twilio)
5. Begin Phase 1 implementation

---

**Document Version**: 1.0  
**Last Updated**: April 12, 2026  
**Author**: AI Clinic Development Team
