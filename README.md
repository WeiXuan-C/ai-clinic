# AI Clinic - Healthtech Platform

AI Clinic is a comprehensive healthtech platform that combines AI-powered medical assistance with real doctor consultations. The system enables patients to get instant answers through an AI chatbot trained on official medical documents, while also providing access to real doctors from various organizations.

## Features

- **AI Chatbot with Document Embedding**: Upload medical documents and get AI-powered responses based on official sources
- **OTP-Based Authentication**: Passwordless login with automatic user registration
- **Multi-Organization Doctor Network**: Doctors from different organizations can join and provide consultations
- **Hybrid Chat System**: Smart routing between AI and human doctors based on availability
- **Real-time Communication**: SignalR-powered real-time chat

## Architecture

This project follows three key design patterns:

1. **Repository Pattern**: Clean separation between data access and business logic
2. **Strategy Pattern**: Flexible authentication methods and chat routing
3. **Observer Pattern**: Event-driven architecture for async processing

## Tech Stack

- **Backend**: ASP.NET Core 10.0 (Blazor Server)
- **Database**: Supabase (PostgreSQL + pgvector)
- **AI**: OpenAI (GPT-4 + text-embedding-3-small)
- **Real-time**: SignalR
- **Authentication**: JWT with OTP

## Getting Started

### Prerequisites

- .NET 10.0 SDK
- Supabase account
- OpenAI API key
- (Optional) SendGrid API key for email
- (Optional) Twilio account for SMS

### Installation

1. **Clone the repository**
```bash
git clone <repository-url>
cd ai-clinic
```

2. **Set up Supabase**
   - Create a new Supabase project at https://supabase.com
   - Copy your project URL and anon key
   - Run the SQL script in `database-setup.sql` in your Supabase SQL Editor

3. **Configure environment variables**

Create a `.env` file or update `appsettings.json`:

```json
{
  "Supabase": {
    "Url": "your-supabase-url",
    "Key": "your-supabase-anon-key"
  },
  "OpenAI": {
    "ApiKey": "your-openai-api-key",
    "Model": "gpt-4",
    "EmbeddingModel": "text-embedding-3-small"
  },
  "JWT": {
    "Secret": "your-super-secret-jwt-key-min-32-chars",
    "Issuer": "ai-clinic",
    "Audience": "ai-clinic-users"
  },
  "Email": {
    "ApiKey": "your-sendgrid-api-key",
    "FromEmail": "noreply@ai-clinic.com"
  }
}
```

4. **Restore packages**
```bash
dotnet restore
```

5. **Run the application**
```bash
dotnet run
```

The application will be available at `https://localhost:5001`

## Project Structure

```
ai-clinic/
├── Backend/
│   ├── Controllers/          # API endpoints
│   ├── Services/             # Business logic
│   ├── Repositories/         # Data access layer
│   ├── Strategies/           # Strategy pattern implementations
│   ├── Events/               # Event-driven architecture
│   ├── Models/               # Domain models
│   ├── DTOs/                 # Data transfer objects
│   └── Hubs/                 # SignalR hubs
├── Components/               # Blazor components
├── Pages/                    # Blazor pages
├── wwwroot/                  # Static files
├── ARCHITECTURE.md           # Detailed architecture documentation
├── database-setup.sql        # Database schema
└── README.md                 # This file
```

## API Endpoints

### Authentication
- `POST /api/auth/request-otp` - Request OTP code
- `POST /api/auth/verify-otp` - Verify OTP and login

### Chat
- `POST /api/chat/send-message` - Send a message
- `GET /api/chat/conversations` - Get user conversations
- `GET /api/chat/conversations/{id}/messages` - Get conversation messages

### Documents
- `POST /api/documents/upload` - Upload medical document (Doctor/Admin only)
- `GET /api/documents` - Get all documents
- `GET /api/documents/{id}` - Get specific document
- `DELETE /api/documents/{id}` - Delete document (Admin only)

### Doctors
- `PUT /api/doctor/availability` - Update doctor availability
- `GET /api/doctor/available` - Get available doctors

## Usage Flow

### For Patients

1. **Login**: Enter email/phone and receive OTP
2. **Verify**: Enter OTP code (auto-registration if new user)
3. **Chat**: Start a conversation
   - AI responds instantly if no doctors available
   - Automatically routed to doctor when available
4. **View History**: Access past conversations

### For Doctors

1. **Login**: Use OTP authentication
2. **Set Availability**: Toggle online/offline status
3. **Receive Patients**: System routes patients based on availability
4. **Respond**: Chat with patients in real-time
5. **Upload Documents**: Add medical documents for AI training

### For Admins

1. **Manage Documents**: Upload, view, and delete medical documents
2. **Monitor System**: View all users and conversations
3. **Manage Users**: Assign roles and permissions

## Development

### Running Tests
```bash
dotnet test
```

### Building for Production
```bash
dotnet publish -c Release
```

## Configuration

### OpenAI Setup
1. Get API key from https://platform.openai.com
2. Add to `appsettings.json` under `OpenAI:ApiKey`
3. Adjust model and token limits as needed

### Email Setup (SendGrid)
1. Create account at https://sendgrid.com
2. Generate API key
3. Add to `appsettings.json` under `Email:ApiKey`

### SMS Setup (Twilio)
1. Create account at https://twilio.com
2. Get Account SID and Auth Token
3. Add to `appsettings.json` under `SMS` section

## Security Considerations

- All API endpoints (except auth) require JWT authentication
- Role-based authorization for sensitive operations
- OTP tokens expire after 5 minutes
- Passwords are never stored (OTP-only authentication)
- HTTPS enforced in production
- Input validation on all endpoints

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## License

This project is licensed under the MIT License.

## Support

For issues and questions:
- Create an issue on GitHub
- Email: support@ai-clinic.com

## Roadmap

- [ ] Multi-language support
- [ ] Video consultations
- [ ] Appointment scheduling
- [ ] Prescription management
- [ ] Mobile app (iOS/Android)
- [ ] Analytics dashboard
- [ ] Payment integration

---

**Version**: 1.0.0  
**Last Updated**: April 12, 2026
