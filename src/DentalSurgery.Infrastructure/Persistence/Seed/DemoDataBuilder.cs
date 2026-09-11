using DentalSurgery.Application.Abstractions;
using DentalSurgery.Application.Billing;
using DentalSurgery.Domain.Common;
using DentalSurgery.Domain.Entities;
using DentalSurgery.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DentalSurgery.Infrastructure.Persistence.Seed;

/// <summary>
/// Generates a coherent demonstration dataset: patients with medical histories,
/// charted teeth, appointments spanning past and future, completed treatment,
/// invoices, payments and laboratory work. Deterministic, so the same database
/// is produced every time.
/// </summary>
public class DemoDataBuilder(DentalDbContext db, ILogger logger)
{
    private readonly Random _random = new(20260906);
    private int _planSequence;

    private static readonly string[] FirstNamesFemale =
    {
        "Aisha", "Beatrix", "Chloe", "Delphine", "Eleanor", "Farida", "Grace", "Heidi", "Imogen",
        "Jasmine", "Keira", "Lucia", "Margot", "Nadia", "Orla", "Petra", "Rosalind", "Saoirse",
        "Tamsin", "Ursula", "Verity", "Wren", "Yara", "Zainab"
    };

    private static readonly string[] FirstNamesMale =
    {
        "Alaric", "Bertrand", "Cormac", "Dominic", "Elliot", "Fabian", "Gideon", "Hamish", "Ivo",
        "Jonah", "Kwame", "Lucian", "Marcus", "Nikolai", "Oscar", "Piers", "Quentin", "Rafael",
        "Sebastian", "Theo", "Ulrich", "Viktor", "Wilfred", "Xavier"
    };

    private static readonly string[] Surnames =
    {
        "Abernathy", "Blackwood", "Castellano", "Delacroix", "Eriksson", "Fairweather", "Gallagher",
        "Hollingsworth", "Ivanova", "Jankowski", "Kowalczyk", "Lindqvist", "Mbeki", "Nakamura",
        "Oyelaran", "Pemberton", "Quintero", "Rasmussen", "Silvestri", "Thornbury", "Ubaldi",
        "Vasquez", "Whitfield", "Xiong", "Yilmaz", "Zieliński", "Ashworth", "Bhattacharya",
        "Carmichael", "Duvall", "Fitzgerald", "Goswami", "Haverford", "Ingerson", "Jayawardena"
    };

    private static readonly string[] Streets =
    {
        "Wentworth Gardens", "Bramble Lane", "Carlisle Terrace", "Duchess Walk", "Elmfield Road",
        "Fenwick Close", "Granville Avenue", "Hazelmere Drive", "Ivybridge Court", "Juniper Way",
        "Kestrel Rise", "Larkspur Street", "Merton Crescent", "Northgate Row", "Orchard Hill"
    };

    private static readonly string[] Occupations =
    {
        "Software engineer", "Secondary school teacher", "Architect", "Nurse", "Solicitor",
        "Graphic designer", "Chef", "Retired", "Student", "Accountant", "Electrician",
        "Paramedic", "Journalist", "Physiotherapist", "Delivery driver", "Retail manager",
        "Civil servant", "Musician", "Bus driver", "Research scientist"
    };

    private static readonly string[] ReferralSources =
    {
        "Existing patient referral", "Google search", "Passing trade", "Practice website",
        "Social media", "GP referral", "Specialist referral", "Insurance directory", "Local advertising"
    };

    public async Task BuildAsync(CancellationToken ct = default)
    {
        if (await db.Patients.AnyAsync(ct))
        {
            logger.LogInformation("Demonstration data already present; skipping.");
            return;
        }

        var context = await LoadContextAsync(ct);
        if (context is null)
        {
            logger.LogWarning("Reference data is missing, so demonstration data was not created.");
            return;
        }

        var patients = BuildPatients(context, 46);
        db.Patients.AddRange(patients);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Created {Count} demonstration patients.", patients.Count);

        await BuildClinicalHistoryAsync(context, patients, ct);
        await BuildScheduleAsync(context, patients, ct);
        await BuildSterilisationLogAsync(context, ct);
        await BuildLabCasesAsync(context, patients, ct);
        await BuildTasksAsync(context, patients, ct);
        await SyncNumberSequencesAsync(ct);

        logger.LogInformation("Demonstration data complete.");
    }

