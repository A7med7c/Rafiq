using System;
using System.Collections.Generic;
using System.Linq;

namespace Rafiq.Application.Features.PatientProfiles.Commands.Allergies.CreateAllergy;

public static class AllergyConflictChecker
{
    private static readonly Dictionary<string, string[]> AllergyToMedicines = new(StringComparer.OrdinalIgnoreCase)
    {
        { "حساسية البنسلين", new[] { "أموكسيسيلين", "أمبيسيلين", "أوجمنتين", "Amoxicillin", "Ampicillin", "Augmentin", "Penicillin" } },
        { "حساسية السيفالوسبورين", new[] { "سيفترياكسون", "سيفيكسيم", "سيفوروكسيم", "Ceftriaxone", "Cefixime", "Cefuroxime", "Cephalosporin" } },
        { "حساسية السلفا", new[] { "سيبترين", "سلفاميثوكسازول", "تريميثوبريم", "Septra", "Bactrim", "Sulfamethoxazole", "Trimethoprim", "Sulfa" } },
        { "حساسية الأسبرين", new[] { "أسبرين", "أسبرين بروتكت", "Aspirin", "Aspirin Protect" } },
        { "حساسية مضادات الالتهاب غير الستيرويدية (NSAIDs)", new[] { "بروفين", "كتافلام", "فولتارين", "نابروكسين", "كيتورولاك", "Ibuprofen", "Cataflam", "Voltaren", "Naproxen", "Ketorolac", "NSAID" } },
        { "حساسية الإيبوبروفين", new[] { "بروفين", "بروفينال", "Ibuprofen", "Brufen", "Profenal" } },
        { "حساسية الديكلوفيناك", new[] { "فولتارين", "كتافلام", "ديكلوفيناك", "Voltaren", "Cataflam", "Diclofenac" } },
        { "حساسية الباراسيتامول", new[] { "بانادول", "سيتال", "أدول", "Panadol", "Cetal", "Adol", "Paracetamol", "Acetaminophen" } },
        { "حساسية الليدوكايين", new[] { "ليدوكايين", "Lidocaine" } },
        { "حساسية اليود", new[] { "يود", "صبغات الأشعة", "Iodine" } },
        { "حساسية الهيبارين", new[] { "الهيبارين", "الإينوكسابارين", "Heparin", "Enoxaparin" } },
        { "حساسية الأنسولين", new[] { "أنسولين", "Insulin" } },
        { "حساسية اللاتكس", new[] { "لاتكس", "Latex" } },
        { "حساسية الفول السوداني", new[] { "الفول السوداني", "زيت الفول السوداني", "Peanut" } },
        { "حساسية الصويا", new[] { "صويا", "Soy" } },
        { "حساسية اللاكتوز", new[] { "لاكتوز", "Lactose" } },
        { "حساسية البيض", new[] { "لقاح", "Egg", "Vaccine" } },
        { "حساسية الجيلاتين", new[] { "جيلاتين", "Gelatin" } },
        { "حساسية السمك", new[] { "زيت السمك", "Fish oil", "Omega" } },
        { "حساسية المحار", new[] { "محار", "Shellfish" } }
    };

    public static List<string> GetConflictingMedicines(string allergyName, IEnumerable<string> currentMedicines)
    {
        var conflicts = new List<string>();
        
        var matchedKey = AllergyToMedicines.Keys.FirstOrDefault(k => 
            allergyName.Contains(k, StringComparison.OrdinalIgnoreCase) || 
            k.Contains(allergyName, StringComparison.OrdinalIgnoreCase));
            
        if (matchedKey != null)
        {
            var drugsToAvoid = AllergyToMedicines[matchedKey];
            foreach (var med in currentMedicines)
            {
                if (drugsToAvoid.Any(d => med.Contains(d, StringComparison.OrdinalIgnoreCase)))
                {
                    conflicts.Add(med);
                }
            }
        }
        
        return conflicts;
    }

    public static List<string> GetConflictingAllergies(string medicineName, IEnumerable<string> currentAllergies)
    {
        var conflicts = new List<string>();
        
        foreach (var allergy in currentAllergies)
        {
            var matchedKey = AllergyToMedicines.Keys.FirstOrDefault(k => 
                allergy.Contains(k, StringComparison.OrdinalIgnoreCase) || 
                k.Contains(allergy, StringComparison.OrdinalIgnoreCase));
                
            if (matchedKey != null)
            {
                var drugsToAvoid = AllergyToMedicines[matchedKey];
                if (drugsToAvoid.Any(d => medicineName.Contains(d, StringComparison.OrdinalIgnoreCase)))
                {
                    conflicts.Add(allergy);
                }
            }
        }
        
        return conflicts;
    }
}
