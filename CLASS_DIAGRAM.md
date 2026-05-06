# AI Clinic - Complete UML Class Diagram

## Model Classes

### User
```
┌─────────────────────────────────────┐
          <<Entity>>               
             User                  
├─────────────────────────────────────┤
- Id: Guid                         
- Email: string                    
- PasswordHash: string             
- Phone: string?                   
- Role: UserRole                   
- IsActive: bool                   
- CreatedAt: DateTime              
- UpdatedAt: DateTime              
- LastLoginAt: DateTime?           
- DataSharingEnabled: bool         
- AiAnalysisEnabled: bool          
- ActivityTrackingEnabled: bool    
- IsDeactivated: bool              
- DeactivatedAt: DateTime?         
├─────────────────────────────────────┤
+ PatientProfile: PatientProfile?  
+ DoctorProfile: DoctorProfile?    
+ AdminProfile: AdminProfile?      
+ PatientConversations: ICollection
+ DoctorConversations: ICollection 
+ Messages: ICollection<Message>   
+ Documents: ICollection<Document> 
└─────────────────────────────────────┘
```

### PatientProfile
```
┌─────────────────────────────────────┐
          <<Entity>>               
        PatientProfile             
├─────────────────────────────────────┤
- Id: Guid                         
- UserId: Guid                     
- FullName: string?                
- DateOfBirth: DateTime?           
- Gender: string?                  
- Address: string?                 
- EmergencyContactName: string?    
- EmergencyContactPhone: string?   
- BloodType: string?               
- Allergies: string?               
- ChronicConditions: string?       
- CurrentMedications: string?      
- ProfilePhoto: byte[]?            
- CreatedAt: DateTime              
- UpdatedAt: DateTime              
├─────────────────────────────────────┤
+ User: User                       
└─────────────────────────────────────┘
```

### DoctorProfile
```
┌─────────────────────────────────────┐
          <<Entity>>               
        DoctorProfile              
├─────────────────────────────────────┤
- Id: Guid                         
- UserId: Guid                     
- FullName: string                 
- Title: string?                   
- LicenseNumber: string            
- PrimarySpecialization: string    
- SubSpecializations: string?      
- MedicalExpertiseTags: string?    
- SymptomsExpertise: string?       
- ConditionsTreated: string?       
- ProceduresPerformed: string?     
- AgeGroupsTreated: string?        
- LanguagesSpoken: string?         
- YearsOfExperience: int?          
- AvailabilityStatus: Status       
- WorkingHours: string?            
- CurrentActiveConversations: int  
- TotalConsultations: int          
- AverageRating: decimal           
- TotalRatings: int                
- ProfilePhotoUrl: string?         
- ProfilePhoto: byte[]?            
- IsVerified: bool                 
- IsActive: bool                   
- IsAcceptingPatients: bool        
- AutoAcceptAppointments: bool     
- MaxDailyPatients: int            
- NotifyUrgentConsultations: bool  
- NotifyNewAppointments: bool      
- NotifyAiAssessments: bool        
- NotifyEmailSummaries: bool       
- SessionTimeoutMinutes: int       
- CreatedAt: DateTime              
- UpdatedAt: DateTime              
├─────────────────────────────────────┤
+ User: User                       
+ Ratings: ICollection<Rating>     
└─────────────────────────────────────┘
```

### AdminProfile
```
┌─────────────────────────────────────┐
          <<Entity>>               
         AdminProfile              
├─────────────────────────────────────┤
- Id: Guid                         
- UserId: Guid                     
- FullName: string                 
- CreatedAt: DateTime              
- UpdatedAt: DateTime              
- ManageUsers: bool                
- ManageAi: bool                   
- ManageDoctors: bool              
- ManageTickets: bool              
- ManagePermissions: bool          
├─────────────────────────────────────┤
+ User: User                       
└─────────────────────────────────────┘
```

