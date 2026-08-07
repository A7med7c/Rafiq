import os
import re

dtos = [
    r'c:\Users\ka\Desktop\ITI\Rafiq\Rafiq\src\Rafiq.Application\Features\LabReports\DTOs\BedrockLabReportDto.cs',
    r'c:\Users\ka\Desktop\ITI\Rafiq\Rafiq\src\Rafiq.Application\Features\Prescriptions\DTOs\BedrockPrescriptionDto.cs',
    r'c:\Users\ka\Desktop\ITI\Rafiq\Rafiq\src\Rafiq.Application\Features\UserMedicines\DTOs\BedrockMedicineBoxDto.cs',
    r'c:\Users\ka\Desktop\ITI\Rafiq\Rafiq\src\Rafiq.Application\Features\GeneralDocuments\DTOs\BedrockGeneralDocumentDto.cs'
]

props = '''
    public string? MedicalAttentionReason { get; set; }
    public string? RecommendedSpecialty { get; set; }
    public double? ConfidenceScore { get; set; }
}'''

for dto in dtos:
    with open(dto, 'r', encoding='utf-8') as f:
        content = f.read()
    if 'MedicalAttentionReason' not in content:
        content = content.replace('}\n', props + '\n', 1)
        with open(dto, 'w', encoding='utf-8') as f:
            f.write(content)

prompts = [
    r'c:\Users\ka\Desktop\ITI\Rafiq\Rafiq\src\Rafiq.Application\AI\Prompts\LabReportPrompt.cs',
    r'c:\Users\ka\Desktop\ITI\Rafiq\Rafiq\src\Rafiq.Application\AI\Prompts\PrescriptionPrompt.cs',
    r'c:\Users\ka\Desktop\ITI\Rafiq\Rafiq\src\Rafiq.Application\AI\Prompts\MedicineBoxPrompt.cs',
    r'c:\Users\ka\Desktop\ITI\Rafiq\Rafiq\src\Rafiq.Application\AI\Prompts\GeneralDocumentPrompt.cs'
]

warning_rules = '''        Medical Warning Rules:
        - Generate warnings ONLY from findings explicitly present in the uploaded medical record. NEVER infer, assume, or diagnose unsupported conditions.
        - The examples provided below are just conceptual. Evaluate overall clinical significance instead of strict matching.
        - Generate a warning ONLY when findings indicate medical evaluation or follow-up is likely needed (e.g. suspicious lung opacity, large pleural effusion, possible malignancy).
        - DO NOT generate a warning for minor/routine deviations (e.g. minor variations without clinical urgency).
        - If a warning is warranted, populate "medicalAttentionReason" with a concise explanation (maximum 40 words, non-medical terms) in {{langName}}.
        - Set "recommendedSpecialty" to EXACTLY ONE of the following, or null if confidence isn't high enough: Cardiologist, Pulmonologist, Endocrinologist, Nephrologist, Neurologist, OrthopedicSurgeon, GeneralSurgeon, EntSpecialist, Dermatologist, Gastroenterologist, Ophthalmologist, Urologist, Gynecologist, Hematologist, Oncologist, EmergencyDepartment.
        - Set "confidenceScore" between 0.00 and 1.00. This represents your confidence in the medical recommendation itself, NOT OCR/classification confidence.
        - If no warning is needed, return null for medicalAttentionReason, recommendedSpecialty, and confidenceScore.
        """;'''

for prompt in prompts:
    with open(prompt, 'r', encoding='utf-8') as f:
        content = f.read()
    if 'Medical Warning Rules' not in content:
        # replace old warning rule
        content = re.sub(r'WARNING RULE:.*?""";', warning_rules, content, flags=re.DOTALL)
        # replace json schema
        content = content.replace('"aiSummary": ""', '"aiSummary": "",\n          "medicalAttentionReason": null,\n          "recommendedSpecialty": null,\n          "confidenceScore": null')
        with open(prompt, 'w', encoding='utf-8') as f:
            f.write(content)
