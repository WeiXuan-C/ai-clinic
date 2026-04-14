# AI Clinic - System Architecture & Design Documentation

## Executive Summary

AI Clinic is a healthtech platform that combines AI-powered medical assistance with real doctor consultations. The system enables patients to get instant answers through an AI chatbot trained on official medical documents, while also providing access to real doctors from various organizations for personalized care.

## Core Features

1. **AI Chatbot with Document**
   - Patients upload medical documents
   - Patients receive accurate, document-backed answers

2. **OTP-Based Authentication with Auto-Registration**
   - Passwordless login via OTP (email)
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
## System Architecture

### Technology Stack

#### Backend
- **Framework**: ASP.NET Core 10.0 (Blazor Server)
- **Language**: C# 12
- **Database**: Supabase (PostgreSQL)

#### Frontend
- **Framework**: Blazor Server Components
- **UI Library**: MudBlazor

### Layer Details

#### Application Layer
- **Location**: `/Application`
- **Responsibility**: Business logic and application services
- **Contains**:
  - Command: Command pattern implementations
  - Service: Application service interfaces
  - DTO: Data Transfer Objects
  - Factory: Factory pattern implementations

#### Infrastructure Layer
- **Location**: `/Infrastructure`
- **Responsibility**: Data access and external service integration
- **Contains**:
  - DB: Database context
  - Supabase: Supabase integration
  - API: External API calls

#### Presentation Layer
- **Location**: `/Presentation`, `/Components`, `/Pages`
- **Responsibility**: User interface and interaction
- **Contains**:
  - Controller: Controllers
  - State: State management
  - UI Components: Reusable interface components
  - Pages: Complete page views

#### Cross-cutting Concerns
- **Location**: `DependencyInjection.cs`
- **Responsibility**: Dependency injection configuration
- **Contains**: Registration of all dependencies and services

### Design Patterns

#### Project Folder Structure

This project follows a clean layered architecture where each folder has a clear responsibility:

##### Folder Layer Mapping

| Folder | Layer | Purpose |
|--------|-------|---------|
| `/Application` | Application Layer | Command, Service, DTO, Factory |
| `/Infrastructure` | Infrastructure Layer | DB, Supabase, API |
| `/Presentation` | Presentation Layer | Controller, State |
| `/Components` | Presentation Layer | UI Components |
| `/Pages` | Presentation Layer | Pages |
| `DependencyInjection.cs` | Cross-cutting | Register all dependencies | 

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

### 3. OTP Flow
```
Login Request:
1. User enters email
2. System checks if user exists
3. Generate 6-digit OTP
4. Store OTP with 5-minute expiration
5. Send via email
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

## Creative Features

### 1. Smart Document Suggestions
- AI analyzes patient questions
- Suggests relevant documents to doctors
- Highlights key sections for quick reference

### 2. Symptom Checker Integration
- Pre-chat symptom assessment
- Routes to appropriate specialist
- Provides preliminary information

### 3. Doctor Performance Analytics
- Response time tracking
- Patient satisfaction scores
- Specialization effectiveness