    /// <summary>
    /// The demo data assigns its own human-readable numbers, so the sequences
    /// must be advanced past them. Without this the first genuine registration
    /// would be handed a number that already exists.
    /// </summary>
    private async Task SyncNumberSequencesAsync(CancellationToken ct)
    {
        var counts = new Dictionary<string, int>
        {
            [SequenceNames.Patient] = await db.Patients.IgnoreQueryFilters().CountAsync(ct),
            [SequenceNames.Invoice] = await db.Invoices.IgnoreQueryFilters().CountAsync(ct),
            [SequenceNames.Payment] = await db.Payments.IgnoreQueryFilters().CountAsync(ct),
            [SequenceNames.Appointment] = await db.Appointments.IgnoreQueryFilters().CountAsync(ct),
            [SequenceNames.LabCase] = await db.LabCases.IgnoreQueryFilters().CountAsync(ct),
            [SequenceNames.TreatmentPlan] = await db.TreatmentPlans.IgnoreQueryFilters().CountAsync(ct)
        };

        var sequences = await db.NumberSequences
            .Where(s => counts.Keys.Contains(s.Name))
            .ToListAsync(ct);

        foreach (var sequence in sequences)
        {
            var next = counts[sequence.Name] + 1;
            if (sequence.NextValue < next) sequence.NextValue = next;

            // Year-scoped sequences reset annually; stamp the year the demo used.
            if (sequence.IncludeYear) sequence.ResetYear = DateTime.UtcNow.Year;
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Advanced {Count} number sequences past the demonstration data.", sequences.Count);
    }

    // ------------------------------------------------------------------ context

    private sealed class SeedContext
    {
        public required List<Staff> Providers { get; init; }
        public required List<Staff> AllStaff { get; init; }
        public required Staff Hygienist { get; init; }
        public required Staff Surgeon { get; init; }
        public required Location MainLocation { get; init; }
        public required List<Operatory> Operatories { get; init; }
        public required List<Tooth> Teeth { get; init; }
        public required Dictionary<string, ProcedureCode> Codes { get; init; }
        public required List<MedicalCondition> Conditions { get; init; }
        public required List<Allergen> Allergens { get; init; }
        public required List<Medication> Medications { get; init; }
        public required List<InsurancePlan> Plans { get; init; }
        public required List<DentalLaboratory> Labs { get; init; }
        public required List<Steriliser> Sterilisers { get; init; }
        public required List<InstrumentSet> InstrumentSets { get; init; }

        public ProcedureCode Code(string code) => Codes[code];
        public Tooth Tooth(int fdi) => Teeth.First(t => t.FdiNumber == fdi);
    }

    private async Task<SeedContext?> LoadContextAsync(CancellationToken ct)
    {
        var staff = await db.Staff.ToListAsync(ct);
        var teeth = await db.Teeth.ToListAsync(ct);
        var codes = await db.ProcedureCodes.ToListAsync(ct);
        // The primary site, whatever it is called. Looking for the literal code
        // "HARLEY" only ever matched a practice built by the demonstration
        // seeder itself: on an install that already had a practice, this
        // returned null and the whole build silently did nothing.
        var location = await db.Locations.FirstOrDefaultAsync(l => l.IsPrimary, ct)
                       ?? await db.Locations.OrderBy(l => l.Code).FirstOrDefaultAsync(ct);

        if (staff.Count == 0 || teeth.Count == 0 || codes.Count == 0 || location is null) return null;

        return new SeedContext
        {
            AllStaff = staff,
            Providers = staff.Where(s => s.IsProvider).ToList(),
            Hygienist = staff.First(s => s.Role == StaffRole.DentalHygienist),
            Surgeon = staff.First(s => s.Role == StaffRole.OralSurgeon),
            MainLocation = location,
            Operatories = await db.Operatories.Where(o => o.LocationId == location.Id)
                .OrderBy(o => o.DisplayOrder).ToListAsync(ct),
            Teeth = teeth,
            Codes = codes.ToDictionary(c => c.Code),
            Conditions = await db.MedicalConditions.ToListAsync(ct),
            Allergens = await db.Allergens.ToListAsync(ct),
            Medications = await db.Medications.ToListAsync(ct),
            Plans = await db.InsurancePlans.ToListAsync(ct),
            Labs = await db.DentalLaboratories.ToListAsync(ct),
            Sterilisers = await db.Sterilisers.ToListAsync(ct),
            InstrumentSets = await db.InstrumentSets.ToListAsync(ct)
        };
    }

    // ------------------------------------------------------------------ patients

    private List<Patient> BuildPatients(SeedContext context, int count)
    {
        var patients = new List<Patient>();
        var today = DateOnly.FromDateTime(DateTime.Today);
        var dentists = context.Providers.Where(p => p.Role != StaffRole.DentalHygienist &&
                                                    p.Role != StaffRole.DentalTherapist).ToList();

        for (var i = 1; i <= count; i++)
        {
            var isFemale = _random.Next(2) == 0;
            var first = isFemale ? Pick(FirstNamesFemale) : Pick(FirstNamesMale);
            var last = Pick(Surnames);

            // A realistic age spread, weighted towards working age.
            var age = _random.Next(100) switch
            {
                < 12 => _random.Next(3, 18),
                < 60 => _random.Next(18, 55),
                < 88 => _random.Next(55, 75),
                _ => _random.Next(75, 92)
            };

            var dob = today.AddYears(-age).AddDays(-_random.Next(0, 365));
            var registered = today.AddDays(-_random.Next(30, 2600));
            var recallMonths = _random.Next(10) < 7 ? 6 : _random.Next(10) < 8 ? 3 : 12;
            var lastExam = registered > today.AddMonths(-recallMonths)
                ? registered
                : today.AddDays(-_random.Next(15, recallMonths * 32));

            var provider = dentists[_random.Next(dentists.Count)];

            var patient = new Patient
            {
                PatientNumber = $"P-{i:D6}",
                Name = new PersonName
                {
                    Title = isFemale ? Pick(new[] { "Ms", "Mrs", "Miss", "Dr" }) : Pick(new[] { "Mr", "Dr", "Mr", "Mr" }),
                    FirstName = first,
                    LastName = last,
                    PreferredName = _random.Next(6) == 0 ? Shorten(first) : null
                },
                DateOfBirth = dob,
                Gender = isFemale ? Gender.Female : Gender.Male,
                MaritalStatus = age < 25 ? MaritalStatus.Single : Pick(new[]
                {
                    MaritalStatus.Single, MaritalStatus.Married, MaritalStatus.Married,
                    MaritalStatus.Divorced, MaritalStatus.CivilPartnership
                }),
                Contact = new ContactDetails
                {
                    MobilePhone = $"07{_random.Next(700, 999)} {_random.Next(100000, 999999)}",
                    HomePhone = _random.Next(3) == 0 ? $"020 7{_random.Next(100, 999)} {_random.Next(1000, 9999)}" : null,
                    Email = $"{first.ToLowerInvariant()}.{Ascii(last).ToLowerInvariant()}{_random.Next(1, 99)}@example.com",
                    PreferredContactMethod = Pick(new[] { "Email", "SMS", "Phone" })
                },
                Address = new Address
                {
                    Line1 = $"{_random.Next(1, 180)} {Pick(Streets)}",
                    City = "London",
                    County = "Greater London",
                    PostCode = $"{Pick(new[] { "N", "SE", "SW", "W", "NW", "E" })}{_random.Next(1, 20)} {_random.Next(1, 9)}{RandomLetters(2)}",
                    Country = "United Kingdom"
                },
                Occupation = age < 18 ? "School pupil" : age > 68 ? "Retired" : Pick(Occupations),
                NhsNumber = $"{_random.Next(400, 999)} {_random.Next(100, 999)} {_random.Next(1000, 9999)}",
                RegistrationDate = registered,
                Status = _random.Next(100) < 92 ? PatientStatus.Active : PatientStatus.Inactive,
                PrimaryProviderId = provider.Id,
                PrimaryHygienistId = context.Hygienist.Id,
                PreferredLocationId = context.MainLocation.Id,
                RecallIntervalMonths = recallMonths,
                LastExamDate = lastExam,
                LastHygieneDate = lastExam.AddDays(_random.Next(-40, 40)),
                NextRecallDue = lastExam.AddMonths(recallMonths),
                ReferralSource = Pick(ReferralSources),
                AllowEmail = true,
                AllowSms = _random.Next(10) < 9,
                AllowPhoneCall = true,
                AllowPost = _random.Next(10) < 4,
                AllowMarketing = _random.Next(10) < 3,
                ConsentToContactAtUtc = registered.ToDateTime(TimeOnly.MinValue),
                PrivacyNoticeAcceptedAtUtc = registered.ToDateTime(TimeOnly.MinValue),
                PreferredLanguage = _random.Next(12) == 0 ? Pick(new[] { "Polish", "Portuguese", "Bengali", "Somali" }) : "English"
            };

            patient.RequiresInterpreter = patient.PreferredLanguage != "English" && _random.Next(3) == 0;

            AddContacts(patient, age);
            AddMedicalHistory(context, patient, age);
            AddSocialHistory(patient, age);
            AddInsurance(context, patient);

            patients.Add(patient);
        }

        return patients;
    }

    private void AddContacts(Patient patient, int age)
    {
        if (age < 18)
        {
            patient.Contacts.Add(new PatientContact
            {
                PatientId = patient.Id,
                Name = new PersonName { FirstName = Pick(FirstNamesFemale), LastName = patient.Name.LastName },
                Relationship = ContactRelationship.Parent,
                Role = ContactRole.Guardian,
                Contact = new ContactDetails { MobilePhone = patient.Contact.MobilePhone, Email = patient.Contact.Email, PreferredContactMethod = "Phone" },
                IsPrimary = true,
                HasLegalAuthority = true
            });
            return;
        }

        if (_random.Next(10) < 6)
        {
            patient.Contacts.Add(new PatientContact
            {
                PatientId = patient.Id,
                Name = new PersonName
                {
                    FirstName = _random.Next(2) == 0 ? Pick(FirstNamesFemale) : Pick(FirstNamesMale),
                    LastName = _random.Next(2) == 0 ? patient.Name.LastName : Pick(Surnames)
                },
                Relationship = Pick(new[]
                {
                    ContactRelationship.Spouse, ContactRelationship.Partner,
                    ContactRelationship.Sibling, ContactRelationship.Child, ContactRelationship.Parent
                }),
                Role = ContactRole.Emergency,
                Contact = new ContactDetails { MobilePhone = $"07{_random.Next(700, 999)} {_random.Next(100000, 999999)}", PreferredContactMethod = "Phone" },
                IsPrimary = true
            });
        }
    }

    private void AddMedicalHistory(SeedContext context, Patient patient, int age)
    {
        // Older patients carry more conditions; children very few.
        var conditionCount = age switch
        {
            < 18 => _random.Next(0, 2),
            < 45 => _random.Next(0, 3),
            < 65 => _random.Next(0, 4),
            _ => _random.Next(1, 5)
        };

        var chosen = context.Conditions.OrderBy(_ => _random.Next()).Take(conditionCount).ToList();
        foreach (var condition in chosen)
        {
            if (condition.Code == "PREG" && (patient.Gender != Gender.Female || age is < 18 or > 45)) continue;

            patient.MedicalConditions.Add(new PatientMedicalCondition
            {
                PatientId = patient.Id,
                MedicalConditionId = condition.Id,
                Status = _random.Next(5) == 0 ? ConditionStatus.Resolved : ConditionStatus.Chronic,
                DiagnosedOn = DateOnly.FromDateTime(DateTime.Today).AddYears(-_random.Next(1, Math.Max(2, age / 3))),
                Severity = condition.DefaultSeverity,
                ManagedBy = _random.Next(2) == 0 ? "GP" : "Hospital consultant"
            });
        }

        // Roughly a fifth of patients have a recorded allergy.
        if (_random.Next(100) < 22)
        {
            var allergen = context.Allergens[_random.Next(context.Allergens.Count)];
            var severity = Pick(new[]
            {
                AllergySeverity.Mild, AllergySeverity.Moderate, AllergySeverity.Moderate,
                AllergySeverity.Severe, AllergySeverity.Anaphylaxis
            });

            patient.Allergies.Add(new PatientAllergy
            {
                PatientId = patient.Id,
                AllergenId = allergen.Id,
                AllergyType = allergen.AllergyType,
                Severity = severity,
                Reaction = severity switch
                {
                    AllergySeverity.Anaphylaxis => "Airway swelling and collapse; carries an adrenaline auto-injector.",
                    AllergySeverity.Severe => "Widespread urticaria and facial swelling.",
                    AllergySeverity.Moderate => "Rash and itching within an hour.",
                    _ => "Mild localised rash."
                },
                VerifiedBy = "Patient report"
            });

            if (severity >= AllergySeverity.Severe)
            {
                patient.Alerts.Add(new PatientAlert
                {
                    PatientId = patient.Id,
                    Category = AlertCategory.Allergy,
                    Severity = severity == AllergySeverity.Anaphylaxis ? AlertSeverity.Critical : AlertSeverity.High,
                    Title = $"{severity} allergy: {allergen.Name}",
                    Detail = "Confirm before prescribing or selecting materials.",
                    RaisedBy = "system"
                });
            }
        }

        // Current medication, more likely with age.
        var medicationCount = age > 55 ? _random.Next(0, 4) : _random.Next(0, 2);
        foreach (var medication in context.Medications
                     .Where(m => !m.IsAnaesthetic && m.DrugClass != null && !m.DrugClass.StartsWith("Emergency"))
                     .OrderBy(_ => _random.Next()).Take(medicationCount))
        {
            patient.Medications.Add(new PatientMedication
            {
                PatientId = patient.Id,
                MedicationId = medication.Id,
                Dosage = medication.DefaultDosage,
                Frequency = medication.DefaultFrequency,
                Route = medication.DefaultRoute,
                StartDate = DateOnly.FromDateTime(DateTime.Today).AddMonths(-_random.Next(1, 60)),
                IsCurrent = true,
                PrescribedBy = "GP"
            });
        }

        // A medical history review at the last visit.
        var isPregnant = patient.Gender == Gender.Female && age is >= 18 and <= 44 && _random.Next(40) == 0;

        patient.MedicalHistoryReviews.Add(new MedicalHistoryReview
        {
            PatientId = patient.Id,
            ReviewDate = patient.LastExamDate ?? patient.RegistrationDate,
            NoChangesReported = _random.Next(10) < 7,
            IsPregnant = isPregnant,
            WeeksPregnant = isPregnant ? _random.Next(6, 38) : null,
            TakingAnticoagulants = patient.Medications.Any(m => m.DisplayName.Contains("Aspirin", StringComparison.OrdinalIgnoreCase)),
            RequiresAntibioticProphylaxis = chosen.Any(c => c.RequiresAntibioticProphylaxis),
            AsaClassification = chosen.Count switch
            {
                0 => AsaClassification.AsaI,
                1 or 2 => AsaClassification.AsaII,
                3 => AsaClassification.AsaIII,
                _ => AsaClassification.AsaIII
            },
            PatientSignatureObtained = true
        });
    }

    private void AddSocialHistory(Patient patient, int age)
    {
        var smoking = age < 16 ? SmokingStatus.Never : Pick(new[]
        {
            SmokingStatus.Never, SmokingStatus.Never, SmokingStatus.Never, SmokingStatus.Never,
            SmokingStatus.Former, SmokingStatus.Light, SmokingStatus.Moderate, SmokingStatus.Vaping
        });

        db.SocialHistories.Add(new SocialHistory
        {
            PatientId = patient.Id,
            RecordedOn = patient.LastExamDate ?? patient.RegistrationDate,
            SmokingStatus = smoking,
            CigarettesPerDay = smoking is SmokingStatus.Light ? _random.Next(1, 10)
                : smoking is SmokingStatus.Moderate ? _random.Next(10, 20) : null,
            Alcohol = age < 18 ? AlcoholConsumption.None : Pick(new[]
            {
                AlcoholConsumption.None, AlcoholConsumption.Occasional, AlcoholConsumption.Occasional,
                AlcoholConsumption.Moderate, AlcoholConsumption.Heavy
            }),
            BrushingPerDay = Pick(new[] { 1, 2, 2, 2, 3 }),
            UsesFluorideToothpaste = true,
            Flosses = _random.Next(10) < 4,
            UsesInterdentalBrushes = _random.Next(10) < 5,
            UsesMouthwash = _random.Next(10) < 6,
            UsesElectricToothbrush = _random.Next(10) < 6,
            SugarIntakeEpisodesPerDay = _random.Next(1, 7),
            AcidicDrinkConsumption = _random.Next(10) < 4,
            Bruxism = _random.Next(10) < 3,
            OralHygiene = Pick(new[]
            {
                OralHygieneRating.Poor, OralHygieneRating.Fair, OralHygieneRating.Fair,
                OralHygieneRating.Good, OralHygieneRating.Good, OralHygieneRating.Excellent
            }),
            DentalAnxiety = _random.Next(10) < 3,
            AnxietyScore = _random.Next(5, 25)
        });
    }

    private void AddInsurance(SeedContext context, Patient patient)
    {
        if (context.Plans.Count == 0 || _random.Next(100) >= 45) return;

        var plan = context.Plans[_random.Next(context.Plans.Count)];
        patient.InsurancePolicies.Add(new PatientInsurance
        {
            PatientId = patient.Id,
            InsurancePlanId = plan.Id,
            Priority = InsurancePriority.Primary,
            MemberId = $"{_random.Next(10000000, 99999999)}",
            PolicyNumber = $"POL{_random.Next(100000, 999999)}",
            SubscriberIsPatient = true,
            EffectiveFrom = patient.RegistrationDate,
            BenefitsUsedThisYear = Math.Round((decimal)_random.Next(0, 900), 2),
            BenefitsVerifiedOn = DateOnly.FromDateTime(DateTime.Today).AddDays(-_random.Next(10, 300)),
            VerifiedBy = "Reception"
        });
    }

    // ------------------------------------------------------------------ clinical history

    private async Task BuildClinicalHistoryAsync(
        SeedContext context, List<Patient> patients, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var permanentTeeth = context.Teeth.Where(t => !t.IsPrimary).ToList();

        var records = new List<ToothConditionRecord>();
        var procedures = new List<Procedure>();
        var notes = new List<ClinicalNote>();
        var perioCharts = new List<PeriodontalChart>();
        var implants = new List<DentalImplant>();
        var radiographs = new List<RadiographRecord>();
        var plans = new List<TreatmentPlan>();

        foreach (var patient in patients)
        {
            var age = patient.AgeYears ?? 30;
            var provider = context.Providers.First(p => p.Id == patient.PrimaryProviderId);
            var teethForPatient = age < 13
                ? permanentTeeth.Where(t => t.PositionInQuadrant <= 6).ToList()
                : permanentTeeth;

            // --- historic restorations ------------------------------------
            var restorationCount = Math.Min(12, age / 6 + _random.Next(0, 4));
            var used = new HashSet<int>();

            for (var i = 0; i < restorationCount; i++)
            {
                var tooth = teethForPatient[_random.Next(teethForPatient.Count)];
                if (!used.Add(tooth.FdiNumber)) continue;

                var surfaces = RandomSurfaces(tooth);
                records.Add(new ToothConditionRecord
                {
                    PatientId = patient.Id,
                    ToothId = tooth.Id,
                    Surfaces = surfaces,
                    ConditionType = ToothConditionType.Restoration,
                    Status = ChartEntryStatus.Existing,
                    Material = _random.Next(3) == 0 ? RestorationMaterial.Amalgam : RestorationMaterial.CompositeResin,
                    RecordedOn = patient.RegistrationDate.AddDays(_random.Next(0, 60)),
                    RecordedByStaffId = provider.Id,
                    Notes = "Recorded at registration as pre-existing."
                });
            }

            // --- wisdom teeth --------------------------------------------
            if (age >= 18 && _random.Next(100) < 40)
            {
                foreach (var fdi in new[] { 18, 28, 38, 48 }.Where(_ => _random.Next(2) == 0))
                {
                    records.Add(new ToothConditionRecord
                    {
                        PatientId = patient.Id,
                        ToothId = context.Tooth(fdi).Id,
                        ConditionType = _random.Next(2) == 0 ? ToothConditionType.Missing : ToothConditionType.Impacted,
                        Surfaces = ToothSurface.Whole,
                        Status = ChartEntryStatus.Existing,
                        RecordedOn = patient.RegistrationDate,
                        RecordedByStaffId = provider.Id
                    });
                }
            }

            // --- active caries -------------------------------------------
            var cariesCount = _random.Next(100) < 35 ? _random.Next(1, 4) : 0;
            var cariousTeeth = new List<(Tooth Tooth, ToothSurface Surfaces)>();

            for (var i = 0; i < cariesCount; i++)
            {
                var tooth = teethForPatient[_random.Next(teethForPatient.Count)];
                if (used.Contains(tooth.FdiNumber)) continue;
                used.Add(tooth.FdiNumber);

                var surfaces = RandomSurfaces(tooth);
                cariousTeeth.Add((tooth, surfaces));

                records.Add(new ToothConditionRecord
                {
                    PatientId = patient.Id,
                    ToothId = tooth.Id,
                    Surfaces = surfaces,
                    ConditionType = ToothConditionType.Caries,
                    Status = ChartEntryStatus.Existing,
                    RecordedOn = patient.LastExamDate ?? today,
                    RecordedByStaffId = provider.Id,
                    SeverityScore = _random.Next(1, 4),
                    Notes = "Radiographic caries confirmed clinically."
                });
            }

            // --- treatment plan for the carious teeth ---------------------
            if (cariousTeeth.Count > 0)
            {
                plans.Add(BuildTreatmentPlan(context, patient, provider, cariousTeeth, today));
            }

            // --- completed treatment history -------------------------------
            var visitCount = Math.Min(8, 1 + (today.DayNumber - patient.RegistrationDate.DayNumber) / 220);
            for (var visit = 0; visit < visitCount; visit++)
            {
                var visitDate = patient.RegistrationDate.AddDays(_random.Next(0, Math.Max(1, today.DayNumber - patient.RegistrationDate.DayNumber)));
                if (visitDate > today) continue;

                var isHygiene = _random.Next(3) == 0;
                var code = isHygiene ? context.Code("D1110") : context.Code("D0120");
                var actingProvider = isHygiene ? context.Hygienist : provider;

                var procedure = new Procedure
                {
                    PatientId = patient.Id,
                    ProcedureCodeId = code.Id,
                    ProviderId = actingProvider.Id,
                    LocationId = context.MainLocation.Id,
                    DateOfService = visitDate.ToDateTime(new TimeOnly(_random.Next(9, 17), 0)),
                    Status = ProcedureStatus.Completed,
                    Fee = code.DefaultFee,
                    IsBillable = true,
                    StartedAtUtc = visitDate.ToDateTime(new TimeOnly(9, 0)),
                    CompletedAtUtc = visitDate.ToDateTime(new TimeOnly(9, 30))
                };
                procedures.Add(procedure);

                notes.Add(BuildExamNote(patient, actingProvider, visitDate, isHygiene, cariousTeeth.Count));

                if (!isHygiene && _random.Next(3) == 0)
                {
                    var radiographCode = context.Code("D0274");
                    procedures.Add(new Procedure
                    {
                        PatientId = patient.Id,
                        ProcedureCodeId = radiographCode.Id,
                        ProviderId = actingProvider.Id,
                        LocationId = context.MainLocation.Id,
                        DateOfService = procedure.DateOfService,
                        Status = ProcedureStatus.Completed,
                        Fee = radiographCode.DefaultFee,
                        IsBillable = true
                    });

                    radiographs.Add(new RadiographRecord
                    {
                        PatientId = patient.Id,
                        RadiographType = RadiographType.Bitewing,
                        TakenAtUtc = procedure.DateOfService,
                        TakenByStaffId = actingProvider.Id,
                        ToothNumbers = "16,17,26,27,36,37,46,47",
                        KiloVoltagePeak = 65m,
                        MilliAmperage = 7m,
                        ExposureSeconds = 0.16m,
                        DoseMicroSieverts = 5m,
                        LeadApronUsed = false,
                        JustificationReason = "Caries assessment at routine examination",
                        QualityRating = Pick(new[] { "Grade 1 - excellent", "Grade 1 - excellent", "Grade 2 - diagnostically acceptable" }),
                        Findings = cariousTeeth.Count > 0
                            ? "Interproximal radiolucencies consistent with the clinical findings."
                            : "No caries detected. Bone levels within normal limits.",
                        IsReported = true,
                        ReportedByStaffId = actingProvider.Id
                    });
                }
            }

            // --- periodontal chart for older or higher-risk patients --------
            if (age > 30 && _random.Next(100) < 55)
            {
                perioCharts.Add(BuildPerioChart(context, patient, teethForPatient, age, today));
            }

            // --- implant cases -----------------------------------------------
            if (age > 35 && _random.Next(100) < 12)
            {
                var (implant, implantProcedures, implantRecords) =
                    BuildImplantCase(context, patient, today);
                implants.Add(implant);
                procedures.AddRange(implantProcedures);
                records.AddRange(implantRecords);
            }
        }

        db.ToothConditionRecords.AddRange(records);
        db.Procedures.AddRange(procedures);
        db.ClinicalNotes.AddRange(notes);
        db.PeriodontalCharts.AddRange(perioCharts);
        db.RadiographRecords.AddRange(radiographs);
        db.TreatmentPlans.AddRange(plans);
        await db.SaveChangesAsync(ct);

        // Implants reference procedures, so they are saved afterwards.
        db.DentalImplants.AddRange(implants);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Created {Records} chart entries, {Procedures} procedures, {Notes} notes, {Perio} perio charts, {Plans} treatment plans.",
            records.Count, procedures.Count, notes.Count, perioCharts.Count, plans.Count);

        await BuildBillingAsync(patients, ct);
        await BuildClaimsAsync(patients, ct);
    }

