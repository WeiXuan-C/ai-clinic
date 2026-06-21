using System.Text.Json;
using System.Text.RegularExpressions;
using ai_clinic.Models;
using ai_clinic.Services.DoctorRecommendation;
using ai_clinic.Services.DoctorRecommendation.Strategies;

namespace ai_clinic.Services;

/// <summary>
/// Patient consultation workflow service
/// Handles the complete flow from AI dialogue to doctor recommendation to consultation creation
/// 
/// Data Storage Strategy (no new tables created, leveraging existing tables):
/// 
/// 1. messages table:
///    - Store patient messages (sender_type='patient')
///    - Store AI responses (sender_type='ai', ai_model_used, ai_confidence_score)
///    - Store doctor messages (sender_type='doctor')
/// 
/// 2. medical_records table:
///    - AI phase: record_type='AI Consultation', created_by_doctor_id=NULL
///      - content: AI-generated summary
///      - diagnosis_description: symptoms + severity + specialty
///    - Doctor phase: record_type='Consultation Note', created_by_doctor_id=doctor_id
///      - Complete medical record
/// 
/// 3. conversations table:
///    - initial_symptoms: JSON array storing extracted symptoms
///    - ai_suggested_specialization: AI-recommended specialty
///    - status: active/archived/closed
///    - assigned_doctor_id: NULL (AI dialogue) or doctor_id (doctor dialogue)
/// 
/// 4. consultation_notes table:
///    - Created when user selects doctor (is_finalized=FALSE)
///    - Updated when doctor completes consultation (is_finalized=TRUE)
///    - symptoms: JSON array format
/// 
/// 5. prescriptions table:
///    - Created when doctor issues prescription
///    - Linked to consultation_note_id
/// 
/// AiSymptomAnalysis is an in-memory DTO, no database table needed
/// </summary>
public class PatientConsultationWorkflowService
{
    private readonly AiAssistantService _aiAssistantService;
    private readonly MedicalRecordService _medicalRecordService;
    private readonly DoctorRecommendationService _doctorRecommendationService;
    private readonly ConversationService _conversationService;
    private readonly MessageService _messageService;
    private readonly ConsultationService _consultationService;

    public PatientConsultationWorkflowService(
        AiAssistantService aiAssistantService,
        MedicalRecordService medicalRecordService,
        DoctorRecommendationService doctorRecommendationService,
        ConversationService conversationService,
        MessageService messageService,
        ConsultationService consultationService)
    {
        _aiAssistantService = aiAssistantService;
        _medicalRecordService = medicalRecordService;
        _doctorRecommendationService = doctorRecommendationService;
        _conversationService = conversationService;
        _messageService = messageService;
        _consultationService = consultationService;
    }

    /// <summary>
    /// Process patient message and return AI analysis result (including structured symptom extraction)
    /// 
    /// Return format example:
    /// {
    ///   "symptoms": ["chest pain", "shortness of breath"],
    ///   "condition": "Cardiology",
    ///   "severity": "high",
    ///   "summary": "Patient experiencing chest pain and breathing difficulty",
    ///   "aiResponse": "Complete AI response content..."
    /// }
    /// </summary>
    public async Task<AiSymptomAnalysis> ProcessPatientMessageAsync(
        Guid conversationId,
        Guid patientId,
        string patientMessage)
    {
        Console.WriteLine("=== [WORKFLOW] ProcessPatientMessageAsync Started ===");
        Console.WriteLine($"[WORKFLOW] Conversation ID: {conversationId}");
        Console.WriteLine($"[WORKFLOW] Patient ID: {patientId}");
        Console.WriteLine($"[WORKFLOW] Message: {patientMessage}");

        // 1. Save patient message
        await _messageService.CreatePatientMessageAsync(conversationId, patientId, patientMessage);

        // 2. Get AI response
        var aiResponse = await _aiAssistantService.GenerateMedicalResponseAsync(patientMessage);
        
        // 3. Analyze symptoms and extract structured information (key step: extract symptoms, specialty, severity)
        var analysis = await AnalyzeSymptomsAsync(patientMessage, aiResponse);
        
        // 4. Save AI message
        await _messageService.CreateAiMessageAsync(
            conversationId, 
            analysis.AiResponse,
            _aiAssistantService.CurrentModelName,
            analysis.ConfidenceScore);

        // 5. Save to medical record (medical_records table, record_type='AI Consultation')
        await SaveToMedicalRecordAsync(patientId, conversationId, analysis);

        Console.WriteLine("[WORKFLOW] ProcessPatientMessageAsync Completed");
        Console.WriteLine($"[WORKFLOW] Extracted Symptoms: {string.Join(", ", analysis.Symptoms)}");
        Console.WriteLine($"[WORKFLOW] Condition: {analysis.Condition}, Severity: {analysis.Severity}");
        
        return analysis;
    }

