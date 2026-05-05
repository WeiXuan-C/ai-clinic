# Tables Data Usage Guide

## Overview

This document explains what data is stored in each of the three main tables: `consultation_notes`, `medical_records`, and `prescriptions`.

---

## 📋 Table 1: `consultation_notes`

### Purpose
Stores doctor's consultation records during patient-doctor interactions.

### When Created
- **Initial**: When patient selects a doctor (after AI recommendation)
- **Updated**: When doctor completes the consultation

### Data Stored

| Field | Type | Description | Example |
|-------|------|-------------|---------|
| `id` | UUID | Unique identifier | `550e8400-e29b-41d4-a716-446655440000` |
| `conversation_id` | UUID | Links to the conversation | `660e8400-e29b-41d4-a716-446655440001` |
| `doctor_id` | UUID | Doctor who created this note | `770e8400-e29b-41d4-a716-446655440002` |
| `patient_id` | UUID | Patient being consulted | `880e8400-e29b-41d4-a716-446655440003` |
| `symptoms` | JSON Array | List of symptoms | `["chest pain", "shortness of breath"]` |
| `physical_examination` | Text | Doctor's physical exam findings | `"Patient appears in distress, BP 140/90..."` |
| `diagnosis` | Text | Doctor's diagnosis | `"Acute Coronary Syndrome"` |
| `treatment_plan` | Text | Recommended treatment | `"Immediate hospitalization, ECG, blood tests..."` |
| `follow_up_instructions` | Text | Follow-up care instructions | `"Follow up in 1 week, avoid strenuous activity..."` |
| `prescription_id` | UUID | Links to prescription (if any) | `990e8400-e29b-41d4-a716-446655440004` |
| `is_finalized` | Boolean | Whether consultation is complete | `FALSE` (initial) → `TRUE` (completed) |
| `finalized_at` | Timestamp | When consultation was finalized | `2026-05-05 14:30:00` |
| `created_at` | Timestamp | When record was created | `2026-05-05 10:00:00` |
| `updated_at` | Timestamp | Last update time | `2026-05-05 14:30:00` |

### Lifecycle

```
1. Initial Creation (when patient selects doctor):
   - symptoms: ["chest pain", "shortness of breath"] (from AI)
   - diagnosis: "Pending - Referred from AI: Cardiology"
   - treatment_plan: "To be determined by doctor"
   - is_finalized: FALSE

2. Doctor Completes Consultation:
   - physical_examination: "Patient appears in distress..."
   - diagnosis: "Acute Coronary Syndrome"
   - treatment_plan: "Immediate hospitalization..."
   - follow_up_instructions: "Follow up in 1 week..."
   - is_finalized: TRUE
   - finalized_at: NOW()
```

### Example Data

**Initial (AI Referral)**:
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "conversation_id": "660e8400-e29b-41d4-a716-446655440001",
  "doctor_id": "770e8400-e29b-41d4-a716-446655440002",
  "patient_id": "880e8400-e29b-41d4-a716-446655440003",
  "symptoms": ["chest pain", "shortness of breath", "dizziness"],
  "physical_examination": null,
  "diagnosis": "Pending - Referred from AI: Cardiology",
  "treatment_plan": "To be determined by doctor",
  "follow_up_instructions": "AI Preliminary Assessment:\n- Severity: high\n- Summary: Patient experiencing chest pain and breathing difficulty",
  "is_finalized": false,
  "created_at": "2026-05-05 10:00:00"
}
```

**Completed (After Doctor Consultation)**:
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "conversation_id": "660e8400-e29b-41d4-a716-446655440001",
  "doctor_id": "770e8400-e29b-41d4-a716-446655440002",
  "patient_id": "880e8400-e29b-41d4-a716-446655440003",
  "symptoms": ["chest pain", "shortness of breath", "dizziness"],
  "physical_examination": "Patient appears in distress. BP 140/90, HR 95, RR 22. Chest auscultation reveals irregular heartbeat.",
  "diagnosis": "Acute Coronary Syndrome - suspected NSTEMI",
  "treatment_plan": "1. Immediate hospitalization\n2. ECG and cardiac enzymes\n3. Aspirin 325mg stat\n4. Nitroglycerin sublingual PRN\n5. Cardiology consult",
  "follow_up_instructions": "Follow up in cardiology clinic within 1 week after discharge. Avoid strenuous activity. Take medications as prescribed.",
  "prescription_id": "990e8400-e29b-41d4-a716-446655440004",
  "is_finalized": true,
  "finalized_at": "2026-05-05 14:30:00",
  "updated_at": "2026-05-05 14:30:00"
}
```