    private TreatmentPlan BuildTreatmentPlan(
        SeedContext context, Patient patient, Staff provider,
        List<(Tooth Tooth, ToothSurface Surfaces)> carious, DateOnly today)
    {
        var plan = new TreatmentPlan
        {
            PatientId = patient.Id,
            PlanNumber = $"TP-{++_planSequence:D6}",
            Name = "Restorative treatment plan",
            ProviderId = provider.Id,
            CreatedOn = patient.LastExamDate ?? today,
            PresentedOn = patient.LastExamDate ?? today,
            ValidUntil = (patient.LastExamDate ?? today).AddDays(90),
            Status = Pick(new[]
            {
                TreatmentPlanStatus.Presented, TreatmentPlanStatus.Accepted,
                TreatmentPlanStatus.Accepted, TreatmentPlanStatus.PartiallyAccepted,
                TreatmentPlanStatus.InProgress, TreatmentPlanStatus.Declined
            }),
            PresentedBy = provider.DisplayName,
            RisksDiscussed = "Risk of pulpal involvement if restoration is deferred; possible need for root canal treatment.",
            AlternativesDiscussed = "Monitoring with preventive care, or extraction where the tooth is unrestorable."
        };

        var phase = new TreatmentPlanPhase
        {
            TreatmentPlanId = plan.Id,
            PhaseNumber = 1,
            Name = "Phase 1 - Stabilisation",
            ClinicalObjective = "Remove active caries and restore the affected teeth.",
            Priority = TreatmentPriority.High
        };

        var sequence = 1;
        foreach (var (tooth, surfaces) in carious)
        {
            var surfaceCount = SurfaceNotation.Split(surfaces).Count;
            var code = tooth.IsAnterior
                ? surfaceCount switch { 1 => "D2330", 2 => "D2331", _ => "D2332" }
                : surfaceCount switch { 1 => "D2391", 2 => "D2392", _ => "D2393" };

            var procedureCode = context.Code(code);
            var accepted = plan.Status is TreatmentPlanStatus.Accepted or TreatmentPlanStatus.InProgress ||
                           (plan.Status == TreatmentPlanStatus.PartiallyAccepted && _random.Next(2) == 0);

            phase.Items.Add(new TreatmentPlanItem
            {
                TreatmentPlanPhaseId = phase.Id,
                ProcedureCodeId = procedureCode.Id,
                ToothId = tooth.Id,
                Surfaces = surfaces,
                ProviderId = provider.Id,
                UnitFee = procedureCode.DefaultFee,
                Sequence = sequence++,
                Priority = TreatmentPriority.High,
                Status = plan.Status switch
                {
                    TreatmentPlanStatus.Declined => TreatmentPlanItemStatus.Declined,
                    TreatmentPlanStatus.Presented => TreatmentPlanItemStatus.Proposed,
                    _ => accepted ? TreatmentPlanItemStatus.Accepted : TreatmentPlanItemStatus.Declined
                },
                EstimatedInsurance = Math.Round(procedureCode.DefaultFee * 0.8m, 2),
                EstimatedPatient = Math.Round(procedureCode.DefaultFee * 0.2m, 2)
            });
        }

        plan.Phases.Add(phase);
        plan.TotalFee = phase.Items.Sum(i => i.GrossFee);
        plan.EstimatedInsurancePortion = phase.Items.Sum(i => i.EstimatedInsurance);
        plan.EstimatedPatientPortion = plan.TotalFee - plan.EstimatedInsurancePortion;
        plan.ConsentObtained = plan.Status is TreatmentPlanStatus.Accepted or TreatmentPlanStatus.InProgress;

        return plan;
    }

