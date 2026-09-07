using DentalSurgery.Domain.Entities;
using DentalSurgery.Domain.Enums;

namespace DentalSurgery.Infrastructure.Persistence.Seed;

/// <summary>Seed content for the medical, allergy, drug and template catalogues.</summary>
public static class ClinicalCatalogues
{
    public static List<MedicalCondition> MedicalConditions()
    {
        var list = new List<MedicalCondition>();
        var order = 0;

        void Add(string code, string name, string category, string? icd = null,
            bool prophylaxis = false, bool bleeding = false, bool anaesthesia = false,
            bool healing = false, bool adrenaline = false, bool steroid = false,
            AlertSeverity severity = AlertSeverity.Medium, string? guidance = null)
        {
            list.Add(new MedicalCondition
            {
                Code = code,
                Name = name,
                Category = category,
                Icd10Code = icd,
                RequiresAntibioticProphylaxis = prophylaxis,
                IncreasesBleedingRisk = bleeding,
                AffectsAnaesthesia = anaesthesia,
                AffectsHealing = healing,
                ContraindicatesAdrenaline = adrenaline,
                RequiresSteroidCover = steroid,
                DefaultSeverity = severity,
                ClinicalGuidance = guidance,
                SortOrder = order += 10,
                IsSystem = true
            });
        }

        // Cardiovascular
        Add("HTN", "Hypertension", "Cardiovascular", "I10", anaesthesia: true, adrenaline: true,
            guidance: "Check blood pressure before treatment. Defer elective care if 180/110 or above. Limit adrenaline.");
        Add("IHD", "Ischaemic heart disease", "Cardiovascular", "I25", anaesthesia: true, adrenaline: true,
            severity: AlertSeverity.High,
            guidance: "Short morning appointments. Have GTN spray available. Limit adrenaline to two cartridges.");
        Add("MI", "Previous myocardial infarction", "Cardiovascular", "I21", anaesthesia: true, adrenaline: true,
            severity: AlertSeverity.High, guidance: "Defer elective treatment for six months after the event.");
        Add("AF", "Atrial fibrillation", "Cardiovascular", "I48", bleeding: true, anaesthesia: true,
            severity: AlertSeverity.High, guidance: "Usually anticoagulated. Check INR or DOAC timing before surgery.");
        Add("CHF", "Congestive heart failure", "Cardiovascular", "I50", anaesthesia: true,
            severity: AlertSeverity.High, guidance: "Treat semi-upright. Avoid supine positioning.");
        Add("VALVE", "Prosthetic heart valve", "Cardiovascular", "Z95.2", prophylaxis: true, bleeding: true,
            severity: AlertSeverity.Critical, guidance: "Antibiotic prophylaxis indicated. Almost always anticoagulated.");
        Add("IE", "Previous infective endocarditis", "Cardiovascular", "I33", prophylaxis: true,
            severity: AlertSeverity.Critical, guidance: "Highest-risk group. Prophylaxis before all invasive dental procedures.");
        Add("PACE", "Cardiac pacemaker or ICD", "Cardiovascular", "Z95.0", severity: AlertSeverity.High,
            guidance: "Avoid electrosurgery, ultrasonic scalers and diathermy near the device.");
        Add("CVA", "Stroke or TIA", "Cardiovascular", "I63", bleeding: true, severity: AlertSeverity.High,
            guidance: "Usually on antiplatelet therapy. Defer elective care for six months after the event.");

        // Endocrine
        Add("DM1", "Type 1 diabetes mellitus", "Endocrine", "E10", healing: true, severity: AlertSeverity.High,
            guidance: "Morning appointments after a normal meal. Have glucose available. Healing may be delayed.");
        Add("DM2", "Type 2 diabetes mellitus", "Endocrine", "E11", healing: true,
            guidance: "Check recent HbA1c. Poor control increases periodontal and infection risk.");
        Add("THYR", "Thyroid disease", "Endocrine", "E03", anaesthesia: true, severity: AlertSeverity.Low,
            guidance: "Uncontrolled hyperthyroidism: avoid adrenaline.");
        Add("ADREN", "Adrenal insufficiency", "Endocrine", "E27", steroid: true, severity: AlertSeverity.High,
            guidance: "Consider steroid cover for surgical procedures. Risk of adrenal crisis.");

        // Haematological
        Add("ANTICOAG", "Anticoagulant therapy", "Haematological", "Z79.01", bleeding: true,
            severity: AlertSeverity.High, guidance: "Check INR within 24-72 hours for warfarin. Do not stop DOACs without advice.");
        Add("HAEM", "Haemophilia or bleeding disorder", "Haematological", "D66", bleeding: true,
            severity: AlertSeverity.Critical, guidance: "Liaise with the haematology team before any invasive procedure.");
        Add("ANAEM", "Anaemia", "Haematological", "D64", severity: AlertSeverity.Low);
        Add("LEUK", "Leukaemia or lymphoma", "Haematological", "C95", bleeding: true, healing: true,
            prophylaxis: true, severity: AlertSeverity.Critical,
            guidance: "Check current blood counts. Defer elective care during active treatment.");
        Add("SICKLE", "Sickle cell disease", "Haematological", "D57", anaesthesia: true, healing: true,
            severity: AlertSeverity.High, guidance: "Avoid hypoxia and dehydration. Avoid general anaesthesia where possible.");

        // Respiratory
        Add("ASTHMA", "Asthma", "Respiratory", "J45", anaesthesia: true,
            guidance: "Ask the patient to bring their inhaler. Avoid NSAIDs in aspirin-sensitive asthma.");
        Add("COPD", "Chronic obstructive pulmonary disease", "Respiratory", "J44", anaesthesia: true,
            severity: AlertSeverity.High, guidance: "Treat semi-upright. Avoid rubber dam if it worsens breathing.");
        Add("OSA", "Obstructive sleep apnoea", "Respiratory", "G47.33", anaesthesia: true,
            severity: AlertSeverity.High, guidance: "High risk with sedation. Specialist assessment advised.");

        // Gastrointestinal / hepatic / renal
        Add("GORD", "Gastro-oesophageal reflux disease", "Gastrointestinal", "K21", severity: AlertSeverity.Low,
            guidance: "Associated with dental erosion. Treat semi-upright.");
        Add("HEP", "Hepatitis B or C", "Hepatic", "B18", bleeding: true, severity: AlertSeverity.High,
            guidance: "Impaired clotting and drug metabolism. Standard precautions apply to all patients.");
        Add("CIRR", "Liver cirrhosis", "Hepatic", "K74", bleeding: true, healing: true,
            severity: AlertSeverity.High, guidance: "Avoid paracetamol at full dose. Reduced clotting factor synthesis.");
        Add("CKD", "Chronic kidney disease", "Renal", "N18", bleeding: true, healing: true,
            severity: AlertSeverity.High, guidance: "Adjust drug doses. Treat on the day after dialysis, not the same day.");
        Add("DIAL", "Renal dialysis", "Renal", "Z99.2", bleeding: true, prophylaxis: true,
            severity: AlertSeverity.High, guidance: "Avoid the arm with the fistula for blood pressure.");

        // Neurological / psychiatric
        Add("EPIL", "Epilepsy", "Neurological", "G40", severity: AlertSeverity.High,
            guidance: "Note seizure frequency and triggers. Gingival overgrowth is common with phenytoin.");
        Add("PARK", "Parkinson disease", "Neurological", "G20", guidance: "Movement may complicate treatment. Consider shorter visits.");
        Add("DEMEN", "Dementia", "Neurological", "F03", guidance: "Confirm capacity and consent arrangements.");
        Add("ANX", "Dental anxiety or phobia", "Psychiatric", "F40.2", severity: AlertSeverity.Low,
            guidance: "Consider behavioural techniques or sedation referral.");

        // Musculoskeletal / immune
        Add("RA", "Rheumatoid arthritis", "Musculoskeletal", "M06", healing: true,
            guidance: "Often on immunosuppressants. Consider shorter appointments for joint comfort.");
        Add("OSTEO", "Osteoporosis", "Musculoskeletal", "M81", healing: true, severity: AlertSeverity.High,
            guidance: "Check for antiresorptive therapy and MRONJ risk before extractions.");
        Add("JOINT", "Prosthetic joint replacement", "Musculoskeletal", "Z96.6",
            guidance: "Routine prophylaxis is not generally indicated. Follow current national guidance.");
        Add("HIV", "HIV infection", "Immune", "B20", healing: true, prophylaxis: true,
            severity: AlertSeverity.High, guidance: "Check CD4 count and viral load. Watch for oral candidiasis.");
        Add("IMMUNO", "Immunosuppression", "Immune", "D84.9", healing: true, prophylaxis: true,
            severity: AlertSeverity.High, guidance: "Increased infection risk. Consider antibiotic cover for surgery.");

        // Oncology / other
        Add("RADIO", "Radiotherapy to head or neck", "Oncology", "Z92.3", healing: true,
            severity: AlertSeverity.Critical,
            guidance: "Osteoradionecrosis risk. Avoid extractions; refer for specialist assessment.");
        Add("CHEMO", "Current chemotherapy", "Oncology", "Z51.1", bleeding: true, healing: true,
            prophylaxis: true, severity: AlertSeverity.Critical,
            guidance: "Check neutrophil and platelet counts before invasive treatment.");
        Add("BISPHOS", "Antiresorptive therapy", "Oncology", "Z79.83", healing: true,
            severity: AlertSeverity.High, guidance: "MRONJ risk. Prefer conservative alternatives to extraction.");
        Add("PREG", "Pregnancy", "Obstetric", "Z33", severity: AlertSeverity.High,
            guidance: "Avoid elective radiographs. Second trimester is the preferred window for treatment.");
        Add("XERO", "Xerostomia", "Oral", "K11.7", severity: AlertSeverity.Low,
            guidance: "High caries risk. Prescribe high-fluoride toothpaste and saliva substitutes.");
        Add("BRUX", "Bruxism", "Oral", "F45.8", severity: AlertSeverity.Low,
            guidance: "Consider an occlusal guard. Restorations are at increased risk of failure.");
        Add("TMD", "Temporomandibular disorder", "Oral", "M26.6", severity: AlertSeverity.Low,
            guidance: "Limit mouth opening time. Support the jaw during long procedures.");

        return list;
    }

