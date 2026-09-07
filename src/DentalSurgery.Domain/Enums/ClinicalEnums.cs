namespace DentalSurgery.Domain.Enums;

public enum DentalArch { Upper = 0, Lower = 1 }

public enum Quadrant
{
    None = 0,
    UpperRight = 1, UpperLeft = 2, LowerLeft = 3, LowerRight = 4,
    UpperRightPrimary = 5, UpperLeftPrimary = 6, LowerLeftPrimary = 7, LowerRightPrimary = 8
}

public enum ToothType { Incisor = 0, Canine = 1, Premolar = 2, Molar = 3, Supernumerary = 4 }

public enum Dentition { Permanent = 0, Primary = 1, Mixed = 2 }

/// <summary>Tooth surfaces as flags so a restoration can span several (e.g. MOD).</summary>
[Flags]
public enum ToothSurface
{
    None = 0,
    Mesial = 1 << 0,
    Distal = 1 << 1,
    Buccal = 1 << 2,
    Lingual = 1 << 3,
    Occlusal = 1 << 4,
    Incisal = 1 << 5,
    Cervical = 1 << 6,
    Palatal = 1 << 7,
    Root = 1 << 8,
    Whole = 1 << 9
}

public enum ToothConditionType
{
    Sound = 0,
    Caries = 1,
    Restoration = 2,
    Crown = 3,
    BridgePontic = 4,
    BridgeAbutment = 5,
    Veneer = 6,
    Inlay = 7,
    Onlay = 8,
    RootCanalTreated = 9,
    Implant = 10,
    ImplantCrown = 11,
    Missing = 12,
    Extracted = 13,
    Unerupted = 14,
    Impacted = 15,
    PartiallyErupted = 16,
    Sealant = 17,
    Fracture = 18,
    Attrition = 19,
    Abrasion = 20,
    Erosion = 21,
    Abfraction = 22,
    Discolouration = 23,
    Hypoplasia = 24,
    Abscess = 25,
    PeriapicalLesion = 26,
    Mobility = 27,
    Recession = 28,
    Denture = 29,
    PartialDenture = 30,
    SpaceMaintainer = 31,
    OrthodonticBracket = 32,
    Watch = 33,
    RetainedRoot = 34,
    PostAndCore = 35,
    TemporaryRestoration = 36,
    Supernumerary = 37,
    Diastema = 38,
    Calculus = 39,
    Other = 99
}

public enum ChartEntryStatus { Existing = 0, Planned = 1, InProgress = 2, Completed = 3, Watch = 4, Referred = 5, Voided = 6 }

public enum RestorationMaterial
{
    None = 0, Amalgam = 1, CompositeResin = 2, GlassIonomer = 3, ResinModifiedGlassIonomer = 4,
    Compomer = 5, GoldAlloy = 6, PorcelainFusedToMetal = 7, AllCeramic = 8, Zirconia = 9,
    LithiumDisilicate = 10, BaseMetalAlloy = 11, Titanium = 12, Acrylic = 13,
    TemporaryMaterial = 14, StainlessSteel = 15, Other = 99
}

public enum PeriodontalSite { MesioBuccal = 0, Buccal = 1, DistoBuccal = 2, MesioLingual = 3, Lingual = 4, DistoLingual = 5 }

public enum MobilityGrade { None = 0, GradeI = 1, GradeII = 2, GradeIII = 3 }

public enum FurcationGrade { None = 0, ClassI = 1, ClassII = 2, ClassIII = 3, ClassIV = 4 }

public enum PeriodontalDiagnosis
{
    Healthy = 0,
    Gingivitis = 1,
    PeriodontitisStageIGradeA = 10, PeriodontitisStageIGradeB = 11, PeriodontitisStageIGradeC = 12,
    PeriodontitisStageIIGradeA = 20, PeriodontitisStageIIGradeB = 21, PeriodontitisStageIIGradeC = 22,
    PeriodontitisStageIIIGradeA = 30, PeriodontitisStageIIIGradeB = 31, PeriodontitisStageIIIGradeC = 32,
    PeriodontitisStageIVGradeA = 40, PeriodontitisStageIVGradeB = 41, PeriodontitisStageIVGradeC = 42,
    NecrotisingPeriodontalDisease = 50,
    PeriImplantMucositis = 60,
    PeriImplantitis = 61
}

