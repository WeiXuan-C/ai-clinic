using System.Text.Json;
using System.Text.RegularExpressions;
using ai_clinic.Models;
using ai_clinic.Services.DoctorRecommendation;
using ai_clinic.Services.DoctorRecommendation.Strategies;

namespace ai_clinic.Services;

/// <summary>
/// 患者咨询工作流服务
/// 处理从AI对话到医生推荐再到咨询创建的完整流程
/// 
/// 数据存储策略（不创建新表，充分利用现有表）:
/// 
/// 1. messages 表:
///    - 存储患者消息 (sender_type='patient')
///    - 存储AI响应 (sender_type='ai', ai_model_used, ai_confidence_score)
///    - 存储医生消息 (sender_type='doctor')
/// 
/// 2. medical_records 表:
///    - AI阶段: record_type='AI Consultation', created_by_doctor_id=NULL
///      - content: AI生成的摘要
///      - diagnosis_description: 症状+严重程度+专科
///    - 医生阶段: record_type='Consultation Note', created_by_doctor_id=doctor_id
///      - 完整的医疗记录
/// 
/// 3. conversations 表:
///    - initial_symptoms: JSON数组，存储提取的症状
///    - ai_suggested_specialization: AI建议的专科
///    - status: active/archived/closed
///    - assigned_doctor_id: NULL(AI对话) 或 doctor_id(医生对话)
/// 
/// 4. consultation_notes 表:
///    - 用户选择医生后创建 (is_finalized=FALSE)
///    - 医生完成咨询后更新 (is_finalized=TRUE)
///    - symptoms: JSON数组格式
/// 
/// 5. prescriptions 表:
///    - 医生开具处方时创建
///    - 关联到 consultation_note_id
/// 
/// AiSymptomAnalysis 是内存中的DTO，不需要数据库表
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
    /// 处理患者消息并返回AI分析结果（包含结构化症状提取）
    /// 
    /// 返回格式示例:
    /// {
    ///   "symptoms": ["chest pain", "shortness of breath"],
    ///   "condition": "Cardiology",
    ///   "severity": "high",
    ///   "summary": "Patient experiencing chest pain and breathing difficulty",
    ///   "aiResponse": "完整的AI回复内容..."
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

        // 1. 保存患者消息
        await _messageService.CreatePatientMessageAsync(conversationId, patientId, patientMessage);

        // 2. 获取AI响应
        var aiResponse = await _aiAssistantService.GenerateMedicalResponseAsync(patientMessage);
        
        // 3. 分析症状并提取结构化信息（关键步骤：提取症状、专科、严重程度）
        var analysis = await AnalyzeSymptomsAsync(patientMessage, aiResponse);
        
        // 4. 保存AI消息
        await _messageService.CreateAiMessageAsync(
            conversationId, 
            analysis.AiResponse,
            _aiAssistantService.CurrentModelName,
            analysis.ConfidenceScore);

        // 5. 保存到医疗记录（medical_records表，record_type='AI Consultation'）
        await SaveToMedicalRecordAsync(patientId, conversationId, analysis);

        Console.WriteLine("[WORKFLOW] ProcessPatientMessageAsync Completed");
        Console.WriteLine($"[WORKFLOW] Extracted Symptoms: {string.Join(", ", analysis.Symptoms)}");
        Console.WriteLine($"[WORKFLOW] Condition: {analysis.Condition}, Severity: {analysis.Severity}");
        
        return analysis;
    }

    /// <summary>
    /// 分析症状并提取结构化信息
    /// </summary>
    private async Task<AiSymptomAnalysis> AnalyzeSymptomsAsync(string patientMessage, string aiResponse)
    {
        Console.WriteLine("[WORKFLOW] Analyzing symptoms...");

        // 使用AI提取结构化信息
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

        // 解析JSON响应
        var analysis = ParseAiExtractionResponse(extractionResponse, aiResponse);
        
        Console.WriteLine($"[WORKFLOW] Extracted {analysis.Symptoms.Count} symptoms, condition: {analysis.Condition}, severity: {analysis.Severity}");
        
        return analysis;
    }

    /// <summary>
    /// 解析AI提取的JSON响应
    /// </summary>
    private AiSymptomAnalysis ParseAiExtractionResponse(string jsonResponse, string originalAiResponse)
    {
        try
        {
            // 清理响应，移除可能的markdown代码块标记
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

            // 提取症状
            if (extracted.ContainsKey("symptoms") && extracted["symptoms"].ValueKind == JsonValueKind.Array)
            {
                analysis.Symptoms = extracted["symptoms"]
                    .EnumerateArray()
                    .Select(s => s.GetString() ?? "")
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();
            }

            // 提取专科
            if (extracted.ContainsKey("condition"))
            {
                analysis.Condition = extracted["condition"].GetString();
            }

            // 提取严重程度
            if (extracted.ContainsKey("severity"))
            {
                var severity = extracted["severity"].GetString()?.ToLower() ?? "medium";
                analysis.Severity = severity;
            }

            // 提取摘要
            if (extracted.ContainsKey("summary"))
            {
                analysis.Summary = extracted["summary"].GetString() ?? "";
            }

            return analysis;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WORKFLOW ERROR] Failed to parse AI extraction: {ex.Message}");
            
            // 返回默认分析结果
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
    /// 保存AI分析到医疗记录
    /// 
    /// 数据存储策略:
    /// - record_type = "AI Consultation" (标识为AI生成)
    /// - created_by_doctor_id = NULL (表示非医生创建)
    /// - content = AI生成的摘要
    /// - diagnosis_description = 症状列表 + 严重程度 + 建议专科
    /// 
    /// 同时更新 conversations 表的 AI 相关字段
    /// </summary>
    private async Task SaveToMedicalRecordAsync(
        Guid patientId,
        Guid conversationId,
        AiSymptomAnalysis analysis)
    {
        Console.WriteLine("[WORKFLOW] Saving AI analysis to medical_records table...");

        // 构建详细的诊断描述（包含所有AI分析信息）
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
            CreatedByDoctorId = null, // NULL表示AI生成，非医生创建
            RecordType = "AI Consultation",
            Title = $"AI Consultation - {analysis.Condition ?? "General"}",
            Content = analysis.Summary, // AI生成的摘要
            DiagnosisDescription = diagnosisDescription, // 详细的分析结果
            RecordDate = DateTime.UtcNow
        };

        await _medicalRecordService.CreateAsync(record);
        Console.WriteLine($"[WORKFLOW] Medical record created: {record.Id}");

        // 同时更新 conversations 表的 AI 相关字段
        await UpdateConversationWithAiAnalysisAsync(conversationId, analysis);
    }

    /// <summary>
    /// 更新对话表的AI分析字段
    /// </summary>
    private async Task UpdateConversationWithAiAnalysisAsync(
        Guid conversationId,
        AiSymptomAnalysis analysis)
    {
        using var db = Data.DbClient.Instance.GetDb();
        var conversation = await db.Conversations.FindAsync(conversationId);
        
        if (conversation != null)
        {
            // 将症状列表转换为JSON字符串存储
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
    /// 获取推荐的医生列表
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

        // 根据严重程度选择不同的匹配策略
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
    /// 创建与医生的咨询（用户选择医生后调用）
    /// 
    /// 数据存储策略:
    /// 1. 创建新的 conversation (assigned_doctor_id = doctor_id)
    /// 2. 发送摘要消息到 messages 表 (sender_type = 'patient')
    /// 3. 更新原AI对话状态为 archived
    /// 4. 创建初始 consultation_note (is_finalized = FALSE)
    /// 
    /// 返回新创建的医生对话
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

        // 1. 获取AI对话的历史消息
        var aiMessages = await _messageService.GetByConversationIdAsync(aiConversationId);
        Console.WriteLine($"[WORKFLOW] Retrieved {aiMessages.Count} messages from AI conversation");
        
        // 2. 生成对话摘要（发送给医生的初始消息）
        var conversationSummary = await GenerateConversationSummaryAsync(aiMessages, analysis);

        // 3. 创建与医生的新对话
        var doctorConversation = await _conversationService.CreateDoctorConversationAsync(
            patientId,
            doctorId,
            conversationSummary); // 摘要作为初始消息

        Console.WriteLine($"[WORKFLOW] New doctor conversation created: {doctorConversation.Id}");

        // 4. 复制AI分析的症状信息到新对话
        await CopyAiAnalysisToNewConversationAsync(doctorConversation.Id, analysis);

        // 5. 更新原AI对话状态为 archived
        await _conversationService.UpdateStatusAsync(aiConversationId, ConversationStatus.Archived);
        Console.WriteLine($"[WORKFLOW] Original AI conversation {aiConversationId} archived");

        // 6. 创建初始咨询记录（待医生完成，is_finalized=FALSE）
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
    /// 复制AI分析信息到新的医生对话
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
    /// 生成对话摘要发送给医生
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
    /// 创建初始咨询记录
    /// 
    /// 数据存储策略:
    /// - is_finalized = FALSE (表示待医生完成)
    /// - symptoms = JSON数组格式的症状列表
    /// - diagnosis = "Pending - Referred from AI: {专科}"
    /// - treatment_plan = "To be determined by doctor"
    /// 
    /// 医生稍后会更新这条记录，填写完整的诊断和治疗计划
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
            Symptoms = JsonSerializer.Serialize(analysis.Symptoms), // JSON数组格式
            Diagnosis = $"Pending - Referred from AI: {analysis.Condition ?? "General Practice"}",
            TreatmentPlan = "To be determined by doctor",
            FollowUpInstructions = $"AI Preliminary Assessment:\n- Severity: {analysis.Severity}\n- Summary: {analysis.Summary}",
            IsFinalized = false // 待医生完成
        };

        await _consultationService.CreateAsync(note);
        Console.WriteLine($"[WORKFLOW] Initial consultation note created: {note.Id}");
        Console.WriteLine($"[WORKFLOW] Doctor will complete this consultation note later");
        
        return note;
    }

    /// <summary>
    /// 完整工作流：从患者消息到医生推荐
    /// 
    /// 步骤:
    /// 1. 处理患者消息
    /// 2. AI分析并提取症状
    /// 3. 保存到medical_records
    /// 4. 推荐相关医生
    /// </summary>
    public async Task<PatientConsultationWorkflowResult> ExecuteFullWorkflowAsync(
        Guid conversationId,
        Guid patientId,
        string patientMessage)
    {
        Console.WriteLine("=== [WORKFLOW] ExecuteFullWorkflowAsync Started ===");

        // 1. 处理患者消息并获取AI分析
        var analysis = await ProcessPatientMessageAsync(conversationId, patientId, patientMessage);

        // 2. 获取推荐医生
        var recommendedDoctors = await GetRecommendedDoctorsAsync(analysis);

        Console.WriteLine("=== [WORKFLOW] ExecuteFullWorkflowAsync Completed ===");

        return new PatientConsultationWorkflowResult
        {
            Analysis = analysis,
            RecommendedDoctors = recommendedDoctors
        };
    }

    /// <summary>
    /// 完整工作流：从患者选择医生到创建咨询记录
    /// 
    /// 步骤:
    /// 1. 创建与医生的新对话
    /// 2. 生成并发送对话摘要给医生
    /// 3. 创建初始consultation_note (is_finalized=FALSE)
    /// 4. 归档原AI对话
    /// 
    /// 此方法在用户从推荐列表中选择医生后调用
    /// </summary>
    public async Task<DoctorConsultationCreationResult> CreateDoctorConsultationWithNotesAsync(
        Guid patientId,
        Guid doctorId,
        Guid aiConversationId,
        AiSymptomAnalysis analysis)
    {
        Console.WriteLine("=== [WORKFLOW] CreateDoctorConsultationWithNotesAsync Started ===");

        // 创建医生咨询（包含对话、摘要消息、consultation_note）
        var doctorConversation = await CreateDoctorConsultationAsync(
            patientId,
            doctorId,
            aiConversationId,
            analysis);

        // 获取创建的consultation_note
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
/// 工作流执行结果
/// </summary>
public class PatientConsultationWorkflowResult
{
    public AiSymptomAnalysis Analysis { get; set; } = new();
    public List<DoctorMatchResult> RecommendedDoctors { get; set; } = new();
}

/// <summary>
/// 医生咨询创建结果
/// </summary>
public class DoctorConsultationCreationResult
{
    /// <summary>
    /// 新创建的医生对话
    /// </summary>
    public Conversation DoctorConversation { get; set; } = null!;

    /// <summary>
    /// 初始咨询记录（is_finalized=FALSE，待医生完成）
    /// </summary>
    public ConsultationNote? InitialConsultationNote { get; set; }

    /// <summary>
    /// 发送给医生的对话摘要消息
    /// </summary>
    public string ConversationSummaryMessage { get; set; } = string.Empty;
}

/// <summary>
/// AI分析患者症状后的结构化响应
/// 这是一个内存DTO，不是数据库表
/// 
/// 示例输出:
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
    /// 提取的症状列表
    /// </summary>
    public List<string> Symptoms { get; set; } = new List<string>();

    /// <summary>
    /// 建议的专科（如 "Cardiology", "Neurology"）
    /// </summary>
    public string? Condition { get; set; }

    /// <summary>
    /// 严重程度（"low", "medium", "high", "emergency"）
    /// </summary>
    public string Severity { get; set; } = "medium";

    /// <summary>
    /// AI生成的摘要
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// AI的完整回复内容
    /// </summary>
    public string AiResponse { get; set; } = string.Empty;

    /// <summary>
    /// 置信度分数 (0-1)
    /// </summary>
    public decimal ConfidenceScore { get; set; } = 0.8m;
}