### Conversation
```
┌─────────────────────────────────────┐
          <<Entity>>               
         Conversation              
├─────────────────────────────────────┤
- Id: Guid                         
- PatientId: Guid                  
- AssignedDoctorId: Guid?          
- Title: string?                   
- Status: ConversationStatus       
- InitialSymptoms: string?         
- AiSuggestedSpecialization: string?│
- StartedAt: DateTime              
- ClosedAt: DateTime?              
- LastMessageAt: DateTime          
- TotalMessages: int               
- AiMessagesCount: int             
- DoctorMessagesCount: int         
- CreatedAt: DateTime              
- UpdatedAt: DateTime              
- ConsultationStatus: string       
- DiagnosisCompleted: bool         
- PrescriptionGenerated: bool      
- RequiredSpecialization: string?  
- AiConfidenceScore: decimal?      
├─────────────────────────────────────┤
+ Patient: User                    
+ AssignedDoctor: User?            
+ Messages: ICollection<Message>   
+ Documents: ICollection<Document> 
+ Ratings: ICollection<Rating>     
└─────────────────────────────────────┘
```

### Message
```
┌─────────────────────────────────────┐
          <<Entity>>               
           Message                 
├─────────────────────────────────────┤
- Id: Guid                         
- ConversationId: Guid             
- SenderId: Guid?                  
- SenderType: MessageSenderType    
- Content: string                  
- AiModelUsed: string?             
- AiConfidenceScore: decimal?      
- DocumentReferences: string?      
- IsRead: bool                     
- ReadAt: DateTime?                
- CreatedAt: DateTime              
├─────────────────────────────────────┤
+ Conversation: Conversation       
+ Sender: User?                    
└─────────────────────────────────────┘
```

### ConsultationNote
```
┌─────────────────────────────────────┐
          <<Entity>>               
      ConsultationNote             
├─────────────────────────────────────┤
- Id: Guid                         
- ConversationId: Guid             
- DoctorId: Guid                   
- PatientId: Guid                  
- Symptoms: string?                
- PhysicalExamination: string?     
- Diagnosis: string                
- TreatmentPlan: string?           
- FollowUpInstructions: string?    
- PrescriptionId: Guid?            
- IsFinalized: bool                
- FinalizedAt: DateTime?           
- CreatedAt: DateTime              
- UpdatedAt: DateTime              
├─────────────────────────────────────┤
+ Conversation: Conversation       
+ Doctor: User                     
+ Patient: User                    
+ Prescription: MedicalRecord?     
+ Prescriptions: ICollection       
└─────────────────────────────────────┘
```

### Prescription
```
┌─────────────────────────────────────┐
          <<Entity>>               
        Prescription               
├─────────────────────────────────────┤
- Id: Guid                         
- ConsultationNoteId: Guid?        
- PatientId: Guid                  
- DoctorId: Guid                   
- MedicationName: string           
- Dosage: string                   
- Frequency: string                
- Duration: string?                
- Instructions: string?            
- IsActive: bool                   
- CreatedAt: DateTime              
- UpdatedAt: DateTime              
├─────────────────────────────────────┤
+ ConsultationNote: ConsultationNote│
+ Patient: User                    
+ Doctor: User                     
└─────────────────────────────────────┘
```

### MedicalRecord
```
┌─────────────────────────────────────┐
          <<Entity>>               
       MedicalRecord               
├─────────────────────────────────────┤
- Id: Guid                         
- PatientId: Guid                  
- ConversationId: Guid?            
- CreatedByDoctorId: Guid?         
- RecordType: string               
- Title: string                    
- Content: string                  
- DiagnosisCode: string?           
- DiagnosisDescription: string?    
- Medications: string?             
- RecordDate: DateTime             
- IsExported: bool                 
- ExportCount: int                 
- LastExportedAt: DateTime?        
- CreatedAt: DateTime              
- UpdatedAt: DateTime              
├─────────────────────────────────────┤
+ Patient: User                    
+ Conversation: Conversation?      
+ CreatedByDoctor: User?           
└─────────────────────────────────────┘
```

### Document
```
┌─────────────────────────────────────┐
          <<Entity>>               
          Document                 
├─────────────────────────────────────┤
- Id: Guid                         
- ConversationId: Guid?            
- UploadedByUserId: Guid           
- FileName: string                 
- FileType: DocumentType           
- FileSizeBytes: long              
- FileUrl: string                  
- MimeType: string?                
- IsProcessed: bool                
- ExtractedText: string?           
- Description: string?             
- Tags: string?                    
- CreatedAt: DateTime              
- PatientId: Guid?                 
- Title: string?                   
- DocumentTypeString: string?      
- FileData: byte[]?                
├─────────────────────────────────────┤
+ Conversation: Conversation       
+ UploadedByUser: User             
+ Patient: PatientProfile?         
└─────────────────────────────────────┘
```