    /// <summary>
    /// Analyze symptoms and extract structured information
    /// </summary>
    private async Task<AiSymptomAnalysis> AnalyzeSymptomsAsync(string patientMessage, string aiResponse)
    {
        Console.WriteLine("[WORKFLOW] Analyzing symptoms...");

        // Use AI to extract structured information
        var extractionPrompt = $@"Based on the patient's message, extract structured information in JSON format.

Patient Message: ""{patientMessage}""

AI Response: ""{aiResponse}""

Extract the following information and respond ONLY with valid JSON (no markdown, no code blocks):
{{
  ""symptoms"": [""symptom1"", ""symptom2""],
  ""condition"": ""suggested medical specialization (e.g., Cardiology, Neurology, General Practice)"",
  ""severity"": ""low/medium/high/emergency"",
  ""summary"": ""brief summary of the patient's condition""
}}

Rules:
- symptoms: list of specific symptoms mentioned
- condition: the most appropriate medical specialization
- severity: assess urgency (low=routine, medium=should see doctor soon, high=urgent, emergency=immediate attention)
- summary: 1-2 sentence summary

Respond with ONLY the JSON object, nothing else.";

        var extractionResponse = await _aiAssistantService.GenerateResponseAsync(
            extractionPrompt,
            "You are a medical information extraction assistant. Extract structured data from patient conversations.",
            temperature: 0.3,
            maxTokens: 500);

        Console.WriteLine($"[WORKFLOW] Extraction response: {extractionResponse}");

        // Parse JSON response
        var analysis = ParseAiExtractionResponse(extractionResponse, aiResponse);
        
        Console.WriteLine($"[WORKFLOW] Extracted {analysis.Symptoms.Count} symptoms, condition: {analysis.Condition}, severity: {analysis.Severity}");
        
        return analysis;
    }