    public static List<Allergen> Allergens()
    {
        var list = new List<Allergen>();
        var order = 0;

        void Add(string code, string name, AllergyType type, bool common = false, string? cross = null)
        {
            list.Add(new Allergen
            {
                Code = code, Name = name, AllergyType = type,
                IsCommonInDentistry = common, CrossReactants = cross,
                SortOrder = order += 10, IsSystem = true
            });
        }

        Add("PEN", "Penicillin", AllergyType.Drug, true, "Amoxicillin, ampicillin, co-amoxiclav, cephalosporins");
        Add("AMOX", "Amoxicillin", AllergyType.Drug, true, "Penicillin group");
        Add("CEPH", "Cephalosporins", AllergyType.Drug, false, "Penicillin group");
        Add("ERYTH", "Erythromycin", AllergyType.Drug, true, "Clarithromycin, azithromycin");
        Add("CLIND", "Clindamycin", AllergyType.Drug);
        Add("METRO", "Metronidazole", AllergyType.Drug, true);
        Add("NSAID", "NSAIDs", AllergyType.Drug, true, "Ibuprofen, diclofenac, naproxen, aspirin");
        Add("ASP", "Aspirin", AllergyType.Drug, true, "NSAID group");
        Add("CODE", "Codeine", AllergyType.Drug, false, "Opioid group");
        Add("SULF", "Sulphonamides", AllergyType.Drug);
        Add("LIDO", "Lidocaine", AllergyType.Anaesthetic, true, "Amide anaesthetics: articaine, prilocaine, mepivacaine");
        Add("ARTIC", "Articaine", AllergyType.Anaesthetic, true, "Amide anaesthetics");
        Add("PRILO", "Prilocaine", AllergyType.Anaesthetic, true, "Amide anaesthetics");
        Add("BENZ", "Benzocaine", AllergyType.Anaesthetic, true, "Ester anaesthetics: procaine, tetracaine");
        Add("EPIN", "Adrenaline", AllergyType.Drug, true);
        Add("LATEX", "Latex", AllergyType.Latex, true, "Banana, avocado, kiwi, chestnut");
        Add("NICKEL", "Nickel", AllergyType.Metal, true, "Stainless steel, some orthodontic alloys");
        Add("CHROM", "Chromium", AllergyType.Metal, false);
        Add("COBALT", "Cobalt", AllergyType.Metal, false, "Cobalt-chrome denture frameworks");
        Add("GOLD", "Gold", AllergyType.Metal);
        Add("MERC", "Mercury", AllergyType.Metal, true, "Dental amalgam");
        Add("ACRY", "Acrylic monomer", AllergyType.Material, true, "Denture base, temporary crowns");
        Add("CHX", "Chlorhexidine", AllergyType.Material, true);
        Add("IODINE", "Iodine or povidone-iodine", AllergyType.Material, true);
        Add("EUGEN", "Eugenol", AllergyType.Material, true, "Zinc oxide eugenol cements");
        Add("COLOPH", "Colophony", AllergyType.Material, false, "Periodontal dressings");
        Add("PEANUT", "Peanuts", AllergyType.Food);
        Add("EGG", "Eggs", AllergyType.Food, false, "Propofol formulations");
        Add("SOY", "Soya", AllergyType.Food, false, "Propofol formulations");
        Add("POLLEN", "Pollen", AllergyType.Environmental);
        Add("DUST", "House dust mite", AllergyType.Environmental);

        return list;
    }