### DoctorRating
```
┌─────────────────────────────────────┐
          <<Entity>>               
        DoctorRating               
├─────────────────────────────────────┤
- Id: Guid                         
- DoctorId: Guid                   
- PatientId: Guid                  
- ConversationId: Guid             
- Rating: int                      
- ReviewText: string?              
- ProfessionalismRating: int?      
- CommunicationRating: int?        
- KnowledgeRating: int?            
- ResponseTimeRating: int?         
- CreatedAt: DateTime              
├─────────────────────────────────────┤
+ Doctor: User                     
+ Patient: User                    
+ Conversation: Conversation       
└─────────────────────────────────────┘
```

### ActivityLog
```
┌─────────────────────────────────────┐
          <<Entity>>               
        ActivityLog                
├─────────────────────────────────────┤
- Id: Guid                         
- UserId: Guid?                    
- Action: string                   
- EntityType: string?              
- EntityId: Guid?                  
- IpAddress: string?               
- UserAgent: string?               
- Details: string?                 
- CreatedAt: DateTime              
├─────────────────────────────────────┤
+ User: User?                      
└─────────────────────────────────────┘
```

### AiAssistantSetting
```
┌─────────────────────────────────────┐
          <<Entity>>               
     AiAssistantSetting            
├─────────────────────────────────────┤
- Id: Guid                         
- ModelName: string                
- IsActive: bool                   
- SystemPrompt: string?            
- EnableDocumentAnalysis: bool     
- EnableSymptomChecker: bool       
- EnableDoctorRecommendation: bool 
- CreatedByAdminId: Guid?          
- CreatedAt: DateTime              
- UpdatedAt: DateTime              
├─────────────────────────────────────┤
+ CreatedByAdmin: User?            
└─────────────────────────────────────┘
```

### SupportTicket
```
┌─────────────────────────────────────┐
          <<Entity>>               
       SupportTicket               
├─────────────────────────────────────┤
- Id: Guid                         
- UserId: Guid                     
- Subject: string                  
- Description: string              
- Category: string?                
- Priority: string                 
- Status: string                   
- CreatedAt: DateTime              
- UpdatedAt: DateTime              
- ResolvedAt: DateTime?            
- ClosedAt: DateTime?              
├─────────────────────────────────────┤
+ User: User                       
+ Attachments: ICollection         
+ Responses: ICollection           
└─────────────────────────────────────┘
```

### SupportTicketAttachment
```
┌─────────────────────────────────────┐
          <<Entity>>               
  SupportTicketAttachment          
├─────────────────────────────────────┤
- Id: Guid                         
- TicketId: Guid                   
- FileName: string                 
- FileUrl: string                  
- FileSizeBytes: long              
- MimeType: string?                
- UploadedAt: DateTime             
├─────────────────────────────────────┤
+ Ticket: SupportTicket            
└─────────────────────────────────────┘
```

### SupportTicketResponse
```
┌─────────────────────────────────────┐
          <<Entity>>               
   SupportTicketResponse           
├─────────────────────────────────────┤
- Id: Guid                         
- TicketId: Guid                   
- ResponderId: Guid                
- Message: string                  
- IsInternalNote: bool             
- CreatedAt: DateTime              
├─────────────────────────────────────┤
+ Ticket: SupportTicket            
+ Responder: User                  
└─────────────────────────────────────┘
```

### UserSuspension
```
┌─────────────────────────────────────┐
          <<Entity>>               
       UserSuspension              
├─────────────────────────────────────┤
- Id: Guid                         
- UserId: Guid                     
- SuspendedByAdminId: Guid         
- Reason: string                   
- SuspensionStart: DateTime        
- SuspensionEnd: DateTime?         
- IsActive: bool                   
- LiftedAt: DateTime?              
- LiftedByAdminId: Guid?           
- CreatedAt: DateTime              
├─────────────────────────────────────┤
+ User: User                       
+ SuspendedByAdmin: User           
+ LiftedByAdmin: User?             
└─────────────────────────────────────┘
```