    /// <summary>
    /// Parse AI-extracted JSON response
    /// </summary>
    private AiSymptomAnalysis ParseAiExtractionResponse(string jsonResponse, string originalAiResponse)
    {
        try
        {
            // Clean up response, remove possible markdown code block markers
            var cleanJson = jsonResponse.Trim();
            cleanJson = Regex.Replace(cleanJson, @"^```json\s*", "", RegexOptions.Multiline);
            cleanJson = Regex.Replace(cleanJson, @"^```\s*", "", RegexOptions.Multiline);
            cleanJson = Regex.Replace(cleanJson, @"\s*```$", "", RegexOptions.Multiline);
            cleanJson = cleanJson.Trim();

            Console.WriteLine($"[WORKFLOW] Cleaned JSON: {cleanJson}");

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var extracted = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(cleanJson, options);

            if (extracted == null)
            {
                throw new Exception("Failed to deserialize JSON");
            }

            var analysis = new AiSymptomAnalysis
            {
                AiResponse = originalAiResponse,
                ConfidenceScore = 0.8m
            };

            // Extract symptoms
            if (extracted.ContainsKey("symptoms") && extracted["symptoms"].ValueKind == JsonValueKind.Array)
            {
                analysis.Symptoms = extracted["symptoms"]
                    .EnumerateArray()
                    .Select(s => s.GetString() ?? "")
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();
            }

            // Extract specialty
            if (extracted.ContainsKey("condition"))
            {
                analysis.Condition = extracted["condition"].GetString();
            }

            // Extract severity
            if (extracted.ContainsKey("severity"))
            {
                var severity = extracted["severity"].GetString()?.ToLower() ?? "medium";
                analysis.Severity = severity;
            }

            // Extract summary
            if (extracted.ContainsKey("summary"))
            {
                analysis.Summary = extracted["summary"].GetString() ?? "";
            }

            return analysis;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WORKFLOW ERROR] Failed to parse AI extraction: {ex.Message}");
            
            // Return default analysis result
            return new AiSymptomAnalysis
            {
                AiResponse = originalAiResponse,
                Symptoms = new List<string> { "general symptoms" },
                Condition = "General Practice",
                Severity = "medium",
                Summary = "Patient requires medical consultation",
                ConfidenceScore = 0.5m
            };
        }
    }

    /// <summary>
    /// Save AI analysis to medical record
    /// 
    /// Data storage strategy:
    /// - record_type = "AI Consultation" (identifies as AI-generated)
    /// - created_by_doctor_id = NULL (indicates not created by doctor)
    /// - content = AI-generated summary
    /// - diagnosis_description = symptom list + severity + recommended specialty
    /// 
    /// Also updates AI-related fields in conversations table
    /// </summary>
    private async Task SaveToMedicalRecordAsync(
        Guid patientId,
        Guid conversationId,
        AiSymptomAnalysis analysis)
    {
        Console.WriteLine("[WORKFLOW] Saving AI analysis to medical_records table...");

        // Build detailed diagnosis description (containing all AI analysis information)
        var diagnosisDescription = $@"AI Analysis Results:
Symptoms: {string.Join(", ", analysis.Symptoms)}
Suggested Specialization: {analysis.Condition ?? "General Practice"}
Severity Level: {analysis.Severity}
Confidence Score: {analysis.ConfidenceScore:P0}

This is an AI-generated preliminary assessment. Professional medical consultation is recommended.";

        var record = new MedicalRecord
        {
            PatientId = patientId,
            ConversationId = conversationId,
            CreatedByDoctorId = null, // NULL indicates AI-generated, not created by doctor
            RecordType = "AI Consultation",
            Title = $"AI Consultation - {analysis.Condition ?? "General"}",
            Content = analysis.Summary, // AI-generated summary
            DiagnosisDescription = diagnosisDescription, // Detailed analysis results
            RecordDate = DateTime.UtcNow
        };

        await _medicalRecordService.CreateAsync(record);
        Console.WriteLine($"[WORKFLOW] Medical record created: {record.Id}");

        // Also update AI-related fields in conversations table
        await UpdateConversationWithAiAnalysisAsync(conversationId, analysis);
    }

    /// <summary>
    /// Update conversation table with AI analysis fields
    /// </summary>
    private async Task UpdateConversationWithAiAnalysisAsync(
        Guid conversationId,
        AiSymptomAnalysis analysis)
    {
        using var db = Data.DbClient.Instance.GetDb();
        var conversation = await db.Conversations.FindAsync(conversationId);
        
        if (conversation != null)
        {
            // Convert symptom list to JSON string for storage
            conversation.InitialSymptoms = System.Text.Json.JsonSerializer.Serialize(analysis.Symptoms);
            conversation.AiSuggestedSpecialization = analysis.Condition;
            conversation.RequiredSpecialization = analysis.Condition;
            conversation.AiConfidenceScore = analysis.ConfidenceScore;
            conversation.UpdatedAt = DateTime.UtcNow;
            
            await db.SaveChangesAsync();
            Console.WriteLine($"[WORKFLOW] Conversation updated with AI analysis");
        }
    }

    /// <summary>
    /// Get recommended doctor list
    /// </summary>
    public async Task<List<DoctorMatchResult>> GetRecommendedDoctorsAsync(AiSymptomAnalysis analysis)
    {
        Console.WriteLine("[WORKFLOW] Getting recommended doctors...");

        var criteria = new DoctorSearchCriteria
        {
            Symptoms = analysis.Symptoms,
            PreferredSpecialization = analysis.Condition,
            MaxResults = 5
        };

        // Select different matching strategies based on severity
        IDoctorMatchingStrategy strategy = analysis.Severity switch
        {
            "emergency" or "high" => new SpecializationBasedMatchingStrategy(),
            "low" => new BalancedMatchingStrategy(),
            _ => new SymptomBasedMatchingStrategy()
        };

        _doctorRecommendationService.MatchingStrategy = strategy;
        var doctors = await _doctorRecommendationService.GetRecommendedDoctorsAsync(criteria);

        Console.WriteLine($"[WORKFLOW] Found {doctors.Count} recommended doctors");
        return doctors;
    }

    /// <summary>
    /// Create consultation with doctor (called after user selects doctor)
    /// 
    /// Data storage strategy:
    /// 1. Create new conversation (assigned_doctor_id = doctor_id)
    /// 2. Send summary message to messages table (sender_type = 'patient')
    /// 3. Update original AI conversation status to archived
    /// 4. Create initial consultation_note (is_finalized = FALSE)
    /// 
    /// Returns newly created doctor conversation
    /// </summary>
    public async Task<Conversation> CreateDoctorConsultationAsync(
        Guid patientId,
        Guid doctorId,
        Guid aiConversationId,
        AiSymptomAnalysis analysis)
    {
        Console.WriteLine("=== [WORKFLOW] CreateDoctorConsultationAsync Started ===");
        Console.WriteLine($"[WORKFLOW] Patient ID: {patientId}");
        Console.WriteLine($"[WORKFLOW] Doctor ID: {doctorId}");
        Console.WriteLine($"[WORKFLOW] AI Conversation ID: {aiConversationId}");

        // 1. Get AI conversation message history
        var aiMessages = await _messageService.GetByConversationIdAsync(aiConversationId);
        Console.WriteLine($"[WORKFLOW] Retrieved {aiMessages.Count} messages from AI conversation");
        
        // 2. Generate conversation summary (initial message to send to doctor)
        var conversationSummary = await GenerateConversationSummaryAsync(aiMessages, analysis);

        // 3. Create new conversation with doctor
        var doctorConversation = await _conversationService.CreateDoctorConversationAsync(
            patientId,
            doctorId,
            conversationSummary); // Summary as initial message

        Console.WriteLine($"[WORKFLOW] New doctor conversation created: {doctorConversation.Id}");

        // 4. Copy AI analysis symptom information to new conversation
        await CopyAiAnalysisToNewConversationAsync(doctorConversation.Id, analysis);

        // 5. Update original AI conversation status to archived
        await _conversationService.UpdateStatusAsync(aiConversationId, ConversationStatus.Archived);
        Console.WriteLine($"[WORKFLOW] Original AI conversation {aiConversationId} archived");

        // 6. Create initial consultation record (pending doctor completion, is_finalized=FALSE)
        var consultationNote = await CreateInitialConsultationNoteAsync(
            doctorConversation.Id,
            patientId,
            doctorId,
            analysis);

        Console.WriteLine($"[WORKFLOW] Initial consultation note created: {consultationNote.Id}");
        Console.WriteLine("=== [WORKFLOW] CreateDoctorConsultationAsync Completed ===");
        
        return doctorConversation;
    }

    /// <summary>
    /// Copy AI analysis information to new doctor conversation
    /// </summary>
    private async Task CopyAiAnalysisToNewConversationAsync(
        Guid conversationId,
        AiSymptomAnalysis analysis)
    {
        using var db = Data.DbClient.Instance.GetDb();
        var conversation = await db.Conversations.FindAsync(conversationId);
        
        if (conversation != null)
        {
            conversation.InitialSymptoms = System.Text.Json.JsonSerializer.Serialize(analysis.Symptoms);
            conversation.AiSuggestedSpecialization = analysis.Condition;
            conversation.RequiredSpecialization = analysis.Condition;
            conversation.AiConfidenceScore = analysis.ConfidenceScore;
            conversation.UpdatedAt = DateTime.UtcNow;
            
            await db.SaveChangesAsync();
            Console.WriteLine($"[WORKFLOW] AI analysis copied to new conversation");
        }
    }

    /// <summary>
    /// Generate conversation summary to send to doctor
    /// </summary>
    private async Task<string> GenerateConversationSummaryAsync(
        List<Message> messages,
        AiSymptomAnalysis analysis)
    {
        Console.WriteLine("[WORKFLOW] Generating conversation summary...");

        var conversationText = string.Join("\n", messages.Select(m =>
            $"{m.SenderType}: {m.Content}"));

        var summaryPrompt = $@"Summarize this patient-AI conversation for a doctor. Be concise and professional.

Conversation:
{conversationText}

AI Analysis:
- Symptoms: {string.Join(", ", analysis.Symptoms)}
- Suggested Specialization: {analysis.Condition}
- Severity: {analysis.Severity}
- Summary: {analysis.Summary}

Create a brief professional summary (3-4 sentences) that a doctor can quickly read to understand the patient's situation.";

        var summary = await _aiAssistantService.GenerateResponseAsync(
            summaryPrompt,
            "You are a medical documentation assistant. Create concise, professional summaries for doctors.",
            temperature: 0.5,
            maxTokens: 300);

        var fullMessage = $@"**Patient Consultation Summary**

{summary}

**Key Information:**
- Symptoms: {string.Join(", ", analysis.Symptoms)}
- Suggested Specialization: {analysis.Condition}
- Severity Level: {analysis.Severity}

**AI Analysis:**
{analysis.Summary}

---
This patient has been referred to you based on the AI preliminary assessment. Please review and provide your professional consultation.";

        Console.WriteLine("[WORKFLOW] Conversation summary generated");
        return fullMessage;
    }

    /// <summary>
    /// Create initial consultation record
    /// 
    /// Data storage strategy:
    /// - is_finalized = FALSE (indicates pending doctor completion)
    /// - symptoms = JSON array format symptom list
    /// - diagnosis = "Pending - Referred from AI: {specialty}"
    /// - treatment_plan = "To be determined by doctor"
    /// 
    /// Doctor will update this record later to fill in complete diagnosis and treatment plan
    /// </summary>
    private async Task<ConsultationNote> CreateInitialConsultationNoteAsync(
        Guid conversationId,
        Guid patientId,
        Guid doctorId,
        AiSymptomAnalysis analysis)
    {
        Console.WriteLine("[WORKFLOW] Creating initial consultation note...");

        var note = new ConsultationNote
        {
            ConversationId = conversationId,
            PatientId = patientId,
            DoctorId = doctorId,
            Symptoms = JsonSerializer.Serialize(analysis.Symptoms), // JSON array format
            Diagnosis = $"Pending - Referred from AI: {analysis.Condition ?? "General Practice"}",
            TreatmentPlan = "To be determined by doctor",
            FollowUpInstructions = $"AI Preliminary Assessment:\n- Severity: {analysis.Severity}\n- Summary: {analysis.Summary}",
            IsFinalized = false // Pending doctor completion
        };

        await _consultationService.CreateAsync(note);
        Console.WriteLine($"[WORKFLOW] Initial consultation note created: {note.Id}");
        Console.WriteLine($"[WORKFLOW] Doctor will complete this consultation note later");
        
        return note;
    }

    /// <summary>
    /// Complete workflow: from patient message to doctor recommendation
    /// 
    /// Steps:
    /// 1. Process patient message
    /// 2. AI analysis and symptom extraction
    /// 3. Save to medical_records
    /// 4. Recommend relevant doctors
    /// </summary>
    public async Task<PatientConsultationWorkflowResult> ExecuteFullWorkflowAsync(
        Guid conversationId,
        Guid patientId,
        string patientMessage)
    {
        Console.WriteLine("=== [WORKFLOW] ExecuteFullWorkflowAsync Started ===");

        // 1. Process patient message and get AI analysis
        var analysis = await ProcessPatientMessageAsync(conversationId, patientId, patientMessage);

        // 2. Get recommended doctors
        var recommendedDoctors = await GetRecommendedDoctorsAsync(analysis);

        Console.WriteLine("=== [WORKFLOW] ExecuteFullWorkflowAsync Completed ===");

        return new PatientConsultationWorkflowResult
        {
            Analysis = analysis,
            RecommendedDoctors = recommendedDoctors
        };
    }

    /// <summary>
    /// Complete workflow: from patient selecting doctor to creating consultation record
    /// 
    /// Steps:
    /// 1. Create new conversation with doctor
    /// 2. Generate and send conversation summary to doctor
    /// 3. Create initial consultation_note (is_finalized=FALSE)
    /// 4. Archive original AI conversation
    /// 
    /// This method is called after user selects a doctor from the recommended list
    /// </summary>
    public async Task<DoctorConsultationCreationResult> CreateDoctorConsultationWithNotesAsync(
        Guid patientId,
        Guid doctorId,
        Guid aiConversationId,
        AiSymptomAnalysis analysis)
    {
        Console.WriteLine("=== [WORKFLOW] CreateDoctorConsultationWithNotesAsync Started ===");

        // Create doctor consultation (includes conversation, summary message, consultation_note)
        var doctorConversation = await CreateDoctorConsultationAsync(
            patientId,
            doctorId,
            aiConversationId,
            analysis);

        // Get created consultation_note
        var consultationNotes = await _consultationService.GetByConversationIdAsync(doctorConversation.Id);
        var initialNote = consultationNotes.FirstOrDefault();

        Console.WriteLine("=== [WORKFLOW] CreateDoctorConsultationWithNotesAsync Completed ===");

        return new DoctorConsultationCreationResult
        {
            DoctorConversation = doctorConversation,
            InitialConsultationNote = initialNote,
            ConversationSummaryMessage = doctorConversation.Messages.FirstOrDefault()?.Content ?? ""
        };
    }
}