public enum ClinicalNoteType
{
    Soap = 0, Progress = 1, Consultation = 2, Operative = 3, PostOperative = 4,
    Telephone = 5, Referral = 6, Emergency = 7, Hygiene = 8, Orthodontic = 9,
    Anaesthesia = 10, Radiology = 11, Laboratory = 12, Administrative = 13
}

public enum AllergyType { Drug = 0, Food = 1, Environmental = 2, Latex = 3, Metal = 4, Anaesthetic = 5, Material = 6, Other = 99 }

public enum AllergySeverity { Unknown = 0, Mild = 1, Moderate = 2, Severe = 3, Anaphylaxis = 4 }

public enum ConditionStatus { Active = 0, Resolved = 1, Chronic = 2, InRemission = 3, Suspected = 4, RuledOut = 5 }

public enum AsaClassification { AsaI = 1, AsaII = 2, AsaIII = 3, AsaIV = 4, AsaV = 5, AsaVI = 6 }

public enum AnaesthesiaType
{
    None = 0, TopicalOnly = 1, LocalInfiltration = 2, LocalBlock = 3,
    NitrousOxideSedation = 4, OralSedation = 5, IntravenousSedation = 6, GeneralAnaesthesia = 7
}

public enum InjectionTechnique
{
    Infiltration = 0, InferiorAlveolarNerveBlock = 1, LingualNerveBlock = 2, LongBuccalBlock = 3,
    MentalNerveBlock = 4, IncisiveNerveBlock = 5, PosteriorSuperiorAlveolar = 6,
    MiddleSuperiorAlveolar = 7, AnteriorSuperiorAlveolar = 8, GreaterPalatine = 9,
    Nasopalatine = 10, Intraligamentary = 11, Intraosseous = 12, GowGates = 13, VaziraniAkinosi = 14
}

public enum SurgeryType
{
    SimpleExtraction = 0, SurgicalExtraction = 1, ImpactedToothRemoval = 2, WisdomToothRemoval = 3,
    Coronectomy = 4, ImplantPlacement = 5, ImplantExposure = 6, ImplantRemoval = 7,
    BoneGraft = 8, SinusLift = 9, RidgeAugmentation = 10, SocketPreservation = 11,
    Apicectomy = 12, Frenectomy = 13, Gingivectomy = 14, Gingivoplasty = 15,
    CrownLengthening = 16, FlapSurgery = 17, GuidedTissueRegeneration = 18,
    SoftTissueGraft = 19, Biopsy = 20, CystEnucleation = 21, AbscessDrainage = 22,
    Operculectomy = 23, Alveoloplasty = 24, ToriRemoval = 25, Other = 99
}

public enum BoneQuality { Unknown = 0, D1 = 1, D2 = 2, D3 = 3, D4 = 4 }

public enum SurgicalOutcome { Uneventful = 0, MinorComplication = 1, MajorComplication = 2, Aborted = 3, Referred = 4 }

public enum ImplantStatus { Planned = 0, Placed = 1, Integrating = 2, Exposed = 3, Restored = 4, Failing = 5, Failed = 6, Explanted = 7 }

public enum RadiographType
{
    Bitewing = 0, Periapical = 1, Panoramic = 2, Cephalometric = 3, Occlusal = 4,
    FullMouthSeries = 5, ConeBeamCt = 6, IntraoralPhotograph = 7, ExtraoralPhotograph = 8,
    IntraoralScan = 9, Sialogram = 10
}

public enum PrescriptionStatus { Draft = 0, Issued = 1, Transmitted = 2, Dispensed = 3, Cancelled = 4, Expired = 5 }

public enum MedicationRoute { Oral = 0, Topical = 1, Intramuscular = 2, Intravenous = 3, Subcutaneous = 4, Inhalation = 5, Sublingual = 6, Rectal = 7, Rinse = 8, Other = 99 }

public enum MedicationForm { Tablet = 0, Capsule = 1, Liquid = 2, Suspension = 3, Gel = 4, Ointment = 5, Injection = 6, Inhaler = 7, Mouthwash = 8, Lozenge = 9, Patch = 10, Other = 99 }

public enum ConsentStatus { Pending = 0, Signed = 1, Declined = 2, Withdrawn = 3, Expired = 4 }

public enum DocumentType
{
    Radiograph = 0, Photograph = 1, ConsentForm = 2, ReferralLetter = 3, ClinicalReport = 4,
    LabPrescription = 5, InsuranceDocument = 6, Identification = 7, Correspondence = 8,
    TreatmentPlan = 9, Invoice = 10, Receipt = 11, MedicalHistoryForm = 12, Scan = 13, Other = 99
}