## Enum Classes

### UserRole
```
┌─────────────────────────────────────┐
         <<Enumeration>>           
           UserRole                
├─────────────────────────────────────┤
Patient                            
Doctor                             
Admin                              
└─────────────────────────────────────┘
```

### ConversationStatus
```
┌─────────────────────────────────────┐
         <<Enumeration>>           
      ConversationStatus           
├─────────────────────────────────────┤
Active                             
Closed                             
Archived                           
Deactive                           
└─────────────────────────────────────┘
```

### MessageSenderType
```
┌─────────────────────────────────────┐
         <<Enumeration>>           
      MessageSenderType            
├─────────────────────────────────────┤
Patient                            
Doctor                             
AI                                 
└─────────────────────────────────────┘
```

### DoctorAvailabilityStatus
```
┌─────────────────────────────────────┐
         <<Enumeration>>           
  DoctorAvailabilityStatus         
├─────────────────────────────────────┤
Available                          
Busy                               
Offline                            
└─────────────────────────────────────┘
```

### DocumentType
```
┌─────────────────────────────────────┐
         <<Enumeration>>           
        DocumentType               
├─────────────────────────────────────┤
MedicalRecord                      
LabResult                          
Prescription                       
Image                              
Other                              
└─────────────────────────────────────┘
```

## Service Classes

### UserService
```
┌─────────────────────────────────────┐
         <<Service>>               
         UserService               
├─────────────────────────────────────┤
+ GetByIdAsync(userId): Task<User?>
+ GetByEmailAsync(email): Task     
+ CreateAsync(user, password): Task
+ VerifyPasswordAsync(): Task<bool>
+ AuthenticateAsync(): Task<User?> 
+ UpdateAsync(user): Task<User>    
+ DeactivateAsync(userId): Task    
+ GetByRoleAsync(role): Task<List> 
+ GetAllUsersAsync(): Task<List>   
+ ActivateAsync(userId): Task      
+ DeleteAsync(userId): Task        
└─────────────────────────────────────┘
```

### PatientProfileService
```
┌─────────────────────────────────────┐
         <<Service>>               
    PatientProfileService          
├─────────────────────────────────────┤
+ GetByUserIdAsync(): Task         
+ CreateAsync(profile): Task      
+ UpdateAsync(profile): Task       
+ UpdateProfilePhotoAsync(): Task  
+ GetProfilePhotoAsync(): Task     
+ GetMedicalHistorySummaryAsync()  
└─────────────────────────────────────┘
```

### DoctorProfileService
```
┌─────────────────────────────────────┐
         <<Service>>               
    DoctorProfileService           
├─────────────────────────────────────┤
+ GetByUserIdAsync(): Task         
+ CreateAsync(profile): Task       
+ GetAvailableDoctorsAsync(): Task 
+ GetBySpecializationAsync(): Task 
+ UpdateAsync(profile): Task       
+ UpdateProfilePhotoAsync(): Task  
+ GetProfilePhotoAsync(): Task     
+ UpdateAvailabilityAsync(): Task  
+ UpdateRatingAsync(): Task        
+ GetAllAsync(): Task<List>        
└─────────────────────────────────────┘
```

### ConversationService
```
┌─────────────────────────────────────┐
         <<Service>>               
     ConversationService           
├─────────────────────────────────────┤
+ GetByIdAsync(id): Task           
+ GetConversationListByPatientId() 
+ GetByPatientIdAsync(): Task      
+ GetByDoctorIdAsync(): Task       
+ CreateAiConversationAsync(): Task
+ CreateDoctorConversationAsync()  
+ CreateAsync(conversation): Task  
+ AssignDoctorAsync(): Task        
+ UpdateStatusAsync(): Task        
+ UpdateTitleAsync(): Task         
+ GetActiveConversationsAsync()    
+ GetAvailableDoctorsAsync(): Task 
└─────────────────────────────────────┘
```