    public static List<Medication> Medications()
    {
        var list = new List<Medication>();
        var order = 0;

        void Add(string code, string name, string generic, string drugClass, MedicationForm form,
            string strength, string dosage, string frequency, int? days, int? quantity,
            bool antibiotic = false, bool analgesic = false, bool anaesthetic = false,
            bool controlled = false, string? schedule = null,
            bool pregnancyContra = false, string? interactions = null, string? contraindications = null,
            MedicationRoute route = MedicationRoute.Oral, string? instructions = null)
        {
            list.Add(new Medication
            {
                Code = code, Name = name, GenericName = generic, DrugClass = drugClass,
                Form = form, Strength = strength, DefaultRoute = route,
                DefaultDosage = dosage, DefaultFrequency = frequency,
                DefaultDurationDays = days, DefaultQuantity = quantity,
                DefaultInstructions = instructions,
                IsAntibiotic = antibiotic, IsAnalgesic = analgesic, IsAnaesthetic = anaesthetic,
                IsControlledDrug = controlled, ControlledSchedule = schedule,
                ContraindicatedInPregnancy = pregnancyContra,
                Interactions = interactions, Contraindications = contraindications,
                SortOrder = order += 10, IsSystem = true
            });
        }

        // Antibiotics
        Add("AMOX500", "Amoxicillin 500mg", "Amoxicillin", "Penicillin antibiotic", MedicationForm.Capsule,
            "500mg", "1 capsule", "Three times daily", 5, 15, antibiotic: true,
            contraindications: "Penicillin allergy", instructions: "Take with or after food. Complete the full course.");
        Add("AMOX3G", "Amoxicillin 3g sachet (prophylaxis)", "Amoxicillin", "Penicillin antibiotic",
            MedicationForm.Suspension, "3g", "3g", "Single dose one hour before the procedure", 1, 1,
            antibiotic: true, contraindications: "Penicillin allergy");
        Add("METRO400", "Metronidazole 400mg", "Metronidazole", "Nitroimidazole antibiotic", MedicationForm.Tablet,
            "400mg", "1 tablet", "Three times daily", 5, 15, antibiotic: true,
            interactions: "Alcohol (disulfiram reaction), warfarin",
            instructions: "Avoid all alcohol during the course and for 48 hours afterwards.");
        Add("CLIND300", "Clindamycin 300mg", "Clindamycin", "Lincosamide antibiotic", MedicationForm.Capsule,
            "300mg", "1 capsule", "Four times daily", 5, 20, antibiotic: true,
            contraindications: "History of antibiotic-associated colitis",
            instructions: "Stop and seek advice if severe diarrhoea develops.");
        Add("CLARI500", "Clarithromycin 500mg", "Clarithromycin", "Macrolide antibiotic", MedicationForm.Tablet,
            "500mg", "1 tablet", "Twice daily", 5, 10, antibiotic: true,
            interactions: "Statins, warfarin, colchicine");
        Add("COAMOX", "Co-amoxiclav 625mg", "Amoxicillin/clavulanic acid", "Penicillin antibiotic",
            MedicationForm.Tablet, "625mg", "1 tablet", "Three times daily", 5, 15, antibiotic: true,
            contraindications: "Penicillin allergy");
        Add("DOXY100", "Doxycycline 100mg", "Doxycycline", "Tetracycline antibiotic", MedicationForm.Capsule,
            "100mg", "1 capsule", "Once daily", 7, 7, antibiotic: true, pregnancyContra: true,
            instructions: "Take with plenty of water and remain upright for 30 minutes.");

        // Analgesics
        Add("IBU400", "Ibuprofen 400mg", "Ibuprofen", "NSAID", MedicationForm.Tablet, "400mg",
            "1-2 tablets", "Three times daily", 5, 30, analgesic: true,
            contraindications: "Peptic ulcer, severe asthma, renal impairment, third trimester",
            instructions: "Take with food. Do not exceed 1200mg in 24 hours without advice.");
        Add("PARA500", "Paracetamol 500mg", "Paracetamol", "Analgesic", MedicationForm.Tablet, "500mg",
            "2 tablets", "Every 4-6 hours as required", 5, 32, analgesic: true,
            instructions: "Maximum 8 tablets in 24 hours. Do not take with other paracetamol products.");
        Add("CODPARA", "Co-codamol 30/500", "Codeine/paracetamol", "Opioid analgesic combination",
            MedicationForm.Tablet, "30mg/500mg", "1-2 tablets", "Every 6 hours as required", 3, 20,
            analgesic: true, controlled: true, schedule: "CD5",
            instructions: "May cause drowsiness and constipation. Do not drive if affected.");
        Add("DICLO50", "Diclofenac 50mg", "Diclofenac sodium", "NSAID", MedicationForm.Tablet, "50mg",
            "1 tablet", "Three times daily", 5, 15, analgesic: true,
            contraindications: "Cardiovascular disease, peptic ulcer");

        // Antifungals and antivirals
        Add("NYSTATIN", "Nystatin oral suspension", "Nystatin", "Antifungal", MedicationForm.Suspension,
            "100,000 units/ml", "1ml", "Four times daily", 7, 30, route: MedicationRoute.Rinse,
            instructions: "Hold in the mouth near the affected area before swallowing. Continue for 48 hours after symptoms clear.");
        Add("MICONAZ", "Miconazole oral gel", "Miconazole", "Antifungal", MedicationForm.Gel, "20mg/g",
            "2.5ml", "Four times daily", 7, 1, route: MedicationRoute.Topical,
            interactions: "Warfarin, statins", contraindications: "Warfarin therapy");
        Add("FLUCON", "Fluconazole 50mg", "Fluconazole", "Antifungal", MedicationForm.Capsule, "50mg",
            "1 capsule", "Once daily", 7, 7, interactions: "Warfarin, statins, phenytoin");
        Add("ACICLO", "Aciclovir 200mg", "Aciclovir", "Antiviral", MedicationForm.Tablet, "200mg",
            "1 tablet", "Five times daily", 5, 25);

        // Rinses and topicals
        Add("CHX02", "Chlorhexidine mouthwash 0.2%", "Chlorhexidine gluconate", "Antiseptic",
            MedicationForm.Mouthwash, "0.2%", "10ml", "Twice daily", 14, 300, route: MedicationRoute.Rinse,
            instructions: "Rinse for one minute. Do not use within 30 minutes of toothpaste. May stain teeth.");
        Add("BENZYD", "Benzydamine mouthwash 0.15%", "Benzydamine hydrochloride", "Topical analgesic",
            MedicationForm.Mouthwash, "0.15%", "15ml", "Every 3 hours as required", 7, 300,
            route: MedicationRoute.Rinse, analgesic: true);
        Add("FLUORIDE", "Sodium fluoride toothpaste 5000ppm", "Sodium fluoride", "Fluoride",
            MedicationForm.Gel, "5000ppm", "2cm", "Twice daily", 90, 1, route: MedicationRoute.Topical,
            instructions: "Brush and spit; do not rinse afterwards. Not for children under 16.");
        Add("SALINE", "Warm salt water rinse", "Sodium chloride", "Rinse", MedicationForm.Mouthwash,
            "-", "One cup", "After meals", 7, 1, route: MedicationRoute.Rinse,
            instructions: "Half a teaspoon of salt in a cup of warm water. Start 24 hours after surgery.");

        // Anaesthetics
        Add("LIDO2", "Lidocaine 2% with adrenaline 1:80,000", "Lidocaine hydrochloride", "Amide local anaesthetic",
            MedicationForm.Injection, "2%", "1-2 cartridges", "As required", null, null,
            anaesthetic: true, route: MedicationRoute.Intramuscular);
        Add("ARTIC4", "Articaine 4% with adrenaline 1:100,000", "Articaine hydrochloride", "Amide local anaesthetic",
            MedicationForm.Injection, "4%", "1-2 cartridges", "As required", null, null,
            anaesthetic: true, route: MedicationRoute.Intramuscular);
        Add("PRILO3", "Prilocaine 3% with felypressin", "Prilocaine hydrochloride", "Amide local anaesthetic",
            MedicationForm.Injection, "3%", "1-2 cartridges", "As required", null, null,
            anaesthetic: true, route: MedicationRoute.Intramuscular,
            contraindications: "Pregnancy (felypressin), methaemoglobinaemia");
        Add("MEPI3", "Mepivacaine 3% plain", "Mepivacaine hydrochloride", "Amide local anaesthetic",
            MedicationForm.Injection, "3%", "1-2 cartridges", "As required", null, null,
            anaesthetic: true, route: MedicationRoute.Intramuscular);

        // Emergency drugs
        Add("ADREN1000", "Adrenaline 1:1000 injection", "Adrenaline", "Emergency - anaphylaxis",
            MedicationForm.Injection, "1mg/ml", "0.5ml", "Repeat after 5 minutes if needed", null, null,
            route: MedicationRoute.Intramuscular);
        Add("GTN", "Glyceryl trinitrate spray", "Glyceryl trinitrate", "Emergency - angina",
            MedicationForm.Inhaler, "400mcg", "1-2 sprays", "As required", null, null,
            route: MedicationRoute.Sublingual);
        Add("MIDAZ", "Midazolam 10mg buccal", "Midazolam", "Emergency - seizure", MedicationForm.Liquid,
            "10mg/ml", "10mg", "Single dose", null, null, controlled: true, schedule: "CD3",
            route: MedicationRoute.Sublingual);
        Add("GLUCAGON", "Glucagon 1mg injection", "Glucagon", "Emergency - hypoglycaemia",
            MedicationForm.Injection, "1mg", "1mg", "Single dose", null, null,
            route: MedicationRoute.Intramuscular);

        return list;
    }