---

## 📄 Table 2: `medical_records`

### Purpose
Stores all medical records including AI consultations and doctor diagnoses.

### When Created
- **AI Stage**: After AI analyzes patient symptoms
- **Doctor Stage**: After doctor completes consultation (optional formal record)

### Data Stored

| Field | Type | Description | Example |
|-------|------|-------------|---------|
| `id` | UUID | Unique identifier | `aa0e8400-e29b-41d4-a716-446655440000` |
| `patient_id` | UUID | Patient this record belongs to | `880e8400-e29b-41d4-a716-446655440003` |
| `conversation_id` | UUID | Links to conversation (optional) | `660e8400-e29b-41d4-a716-446655440001` |
| `created_by_doctor_id` | UUID | Doctor who created (NULL for AI) | `NULL` (AI) or `doctor_id` (Doctor) |
| `record_type` | String | Type of record | `"AI Consultation"` or `"Consultation Note"` |
| `title` | String | Record title | `"AI Consultation - Cardiology"` |
| `content` | Text | Main content/summary | `"Patient experiencing chest pain and breathing difficulty"` |
| `diagnosis_code` | String | ICD-10 code (optional) | `"I20.0"` |
| `diagnosis_description` | Text | Detailed diagnosis | `"Symptoms: chest pain, shortness of breath\nSeverity: high"` |
| `medications` | JSON | Medications (optional) | `{"medications": [{"name": "Aspirin", "dosage": "81mg"}]}` |
| `record_date` | Date | Date of record | `2026-05-05` |
| `is_exported` | Boolean | Whether exported | `false` |
| `export_count` | Integer | Number of exports | `0` |
| `last_exported_at` | Timestamp | Last export time | `null` |
| `created_at` | Timestamp | Creation time | `2026-05-05 10:00:00` |
| `updated_at` | Timestamp | Last update time | `2026-05-05 10:00:00` |

### Key Distinction: AI vs Doctor Records

| Aspect | AI Record | Doctor Record |
|--------|-----------|---------------|
| `created_by_doctor_id` | **NULL** | **doctor_id** |
| `record_type` | `"AI Consultation"` | `"Consultation Note"`, `"Prescription"`, etc. |
| `diagnosis_code` | Usually NULL | ICD-10 code (e.g., `"I20.0"`) |
| `content` | AI-generated summary | Doctor's detailed notes |

### Example Data

**AI Consultation Record**:
```json
{
  "id": "aa0e8400-e29b-41d4-a716-446655440000",
  "patient_id": "880e8400-e29b-41d4-a716-446655440003",
  "conversation_id": "660e8400-e29b-41d4-a716-446655440001",
  "created_by_doctor_id": null,
  "record_type": "AI Consultation",
  "title": "AI Consultation - Cardiology",
  "content": "Patient experiencing chest pain and breathing difficulty. Immediate medical attention recommended.",
  "diagnosis_code": null,
  "diagnosis_description": "AI Analysis Results:\nSymptoms: chest pain, shortness of breath\nSuggested Specialization: Cardiology\nSeverity Level: high\nConfidence Score: 85%\n\nThis is an AI-generated preliminary assessment. Professional medical consultation is recommended.",
  "medications": null,
  "record_date": "2026-05-05",
  "created_at": "2026-05-05 10:00:00"
}
```