### MessageService
```
┌─────────────────────────────────────┐
         <<Service>>               
       MessageService              
├─────────────────────────────────────┤
+ GetByConversationIdAsync(): Task 
+ CreatePatientMessageAsync(): Task
+ CreateAiMessageAsync(): Task     
+ CreateDoctorMessageAsync(): Task 
+ CreateAsync(message): Task       
+ GetLatestMessagesAsync(): Task   
+ MarkAsReadAsync(messageId): Task 
+ MarkConversationAsReadAsync()    
+ GetUnreadCountAsync(): Task<int> 
└─────────────────────────────────────┘
```

### ConsultationService
```
┌─────────────────────────────────────┐
         <<Service>>               
     ConsultationService           
├─────────────────────────────────────┤
+ GetByIdAsync(noteId): Task       
+ GetByConversationIdAsync(): Task 
+ GetByPatientIdAsync(): Task      
+ CreateAsync(note): Task          
+ UpdateAsync(note): Task          
└─────────────────────────────────────┘
```

### MedicalRecordService
```
┌─────────────────────────────────────┐
         <<Service>>               
    MedicalRecordService           
├─────────────────────────────────────┤
+ GetByPatientIdAsync(): Task      
+ GetByDoctorIdAsync(): Task       
+ GetByIdAsync(recordId): Task     
+ CreateAsync(record): Task        
+ UpdateAsync(record): Task        
+ DeleteAsync(recordId): Task<bool>
└─────────────────────────────────────┘
```

### PrescriptionService
```
┌─────────────────────────────────────┐
         <<Service>>               
     PrescriptionService           
├─────────────────────────────────────┤
+ GetByIdAsync(id): Task           
+ GetByPatientIdAsync(): Task      
+ GetActiveByPatientIdAsync(): Task
+ GetByConsultationNoteIdAsync()   
+ GetByDoctorIdAsync(): Task       
+ CreateAsync(prescription): Task  
+ CreateBatchAsync(list): Task     
+ UpdateAsync(prescription): Task  
+ DeleteAsync(id): Task<bool>      
+ DeactivateAsync(id): Task        
+ ReactivateAsync(id): Task        
+ GetPatientStatisticsAsync(): Task
└─────────────────────────────────────┘
```

### DocumentService
```
┌─────────────────────────────────────┐
         <<Service>>               
      DocumentService              
├─────────────────────────────────────┤
+ GetByIdAsync(documentId): Task   
+ GetByConversationIdAsync(): Task 
+ GetByPatientIdAsync(): Task      
+ CreateAsync(document): Task      
+ DeleteAsync(documentId): Task    
+ GetByFileTypeAsync(): Task       
└─────────────────────────────────────┘
```

### AdminService
```
┌─────────────────────────────────────┐
         <<Service>>               
        AdminService               
├─────────────────────────────────────┤
- _activityLogService              
├─────────────────────────────────────┤
+ GetAllUsersAsync(): Task         
+ GetAllDoctorsAsync(): Task       
+ GetPendingVerificationsAsync()   
+ VerifyDoctorAsync(): Task<bool>  
+ SuspendUserAsync(): Task<bool>   
+ DeleteUserAsync(): Task<bool>    
+ GetAdminProfileAsync(): Task     
+ HasPermissionAsync(): Task<bool> 
+ UpdateAdminPermissionsAsync()    
+ GetDashboardStatsAsync(): Task   
└─────────────────────────────────────┘
```

### SupportTicketService
```
┌─────────────────────────────────────┐
         <<Service>>               
    SupportTicketService           
├─────────────────────────────────────┤
+ GetByIdAsync(ticketId): Task     
+ GetByUserIdAsync(userId): Task   
+ CreateAsync(ticket): Task        
+ AddResponseAsync(response): Task 
+ UpdateStatusAsync(): Task        
+ GetOpenTicketsAsync(): Task      
+ GetAllTicketsAsync(): Task       
+ GetAllAsync(): Task<List>        
+ GetByStatusAsync(status): Task   
└─────────────────────────────────────┘
```

### AiAssistantService
```
┌─────────────────────────────────────┐
         <<Service>>               
     AiAssistantService            
├─────────────────────────────────────┤
- _modelContext: AiModelContext    
├─────────────────────────────────────┤
+ CurrentModelName: string         
+ GetAvailableModels(): List       
+ SwitchModel(modelKey): void      
+ GenerateMedicalResponseAsync()   
+ GenerateConsultationNoteAsync()  
+ AnalyzeMedicalDocumentAsync()    
+ GenerateResponseAsync(): Task    
+ GenerateStreamingResponseAsync() 
└─────────────────────────────────────┘
```

