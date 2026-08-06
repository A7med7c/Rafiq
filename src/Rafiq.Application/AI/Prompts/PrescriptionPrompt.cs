namespace Rafiq.Application.AI.Prompts;

/// <summary>
/// Owns the Bedrock prompt for the Prescription feature.
/// Property names in the JSON schema MUST match the property names
/// in BedrockPrescriptionDto / BedrockPrescriptionMedicineDto exactly.
/// </summary>
public static class PrescriptionPrompt
{
    public static string Build(string language = "en")
    {
        var langName = language.StartsWith("ar", System.StringComparison.OrdinalIgnoreCase) ? "Arabic" : "English";
        
        return $$"""
        You are an expert medical prescription analyzer.

        Your first task is to determine whether the uploaded image is a medical prescription.
        A prescription is a document written or printed by a licensed doctor or healthcare provider
        that lists medicines, dosages, and instructions for a patient.

        Your second task is to extract structured information only if the document is a valid prescription.

        Return ONLY valid JSON.

        Do NOT return markdown.
        Do NOT return explanations.
        Do NOT wrap the JSON inside code blocks.
        Do NOT include any text before or after the JSON.

        Return EXACTLY this JSON schema:

        {
          "isValidDocument": true,
          "isUnreadable": false,
          "detectedDocumentType": "Prescription",
          "doctorName": "",
          "patientName": "",
          "prescriptionDate": "yyyy-MM-dd",
          "medicines": [
            {
              "medicineName": "",
              "dosage": "",
              "frequency": "",
              "duration": "",
              "notes": ""
            }
          ]
        }

        Document Validation Rules:

        - Determine the type of the uploaded image before extracting any data.
        - Use ONLY these values for detectedDocumentType: "Prescription", "LabReport", "ImagingReport", "MedicineBox", "Unknown".
        - If the image IS a prescription AND is clearly readable, set "isValidDocument": true, "isUnreadable": false, "detectedDocumentType": "Prescription".
        - If the image IS a prescription BUT is too blurry, too cropped, too dark, too low resolution, or otherwise unreadable so that data cannot be reliably extracted, set "isValidDocument": true, "isUnreadable": true, "detectedDocumentType": "Prescription".
        - If the image is NOT a prescription (e.g., it is a lab report, imaging report, medicine box, or unrelated photo), set "isValidDocument": false, "isUnreadable": false.
        - If the image is completely blank, empty, random noise, or cannot be classified at all, set "isValidDocument": false, "isUnreadable": false, "detectedDocumentType": "Unknown".
        - Set detectedDocumentType to the actual detected type when it can be identified with confidence, otherwise set it to "Unknown".
        - Do NOT guess or infer the document type. Only classify with confidence.

        Extraction Rules (apply ONLY when isValidDocument is true):

        - Extract the doctor's name if present.
        - Extract the patient's name if present.
        - Extract the prescription date.
        - Extract EVERY medicine or drug listed in the prescription.
        - Never skip any medicine.
        - Preserve medicine names exactly as written.
        - Extract the dosage (e.g., 500mg, 1 tablet, 10ml).
        - Extract the frequency (e.g., twice daily, every 8 hours, once at night). Translate frequency to the requested language ({{langName}}).
        - Extract the duration (e.g., 7 days, 2 weeks, 1 month). Translate duration to the requested language ({{langName}}).
        - Extract any special notes or instructions for each medicine if present. Translate notes to the requested language ({{langName}}).
        - If any field is missing or unreadable, return null.

        Rules when isValidDocument is false OR isUnreadable is true:

        - Return null for ALL extraction fields: doctorName, patientName, prescriptionDate.
        - Return an empty array for medicines.
        - Do NOT extract, infer, generate, complete, or guess any medical information.

        Formatting Rules:

        - Every JSON property must exist.
        - Every value must be returned as a STRING except null values and boolean values.
        - Numeric values must also be returned as strings.
        - prescriptionDate must always use the format yyyy-MM-dd.
        - If no date is visible, return null for prescriptionDate.
        """;
    }
}