/// <summary>
/// Workflow execution result
/// </summary>
public class PatientConsultationWorkflowResult
{
    public AiSymptomAnalysis Analysis { get; set; } = new();
    public List<DoctorMatchResult> RecommendedDoctors { get; set; } = new();
}

/// <summary>
/// Doctor consultation creation result
/// </summary>
public class DoctorConsultationCreationResult
{
    /// <summary>
    /// Newly created doctor conversation
    /// </summary>
    public Conversation DoctorConversation { get; set; } = null!;

    /// <summary>
    /// Initial consultation record (is_finalized=FALSE, pending doctor completion)
    /// </summary>
    public ConsultationNote? InitialConsultationNote { get; set; }

    /// <summary>
    /// Conversation summary message sent to doctor
    /// </summary>
    public string ConversationSummaryMessage { get; set; } = string.Empty;
}

/// <summary>
/// AI analysis of patient symptoms structured response
/// This is an in-memory DTO, not a database table
/// 
/// Example output:
/// {
///   "symptoms": ["chest pain", "shortness of breath"],
///   "condition": "Cardiology",
///   "severity": "high",
///   "summary": "Patient experiencing chest pain and breathing difficulty"
/// }
/// </summary>
public class AiSymptomAnalysis
{
    /// <summary>
    /// Extracted symptom list
    /// </summary>
    public List<string> Symptoms { get; set; } = new List<string>();

    /// <summary>
    /// Recommended specialty (e.g., "Cardiology", "Neurology")
    /// </summary>
    public string? Condition { get; set; }

    /// <summary>
    /// Severity level ("low", "medium", "high", "emergency")
    /// </summary>
    public string Severity { get; set; } = "medium";

    /// <summary>
    /// AI-generated summary
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// AI's complete response content
    /// </summary>
    public string AiResponse { get; set; } = string.Empty;

    /// <summary>
    /// Confidence score (0-1)
    /// </summary>
    public decimal ConfidenceScore { get; set; } = 0.8m;
}