    public static List<ConsentFormTemplate> ConsentForms()
    {
        return new List<ConsentFormTemplate>
        {
            new()
            {
                Code = "CONS-EXT", Name = "Consent for tooth extraction", Version = "2.1",
                SortOrder = 10, IsSystem = true, RequiresWitness = false, ValidForDays = 90,
                Body = "I confirm that the reasons for extracting the tooth or teeth listed above have been " +
                       "explained to me, along with what the procedure involves, how long recovery usually takes, " +
                       "and what will happen if I choose not to proceed. I have had the opportunity to ask questions.",
                Risks = "Pain, swelling and bruising for several days. Bleeding. Infection or dry socket. " +
                        "Damage to nearby teeth, fillings or crowns. Numbness of the lip, chin or tongue, " +
                        "which is usually temporary but can rarely be permanent. Jaw stiffness. " +
                        "Sinus communication for upper back teeth. Retained root fragments.",
                Benefits = "Removal of a source of pain and infection, and prevention of further damage to " +
                           "surrounding tissues.",
                Alternatives = "Root canal treatment where the tooth is restorable. Antibiotics as a temporary " +
                               "measure only. Leaving the tooth in place and monitoring it.",
                AppliesToProcedureCodes = "D7111,D7140,D7210,D7250"
            },
            new()
            {
                Code = "CONS-SURG-EXT", Name = "Consent for surgical removal of an impacted tooth", Version = "2.1",
                SortOrder = 20, IsSystem = true, RequiresWitness = true, ValidForDays = 90,
                Body = "I understand that this procedure involves raising the gum, and may involve removing bone " +
                       "and dividing the tooth, in order to remove it. Stitches may be placed.",
                Risks = "All the risks of a simple extraction, plus: a higher chance of swelling, bruising and " +
                        "restricted mouth opening. Injury to the inferior alveolar or lingual nerve causing altered " +
                        "sensation of the lip, chin or tongue - temporary in about 1 in 10 cases and permanent in " +
                        "under 1 in 100. Jaw fracture is very rare. Sinus involvement for upper teeth.",
                Benefits = "Resolution of recurrent infection, pain, cyst formation or damage to the adjacent tooth.",
                Alternatives = "Leaving the tooth and monitoring it radiographically. Coronectomy, where the crown " +
                               "alone is removed to reduce nerve risk. Repeated courses of antibiotics.",
                AppliesToProcedureCodes = "D7220,D7230,D7240,D7251"
            },
            new()
            {
                Code = "CONS-IMPLANT", Name = "Consent for dental implant treatment", Version = "3.0",
                SortOrder = 30, IsSystem = true, RequiresWitness = true, ValidForDays = 180,
                Body = "I understand that implant treatment is carried out in stages over several months, " +
                       "that it requires excellent oral hygiene and regular maintenance, and that the final " +
                       "result depends on healing that cannot be fully predicted in advance.",
                Risks = "Failure of the implant to integrate, reported at roughly 2 to 5 per cent. Infection. " +
                        "Nerve injury causing altered sensation. Sinus perforation in the upper jaw. " +
                        "Bone or gum loss around the implant over time. Screw loosening or fracture of components. " +
                        "Aesthetic limitations, particularly gum recession at the front of the mouth. " +
                        "Smoking and uncontrolled diabetes materially increase the risk of failure.",
                Benefits = "A fixed replacement for a missing tooth that does not require cutting down the " +
                           "neighbouring teeth, and that helps preserve the surrounding bone.",
                Alternatives = "A conventional or resin-bonded bridge. A removable partial denture. " +
                               "Leaving the space unrestored.",
                AppliesToProcedureCodes = "D6010,D6011,D6104,D7951,D7952"
            },
            new()
            {
                Code = "CONS-RCT", Name = "Consent for root canal treatment", Version = "1.4",
                SortOrder = 40, IsSystem = true, ValidForDays = 90,
                Body = "I understand that root canal treatment aims to save a tooth whose nerve has been " +
                       "damaged or infected, and that it usually requires more than one visit.",
                Risks = "The treatment may not succeed, with reported success between 80 and 90 per cent. " +
                        "Instrument fracture within the canal. Perforation of the root. Persistent infection " +
                        "requiring retreatment, surgery or extraction. The tooth becomes more brittle and " +
                        "usually needs a crown afterwards. Post-operative discomfort for a few days.",
                Benefits = "Retention of the natural tooth and relief of pain and infection.",
                Alternatives = "Extraction, followed by an implant, bridge or denture if replacement is wanted. " +
                               "Leaving the tooth untreated is likely to lead to worsening infection.",
                AppliesToProcedureCodes = "D3310,D3320,D3330,D3346,D3348"
            },
            new()
            {
                Code = "CONS-SED", Name = "Consent for conscious sedation", Version = "2.0",
                SortOrder = 50, IsSystem = true, RequiresWitness = true, ValidForDays = 30,
                Body = "I understand that sedation will make me relaxed and drowsy but not unconscious, " +
                       "that local anaesthetic will still be used, and that I must follow the pre- and " +
                       "post-operative instructions I have been given.",
                Risks = "Over-sedation with reduced breathing, which is managed with monitoring and reversal " +
                        "agents. Nausea. Bruising at the injection site. Partial or complete amnesia for the visit. " +
                        "Drowsiness lasting the rest of the day.",
                Benefits = "Reduced anxiety, allowing treatment to be completed comfortably.",
                Alternatives = "Local anaesthetic alone. Behavioural techniques and acclimatisation. " +
                               "Referral for general anaesthetic.",
                AppliesToProcedureCodes = "D9230,D9243"
            },
            new()
            {
                Code = "CONS-PERIO-SURG", Name = "Consent for periodontal surgery", Version = "1.2",
                SortOrder = 60, IsSystem = true, ValidForDays = 90,
                Body = "I understand that this surgery aims to reduce the depth of gum pockets and to make " +
                       "the areas cleanable, and that its success depends heavily on my own oral hygiene " +
                       "and on stopping smoking.",
                Risks = "Gum recession leaving teeth looking longer. Increased sensitivity to cold. " +
                        "Post-operative discomfort and swelling. Some teeth may loosen temporarily. " +
                        "The disease may recur if maintenance is not kept up.",
                Benefits = "Reduced pocket depth, easier cleaning, and improved long-term prognosis for the teeth.",
                Alternatives = "Continued non-surgical therapy and maintenance. Extraction of teeth with a " +
                               "hopeless prognosis.",
                AppliesToProcedureCodes = "D4210,D4240,D4249,D4260,D4263,D4266"
            },
            new()
            {
                Code = "CONS-CROWN", Name = "Consent for crown or bridge treatment", Version = "1.3",
                SortOrder = 70, IsSystem = true, ValidForDays = 120,
                Body = "I understand that preparing a tooth for a crown involves permanently removing tooth " +
                       "substance, and that a temporary restoration will be worn while the laboratory work is made.",
                Risks = "The nerve of the tooth may die, requiring root canal treatment afterwards, reported in " +
                        "up to 15 per cent of cases. Sensitivity. The temporary may come off. Slight differences " +
                        "in shade or shape from natural teeth. Crowns have a finite lifespan and may need replacing.",
                Benefits = "Protection and restoration of a heavily broken-down tooth, and improved appearance.",
                Alternatives = "A large direct filling. An inlay or onlay. Extraction.",
                AppliesToProcedureCodes = "D2740,D2750,D2790,D6240,D6750"
            },
            new()
            {
                Code = "CONS-ORTHO", Name = "Consent for orthodontic treatment", Version = "1.1",
                SortOrder = 80, IsSystem = true, ValidForDays = 180,
                Body = "I understand that orthodontic treatment usually takes 18 to 30 months, requires regular " +
                       "visits, and that retainers must be worn afterwards, in most cases indefinitely.",
                Risks = "Decalcification and decay if hygiene is poor. Shortening of the roots. Gum problems. " +
                        "Relapse if retainers are not worn. Discomfort after adjustments. Rarely, jaw joint symptoms.",
                Benefits = "Improved alignment, bite function and appearance.",
                Alternatives = "No treatment. Limited treatment addressing only part of the problem. " +
                               "Restorative camouflage with veneers or crowns.",
                AppliesToProcedureCodes = "D8080,D8090,D8210,D8703"
            },
            new()
            {
                Code = "CONS-GDPR", Name = "Privacy notice and data-processing consent", Version = "2.0",
                SortOrder = 90, IsSystem = true,
                Body = "I have read the practice privacy notice. I understand what personal and health data " +
                       "the practice holds about me, why it is held, how long it is kept, and who it may be " +
                       "shared with. I understand my right to access my records and to withdraw consent for " +
                       "non-essential contact at any time.",
                Risks = "-", Benefits = "-", Alternatives = "-"
            }
        };
    }

