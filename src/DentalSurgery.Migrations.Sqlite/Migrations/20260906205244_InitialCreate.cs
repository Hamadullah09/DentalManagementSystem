using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalSurgery.Migrations.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Allergens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AllergyType = table.Column<int>(type: "INTEGER", nullable: false),
                    CrossReactants = table.Column<string>(type: "TEXT", nullable: true),
                    IsCommonInDentistry = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsSystem = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Allergens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Key = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: true),
                    Category = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    DataType = table.Column<string>(type: "TEXT", nullable: false),
                    IsSystem = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsEncrypted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TimestampUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Action = table.Column<int>(type: "INTEGER", nullable: false),
                    EntityName = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    EntityId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    IpAddress = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "TEXT", nullable: true),
                    OldValues = table.Column<string>(type: "TEXT", nullable: true),
                    NewValues = table.Column<string>(type: "TEXT", nullable: true),
                    ChangedColumns = table.Column<string>(type: "TEXT", nullable: true),
                    PatientId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConsentFormTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Body = table.Column<string>(type: "TEXT", nullable: false),
                    Risks = table.Column<string>(type: "TEXT", nullable: true),
                    Benefits = table.Column<string>(type: "TEXT", nullable: true),
                    Alternatives = table.Column<string>(type: "TEXT", nullable: true),
                    Version = table.Column<string>(type: "TEXT", nullable: false),
                    RequiresWitness = table.Column<bool>(type: "INTEGER", nullable: false),
                    ValidForDays = table.Column<int>(type: "INTEGER", nullable: true),
                    AppliesToProcedureCodes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsSystem = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsentFormTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DentalLaboratories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    AccountNumber = table.Column<string>(type: "TEXT", nullable: true),
                    ContactPerson = table.Column<string>(type: "TEXT", nullable: true),
                    Contact_Mobile = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    Contact_Home = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    Contact_Work = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    Contact_Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Contact_Preferred = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false, defaultValue: "Any"),
                    Address_Line1 = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Address_Line2 = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Address_City = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Address_County = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Address_PostCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Address_Country = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false, defaultValue: "United Kingdom"),
                    Specialities = table.Column<string>(type: "TEXT", nullable: true),
                    StandardTurnaroundDays = table.Column<int>(type: "INTEGER", nullable: false),
                    AcceptsDigitalImpressions = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsPreferred = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DentalLaboratories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InsuranceCarriers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    PayerId = table.Column<string>(type: "TEXT", nullable: true),
                    ElectronicPayerId = table.Column<string>(type: "TEXT", nullable: true),
                    Address_Line1 = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Address_Line2 = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Address_City = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Address_County = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Address_PostCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Address_Country = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false, defaultValue: "United Kingdom"),
                    ClaimsAddress_Line1 = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ClaimsAddress_Line2 = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ClaimsAddress_City = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ClaimsAddress_County = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ClaimsAddress_PostCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    ClaimsAddress_Country = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Contact_Mobile = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    Contact_Home = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    Contact_Work = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    Contact_Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Contact_Preferred = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false, defaultValue: "Any"),
                    ClaimsPhone = table.Column<string>(type: "TEXT", nullable: true),
                    Website = table.Column<string>(type: "TEXT", nullable: true),
                    AcceptsElectronicClaims = table.Column<bool>(type: "INTEGER", nullable: false),
                    TypicalPaymentDays = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsuranceCarriers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MedicalConditions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Icd10Code = table.Column<string>(type: "TEXT", nullable: true),
                    Category = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    RequiresAntibioticProphylaxis = table.Column<bool>(type: "INTEGER", nullable: false),
                    IncreasesBleedingRisk = table.Column<bool>(type: "INTEGER", nullable: false),
                    AffectsAnaesthesia = table.Column<bool>(type: "INTEGER", nullable: false),
                    AffectsHealing = table.Column<bool>(type: "INTEGER", nullable: false),
                    ContraindicatesAdrenaline = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequiresSteroidCover = table.Column<bool>(type: "INTEGER", nullable: false),
                    DefaultSeverity = table.Column<int>(type: "INTEGER", nullable: false),
                    ClinicalGuidance = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsSystem = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalConditions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Medications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GenericName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    BrandNames = table.Column<string>(type: "TEXT", nullable: true),
                    DrugClass = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    Form = table.Column<int>(type: "INTEGER", nullable: false),
                    Strength = table.Column<string>(type: "TEXT", nullable: true),
                    DefaultRoute = table.Column<int>(type: "INTEGER", nullable: false),
                    DefaultDosage = table.Column<string>(type: "TEXT", nullable: true),
                    DefaultFrequency = table.Column<string>(type: "TEXT", nullable: true),
                    DefaultDurationDays = table.Column<int>(type: "INTEGER", nullable: true),
                    DefaultQuantity = table.Column<int>(type: "INTEGER", nullable: true),
                    DefaultInstructions = table.Column<string>(type: "TEXT", nullable: true),
                    IsControlledDrug = table.Column<bool>(type: "INTEGER", nullable: false),
                    ControlledSchedule = table.Column<string>(type: "TEXT", nullable: true),
                    IsAntibiotic = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsAnalgesic = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsAnaesthetic = table.Column<bool>(type: "INTEGER", nullable: false),
                    ContraindicatedInPregnancy = table.Column<bool>(type: "INTEGER", nullable: false),
                    ContraindicatedInBreastfeeding = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequiresRenalAdjustment = table.Column<bool>(type: "INTEGER", nullable: false),
                    Interactions = table.Column<string>(type: "TEXT", nullable: true),
                    Contraindications = table.Column<string>(type: "TEXT", nullable: true),
                    SideEffects = table.Column<string>(type: "TEXT", nullable: true),
                    UnitCost = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsSystem = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Medications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MessageTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Channel = table.Column<int>(type: "INTEGER", nullable: false),
                    Subject = table.Column<string>(type: "TEXT", nullable: true),
                    Body = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", nullable: true),
                    AvailableTokens = table.Column<string>(type: "TEXT", nullable: true),
                    IsDefaultForCategory = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsSystem = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NumberSequences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Prefix = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    Suffix = table.Column<string>(type: "TEXT", nullable: true),
                    NextValue = table.Column<long>(type: "INTEGER", nullable: false),
                    PadWidth = table.Column<int>(type: "INTEGER", nullable: false),
                    IncludeYear = table.Column<bool>(type: "INTEGER", nullable: false),
                    ResetYear = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NumberSequences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pharmacies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Address_Line1 = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Address_Line2 = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Address_City = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Address_County = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Address_PostCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Address_Country = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false, defaultValue: "United Kingdom"),
                    Contact_Mobile = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    Contact_Home = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    Contact_Work = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    Contact_Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Contact_Preferred = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false, defaultValue: "Any"),
                    FaxNumber = table.Column<string>(type: "TEXT", nullable: true),
                    PharmacyCode = table.Column<string>(type: "TEXT", nullable: true),
                    AcceptsElectronicPrescriptions = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pharmacies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PostOperativeInstructions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Body = table.Column<string>(type: "TEXT", nullable: false),
                    AppliesToProcedureCodes = table.Column<string>(type: "TEXT", nullable: true),
                    AppliesToSurgeryType = table.Column<int>(type: "INTEGER", nullable: true),
                    WarningSigns = table.Column<string>(type: "TEXT", nullable: true),
                    EmergencyContactText = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsSystem = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostOperativeInstructions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Practices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    LegalName = table.Column<string>(type: "TEXT", nullable: true),
                    RegistrationNumber = table.Column<string>(type: "TEXT", nullable: true),
                    TaxNumber = table.Column<string>(type: "TEXT", nullable: true),
                    RegulatorNumber = table.Column<string>(type: "TEXT", nullable: true),
                    Address_Line1 = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Address_Line2 = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Address_City = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Address_County = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Address_PostCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Address_Country = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false, defaultValue: "United Kingdom"),
                    Contact_Mobile = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    Contact_Home = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    Contact_Work = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    Contact_Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Contact_Preferred = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false, defaultValue: "Any"),
                    Website = table.Column<string>(type: "TEXT", nullable: true),
                    LogoPath = table.Column<string>(type: "TEXT", nullable: true),
                    CurrencyCode = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    CurrencySymbol = table.Column<string>(type: "TEXT", maxLength: 5, nullable: false),
                    TimeZoneId = table.Column<string>(type: "TEXT", nullable: false),
                    Locale = table.Column<string>(type: "TEXT", nullable: false),
                    DefaultAppointmentMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    DefaultRecallIntervalMonths = table.Column<int>(type: "INTEGER", nullable: false),
                    DefaultTaxRatePercent = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    InvoicePaymentTermDays = table.Column<int>(type: "INTEGER", nullable: false),
                    InvoiceFooterText = table.Column<string>(type: "TEXT", nullable: true),
                    BankDetails = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Practices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProcedureCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ShortDescription = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    LongDescription = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    SubCategory = table.Column<string>(type: "TEXT", nullable: true),
                    DefaultFee = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    DefaultDurationMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    RequiresTooth = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequiresSurfaces = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequiresQuadrant = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequiresArch = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequiresRootCount = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsSurgical = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequiresAnaesthesia = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequiresLabWork = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequiresConsent = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequiresRadiograph = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDiagnosticOnly = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsHygieneProcedure = table.Column<bool>(type: "INTEGER", nullable: false),
                    GeneratesRecall = table.Column<bool>(type: "INTEGER", nullable: false),
                    RecallIntervalMonths = table.Column<int>(type: "INTEGER", nullable: true),
                    TypicalVisits = table.Column<int>(type: "INTEGER", nullable: false),
                    InsuranceCategory = table.Column<string>(type: "TEXT", nullable: true),
                    TypicalInsuranceCoveragePercent = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: true),
                    IsTaxable = table.Column<bool>(type: "INTEGER", nullable: false),
                    ClinicalNoteTemplate = table.Column<string>(type: "TEXT", nullable: true),
                    PostOperativeInstructionCode = table.Column<string>(type: "TEXT", nullable: true),
                    ColourHex = table.Column<string>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcedureCodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    IsSystemRole = table.Column<bool>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SavedViews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Module = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    OwnerUserId = table.Column<string>(type: "TEXT", nullable: true),
                    IsShared = table.Column<bool>(type: "INTEGER", nullable: false),
                    FilterJson = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedViews", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    AccountNumber = table.Column<string>(type: "TEXT", nullable: true),
                    ContactPerson = table.Column<string>(type: "TEXT", nullable: true),
                    Contact_Mobile = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    Contact_Home = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    Contact_Work = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    Contact_Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Contact_Preferred = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false, defaultValue: "Any"),
                    Address_Line1 = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Address_Line2 = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Address_City = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Address_County = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Address_PostCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Address_Country = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false, defaultValue: "United Kingdom"),
                    Website = table.Column<string>(type: "TEXT", nullable: true),
                    PaymentTerms = table.Column<string>(type: "TEXT", nullable: true),
                    LeadTimeDays = table.Column<int>(type: "INTEGER", nullable: false),
                    MinimumOrderValue = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: true),
                    IsPreferred = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Teeth",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FdiNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    UniversalNumber = table.Column<string>(type: "TEXT", maxLength: 4, nullable: false),
                    PalmerNotation = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    ShortName = table.Column<string>(type: "TEXT", maxLength: 24, nullable: false),
                    Arch = table.Column<int>(type: "INTEGER", nullable: false),
                    Quadrant = table.Column<int>(type: "INTEGER", nullable: false),
                    ToothType = table.Column<int>(type: "INTEGER", nullable: false),
                    IsPrimary = table.Column<bool>(type: "INTEGER", nullable: false),
                    PositionInQuadrant = table.Column<int>(type: "INTEGER", nullable: false),
                    RootCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CanalCount = table.Column<int>(type: "INTEGER", nullable: false),
                    HasOcclusalSurface = table.Column<bool>(type: "INTEGER", nullable: false),
                    ValidSurfaces = table.Column<int>(type: "INTEGER", nullable: false),
                    ChartOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teeth", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    FirstName = table.Column<string>(type: "TEXT", nullable: true),
                    LastName = table.Column<string>(type: "TEXT", nullable: true),
                    StaffId = table.Column<Guid>(type: "TEXT", nullable: true),
                    JobTitle = table.Column<string>(type: "TEXT", nullable: true),
                    DefaultLocationId = table.Column<Guid>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    MustChangePassword = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastLoginUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastLoginIp = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AvatarPath = table.Column<string>(type: "TEXT", nullable: true),
                    ThemePreference = table.Column<string>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: true),
                    SecurityStamp = table.Column<string>(type: "TEXT", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true),
                    PhoneNumber = table.Column<string>(type: "TEXT", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FeeSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    ScheduleType = table.Column<int>(type: "INTEGER", nullable: false),
                    InsuranceCarrierId = table.Column<Guid>(type: "TEXT", nullable: true),
                    EffectiveFrom = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    BlanketAdjustmentPercent = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeeSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeeSchedules_InsuranceCarriers_InsuranceCarrierId",
                        column: x => x.InsuranceCarrierId,
                        principalTable: "InsuranceCarriers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Locations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PracticeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Address_Line1 = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Address_Line2 = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Address_City = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Address_County = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Address_PostCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Address_Country = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false, defaultValue: "United Kingdom"),
                    Contact_Mobile = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    Contact_Home = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    Contact_Work = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    Contact_Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Contact_Preferred = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false, defaultValue: "Any"),
                    ColourHex = table.Column<string>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsPrimary = table.Column<bool>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Locations_Practices_PracticeId",
                        column: x => x.PracticeId,
                        principalTable: "Practices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RoleId = table.Column<string>(type: "TEXT", nullable: false),
                    ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                    ClaimValue = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleClaims_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Sku = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    SubCategory = table.Column<string>(type: "TEXT", nullable: true),
                    Manufacturer = table.Column<string>(type: "TEXT", nullable: true),
                    ManufacturerPartNumber = table.Column<string>(type: "TEXT", nullable: true),
                    Barcode = table.Column<string>(type: "TEXT", nullable: true),
                    UnitOfMeasure = table.Column<string>(type: "TEXT", nullable: false),
                    UnitsPerPack = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentStock = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    ReorderLevel = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    ReorderQuantity = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    MaximumStock = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: true),
                    UnitCost = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    LastPurchasePrice = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: true),
                    LastPurchaseDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    SellingPrice = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: true),
                    PreferredSupplierId = table.Column<Guid>(type: "TEXT", nullable: true),
                    LocationId = table.Column<Guid>(type: "TEXT", nullable: true),
                    StorageLocation = table.Column<string>(type: "TEXT", nullable: true),
                    RequiresLotTracking = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequiresExpiryTracking = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsControlledSubstance = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsSterilisable = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsSingleUse = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequiresColdStorage = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    SafetyDataSheetPath = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryItems_Suppliers_PreferredSupplierId",
                        column: x => x.PreferredSupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrderNumber = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    SupplierId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LocationId = table.Column<Guid>(type: "TEXT", nullable: true),
                    OrderDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    ExpectedDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    ReceivedDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Subtotal = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    TaxAmount = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    ShippingCost = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    Total = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    SupplierReference = table.Column<string>(type: "TEXT", nullable: true),
                    TrackingNumber = table.Column<string>(type: "TEXT", nullable: true),
                    OrderedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ApprovedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ApprovedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                    ClaimValue = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserClaims_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderKey = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_UserLogins_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    RoleId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    LoginProvider = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_UserTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FeeScheduleItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FeeScheduleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProcedureCodeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Fee = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    AllowedAmount = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: true),
                    CoveragePercent = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeeScheduleItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeeScheduleItems_FeeSchedules_FeeScheduleId",
                        column: x => x.FeeScheduleId,
                        principalTable: "FeeSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FeeScheduleItems_ProcedureCodes_ProcedureCodeId",
                        column: x => x.ProcedureCodeId,
                        principalTable: "ProcedureCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InsurancePlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    InsuranceCarrierId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PlanName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    GroupNumber = table.Column<string>(type: "TEXT", nullable: true),
                    GroupName = table.Column<string>(type: "TEXT", nullable: true),
                    PlanType = table.Column<int>(type: "INTEGER", nullable: false),
                    AnnualMaximum = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: true),
                    LifetimeMaximum = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: true),
                    IndividualDeductible = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: true),
                    FamilyDeductible = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: true),
                    DeductibleAppliesToPreventive = table.Column<bool>(type: "INTEGER", nullable: false),
                    PreventiveCoveragePercent = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    DiagnosticCoveragePercent = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    BasicCoveragePercent = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    MajorCoveragePercent = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    EndodonticCoveragePercent = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    PeriodonticCoveragePercent = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    OrthodonticCoveragePercent = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    OrthodonticLifetimeMaximum = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: true),
                    ImplantCoveragePercent = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    SurgeryCoveragePercent = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    WaitingPeriodBasicMonths = table.Column<int>(type: "INTEGER", nullable: true),
                    WaitingPeriodMajorMonths = table.Column<int>(type: "INTEGER", nullable: true),
                    WaitingPeriodOrthoMonths = table.Column<int>(type: "INTEGER", nullable: true),
                    BenefitYearStartMonth = table.Column<string>(type: "TEXT", nullable: true),
                    RequiresPreAuthorisation = table.Column<bool>(type: "INTEGER", nullable: false),
                    PreAuthorisationThreshold = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: true),
                    FeeScheduleId = table.Column<Guid>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CoverageNotes = table.Column<string>(type: "TEXT", nullable: true),
                    Exclusions = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsurancePlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InsurancePlans_FeeSchedules_FeeScheduleId",
                        column: x => x.FeeScheduleId,
                        principalTable: "FeeSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InsurancePlans_InsuranceCarriers_InsuranceCarrierId",
                        column: x => x.InsuranceCarrierId,
                        principalTable: "InsuranceCarriers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BusinessHours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    LocationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DayOfWeek = table.Column<int>(type: "INTEGER", nullable: false),
                    IsClosed = table.Column<bool>(type: "INTEGER", nullable: false),
                    OpenTime = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    CloseTime = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    BreakStart = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    BreakEnd = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessHours", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessHours_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClinicClosures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    LocationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", nullable: false),
                    BlocksBooking = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicClosures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClinicClosures_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Operatories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    LocationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    ColourHex = table.Column<string>(type: "TEXT", nullable: true),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsSurgicalSuite = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasXRay = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasIntraoralScanner = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasSedationEquipment = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsWheelchairAccessible = table.Column<bool>(type: "INTEGER", nullable: false),
                    EquipmentNotes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Operatories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Operatories_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Staff",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    StaffNumber = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Name_Title = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Name_First = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false, defaultValue: ""),
                    Name_Middle = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Name_Last = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false, defaultValue: ""),
                    Name_Preferred = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Name_Suffix = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Contact_Mobile = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    Contact_Home = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    Contact_Work = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    Contact_Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Contact_Preferred = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false, defaultValue: "Any"),
                    Address_Line1 = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Address_Line2 = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Address_City = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Address_County = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Address_PostCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Address_Country = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false, defaultValue: "United Kingdom"),
                    DateOfBirth = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Gender = table.Column<int>(type: "INTEGER", nullable: false),
                    Role = table.Column<int>(type: "INTEGER", nullable: false),
                    EmploymentType = table.Column<int>(type: "INTEGER", nullable: false),
                    JobTitle = table.Column<string>(type: "TEXT", nullable: true),
                    Specialty = table.Column<string>(type: "TEXT", nullable: true),
                    Qualifications = table.Column<string>(type: "TEXT", nullable: true),
                    RegistrationNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    RegistrationExpiry = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    IndemnityProvider = table.Column<string>(type: "TEXT", nullable: true),
                    IndemnityPolicyNumber = table.Column<string>(type: "TEXT", nullable: true),
                    IndemnityExpiry = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    DbsCheckReference = table.Column<string>(type: "TEXT", nullable: true),
                    DbsCheckDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    HepatitisBImmunityDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    HireDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    TerminationDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsProvider = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanPrescribe = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanPerformSurgery = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanAdministerSedation = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanTakeRadiographs = table.Column<bool>(type: "INTEGER", nullable: false),
                    ColourHex = table.Column<string>(type: "TEXT", maxLength: 9, nullable: true),
                    DefaultLocationId = table.Column<Guid>(type: "TEXT", nullable: true),
                    DefaultAppointmentMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    HourlyRate = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: true),
                    CommissionPercent = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: true),
                    DailyProductionTarget = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: true),
                    ApplicationUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    Signature = table.Column<string>(type: "TEXT", nullable: true),
                    Biography = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    PhotoPath = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Staff", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Staff_Locations_DefaultLocationId",
                        column: x => x.DefaultLocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Sterilisers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Manufacturer = table.Column<string>(type: "TEXT", nullable: true),
                    Model = table.Column<string>(type: "TEXT", nullable: true),
                    SerialNumber = table.Column<string>(type: "TEXT", nullable: true),
                    LocationId = table.Column<Guid>(type: "TEXT", nullable: true),
                    InstallationDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    LastServiceDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    NextServiceDue = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    LastValidationDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    NextValidationDue = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    PressureVesselCertExpiry = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    NextCycleNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sterilisers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sterilisers_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "InventoryLots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    InventoryItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LotNumber = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    ExpiryDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    ReceivedDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    QuantityReceived = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    QuantityRemaining = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    UnitCost = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SupplierId = table.Column<Guid>(type: "TEXT", nullable: true),
                    IsQuarantined = table.Column<bool>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryLots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryLots_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryLots_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrderLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "TEXT", nullable: false),
                    InventoryItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Sequence = table.Column<int>(type: "INTEGER", nullable: false),
                    QuantityOrdered = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    QuantityReceived = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    UnitCost = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    DiscountPercent = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    TaxRatePercent = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrderLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderLines_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderLines_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Patients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PatientNumber = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Name_Title = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Name_First = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false, defaultValue: ""),
                    Name_Middle = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Name_Last = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false, defaultValue: ""),
                    Name_Preferred = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Name_Suffix = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Contact_Mobile = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    Contact_Home = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    Contact_Work = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    Contact_Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Contact_Preferred = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false, defaultValue: "Any"),
                    Address_Line1 = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Address_Line2 = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Address_City = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Address_County = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Address_PostCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Address_Country = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false, defaultValue: "United Kingdom"),
                    DateOfBirth = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    DateOfDeath = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Gender = table.Column<int>(type: "INTEGER", nullable: false),
                    MaritalStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    NationalInsuranceNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    NhsNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Occupation = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    Employer = table.Column<string>(type: "TEXT", maxLength: 160, nullable: true),
                    PreferredLanguage = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                    RequiresInterpreter = table.Column<bool>(type: "INTEGER", nullable: false),
                    Ethnicity = table.Column<string>(type: "TEXT", nullable: true),
                    PhotoPath = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    RegistrationDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    InactiveDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    InactiveReason = table.Column<string>(type: "TEXT", nullable: true),
                    ReferralSource = table.Column<string>(type: "TEXT", maxLength: 160, nullable: true),
                    ReferredByPatientId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PrimaryProviderId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PrimaryHygienistId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PreferredLocationId = table.Column<Guid>(type: "TEXT", nullable: true),
                    RecallIntervalMonths = table.Column<int>(type: "INTEGER", nullable: false),
                    LastExamDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    LastHygieneDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    LastRadiographDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    NextRecallDue = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    AllowEmail = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowSms = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowPhoneCall = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowPost = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowMarketing = table.Column<bool>(type: "INTEGER", nullable: false),
                    ConsentToContactAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PrivacyNoticeAcceptedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AccountBalance = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    InsurancePending = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    GuarantorPatientId = table.Column<Guid>(type: "TEXT", nullable: true),
                    GeneralPractitionerName = table.Column<string>(type: "TEXT", nullable: true),
                    GeneralPracticeSurgery = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Patients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Patients_Locations_PreferredLocationId",
                        column: x => x.PreferredLocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Patients_Patients_GuarantorPatientId",
                        column: x => x.GuarantorPatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Patients_Patients_ReferredByPatientId",
                        column: x => x.ReferredByPatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Patients_Staff_PrimaryHygienistId",
                        column: x => x.PrimaryHygienistId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Patients_Staff_PrimaryProviderId",
                        column: x => x.PrimaryProviderId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "StaffScheduleSlots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    StaffId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LocationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DefaultOperatoryId = table.Column<Guid>(type: "TEXT", nullable: true),
                    DayOfWeek = table.Column<int>(type: "INTEGER", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    BreakStart = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    BreakEnd = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    EffectiveFrom = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffScheduleSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaffScheduleSlots_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StaffScheduleSlots_Operatories_DefaultOperatoryId",
                        column: x => x.DefaultOperatoryId,
                        principalTable: "Operatories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_StaffScheduleSlots_Staff_StaffId",
                        column: x => x.StaffId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StaffTimeOff",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    StaffId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StartUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsAllDay = table.Column<bool>(type: "INTEGER", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "TEXT", nullable: true),
                    IsApproved = table.Column<bool>(type: "INTEGER", nullable: false),
                    ApprovedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ApprovedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffTimeOff", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaffTimeOff_Staff_StaffId",
                        column: x => x.StaffId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SterilisationCycles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SteriliserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CycleNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Program = table.Column<int>(type: "INTEGER", nullable: false),
                    PeakTemperatureCelsius = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: true),
                    PeakPressureBar = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: true),
                    HoldTimeMinutes = table.Column<int>(type: "INTEGER", nullable: true),
                    ChemicalIndicatorPass = table.Column<bool>(type: "INTEGER", nullable: false),
                    BiologicalIndicatorPass = table.Column<bool>(type: "INTEGER", nullable: true),
                    BiologicalIndicatorReadDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    HelixTestPass = table.Column<bool>(type: "INTEGER", nullable: false),
                    VacuumLeakTestPass = table.Column<bool>(type: "INTEGER", nullable: false),
                    PrinterRecordAttached = table.Column<bool>(type: "INTEGER", nullable: false),
                    Result = table.Column<int>(type: "INTEGER", nullable: false),
                    OperatorStaffId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ItemCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LoadContents = table.Column<string>(type: "TEXT", nullable: true),
                    FailureReason = table.Column<string>(type: "TEXT", nullable: true),
                    CorrectiveAction = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SterilisationCycles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SterilisationCycles_Staff_OperatorStaffId",
                        column: x => x.OperatorStaffId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SterilisationCycles_Sterilisers_SteriliserId",
                        column: x => x.SteriliserId,
                        principalTable: "Sterilisers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockMovements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    InventoryItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    InventoryLotId = table.Column<Guid>(type: "TEXT", nullable: true),
                    MovementDateUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    MovementType = table.Column<int>(type: "INTEGER", nullable: false),
                    Quantity = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    BalanceAfter = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    UnitCost = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: true),
                    ProcedureId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PurchaseOrderId = table.Column<Guid>(type: "TEXT", nullable: true),
                    LocationId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PerformedByStaffId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Reference = table.Column<string>(type: "TEXT", nullable: true),
                    Reason = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockMovements_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockMovements_InventoryLots_InventoryLotId",
                        column: x => x.InventoryLotId,
                        principalTable: "InventoryLots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AccountAdjustments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PatientId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AdjustmentDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    AdjustmentType = table.Column<int>(type: "INTEGER", nullable: false),
                    Amount = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    InvoiceId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 400, nullable: false),
                    ApprovedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ApprovedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountAdjustments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountAdjustments_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Appointments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AppointmentNumber = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    PatientId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProviderId = table.Column<Guid>(type: "TEXT", nullable: true),
                    AssistantId = table.Column<Guid>(type: "TEXT", nullable: true),
                    LocationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OperatoryId = table.Column<Guid>(type: "TEXT", nullable: true),
                    StartUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AppointmentType = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    PrivateNotes = table.Column<string>(type: "TEXT", nullable: true),
                    ColourHex = table.Column<string>(type: "TEXT", nullable: true),
                    IsRecallVisit = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsNewPatient = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsEmergency = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequiresInterpreter = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequiresSedation = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequiresPreMedication = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequiresLabWork = table.Column<bool>(type: "INTEGER", nullable: false),
                    LabWorkReceived = table.Column<bool>(type: "INTEGER", nullable: false),
                    ConfirmedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ConfirmedVia = table.Column<string>(type: "TEXT", nullable: true),
                    ArrivedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SeatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TreatmentStartedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CheckedOutAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CancellationReason = table.Column<string>(type: "TEXT", nullable: true),
                    CancelledBy = table.Column<string>(type: "TEXT", nullable: true),
                    RescheduledToAppointmentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    RescheduledFromAppointmentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    RecallScheduleId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TreatmentPlanId = table.Column<Guid>(type: "TEXT", nullable: true),
                    BookedBy = table.Column<string>(type: "TEXT", nullable: true),
                    BookedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Appointments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Appointments_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Appointments_Operatories_OperatoryId",
                        column: x => x.OperatoryId,
                        principalTable: "Operatories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Appointments_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Appointments_Staff_AssistantId",
                        column: x => x.AssistantId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Appointments_Staff_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CommunicationLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PatientId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Channel = table.Column<int>(type: "INTEGER", nullable: false),
                    Direction = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Subject = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Body = table.Column<string>(type: "TEXT", nullable: true),
                    Recipient = table.Column<string>(type: "TEXT", nullable: true),
                    Sender = table.Column<string>(type: "TEXT", nullable: true),
                    StaffId = table.Column<Guid>(type: "TEXT", nullable: true),
                    AppointmentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    InvoiceId = table.Column<Guid>(type: "TEXT", nullable: true),
                    RecallScheduleId = table.Column<Guid>(type: "TEXT", nullable: true),
                    MessageTemplateId = table.Column<Guid>(type: "TEXT", nullable: true),
                    RequiresFollowUp = table.Column<bool>(type: "INTEGER", nullable: false),
                    FollowUpDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Outcome = table.Column<string>(type: "TEXT", nullable: true),
                    FailureReason = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunicationLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommunicationLogs_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommunicationLogs_Staff_StaffId",
                        column: x => x.StaffId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    PatientId = table.Column<Guid>(type: "TEXT", nullable: false),
                    GuarantorPatientId = table.Column<Guid>(type: "TEXT", nullable: true),
                    LocationId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ProviderId = table.Column<Guid>(type: "TEXT", nullable: true),
                    IssueDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Subtotal = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    DiscountAmount = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    TaxAmount = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    Total = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    InsuranceEstimate = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    AmountPaid = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    WriteOffAmount = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    PaidInFullOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    VoidedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    VoidReason = table.Column<string>(type: "TEXT", nullable: true),
                    PurchaseOrderReference = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    TermsText = table.Column<string>(type: "TEXT", nullable: true),
                    RemindersSent = table.Column<int>(type: "INTEGER", nullable: false),
                    LastReminderOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Invoices_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Invoices_Patients_GuarantorPatientId",
                        column: x => x.GuarantorPatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Invoices_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Invoices_Staff_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "LabCases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CaseNumber = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    PatientId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DentalLaboratoryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProcedureId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TreatmentPlanItemId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ProviderId = table.Column<Guid>(type: "TEXT", nullable: true),
                    DeliveryAppointmentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CaseType = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ToothNumbers = table.Column<string>(type: "TEXT", nullable: true),
                    Arch = table.Column<int>(type: "INTEGER", nullable: true),
                    Shade = table.Column<string>(type: "TEXT", nullable: true),
                    ShadeGuide = table.Column<string>(type: "TEXT", nullable: true),
                    Material = table.Column<int>(type: "INTEGER", nullable: false),
                    MaterialDetail = table.Column<string>(type: "TEXT", nullable: true),
                    OcclusalScheme = table.Column<string>(type: "TEXT", nullable: true),
                    PonticDesign = table.Column<string>(type: "TEXT", nullable: true),
                    MarginDesign = table.Column<string>(type: "TEXT", nullable: true),
                    DigitalImpression = table.Column<bool>(type: "INTEGER", nullable: false),
                    PhysicalImpressionSent = table.Column<bool>(type: "INTEGER", nullable: false),
                    BiteRegistrationSent = table.Column<bool>(type: "INTEGER", nullable: false),
                    OppositingModelSent = table.Column<bool>(type: "INTEGER", nullable: false),
                    PhotographsSent = table.Column<bool>(type: "INTEGER", nullable: false),
                    SentDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    DueDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    ReceivedDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    TryInDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    DeliveryDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    LabFee = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: true),
                    PatientCharge = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: true),
                    LabInvoiceNumber = table.Column<string>(type: "TEXT", nullable: true),
                    TrackingNumber = table.Column<string>(type: "TEXT", nullable: true),
                    IsRemake = table.Column<bool>(type: "INTEGER", nullable: false),
                    RemakeOfCaseId = table.Column<Guid>(type: "TEXT", nullable: true),
                    RemakeReason = table.Column<string>(type: "TEXT", nullable: true),
                    QualityIssue = table.Column<bool>(type: "INTEGER", nullable: false),
                    Instructions = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabCases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LabCases_DentalLaboratories_DentalLaboratoryId",
                        column: x => x.DentalLaboratoryId,
                        principalTable: "DentalLaboratories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LabCases_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LabCases_Staff_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "LedgerEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PatientId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EntryDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    EntryType = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 400, nullable: false),
                    Debit = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    Credit = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    RunningBalance = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    InvoiceId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PaymentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ProcedureId = table.Column<Guid>(type: "TEXT", nullable: true),
                    AdjustmentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    InsuranceClaimId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ProviderId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Reference = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LedgerEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LedgerEntries_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MedicalHistoryReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PatientId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReviewDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    ReviewedByStaffId = table.Column<Guid>(type: "TEXT", nullable: true),
                    AppointmentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    NoChangesReported = table.Column<bool>(type: "INTEGER", nullable: false),
                    ChangesSummary = table.Column<string>(type: "TEXT", nullable: true),
                    IsPregnant = table.Column<bool>(type: "INTEGER", nullable: false),
                    WeeksPregnant = table.Column<int>(type: "INTEGER", nullable: true),
                    IsBreastfeeding = table.Column<bool>(type: "INTEGER", nullable: false),
                    TakingAnticoagulants = table.Column<bool>(type: "INTEGER", nullable: false),
                    TakingBisphosphonates = table.Column<bool>(type: "INTEGER", nullable: false),
                    TakingImmunosuppressants = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasPacemaker = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasProstheticJoint = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasProstheticHeartValve = table.Column<bool>(type: "INTEGER", nullable: false),
                    HistoryOfEndocarditis = table.Column<bool>(type: "INTEGER", nullable: false),
                    HistoryOfRadiotherapyToHeadOrNeck = table.Column<bool>(type: "INTEGER", nullable: false),
                    HistoryOfChemotherapy = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequiresAntibioticProphylaxis = table.Column<bool>(type: "INTEGER", nullable: false),
                    AsaClassification = table.Column<int>(type: "INTEGER", nullable: false),
                    PatientSignatureObtained = table.Column<bool>(type: "INTEGER", nullable: false),
                    SignatureData = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalHistoryReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicalHistoryReviews_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MedicalHistoryReviews_Staff_ReviewedByStaffId",
                        column: x => x.ReviewedByStaffId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PatientAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PatientId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    Severity = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Detail = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    EffectiveTo = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    RaisedBy = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientAlerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientAlerts_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PatientAllergies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PatientId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AllergenId = table.Column<Guid>(type: "TEXT", nullable: true),
                    FreeTextAllergen = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    AllergyType = table.Column<int>(type: "INTEGER", nullable: false),
                    Severity = table.Column<int>(type: "INTEGER", nullable: false),
                    Reaction = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    OnsetDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    VerifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientAllergies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientAllergies_Allergens_AllergenId",
                        column: x => x.AllergenId,
                        principalTable: "Allergens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PatientAllergies_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PatientConsents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PatientId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ConsentFormTemplateId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProcedureId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TreatmentPlanId = table.Column<Guid>(type: "TEXT", nullable: true),
                    AppointmentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    SignedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SignedByName = table.Column<string>(type: "TEXT", nullable: true),
                    SignatureData = table.Column<string>(type: "TEXT", nullable: true),
                    RelationshipIfNotPatient = table.Column<string>(type: "TEXT", nullable: true),
                    WitnessStaffId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ClinicianStaffId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CapturedBodySnapshot = table.Column<string>(type: "TEXT", nullable: true),
                    ExpiresOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    WithdrawnAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    WithdrawalReason = table.Column<string>(type: "TEXT", nullable: true),
                    DocumentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientConsents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientConsents_ConsentFormTemplates_ConsentFormTemplateId",
                        column: x => x.ConsentFormTemplateId,
                        principalTable: "ConsentFormTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PatientConsents_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PatientConsents_Staff_ClinicianStaffId",
                        column: x => x.ClinicianStaffId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PatientConsents_Staff_WitnessStaffId",
                        column: x => x.WitnessStaffId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PatientContacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PatientId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name_Title = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Name_First = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false, defaultValue: ""),
                    Name_Middle = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Name_Last = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false, defaultValue: ""),
                    Name_Preferred = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Name_Suffix = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Contact_Mobile = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    Contact_Home = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    Contact_Work = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    Contact_Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Contact_Preferred = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false, defaultValue: "Any"),
                    Address_Line1 = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Address_Line2 = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Address_City = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Address_County = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Address_PostCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Address_Country = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Relationship = table.Column<int>(type: "INTEGER", nullable: false),
                    Role = table.Column<int>(type: "INTEGER", nullable: false),
                    IsPrimary = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasLegalAuthority = table.Column<bool>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientContacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientContacts_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PatientDiagnoses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PatientId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CodeSystem = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    ToothId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Quadrant = table.Column<int>(type: "INTEGER", nullable: false),
                    DiagnosedOn = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    ResolvedOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    DiagnosedByStaffId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ClinicalNoteId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientDiagnoses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientDiagnoses_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PatientDiagnoses_Staff_DiagnosedByStaffId",
                        column: x => x.DiagnosedByStaffId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PatientDiagnoses_Teeth_ToothId",
                        column: x => x.ToothId,
                        principalTable: "Teeth",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PatientDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PatientId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DocumentType = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 260, nullable: false),
                    StoragePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    SizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    Sha256 = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Tags = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    UploadedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UploadedBy = table.Column<string>(type: "TEXT", nullable: true),
                    AppointmentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ProcedureId = table.Column<Guid>(type: "TEXT", nullable: true),
                    IsClinicallySignificant = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsPatientVisible = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientDocuments_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PatientInsurances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PatientId = table.Column<Guid>(type: "TEXT", nullable: false),
                    InsurancePlanId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    MemberId = table.Column<string>(type: "TEXT", nullable: true),
                    PolicyNumber = table.Column<string>(type: "TEXT", nullable: true),
                    SubscriberIsPatient = table.Column<bool>(type: "INTEGER", nullable: false),
                    Subscriber_Title = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Subscriber_First = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Subscriber_Middle = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Subscriber_Last = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Subscriber_Preferred = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Subscriber_Suffix = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    SubscriberDateOfBirth = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    SubscriberId = table.Column<string>(type: "TEXT", nullable: true),
                    RelationshipToSubscriber = table.Column<int>(type: "INTEGER", nullable: false),
                    SubscriberEmployer = table.Column<string>(type: "TEXT", nullable: true),
                    EffectiveFrom = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    TerminatedOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    BenefitsUsedThisYear = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    DeductibleMetThisYear = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    RemainingAnnualMaximum = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: true),
                    BenefitsVerifiedOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    VerifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientInsurances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientInsurances_InsurancePlans_InsurancePlanId",
                        column: x => x.InsurancePlanId,
                        principalTable: "InsurancePlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PatientInsurances_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PatientMedicalConditions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PatientId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MedicalConditionId = table.Column<Guid>(type: "TEXT", nullable: true),
                    FreeTextCondition = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    DiagnosedOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    ResolvedOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Severity = table.Column<int>(type: "INTEGER", nullable: false),
                    ManagedBy = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientMedicalConditions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientMedicalConditions_MedicalConditions_MedicalConditionId",
                        column: x => x.MedicalConditionId,
                        principalTable: "MedicalConditions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PatientMedicalConditions_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PatientMedications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PatientId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MedicationId = table.Column<Guid>(type: "TEXT", nullable: true),
                    FreeTextMedication = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Dosage = table.Column<string>(type: "TEXT", nullable: true),
                    Frequency = table.Column<string>(type: "TEXT", nullable: true),
                    Route = table.Column<int>(type: "INTEGER", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    IsCurrent = table.Column<bool>(type: "INTEGER", nullable: false),
                    PrescribedBy = table.Column<string>(type: "TEXT", nullable: true),
                    Indication = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientMedications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientMedications_Medications_MedicationId",
                        column: x => x.MedicationId,
                        principalTable: "Medications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PatientMedications_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PlanNumber = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    PatientId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TreatmentPlanId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TotalAmount = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    DownPayment = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    InstallmentAmount = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    Frequency = table.Column<int>(type: "INTEGER", nullable: false),
                    NumberOfInstallments = table.Column<int>(type: "INTEGER", nullable: false),
                    InterestRatePercent = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    AdministrationFee = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    AgreementSigned = table.Column<bool>(type: "INTEGER", nullable: false),
                    AgreementSignedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SignatureData = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentPlans_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PeriodontalCharts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PatientId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ExamDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    ExaminerStaffId = table.Column<Guid>(type: "TEXT", nullable: true),
                    AppointmentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Diagnosis = table.Column<int>(type: "INTEGER", nullable: false),
                    BleedingOnProbingPercent = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: true),
                    PlaqueScorePercent = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: true),
                    CalculusScorePercent = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: true),
                    BpeUpperRight = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true),
                    BpeUpperAnterior = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true),
                    BpeUpperLeft = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true),
                    BpeLowerRight = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true),
                    BpeLowerAnterior = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true),
                    BpeLowerLeft = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true),
                    IsFullMouth = table.Column<bool>(type: "INTEGER", nullable: false),
                    RiskFactors = table.Column<string>(type: "TEXT", nullable: true),
                    TreatmentRecommendation = table.Column<string>(type: "TEXT", nullable: true),
                    NextReviewDue = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PeriodontalCharts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PeriodontalCharts_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PeriodontalCharts_Staff_ExaminerStaffId",
                        column: x => x.ExaminerStaffId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Prescriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PatientId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PrescriptionNumber = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    PrescriberStaffId = table.Column<Guid>(type: "TEXT", nullable: true),
                    AppointmentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ProcedureId = table.Column<Guid>(type: "TEXT", nullable: true),
                    IssueDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    ValidUntil = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    PharmacyId = table.Column<Guid>(type: "TEXT", nullable: true),
                    AllergiesChecked = table.Column<bool>(type: "INTEGER", nullable: false),
                    InteractionsChecked = table.Column<bool>(type: "INTEGER", nullable: false),
                    InteractionWarnings = table.Column<string>(type: "TEXT", nullable: true),
                    Indication = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    IsSigned = table.Column<bool>(type: "INTEGER", nullable: false),
                    SignedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SignatureData = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prescriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Prescriptions_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Prescriptions_Pharmacies_PharmacyId",
                        column: x => x.PharmacyId,
                        principalTable: "Pharmacies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Prescriptions_Staff_PrescriberStaffId",
                        column: x => x.PrescriberStaffId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "RecallSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PatientId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RecallType = table.Column<int>(type: "INTEGER", nullable: false),
                    CustomName = table.Column<string>(type: "TEXT", nullable: true),
                    IntervalMonths = table.Column<int>(type: "INTEGER", nullable: false),
                    LastCompletedDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    DueDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    PreferredProviderId = table.Column<Guid>(type: "TEXT", nullable: true),
                    BookedAppointmentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ContactAttempts = table.Column<int>(type: "INTEGER", nullable: false),
                    LastContactedOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    SuspendedUntil = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecallSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecallSchedules_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecallSchedules_Staff_PreferredProviderId",
                        column: x => x.PreferredProviderId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Referrals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PatientId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReferralNumber = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Direction = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    InternalProviderId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ExternalProviderName = table.Column<string>(type: "TEXT", nullable: true),
                    ExternalPracticeName = table.Column<string>(type: "TEXT", nullable: true),
                    ExternalSpecialty = table.Column<string>(type: "TEXT", nullable: true),
                    ExtContact_Mobile = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    ExtContact_Home = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    ExtContact_Work = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    ExtContact_Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ExtContact_Preferred = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false, defaultValue: "Any"),
                    ExtAddress_Line1 = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ExtAddress_Line2 = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ExtAddress_City = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ExtAddress_County = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ExtAddress_PostCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    ExtAddress_Country = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ReferralDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    AppointmentDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    CompletedDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    ClinicalSummary = table.Column<string>(type: "TEXT", nullable: true),
                    RelevantHistory = table.Column<string>(type: "TEXT", nullable: true),
                    ToothNumbers = table.Column<string>(type: "TEXT", nullable: true),
                    RadiographsAttached = table.Column<bool>(type: "INTEGER", nullable: false),
                    Outcome = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Referrals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Referrals_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Referrals_Staff_InternalProviderId",
                        column: x => x.InternalProviderId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SocialHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PatientId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RecordedOn = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    RecordedByStaffId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SmokingStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    CigarettesPerDay = table.Column<int>(type: "INTEGER", nullable: true),
                    YearsSmoked = table.Column<int>(type: "INTEGER", nullable: true),
                    QuitSmokingDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Alcohol = table.Column<int>(type: "INTEGER", nullable: false),
                    AlcoholUnitsPerWeek = table.Column<int>(type: "INTEGER", nullable: true),
                    RecreationalDrugUse = table.Column<bool>(type: "INTEGER", nullable: false),
                    BetelNutOrTobaccoChewing = table.Column<bool>(type: "INTEGER", nullable: false),
                    BrushingPerDay = table.Column<int>(type: "INTEGER", nullable: true),
                    UsesFluorideToothpaste = table.Column<bool>(type: "INTEGER", nullable: false),
                    Flosses = table.Column<bool>(type: "INTEGER", nullable: false),
                    UsesInterdentalBrushes = table.Column<bool>(type: "INTEGER", nullable: false),
                    UsesMouthwash = table.Column<bool>(type: "INTEGER", nullable: false),
                    UsesElectricToothbrush = table.Column<bool>(type: "INTEGER", nullable: false),
                    SugarIntakeEpisodesPerDay = table.Column<int>(type: "INTEGER", nullable: true),
                    AcidicDrinkConsumption = table.Column<bool>(type: "INTEGER", nullable: false),
                    Bruxism = table.Column<bool>(type: "INTEGER", nullable: false),
                    NailBiting = table.Column<bool>(type: "INTEGER", nullable: false),
                    MouthBreathing = table.Column<bool>(type: "INTEGER", nullable: false),
                    OralHygiene = table.Column<int>(type: "INTEGER", nullable: false),
                    DentalAnxiety = table.Column<bool>(type: "INTEGER", nullable: false),
                    AnxietyScore = table.Column<int>(type: "INTEGER", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocialHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SocialHistories_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TreatmentPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PatientId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PlanNumber = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    VersionNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    SupersedesPlanId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ProviderId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedOn = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    PresentedOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    DecisionOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    ValidUntil = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    CompletedOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    FeeScheduleId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TotalFee = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    TotalDiscount = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    EstimatedInsurancePortion = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    EstimatedPatientPortion = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    ConsentObtained = table.Column<bool>(type: "INTEGER", nullable: false),
                    ConsentSignedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ConsentSignatureData = table.Column<string>(type: "TEXT", nullable: true),
                    PresentedBy = table.Column<string>(type: "TEXT", nullable: true),
                    DeclineReason = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    RisksDiscussed = table.Column<string>(type: "TEXT", nullable: true),
                    AlternativesDiscussed = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TreatmentPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TreatmentPlans_FeeSchedules_FeeScheduleId",
                        column: x => x.FeeScheduleId,
                        principalTable: "FeeSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TreatmentPlans_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TreatmentPlans_Staff_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "VitalSignRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PatientId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RecordedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RecordedByStaffId = table.Column<Guid>(type: "TEXT", nullable: true),
                    AppointmentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ProcedureId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SystolicBp = table.Column<int>(type: "INTEGER", nullable: true),
                    DiastolicBp = table.Column<int>(type: "INTEGER", nullable: true),
                    PulseBpm = table.Column<int>(type: "INTEGER", nullable: true),
                    TemperatureCelsius = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: true),
                    RespiratoryRate = table.Column<int>(type: "INTEGER", nullable: true),
                    OxygenSaturation = table.Column<int>(type: "INTEGER", nullable: true),
                    BloodGlucoseMmolL = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: true),
                    WeightKg = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: true),
                    HeightCm = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VitalSignRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VitalSignRecords_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VitalSignRecords_Staff_RecordedByStaffId",
                        column: x => x.RecordedByStaffId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "WaitlistEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PatientId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PreferredProviderId = table.Column<Guid>(type: "TEXT", nullable: true),
                    LocationId = table.Column<Guid>(type: "TEXT", nullable: true),
                    AppointmentType = table.Column<int>(type: "INTEGER", nullable: false),
                    ProcedureDescription = table.Column<string>(type: "TEXT", nullable: true),
                    EstimatedMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    AvailableFrom = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    AvailableUntil = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    MondayOk = table.Column<bool>(type: "INTEGER", nullable: false),
                    TuesdayOk = table.Column<bool>(type: "INTEGER", nullable: false),
                    WednesdayOk = table.Column<bool>(type: "INTEGER", nullable: false),
                    ThursdayOk = table.Column<bool>(type: "INTEGER", nullable: false),
                    FridayOk = table.Column<bool>(type: "INTEGER", nullable: false),
                    SaturdayOk = table.Column<bool>(type: "INTEGER", nullable: false),
                    SundayOk = table.Column<bool>(type: "INTEGER", nullable: false),
                    MorningOk = table.Column<bool>(type: "INTEGER", nullable: false),
                    AfternoonOk = table.Column<bool>(type: "INTEGER", nullable: false),
                    EveningOk = table.Column<bool>(type: "INTEGER", nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ContactAttempts = table.Column<int>(type: "INTEGER", nullable: false),
                    LastContactedUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    BookedAppointmentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WaitlistEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WaitlistEntries_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WaitlistEntries_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WaitlistEntries_Staff_PreferredProviderId",
                        column: x => x.PreferredProviderId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "WorkTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Category = table.Column<string>(type: "TEXT", nullable: false),
                    PatientId = table.Column<Guid>(type: "TEXT", nullable: true),
                    AssignedToStaffId = table.Column<Guid>(type: "TEXT", nullable: true),
                    RelatedAppointmentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    RelatedLabCaseId = table.Column<Guid>(type: "TEXT", nullable: true),
                    RelatedClaimId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    IsCompleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkTasks_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkTasks_Staff_AssignedToStaffId",
                        column: x => x.AssignedToStaffId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "InstrumentSets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SetCode = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Contents = table.Column<string>(type: "TEXT", nullable: true),
                    ItemCount = table.Column<int>(type: "INTEGER", nullable: false),
                    TrayType = table.Column<string>(type: "TEXT", nullable: true),
                    LocationId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    LastCycleId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SterilisedOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    SterilityExpiryDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    UsageCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LastUsedOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    IsSurgicalSet = table.Column<bool>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstrumentSets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InstrumentSets_SterilisationCycles_LastCycleId",
                        column: x => x.LastCycleId,
                        principalTable: "SterilisationCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AppointmentReminders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Channel = table.Column<int>(type: "INTEGER", nullable: false),
                    ScheduledForUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    HoursBeforeAppointment = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    SentAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeliveredAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RespondedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Recipient = table.Column<string>(type: "TEXT", nullable: true),
                    MessageBody = table.Column<string>(type: "TEXT", nullable: true),
                    Response = table.Column<string>(type: "TEXT", nullable: true),
                    FailureReason = table.Column<string>(type: "TEXT", nullable: true),
                    AttemptCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentReminders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppointmentReminders_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClinicalNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PatientId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ProcedureId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ProviderId = table.Column<Guid>(type: "TEXT", nullable: true),
                    NoteDateUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    NoteType = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ChiefComplaint = table.Column<string>(type: "TEXT", nullable: true),
                    Subjective = table.Column<string>(type: "TEXT", nullable: true),
                    Objective = table.Column<string>(type: "TEXT", nullable: true),
                    Assessment = table.Column<string>(type: "TEXT", nullable: true),
                    Plan = table.Column<string>(type: "TEXT", nullable: true),
                    Body = table.Column<string>(type: "TEXT", nullable: true),
                    ExaminationFindings = table.Column<string>(type: "TEXT", nullable: true),
                    SoftTissueExam = table.Column<string>(type: "TEXT", nullable: true),
                    OralCancerScreeningDone = table.Column<bool>(type: "INTEGER", nullable: false),
                    OcclusionNotes = table.Column<string>(type: "TEXT", nullable: true),
                    RadiographicFindings = table.Column<string>(type: "TEXT", nullable: true),
                    TreatmentProvided = table.Column<string>(type: "TEXT", nullable: true),
                    MedicationsGiven = table.Column<string>(type: "TEXT", nullable: true),
                    PatientInstructions = table.Column<string>(type: "TEXT", nullable: true),
                    NextVisitPlan = table.Column<string>(type: "TEXT", nullable: true),
                    IsSigned = table.Column<bool>(type: "INTEGER", nullable: false),
                    SignedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SignedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SignatureHash = table.Column<string>(type: "TEXT", nullable: true),
                    IsAmended = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicalNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClinicalNotes_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ClinicalNotes_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClinicalNotes_Staff_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProcedureId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ProcedureCodeId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ToothId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Sequence = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 400, nullable: false),
                    ServiceDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Quantity = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    UnitPrice = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    DiscountAmount = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    TaxRatePercent = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    InsurancePortion = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    PatientPortion = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    ProviderId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceLines_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InvoiceLines_ProcedureCodes_ProcedureCodeId",
                        column: x => x.ProcedureCodeId,
                        principalTable: "ProcedureCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InvoiceLines_Teeth_ToothId",
                        column: x => x.ToothId,
                        principalTable: "Teeth",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "RadiographRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PatientId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RadiographType = table.Column<int>(type: "INTEGER", nullable: false),
                    TakenAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TakenByStaffId = table.Column<Guid>(type: "TEXT", nullable: true),
                    AppointmentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ToothNumbers = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Region = table.Column<string>(type: "TEXT", nullable: true),
                    Arch = table.Column<int>(type: "INTEGER", nullable: true),
                    Quadrant = table.Column<int>(type: "INTEGER", nullable: false),
                    KiloVoltagePeak = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: true),
                    MilliAmperage = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: true),
                    ExposureSeconds = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: true),
                    DoseMicroSieverts = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: true),
                    EquipmentUsed = table.Column<string>(type: "TEXT", nullable: true),
                    LeadApronUsed = table.Column<bool>(type: "INTEGER", nullable: false),
                    JustificationReason = table.Column<string>(type: "TEXT", nullable: true),
                    IsRepeat = table.Column<bool>(type: "INTEGER", nullable: false),
                    RepeatReason = table.Column<string>(type: "TEXT", nullable: true),
                    Findings = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    Report = table.Column<string>(type: "TEXT", nullable: true),
                    IsReported = table.Column<bool>(type: "INTEGER", nullable: false),
                    ReportedByStaffId = table.Column<Guid>(type: "TEXT", nullable: true),
                    QualityRating = table.Column<string>(type: "TEXT", nullable: true),
                    DocumentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ImagePath = table.Column<string>(type: "TEXT", nullable: true),
                    ThumbnailPath = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RadiographRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RadiographRecords_PatientDocuments_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "PatientDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RadiographRecords_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RadiographRecords_Staff_TakenByStaffId",
                        column: x => x.TakenByStaffId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "InsuranceClaims",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClaimNumber = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    PatientId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PatientInsuranceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ProviderId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    IsPreAuthorisation = table.Column<bool>(type: "INTEGER", nullable: false),
                    ServiceDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    SubmittedOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    AcknowledgedOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    AdjudicatedOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    PaidOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    TotalCharged = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    TotalAllowed = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    TotalPaid = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    DeductibleApplied = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    PatientResponsibility = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    WriteOffAmount = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    PayerClaimReference = table.Column<string>(type: "TEXT", nullable: true),
                    SubmissionMethod = table.Column<string>(type: "TEXT", nullable: true),
                    DenialReason = table.Column<string>(type: "TEXT", nullable: true),
                    DenialCode = table.Column<string>(type: "TEXT", nullable: true),
                    AppealSubmitted = table.Column<bool>(type: "INTEGER", nullable: false),
                    AppealDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    AppealNotes = table.Column<string>(type: "TEXT", nullable: true),
                    AttachmentsIncluded = table.Column<bool>(type: "INTEGER", nullable: false),
                    NarrativeIncluded = table.Column<bool>(type: "INTEGER", nullable: false),
                    Narrative = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    ResubmissionCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsuranceClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InsuranceClaims_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InsuranceClaims_PatientInsurances_PatientInsuranceId",
                        column: x => x.PatientInsuranceId,
                        principalTable: "PatientInsurances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InsuranceClaims_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InsuranceClaims_Staff_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PaymentPlanInstallments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PaymentPlanId = table.Column<Guid>(type: "TEXT", nullable: false),
                    InstallmentNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    AmountDue = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    AmountPaid = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    PaidOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    PaymentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    FailedAttempts = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentPlanInstallments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentPlanInstallments_PaymentPlans_PaymentPlanId",
                        column: x => x.PaymentPlanId,
                        principalTable: "PaymentPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PeriodontalMeasurements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PeriodontalChartId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ToothId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Site = table.Column<int>(type: "INTEGER", nullable: false),
                    PocketDepthMm = table.Column<int>(type: "INTEGER", nullable: false),
                    GingivalMarginMm = table.Column<int>(type: "INTEGER", nullable: false),
                    BleedingOnProbing = table.Column<bool>(type: "INTEGER", nullable: false),
                    Suppuration = table.Column<bool>(type: "INTEGER", nullable: false),
                    PlaquePresent = table.Column<bool>(type: "INTEGER", nullable: false),
                    CalculusPresent = table.Column<bool>(type: "INTEGER", nullable: false),
                    Mobility = table.Column<int>(type: "INTEGER", nullable: false),
                    Furcation = table.Column<int>(type: "INTEGER", nullable: false),
                    KeratinisedTissueMm = table.Column<int>(type: "INTEGER", nullable: true),
                    IsImplantSite = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsMissing = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PeriodontalMeasurements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PeriodontalMeasurements_PeriodontalCharts_PeriodontalChartId",
                        column: x => x.PeriodontalChartId,
                        principalTable: "PeriodontalCharts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PeriodontalMeasurements_Teeth_ToothId",
                        column: x => x.ToothId,
                        principalTable: "Teeth",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PrescriptionItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PrescriptionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MedicationId = table.Column<Guid>(type: "TEXT", nullable: true),
                    FreeTextMedication = table.Column<string>(type: "TEXT", nullable: true),
                    Dosage = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Route = table.Column<int>(type: "INTEGER", nullable: false),
                    Frequency = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    DurationDays = table.Column<int>(type: "INTEGER", nullable: true),
                    Quantity = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    Unit = table.Column<string>(type: "TEXT", nullable: true),
                    Repeats = table.Column<int>(type: "INTEGER", nullable: false),
                    AsRequired = table.Column<bool>(type: "INTEGER", nullable: false),
                    Instructions = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Sequence = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrescriptionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrescriptionItems_Medications_MedicationId",
                        column: x => x.MedicationId,
                        principalTable: "Medications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrescriptionItems_Prescriptions_PrescriptionId",
                        column: x => x.PrescriptionId,
                        principalTable: "Prescriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TreatmentPlanPhases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TreatmentPlanId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PhaseNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    ClinicalObjective = table.Column<string>(type: "TEXT", nullable: true),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    EstimatedVisits = table.Column<int>(type: "INTEGER", nullable: false),
                    EstimatedWeeksToComplete = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TreatmentPlanPhases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TreatmentPlanPhases_TreatmentPlans_TreatmentPlanId",
                        column: x => x.TreatmentPlanId,
                        principalTable: "TreatmentPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InstrumentSetUsages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    InstrumentSetId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProcedureId = table.Column<Guid>(type: "TEXT", nullable: true),
                    AppointmentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PatientId = table.Column<Guid>(type: "TEXT", nullable: true),
                    UsedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UsedByStaffId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CycleIdAtTimeOfUse = table.Column<Guid>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstrumentSetUsages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InstrumentSetUsages_InstrumentSets_InstrumentSetId",
                        column: x => x.InstrumentSetId,
                        principalTable: "InstrumentSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClinicalNoteAddenda",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClinicalNoteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AddedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AddedBy = table.Column<string>(type: "TEXT", nullable: true),
                    AuthorStaffId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Body = table.Column<string>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicalNoteAddenda", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClinicalNoteAddenda_ClinicalNotes_ClinicalNoteId",
                        column: x => x.ClinicalNoteId,
                        principalTable: "ClinicalNotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PaymentNumber = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    PatientId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PaymentDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Amount = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    Method = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "TEXT", nullable: true),
                    CardLastFour = table.Column<string>(type: "TEXT", nullable: true),
                    CardType = table.Column<string>(type: "TEXT", nullable: true),
                    AuthorisationCode = table.Column<string>(type: "TEXT", nullable: true),
                    BankReference = table.Column<string>(type: "TEXT", nullable: true),
                    ChequeNumber = table.Column<string>(type: "TEXT", nullable: true),
                    InsuranceClaimId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PaymentPlanInstallmentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    LocationId = table.Column<Guid>(type: "TEXT", nullable: true),
                    RefundedAmount = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    RefundedOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    RefundReason = table.Column<string>(type: "TEXT", nullable: true),
                    ReceivedBy = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payments_InsuranceClaims_InsuranceClaimId",
                        column: x => x.InsuranceClaimId,
                        principalTable: "InsuranceClaims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Payments_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TreatmentPlanItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TreatmentPlanPhaseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProcedureCodeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ToothId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Surfaces = table.Column<int>(type: "INTEGER", nullable: false),
                    Quadrant = table.Column<int>(type: "INTEGER", nullable: false),
                    Arch = table.Column<int>(type: "INTEGER", nullable: true),
                    ProviderId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Sequence = table.Column<int>(type: "INTEGER", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    UnitFee = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    DiscountAmount = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    EstimatedInsurance = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    EstimatedPatient = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    ScheduledFor = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    CompletedOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    CompletedProcedureId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PreAuthorisationRequired = table.Column<bool>(type: "INTEGER", nullable: false),
                    PreAuthorisationObtained = table.Column<bool>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TreatmentPlanItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TreatmentPlanItems_ProcedureCodes_ProcedureCodeId",
                        column: x => x.ProcedureCodeId,
                        principalTable: "ProcedureCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TreatmentPlanItems_Staff_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TreatmentPlanItems_Teeth_ToothId",
                        column: x => x.ToothId,
                        principalTable: "Teeth",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TreatmentPlanItems_TreatmentPlanPhases_TreatmentPlanPhaseId",
                        column: x => x.TreatmentPlanPhaseId,
                        principalTable: "TreatmentPlanPhases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentAllocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PaymentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    InvoiceLineId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Amount = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    AllocatedOn = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentAllocations_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentAllocations_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AppointmentProcedures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProcedureCodeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TreatmentPlanItemId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ToothId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Surfaces = table.Column<int>(type: "INTEGER", nullable: false),
                    EstimatedMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    EstimatedFee = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    Sequence = table.Column<int>(type: "INTEGER", nullable: false),
                    IsCompleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CompletedProcedureId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentProcedures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppointmentProcedures_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppointmentProcedures_ProcedureCodes_ProcedureCodeId",
                        column: x => x.ProcedureCodeId,
                        principalTable: "ProcedureCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppointmentProcedures_Teeth_ToothId",
                        column: x => x.ToothId,
                        principalTable: "Teeth",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AppointmentProcedures_TreatmentPlanItems_TreatmentPlanItemId",
                        column: x => x.TreatmentPlanItemId,
                        principalTable: "TreatmentPlanItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Procedures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PatientId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProcedureCodeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TreatmentPlanItemId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ToothId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Surfaces = table.Column<int>(type: "INTEGER", nullable: false),
                    Quadrant = table.Column<int>(type: "INTEGER", nullable: false),
                    Arch = table.Column<int>(type: "INTEGER", nullable: true),
                    RootCanalCount = table.Column<int>(type: "INTEGER", nullable: true),
                    ProviderId = table.Column<Guid>(type: "TEXT", nullable: true),
                    AssistantId = table.Column<Guid>(type: "TEXT", nullable: true),
                    LocationId = table.Column<Guid>(type: "TEXT", nullable: true),
                    DateOfService = table.Column<DateTime>(type: "TEXT", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    Fee = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    DiscountAmount = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    TaxAmount = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    IsBillable = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsWarrantyRedo = table.Column<bool>(type: "INTEGER", nullable: false),
                    RedoOfProcedureId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Material = table.Column<int>(type: "INTEGER", nullable: false),
                    ShadeReference = table.Column<string>(type: "TEXT", nullable: true),
                    BatchNumber = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    Complications = table.Column<string>(type: "TEXT", nullable: true),
                    InvoiceLineId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Procedures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Procedures_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Procedures_InvoiceLines_InvoiceLineId",
                        column: x => x.InvoiceLineId,
                        principalTable: "InvoiceLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Procedures_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Procedures_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Procedures_ProcedureCodes_ProcedureCodeId",
                        column: x => x.ProcedureCodeId,
                        principalTable: "ProcedureCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Procedures_Staff_AssistantId",
                        column: x => x.AssistantId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Procedures_Staff_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Procedures_Teeth_ToothId",
                        column: x => x.ToothId,
                        principalTable: "Teeth",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Procedures_TreatmentPlanItems_TreatmentPlanItemId",
                        column: x => x.TreatmentPlanItemId,
                        principalTable: "TreatmentPlanItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AnaesthesiaRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProcedureId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AnaesthesiaType = table.Column<int>(type: "INTEGER", nullable: false),
                    AdministeredByStaffId = table.Column<Guid>(type: "TEXT", nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RecoveryCompleteAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ConsentObtained = table.Column<bool>(type: "INTEGER", nullable: false),
                    PreOperativeAssessmentDone = table.Column<bool>(type: "INTEGER", nullable: false),
                    FastingConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    EscortConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    AsaClassification = table.Column<int>(type: "INTEGER", nullable: false),
                    TopicalApplied = table.Column<bool>(type: "INTEGER", nullable: false),
                    TopicalAgent = table.Column<string>(type: "TEXT", nullable: true),
                    BaselineSystolicBp = table.Column<int>(type: "INTEGER", nullable: true),
                    BaselineDiastolicBp = table.Column<int>(type: "INTEGER", nullable: true),
                    BaselinePulse = table.Column<int>(type: "INTEGER", nullable: true),
                    BaselineOxygenSaturation = table.Column<int>(type: "INTEGER", nullable: true),
                    LowestOxygenSaturation = table.Column<int>(type: "INTEGER", nullable: true),
                    MonitoringPulseOximetry = table.Column<bool>(type: "INTEGER", nullable: false),
                    MonitoringBloodPressure = table.Column<bool>(type: "INTEGER", nullable: false),
                    MonitoringCapnography = table.Column<bool>(type: "INTEGER", nullable: false),
                    MonitoringEcg = table.Column<bool>(type: "INTEGER", nullable: false),
                    ReversalAgentGiven = table.Column<bool>(type: "INTEGER", nullable: false),
                    ReversalAgent = table.Column<string>(type: "TEXT", nullable: true),
                    DischargeCriteriaMet = table.Column<bool>(type: "INTEGER", nullable: false),
                    AdverseEvents = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnaesthesiaRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnaesthesiaRecords_Procedures_ProcedureId",
                        column: x => x.ProcedureId,
                        principalTable: "Procedures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AnaesthesiaRecords_Staff_AdministeredByStaffId",
                        column: x => x.AdministeredByStaffId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DentalImplants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PatientId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ToothId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PlacementProcedureId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Manufacturer = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    SystemName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    ReferenceNumber = table.Column<string>(type: "TEXT", nullable: true),
                    LotNumber = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                    SerialNumber = table.Column<string>(type: "TEXT", nullable: true),
                    DiameterMm = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    LengthMm = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    Platform = table.Column<string>(type: "TEXT", nullable: true),
                    SurfaceTreatment = table.Column<string>(type: "TEXT", nullable: true),
                    ConnectionType = table.Column<string>(type: "TEXT", nullable: true),
                    PlacementDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    SurgeonStaffId = table.Column<Guid>(type: "TEXT", nullable: true),
                    InsertionTorqueNcm = table.Column<int>(type: "INTEGER", nullable: true),
                    StabilityQuotientIsq = table.Column<int>(type: "INTEGER", nullable: true),
                    BoneQuality = table.Column<int>(type: "INTEGER", nullable: false),
                    GuidedSurgery = table.Column<bool>(type: "INTEGER", nullable: false),
                    ImmediatePlacement = table.Column<bool>(type: "INTEGER", nullable: false),
                    ImmediateLoading = table.Column<bool>(type: "INTEGER", nullable: false),
                    GraftUsed = table.Column<bool>(type: "INTEGER", nullable: false),
                    GraftDetail = table.Column<string>(type: "TEXT", nullable: true),
                    MembraneUsed = table.Column<bool>(type: "INTEGER", nullable: false),
                    HealingAbutmentDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    SecondStageDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    ImpressionDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    RestorationDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    AbutmentType = table.Column<string>(type: "TEXT", nullable: true),
                    RestorationType = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    FailureDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    FailureReason = table.Column<string>(type: "TEXT", nullable: true),
                    WarrantyYears = table.Column<int>(type: "INTEGER", nullable: true),
                    NextReviewDue = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DentalImplants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DentalImplants_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DentalImplants_Procedures_PlacementProcedureId",
                        column: x => x.PlacementProcedureId,
                        principalTable: "Procedures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DentalImplants_Staff_SurgeonStaffId",
                        column: x => x.SurgeonStaffId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DentalImplants_Teeth_ToothId",
                        column: x => x.ToothId,
                        principalTable: "Teeth",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InsuranceClaimLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    InsuranceClaimId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProcedureId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ProcedureCodeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ToothId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Sequence = table.Column<int>(type: "INTEGER", nullable: false),
                    SurfaceCode = table.Column<string>(type: "TEXT", nullable: true),
                    ServiceDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    ChargedAmount = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    AllowedAmount = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    PaidAmount = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    DeductibleAmount = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    CoInsuranceAmount = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    WriteOffAmount = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    AdjudicationCode = table.Column<string>(type: "TEXT", nullable: true),
                    DenialReason = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsuranceClaimLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InsuranceClaimLines_InsuranceClaims_InsuranceClaimId",
                        column: x => x.InsuranceClaimId,
                        principalTable: "InsuranceClaims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InsuranceClaimLines_ProcedureCodes_ProcedureCodeId",
                        column: x => x.ProcedureCodeId,
                        principalTable: "ProcedureCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InsuranceClaimLines_Procedures_ProcedureId",
                        column: x => x.ProcedureId,
                        principalTable: "Procedures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InsuranceClaimLines_Teeth_ToothId",
                        column: x => x.ToothId,
                        principalTable: "Teeth",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ProcedureMaterialUsages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProcedureId = table.Column<Guid>(type: "TEXT", nullable: false),
                    InventoryItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    InventoryLotId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Quantity = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    UnitCost = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcedureMaterialUsages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcedureMaterialUsages_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProcedureMaterialUsages_InventoryLots_InventoryLotId",
                        column: x => x.InventoryLotId,
                        principalTable: "InventoryLots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ProcedureMaterialUsages_Procedures_ProcedureId",
                        column: x => x.ProcedureId,
                        principalTable: "Procedures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SurgicalRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProcedureId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SurgeryType = table.Column<int>(type: "INTEGER", nullable: false),
                    AsaClassification = table.Column<int>(type: "INTEGER", nullable: false),
                    IncisionTimeUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ClosureTimeUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DurationMinutes = table.Column<int>(type: "INTEGER", nullable: true),
                    FlapRaised = table.Column<bool>(type: "INTEGER", nullable: false),
                    FlapDesign = table.Column<string>(type: "TEXT", nullable: true),
                    BoneRemoval = table.Column<bool>(type: "INTEGER", nullable: false),
                    ToothSectioned = table.Column<bool>(type: "INTEGER", nullable: false),
                    SutureRequired = table.Column<bool>(type: "INTEGER", nullable: false),
                    SutureMaterial = table.Column<string>(type: "TEXT", nullable: true),
                    SutureCount = table.Column<int>(type: "INTEGER", nullable: true),
                    SutureRemovalDue = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    GraftPlaced = table.Column<bool>(type: "INTEGER", nullable: false),
                    GraftMaterial = table.Column<string>(type: "TEXT", nullable: true),
                    GraftVolume = table.Column<string>(type: "TEXT", nullable: true),
                    MembranePlaced = table.Column<bool>(type: "INTEGER", nullable: false),
                    MembraneType = table.Column<string>(type: "TEXT", nullable: true),
                    SpecimenSentForHistology = table.Column<bool>(type: "INTEGER", nullable: false),
                    SpecimenReference = table.Column<string>(type: "TEXT", nullable: true),
                    HistologyResult = table.Column<string>(type: "TEXT", nullable: true),
                    EstimatedBloodLoss = table.Column<string>(type: "TEXT", nullable: true),
                    HaemostasisAchieved = table.Column<bool>(type: "INTEGER", nullable: false),
                    Irrigation = table.Column<string>(type: "TEXT", nullable: true),
                    Instrumentation = table.Column<string>(type: "TEXT", nullable: true),
                    InstrumentSetId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Outcome = table.Column<int>(type: "INTEGER", nullable: false),
                    Complications = table.Column<string>(type: "TEXT", nullable: true),
                    NerveProximityWarningGiven = table.Column<bool>(type: "INTEGER", nullable: false),
                    SinusExposure = table.Column<bool>(type: "INTEGER", nullable: false),
                    PostOperativeInstructionsGiven = table.Column<bool>(type: "INTEGER", nullable: false),
                    PostOperativeMedication = table.Column<string>(type: "TEXT", nullable: true),
                    FollowUpDue = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    OperativeNote = table.Column<string>(type: "TEXT", maxLength: 8000, nullable: true),
                    Findings = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurgicalRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SurgicalRecords_InstrumentSets_InstrumentSetId",
                        column: x => x.InstrumentSetId,
                        principalTable: "InstrumentSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SurgicalRecords_Procedures_ProcedureId",
                        column: x => x.ProcedureId,
                        principalTable: "Procedures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ToothConditionRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PatientId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ToothId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Surfaces = table.Column<int>(type: "INTEGER", nullable: false),
                    ConditionType = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Material = table.Column<int>(type: "INTEGER", nullable: false),
                    RecordedOn = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    RecordedByStaffId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ProcedureId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TreatmentPlanItemId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SupersedesId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Mobility = table.Column<int>(type: "INTEGER", nullable: false),
                    Furcation = table.Column<int>(type: "INTEGER", nullable: false),
                    SeverityScore = table.Column<int>(type: "INTEGER", nullable: true),
                    ShadeReference = table.Column<string>(type: "TEXT", nullable: true),
                    ColourHex = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToothConditionRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ToothConditionRecords_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ToothConditionRecords_Procedures_ProcedureId",
                        column: x => x.ProcedureId,
                        principalTable: "Procedures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ToothConditionRecords_Staff_RecordedByStaffId",
                        column: x => x.RecordedByStaffId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ToothConditionRecords_Teeth_ToothId",
                        column: x => x.ToothId,
                        principalTable: "Teeth",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ToothConditionRecords_ToothConditionRecords_SupersedesId",
                        column: x => x.SupersedesId,
                        principalTable: "ToothConditionRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AnaesthesiaAgentDoses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AnaesthesiaRecordId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AgentName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Concentration = table.Column<string>(type: "TEXT", nullable: true),
                    Vasoconstrictor = table.Column<string>(type: "TEXT", nullable: true),
                    Cartridges = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    MillilitresPerCartridge = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    MilligramsPerMillilitre = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    Technique = table.Column<int>(type: "INTEGER", nullable: false),
                    Site = table.Column<string>(type: "TEXT", nullable: true),
                    ToothId = table.Column<Guid>(type: "TEXT", nullable: true),
                    AdministeredAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AspirationNegative = table.Column<bool>(type: "INTEGER", nullable: false),
                    BatchNumber = table.Column<string>(type: "TEXT", nullable: true),
                    ExpiryDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnaesthesiaAgentDoses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnaesthesiaAgentDoses_AnaesthesiaRecords_AnaesthesiaRecordId",
                        column: x => x.AnaesthesiaRecordId,
                        principalTable: "AnaesthesiaRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountAdjustments_PatientId",
                table: "AccountAdjustments",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Allergens_Code",
                table: "Allergens",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AnaesthesiaAgentDoses_AnaesthesiaRecordId",
                table: "AnaesthesiaAgentDoses",
                column: "AnaesthesiaRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_AnaesthesiaRecords_AdministeredByStaffId",
                table: "AnaesthesiaRecords",
                column: "AdministeredByStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_AnaesthesiaRecords_ProcedureId",
                table: "AnaesthesiaRecords",
                column: "ProcedureId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentProcedures_AppointmentId",
                table: "AppointmentProcedures",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentProcedures_ProcedureCodeId",
                table: "AppointmentProcedures",
                column: "ProcedureCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentProcedures_ToothId",
                table: "AppointmentProcedures",
                column: "ToothId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentProcedures_TreatmentPlanItemId",
                table: "AppointmentProcedures",
                column: "TreatmentPlanItemId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentReminders_AppointmentId",
                table: "AppointmentReminders",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentReminders_Status_ScheduledForUtc",
                table: "AppointmentReminders",
                columns: new[] { "Status", "ScheduledForUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_AppointmentNumber",
                table: "Appointments",
                column: "AppointmentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_AssistantId",
                table: "Appointments",
                column: "AssistantId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_LocationId",
                table: "Appointments",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_OperatoryId_StartUtc",
                table: "Appointments",
                columns: new[] { "OperatoryId", "StartUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_PatientId_StartUtc",
                table: "Appointments",
                columns: new[] { "PatientId", "StartUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ProviderId_StartUtc",
                table: "Appointments",
                columns: new[] { "ProviderId", "StartUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_StartUtc",
                table: "Appointments",
                column: "StartUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_Status",
                table: "Appointments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AppSettings_Key",
                table: "AppSettings",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityName_EntityId",
                table: "AuditLogs",
                columns: new[] { "EntityName", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_PatientId",
                table: "AuditLogs",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_TimestampUtc",
                table: "AuditLogs",
                column: "TimestampUtc");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessHours_LocationId",
                table: "BusinessHours",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalNoteAddenda_ClinicalNoteId",
                table: "ClinicalNoteAddenda",
                column: "ClinicalNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalNotes_AppointmentId",
                table: "ClinicalNotes",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalNotes_PatientId_NoteDateUtc",
                table: "ClinicalNotes",
                columns: new[] { "PatientId", "NoteDateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalNotes_ProviderId",
                table: "ClinicalNotes",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicClosures_LocationId",
                table: "ClinicClosures",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationLogs_PatientId_OccurredAtUtc",
                table: "CommunicationLogs",
                columns: new[] { "PatientId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationLogs_StaffId",
                table: "CommunicationLogs",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsentFormTemplates_Code",
                table: "ConsentFormTemplates",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DentalImplants_LotNumber",
                table: "DentalImplants",
                column: "LotNumber");

            migrationBuilder.CreateIndex(
                name: "IX_DentalImplants_PatientId_Status",
                table: "DentalImplants",
                columns: new[] { "PatientId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_DentalImplants_PlacementProcedureId",
                table: "DentalImplants",
                column: "PlacementProcedureId");

            migrationBuilder.CreateIndex(
                name: "IX_DentalImplants_SurgeonStaffId",
                table: "DentalImplants",
                column: "SurgeonStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_DentalImplants_ToothId",
                table: "DentalImplants",
                column: "ToothId");

            migrationBuilder.CreateIndex(
                name: "IX_FeeScheduleItems_FeeScheduleId_ProcedureCodeId",
                table: "FeeScheduleItems",
                columns: new[] { "FeeScheduleId", "ProcedureCodeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FeeScheduleItems_ProcedureCodeId",
                table: "FeeScheduleItems",
                column: "ProcedureCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_FeeSchedules_InsuranceCarrierId",
                table: "FeeSchedules",
                column: "InsuranceCarrierId");

            migrationBuilder.CreateIndex(
                name: "IX_InstrumentSets_LastCycleId",
                table: "InstrumentSets",
                column: "LastCycleId");

            migrationBuilder.CreateIndex(
                name: "IX_InstrumentSets_SetCode",
                table: "InstrumentSets",
                column: "SetCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InstrumentSetUsages_InstrumentSetId",
                table: "InstrumentSetUsages",
                column: "InstrumentSetId");

            migrationBuilder.CreateIndex(
                name: "IX_InstrumentSetUsages_UsedAtUtc",
                table: "InstrumentSetUsages",
                column: "UsedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceClaimLines_InsuranceClaimId",
                table: "InsuranceClaimLines",
                column: "InsuranceClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceClaimLines_ProcedureCodeId",
                table: "InsuranceClaimLines",
                column: "ProcedureCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceClaimLines_ProcedureId",
                table: "InsuranceClaimLines",
                column: "ProcedureId");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceClaimLines_ToothId",
                table: "InsuranceClaimLines",
                column: "ToothId");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceClaims_ClaimNumber",
                table: "InsuranceClaims",
                column: "ClaimNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceClaims_InvoiceId",
                table: "InsuranceClaims",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceClaims_PatientId",
                table: "InsuranceClaims",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceClaims_PatientInsuranceId",
                table: "InsuranceClaims",
                column: "PatientInsuranceId");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceClaims_ProviderId",
                table: "InsuranceClaims",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceClaims_Status_SubmittedOn",
                table: "InsuranceClaims",
                columns: new[] { "Status", "SubmittedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_InsurancePlans_FeeScheduleId",
                table: "InsurancePlans",
                column: "FeeScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_InsurancePlans_InsuranceCarrierId",
                table: "InsurancePlans",
                column: "InsuranceCarrierId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_Category_IsActive",
                table: "InventoryItems",
                columns: new[] { "Category", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_PreferredSupplierId",
                table: "InventoryItems",
                column: "PreferredSupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_Sku",
                table: "InventoryItems",
                column: "Sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLots_ExpiryDate",
                table: "InventoryLots",
                column: "ExpiryDate");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLots_InventoryItemId_LotNumber",
                table: "InventoryLots",
                columns: new[] { "InventoryItemId", "LotNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLots_PurchaseOrderId",
                table: "InventoryLots",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLines_InvoiceId",
                table: "InvoiceLines",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLines_ProcedureCodeId",
                table: "InvoiceLines",
                column: "ProcedureCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLines_ToothId",
                table: "InvoiceLines",
                column: "ToothId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_GuarantorPatientId",
                table: "Invoices",
                column: "GuarantorPatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_InvoiceNumber",
                table: "Invoices",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_LocationId",
                table: "Invoices",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_PatientId_IssueDate",
                table: "Invoices",
                columns: new[] { "PatientId", "IssueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_ProviderId",
                table: "Invoices",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_Status_DueDate",
                table: "Invoices",
                columns: new[] { "Status", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_LabCases_CaseNumber",
                table: "LabCases",
                column: "CaseNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LabCases_DentalLaboratoryId",
                table: "LabCases",
                column: "DentalLaboratoryId");

            migrationBuilder.CreateIndex(
                name: "IX_LabCases_PatientId",
                table: "LabCases",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_LabCases_ProviderId",
                table: "LabCases",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_LabCases_Status_DueDate",
                table: "LabCases",
                columns: new[] { "Status", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_PatientId_EntryDate",
                table: "LedgerEntries",
                columns: new[] { "PatientId", "EntryDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Locations_Code",
                table: "Locations",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Locations_PracticeId",
                table: "Locations",
                column: "PracticeId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalConditions_Code",
                table: "MedicalConditions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MedicalHistoryReviews_PatientId_ReviewDate",
                table: "MedicalHistoryReviews",
                columns: new[] { "PatientId", "ReviewDate" });

            migrationBuilder.CreateIndex(
                name: "IX_MedicalHistoryReviews_ReviewedByStaffId",
                table: "MedicalHistoryReviews",
                column: "ReviewedByStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_Medications_Code",
                table: "Medications",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Medications_Name",
                table: "Medications",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_MessageTemplates_Code",
                table: "MessageTemplates",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NumberSequences_Name",
                table: "NumberSequences",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Operatories_LocationId_Code",
                table: "Operatories",
                columns: new[] { "LocationId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PatientAlerts_PatientId_IsActive",
                table: "PatientAlerts",
                columns: new[] { "PatientId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientAllergies_AllergenId",
                table: "PatientAllergies",
                column: "AllergenId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientAllergies_PatientId_IsActive",
                table: "PatientAllergies",
                columns: new[] { "PatientId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientConsents_ClinicianStaffId",
                table: "PatientConsents",
                column: "ClinicianStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientConsents_ConsentFormTemplateId",
                table: "PatientConsents",
                column: "ConsentFormTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientConsents_PatientId_Status",
                table: "PatientConsents",
                columns: new[] { "PatientId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientConsents_WitnessStaffId",
                table: "PatientConsents",
                column: "WitnessStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientContacts_PatientId",
                table: "PatientContacts",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientDiagnoses_DiagnosedByStaffId",
                table: "PatientDiagnoses",
                column: "DiagnosedByStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientDiagnoses_PatientId_Status",
                table: "PatientDiagnoses",
                columns: new[] { "PatientId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientDiagnoses_ToothId",
                table: "PatientDiagnoses",
                column: "ToothId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientDocuments_PatientId_DocumentType",
                table: "PatientDocuments",
                columns: new[] { "PatientId", "DocumentType" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientInsurances_InsurancePlanId",
                table: "PatientInsurances",
                column: "InsurancePlanId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientInsurances_PatientId_Priority",
                table: "PatientInsurances",
                columns: new[] { "PatientId", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientMedicalConditions_MedicalConditionId",
                table: "PatientMedicalConditions",
                column: "MedicalConditionId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientMedicalConditions_PatientId_Status",
                table: "PatientMedicalConditions",
                columns: new[] { "PatientId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientMedications_MedicationId",
                table: "PatientMedications",
                column: "MedicationId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientMedications_PatientId_IsCurrent",
                table: "PatientMedications",
                columns: new[] { "PatientId", "IsCurrent" });

            migrationBuilder.CreateIndex(
                name: "IX_Patients_DateOfBirth",
                table: "Patients",
                column: "DateOfBirth");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_GuarantorPatientId",
                table: "Patients",
                column: "GuarantorPatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_Name_Last",
                table: "Patients",
                column: "Name_Last");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_NextRecallDue",
                table: "Patients",
                column: "NextRecallDue");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_PatientNumber",
                table: "Patients",
                column: "PatientNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Patients_PreferredLocationId",
                table: "Patients",
                column: "PreferredLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_PrimaryHygienistId",
                table: "Patients",
                column: "PrimaryHygienistId");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_PrimaryProviderId",
                table: "Patients",
                column: "PrimaryProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_ReferredByPatientId",
                table: "Patients",
                column: "ReferredByPatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_Status",
                table: "Patients",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAllocations_InvoiceId",
                table: "PaymentAllocations",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAllocations_PaymentId_InvoiceId",
                table: "PaymentAllocations",
                columns: new[] { "PaymentId", "InvoiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentPlanInstallments_PaymentPlanId_InstallmentNumber",
                table: "PaymentPlanInstallments",
                columns: new[] { "PaymentPlanId", "InstallmentNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentPlanInstallments_Status_DueDate",
                table: "PaymentPlanInstallments",
                columns: new[] { "Status", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentPlans_PatientId",
                table: "PaymentPlans",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentPlans_PlanNumber",
                table: "PaymentPlans",
                column: "PlanNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_InsuranceClaimId",
                table: "Payments",
                column: "InsuranceClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PatientId_PaymentDate",
                table: "Payments",
                columns: new[] { "PatientId", "PaymentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PaymentNumber",
                table: "Payments",
                column: "PaymentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PeriodontalCharts_ExaminerStaffId",
                table: "PeriodontalCharts",
                column: "ExaminerStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_PeriodontalCharts_PatientId_ExamDate",
                table: "PeriodontalCharts",
                columns: new[] { "PatientId", "ExamDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PeriodontalMeasurements_PeriodontalChartId_ToothId_Site",
                table: "PeriodontalMeasurements",
                columns: new[] { "PeriodontalChartId", "ToothId", "Site" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PeriodontalMeasurements_ToothId",
                table: "PeriodontalMeasurements",
                column: "ToothId");

            migrationBuilder.CreateIndex(
                name: "IX_PostOperativeInstructions_Code",
                table: "PostOperativeInstructions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionItems_MedicationId",
                table: "PrescriptionItems",
                column: "MedicationId");

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionItems_PrescriptionId",
                table: "PrescriptionItems",
                column: "PrescriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_PatientId_IssueDate",
                table: "Prescriptions",
                columns: new[] { "PatientId", "IssueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_PharmacyId",
                table: "Prescriptions",
                column: "PharmacyId");

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_PrescriberStaffId",
                table: "Prescriptions",
                column: "PrescriberStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_PrescriptionNumber",
                table: "Prescriptions",
                column: "PrescriptionNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProcedureCodes_Category_IsActive",
                table: "ProcedureCodes",
                columns: new[] { "Category", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ProcedureCodes_Code",
                table: "ProcedureCodes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProcedureMaterialUsages_InventoryItemId",
                table: "ProcedureMaterialUsages",
                column: "InventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcedureMaterialUsages_InventoryLotId",
                table: "ProcedureMaterialUsages",
                column: "InventoryLotId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcedureMaterialUsages_ProcedureId",
                table: "ProcedureMaterialUsages",
                column: "ProcedureId");

            migrationBuilder.CreateIndex(
                name: "IX_Procedures_AppointmentId",
                table: "Procedures",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Procedures_AssistantId",
                table: "Procedures",
                column: "AssistantId");

            migrationBuilder.CreateIndex(
                name: "IX_Procedures_InvoiceLineId",
                table: "Procedures",
                column: "InvoiceLineId");

            migrationBuilder.CreateIndex(
                name: "IX_Procedures_LocationId",
                table: "Procedures",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Procedures_PatientId_DateOfService",
                table: "Procedures",
                columns: new[] { "PatientId", "DateOfService" });

            migrationBuilder.CreateIndex(
                name: "IX_Procedures_ProcedureCodeId",
                table: "Procedures",
                column: "ProcedureCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_Procedures_ProviderId",
                table: "Procedures",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Procedures_Status_DateOfService",
                table: "Procedures",
                columns: new[] { "Status", "DateOfService" });

            migrationBuilder.CreateIndex(
                name: "IX_Procedures_ToothId",
                table: "Procedures",
                column: "ToothId");

            migrationBuilder.CreateIndex(
                name: "IX_Procedures_TreatmentPlanItemId",
                table: "Procedures",
                column: "TreatmentPlanItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderLines_InventoryItemId",
                table: "PurchaseOrderLines",
                column: "InventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderLines_PurchaseOrderId",
                table: "PurchaseOrderLines",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_OrderNumber",
                table: "PurchaseOrders",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_SupplierId",
                table: "PurchaseOrders",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_RadiographRecords_DocumentId",
                table: "RadiographRecords",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_RadiographRecords_PatientId_TakenAtUtc",
                table: "RadiographRecords",
                columns: new[] { "PatientId", "TakenAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RadiographRecords_TakenByStaffId",
                table: "RadiographRecords",
                column: "TakenByStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_RecallSchedules_PatientId_RecallType",
                table: "RecallSchedules",
                columns: new[] { "PatientId", "RecallType" });

            migrationBuilder.CreateIndex(
                name: "IX_RecallSchedules_PreferredProviderId",
                table: "RecallSchedules",
                column: "PreferredProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_RecallSchedules_Status_DueDate",
                table: "RecallSchedules",
                columns: new[] { "Status", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_InternalProviderId",
                table: "Referrals",
                column: "InternalProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_PatientId",
                table: "Referrals",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_ReferralNumber",
                table: "Referrals",
                column: "ReferralNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoleClaims_RoleId",
                table: "RoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "Roles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SavedViews_Module_OwnerUserId",
                table: "SavedViews",
                columns: new[] { "Module", "OwnerUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_SocialHistories_PatientId_RecordedOn",
                table: "SocialHistories",
                columns: new[] { "PatientId", "RecordedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_Staff_ApplicationUserId",
                table: "Staff",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Staff_DefaultLocationId",
                table: "Staff",
                column: "DefaultLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Staff_IsActive_IsProvider",
                table: "Staff",
                columns: new[] { "IsActive", "IsProvider" });

            migrationBuilder.CreateIndex(
                name: "IX_Staff_StaffNumber",
                table: "Staff",
                column: "StaffNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StaffScheduleSlots_DefaultOperatoryId",
                table: "StaffScheduleSlots",
                column: "DefaultOperatoryId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffScheduleSlots_LocationId",
                table: "StaffScheduleSlots",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffScheduleSlots_StaffId_DayOfWeek",
                table: "StaffScheduleSlots",
                columns: new[] { "StaffId", "DayOfWeek" });

            migrationBuilder.CreateIndex(
                name: "IX_StaffTimeOff_StaffId_StartUtc",
                table: "StaffTimeOff",
                columns: new[] { "StaffId", "StartUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SterilisationCycles_OperatorStaffId",
                table: "SterilisationCycles",
                column: "OperatorStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_SterilisationCycles_StartedAtUtc",
                table: "SterilisationCycles",
                column: "StartedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SterilisationCycles_SteriliserId_CycleNumber",
                table: "SterilisationCycles",
                columns: new[] { "SteriliserId", "CycleNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sterilisers_LocationId",
                table: "Sterilisers",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_InventoryItemId_MovementDateUtc",
                table: "StockMovements",
                columns: new[] { "InventoryItemId", "MovementDateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_InventoryLotId",
                table: "StockMovements",
                column: "InventoryLotId");

            migrationBuilder.CreateIndex(
                name: "IX_SurgicalRecords_InstrumentSetId",
                table: "SurgicalRecords",
                column: "InstrumentSetId");

            migrationBuilder.CreateIndex(
                name: "IX_SurgicalRecords_ProcedureId",
                table: "SurgicalRecords",
                column: "ProcedureId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Teeth_ChartOrder",
                table: "Teeth",
                column: "ChartOrder");

            migrationBuilder.CreateIndex(
                name: "IX_Teeth_FdiNumber",
                table: "Teeth",
                column: "FdiNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ToothConditionRecords_PatientId_Status",
                table: "ToothConditionRecords",
                columns: new[] { "PatientId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ToothConditionRecords_PatientId_ToothId",
                table: "ToothConditionRecords",
                columns: new[] { "PatientId", "ToothId" });

            migrationBuilder.CreateIndex(
                name: "IX_ToothConditionRecords_ProcedureId",
                table: "ToothConditionRecords",
                column: "ProcedureId");

            migrationBuilder.CreateIndex(
                name: "IX_ToothConditionRecords_RecordedByStaffId",
                table: "ToothConditionRecords",
                column: "RecordedByStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_ToothConditionRecords_SupersedesId",
                table: "ToothConditionRecords",
                column: "SupersedesId");

            migrationBuilder.CreateIndex(
                name: "IX_ToothConditionRecords_ToothId",
                table: "ToothConditionRecords",
                column: "ToothId");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentPlanItems_ProcedureCodeId",
                table: "TreatmentPlanItems",
                column: "ProcedureCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentPlanItems_ProviderId",
                table: "TreatmentPlanItems",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentPlanItems_Status",
                table: "TreatmentPlanItems",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentPlanItems_ToothId",
                table: "TreatmentPlanItems",
                column: "ToothId");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentPlanItems_TreatmentPlanPhaseId",
                table: "TreatmentPlanItems",
                column: "TreatmentPlanPhaseId");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentPlanPhases_TreatmentPlanId",
                table: "TreatmentPlanPhases",
                column: "TreatmentPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentPlans_FeeScheduleId",
                table: "TreatmentPlans",
                column: "FeeScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentPlans_PatientId_Status",
                table: "TreatmentPlans",
                columns: new[] { "PatientId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentPlans_PlanNumber",
                table: "TreatmentPlans",
                column: "PlanNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentPlans_ProviderId",
                table: "TreatmentPlans",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_UserClaims_UserId",
                table: "UserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLogins_UserId",
                table: "UserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "Users",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "Users",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VitalSignRecords_PatientId_RecordedAtUtc",
                table: "VitalSignRecords",
                columns: new[] { "PatientId", "RecordedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_VitalSignRecords_RecordedByStaffId",
                table: "VitalSignRecords",
                column: "RecordedByStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_WaitlistEntries_LocationId",
                table: "WaitlistEntries",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_WaitlistEntries_PatientId",
                table: "WaitlistEntries",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_WaitlistEntries_PreferredProviderId",
                table: "WaitlistEntries",
                column: "PreferredProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_WaitlistEntries_Status_Priority",
                table: "WaitlistEntries",
                columns: new[] { "Status", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkTasks_AssignedToStaffId",
                table: "WorkTasks",
                column: "AssignedToStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkTasks_IsCompleted_DueDate",
                table: "WorkTasks",
                columns: new[] { "IsCompleted", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkTasks_PatientId",
                table: "WorkTasks",
                column: "PatientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountAdjustments");

            migrationBuilder.DropTable(
                name: "AnaesthesiaAgentDoses");

            migrationBuilder.DropTable(
                name: "AppointmentProcedures");

            migrationBuilder.DropTable(
                name: "AppointmentReminders");

            migrationBuilder.DropTable(
                name: "AppSettings");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "BusinessHours");

            migrationBuilder.DropTable(
                name: "ClinicalNoteAddenda");

            migrationBuilder.DropTable(
                name: "ClinicClosures");

            migrationBuilder.DropTable(
                name: "CommunicationLogs");

            migrationBuilder.DropTable(
                name: "DentalImplants");

            migrationBuilder.DropTable(
                name: "FeeScheduleItems");

            migrationBuilder.DropTable(
                name: "InstrumentSetUsages");

            migrationBuilder.DropTable(
                name: "InsuranceClaimLines");

            migrationBuilder.DropTable(
                name: "LabCases");

            migrationBuilder.DropTable(
                name: "LedgerEntries");

            migrationBuilder.DropTable(
                name: "MedicalHistoryReviews");

            migrationBuilder.DropTable(
                name: "MessageTemplates");

            migrationBuilder.DropTable(
                name: "NumberSequences");

            migrationBuilder.DropTable(
                name: "PatientAlerts");

            migrationBuilder.DropTable(
                name: "PatientAllergies");

            migrationBuilder.DropTable(
                name: "PatientConsents");

            migrationBuilder.DropTable(
                name: "PatientContacts");

            migrationBuilder.DropTable(
                name: "PatientDiagnoses");

            migrationBuilder.DropTable(
                name: "PatientMedicalConditions");

            migrationBuilder.DropTable(
                name: "PatientMedications");

            migrationBuilder.DropTable(
                name: "PaymentAllocations");

            migrationBuilder.DropTable(
                name: "PaymentPlanInstallments");

            migrationBuilder.DropTable(
                name: "PeriodontalMeasurements");

            migrationBuilder.DropTable(
                name: "PostOperativeInstructions");

            migrationBuilder.DropTable(
                name: "PrescriptionItems");

            migrationBuilder.DropTable(
                name: "ProcedureMaterialUsages");

            migrationBuilder.DropTable(
                name: "PurchaseOrderLines");

            migrationBuilder.DropTable(
                name: "RadiographRecords");

            migrationBuilder.DropTable(
                name: "RecallSchedules");

            migrationBuilder.DropTable(
                name: "Referrals");

            migrationBuilder.DropTable(
                name: "RoleClaims");

            migrationBuilder.DropTable(
                name: "SavedViews");

            migrationBuilder.DropTable(
                name: "SocialHistories");

            migrationBuilder.DropTable(
                name: "StaffScheduleSlots");

            migrationBuilder.DropTable(
                name: "StaffTimeOff");

            migrationBuilder.DropTable(
                name: "StockMovements");

            migrationBuilder.DropTable(
                name: "SurgicalRecords");

            migrationBuilder.DropTable(
                name: "ToothConditionRecords");

            migrationBuilder.DropTable(
                name: "UserClaims");

            migrationBuilder.DropTable(
                name: "UserLogins");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "UserTokens");

            migrationBuilder.DropTable(
                name: "VitalSignRecords");

            migrationBuilder.DropTable(
                name: "WaitlistEntries");

            migrationBuilder.DropTable(
                name: "WorkTasks");

            migrationBuilder.DropTable(
                name: "AnaesthesiaRecords");

            migrationBuilder.DropTable(
                name: "ClinicalNotes");

            migrationBuilder.DropTable(
                name: "DentalLaboratories");

            migrationBuilder.DropTable(
                name: "Allergens");

            migrationBuilder.DropTable(
                name: "ConsentFormTemplates");

            migrationBuilder.DropTable(
                name: "MedicalConditions");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "PaymentPlans");

            migrationBuilder.DropTable(
                name: "PeriodontalCharts");

            migrationBuilder.DropTable(
                name: "Medications");

            migrationBuilder.DropTable(
                name: "Prescriptions");

            migrationBuilder.DropTable(
                name: "PatientDocuments");

            migrationBuilder.DropTable(
                name: "InventoryLots");

            migrationBuilder.DropTable(
                name: "InstrumentSets");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Procedures");

            migrationBuilder.DropTable(
                name: "InsuranceClaims");

            migrationBuilder.DropTable(
                name: "Pharmacies");

            migrationBuilder.DropTable(
                name: "InventoryItems");

            migrationBuilder.DropTable(
                name: "PurchaseOrders");

            migrationBuilder.DropTable(
                name: "SterilisationCycles");

            migrationBuilder.DropTable(
                name: "Appointments");

            migrationBuilder.DropTable(
                name: "InvoiceLines");

            migrationBuilder.DropTable(
                name: "TreatmentPlanItems");

            migrationBuilder.DropTable(
                name: "PatientInsurances");

            migrationBuilder.DropTable(
                name: "Suppliers");

            migrationBuilder.DropTable(
                name: "Sterilisers");

            migrationBuilder.DropTable(
                name: "Operatories");

            migrationBuilder.DropTable(
                name: "Invoices");

            migrationBuilder.DropTable(
                name: "ProcedureCodes");

            migrationBuilder.DropTable(
                name: "Teeth");

            migrationBuilder.DropTable(
                name: "TreatmentPlanPhases");

            migrationBuilder.DropTable(
                name: "InsurancePlans");

            migrationBuilder.DropTable(
                name: "TreatmentPlans");

            migrationBuilder.DropTable(
                name: "FeeSchedules");

            migrationBuilder.DropTable(
                name: "Patients");

            migrationBuilder.DropTable(
                name: "InsuranceCarriers");

            migrationBuilder.DropTable(
                name: "Staff");

            migrationBuilder.DropTable(
                name: "Locations");

            migrationBuilder.DropTable(
                name: "Practices");
        }
    }
}