    private ClinicalNote BuildExamNote(
        Patient patient, Staff provider, DateOnly date, bool isHygiene, int cariesCount)
    {
        if (isHygiene)
        {
            return new ClinicalNote
            {
                PatientId = patient.Id,
                ProviderId = provider.Id,
                NoteDateUtc = date.ToDateTime(new TimeOnly(10, 0)),
                NoteType = ClinicalNoteType.Hygiene,
                Title = "Hygiene visit",
                Subjective = "Attends for routine scale and polish. No new complaints reported.",
                Objective = $"Generalised supragingival calculus, heaviest lingual to the lower anteriors. " +
                            $"Bleeding on probing at approximately {_random.Next(8, 35)}% of sites.",
                Assessment = "Plaque-induced gingivitis, localised.",
                Plan = "Ultrasonic scaling completed full mouth, followed by prophylaxis paste polish. " +
                       "Oral hygiene instruction reinforced with interdental brush sizing.",
                TreatmentProvided = "Full-mouth ultrasonic debridement and polish.",
                PatientInstructions = "Interdental brushes daily. Brush twice a day for two minutes with fluoride toothpaste.",
                NextVisitPlan = $"Recall in {patient.RecallIntervalMonths} months.",
                IsSigned = true,
                SignedAtUtc = date.ToDateTime(new TimeOnly(10, 45)),
                SignedBy = provider.DisplayName
            };
        }

        var findings = cariesCount > 0
            ? $"Caries identified on {cariesCount} tooth/teeth, confirmed radiographically."
            : "No new caries detected. Existing restorations are sound and well contoured.";

        return new ClinicalNote
        {
            PatientId = patient.Id,
            ProviderId = provider.Id,
            NoteDateUtc = date.ToDateTime(new TimeOnly(9, 30)),
            NoteType = ClinicalNoteType.Soap,
            Title = "Routine examination",
            ChiefComplaint = _random.Next(4) == 0
                ? "Occasional sensitivity to cold on the upper right."
                : "No complaints; attending for routine check-up.",
            Subjective = "Medical history reviewed and confirmed. No changes since the last visit.",
            Objective = $"Extra-oral examination unremarkable. No lymphadenopathy. TMJ within normal range. " +
                        $"Soft tissues healthy. {findings}",
            Assessment = cariesCount > 0
                ? $"Active caries affecting {cariesCount} tooth/teeth. Caries risk assessed as moderate."
                : "Dentally fit. Caries risk assessed as low.",
            Plan = cariesCount > 0
                ? "Treatment plan discussed and provided in writing. Restorations to be arranged."
                : $"Continue current regimen. Recall in {patient.RecallIntervalMonths} months.",
            SoftTissueExam = "Buccal mucosa, floor of mouth, tongue and oropharynx examined. No lesions seen.",
            OralCancerScreeningDone = true,
            OcclusionNotes = Pick(new[] { "Class I incisal relationship.", "Class II division 1 incisal relationship.", "Class III incisal relationship." }),
            NextVisitPlan = $"Recall in {patient.RecallIntervalMonths} months.",
            IsSigned = true,
            SignedAtUtc = date.ToDateTime(new TimeOnly(10, 0)),
            SignedBy = provider.DisplayName
        };
    }

    private PeriodontalChart BuildPerioChart(
        SeedContext context, Patient patient, List<Tooth> teeth, int age, DateOnly today)
    {
        // Disease severity is scaled by age so the dataset shows a realistic spread.
        var severity = age switch
        {
            < 40 => 0,
            < 55 => _random.Next(0, 2),
            < 70 => _random.Next(0, 3),
            _ => _random.Next(1, 4)
        };

        var chart = new PeriodontalChart
        {
            PatientId = patient.Id,
            ExamDate = patient.LastExamDate ?? today,
            ExaminerStaffId = context.Hygienist.Id,
            IsFullMouth = true
        };

        var sites = Enum.GetValues<PeriodontalSite>();
        foreach (var tooth in teeth.Where(t => t.PositionInQuadrant <= 7))
        {
            foreach (var site in sites)
            {
                var baseDepth = 2 + severity;
                var depth = Math.Clamp(baseDepth + _random.Next(-1, 2), 1, 11);

                // Molars and interproximal sites run deeper.
                if (tooth.ToothType == ToothType.Molar) depth += _random.Next(0, 2);
                if (site is PeriodontalSite.MesioBuccal or PeriodontalSite.DistoBuccal
                    or PeriodontalSite.MesioLingual or PeriodontalSite.DistoLingual) depth += _random.Next(0, 2);

                depth = Math.Clamp(depth, 1, 12);
                var recession = depth >= 5 && _random.Next(3) == 0 ? -_random.Next(1, 4) : 0;

                chart.Measurements.Add(new PeriodontalMeasurement
                {
                    PeriodontalChartId = chart.Id,
                    ToothId = tooth.Id,
                    Site = site,
                    PocketDepthMm = depth,
                    GingivalMarginMm = recession,
                    BleedingOnProbing = depth >= 4 ? _random.Next(10) < 7 : _random.Next(10) < 2,
                    PlaquePresent = _random.Next(10) < 4 + severity,
                    CalculusPresent = _random.Next(10) < 2 + severity,
                    Suppuration = depth >= 7 && _random.Next(10) < 2,
                    Mobility = depth >= 7 && _random.Next(3) == 0 ? MobilityGrade.GradeI : MobilityGrade.None,
                    Furcation = tooth.ToothType == ToothType.Molar && depth >= 6 && _random.Next(3) == 0
                        ? FurcationGrade.ClassI : FurcationGrade.None
                });
            }
        }

        var recorded = chart.Measurements.Count;
        chart.BleedingOnProbingPercent = recorded == 0 ? 0
            : Math.Round(100m * chart.Measurements.Count(m => m.BleedingOnProbing) / recorded, 1);
        chart.PlaqueScorePercent = recorded == 0 ? 0
            : Math.Round(100m * chart.Measurements.Count(m => m.PlaquePresent) / recorded, 1);

        chart.Diagnosis = severity switch
        {
            0 => PeriodontalDiagnosis.Gingivitis,
            1 => PeriodontalDiagnosis.PeriodontitisStageIGradeB,
            2 => PeriodontalDiagnosis.PeriodontitisStageIIGradeB,
            _ => PeriodontalDiagnosis.PeriodontitisStageIIIGradeB
        };

        chart.NextReviewDue = chart.ExamDate.AddMonths(severity >= 2 ? 3 : 6);
        chart.TreatmentRecommendation = severity switch
        {
            0 => "Oral hygiene reinforcement and routine scale and polish.",
            1 => "Non-surgical periodontal therapy, one quadrant per visit. Review at 3 months.",
            2 => "Subgingival instrumentation under local anaesthetic. Review at 8-12 weeks.",
            _ => "Full-mouth debridement, then reassess for possible surgical management. Consider referral."
        };

        return chart;
    }