    public static List<PostOperativeInstruction> PostOperativeInstructions()
    {
        return new List<PostOperativeInstruction>
        {
            new()
            {
                Code = "POST-EXT", Name = "After a tooth extraction", SortOrder = 10, IsSystem = true,
                AppliesToSurgeryType = SurgeryType.SimpleExtraction,
                AppliesToProcedureCodes = "D7111,D7140,D7210,D7250",
                Body =
                    "For the first 24 hours\n" +
                    "- Bite firmly on the gauze pack for 30 minutes. Replace it if bleeding continues.\n" +
                    "- Do not rinse, spit or use a straw. The clot needs to stay in place.\n" +
                    "- Avoid hot drinks, alcohol, smoking and strenuous exercise.\n" +
                    "- Eat soft food on the other side of the mouth.\n\n" +
                    "From 24 hours onwards\n" +
                    "- Rinse gently with warm salt water (half a teaspoon of salt in a cup of warm water) " +
                    "after meals and before bed, for about a week.\n" +
                    "- Keep brushing your other teeth normally. Clean gently around the socket.\n" +
                    "- Take pain relief as advised. Paracetamol and ibuprofen together work well if you can take both.\n\n" +
                    "What to expect\n" +
                    "- Some oozing for the first day, soreness for three to four days, and swelling that peaks " +
                    "at about 48 hours are all normal.",
                WarningSigns =
                    "Contact the practice if you have: bleeding that will not stop after 30 minutes of firm " +
                    "biting; severe pain starting three to five days afterwards, which may be dry socket; " +
                    "spreading swelling; difficulty swallowing or breathing; a fever above 38C; or numbness " +
                    "that has not resolved after 24 hours.",
                EmergencyContactText = "Practice: 020 7946 0812. Out of hours, call NHS 111. " +
                                       "For difficulty breathing or swallowing, call 999."
            },
            new()
            {
                Code = "POST-SURG", Name = "After oral surgery", SortOrder = 20, IsSystem = true,
                AppliesToSurgeryType = SurgeryType.SurgicalExtraction,
                AppliesToProcedureCodes = "D7220,D7230,D7240,D7251,D7310",
                Body =
                    "Follow the standard extraction advice, and in addition:\n\n" +
                    "- Use an ice pack against the cheek for 15 minutes in every hour for the first 6 hours.\n" +
                    "- Sleep with an extra pillow for the first two nights.\n" +
                    "- Expect more swelling and jaw stiffness than after a simple extraction. This peaks at " +
                    "48 to 72 hours and then settles.\n" +
                    "- Take the full course of any antibiotics prescribed.\n" +
                    "- If stitches were placed, they will either dissolve within two weeks or be removed at " +
                    "your review appointment.\n" +
                    "- Do not drive or operate machinery if you have had sedation, and do not sign legal " +
                    "documents for 24 hours.",
                WarningSigns =
                    "Contact the practice for: uncontrolled bleeding; worsening pain after day three; " +
                    "swelling that closes the eye or crosses the midline of the neck; fever; " +
                    "persistent numbness of the lip or tongue beyond 24 hours; or bad taste with discharge.",
                EmergencyContactText = "Practice: 020 7946 0812. Out of hours, call NHS 111."
            },
            new()
            {
                Code = "POST-IMPLANT", Name = "After implant surgery", SortOrder = 30, IsSystem = true,
                AppliesToSurgeryType = SurgeryType.ImplantPlacement,
                AppliesToProcedureCodes = "D6010,D6011,D6104",
                Body =
                    "- Do not disturb the surgical site. Do not pull the lip out to look at it.\n" +
                    "- Use the chlorhexidine mouthwash provided twice daily from tomorrow, for two weeks.\n" +
                    "- Brush your other teeth normally but avoid the implant site until reviewed.\n" +
                    "- Eat soft food for a week and avoid chewing on the implant.\n" +
                    "- Do not smoke. Smoking is the single largest avoidable cause of implant failure.\n" +
                    "- If you wear a denture over the site, only use it as advised.\n" +
                    "- Take all medication as prescribed and attend the review appointment.",
                WarningSigns =
                    "Contact the practice for: the implant or healing cap feeling loose; pus or a persistent " +
                    "bad taste; swelling that worsens after day three; a fever; or numbness that does not resolve.",
                EmergencyContactText = "Practice: 020 7946 0812. Out of hours, call NHS 111."
            },
            new()
            {
                Code = "POST-RCT", Name = "After root canal treatment", SortOrder = 40, IsSystem = true,
                AppliesToProcedureCodes = "D3310,D3320,D3330,D3346,D3348",
                Body =
                    "- Do not eat until the numbness has fully worn off, to avoid biting your lip or cheek.\n" +
                    "- The tooth may be tender to bite on for a few days. Chew on the other side.\n" +
                    "- Take paracetamol or ibuprofen as needed.\n" +
                    "- A temporary filling has been placed. Avoid hard and sticky food on that tooth.\n" +
                    "- The tooth is brittle until it is permanently restored. Book the crown appointment as advised.",
                WarningSigns =
                    "Contact the practice if: the temporary filling comes out; swelling develops; " +
                    "pain is severe or increasing after 48 hours; or the tooth feels high when you bite.",
                EmergencyContactText = "Practice: 020 7946 0812."
            },
            new()
            {
                Code = "POST-PERIO", Name = "After periodontal treatment", SortOrder = 50, IsSystem = true,
                AppliesToProcedureCodes = "D4341,D4342,D4210,D4240,D4260",
                Body =
                    "- Gums may be tender and teeth sensitive to cold for a week or two.\n" +
                    "- Keep brushing gently but thoroughly. Cleaning is what makes the treatment work.\n" +
                    "- Use the interdental brushes at the sizes shown to you, once a day.\n" +
                    "- Use desensitising toothpaste for sensitivity: rub a little onto the sensitive area " +
                    "and do not rinse.\n" +
                    "- Gums may shrink slightly as inflammation settles, and spaces between teeth may become " +
                    "more visible. This is a sign of healing.\n" +
                    "- Attend your review so healing can be assessed and your recall interval set.",
                WarningSigns = "Contact the practice for: increasing pain after three days; swelling; " +
                               "an abscess; or bleeding that does not settle.",
                EmergencyContactText = "Practice: 020 7946 0812."
            },
            new()
            {
                Code = "POST-CROWN", Name = "After crown or bridge preparation", SortOrder = 60, IsSystem = true,
                AppliesToProcedureCodes = "D2740,D2750,D2790,D6240,D6750",
                Body =
                    "- A temporary crown is in place. Avoid sticky and hard food, and floss out sideways " +
                    "rather than upwards so you do not pull it off.\n" +
                    "- Some sensitivity to hot and cold is normal and settles once the final crown is fitted.\n" +
                    "- Keep the area clean; gums that are inflamed at the fit appointment make the fit harder.\n" +
                    "- If the temporary comes off, keep it and contact the practice. Do not leave the tooth " +
                    "uncovered, as it can move.",
                WarningSigns = "Contact the practice if the temporary is lost, if the bite feels wrong, " +
                               "or if there is throbbing or spontaneous pain.",
                EmergencyContactText = "Practice: 020 7946 0812."
            },
            new()
            {
                Code = "POST-SED", Name = "After sedation", SortOrder = 70, IsSystem = true,
                Body =
                    "For the next 24 hours you must not:\n" +
                    "- Drive any vehicle or ride a bicycle.\n" +
                    "- Operate machinery or cook.\n" +
                    "- Drink alcohol or take recreational drugs.\n" +
                    "- Sign any legal document or make important decisions.\n" +
                    "- Be responsible for the care of a child or dependent adult on your own.\n\n" +
                    "A responsible adult must take you home and stay with you overnight. " +
                    "Eat lightly and rest. Your memory of the appointment may be patchy, which is expected.",
                WarningSigns = "Seek urgent advice for: persistent vomiting; difficulty breathing; " +
                               "or unusual drowsiness that does not improve.",
                EmergencyContactText = "Practice: 020 7946 0812. In an emergency call 999."
            }
        };
    }

