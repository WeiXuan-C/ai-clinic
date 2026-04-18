# AI Clinic - System Architecture & Design Documentation

## Executive Summary

AI Clinic is a healthtech platform that combines AI-powered medical assistance with real doctor consultations. The system enables patients to get instant answers through an AI chatbot trained on official medical documents, while also providing access to real doctors from various organizations for personalized care.

## Core Features

1. **AI Chatbot with Document Grounding**
   - Patients upload medical documents
   - AI analyzes documents using vector embeddings
   - Patients receive accurate, document-backed answers with citations

2. **OTP-Based Authentication with Auto-Registration**
   - Passwordless login via OTP (email)
   - Automatic user registration on first login
   - Role-based access (Patient, Doctor, Admin)

3. **Multi-Organization Doctor Network**
   - Doctors from different organizations can join
   - Real-time availability status
   - Smart routing to available doctors based on specialization and workload

4. **Hybrid Chat System**
   - AI chatbot for instant responses
   - Automatic escalation to human doctors when available
   - Seamless handoff between AI and human support
   - Context preservation across AI-to-human transitions

---

## System Architecture

### Technology Stack

#### Backend
- **Framework**: ASP.NET Core 8.0 (Blazor Server)
- **Language**: C# 12
- **Database**: Supabase (PostgreSQL)
- **Real-time**: Supabase Realtime for live chat updates

#### Frontend
- **Framework**: Blazor Server Components
- **UI Library**: Custom Stitch Design System
- **State Management**: Singleton State Pattern

---

## Architecture Flow

```
Supabase (Database)
    ↓
DAO (Data Access - Adapter Pattern)
    ↓
Controller (Facade Pattern - Simplifies Complex Logic)
    ↓
Service (Business Logic using Factory Objects)
    ↓
State (Singleton Pattern - Global State Management)
    ↓
UI (Blazor Components)
```

---

## Design Patterns Implementation

### 1. **Adapter Pattern** → DAO Layer
- **Purpose**: Adapts Supabase PostgreSQL interface to application-specific repository interfaces
- **Location**: `DAOs/` folder
- **Implementation**: Each DAO implements a repository interface and adapts Supabase client calls

**Example:**
```csharp
// Interface (Database/Interfaces/IUserRepository.cs)
public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
}

// Adapter Implementation (DAOs/UserDAO.cs)
public class UserDAO : IUserRepository
{
    private readonly Supabase.Client _supabase;
    
    // Adapts Supabase calls to repository interface
    public async Task<User?> GetByEmailAsync(string email)
    {
        var response = await _supabase
            .From<User>()
            .Where(x => x.Email == email)
            .Single();
        return response;
    }
}
```

### 2. **Singleton Pattern** → State Layer
- **Purpose**: Ensures single instance of application state across the entire application
- **Location**: `UI/State/` folder
- **Implementation**: State classes registered as Singleton in DI container

**Example:**
```csharp
// State/AuthState.cs
public class AuthState
{
    private User? _currentUser;
    public event Action? OnChange;
    
    public User? CurrentUser
    {
        get => _currentUser;
        set
        {
            _currentUser = value;
            NotifyStateChanged();
        }
    }
    
    private void NotifyStateChanged() => OnChange?.Invoke();
}

// Registration in DependencyInjection.cs
services.AddSingleton<AuthState>();
```

### 3. **Abstract Factory Pattern** → Factory Layer
- **Purpose**: Creates families of related objects without specifying concrete classes
- **Location**: `Factories/` folder
- **Implementation**: Factories create service and state objects with proper dependencies

**Example:**
```csharp
// Factories/ServiceFactory.cs
public interface IServiceFactory
{
    IAuthService CreateAuthService();
    IChatService CreateChatService();
}

public class ServiceFactory : IServiceFactory
{
    private readonly IUserRepository _userRepo;
    private readonly IConversationRepository _convRepo;
    
    public IAuthService CreateAuthService()
    {
        return new AuthService(_userRepo);
    }
    
    public IChatService CreateChatService()
    {
        return new ChatService(_convRepo);
    }
}
```

### 4. **Facade Pattern** → Controller Layer
- **Purpose**: Simplifies complex subsystem interactions by providing unified interface
- **Location**: `Controller/` folder
- **Implementation**: Controllers coordinate between multiple services and state

