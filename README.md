# AI Clinic - Medical Consultation Platform

## Project Overview

AI Clinic is a modern medical consultation platform built with Blazor Server architecture and integrated with Supabase backend services. The system supports real-time communication between patients and doctors, providing complete user management, consultation records, document management, and rating systems.

---

## 🚀 How to Run the Project

### Prerequisites

Before running the project, ensure you have the following installed:

1. **.NET 8.0 SDK**
   - Download from: https://dotnet.microsoft.com/download/dotnet/8.0
   - Verify installation: `dotnet --version`

2. **Supabase Account**
   - Create a free account at: https://supabase.com
   - Create a new project and note down your URL and anon key

3. **OpenAI API Key** (Optional, for AI features)
   - Get your API key from: https://platform.openai.com/api-keys

4. **OpenRouter API Key** (Optional, for multi-model AI support)
   - Get your API key from: https://openrouter.ai

5. **Gmail Account** (For OTP email sending)
   - Enable 2-Step Verification
   - Generate App Password: https://support.google.com/accounts/answer/185833

### Step 1: Extract the ZIP File

Extract the `ai-clinic.zip` file to your desired location:

```bash
# Example location
C:\Projects\ai-clinic\
```

### Step 2: Configure Environment Variables

1. Navigate to the project directory
2. Copy `.env.example` to `.env`:
   ```bash
   copy .env.example .env
   ```

3. Open `.env` file and configure the following:

```env
# Supabase Configuration
SUPABASE_URL=https://your-project-id.supabase.co
SUPABASE_KEY=your-supabase-anon-key

# OpenAI Configuration (Optional)
OPENAI_API_KEY=sk-your-openai-api-key

# OpenRouter Configuration (Optional)
OPENROUTER_API_KEY=sk-or-your-openrouter-key

# JWT Configuration
JWT_SECRET=your-super-secret-jwt-key-minimum-32-characters-long

# SMTP Configuration for OTP emails
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USERNAME=your-email@gmail.com
SMTP_PASSWORD=your-16-digit-app-password
SMTP_FROM_EMAIL=your-email@gmail.com
SMTP_FROM_NAME=AI Clinic
SMTP_ENABLE_SSL=true
```

### Step 3: Set Up Supabase Database

1. Log in to your Supabase dashboard
2. Navigate to **SQL Editor**
3. Run the migration scripts in the following order:

```sql
-- 1. Create initial database schema
-- Run: Database/database-setup.sql

-- 2. Run additional migrations (in order)
-- Run: Database/add-doctor-settings-columns.sql
-- Run: Database/add-consultation-summary-column.sql
-- Run: Database/add-profile-photo-column.sql
-- Run: Database/add-medical-document-fields.sql
-- Run: Database/add-ai-model-management.sql
```

**Or use the Supabase CLI:**
```bash
# Install Supabase CLI
npm install -g supabase

# Link to your project
supabase link --project-ref your-project-id

# Run migrations
supabase db push
```

### Step 4: Restore NuGet Packages

Open a terminal/command prompt in the project directory and restore dependencies:

```bash
cd C:\Projects\ai-clinic
dotnet restore
```

### Step 5: Build the Project

Compile the project to check for errors:

```bash
dotnet build
```

If successful, you should see:
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### Step 6: Run the Application

Start the development server:

```bash
dotnet run
```

Or use the watch mode for auto-reload during development:

```bash
dotnet watch run
```

The application will start and display:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:5001
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
```

### Step 7: Access the Application

Open your browser and navigate to:
- **HTTPS**: https://localhost:5001
- **HTTP**: http://localhost:5000

### Step 8: Create Initial Admin Account

1. Navigate to the **Sign Up** page
2. Select **Admin** role (if available)
3. Fill in the registration form
4. Check your email for the OTP code
5. Complete the registration

**Note**: For production, you may need to manually set the first admin in the database.

---

## 🐛 Troubleshooting

### Issue: "Connection string not found"

**Solution**: Ensure your `.env` file is in the project root directory and properly configured.

### Issue: "Failed to send OTP email"

**Solutions**:
1. Verify SMTP credentials in `.env`
2. Ensure Gmail App Password is correct (not your Gmail password)
3. Check if 2-Step Verification is enabled on your Gmail account

### Issue: "Supabase connection failed"

**Solutions**:
1. Verify `SUPABASE_URL` and `SUPABASE_KEY` in `.env`
2. Check your internet connection
3. Ensure your Supabase project is active

### Issue: Port already in use

**Solution**: Change the port in `appsettings.json` or kill the process using the port:

```bash
# Windows
netstat -ano | findstr :5001
taskkill /PID <process_id> /F

# Or change port in appsettings.json
"Kestrel": {
  "Endpoints": {
    "Http": {
      "Url": "http://localhost:5002"
    }
  }
}
```

### Issue: SSL Certificate errors

**Solution**: Trust the development certificate:

```bash
dotnet dev-certs https --trust
```

---

## 📝 Development Commands

```bash
# Restore packages
dotnet restore

# Build project
dotnet build

# Run application
dotnet run

# Run with hot reload
dotnet watch run