**Doctor Consultation Record**:
```json
{
  "id": "bb0e8400-e29b-41d4-a716-446655440001",
  "patient_id": "880e8400-e29b-41d4-a716-446655440003",
  "conversation_id": "660e8400-e29b-41d4-a716-446655440001",
  "created_by_doctor_id": "770e8400-e29b-41d4-a716-446655440002",
  "record_type": "Consultation Note",
  "title": "Cardiology Consultation - Acute Coronary Syndrome",
  "content": "Patient presented with chest pain and shortness of breath. Physical examination revealed elevated BP and irregular heartbeat. ECG shows ST-segment changes consistent with NSTEMI. Patient admitted for further cardiac workup and treatment.",
  "diagnosis_code": "I20.0",
  "diagnosis_description": "Acute Coronary Syndrome - Non-ST Elevation Myocardial Infarction (NSTEMI)",
  "medications": {
    "medications": [
      {
        "name": "Aspirin",
        "dosage": "81mg",
        "frequency": "once daily",
        "duration": "ongoing"
      },
      {
        "name": "Atorvastatin",
        "dosage": "40mg",
        "frequency": "once daily at bedtime",
        "duration": "ongoing"
      }
    ]
  },
  "record_date": "2026-05-05",
  "created_at": "2026-05-05 14:30:00"
}
```

---

## 💊 Table 3: `prescriptions`

### Purpose
Stores individual prescription items prescribed by doctors.

### When Created
- When doctor prescribes medication during or after consultation

### Data Stored

| Field | Type | Description | Example |
|-------|------|-------------|---------|
| `id` | UUID | Unique identifier | `990e8400-e29b-41d4-a716-446655440004` |
| `consultation_note_id` | UUID | Links to consultation note | `550e8400-e29b-41d4-a716-446655440000` |
| `patient_id` | UUID | Patient receiving prescription | `880e8400-e29b-41d4-a716-446655440003` |
| `doctor_id` | UUID | Doctor who prescribed | `770e8400-e29b-41d4-a716-446655440002` |
| `medication_name` | String | Name of medication | `"Aspirin"` |
| `dosage` | String | Dosage amount | `"81mg"` |
| `frequency` | String | How often to take | `"once daily"`, `"twice daily"`, `"as needed"` |
| `duration` | String | How long to take | `"7 days"`, `"2 weeks"`, `"ongoing"` |
| `instructions` | Text | Special instructions | `"Take with food. Avoid alcohol."` |
| `is_active` | Boolean | Whether prescription is active | `true` |
| `created_at` | Timestamp | When prescribed | `2026-05-05 14:30:00` |
| `updated_at` | Timestamp | Last update time | `2026-05-05 14:30:00` |

### Example Data

**Single Prescription**:
```json
{
  "id": "990e8400-e29b-41d4-a716-446655440004",
  "consultation_note_id": "550e8400-e29b-41d4-a716-446655440000",
  "patient_id": "880e8400-e29b-41d4-a716-446655440003",
  "doctor_id": "770e8400-e29b-41d4-a716-446655440002",
  "medication_name": "Aspirin",
  "dosage": "81mg",
  "frequency": "once daily",
  "duration": "ongoing",
  "instructions": "Take with food in the morning. Do not stop without consulting your doctor.",
  "is_active": true,
  "created_at": "2026-05-05 14:30:00"
}
```

**Multiple Prescriptions for Same Consultation**:
```json
[
  {
    "id": "990e8400-e29b-41d4-a716-446655440004",
    "consultation_note_id": "550e8400-e29b-41d4-a716-446655440000",
    "medication_name": "Aspirin",
    "dosage": "81mg",
    "frequency": "once daily",
    "duration": "ongoing",
    "instructions": "Take with food in the morning."
  },
  {
    "id": "991e8400-e29b-41d4-a716-446655440005",
    "consultation_note_id": "550e8400-e29b-41d4-a716-446655440000",
    "medication_name": "Atorvastatin",
    "dosage": "40mg",
    "frequency": "once daily at bedtime",
    "duration": "ongoing",
    "instructions": "Take at bedtime. Avoid grapefruit juice."
  },
  {
    "id": "992e8400-e29b-41d4-a716-446655440006",
    "consultation_note_id": "550e8400-e29b-41d4-a716-446655440000",
    "medication_name": "Nitroglycerin",
    "dosage": "0.4mg",
    "frequency": "as needed",
    "duration": "ongoing",
    "instructions": "Sublingual. Take for chest pain. If pain persists after 3 doses (5 min apart), call emergency."
  }
]
```