    public static List<MessageTemplate> MessageTemplates()
    {
        return new List<MessageTemplate>
        {
            new()
            {
                Code = "APPT-REMIND-SMS", Name = "Appointment reminder (SMS)",
                Channel = CommunicationChannel.Sms, Category = "Reminder", IsDefaultForCategory = true,
                SortOrder = 10, IsSystem = true,
                AvailableTokens = "{PatientFirstName},{AppointmentDate},{AppointmentTime},{ProviderName},{PracticeName},{PracticePhone}",
                Body = "Hi {PatientFirstName}, this is a reminder of your appointment at {PracticeName} on " +
                       "{AppointmentDate} at {AppointmentTime} with {ProviderName}. Reply YES to confirm or " +
                       "call {PracticePhone} to change it."
            },
            new()
            {
                Code = "APPT-REMIND-EMAIL", Name = "Appointment reminder (email)",
                Channel = CommunicationChannel.Email, Category = "Reminder", SortOrder = 20, IsSystem = true,
                Subject = "Your appointment on {AppointmentDate}",
                AvailableTokens = "{PatientFirstName},{AppointmentDate},{AppointmentTime},{ProviderName},{PracticeName},{PracticeAddress},{PracticePhone}",
                Body = "Dear {PatientFirstName},\n\nThis is a reminder of your appointment with {ProviderName} " +
                       "on {AppointmentDate} at {AppointmentTime}.\n\n{PracticeName}\n{PracticeAddress}\n\n" +
                       "If you need to change or cancel, please give us at least 24 hours' notice by calling " +
                       "{PracticePhone}.\n\nKind regards,\nThe team at {PracticeName}"
            },
            new()
            {
                Code = "RECALL-DUE", Name = "Recall due", Channel = CommunicationChannel.Email,
                Category = "Recall", IsDefaultForCategory = true, SortOrder = 30, IsSystem = true,
                Subject = "Time for your dental check-up",
                AvailableTokens = "{PatientFirstName},{RecallType},{DueDate},{PracticeName},{PracticePhone}",
                Body = "Dear {PatientFirstName},\n\nOur records show that your {RecallType} is due on {DueDate}.\n\n" +
                       "Regular check-ups let us pick up problems early, while they are simpler and cheaper to " +
                       "treat. Please call {PracticePhone} to book a convenient time.\n\n" +
                       "Kind regards,\nThe team at {PracticeName}"
            },
            new()
            {
                Code = "TREATMENT-PLAN", Name = "Treatment plan sent", Channel = CommunicationChannel.Email,
                Category = "TreatmentPlan", SortOrder = 40, IsSystem = true,
                Subject = "Your treatment plan from {PracticeName}",
                AvailableTokens = "{PatientFirstName},{PlanNumber},{TotalFee},{ProviderName},{PracticeName},{PracticePhone}",
                Body = "Dear {PatientFirstName},\n\nPlease find attached the treatment plan ({PlanNumber}) that " +
                       "{ProviderName} discussed with you, with an estimated total of {TotalFee}.\n\n" +
                       "The estimate is valid for 90 days. Please take your time to read it, and call " +
                       "{PracticePhone} with any questions or to book the first appointment.\n\n" +
                       "Kind regards,\nThe team at {PracticeName}"
            },
            new()
            {
                Code = "INVOICE-DUE", Name = "Invoice reminder", Channel = CommunicationChannel.Email,
                Category = "Billing", IsDefaultForCategory = true, SortOrder = 50, IsSystem = true,
                Subject = "Invoice {InvoiceNumber} from {PracticeName}",
                AvailableTokens = "{PatientFirstName},{InvoiceNumber},{Amount},{DueDate},{PracticeName},{PracticePhone}",
                Body = "Dear {PatientFirstName},\n\nInvoice {InvoiceNumber} for {Amount} is due on {DueDate}.\n\n" +
                       "If you have already paid, please ignore this message. If you would like to discuss a " +
                       "payment plan, call us on {PracticePhone} and we will be glad to help.\n\n" +
                       "Kind regards,\nThe accounts team at {PracticeName}"
            },
            new()
            {
                Code = "POSTOP-CHECK", Name = "Post-operative check", Channel = CommunicationChannel.Sms,
                Category = "Clinical", SortOrder = 60, IsSystem = true,
                AvailableTokens = "{PatientFirstName},{PracticeName},{PracticePhone}",
                Body = "Hi {PatientFirstName}, we hope you are recovering well after yesterday's treatment. " +
                       "If you have any concerns, call {PracticePhone}. {PracticeName}"
            },
            new()
            {
                Code = "LAB-READY", Name = "Laboratory work ready", Channel = CommunicationChannel.Sms,
                Category = "Clinical", SortOrder = 70, IsSystem = true,
                AvailableTokens = "{PatientFirstName},{CaseType},{PracticeName},{PracticePhone}",
                Body = "Hi {PatientFirstName}, your {CaseType} has arrived from the laboratory. " +
                       "Please call {PracticePhone} to book the fit appointment. {PracticeName}"
            },
            new()
            {
                Code = "NOSHOW", Name = "Missed appointment", Channel = CommunicationChannel.Email,
                Category = "Reminder", SortOrder = 80, IsSystem = true,
                Subject = "We missed you today",
                AvailableTokens = "{PatientFirstName},{AppointmentDate},{PracticeName},{PracticePhone}",
                Body = "Dear {PatientFirstName},\n\nWe had you booked in on {AppointmentDate} but were not able " +
                       "to see you. We know things come up.\n\nPlease call {PracticePhone} to rearrange. " +
                       "Letting us know in advance helps us offer the time to someone else.\n\n" +
                       "Kind regards,\nThe team at {PracticeName}"
            }
        };
    }
}
