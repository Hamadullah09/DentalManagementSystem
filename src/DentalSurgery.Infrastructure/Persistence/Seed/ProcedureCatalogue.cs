using DentalSurgery.Domain.Entities;
using DentalSurgery.Domain.Enums;

namespace DentalSurgery.Infrastructure.Persistence.Seed;

/// <summary>
/// The billable procedure catalogue. Codes follow the CDT convention (D-codes)
/// which most practice-management systems recognise. Fees are indicative
/// private-practice prices in GBP and are edited per practice after install.
/// </summary>
public static class ProcedureCatalogue
{
    public static List<ProcedureCode> Build()
    {
        var list = new List<ProcedureCode>();
        var order = 0;

        void Add(
            string code, string description, ProcedureCategory category, decimal fee, int minutes,
            bool tooth = false, bool surfaces = false, bool quadrant = false, bool arch = false,
            bool surgical = false, bool anaesthesia = false, bool lab = false, bool consent = false,
            bool radiograph = false, bool hygiene = false, bool recall = false, int? recallMonths = null,
            string? longDescription = null, int visits = 1)
        {
            list.Add(new ProcedureCode
            {
                Code = code,
                ShortDescription = description,
                LongDescription = longDescription,
                Category = category,
                DefaultFee = fee,
                DefaultDurationMinutes = minutes,
                RequiresTooth = tooth,
                RequiresSurfaces = surfaces,
                RequiresQuadrant = quadrant,
                RequiresArch = arch,
                IsSurgical = surgical,
                RequiresAnaesthesia = anaesthesia,
                RequiresLabWork = lab,
                RequiresConsent = consent,
                RequiresRadiograph = radiograph,
                IsHygieneProcedure = hygiene,
                GeneratesRecall = recall,
                RecallIntervalMonths = recallMonths,
                TypicalVisits = visits,
                IsDiagnosticOnly = category is ProcedureCategory.Diagnostic or ProcedureCategory.Radiology,
                SortOrder = order += 10,
                InsuranceCategory = InsuranceBand(category)
            });
        }

        // ---------------------------------------------------- diagnostic
        Add("D0120", "Periodic oral examination", ProcedureCategory.Diagnostic, 45m, 20, recall: true, recallMonths: 6);
        Add("D0140", "Limited oral evaluation - problem focused", ProcedureCategory.Diagnostic, 55m, 20);
        Add("D0150", "Comprehensive oral evaluation - new patient", ProcedureCategory.Diagnostic, 85m, 45,
            longDescription: "Full assessment including charting, soft-tissue examination and treatment planning.");
        Add("D0160", "Detailed and extensive oral evaluation", ProcedureCategory.Diagnostic, 110m, 60);
        Add("D0170", "Re-evaluation - limited, problem focused", ProcedureCategory.Diagnostic, 40m, 15);
        Add("D0180", "Comprehensive periodontal evaluation", ProcedureCategory.Diagnostic, 95m, 45);
        Add("D0190", "Screening of a patient", ProcedureCategory.Diagnostic, 25m, 10);
        Add("D0431", "Adjunctive oral cancer screening test", ProcedureCategory.Diagnostic, 45m, 15);
        Add("D0460", "Pulp vitality tests", ProcedureCategory.Diagnostic, 30m, 15, tooth: true);
        Add("D0470", "Diagnostic casts", ProcedureCategory.Diagnostic, 60m, 30, lab: true);

        // ---------------------------------------------------- radiology
        Add("D0210", "Intraoral - complete series of radiographs", ProcedureCategory.Radiology, 120m, 40);
        Add("D0220", "Intraoral - periapical, first radiograph", ProcedureCategory.Radiology, 22m, 10, tooth: true);
        Add("D0230", "Intraoral - periapical, each additional", ProcedureCategory.Radiology, 16m, 5, tooth: true);
        Add("D0270", "Bitewing - single radiograph", ProcedureCategory.Radiology, 22m, 10);
        Add("D0272", "Bitewings - two radiographs", ProcedureCategory.Radiology, 36m, 15, recall: true, recallMonths: 12);
        Add("D0274", "Bitewings - four radiographs", ProcedureCategory.Radiology, 55m, 20, recall: true, recallMonths: 12);
        Add("D0330", "Panoramic radiograph", ProcedureCategory.Radiology, 85m, 20);
        Add("D0340", "Cephalometric radiograph", ProcedureCategory.Radiology, 90m, 20);
        Add("D0364", "Cone beam CT - limited field of view", ProcedureCategory.Radiology, 195m, 30);
        Add("D0367", "Cone beam CT - both jaws", ProcedureCategory.Radiology, 295m, 40);
        Add("D0350", "Intraoral photographic image", ProcedureCategory.Radiology, 25m, 10);

        // ---------------------------------------------------- preventive
        Add("D1110", "Prophylaxis - adult", ProcedureCategory.Preventive, 70m, 40, hygiene: true, recall: true, recallMonths: 6);
        Add("D1120", "Prophylaxis - child", ProcedureCategory.Preventive, 50m, 30, hygiene: true, recall: true, recallMonths: 6);
        Add("D1206", "Topical fluoride varnish", ProcedureCategory.Preventive, 28m, 10, hygiene: true);
        Add("D1330", "Oral hygiene instruction", ProcedureCategory.Preventive, 30m, 20, hygiene: true);
        Add("D1351", "Sealant - per tooth", ProcedureCategory.Preventive, 42m, 20, tooth: true);
        Add("D1354", "Interim caries arresting medicament", ProcedureCategory.Preventive, 35m, 15, tooth: true);
        Add("D1510", "Space maintainer - fixed, unilateral", ProcedureCategory.Preventive, 220m, 40, quadrant: true, lab: true);
        Add("D1999", "Airflow stain removal", ProcedureCategory.Preventive, 60m, 30, hygiene: true);

        // ---------------------------------------------------- restorative
        Add("D2140", "Amalgam - one surface", ProcedureCategory.Restorative, 95m, 30, tooth: true, surfaces: true, anaesthesia: true);
        Add("D2150", "Amalgam - two surfaces", ProcedureCategory.Restorative, 120m, 40, tooth: true, surfaces: true, anaesthesia: true);
        Add("D2160", "Amalgam - three surfaces", ProcedureCategory.Restorative, 145m, 45, tooth: true, surfaces: true, anaesthesia: true);
        Add("D2161", "Amalgam - four or more surfaces", ProcedureCategory.Restorative, 170m, 50, tooth: true, surfaces: true, anaesthesia: true);
        Add("D2330", "Composite - one surface, anterior", ProcedureCategory.Restorative, 110m, 35, tooth: true, surfaces: true, anaesthesia: true);
        Add("D2331", "Composite - two surfaces, anterior", ProcedureCategory.Restorative, 135m, 45, tooth: true, surfaces: true, anaesthesia: true);
        Add("D2332", "Composite - three surfaces, anterior", ProcedureCategory.Restorative, 160m, 50, tooth: true, surfaces: true, anaesthesia: true);
        Add("D2335", "Composite - four or more surfaces, anterior", ProcedureCategory.Restorative, 195m, 60, tooth: true, surfaces: true, anaesthesia: true);
        Add("D2391", "Composite - one surface, posterior", ProcedureCategory.Restorative, 125m, 40, tooth: true, surfaces: true, anaesthesia: true);
        Add("D2392", "Composite - two surfaces, posterior", ProcedureCategory.Restorative, 155m, 50, tooth: true, surfaces: true, anaesthesia: true);
        Add("D2393", "Composite - three surfaces, posterior", ProcedureCategory.Restorative, 185m, 55, tooth: true, surfaces: true, anaesthesia: true);
        Add("D2394", "Composite - four or more surfaces, posterior", ProcedureCategory.Restorative, 215m, 65, tooth: true, surfaces: true, anaesthesia: true);
        Add("D2910", "Re-cement inlay, onlay or veneer", ProcedureCategory.Restorative, 65m, 20, tooth: true);
        Add("D2920", "Re-cement crown", ProcedureCategory.Restorative, 70m, 20, tooth: true);
        Add("D2940", "Protective restoration (temporary)", ProcedureCategory.Restorative, 60m, 20, tooth: true);
        Add("D2950", "Core build-up, including pins", ProcedureCategory.Restorative, 165m, 45, tooth: true, anaesthesia: true);
        Add("D2954", "Prefabricated post and core", ProcedureCategory.Restorative, 210m, 50, tooth: true, anaesthesia: true);

        // ---------------------------------------------------- fixed prosthodontics
        Add("D2740", "Crown - porcelain/ceramic", ProcedureCategory.ProsthodonticsFixed, 750m, 75, tooth: true,
            anaesthesia: true, lab: true, consent: true, visits: 2);
        Add("D2750", "Crown - porcelain fused to high noble metal", ProcedureCategory.ProsthodonticsFixed, 720m, 75,
            tooth: true, anaesthesia: true, lab: true, consent: true, visits: 2);
        Add("D2790", "Crown - full cast high noble metal", ProcedureCategory.ProsthodonticsFixed, 780m, 75,
            tooth: true, anaesthesia: true, lab: true, consent: true, visits: 2);
        Add("D2510", "Inlay - metallic, one surface", ProcedureCategory.ProsthodonticsFixed, 480m, 60, tooth: true, lab: true, visits: 2);
        Add("D2610", "Inlay - porcelain/ceramic, one surface", ProcedureCategory.ProsthodonticsFixed, 520m, 60, tooth: true, lab: true, visits: 2);
        Add("D2642", "Onlay - porcelain/ceramic, two surfaces", ProcedureCategory.ProsthodonticsFixed, 620m, 70, tooth: true, lab: true, visits: 2);
        Add("D2962", "Veneer - porcelain laminate", ProcedureCategory.Cosmetic, 690m, 75, tooth: true, lab: true, consent: true, visits: 2);
        Add("D6240", "Pontic - porcelain fused to high noble metal", ProcedureCategory.ProsthodonticsFixed, 720m, 60,
            tooth: true, lab: true, consent: true, visits: 2);
        Add("D6750", "Retainer crown - porcelain fused to metal", ProcedureCategory.ProsthodonticsFixed, 720m, 60,
            tooth: true, lab: true, consent: true, visits: 2);
        Add("D6545", "Resin-bonded bridge retainer", ProcedureCategory.ProsthodonticsFixed, 480m, 60, tooth: true, lab: true, visits: 2);

        // ---------------------------------------------------- endodontics
        Add("D3110", "Pulp cap - direct", ProcedureCategory.Endodontics, 75m, 20, tooth: true, anaesthesia: true);
        Add("D3220", "Therapeutic pulpotomy", ProcedureCategory.Endodontics, 145m, 40, tooth: true, anaesthesia: true);
        Add("D3310", "Endodontic therapy - anterior tooth", ProcedureCategory.Endodontics, 420m, 75, tooth: true,
            anaesthesia: true, radiograph: true, consent: true);
        Add("D3320", "Endodontic therapy - premolar", ProcedureCategory.Endodontics, 520m, 90, tooth: true,
            anaesthesia: true, radiograph: true, consent: true);
        Add("D3330", "Endodontic therapy - molar", ProcedureCategory.Endodontics, 680m, 120, tooth: true,
            anaesthesia: true, radiograph: true, consent: true);
        Add("D3346", "Retreatment of root canal - anterior", ProcedureCategory.Endodontics, 550m, 90, tooth: true,
            anaesthesia: true, radiograph: true, consent: true);
        Add("D3348", "Retreatment of root canal - molar", ProcedureCategory.Endodontics, 850m, 150, tooth: true,
            anaesthesia: true, radiograph: true, consent: true);
        Add("D3410", "Apicoectomy - anterior", ProcedureCategory.Endodontics, 620m, 90, tooth: true,
            surgical: true, anaesthesia: true, consent: true, radiograph: true);
        Add("D3430", "Retrograde filling - per root", ProcedureCategory.Endodontics, 180m, 30, tooth: true, surgical: true);

        // ---------------------------------------------------- periodontics
        Add("D4210", "Gingivectomy - four or more teeth per quadrant", ProcedureCategory.Periodontics, 420m, 60,
            quadrant: true, surgical: true, anaesthesia: true, consent: true);
        Add("D4240", "Gingival flap procedure - per quadrant", ProcedureCategory.Periodontics, 560m, 90,
            quadrant: true, surgical: true, anaesthesia: true, consent: true);
        Add("D4249", "Clinical crown lengthening - hard tissue", ProcedureCategory.Periodontics, 520m, 75,
            tooth: true, surgical: true, anaesthesia: true, consent: true);
        Add("D4260", "Osseous surgery - per quadrant", ProcedureCategory.Periodontics, 780m, 120,
            quadrant: true, surgical: true, anaesthesia: true, consent: true);
        Add("D4263", "Bone replacement graft - first site", ProcedureCategory.Periodontics, 480m, 60,
            tooth: true, surgical: true, anaesthesia: true, consent: true);
        Add("D4266", "Guided tissue regeneration - resorbable barrier", ProcedureCategory.Periodontics, 520m, 60,
            tooth: true, surgical: true, anaesthesia: true, consent: true);
        Add("D4341", "Scaling and root planing - four or more teeth per quadrant", ProcedureCategory.Periodontics,
            180m, 60, quadrant: true, anaesthesia: true, hygiene: true);
        Add("D4342", "Scaling and root planing - one to three teeth per quadrant", ProcedureCategory.Periodontics,
            120m, 40, quadrant: true, anaesthesia: true, hygiene: true);
        Add("D4346", "Scaling with generalised moderate inflammation", ProcedureCategory.Periodontics, 110m, 45, hygiene: true);
        Add("D4910", "Periodontal maintenance", ProcedureCategory.Periodontics, 95m, 50, hygiene: true,
            recall: true, recallMonths: 3);
        Add("D4921", "Gingival irrigation - per quadrant", ProcedureCategory.Periodontics, 45m, 15, quadrant: true, hygiene: true);

        // ---------------------------------------------------- oral surgery
        Add("D7111", "Extraction - coronal remnants, primary tooth", ProcedureCategory.OralAndMaxillofacialSurgery,
            95m, 20, tooth: true, surgical: true, anaesthesia: true, consent: true);
        Add("D7140", "Extraction - erupted tooth or exposed root", ProcedureCategory.OralAndMaxillofacialSurgery,
            145m, 30, tooth: true, surgical: true, anaesthesia: true, consent: true, radiograph: true);
        Add("D7210", "Surgical extraction - erupted tooth requiring bone removal",
            ProcedureCategory.OralAndMaxillofacialSurgery, 265m, 45, tooth: true, surgical: true,
            anaesthesia: true, consent: true, radiograph: true);
        Add("D7220", "Removal of impacted tooth - soft tissue", ProcedureCategory.OralAndMaxillofacialSurgery,
            320m, 45, tooth: true, surgical: true, anaesthesia: true, consent: true, radiograph: true);
        Add("D7230", "Removal of impacted tooth - partially bony", ProcedureCategory.OralAndMaxillofacialSurgery,
            420m, 60, tooth: true, surgical: true, anaesthesia: true, consent: true, radiograph: true);
        Add("D7240", "Removal of impacted tooth - completely bony", ProcedureCategory.OralAndMaxillofacialSurgery,
            520m, 75, tooth: true, surgical: true, anaesthesia: true, consent: true, radiograph: true);
        Add("D7250", "Removal of residual tooth roots", ProcedureCategory.OralAndMaxillofacialSurgery,
            280m, 45, tooth: true, surgical: true, anaesthesia: true, consent: true);
        Add("D7251", "Coronectomy - intentional partial tooth removal",
            ProcedureCategory.OralAndMaxillofacialSurgery, 480m, 60, tooth: true, surgical: true,
            anaesthesia: true, consent: true, radiograph: true);
        Add("D7285", "Incisional biopsy of oral tissue - hard", ProcedureCategory.OralAndMaxillofacialSurgery,
            340m, 45, surgical: true, anaesthesia: true, consent: true);
        Add("D7286", "Incisional biopsy of oral tissue - soft", ProcedureCategory.OralAndMaxillofacialSurgery,
            290m, 40, surgical: true, anaesthesia: true, consent: true);
        Add("D7310", "Alveoloplasty with extractions - per quadrant", ProcedureCategory.OralAndMaxillofacialSurgery,
            260m, 40, quadrant: true, surgical: true, anaesthesia: true);
        Add("D7510", "Incision and drainage of abscess - intraoral", ProcedureCategory.OralAndMaxillofacialSurgery,
            185m, 30, surgical: true, anaesthesia: true);
        Add("D7960", "Frenectomy", ProcedureCategory.OralAndMaxillofacialSurgery, 320m, 40,
            surgical: true, anaesthesia: true, consent: true);
        Add("D7971", "Excision of pericoronal gingiva (operculectomy)",
            ProcedureCategory.OralAndMaxillofacialSurgery, 180m, 30, tooth: true, surgical: true, anaesthesia: true);
        Add("D7953", "Bone replacement graft for ridge preservation",
            ProcedureCategory.OralAndMaxillofacialSurgery, 460m, 45, tooth: true, surgical: true,
            anaesthesia: true, consent: true);

        // ---------------------------------------------------- implants
        Add("D6010", "Surgical placement of implant body - endosteal", ProcedureCategory.ImplantServices,
            1450m, 90, tooth: true, surgical: true, anaesthesia: true, consent: true, radiograph: true);
        Add("D6011", "Second stage implant surgery", ProcedureCategory.ImplantServices, 320m, 45,
            tooth: true, surgical: true, anaesthesia: true);
        Add("D6056", "Prefabricated abutment", ProcedureCategory.ImplantServices, 380m, 45, tooth: true, lab: true);
        Add("D6057", "Custom fabricated abutment", ProcedureCategory.ImplantServices, 520m, 45, tooth: true, lab: true);
        Add("D6058", "Abutment supported porcelain/ceramic crown", ProcedureCategory.ImplantServices, 890m, 60,
            tooth: true, lab: true, consent: true, visits: 2);
        Add("D6104", "Bone graft at time of implant placement", ProcedureCategory.ImplantServices, 520m, 45,
            tooth: true, surgical: true, anaesthesia: true, consent: true);
        Add("D7951", "Sinus augmentation via lateral open approach", ProcedureCategory.ImplantServices, 1650m, 120,
            surgical: true, anaesthesia: true, consent: true, radiograph: true);
        Add("D7952", "Sinus augmentation via vertical approach", ProcedureCategory.ImplantServices, 950m, 75,
            surgical: true, anaesthesia: true, consent: true, radiograph: true);
        Add("D6080", "Implant maintenance procedures", ProcedureCategory.ImplantServices, 110m, 40,
            hygiene: true, recall: true, recallMonths: 6);

        // ---------------------------------------------------- removable prosthodontics
        Add("D5110", "Complete denture - maxillary", ProcedureCategory.ProsthodonticsRemovable, 1150m, 60,
            arch: true, lab: true, consent: true, visits: 5);
        Add("D5120", "Complete denture - mandibular", ProcedureCategory.ProsthodonticsRemovable, 1150m, 60,
            arch: true, lab: true, consent: true, visits: 5);
        Add("D5130", "Immediate denture - maxillary", ProcedureCategory.ProsthodonticsRemovable, 1350m, 60,
            arch: true, lab: true, consent: true, visits: 4);
        Add("D5213", "Maxillary partial denture - cast metal framework", ProcedureCategory.ProsthodonticsRemovable,
            1250m, 60, arch: true, lab: true, consent: true, visits: 5);
        Add("D5214", "Mandibular partial denture - cast metal framework", ProcedureCategory.ProsthodonticsRemovable,
            1250m, 60, arch: true, lab: true, consent: true, visits: 5);
        Add("D5225", "Maxillary partial denture - flexible base", ProcedureCategory.ProsthodonticsRemovable,
            780m, 45, arch: true, lab: true, visits: 4);
        Add("D5511", "Repair broken complete denture base", ProcedureCategory.ProsthodonticsRemovable, 160m, 30, lab: true);
        Add("D5730", "Reline complete maxillary denture - chairside", ProcedureCategory.ProsthodonticsRemovable, 220m, 45, arch: true);
        Add("D5750", "Reline complete maxillary denture - laboratory", ProcedureCategory.ProsthodonticsRemovable,
            320m, 30, arch: true, lab: true, visits: 2);
        Add("D5410", "Adjust complete denture - maxillary", ProcedureCategory.ProsthodonticsRemovable, 65m, 20, arch: true);

        // ---------------------------------------------------- orthodontics
        Add("D8080", "Comprehensive orthodontic treatment - adolescent", ProcedureCategory.Orthodontics,
            3400m, 45, consent: true, visits: 24);
        Add("D8090", "Comprehensive orthodontic treatment - adult", ProcedureCategory.Orthodontics,
            3900m, 45, consent: true, visits: 24);
        Add("D8210", "Removable appliance therapy", ProcedureCategory.Orthodontics, 950m, 40, lab: true, visits: 8);
        Add("D8670", "Periodic orthodontic treatment visit", ProcedureCategory.Orthodontics, 120m, 25);
        Add("D8680", "Orthodontic retention - removal of appliances and retainers",
            ProcedureCategory.Orthodontics, 420m, 60, lab: true, visits: 2);
        Add("D8703", "Clear aligner therapy - limited", ProcedureCategory.Orthodontics, 2450m, 40,
            consent: true, lab: true, visits: 12);

        // ---------------------------------------------------- cosmetic and adjunctive
        Add("D9972", "External bleaching - per arch", ProcedureCategory.Cosmetic, 320m, 45, arch: true, lab: true);
        Add("D9975", "External bleaching for home application", ProcedureCategory.Cosmetic, 280m, 30, lab: true);
        Add("D9110", "Palliative treatment of dental pain", ProcedureCategory.Adjunctive, 85m, 25);
        Add("D9215", "Local anaesthesia", ProcedureCategory.Adjunctive, 0m, 5);
        Add("D9230", "Inhalation of nitrous oxide - analgesia", ProcedureCategory.Adjunctive, 95m, 15, consent: true);
        Add("D9243", "Intravenous moderate sedation - per 15 minutes", ProcedureCategory.Adjunctive, 180m, 15, consent: true);
        Add("D9310", "Consultation with a specialist", ProcedureCategory.Adjunctive, 120m, 45);
        Add("D9440", "Office visit - after regularly scheduled hours", ProcedureCategory.Adjunctive, 150m, 30);
        Add("D9944", "Occlusal guard - hard appliance, full arch", ProcedureCategory.Adjunctive, 420m, 45,
            arch: true, lab: true, visits: 2);
        Add("D9986", "Missed appointment", ProcedureCategory.Adjunctive, 45m, 0);
        Add("D9987", "Cancelled appointment - short notice", ProcedureCategory.Adjunctive, 30m, 0);

        // ---------------------------------------------------- paediatric
        Add("D2930", "Prefabricated stainless steel crown - primary tooth", ProcedureCategory.Paediatric,
            185m, 40, tooth: true, anaesthesia: true);
        Add("D3230", "Pulpal therapy - anterior primary tooth", ProcedureCategory.Paediatric, 165m, 40,
            tooth: true, anaesthesia: true);
        Add("D1208", "Topical application of fluoride - child", ProcedureCategory.Paediatric, 26m, 10, hygiene: true);

        return list;
    }

    private static string InsuranceBand(ProcedureCategory category) => category switch
    {
        ProcedureCategory.Diagnostic or ProcedureCategory.Radiology => "Diagnostic",
        ProcedureCategory.Preventive or ProcedureCategory.Paediatric => "Preventive",
        ProcedureCategory.Restorative or ProcedureCategory.Endodontics or ProcedureCategory.Periodontics => "Basic",
        ProcedureCategory.ProsthodonticsFixed or ProcedureCategory.ProsthodonticsRemovable
            or ProcedureCategory.ImplantServices or ProcedureCategory.MaxillofacialProsthetics => "Major",
        ProcedureCategory.Orthodontics => "Orthodontic",
        ProcedureCategory.OralAndMaxillofacialSurgery => "Surgery",
        _ => "Adjunctive"
    };
}