## Relationships

```
User "1" --o "0..1" PatientProfile : has
User "1" --o "0..1" DoctorProfile : has
User "1" --o "0..1" AdminProfile : has
User "1" --o "*" Conversation : creates (as patient)
User "1" --o "*" Conversation : handles (as doctor)
User "1" --o "*" Message : sends
User "1" --o "*" Document : uploads

Conversation "1" --o "*" Message : contains
Conversation "1" --o "*" Document : has
Conversation "1" --o "*" DoctorRating : receives
Conversation "1" --o "0..1" ConsultationNote : generates

ConsultationNote "1" --o "*" Prescription : includes
MedicalRecord "*" --o "1" User : belongs to (patient)
MedicalRecord "*" --o "0..1" User : created by (doctor)

SupportTicket "1" --o "*" SupportTicketAttachment : has
SupportTicket "1" --o "*" SupportTicketResponse : has

DoctorProfile "1" --o "*" DoctorRating : receives
```

---

**Total Classes: 30+**
- **Model Classes**: 17
- **Enum Classes**: 5  
- **Service Classes**: 12+
- **Additional Classes**: DTOs, Facades, AI Strategies, etc.


---

## Design Pattern Diagrams

### Strategy Pattern - Doctor Recommendation System

```
┌─────────────────────────────────────────────────────────────────────────┐
                        STRATEGY PATTERN                                
└─────────────────────────────────────────────────────────────────────────┘

                    <<UI>>
              ┌──────────────────┐
               PatientPage    
               (Client)       
              ├──────────────────┤
              + OnSearchDoctor()│
              + DisplayResults()│
              └──────────────────┘
                      
                       uses
                       ▼
              ┌──────────────────────────────┐
                   <<Context>>            
              DoctorRecommendationService 
              ├──────────────────────────────┤
              - _matchingStrategy         
              ├──────────────────────────────┤
              + SetStrategy(strategy)     
              + GetRecommendedDoctorsAsync()│
              + CompareStrategiesAsync()  
              └──────────────────────────────┘
                      
                       delegates to
                       ▼
              ┌──────────────────────────────┐
                   <<Interface>>          
               IDoctorMatchingStrategy    
              ├──────────────────────────────┤
              + MatchDoctors(doctors,     
                  criteria): List<Result> 
              └──────────────────────────────┘
                       △
                       implements
         ┌─────────────┼─────────────┐
                                
┌────────────────┐ ┌────────────────┐ ┌────────────────────┐
<<Strategy>>   <<Strategy>>   <<Strategy>>      
BalancedMatch  SymptomBased   Specialization    
ingStrategy    MatchStrategy  BasedStrategy     
├────────────────┤ ├────────────────┤ ├────────────────────┤
+ MatchDoctors()+ MatchDoctors()+ MatchDoctors() 
(20% symptoms, (60% symptoms, (70% special.,   
 20% special.,  15% special.,  15% experience, 
 15% exp, etc)  10% exp, etc)  10% rating, etc)
└────────────────┘ └────────────────┘ └────────────────────┘

Input DTO:                          Output DTO:
┌──────────────────────┐           ┌──────────────────────┐
DoctorSearchCriteria           DoctorMatchResult   
├──────────────────────┤           ├──────────────────────┤
- Symptoms: List               - Doctor: Profile   
- Specialization               - MatchScore: decimal│
- MinRating                    - MatchReasons: List
- MaxResults                   - ScoreBreakdown    
└──────────────────────┘           └──────────────────────┘
```

### Adapter Pattern - AI Model System