    private (DentalImplant, List<Procedure>, List<ToothConditionRecord>) BuildImplantCase(
        SeedContext context, Patient patient, DateOnly today)
    {
        var candidates = new[] { 16, 26, 36, 46, 15, 25, 35, 45, 11, 21 };
        var fdi = candidates[_random.Next(candidates.Length)];
        var tooth = context.Tooth(fdi);

        var extractionDate = today.AddMonths(-_random.Next(9, 30));
        var placementDate = extractionDate.AddMonths(_random.Next(3, 5));
        var restorationDate = placementDate.AddMonths(4);
        var isRestored = restorationDate <= today;

        var procedures = new List<Procedure>();
        var records = new List<ToothConditionRecord>();

        // Extraction
        var extractionCode = context.Code("D7140");
        var extraction = new Procedure
        {
            PatientId = patient.Id,
            ProcedureCodeId = extractionCode.Id,
            ToothId = tooth.Id,
            ProviderId = context.Surgeon.Id,
            LocationId = context.MainLocation.Id,
            DateOfService = extractionDate.ToDateTime(new TimeOnly(11, 0)),
            Status = ProcedureStatus.Completed,
            Fee = extractionCode.DefaultFee,
            Surfaces = ToothSurface.Whole,
            IsBillable = true,
            Notes = "Tooth unrestorable due to sub-crestal fracture. Atraumatic extraction with socket preservation."
        };
        procedures.Add(extraction);

        db.SurgicalRecords.Add(new SurgicalRecord
        {
            ProcedureId = extraction.Id,
            SurgeryType = SurgeryType.SimpleExtraction,
            IncisionTimeUtc = extraction.DateOfService,
            ClosureTimeUtc = extraction.DateOfService.AddMinutes(28),
            DurationMinutes = 28,
            SutureRequired = true,
            SutureMaterial = "Vicryl 4-0",
            SutureCount = 2,
            SutureRemovalDue = extractionDate.AddDays(10),
            GraftPlaced = true,
            GraftMaterial = "Bio-Oss xenograft 0.5g",
            MembranePlaced = true,
            MembraneType = "Bio-Gide resorbable collagen",
            HaemostasisAchieved = true,
            Outcome = SurgicalOutcome.Uneventful,
            PostOperativeInstructionsGiven = true,
            FollowUpDue = extractionDate.AddDays(10),
            OperativeNote =
                "Local anaesthetic administered. Atraumatic extraction using periotomes and forceps; " +
                "the tooth was delivered intact. Socket curetted and irrigated with sterile saline. " +
                "Buccal plate confirmed intact. Xenograft placed to the level of the crest and covered " +
                "with a resorbable collagen membrane. Two interrupted sutures placed. " +
                "Haemostasis achieved. Post-operative instructions given verbally and in writing."
        });

        records.Add(new ToothConditionRecord
        {
            PatientId = patient.Id, ToothId = tooth.Id, Surfaces = ToothSurface.Whole,
            ConditionType = ToothConditionType.Extracted, Status = ChartEntryStatus.Completed,
            RecordedOn = extractionDate, RecordedByStaffId = context.Surgeon.Id, ProcedureId = extraction.Id
        });

        // Placement
        var placementCode = context.Code("D6010");
        var placement = new Procedure
        {
            PatientId = patient.Id,
            ProcedureCodeId = placementCode.Id,
            ToothId = tooth.Id,
            ProviderId = context.Surgeon.Id,
            LocationId = context.MainLocation.Id,
            DateOfService = placementDate.ToDateTime(new TimeOnly(14, 0)),
            Status = ProcedureStatus.Completed,
            Fee = placementCode.DefaultFee,
            IsBillable = true,
            Notes = "Guided implant placement following CBCT planning."
        };
        procedures.Add(placement);

        var torque = _random.Next(28, 46);
        var isq = _random.Next(62, 80);

        db.SurgicalRecords.Add(new SurgicalRecord
        {
            ProcedureId = placement.Id,
            SurgeryType = SurgeryType.ImplantPlacement,
            IncisionTimeUtc = placement.DateOfService,
            ClosureTimeUtc = placement.DateOfService.AddMinutes(62),
            DurationMinutes = 62,
            FlapRaised = true,
            FlapDesign = "Mid-crestal incision with intrasulcular extensions, no relieving incisions",
            BoneRemoval = true,
            SutureRequired = true,
            SutureMaterial = "Vicryl 4-0",
            SutureCount = 3,
            SutureRemovalDue = placementDate.AddDays(10),
            HaemostasisAchieved = true,
            Irrigation = "Copious chilled sterile saline throughout osteotomy preparation",
            Instrumentation = "Guided drill kit under surgical guide, sequential osteotomy to final diameter",
            InstrumentSetId = context.InstrumentSets.FirstOrDefault(s => s.SetCode == "IS-IMPL-01")?.Id,
            Outcome = SurgicalOutcome.Uneventful,
            NerveProximityWarningGiven = fdi is 36 or 46 or 35 or 45,
            SinusExposure = false,
            PostOperativeInstructionsGiven = true,
            PostOperativeMedication = "Amoxicillin 500mg tds for 5 days; ibuprofen 400mg tds prn; chlorhexidine 0.2% bd",
            FollowUpDue = placementDate.AddDays(10),
            Findings = $"Bone quality assessed as D2. Primary stability achieved at {torque} Ncm.",
            OperativeNote =
                $"Pre-operative CBCT reviewed and a surgical guide fabricated. Local anaesthetic administered " +
                $"and a mid-crestal incision raised with a full-thickness mucoperiosteal flap. Osteotomy prepared " +
                $"through the guide under copious chilled saline irrigation. Implant placed at the planned " +
                $"three-dimensional position, 1mm sub-crestal, achieving {torque} Ncm insertion torque. " +
                $"Resonance frequency analysis recorded an ISQ of {isq}. Cover screw placed and the flap closed " +
                $"passively with interrupted sutures. Post-operative radiograph confirmed satisfactory position."
        });

        db.AnaesthesiaRecords.Add(new AnaesthesiaRecord
        {
            ProcedureId = placement.Id,
            AnaesthesiaType = AnaesthesiaType.LocalBlock,
            AdministeredByStaffId = context.Surgeon.Id,
            StartedAtUtc = placement.DateOfService.AddMinutes(-10),
            EndedAtUtc = placement.DateOfService.AddMinutes(62),
            ConsentObtained = true,
            PreOperativeAssessmentDone = true,
            AsaClassification = AsaClassification.AsaII,
            TopicalApplied = true,
            TopicalAgent = "Benzocaine 20% gel",
            BaselineSystolicBp = _random.Next(112, 145),
            BaselineDiastolicBp = _random.Next(68, 90),
            BaselinePulse = _random.Next(62, 88),
            BaselineOxygenSaturation = _random.Next(96, 100),
            MonitoringPulseOximetry = true,
            MonitoringBloodPressure = true,
            DischargeCriteriaMet = true,
            Doses =
            {
                new AnaesthesiaAgentDose
                {
                    AgentName = "Articaine 4%",
                    Concentration = "4%",
                    Vasoconstrictor = "Adrenaline 1:100,000",
                    Cartridges = 2,
                    MillilitresPerCartridge = 1.7m,
                    MilligramsPerMillilitre = 40m,
                    Technique = fdi > 30 ? InjectionTechnique.InferiorAlveolarNerveBlock : InjectionTechnique.Infiltration,
                    Site = $"Tooth {fdi} region",
                    AdministeredAtUtc = placement.DateOfService.AddMinutes(-10),
                    AspirationNegative = true,
                    BatchNumber = $"AR{_random.Next(10000, 99999)}"
                }
            }
        });

        records.Add(new ToothConditionRecord
        {
            PatientId = patient.Id, ToothId = tooth.Id, Surfaces = ToothSurface.Root,
            ConditionType = ToothConditionType.Implant, Status = ChartEntryStatus.Completed,
            Material = RestorationMaterial.Titanium,
            RecordedOn = placementDate, RecordedByStaffId = context.Surgeon.Id, ProcedureId = placement.Id
        });

        // Restoration
        if (isRestored)
        {
            var crownCode = context.Code("D6058");
            var crown = new Procedure
            {
                PatientId = patient.Id,
                ProcedureCodeId = crownCode.Id,
                ToothId = tooth.Id,
                ProviderId = patient.PrimaryProviderId,
                LocationId = context.MainLocation.Id,
                DateOfService = restorationDate.ToDateTime(new TimeOnly(10, 30)),
                Status = ProcedureStatus.Completed,
                Fee = crownCode.DefaultFee,
                Material = RestorationMaterial.Zirconia,
                ShadeReference = Pick(new[] { "A1", "A2", "A3", "B1", "B2" }),
                IsBillable = true
            };
            procedures.Add(crown);

            records.Add(new ToothConditionRecord
            {
                PatientId = patient.Id, ToothId = tooth.Id, Surfaces = ToothSurface.Whole,
                ConditionType = ToothConditionType.ImplantCrown, Status = ChartEntryStatus.Completed,
                Material = RestorationMaterial.Zirconia, ShadeReference = crown.ShadeReference,
                RecordedOn = restorationDate, RecordedByStaffId = patient.PrimaryProviderId,
                ProcedureId = crown.Id
            });
        }

        var implant = new DentalImplant
        {
            PatientId = patient.Id,
            ToothId = tooth.Id,
            PlacementProcedureId = placement.Id,
            Manufacturer = "Nobel Biocare",
            SystemName = "NobelActive",
            ReferenceNumber = fdi is 11 or 21 ? "36531" : "36547",
            LotNumber = $"NB{_random.Next(1000000, 9999999)}",
            DiameterMm = fdi is 11 or 21 ? 3.5m : 4.3m,
            LengthMm = fdi is 11 or 21 ? 11.5m : 10m,
            Platform = "NP conical connection",
            SurfaceTreatment = "TiUnite anodised",
            ConnectionType = "Internal conical",
            PlacementDate = placementDate,
            SurgeonStaffId = context.Surgeon.Id,
            InsertionTorqueNcm = torque,
            StabilityQuotientIsq = isq,
            BoneQuality = BoneQuality.D2,
            GuidedSurgery = true,
            GraftUsed = true,
            GraftDetail = "Socket preservation at extraction with Bio-Oss and Bio-Gide.",
            MembraneUsed = true,
            HealingAbutmentDate = placementDate.AddMonths(3),
            SecondStageDate = placementDate.AddMonths(3),
            ImpressionDate = isRestored ? restorationDate.AddMonths(-1) : null,
            RestorationDate = isRestored ? restorationDate : null,
            AbutmentType = isRestored ? "Custom titanium abutment" : null,
            RestorationType = isRestored ? "Screw-retained monolithic zirconia crown" : null,
            Status = isRestored ? ImplantStatus.Restored : ImplantStatus.Integrating,
            WarrantyYears = 10,
            NextReviewDue = today.AddMonths(_random.Next(1, 7))
        };

        return (implant, procedures, records);
    }

    // ------------------------------------------------------------------ billing