---

## 🔗 Relationships Between Tables

```
consultation_notes (1) ←→ (many) prescriptions
       ↓
   Links to conversation_id
       ↓
medical_records (also links to same conversation_id)
```

### Example: Complete Patient Record

**Scenario**: Patient with chest pain sees doctor

1. **AI Analysis** → `medical_records`
   ```
   record_type: "AI Consultation"
   created_by_doctor_id: NULL
   content: "Patient experiencing chest pain..."
   ```

2. **Doctor Consultation** → `consultation_notes`
   ```
   Initial (is_finalized: FALSE):
     diagnosis: "Pending - Referred from AI: Cardiology"
   
   Completed (is_finalized: TRUE):
     diagnosis: "Acute Coronary Syndrome"
     treatment_plan: "Immediate hospitalization..."
   ```

3. **Prescriptions** → `prescriptions`
   ```
   Prescription 1: Aspirin 81mg once daily
   Prescription 2: Atorvastatin 40mg once daily
   Prescription 3: Nitroglycerin 0.4mg as needed
   ```

4. **Formal Medical Record** → `medical_records`
   ```
   record_type: "Consultation Note"
   created_by_doctor_id: doctor_id
   diagnosis_code: "I20.0"
   content: "Patient presented with chest pain..."
   ```

---

## 📊 Data Flow Summary

```
Patient Message
    ↓
AI Analysis
    ↓
medical_records (AI Consultation)
    ↓
User Selects Doctor
    ↓
consultation_notes (Initial, is_finalized=FALSE)
    ↓
Doctor Completes Consultation
    ↓
consultation_notes (Updated, is_finalized=TRUE)
    ↓
prescriptions (Multiple records)
    ↓
medical_records (Doctor's formal record - optional)
```

---

## 🔍 Query Examples

### Get All Records for a Patient

```sql
-- AI Analysis Records
SELECT * FROM medical_records
WHERE patient_id = ?
  AND record_type = 'AI Consultation'
  AND created_by_doctor_id IS NULL;

-- Consultation Notes
SELECT * FROM consultation_notes
WHERE patient_id = ?
ORDER BY created_at DESC;

-- Prescriptions
SELECT p.* FROM prescriptions p
JOIN consultation_notes cn ON p.consultation_note_id = cn.id
WHERE cn.patient_id = ?
  AND p.is_active = true;

-- Doctor Medical Records
SELECT * FROM medical_records
WHERE patient_id = ?
  AND created_by_doctor_id IS NOT NULL;
```

### Get Complete Consultation Details

```sql
-- Get consultation with all prescriptions
SELECT 
    cn.*,
    d.full_name as doctor_name,
    p.medication_name,
    p.dosage,
    p.frequency,
    p.duration
FROM consultation_notes cn
JOIN users d ON cn.doctor_id = d.id
LEFT JOIN prescriptions p ON p.consultation_note_id = cn.id
WHERE cn.id = ?;
```

---

## 📝 Summary Table

| Table | Primary Use | Created By | Key Identifier |
|-------|-------------|------------|----------------|
| `consultation_notes` | Doctor's consultation record | Doctor (after patient selects) | `is_finalized` (FALSE → TRUE) |
| `medical_records` | AI analysis + Doctor formal records | AI (NULL) or Doctor (doctor_id) | `created_by_doctor_id` |
| `prescriptions` | Individual medications | Doctor | `consultation_note_id` |

---

## 💡 Key Points

1. **consultation_notes**: One record per doctor consultation, updated from initial to finalized
2. **medical_records**: Multiple records - AI analysis (created_by_doctor_id=NULL) and doctor records (created_by_doctor_id=doctor_id)
3. **prescriptions**: Multiple records per consultation, one per medication
4. All three tables link to the same `conversation_id` for traceability
5. Use `created_by_doctor_id IS NULL` to identify AI-generated records
6. Use `is_finalized` to distinguish initial vs completed consultations