```
┌─────────────────────────────────────────────────────────────────────────┐
                        ADAPTER PATTERN                                 
└─────────────────────────────────────────────────────────────────────────┘

                    <<UI>>
              ┌──────────────────┐
              ConsultationPage
               (Client)       
              ├──────────────────┤
              + OnSendMessage()│
              + DisplayAiReply()│
              └──────────────────┘
                      
                       uses
                       ▼
              ┌──────────────────────────────┐
                   <<Context>>            
                  AiModelContext          
              ├──────────────────────────────┤
              - _currentStrategy          
              - _availableStrategies      
              ├──────────────────────────────┤
              + SetStrategy(key)          
              + GenerateResponseAsync()   
              + GetAvailableModels()      
              └──────────────────────────────┘
                      
                       delegates to
                       ▼
              ┌──────────────────────────────┐
                <<Target Interface>>      
                  IAiModelStrategy        
              ├──────────────────────────────┤
              + ModelId: string           
              + ModelName: string         
              + GenerateResponseAsync()   
              + GenerateStreamingAsync()  
              └──────────────────────────────┘
                       △
                       implements
                      
              ┌──────────────────────────────┐
                   <<Adapter>>            
                BaseAiModelAdapter        
              ├──────────────────────────────┤
              # _apiClient: OpenRouter    ◄──────┐
              ├──────────────────────────────┤      
              + GenerateResponseAsync()          wraps
              # PreprocessPrompt()              
              # PostprocessResponse()           
              └──────────────────────────────┘      
                       △                            
                       extends                    
         ┌─────────────┼─────────────┐             
                                              
┌────────────────┐ ┌────────────────┐ ┌──────────────────┐
<<Strategy>>   <<Strategy>>   <<Strategy>>    
Gemma4Strategy MiniMaxStrategyNemotronStrategy
├────────────────┤ ├────────────────┤ ├──────────────────┤
+ ModelId      + ModelId      + ModelId       
+ ModelName    + ModelName    + ModelName     
└────────────────┘ └────────────────┘ └──────────────────┘
                                                   
                                                   
                                          ┌──────────────────────┐
                                             <<Adaptee>>      
                                          OpenRouterApiClient 
                                          ├──────────────────────┤
                                          - _httpClient       
                                          - _apiKey           
                                          ├──────────────────────┤
                                          + CallApiAsync(     
                                              request)        
                                            : OpenRouterResp  
                                          └──────────────────────┘
                                          (Incompatible Interface)
```

### Facade Pattern - Patient Operations

```
┌─────────────────────────────────────────────────────────────────────────┐
                        FACADE PATTERN                                  
└─────────────────────────────────────────────────────────────────────────┘

                    <<UI>>
              ┌──────────────────┐
              PatientDashboard
               (Client)       
              ├──────────────────┤
              + OnInitialized()│
              + LoadDashboard()│
              └──────────────────┘
                      
                       uses (simplified interface)
                       ▼
              ┌──────────────────────────────────┐
                     <<Facade>>               
                   PatientFacade              
              ├──────────────────────────────────┤
              - _patientProfileService        
              - _conversationService          
              - _medicalRecordService         
              - _prescriptionService          
              - _consultationService          
              - _activityLogService           
              - _documentService              
              - _workflowService              
              ├──────────────────────────────────┤
              + GetDashboardDataAsync()       
              + StartConsultationAsync()      
              + GetMedicalHistoryAsync()      
              + GetPatientRecordsAsync()      
              + UploadMedicalDocumentAsync()  
              + SendMessageAndGetRecommend...()│
              └──────────────────────────────────┘
                      
                       coordinates
                       ▼
         ┌─────────────┬─────────────┬─────────────┐
                                            
┌────────────────┐ ┌────────────────┐ ┌────────────────┐
<<Service>>    <<Service>>    <<Service>>   
PatientProfile Conversation   MedicalRecord 
Service        Service        Service       
├────────────────┤ ├────────────────┤ ├────────────────┤
+ GetByUserId()+ GetByPatient()+ GetByPatient()│
+ UpdateAsync()+ CreateAsync()+ CreateAsync()│
└────────────────┘ └────────────────┘ └────────────────┘

         ┌─────────────┬─────────────┬─────────────┐
                                            
┌────────────────┐ ┌────────────────┐ ┌────────────────┐
<<Service>>    <<Service>>    <<Service>>   
Prescription   Consultation   ActivityLog   
Service        Service        Service       
├────────────────┤ ├────────────────┤ ├────────────────┤
+ GetByPatient()+ GetByPatient()+ LogActivity()│
+ CreateAsync()+ CreateAsync()+ GetLogs()   
└────────────────┘ └────────────────┘ └────────────────┘

Output DTO:
┌──────────────────────────┐
PatientDashboardData    
├──────────────────────────┤
- Profile               
- RecentConversations   
- MedicalRecords        
- ActivePrescriptions   
- UpcomingAppointment   
- RecentHealthMetric    
└──────────────────────────┘
```