**Example:**
```csharp
// Controller/ChatController.cs
public class ChatController
{
    private readonly IChatService _chatService;
    private readonly IDocumentService _documentService;
    private readonly IDoctorService _doctorService;
    private readonly ChatState _chatState;
    
    // Facade method that coordinates complex chat routing logic
    public async Task<Message> SendMessageAsync(string content)
    {
        // 1. Check if doctor is assigned
        var conversation = _chatState.CurrentConversation;
        
        // 2. Route to appropriate handler
        if (conversation.AssignedDoctorId != null)
        {
            return await _chatService.SendToDoctorAsync(content);
        }
        else
        {
            // 3. Check doctor availability
            var availableDoctor = await _doctorService.FindAvailableDoctorAsync();
            
            if (availableDoctor != null)
            {
                await _chatService.AssignDoctorAsync(availableDoctor.Id);
                return await _chatService.SendToDoctorAsync(content);
            }
            else
            {
                // 4. Route to AI with document context
                var documents = await _documentService.GetConversationDocumentsAsync();
                return await _chatService.SendToAIAsync(content, documents);
            }
        }
    }
}
```

---

## Project Folder Structure

```
ai-clinic/
│
├── Database/                    # Core Domain Layer
│   ├── Entities/               # Domain models (User, Doctor, Message, etc.)
│   ├── Interfaces/             # Repository contracts (IUserRepository, etc.)
│   └── Migrations/             # SQL migration files
│
├── DAOs/                       # Data Access Layer (Adapter Pattern)
│   ├── UserDAO.cs             # Adapts Supabase to IUserRepository
│   ├── DoctorDAO.cs
│   ├── ConversationDAO.cs
│   ├── MessageDAO.cs
│   └── DocumentDAO.cs
│
├── Factories/                  # Abstract Factory Pattern
│   ├── ServiceFactory.cs      # Creates service instances
│   └── StateFactory.cs        # Creates state instances
│
├── Services/                   # Business Logic Layer
│   ├── AuthService.cs         # Authentication logic
│   ├── ChatService.cs         # Chat routing and AI integration
│   ├── DoctorService.cs       # Doctor matching and assignment
│   └── DocumentService.cs     # Document processing and vector search
│
├── Controller/                 # Facade Layer
│   ├── AuthController.cs      # Simplifies auth flows
│   ├── ChatController.cs      # Coordinates chat operations
│   └── DoctorController.cs    # Manages doctor operations
│
├── UI/                         # Presentation Layer
│   ├── State/                 # Singleton State Management
│   │   ├── AuthState.cs       # Global auth state
│   │   ├── ChatState.cs       # Global chat state
│   │   └── DoctorState.cs     # Global doctor state
│   │
│   ├── Components/            # Reusable UI components
│   │   ├── App.razor          # Root component
│   │   ├── Routes.razor       # Routing configuration
│   │   └── Layout/            # Layout components
│   │
│   └── Pages/                 # Page components
│       ├── Auth/              # Authentication pages
│       ├── Patient/           # Patient dashboard pages
│       └── General/           # Public pages
│
├── wwwroot/                    # Static assets
│   ├── css/                   # Stylesheets
│   └── js/                    # JavaScript files
│
├── Program.cs                  # Application entry point
├── DependencyInjection.cs     # DI container configuration
└── appsettings.json           # Configuration


```

---

## Data Flow Example: Sending a Chat Message

```
1. UI Component (Patient/Consultation.razor)
   ↓ User types message
   
2. State Layer (ChatState)
   ↓ Updates UI state
   
3. Controller (ChatController.SendMessageAsync)
   ↓ Coordinates the operation (Facade)
   
4. Services Layer
   ├─ ChatService.RouteMessage()
   ├─ DoctorService.FindAvailableDoctor()
   └─ DocumentService.SearchRelevantDocs()
   ↓ Business logic execution
   
5. DAO Layer (MessageDAO, DoctorDAO)
   ↓ Adapts to Supabase interface
   
6. Supabase Database
   ↓ Persists data
   
7. Response flows back up
   ↓
   
8. State Layer updates
   ↓
   
9. UI re-renders with new message
```

---

## Business Logic & Rules

### 1. Chat Routing Logic (Implemented in ChatController)
```
IF patient sends message:
    CHECK if conversation exists
    IF conversation has assigned doctor AND doctor is available:
        ROUTE to doctor via ChatService
    ELSE IF no available doctors:
        ROUTE to AI chatbot
        SEARCH relevant documents using DocumentService
        GENERATE response with document citations
    ELSE:
        FIND available doctor via DoctorService
        ASSIGN doctor to conversation
        NOTIFY doctor
        ROUTE message to doctor
```

### 2. Doctor Assignment Algorithm (Implemented in DoctorService)
```
Priority factors:
1. Specialization match (if patient has specific condition)
2. Current workload (number of active conversations)
3. Doctor rating
4. Response time history

Algorithm:
- Filter available doctors (status = "available")
- Score each doctor based on factors
- Assign highest-scoring doctor
- Update doctor availability if at capacity
```