    private async Task BuildBillingAsync(List<Patient> patients, CancellationToken ct)
    {
        var invoices = new List<Invoice>();
        var payments = new List<Payment>();
        var allocations = new List<PaymentAllocation>();
        var ledger = new List<LedgerEntry>();
        var sequence = 1;
        var paymentSequence = 1;

        foreach (var patient in patients)
        {
            var completed = await db.Procedures
                .Include(p => p.ProcedureCode)
                .Include(p => p.Tooth)
                .Where(p => p.PatientId == patient.Id && p.Status == ProcedureStatus.Completed && p.IsBillable)
                .OrderBy(p => p.DateOfService)
                .ToListAsync(ct);

            if (completed.Count == 0) continue;

            var balance = 0m;

            foreach (var group in completed.GroupBy(p => DateOnly.FromDateTime(p.DateOfService)))
            {
                var issueDate = group.Key;
                var invoice = new Invoice
                {
                    PatientId = patient.Id,
                    InvoiceNumber = $"INV-{issueDate.Year}-{sequence++:D6}",
                    IssueDate = issueDate,
                    DueDate = issueDate.AddDays(30),
                    ProviderId = group.First().ProviderId,
                    Status = InvoiceStatus.Issued
                };

                var lineNumber = 1;
                foreach (var procedure in group)
                {
                    var line = new InvoiceLine
                    {
                        InvoiceId = invoice.Id,
                        ProcedureId = procedure.Id,
                        ProcedureCodeId = procedure.ProcedureCodeId,
                        ToothId = procedure.ToothId,
                        Sequence = lineNumber++,
                        Description = procedure.Description,
                        ServiceDate = issueDate,
                        Quantity = procedure.Quantity,
                        UnitPrice = procedure.Fee,
                        ProviderId = procedure.ProviderId,
                        PatientPortion = procedure.Fee
                    };
                    invoice.Lines.Add(line);
                    procedure.InvoiceLineId = line.Id;
                }

                invoice.Subtotal = invoice.Lines.Sum(l => l.Gross);
                invoice.Total = Math.Round(invoice.Lines.Sum(l => l.LineTotal), 2);

                balance += invoice.Total;
                ledger.Add(new LedgerEntry
                {
                    PatientId = patient.Id,
                    EntryDate = issueDate,
                    EntryType = LedgerEntryType.Charge,
                    Description = $"Invoice {invoice.InvoiceNumber}",
                    Debit = invoice.Total,
                    InvoiceId = invoice.Id,
                    Reference = invoice.InvoiceNumber,
                    RunningBalance = Math.Round(balance, 2)
                });

                // Most invoices are settled; a minority remain outstanding.
                var settlement = _random.Next(100);
                if (settlement < 78)
                {
                    var payment = new Payment
                    {
                        PatientId = patient.Id,
                        PaymentNumber = $"PAY-{issueDate.Year}-{paymentSequence++:D6}",
                        PaymentDate = issueDate.AddDays(_random.Next(0, 25)),
                        Amount = invoice.Total,
                        Method = Pick(new[]
                        {
                            PaymentMethod.CreditCard, PaymentMethod.DebitCard, PaymentMethod.DebitCard,
                            PaymentMethod.Cash, PaymentMethod.BankTransfer, PaymentMethod.Insurance
                        }),
                        Status = PaymentStatus.Cleared,
                        ReceivedBy = "Reception"
                    };

                    payments.Add(payment);
                    allocations.Add(new PaymentAllocation
                    {
                        PaymentId = payment.Id, InvoiceId = invoice.Id,
                        Amount = invoice.Total, AllocatedOn = payment.PaymentDate
                    });

                    invoice.AmountPaid = invoice.Total;
                    invoice.Status = InvoiceStatus.Paid;
                    invoice.PaidInFullOn = payment.PaymentDate;

                    balance -= invoice.Total;
                    ledger.Add(new LedgerEntry
                    {
                        PatientId = patient.Id,
                        EntryDate = payment.PaymentDate,
                        EntryType = payment.Method == PaymentMethod.Insurance
                            ? LedgerEntryType.InsurancePayment : LedgerEntryType.Payment,
                        Description = $"Payment by {payment.Method}",
                        Credit = payment.Amount,
                        PaymentId = payment.Id,
                        InvoiceId = invoice.Id,
                        Reference = payment.PaymentNumber,
                        RunningBalance = Math.Round(balance, 2)
                    });
                }
                else if (settlement < 88)
                {
                    var part = Math.Round(invoice.Total * 0.5m, 2);
                    var payment = new Payment
                    {
                        PatientId = patient.Id,
                        PaymentNumber = $"PAY-{issueDate.Year}-{paymentSequence++:D6}",
                        PaymentDate = issueDate.AddDays(_random.Next(1, 20)),
                        Amount = part,
                        Method = PaymentMethod.DebitCard,
                        Status = PaymentStatus.Cleared,
                        ReceivedBy = "Reception"
                    };

                    payments.Add(payment);
                    allocations.Add(new PaymentAllocation
                    {
                        PaymentId = payment.Id, InvoiceId = invoice.Id,
                        Amount = part, AllocatedOn = payment.PaymentDate
                    });

                    invoice.AmountPaid = part;
                    invoice.Status = InvoiceStatus.PartiallyPaid;

                    balance -= part;
                    ledger.Add(new LedgerEntry
                    {
                        PatientId = patient.Id,
                        EntryDate = payment.PaymentDate,
                        EntryType = LedgerEntryType.Payment,
                        Description = "Part payment received",
                        Credit = part,
                        PaymentId = payment.Id,
                        InvoiceId = invoice.Id,
                        Reference = payment.PaymentNumber,
                        RunningBalance = Math.Round(balance, 2)
                    });
                }
                else if (invoice.DueDate < DateOnly.FromDateTime(DateTime.Today))
                {
                    invoice.Status = InvoiceStatus.Overdue;
                }

                invoices.Add(invoice);
            }

            patient.AccountBalance = Math.Round(balance, 2);
        }

        db.Invoices.AddRange(invoices);
        db.Payments.AddRange(payments);
        await db.SaveChangesAsync(ct);

        db.PaymentAllocations.AddRange(allocations);
        db.LedgerEntries.AddRange(ledger);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Created {Invoices} invoices and {Payments} payments.",
            invoices.Count, payments.Count);
    }


    // ------------------------------------------------------------------ insurance claims

    /// <summary>
    /// Raises insurance claims against the invoices of insured patients, spread
    /// across the lifecycle so the claims screen, the ageing report and the 837D
    /// generator all have something real to work with.
    /// </summary>
    private async Task BuildClaimsAsync(List<Patient> patients, CancellationToken ct)
    {
        var estimator = new InsuranceEstimator();
        var today = DateOnly.FromDateTime(DateTime.Today);
        var claims = new List<InsuranceClaim>();
        var sequence = 1;

        var insured = await db.PatientInsurances
            .Include(i => i.InsurancePlan).ThenInclude(p => p!.InsuranceCarrier)
            .Where(i => patients.Select(p => p.Id).Contains(i.PatientId))
            .ToListAsync(ct);

        var byPatient = insured.GroupBy(i => i.PatientId).ToDictionary(g => g.Key, g => g.First());
        if (byPatient.Count == 0) return;

        // InvoiceLine records the procedure only by id, and the claim needs the
        // surfaces that were treated, so they are looked up once up front.
        var surfaces = await db.Procedures
            .Where(p => byPatient.Keys.Contains(p.PatientId) && p.Surfaces != ToothSurface.None)
            .Select(p => new { p.Id, p.Surfaces })
            .ToDictionaryAsync(p => p.Id, p => p.Surfaces, ct);

        var invoices = await db.Invoices
            .Include(i => i.Lines).ThenInclude(l => l.ProcedureCode)
            .Where(i => byPatient.Keys.Contains(i.PatientId))
            .OrderBy(i => i.IssueDate)
            .ToListAsync(ct);

        foreach (var group in invoices.GroupBy(i => i.PatientId))
        {
            var insurance = byPatient[group.Key];
            var plan = insurance.InsurancePlan;
            if (plan?.InsuranceCarrier is null) continue;

            // Not every invoice goes to the insurer; take the most recent few.
            foreach (var invoice in group.OrderByDescending(i => i.IssueDate).Take(_random.Next(1, 4)))
            {
                var billable = invoice.Lines.Where(l => l.ProcedureCode is not null).ToList();
                if (billable.Count == 0) continue;

                var claim = new InsuranceClaim
                {
                    ClaimNumber = $"CLM-{invoice.IssueDate.Year}-{sequence++:D6}",
                    PatientId = invoice.PatientId,
                    PatientInsuranceId = insurance.Id,
                    InvoiceId = invoice.Id,
                    ProviderId = invoice.ProviderId,
                    ServiceDate = invoice.IssueDate,
                    SubmissionMethod = "Electronic",
                    Notes = $"Raised from invoice {invoice.InvoiceNumber}."
                };

                var lineNumber = 1;
                foreach (var line in billable)
                {
                    var gross = Math.Round(line.LineTotal, 2);
                    var estimate = estimator.Estimate(line.ProcedureCode!, gross, insurance);

                    claim.Lines.Add(new InsuranceClaimLine
                    {
                        InsuranceClaimId = claim.Id,
                        ProcedureId = line.ProcedureId,
                        ProcedureCodeId = line.ProcedureCodeId!.Value,
                        ToothId = line.ToothId,
                        Sequence = lineNumber++,
                        SurfaceCode = line.ProcedureId is { } procedureId && surfaces.TryGetValue(procedureId, out var treated)
                            ? SurfaceNotation.ToCode(treated)
                            : null,
                        ServiceDate = line.ServiceDate,
                        ChargedAmount = gross,
                        AllowedAmount = estimate.AllowedAmount,
                        DeductibleAmount = estimate.DeductibleApplied
                    });
                }

                claim.TotalCharged = Math.Round(claim.Lines.Sum(l => l.ChargedAmount), 2);
                claim.TotalAllowed = Math.Round(claim.Lines.Sum(l => l.AllowedAmount), 2);

                ApplyClaimLifecycle(claim, plan, today);
                claims.Add(claim);
            }
        }

        db.InsuranceClaims.AddRange(claims);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Created {Claims} insurance claims across {Patients} insured patients.",
            claims.Count, claims.Select(c => c.PatientId).Distinct().Count());
    }

    /// <summary>
    /// Walks a claim to a plausible point in its life: some are still being
    /// prepared, some are with the payer, most of the older ones have settled,
    /// and a few came back denied.
    /// </summary>
    private void ApplyClaimLifecycle(InsuranceClaim claim, InsurancePlan plan, DateOnly today)
    {
        var age = today.DayNumber - claim.ServiceDate.DayNumber;
        var roll = _random.Next(100);

        // A claim raised in the last fortnight has usually not been sent yet.
        if (age < 14)
        {
            claim.Status = roll < 45 ? ClaimStatus.Draft : ClaimStatus.ReadyToSend;
            claim.PatientResponsibility = Math.Round(claim.TotalCharged - claim.TotalAllowed, 2);
            return;
        }

        var submitted = claim.ServiceDate.AddDays(_random.Next(1, 6));
        claim.SubmittedOn = submitted;
        claim.PayerClaimReference = $"{plan.InsuranceCarrier!.PayerId}-{_random.Next(100000, 999999)}";

        var settlementDays = Math.Max(7, plan.InsuranceCarrier.TypicalPaymentDays);

        // Still in flight.
        if (age < settlementDays || roll < 12)
        {
            claim.Status = roll % 3 == 0 ? ClaimStatus.InReview : ClaimStatus.Submitted;
            claim.AcknowledgedOn = submitted.AddDays(_random.Next(1, 4));
            claim.PatientResponsibility = Math.Round(claim.TotalCharged - claim.TotalAllowed, 2);
            return;
        }

        var adjudicated = submitted.AddDays(_random.Next(5, settlementDays + 10));
        if (adjudicated > today) adjudicated = today;
        claim.AcknowledgedOn = submitted.AddDays(1);
        claim.AdjudicatedOn = adjudicated;

        // Denied outright.
        if (roll >= 92)
        {
            claim.Status = ClaimStatus.Denied;
            claim.DenialCode = Pick(new[] { "CO-97", "CO-29", "PR-204", "CO-16" });
            claim.DenialReason = Pick(new[]
            {
                "The benefit for this service is included in the payment for another service.",
                "The time limit for filing has expired.",
                "This service is not covered under the patient's current plan.",
                "The claim lacks information needed for adjudication."
            });
            claim.PatientResponsibility = claim.TotalCharged;
            claim.AppealSubmitted = _random.Next(100) < 35;
            if (claim.AppealSubmitted)
            {
                claim.Status = ClaimStatus.Appealed;
                claim.AppealDate = adjudicated.AddDays(_random.Next(3, 21));
                claim.AppealNotes = "Radiographs and a clinical narrative were sent in support.";
            }
            return;
        }

        // Paid, in full or in part.
        var partial = roll >= 78;
        var paidRatio = partial ? 0.55m + (decimal)_random.NextDouble() * 0.25m : 1m;
        var paid = Math.Round(claim.TotalAllowed * paidRatio, 2);

        foreach (var line in claim.Lines)
        {
            line.PaidAmount = Math.Round(line.AllowedAmount * paidRatio, 2);
            line.CoInsuranceAmount = Math.Round(line.AllowedAmount - line.PaidAmount, 2);
            line.WriteOffAmount = Math.Round(line.ChargedAmount - line.AllowedAmount, 2);
            line.AdjudicationCode = partial ? "CO-45" : "PAID";
        }

        claim.TotalPaid = paid;
        claim.WriteOffAmount = Math.Round(claim.TotalCharged - claim.TotalAllowed, 2);
        claim.PatientResponsibility = Math.Round(claim.TotalAllowed - paid, 2);
        claim.PaidOn = adjudicated.AddDays(_random.Next(1, 10));
        if (claim.PaidOn > today) claim.PaidOn = today;
        claim.Status = partial ? ClaimStatus.PartiallyApproved : ClaimStatus.Paid;
    }
    // ------------------------------------------------------------------ schedule

    private async Task BuildScheduleAsync(SeedContext context, List<Patient> patients, CancellationToken ct)
    {
        var appointments = new List<Appointment>();
        var recalls = new List<RecallSchedule>();
        var today = DateTime.Today;
        var sequence = 1;

        var active = patients.Where(p => p.Status == PatientStatus.Active).ToList();
        var chairProviders = context.Providers.ToList();

        // Bookings from four weeks back to six weeks ahead.
        for (var dayOffset = -28; dayOffset <= 42; dayOffset++)
        {
            var date = today.AddDays(dayOffset);
            if (date.DayOfWeek is DayOfWeek.Sunday) continue;

            var isPast = dayOffset < 0;
            var slotsToday = date.DayOfWeek == DayOfWeek.Saturday ? _random.Next(3, 7) : _random.Next(8, 18);

            for (var slot = 0; slot < slotsToday; slot++)
            {
                var patient = active[_random.Next(active.Count)];
                var provider = chairProviders[_random.Next(chairProviders.Count)];
                var operatory = context.Operatories[_random.Next(context.Operatories.Count)];

                var hour = 9 + (slot * 30 / 60);
                var minute = (slot * 30) % 60;
                if (hour >= 17) continue;

                var start = date.AddHours(hour).AddMinutes(minute);
                var duration = provider.DefaultAppointmentMinutes;

                // Avoid double-booking a provider or a room.
                if (appointments.Any(a =>
                        (a.ProviderId == provider.Id || a.OperatoryId == operatory.Id) &&
                        a.StartUtc < start.AddMinutes(duration) && start < a.EndUtc))
                    continue;

                if (appointments.Any(a => a.PatientId == patient.Id &&
                                          a.StartUtc.Date == start.Date))
                    continue;

                var type = provider.Role == StaffRole.DentalHygienist
                    ? AppointmentType.Hygiene
                    : Pick(new[]
                    {
                        AppointmentType.RoutineExam, AppointmentType.RoutineExam, AppointmentType.Restorative,
                        AppointmentType.Restorative, AppointmentType.Consultation, AppointmentType.Emergency,
                        AppointmentType.Endodontic, AppointmentType.Extraction, AppointmentType.PostOperativeReview
                    });

                var status = isPast
                    ? Pick(new[]
                    {
                        AppointmentStatus.Completed, AppointmentStatus.Completed, AppointmentStatus.Completed,
                        AppointmentStatus.Completed, AppointmentStatus.CheckedOut, AppointmentStatus.Completed,
                        AppointmentStatus.NoShow, AppointmentStatus.Cancelled
                    })
                    : dayOffset == 0
                        ? Pick(new[]
                        {
                            AppointmentStatus.Confirmed, AppointmentStatus.ArrivedWaiting,
                            AppointmentStatus.Seated, AppointmentStatus.Completed, AppointmentStatus.Confirmed
                        })
                        : Pick(new[]
                        {
                            AppointmentStatus.Confirmed, AppointmentStatus.Confirmed, AppointmentStatus.Unconfirmed
                        });

                var appointment = new Appointment
                {
                    AppointmentNumber = $"A-{date.Year}-{sequence++:D8}",
                    PatientId = patient.Id,
                    ProviderId = provider.Id,
                    LocationId = context.MainLocation.Id,
                    OperatoryId = operatory.Id,
                    StartUtc = start,
                    EndUtc = start.AddMinutes(duration),
                    AppointmentType = type,
                    Status = status,
                    IsRecallVisit = type is AppointmentType.RoutineExam or AppointmentType.Hygiene,
                    IsEmergency = type == AppointmentType.Emergency,
                    Priority = type == AppointmentType.Emergency ? TreatmentPriority.Urgent : TreatmentPriority.Routine,
                    Reason = DescribeReason(type),
                    BookedAtUtc = start.AddDays(-_random.Next(3, 40)),
                    BookedBy = "Reception"
                };

                if (status is AppointmentStatus.Confirmed or AppointmentStatus.ArrivedWaiting
                    or AppointmentStatus.Seated or AppointmentStatus.Completed or AppointmentStatus.CheckedOut)
                {
                    appointment.ConfirmedAtUtc = start.AddDays(-1);
                    appointment.ConfirmedVia = "SMS";
                }

                if (status is AppointmentStatus.ArrivedWaiting or AppointmentStatus.Seated
                    or AppointmentStatus.Completed or AppointmentStatus.CheckedOut)
                    appointment.ArrivedAtUtc = start.AddMinutes(-_random.Next(2, 15));

                if (status is AppointmentStatus.Seated or AppointmentStatus.Completed or AppointmentStatus.CheckedOut)
                    appointment.SeatedAtUtc = start.AddMinutes(_random.Next(0, 12));

                if (status is AppointmentStatus.Completed or AppointmentStatus.CheckedOut)
                {
                    appointment.TreatmentStartedAtUtc = appointment.SeatedAtUtc?.AddMinutes(3);
                    appointment.CompletedAtUtc = start.AddMinutes(duration - _random.Next(0, 8));
                }

                if (status == AppointmentStatus.CheckedOut)
                    appointment.CheckedOutAtUtc = appointment.CompletedAtUtc?.AddMinutes(5);

                if (status is AppointmentStatus.Cancelled or AppointmentStatus.NoShow)
                {
                    appointment.CancelledAtUtc = start.AddDays(-_random.Next(0, 3));
                    appointment.CancellationReason = status == AppointmentStatus.NoShow
                        ? "Did not attend"
                        : Pick(new[] { "Patient unwell", "Work commitment", "Rearranged by patient", "Transport problem" });
                }

                appointments.Add(appointment);
            }
        }

        // Recall schedules
        foreach (var patient in patients)
        {
            recalls.Add(new RecallSchedule
            {
                PatientId = patient.Id,
                RecallType = RecallType.RoutineExam,
                IntervalMonths = patient.RecallIntervalMonths,
                LastCompletedDate = patient.LastExamDate,
                DueDate = patient.NextRecallDue ?? DateOnly.FromDateTime(today).AddMonths(patient.RecallIntervalMonths),
                Status = patient.Status != PatientStatus.Active ? RecallStatus.Suspended
                    : patient.NextRecallDue < DateOnly.FromDateTime(today) ? RecallStatus.Overdue
                    : RecallStatus.Scheduled,
                PreferredProviderId = patient.PrimaryProviderId,
                IsActive = patient.Status == PatientStatus.Active,
                ContactAttempts = patient.NextRecallDue < DateOnly.FromDateTime(today) ? _random.Next(0, 3) : 0
            });

            if (_random.Next(3) == 0)
            {
                recalls.Add(new RecallSchedule
                {
                    PatientId = patient.Id,
                    RecallType = RecallType.ScaleAndPolish,
                    IntervalMonths = patient.RecallIntervalMonths,
                    LastCompletedDate = patient.LastHygieneDate,
                    DueDate = (patient.LastHygieneDate ?? DateOnly.FromDateTime(today))
                        .AddMonths(patient.RecallIntervalMonths),
                    Status = RecallStatus.Scheduled,
                    PreferredProviderId = context.Hygienist.Id,
                    IsActive = patient.Status == PatientStatus.Active
                });
            }
        }

        db.Appointments.AddRange(appointments);
        db.RecallSchedules.AddRange(recalls);
        await db.SaveChangesAsync(ct);

        // A handful of patients waiting for an earlier slot.
        var waitlist = active.OrderBy(_ => _random.Next()).Take(6).Select(p => new WaitlistEntry
        {
            PatientId = p.Id,
            PreferredProviderId = p.PrimaryProviderId,
            LocationId = context.MainLocation.Id,
            AppointmentType = Pick(new[] { AppointmentType.RoutineExam, AppointmentType.Restorative, AppointmentType.Hygiene }),
            ProcedureDescription = "Would like any earlier cancellation",
            EstimatedMinutes = 30,
            AvailableFrom = DateOnly.FromDateTime(today),
            AvailableUntil = DateOnly.FromDateTime(today).AddMonths(2),
            Priority = Pick(new[] { WaitlistPriority.Normal, WaitlistPriority.Normal, WaitlistPriority.High }),
            Status = WaitlistStatus.Waiting,
            MorningOk = true,
            AfternoonOk = _random.Next(2) == 0
        }).ToList();

        db.WaitlistEntries.AddRange(waitlist);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Created {Appointments} appointments and {Recalls} recall schedules.",
            appointments.Count, recalls.Count);
    }

    private static string DescribeReason(AppointmentType type) => type switch
    {
        AppointmentType.RoutineExam => "Routine examination and radiographs if indicated",
        AppointmentType.Hygiene => "Scale and polish",
        AppointmentType.Restorative => "Restoration as per treatment plan",
        AppointmentType.Emergency => "Pain, seen as an emergency",
        AppointmentType.Endodontic => "Root canal treatment",
        AppointmentType.Extraction => "Extraction as planned",
        AppointmentType.Consultation => "Treatment plan discussion",
        AppointmentType.PostOperativeReview => "Post-operative review and suture removal",
        _ => "Dental appointment"
    };

    // ------------------------------------------------------------------ sterilisation

    private async Task BuildSterilisationLogAsync(SeedContext context, CancellationToken ct)
    {
        if (context.Sterilisers.Count == 0) return;

        var cycles = new List<SterilisationCycle>();
        var nurse = context.AllStaff.FirstOrDefault(s => s.Role == StaffRole.DentalNurse);
        var today = DateTime.Today;

        foreach (var steriliser in context.Sterilisers)
        {
            var cycleNumber = 1;
            for (var dayOffset = -45; dayOffset <= 0; dayOffset++)
            {
                var date = today.AddDays(dayOffset);
                if (date.DayOfWeek == DayOfWeek.Sunday) continue;

                var runsToday = _random.Next(2, 5);
                for (var run = 0; run < runsToday; run++)
                {
                    // One deliberate failure in the record, so the trace workflow has something to show.
                    var failed = dayOffset == -12 && run == 1 && steriliser.Name.Contains("Autoclave 2");
                    var started = date.AddHours(8 + (run * 3)).AddMinutes(_random.Next(0, 50));

                    cycles.Add(new SterilisationCycle
                    {
                        SteriliserId = steriliser.Id,
                        CycleNumber = cycleNumber++,
                        StartedAtUtc = started,
                        CompletedAtUtc = started.AddMinutes(_random.Next(38, 52)),
                        Program = SterilisationProgram.Vacuum134,
                        PeakTemperatureCelsius = failed ? 128.4m : 135m + (decimal)(_random.NextDouble() * 0.8),
                        PeakPressureBar = failed ? 1.9m : 2.1m + (decimal)(_random.NextDouble() * 0.1),
                        HoldTimeMinutes = failed ? 2 : 3,
                        ChemicalIndicatorPass = !failed,
                        HelixTestPass = !failed,
                        VacuumLeakTestPass = true,
                        BiologicalIndicatorPass = run == 0 ? !failed : null,
                        BiologicalIndicatorReadDate = run == 0 ? DateOnly.FromDateTime(date.AddDays(1)) : null,
                        PrinterRecordAttached = true,
                        Result = failed ? SterilisationResult.Fail : SterilisationResult.Pass,
                        OperatorStaffId = nurse?.Id,
                        ItemCount = _random.Next(6, 16),
                        LoadContents = "Mixed cassettes and pouched hand instruments",
                        FailureReason = failed
                            ? "Sterilisation temperature not reached; cycle aborted by the unit."
                            : null,
                        CorrectiveAction = failed
                            ? "Load quarantined and reprocessed on Autoclave 1. Engineer attended; door seal replaced. " +
                              "Helix and biological tests repeated and passed before the unit was returned to service."
                            : null
                    });
                }
            }

            steriliser.NextCycleNumber = cycleNumber;
        }

        db.SterilisationCycles.AddRange(cycles);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Created {Count} sterilisation cycle records.", cycles.Count);
    }

    // ------------------------------------------------------------------ laboratory

    private async Task BuildLabCasesAsync(SeedContext context, List<Patient> patients, CancellationToken ct)
    {
        if (context.Labs.Count == 0) return;

        var cases = new List<LabCase>();
        var today = DateOnly.FromDateTime(DateTime.Today);
        var candidates = patients.Where(p => p.Status == PatientStatus.Active)
            .OrderBy(_ => _random.Next()).Take(14).ToList();

        var sequence = 1;
        foreach (var patient in candidates)
        {
            var lab = context.Labs[_random.Next(context.Labs.Count)];
            var caseType = Pick(new[]
            {
                LabCaseType.Crown, LabCaseType.Crown, LabCaseType.Bridge, LabCaseType.Veneer,
                LabCaseType.PartialDenture, LabCaseType.Nightguard, LabCaseType.ImplantCrown,
                LabCaseType.CompleteDenture, LabCaseType.Retainer
            });

            var sent = today.AddDays(-_random.Next(0, 40));
            var due = sent.AddDays(lab.StandardTurnaroundDays);
            var received = due <= today && _random.Next(10) < 7 ? due.AddDays(_random.Next(-1, 3)) : (DateOnly?)null;

            var status = received.HasValue
                ? (_random.Next(3) == 0 ? LabCaseStatus.Delivered : LabCaseStatus.Received)
                : due < today ? LabCaseStatus.InProduction
                : LabCaseStatus.Sent;

            cases.Add(new LabCase
            {
                CaseNumber = $"LAB-{today.Year}-{sequence++:D5}",
                PatientId = patient.Id,
                DentalLaboratoryId = lab.Id,
                ProviderId = patient.PrimaryProviderId,
                CaseType = caseType,
                Status = status,
                ToothNumbers = caseType switch
                {
                    LabCaseType.Bridge => "14,15,16",
                    LabCaseType.CompleteDenture or LabCaseType.PartialDenture => null,
                    _ => $"{Pick(new[] { 11, 14, 15, 16, 24, 25, 36, 46 })}"
                },
                Arch = caseType is LabCaseType.CompleteDenture or LabCaseType.PartialDenture
                    or LabCaseType.Nightguard or LabCaseType.Retainer
                    ? Pick(new[] { DentalArch.Upper, DentalArch.Lower })
                    : null,
                Shade = caseType is LabCaseType.Nightguard or LabCaseType.Retainer
                    ? null : Pick(new[] { "A1", "A2", "A3", "A3.5", "B1", "B2", "C1" }),
                ShadeGuide = "VITA Classical",
                Material = caseType switch
                {
                    LabCaseType.Crown or LabCaseType.ImplantCrown => RestorationMaterial.Zirconia,
                    LabCaseType.Veneer => RestorationMaterial.LithiumDisilicate,
                    LabCaseType.CompleteDenture or LabCaseType.PartialDenture => RestorationMaterial.Acrylic,
                    LabCaseType.Bridge => RestorationMaterial.PorcelainFusedToMetal,
                    _ => RestorationMaterial.Acrylic
                },
                DigitalImpression = lab.AcceptsDigitalImpressions && _random.Next(2) == 0,
                PhysicalImpressionSent = _random.Next(2) == 0,
                BiteRegistrationSent = true,
                OppositingModelSent = true,
                SentDate = sent,
                DueDate = due,
                ReceivedDate = received,
                DeliveryDate = status == LabCaseStatus.Delivered ? received?.AddDays(_random.Next(1, 8)) : null,
                LabFee = caseType switch
                {
                    LabCaseType.Crown or LabCaseType.ImplantCrown => 148m,
                    LabCaseType.Bridge => 420m,
                    LabCaseType.Veneer => 165m,
                    LabCaseType.CompleteDenture => 385m,
                    LabCaseType.PartialDenture => 340m,
                    _ => 95m
                },
                Instructions = caseType switch
                {
                    LabCaseType.Crown => "Monolithic zirconia, screw-retained where possible. Please leave contacts slightly light.",
                    LabCaseType.Bridge => "Three-unit PFM bridge. Modified ridge lap pontic. Metal occlusal on the molar.",
                    LabCaseType.Nightguard => "Hard acrylic full-arch guard, 2mm thickness, canine guidance.",
                    _ => "Standard construction; please return with the articulated models."
                }
            });
        }

        db.LabCases.AddRange(cases);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Created {Count} laboratory cases.", cases.Count);
    }

    // ------------------------------------------------------------------ tasks

    private async Task BuildTasksAsync(SeedContext context, List<Patient> patients, CancellationToken ct)
    {
        var manager = context.AllStaff.FirstOrDefault(s => s.Role == StaffRole.PracticeManager);
        var receptionist = context.AllStaff.FirstOrDefault(s => s.Role == StaffRole.Receptionist);
        var today = DateOnly.FromDateTime(DateTime.Today);

        var tasks = new List<WorkTask>
        {
            new() { Title = "Chase outstanding insurance claims over 30 days",
                    Description = "Review the claims worklist and follow up anything unpaid after 30 days.",
                    Category = "Billing", AssignedToStaffId = manager?.Id,
                    DueDate = today.AddDays(2), Priority = TreatmentPriority.High },
            new() { Title = "Order implant components for next week's surgical list",
                    Description = "Two NobelActive 4.3 x 10mm and healing abutments required.",
                    Category = "Inventory", AssignedToStaffId = manager?.Id,
                    DueDate = today.AddDays(3), Priority = TreatmentPriority.Urgent },
            new() { Title = "Book the autoclave annual validation",
                    Description = "Autoclave 2 validation is due; arrange the engineer visit.",
                    Category = "Compliance", AssignedToStaffId = manager?.Id,
                    DueDate = today.AddDays(14), Priority = TreatmentPriority.Routine },
            new() { Title = "Call overdue recall patients",
                    Description = "Work through the recall list, prioritising anyone more than six months overdue.",
                    Category = "Recall", AssignedToStaffId = receptionist?.Id,
                    DueDate = today.AddDays(1), Priority = TreatmentPriority.Routine },
            new() { Title = "Confirm tomorrow's unconfirmed appointments",
                    Category = "Scheduling", AssignedToStaffId = receptionist?.Id,
                    DueDate = today, Priority = TreatmentPriority.High },
            new() { Title = "Review the failed sterilisation cycle and complete the incident record",
                    Description = "Autoclave 2 cycle failure. Confirm the affected sets were reprocessed and " +
                                  "record the corrective action in the compliance file.",
                    Category = "Compliance", AssignedToStaffId = manager?.Id,
                    DueDate = today.AddDays(-2), Priority = TreatmentPriority.Urgent }
        };

        var followUps = patients.Where(p => p.Status == PatientStatus.Active)
            .OrderBy(_ => _random.Next()).Take(4)
            .Select(p => new WorkTask
            {
                Title = $"Follow up treatment plan with {p.Name.Display}",
                Description = "Plan was presented but no decision has been recorded. Call to discuss.",
                Category = "TreatmentPlan",
                PatientId = p.Id,
                AssignedToStaffId = receptionist?.Id,
                DueDate = today.AddDays(_random.Next(1, 10)),
                Priority = TreatmentPriority.Routine
            });

        tasks.AddRange(followUps);

        db.WorkTasks.AddRange(tasks);
        await db.SaveChangesAsync(ct);
    }

    // ------------------------------------------------------------------ helpers

    private T Pick<T>(IReadOnlyList<T> items) => items[_random.Next(items.Count)];

    private ToothSurface RandomSurfaces(Tooth tooth)
    {
        var primary = tooth.HasOcclusalSurface ? ToothSurface.Occlusal : ToothSurface.Incisal;
        return _random.Next(10) switch
        {
            < 4 => primary,
            < 7 => ToothSurface.Mesial | primary,
            < 9 => ToothSurface.Distal | primary,
            _ => ToothSurface.Mesial | primary | ToothSurface.Distal
        };
    }

    private string RandomLetters(int count) =>
        new(Enumerable.Range(0, count).Select(_ => (char)('A' + _random.Next(0, 26))).ToArray());

    private static string Shorten(string name) => name.Length <= 4 ? name : name[..Math.Min(4, name.Length)];

    /// <summary>Strips accents so generated email addresses stay ASCII.</summary>
    private static string Ascii(string value)
    {
        var normalised = value.Normalize(System.Text.NormalizationForm.FormD);
        var chars = normalised.Where(c =>
            System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) !=
            System.Globalization.UnicodeCategory.NonSpacingMark &&
            char.IsLetterOrDigit(c));
        return new string(chars.ToArray());
    }
}