### Singleton Pattern - Database Client

```
┌─────────────────────────────────────────────────────────────────────────┐
                        SINGLETON PATTERN                               
└─────────────────────────────────────────────────────────────────────────┘

                    <<UI>>
              ┌──────────────────┐
                Any Page      
               (Client)       
              └──────────────────┘
                      
                       uses
                       ▼
              ┌──────────────────────────────┐
                   <<Singleton>>          
                     DbClient             
              ├──────────────────────────────┤
              - _instance: DbClient       ◄─────┐
              - _lock: object                  
              - _connectionString              
              ├──────────────────────────────┤     
              - DbClient() (private)            ensures
              + Instance: DbClient        ──────┘ single
              + GetDb(): ApplicationDbCtx         instance
              └──────────────────────────────┘

Thread-safe lazy initialization ensures only ONE instance exists
```

---

## UML Stereotypes Used

### Standard Stereotypes

| Stereotype | Meaning | Usage |
|------------|---------|-------|
| `<<UI>>` | User Interface Component | Blazor pages, components that interact with users |
| `<<Entity>>` | Domain Model | Database entities, data classes |
| `<<Service>>` | Business Logic | Service layer classes |
| `<<Interface>>` | Contract Definition | Interfaces defining contracts |
| `<<Facade>>` | Facade Pattern | Simplified interface to subsystems |
| `<<Context>>` | Strategy Context | Manages strategy selection |
| `<<Strategy>>` | Strategy Implementation | Concrete strategy algorithms |
| `<<Adapter>>` | Adapter Pattern | Converts between interfaces |
| `<<Adaptee>>` | Adapted Class | Existing class with incompatible interface |
| `<<Singleton>>` | Singleton Pattern | Ensures single instance |
| `<<DTO>>` | Data Transfer Object | Objects for transferring data |
| `<<Enumeration>>` | Enum Type | Enumeration types |

### Relationship Symbols

| Symbol | Meaning |
|--------|---------|
| `───>` | Association (uses) |
| `◄───` | Dependency |
| `△` | Inheritance (extends/implements) |
| `◄──┐` | Composition (strong ownership) |
| `◄──○` | Aggregation (weak ownership) |

---

## Client Types in This System

### 1. UI Clients (Blazor Pages/Components)

```
<<UI>>
┌──────────────────────┐
PatientDashboard      ← Uses PatientFacade
ConsultationPage      ← Uses AiModelContext
DoctorSearchPage      ← Uses DoctorRecommendationService
AdminPanel            ← Uses AdminFacade
└──────────────────────┘
```

### 2. Service Clients (Other Services)

```
<<Service>>
┌──────────────────────┐
AiAssistantService    ← Uses AiModelContext
WorkflowService       ← Uses DoctorRecommendationService
ConsultationService   ← Uses MessageService
└──────────────────────┘
```

### 3. API Clients (External Systems)

```
<<External>>
┌──────────────────────┐
Mobile App            ← Calls REST API
Third-party System    ← Integrates via API
└──────────────────────┘
```

---

## Design Pattern Summary

| Pattern | Context Class | Interface/Base | Concrete Implementations | Client |
|---------|--------------|----------------|-------------------------|--------|
| **Strategy** | `DoctorRecommendationService` | `IDoctorMatchingStrategy` | `BalancedMatchingStrategy`, `SymptomBasedMatchingStrategy`, `SpecializationBasedMatchingStrategy` | `<<UI>>` PatientPage |
| **Strategy + Adapter** | `AiModelContext` | `IAiModelStrategy` | `Gemma4Strategy`, `MiniMaxStrategy`, `NemotronStrategy` | `<<UI>>` ConsultationPage |
| **Adapter** | `BaseAiModelAdapter` | `IAiModelStrategy` | Concrete strategies | `AiModelContext` |
| **Facade** | `PatientFacade` | N/A | Coordinates 8+ services | `<<UI>>` PatientDashboard |
| **Singleton** | `DbClient` | N/A | Single instance | All services |