# Clean build artifacts
dotnet clean

# Run with specific configuration
dotnet run --configuration Release

# List available commands
dotnet --help
```

---

## 📂 Project Structure

```
ai-clinic/
├── Database/              # SQL migration scripts
├── Data/                  # Database client and helpers
├── Models/                # Data models and entities
├── Services/              # Business logic services
├── UI/
│   ├── Components/        # Reusable Blazor components
│   ├── Pages/             # Page components
│   └── wwwroot/           # Static files (CSS, JS, images)
├── .env.example           # Environment variables template
├── appsettings.json       # Application configuration
├── Program.cs             # Application entry point
└── ai-clinic.csproj       # Project file
```

---

## 🔒 Security Notes

1. **Never commit `.env` file** - It contains sensitive credentials
2. **Change JWT_SECRET** - Use a strong, random key for production
3. **Use App Passwords** - Don't use your actual Gmail password
4. **Enable HTTPS** - Always use HTTPS in production
5. **Database Security** - Configure Row Level Security (RLS) in Supabase

---

## 🌐 Default User Roles

The system supports three user roles:

1. **Patient** - Can create consultations, chat with doctors, upload documents
2. **Doctor** - Can view assigned consultations, respond to patients, manage profile
3. **Admin** - Can manage users, view activity logs, configure AI settings, handle support tickets

---

## 📧 Support

If you encounter any issues:

1. Check the **Troubleshooting** section above
2. Review the error logs in the terminal
3. Check Supabase logs in your dashboard
4. Open an issue in the project repository (if applicable)

---

## Core Features

### 1. Authentication System
- **OTP Email Verification Login**: Passwordless login via 8-digit verification code sent to email
- **Role-Based Registration**:
  - Patient registration flow
  - Doctor registration flow (requires license number and professional information)
  - Admin permission management
- **Session Management**: JWT token management based on Supabase Auth
- **Local Database Verification**: Checks local user table during login to ensure user is registered

### 2. User Profile Management

#### Patient Profile
- **Basic Information**: Name, date of birth, gender, address
- **Medical Information**: Blood type, allergies, chronic conditions, current medications
- **Emergency Contact**: Name, phone number
- **Privacy Settings**:
  - Data sharing toggle
  - AI analysis toggle
  - Activity tracking toggle

#### Doctor Profile
- **Basic Information**: Name, title, license number
- **Professional Information**:
  - Primary Specialization
  - Sub-specializations
  - Medical Expertise Tags
  - Symptoms Expertise
  - Conditions Treated
  - Procedures Performed
  - Age Groups Treated
  - Languages Spoken
- **Availability Management**:
  - Status: available, busy, offline
  - Working hours configuration (JSON format)
  - Current active session count
- **Performance Metrics**:
  - Total consultations
  - Average rating (0-5 stars)
  - Total rating count
- **Verification Status**:
  - Verified status
  - Accepting new patients

#### Admin Profile
- **Permission Management**:
  - User management permission
  - AI system management permission
  - Doctor management permission
  - Ticket management permission
  - Permission assignment permission

### 3. Consultation System

#### Conversation Management
- **Conversation Creation**: Patients initiate conversations with doctors
- **Conversation Status**: active, closed, archived, deactive
- **Conversation Information**:
  - Title
  - Initial symptoms (array)
  - AI-suggested specialty
  - Assigned doctor
  - Consultation status (pending, in_progress, completed)
  - Diagnosis completed flag
  - Prescription generated flag
  - Required specialty
  - AI confidence score
- **Statistics**:
  - Total message count
  - AI message count
  - Doctor message count
  - Last message timestamp

#### Message System
- **Message Types**: patient, doctor, ai, system
- **Message Content**:
  - Text content
  - AI model information (if AI used)
  - AI confidence score
  - Document references (UUID array)
- **Read Status**:
  - Read flag
  - Read timestamp
- **Real-time Updates**: Message real-time push via SignalR

### 4. Doctor Directory & Matching

#### Doctor Search Features
- **Filter Criteria**:
  - Filter by specialty
  - Filter by availability
  - Keyword search
- **Sort Options**:
  - Sort by rating
  - Sort by experience
  - Sort by availability

#### Doctor Information Display
- Doctor avatar
- Name and title
- Specialty and sub-specialties
- Years of experience
- Rating and review count
- Real-time availability status
- Consultation fee
- Next available time
- Verification badge

#### Smart Matching
- Recommend specialty based on patient symptoms
- Find available doctors by specialty
- Consider doctor's current workload
- Prioritize highly-rated doctors

### 5. Document Management

#### Document Upload
- **Supported Types**:
  - Medical record
  - Lab result
  - Prescription
  - Image
  - Other
- **Document Information**:
  - Filename
  - File size
  - MIME type
  - Upload timestamp
  - Description
  - Tags

#### AI Processing
- Documents can be marked as "processed"
- Extract text content for AI analysis
- Documents can be referenced in messages

### 6. Rating System

#### Doctor Rating
- **Overall Rating**: 1-5 stars (required)
- **Detailed Ratings**:
  - Professionalism rating
  - Communication rating
  - Knowledge rating
  - Response time rating
- **Review Text**: Patients can write detailed reviews
- **Linked Conversation**: Each rating is linked to a consultation

#### Rating Statistics
- Automatically calculate doctor's average rating
- Automatically update total rating count
- Triggers automatically maintain rating data

### 7. Support Ticket System

#### Ticket Management
- **Ticket Creation**:
  - Subject (required)
  - Detailed description (required)
  - Category: technical, billing, medical, account, other
  - Priority: low, medium, high, urgent
- **Ticket Status**:
  - open: New ticket
  - in_progress: In progress
  - resolved: Resolved
  - closed: Closed
- **Timestamps**:
  - Created at
  - Updated at
  - Resolved at
  - Closed at

#### Ticket Attachments
- Support file uploads
- Record filename, URL, size, MIME type

#### Ticket Responses
- Admins can reply to tickets
- Support internal notes (admin-only)
- Record response time and responder

---

## Technical Architecture

### Backend Stack
- **Framework**: ASP.NET Core 8.0 (Blazor Server)
- **Language**: C# 12

### Frontend Stack
- **Framework**: Blazor Server Components
- **Render Mode**: Interactive Server Render Mode (prerender: false)
- **UI Library**:
  - MudBlazor 9.3.0
- **Icon Library**: Lucide Icons
- **Styling**: Custom CSS + Stitch Design System

### Dependencies
```xml
<PackageReference Include="MudBlazor" Version="9.3.0" />
```

## Database Design

### Core Table Structure

#### users (User Table)
- Stores basic information for all users
- Roles: patient, doctor, admin
- Privacy settings: data sharing, AI analysis, activity tracking
- Account status: active, suspended

#### patient_profiles (Patient Profile Table)
- Links to users table (user_id)
- Medical information: blood type, allergies, chronic conditions, medications
- Emergency contact information

#### doctor_profiles (Doctor Profile Table)
- Links to users table (user_id)
- License information
- Professional information (multiple array fields)
- Availability status
- Performance metrics

#### admin_profiles (Admin Profile Table)
- Links to users table (user_id)
- Permission configuration

#### conversations (Conversation Table)
- Conversation records between patients and doctors
- Status tracking
- Statistical information

#### messages (Message Table)
- Message records in conversations
- Sender type
- AI-related information

#### documents (Document Table)
- Documents uploaded in conversations
- AI processing status

#### doctor_ratings (Doctor Rating Table)
- Patient ratings for doctors
- Multi-dimensional ratings

#### support_tickets (Support Ticket Table)
- User-submitted support requests
- Status tracking

#### support_ticket_attachments (Ticket Attachment Table)
- Files related to tickets

#### support_ticket_responses (Ticket Response Table)
- Admin reply records

### Database Triggers

#### 1. Auto-update updated_at
- All major tables have `updated_at` triggers
- Automatically update timestamp on UPDATE operations

#### 2. Update Conversation Last Message Time
- When a new message is inserted, automatically update conversation's `last_message_at`
- Automatically increment message counters

#### 3. Update Doctor Average Rating
- When a rating is inserted or updated, automatically recalculate doctor's average rating
- Automatically update total rating count

#### 4. Track Doctor Active Session Count
- When a doctor is assigned or a conversation is closed, automatically update doctor's active session count

---

## Page Structure

### Authentication Pages (UI/Pages/Auth/)
- **Signin.razor**: Login page
  - OTP sending
  - OTP verification
  - Error handling
- **Signup.razor**: Registration page
  - Role selection
  - Information input
  - OTP verification

### Patient Pages (UI/Pages/Patient/)
- **Dashboard.razor**: Patient dashboard
  - Welcome message
  - Recent consultations
  - Appointment information
  - Health metrics
  - AI insight cards
- **Consultation.razor**: Consultation page
  - Conversation history sidebar
  - Doctor directory (searchable, filterable)
  - Chat interface
  - Message input area
  - Document upload
  - Context sidebar
- **Profile.razor**: Profile
- **Records.razor**: Medical records
- **Settings.razor**: Settings
- **Support.razor**: Support tickets

### General Pages (UI/Pages/General/)
- **About.razor**: About page
- **Consultation.razor**: General consultation page
- **Doctors.razor**: Doctor list page

### Layout Components (UI/Components/Layout/)
- **MainLayout.razor**: Main layout
- **SidebarLayout.razor**: Sidebar layout
- **EmptyLayout.razor**: Empty layout (for login pages)

---

## Upcoming Features

### 1. AI Assistant Integration
- AI chatbot
- Document analysis and extraction
- Symptom assessment
- Specialty recommendation

### 2. Real-time Notifications
- New message notifications
- Conversation status change notifications

### 3. Medical Record Export
- PDF generation
- Data export functionality

---

## Security Considerations

### Authentication
- OTP email verification (passwordless)

### Data Privacy
- User privacy settings
- Data sharing controls
- HIPAA compliance considerations (to be improved)

### Access Control
- Role-based access control (RBAC)
- Patients can only access their own data
- Doctors can only access assigned conversations
- Admin permission hierarchy

---

## License

[To be determined]

---

## Contact

[To be determined]