### 3. OTP Flow (Implemented in AuthService)
```
Login Request:
1. User enters email
2. System checks if user exists via UserDAO
3. Generate 6-digit OTP
4. Store OTP with 5-minute expiration
5. Send via email service
6. Return success response

Verification:
1. User enters OTP
2. Validate OTP and expiration
3. IF valid AND user exists:
     Generate session token
     Update AuthState
   ELSE IF valid AND new user:
     Create user account via UserDAO (auto-registration)
     Assign default role (Patient)
     Generate session token
     Update AuthState
   ELSE:
     Return error
4. Mark OTP as used
```

---

## Design Pattern Benefits

### Adapter Pattern (DAO)
✅ **Decouples** application from Supabase-specific implementation  
✅ **Easy to swap** database providers without changing business logic  
✅ **Testable** - can mock repository interfaces  

### Singleton Pattern (State)
✅ **Single source of truth** for application state  
✅ **Reactive updates** - components subscribe to state changes  
✅ **Memory efficient** - one instance shared across app  

### Abstract Factory Pattern (Factories)
✅ **Centralized object creation** with proper dependencies  
✅ **Consistent initialization** of complex objects  
✅ **Easy to extend** with new service types  

### Facade Pattern (Controllers)
✅ **Simplifies complex operations** into single method calls  
✅ **Reduces coupling** between UI and multiple services  
✅ **Cleaner UI code** - components don't need to know about multiple services  

---

## Key Implementation Files

### Core Interfaces
- `Database/Interfaces/IRepository.cs` - Base repository contract
- `Database/Interfaces/IUserRepository.cs` - User-specific operations
- `Database/Interfaces/IConversationRepository.cs` - Conversation operations
- `Database/Interfaces/IMessageRepository.cs` - Message operations
- `Database/Interfaces/IDoctorRepository.cs` - Doctor operations

### DAOs (Adapter Pattern)
- `DAOs/UserDAO.cs` - Adapts Supabase to IUserRepository
- `DAOs/ConversationDAO.cs` - Adapts Supabase to IConversationRepository
- `DAOs/MessageDAO.cs` - Adapts Supabase to IMessageRepository
- `DAOs/DoctorDAO.cs` - Adapts Supabase to IDoctorRepository

### Services (Business Logic)
- `Services/AuthService.cs` - Authentication and authorization
- `Services/ChatService.cs` - Chat routing and message handling
- `Services/DoctorService.cs` - Doctor matching and assignment
- `Services/DocumentService.cs` - Document processing and search

### Controllers (Facade Pattern)
- `Controller/AuthController.cs` - Simplifies auth flows
- `Controller/ChatController.cs` - Coordinates chat operations
- `Controller/DoctorController.cs` - Manages doctor operations

### State (Singleton Pattern)
- `UI/State/AuthState.cs` - Global authentication state
- `UI/State/ChatState.cs` - Global chat state
- `UI/State/DoctorState.cs` - Global doctor state

### Factories (Abstract Factory Pattern)
- `Factories/ServiceFactory.cs` - Creates service instances
- `Factories/StateFactory.cs` - Creates state instances

---

## Development Guidelines

### Adding a New Feature

1. **Define Entity** (if needed) in `Database/Entities/`
2. **Create Interface** in `Database/Interfaces/`
3. **Implement DAO** (Adapter) in `DAOs/`
4. **Create Service** (Business Logic) in `Services/`
5. **Add Controller** (Facade) in `Controller/`
6. **Create State** (if needed) in `UI/State/`
7. **Build UI Component** in `UI/Pages/` or `UI/Components/`
8. **Register in DI** in `DependencyInjection.cs`

### Testing Strategy

- **Unit Tests**: Test services and DAOs independently
- **Integration Tests**: Test controller coordination
- **UI Tests**: Test Blazor components with mock state
- **E2E Tests**: Test complete user flows

---

## Security Considerations

- **Authentication**: OTP-based passwordless auth
- **Authorization**: Role-based access control (Patient, Doctor, Admin)
- **Data Privacy**: HIPAA-compliant data handling
- **Encryption**: All data encrypted at rest and in transit
- **Audit Logging**: All actions logged in activity_logs table

---

## Future Enhancements

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

### 4. Multi-language Support
- Real-time translation for international patients
- Localized medical terminology

### 5. Telemedicine Integration
- Video consultation capabilities
- Screen sharing for document review
- Prescription generation