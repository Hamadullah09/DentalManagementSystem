using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalSurgery.Migrations.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantIsolation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RoleClaims_Roles_RoleId",
                table: "RoleClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_UserClaims_Users_UserId",
                table: "UserClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_UserLogins_Users_UserId",
                table: "UserLogins");

            migrationBuilder.DropForeignKey(
                name: "FK_UserRoles_Roles_RoleId",
                table: "UserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_UserRoles_Users_UserId",
                table: "UserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_UserTokens_Users_UserId",
                table: "UserTokens");

            migrationBuilder.DropIndex(
                name: "UserNameIndex",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_TreatmentPlans_PlanNumber",
                table: "TreatmentPlans");

            migrationBuilder.DropIndex(
                name: "IX_SterilisationCycles_SteriliserId_CycleNumber",
                table: "SterilisationCycles");

            migrationBuilder.DropIndex(
                name: "IX_Staff_StaffNumber",
                table: "Staff");

            migrationBuilder.DropIndex(
                name: "RoleNameIndex",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_Referrals_ReferralNumber",
                table: "Referrals");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_OrderNumber",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_Prescriptions_PrescriptionNumber",
                table: "Prescriptions");

            migrationBuilder.DropIndex(
                name: "IX_PostOperativeInstructions_Code",
                table: "PostOperativeInstructions");

            migrationBuilder.DropIndex(
                name: "IX_PeriodontalMeasurements_PeriodontalChartId_ToothId_Site",
                table: "PeriodontalMeasurements");

            migrationBuilder.DropIndex(
                name: "IX_Payments_PaymentNumber",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_PaymentPlans_PlanNumber",
                table: "PaymentPlans");

            migrationBuilder.DropIndex(
                name: "IX_PaymentPlanInstallments_PaymentPlanId_InstallmentNumber",
                table: "PaymentPlanInstallments");

            migrationBuilder.DropIndex(
                name: "IX_Patients_PatientNumber",
                table: "Patients");

            migrationBuilder.DropIndex(
                name: "IX_Operatories_LocationId_Code",
                table: "Operatories");

            migrationBuilder.DropIndex(
                name: "IX_NumberSequences_Name",
                table: "NumberSequences");

            migrationBuilder.DropIndex(
                name: "IX_MessageTemplates_Code",
                table: "MessageTemplates");

            migrationBuilder.DropIndex(
                name: "IX_Locations_Code",
                table: "Locations");

            migrationBuilder.DropIndex(
                name: "IX_LabCases_CaseNumber",
                table: "LabCases");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_InvoiceNumber",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_InventoryItems_Sku",
                table: "InventoryItems");

            migrationBuilder.DropIndex(
                name: "IX_InsuranceClaims_ClaimNumber",
                table: "InsuranceClaims");

            migrationBuilder.DropIndex(
                name: "IX_InstrumentSets_SetCode",
                table: "InstrumentSets");

            migrationBuilder.DropIndex(
                name: "IX_FeeScheduleItems_FeeScheduleId_ProcedureCodeId",
                table: "FeeScheduleItems");

            migrationBuilder.DropIndex(
                name: "IX_ConsentFormTemplates_Code",
                table: "ConsentFormTemplates");

            migrationBuilder.DropIndex(
                name: "IX_AppSettings_Key",
                table: "AppSettings");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_AppointmentNumber",
                table: "Appointments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserTokens",
                table: "UserTokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserRoles",
                table: "UserRoles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserLogins",
                table: "UserLogins");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserClaims",
                table: "UserClaims");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RoleClaims",
                table: "RoleClaims");

            migrationBuilder.RenameTable(
                name: "UserTokens",
                newName: "AspNetUserTokens");

            migrationBuilder.RenameTable(
                name: "UserRoles",
                newName: "AspNetUserRoles");

            migrationBuilder.RenameTable(
                name: "UserLogins",
                newName: "AspNetUserLogins");

            migrationBuilder.RenameTable(
                name: "UserClaims",
                newName: "AspNetUserClaims");

            migrationBuilder.RenameTable(
                name: "RoleClaims",
                newName: "AspNetRoleClaims");

            migrationBuilder.RenameIndex(
                name: "IX_SurgicalRecords_ProcedureId",
                table: "SurgicalRecords",
                newName: "IX_SurgicalRecords_ProcedureId1");

            migrationBuilder.RenameIndex(
                name: "IX_AnaesthesiaRecords_ProcedureId",
                table: "AnaesthesiaRecords",
                newName: "IX_AnaesthesiaRecords_ProcedureId1");

            migrationBuilder.RenameIndex(
                name: "IX_UserRoles_RoleId",
                table: "AspNetUserRoles",
                newName: "IX_AspNetUserRoles_RoleId");

            migrationBuilder.RenameIndex(
                name: "IX_UserLogins_UserId",
                table: "AspNetUserLogins",
                newName: "IX_AspNetUserLogins_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_UserClaims_UserId",
                table: "AspNetUserClaims",
                newName: "IX_AspNetUserClaims_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_RoleClaims_RoleId",
                table: "AspNetRoleClaims",
                newName: "IX_AspNetRoleClaims_RoleId");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "WorkTasks",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "WaitlistEntries",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "VitalSignRecords",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "TreatmentPlans",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "TreatmentPlanPhases",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "TreatmentPlanItems",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "ToothConditionRecords",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "SurgicalRecords",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Suppliers",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "StockMovements",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Sterilisers",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "SterilisationCycles",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "StaffTimeOff",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "StaffScheduleSlots",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Staff",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "SocialHistories",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "SavedViews",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Roles",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Referrals",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "RecallSchedules",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "RadiographRecords",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "PurchaseOrders",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "PurchaseOrderLines",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Procedures",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "ProcedureMaterialUsages",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Prescriptions",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "PrescriptionItems",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Practices",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "PostOperativeInstructions",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Pharmacies",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "PeriodontalMeasurements",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "PeriodontalCharts",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Payments",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "PaymentPlans",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "PaymentPlanInstallments",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "PaymentAllocations",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Patients",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "PatientMedications",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "PatientMedicalConditions",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "PatientInsurances",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "PatientDocuments",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "PatientDiagnoses",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "PatientContacts",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "PatientConsents",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "PatientAllergies",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "PatientAlerts",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Operatories",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "NumberSequences",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "MessageTemplates",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "MedicalHistoryReviews",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Locations",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "LedgerEntries",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "LabCases",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Invoices",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "InvoiceLines",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "InventoryLots",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "InventoryItems",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "InsurancePlans",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "InsuranceClaims",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "InsuranceClaimLines",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "InsuranceCarriers",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "InstrumentSetUsages",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "InstrumentSets",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "FeeSchedules",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "FeeScheduleItems",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "DentalLaboratories",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "DentalImplants",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "ConsentFormTemplates",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "CommunicationLogs",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "ClinicClosures",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "ClinicalNotes",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "ClinicalNoteAddenda",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "BusinessHours",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "AuditLogs",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "AppSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Appointments",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "AppointmentReminders",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "AppointmentProcedures",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "AnaesthesiaRecords",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "AnaesthesiaAgentDoses",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "AccountAdjustments",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUserTokens",
                table: "AspNetUserTokens",
                columns: new[] { "UserId", "LoginProvider", "Name" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUserRoles",
                table: "AspNetUserRoles",
                columns: new[] { "UserId", "RoleId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUserLogins",
                table: "AspNetUserLogins",
                columns: new[] { "LoginProvider", "ProviderKey" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUserClaims",
                table: "AspNetUserClaims",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetRoleClaims",
                table: "AspNetRoleClaims",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Slug = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    SuspendedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SuspendedReason = table.Column<string>(type: "TEXT", nullable: true),
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
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkTasks_TenantId",
                table: "WorkTasks",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_WaitlistEntries_TenantId",
                table: "WaitlistEntries",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_VitalSignRecords_TenantId",
                table: "VitalSignRecords",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_TenantId",
                table: "Users",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "Users",
                columns: new[] { "TenantId", "NormalizedUserName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentPlans_PlanNumber",
                table: "TreatmentPlans",
                columns: new[] { "TenantId", "PlanNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentPlans_TenantId",
                table: "TreatmentPlans",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentPlanPhases_TenantId",
                table: "TreatmentPlanPhases",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentPlanItems_TenantId",
                table: "TreatmentPlanItems",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ToothConditionRecords_TenantId",
                table: "ToothConditionRecords",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SurgicalRecords_ProcedureId",
                table: "SurgicalRecords",
                columns: new[] { "TenantId", "ProcedureId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SurgicalRecords_TenantId",
                table: "SurgicalRecords",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_TenantId",
                table: "Suppliers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_TenantId",
                table: "StockMovements",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Sterilisers_TenantId",
                table: "Sterilisers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SterilisationCycles_SteriliserId",
                table: "SterilisationCycles",
                column: "SteriliserId");

            migrationBuilder.CreateIndex(
                name: "IX_SterilisationCycles_SteriliserId_CycleNumber",
                table: "SterilisationCycles",
                columns: new[] { "TenantId", "SteriliserId", "CycleNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SterilisationCycles_TenantId",
                table: "SterilisationCycles",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffTimeOff_TenantId",
                table: "StaffTimeOff",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffScheduleSlots_TenantId",
                table: "StaffScheduleSlots",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Staff_StaffNumber",
                table: "Staff",
                columns: new[] { "TenantId", "StaffNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Staff_TenantId",
                table: "Staff",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialHistories_TenantId",
                table: "SocialHistories",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedViews_TenantId",
                table: "SavedViews",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_TenantId",
                table: "Roles",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "Roles",
                columns: new[] { "TenantId", "NormalizedName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_ReferralNumber",
                table: "Referrals",
                columns: new[] { "TenantId", "ReferralNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_TenantId",
                table: "Referrals",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_RecallSchedules_TenantId",
                table: "RecallSchedules",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_RadiographRecords_TenantId",
                table: "RadiographRecords",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_OrderNumber",
                table: "PurchaseOrders",
                columns: new[] { "TenantId", "OrderNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_TenantId",
                table: "PurchaseOrders",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderLines_TenantId",
                table: "PurchaseOrderLines",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Procedures_TenantId",
                table: "Procedures",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcedureMaterialUsages_TenantId",
                table: "ProcedureMaterialUsages",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_PrescriptionNumber",
                table: "Prescriptions",
                columns: new[] { "TenantId", "PrescriptionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_TenantId",
                table: "Prescriptions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionItems_TenantId",
                table: "PrescriptionItems",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Practices_TenantId",
                table: "Practices",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PostOperativeInstructions_Code",
                table: "PostOperativeInstructions",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PostOperativeInstructions_TenantId",
                table: "PostOperativeInstructions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Pharmacies_TenantId",
                table: "Pharmacies",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PeriodontalMeasurements_PeriodontalChartId",
                table: "PeriodontalMeasurements",
                column: "PeriodontalChartId");

            migrationBuilder.CreateIndex(
                name: "IX_PeriodontalMeasurements_PeriodontalChartId_ToothId_Site",
                table: "PeriodontalMeasurements",
                columns: new[] { "TenantId", "PeriodontalChartId", "ToothId", "Site" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PeriodontalMeasurements_TenantId",
                table: "PeriodontalMeasurements",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PeriodontalCharts_TenantId",
                table: "PeriodontalCharts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PaymentNumber",
                table: "Payments",
                columns: new[] { "TenantId", "PaymentNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TenantId",
                table: "Payments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentPlans_PlanNumber",
                table: "PaymentPlans",
                columns: new[] { "TenantId", "PlanNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentPlans_TenantId",
                table: "PaymentPlans",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentPlanInstallments_PaymentPlanId",
                table: "PaymentPlanInstallments",
                column: "PaymentPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentPlanInstallments_PaymentPlanId_InstallmentNumber",
                table: "PaymentPlanInstallments",
                columns: new[] { "TenantId", "PaymentPlanId", "InstallmentNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentPlanInstallments_TenantId",
                table: "PaymentPlanInstallments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAllocations_TenantId",
                table: "PaymentAllocations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_PatientNumber",
                table: "Patients",
                columns: new[] { "TenantId", "PatientNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Patients_TenantId",
                table: "Patients",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientMedications_TenantId",
                table: "PatientMedications",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientMedicalConditions_TenantId",
                table: "PatientMedicalConditions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientInsurances_TenantId",
                table: "PatientInsurances",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientDocuments_TenantId",
                table: "PatientDocuments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientDiagnoses_TenantId",
                table: "PatientDiagnoses",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientContacts_TenantId",
                table: "PatientContacts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientConsents_TenantId",
                table: "PatientConsents",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientAllergies_TenantId",
                table: "PatientAllergies",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientAlerts_TenantId",
                table: "PatientAlerts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Operatories_LocationId",
                table: "Operatories",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Operatories_LocationId_Code",
                table: "Operatories",
                columns: new[] { "TenantId", "LocationId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Operatories_TenantId",
                table: "Operatories",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_NumberSequences_Name",
                table: "NumberSequences",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NumberSequences_TenantId",
                table: "NumberSequences",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageTemplates_Code",
                table: "MessageTemplates",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MessageTemplates_TenantId",
                table: "MessageTemplates",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalHistoryReviews_TenantId",
                table: "MedicalHistoryReviews",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_Code",
                table: "Locations",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Locations_TenantId",
                table: "Locations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_TenantId",
                table: "LedgerEntries",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_LabCases_CaseNumber",
                table: "LabCases",
                columns: new[] { "TenantId", "CaseNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LabCases_TenantId",
                table: "LabCases",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_InvoiceNumber",
                table: "Invoices",
                columns: new[] { "TenantId", "InvoiceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_TenantId",
                table: "Invoices",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLines_TenantId",
                table: "InvoiceLines",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLots_TenantId",
                table: "InventoryLots",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_Sku",
                table: "InventoryItems",
                columns: new[] { "TenantId", "Sku" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_TenantId",
                table: "InventoryItems",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_InsurancePlans_TenantId",
                table: "InsurancePlans",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceClaims_ClaimNumber",
                table: "InsuranceClaims",
                columns: new[] { "TenantId", "ClaimNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceClaims_TenantId",
                table: "InsuranceClaims",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceClaimLines_TenantId",
                table: "InsuranceClaimLines",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceCarriers_TenantId",
                table: "InsuranceCarriers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_InstrumentSetUsages_TenantId",
                table: "InstrumentSetUsages",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_InstrumentSets_SetCode",
                table: "InstrumentSets",
                columns: new[] { "TenantId", "SetCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InstrumentSets_TenantId",
                table: "InstrumentSets",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_FeeSchedules_TenantId",
                table: "FeeSchedules",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_FeeScheduleItems_FeeScheduleId",
                table: "FeeScheduleItems",
                column: "FeeScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_FeeScheduleItems_FeeScheduleId_ProcedureCodeId",
                table: "FeeScheduleItems",
                columns: new[] { "TenantId", "FeeScheduleId", "ProcedureCodeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FeeScheduleItems_TenantId",
                table: "FeeScheduleItems",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_DentalLaboratories_TenantId",
                table: "DentalLaboratories",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_DentalImplants_TenantId",
                table: "DentalImplants",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsentFormTemplates_Code",
                table: "ConsentFormTemplates",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConsentFormTemplates_TenantId",
                table: "ConsentFormTemplates",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationLogs_TenantId",
                table: "CommunicationLogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicClosures_TenantId",
                table: "ClinicClosures",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalNotes_TenantId",
                table: "ClinicalNotes",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalNoteAddenda_TenantId",
                table: "ClinicalNoteAddenda",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessHours_TenantId",
                table: "BusinessHours",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_TenantId",
                table: "AuditLogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AppSettings_Key",
                table: "AppSettings",
                columns: new[] { "TenantId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppSettings_TenantId",
                table: "AppSettings",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_AppointmentNumber",
                table: "Appointments",
                columns: new[] { "TenantId", "AppointmentNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_TenantId",
                table: "Appointments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentReminders_TenantId",
                table: "AppointmentReminders",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentProcedures_TenantId",
                table: "AppointmentProcedures",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AnaesthesiaRecords_ProcedureId",
                table: "AnaesthesiaRecords",
                columns: new[] { "TenantId", "ProcedureId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AnaesthesiaRecords_TenantId",
                table: "AnaesthesiaRecords",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AnaesthesiaAgentDoses_TenantId",
                table: "AnaesthesiaAgentDoses",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountAdjustments_TenantId",
                table: "AccountAdjustments",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetRoleClaims_Roles_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserClaims_Users_UserId",
                table: "AspNetUserClaims",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserLogins_Users_UserId",
                table: "AspNetUserLogins",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserRoles_Roles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserRoles_Users_UserId",
                table: "AspNetUserRoles",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserTokens_Users_UserId",
                table: "AspNetUserTokens",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetRoleClaims_Roles_RoleId",
                table: "AspNetRoleClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserClaims_Users_UserId",
                table: "AspNetUserClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserLogins_Users_UserId",
                table: "AspNetUserLogins");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserRoles_Roles_RoleId",
                table: "AspNetUserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserRoles_Users_UserId",
                table: "AspNetUserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserTokens_Users_UserId",
                table: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_WorkTasks_TenantId",
                table: "WorkTasks");

            migrationBuilder.DropIndex(
                name: "IX_WaitlistEntries_TenantId",
                table: "WaitlistEntries");

            migrationBuilder.DropIndex(
                name: "IX_VitalSignRecords_TenantId",
                table: "VitalSignRecords");

            migrationBuilder.DropIndex(
                name: "IX_Users_TenantId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "UserNameIndex",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_TreatmentPlans_PlanNumber",
                table: "TreatmentPlans");

            migrationBuilder.DropIndex(
                name: "IX_TreatmentPlans_TenantId",
                table: "TreatmentPlans");

            migrationBuilder.DropIndex(
                name: "IX_TreatmentPlanPhases_TenantId",
                table: "TreatmentPlanPhases");

            migrationBuilder.DropIndex(
                name: "IX_TreatmentPlanItems_TenantId",
                table: "TreatmentPlanItems");

            migrationBuilder.DropIndex(
                name: "IX_ToothConditionRecords_TenantId",
                table: "ToothConditionRecords");

            migrationBuilder.DropIndex(
                name: "IX_SurgicalRecords_ProcedureId",
                table: "SurgicalRecords");

            migrationBuilder.DropIndex(
                name: "IX_SurgicalRecords_TenantId",
                table: "SurgicalRecords");

            migrationBuilder.DropIndex(
                name: "IX_Suppliers_TenantId",
                table: "Suppliers");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_TenantId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_Sterilisers_TenantId",
                table: "Sterilisers");

            migrationBuilder.DropIndex(
                name: "IX_SterilisationCycles_SteriliserId",
                table: "SterilisationCycles");

            migrationBuilder.DropIndex(
                name: "IX_SterilisationCycles_SteriliserId_CycleNumber",
                table: "SterilisationCycles");

            migrationBuilder.DropIndex(
                name: "IX_SterilisationCycles_TenantId",
                table: "SterilisationCycles");

            migrationBuilder.DropIndex(
                name: "IX_StaffTimeOff_TenantId",
                table: "StaffTimeOff");

            migrationBuilder.DropIndex(
                name: "IX_StaffScheduleSlots_TenantId",
                table: "StaffScheduleSlots");

            migrationBuilder.DropIndex(
                name: "IX_Staff_StaffNumber",
                table: "Staff");

            migrationBuilder.DropIndex(
                name: "IX_Staff_TenantId",
                table: "Staff");

            migrationBuilder.DropIndex(
                name: "IX_SocialHistories_TenantId",
                table: "SocialHistories");

            migrationBuilder.DropIndex(
                name: "IX_SavedViews_TenantId",
                table: "SavedViews");

            migrationBuilder.DropIndex(
                name: "IX_Roles_TenantId",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "RoleNameIndex",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_Referrals_ReferralNumber",
                table: "Referrals");

            migrationBuilder.DropIndex(
                name: "IX_Referrals_TenantId",
                table: "Referrals");

            migrationBuilder.DropIndex(
                name: "IX_RecallSchedules_TenantId",
                table: "RecallSchedules");

            migrationBuilder.DropIndex(
                name: "IX_RadiographRecords_TenantId",
                table: "RadiographRecords");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_OrderNumber",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_TenantId",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrderLines_TenantId",
                table: "PurchaseOrderLines");

            migrationBuilder.DropIndex(
                name: "IX_Procedures_TenantId",
                table: "Procedures");

            migrationBuilder.DropIndex(
                name: "IX_ProcedureMaterialUsages_TenantId",
                table: "ProcedureMaterialUsages");

            migrationBuilder.DropIndex(
                name: "IX_Prescriptions_PrescriptionNumber",
                table: "Prescriptions");

            migrationBuilder.DropIndex(
                name: "IX_Prescriptions_TenantId",
                table: "Prescriptions");

            migrationBuilder.DropIndex(
                name: "IX_PrescriptionItems_TenantId",
                table: "PrescriptionItems");

            migrationBuilder.DropIndex(
                name: "IX_Practices_TenantId",
                table: "Practices");

            migrationBuilder.DropIndex(
                name: "IX_PostOperativeInstructions_Code",
                table: "PostOperativeInstructions");

            migrationBuilder.DropIndex(
                name: "IX_PostOperativeInstructions_TenantId",
                table: "PostOperativeInstructions");

            migrationBuilder.DropIndex(
                name: "IX_Pharmacies_TenantId",
                table: "Pharmacies");

            migrationBuilder.DropIndex(
                name: "IX_PeriodontalMeasurements_PeriodontalChartId",
                table: "PeriodontalMeasurements");

            migrationBuilder.DropIndex(
                name: "IX_PeriodontalMeasurements_PeriodontalChartId_ToothId_Site",
                table: "PeriodontalMeasurements");

            migrationBuilder.DropIndex(
                name: "IX_PeriodontalMeasurements_TenantId",
                table: "PeriodontalMeasurements");

            migrationBuilder.DropIndex(
                name: "IX_PeriodontalCharts_TenantId",
                table: "PeriodontalCharts");

            migrationBuilder.DropIndex(
                name: "IX_Payments_PaymentNumber",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_TenantId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_PaymentPlans_PlanNumber",
                table: "PaymentPlans");

            migrationBuilder.DropIndex(
                name: "IX_PaymentPlans_TenantId",
                table: "PaymentPlans");

            migrationBuilder.DropIndex(
                name: "IX_PaymentPlanInstallments_PaymentPlanId",
                table: "PaymentPlanInstallments");

            migrationBuilder.DropIndex(
                name: "IX_PaymentPlanInstallments_PaymentPlanId_InstallmentNumber",
                table: "PaymentPlanInstallments");

            migrationBuilder.DropIndex(
                name: "IX_PaymentPlanInstallments_TenantId",
                table: "PaymentPlanInstallments");

            migrationBuilder.DropIndex(
                name: "IX_PaymentAllocations_TenantId",
                table: "PaymentAllocations");

            migrationBuilder.DropIndex(
                name: "IX_Patients_PatientNumber",
                table: "Patients");

            migrationBuilder.DropIndex(
                name: "IX_Patients_TenantId",
                table: "Patients");

            migrationBuilder.DropIndex(
                name: "IX_PatientMedications_TenantId",
                table: "PatientMedications");

            migrationBuilder.DropIndex(
                name: "IX_PatientMedicalConditions_TenantId",
                table: "PatientMedicalConditions");

            migrationBuilder.DropIndex(
                name: "IX_PatientInsurances_TenantId",
                table: "PatientInsurances");

            migrationBuilder.DropIndex(
                name: "IX_PatientDocuments_TenantId",
                table: "PatientDocuments");

            migrationBuilder.DropIndex(
                name: "IX_PatientDiagnoses_TenantId",
                table: "PatientDiagnoses");

            migrationBuilder.DropIndex(
                name: "IX_PatientContacts_TenantId",
                table: "PatientContacts");

            migrationBuilder.DropIndex(
                name: "IX_PatientConsents_TenantId",
                table: "PatientConsents");

            migrationBuilder.DropIndex(
                name: "IX_PatientAllergies_TenantId",
                table: "PatientAllergies");

            migrationBuilder.DropIndex(
                name: "IX_PatientAlerts_TenantId",
                table: "PatientAlerts");

            migrationBuilder.DropIndex(
                name: "IX_Operatories_LocationId",
                table: "Operatories");

            migrationBuilder.DropIndex(
                name: "IX_Operatories_LocationId_Code",
                table: "Operatories");

            migrationBuilder.DropIndex(
                name: "IX_Operatories_TenantId",
                table: "Operatories");

            migrationBuilder.DropIndex(
                name: "IX_NumberSequences_Name",
                table: "NumberSequences");

            migrationBuilder.DropIndex(
                name: "IX_NumberSequences_TenantId",
                table: "NumberSequences");

            migrationBuilder.DropIndex(
                name: "IX_MessageTemplates_Code",
                table: "MessageTemplates");

            migrationBuilder.DropIndex(
                name: "IX_MessageTemplates_TenantId",
                table: "MessageTemplates");

            migrationBuilder.DropIndex(
                name: "IX_MedicalHistoryReviews_TenantId",
                table: "MedicalHistoryReviews");

            migrationBuilder.DropIndex(
                name: "IX_Locations_Code",
                table: "Locations");

            migrationBuilder.DropIndex(
                name: "IX_Locations_TenantId",
                table: "Locations");

            migrationBuilder.DropIndex(
                name: "IX_LedgerEntries_TenantId",
                table: "LedgerEntries");

            migrationBuilder.DropIndex(
                name: "IX_LabCases_CaseNumber",
                table: "LabCases");

            migrationBuilder.DropIndex(
                name: "IX_LabCases_TenantId",
                table: "LabCases");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_InvoiceNumber",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_TenantId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_InvoiceLines_TenantId",
                table: "InvoiceLines");

            migrationBuilder.DropIndex(
                name: "IX_InventoryLots_TenantId",
                table: "InventoryLots");

            migrationBuilder.DropIndex(
                name: "IX_InventoryItems_Sku",
                table: "InventoryItems");

            migrationBuilder.DropIndex(
                name: "IX_InventoryItems_TenantId",
                table: "InventoryItems");

            migrationBuilder.DropIndex(
                name: "IX_InsurancePlans_TenantId",
                table: "InsurancePlans");

            migrationBuilder.DropIndex(
                name: "IX_InsuranceClaims_ClaimNumber",
                table: "InsuranceClaims");

            migrationBuilder.DropIndex(
                name: "IX_InsuranceClaims_TenantId",
                table: "InsuranceClaims");

            migrationBuilder.DropIndex(
                name: "IX_InsuranceClaimLines_TenantId",
                table: "InsuranceClaimLines");

            migrationBuilder.DropIndex(
                name: "IX_InsuranceCarriers_TenantId",
                table: "InsuranceCarriers");

            migrationBuilder.DropIndex(
                name: "IX_InstrumentSetUsages_TenantId",
                table: "InstrumentSetUsages");

            migrationBuilder.DropIndex(
                name: "IX_InstrumentSets_SetCode",
                table: "InstrumentSets");

            migrationBuilder.DropIndex(
                name: "IX_InstrumentSets_TenantId",
                table: "InstrumentSets");

            migrationBuilder.DropIndex(
                name: "IX_FeeSchedules_TenantId",
                table: "FeeSchedules");

            migrationBuilder.DropIndex(
                name: "IX_FeeScheduleItems_FeeScheduleId",
                table: "FeeScheduleItems");

            migrationBuilder.DropIndex(
                name: "IX_FeeScheduleItems_FeeScheduleId_ProcedureCodeId",
                table: "FeeScheduleItems");

            migrationBuilder.DropIndex(
                name: "IX_FeeScheduleItems_TenantId",
                table: "FeeScheduleItems");

            migrationBuilder.DropIndex(
                name: "IX_DentalLaboratories_TenantId",
                table: "DentalLaboratories");

            migrationBuilder.DropIndex(
                name: "IX_DentalImplants_TenantId",
                table: "DentalImplants");

            migrationBuilder.DropIndex(
                name: "IX_ConsentFormTemplates_Code",
                table: "ConsentFormTemplates");

            migrationBuilder.DropIndex(
                name: "IX_ConsentFormTemplates_TenantId",
                table: "ConsentFormTemplates");

            migrationBuilder.DropIndex(
                name: "IX_CommunicationLogs_TenantId",
                table: "CommunicationLogs");

            migrationBuilder.DropIndex(
                name: "IX_ClinicClosures_TenantId",
                table: "ClinicClosures");

            migrationBuilder.DropIndex(
                name: "IX_ClinicalNotes_TenantId",
                table: "ClinicalNotes");

            migrationBuilder.DropIndex(
                name: "IX_ClinicalNoteAddenda_TenantId",
                table: "ClinicalNoteAddenda");

            migrationBuilder.DropIndex(
                name: "IX_BusinessHours_TenantId",
                table: "BusinessHours");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_TenantId",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AppSettings_Key",
                table: "AppSettings");

            migrationBuilder.DropIndex(
                name: "IX_AppSettings_TenantId",
                table: "AppSettings");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_AppointmentNumber",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_TenantId",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_AppointmentReminders_TenantId",
                table: "AppointmentReminders");

            migrationBuilder.DropIndex(
                name: "IX_AppointmentProcedures_TenantId",
                table: "AppointmentProcedures");

            migrationBuilder.DropIndex(
                name: "IX_AnaesthesiaRecords_ProcedureId",
                table: "AnaesthesiaRecords");

            migrationBuilder.DropIndex(
                name: "IX_AnaesthesiaRecords_TenantId",
                table: "AnaesthesiaRecords");

            migrationBuilder.DropIndex(
                name: "IX_AnaesthesiaAgentDoses_TenantId",
                table: "AnaesthesiaAgentDoses");

            migrationBuilder.DropIndex(
                name: "IX_AccountAdjustments_TenantId",
                table: "AccountAdjustments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUserTokens",
                table: "AspNetUserTokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUserRoles",
                table: "AspNetUserRoles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUserLogins",
                table: "AspNetUserLogins");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUserClaims",
                table: "AspNetUserClaims");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetRoleClaims",
                table: "AspNetRoleClaims");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "WorkTasks");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "WaitlistEntries");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "VitalSignRecords");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "TreatmentPlans");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "TreatmentPlanPhases");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "TreatmentPlanItems");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ToothConditionRecords");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "SurgicalRecords");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Sterilisers");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "SterilisationCycles");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "StaffTimeOff");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "StaffScheduleSlots");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Staff");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "SocialHistories");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "SavedViews");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Referrals");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "RecallSchedules");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "RadiographRecords");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "PurchaseOrderLines");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Procedures");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ProcedureMaterialUsages");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "PrescriptionItems");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Practices");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "PostOperativeInstructions");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Pharmacies");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "PeriodontalMeasurements");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "PeriodontalCharts");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "PaymentPlans");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "PaymentPlanInstallments");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "PaymentAllocations");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "PatientMedications");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "PatientMedicalConditions");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "PatientInsurances");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "PatientDocuments");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "PatientDiagnoses");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "PatientContacts");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "PatientConsents");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "PatientAllergies");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "PatientAlerts");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Operatories");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "NumberSequences");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "MessageTemplates");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "MedicalHistoryReviews");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "LedgerEntries");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "LabCases");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "InvoiceLines");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "InventoryLots");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "InsurancePlans");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "InsuranceClaims");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "InsuranceClaimLines");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "InsuranceCarriers");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "InstrumentSetUsages");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "InstrumentSets");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "FeeSchedules");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "FeeScheduleItems");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "DentalLaboratories");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "DentalImplants");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ConsentFormTemplates");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "CommunicationLogs");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ClinicClosures");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ClinicalNotes");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ClinicalNoteAddenda");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "BusinessHours");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AppSettings");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AppointmentReminders");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AppointmentProcedures");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AnaesthesiaRecords");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AnaesthesiaAgentDoses");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AccountAdjustments");

            migrationBuilder.RenameTable(
                name: "AspNetUserTokens",
                newName: "UserTokens");

            migrationBuilder.RenameTable(
                name: "AspNetUserRoles",
                newName: "UserRoles");

            migrationBuilder.RenameTable(
                name: "AspNetUserLogins",
                newName: "UserLogins");

            migrationBuilder.RenameTable(
                name: "AspNetUserClaims",
                newName: "UserClaims");

            migrationBuilder.RenameTable(
                name: "AspNetRoleClaims",
                newName: "RoleClaims");

            migrationBuilder.RenameIndex(
                name: "IX_SurgicalRecords_ProcedureId1",
                table: "SurgicalRecords",
                newName: "IX_SurgicalRecords_ProcedureId");

            migrationBuilder.RenameIndex(
                name: "IX_AnaesthesiaRecords_ProcedureId1",
                table: "AnaesthesiaRecords",
                newName: "IX_AnaesthesiaRecords_ProcedureId");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "UserRoles",
                newName: "IX_UserRoles_RoleId");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "UserLogins",
                newName: "IX_UserLogins_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "UserClaims",
                newName: "IX_UserClaims_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "RoleClaims",
                newName: "IX_RoleClaims_RoleId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserTokens",
                table: "UserTokens",
                columns: new[] { "UserId", "LoginProvider", "Name" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserRoles",
                table: "UserRoles",
                columns: new[] { "UserId", "RoleId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserLogins",
                table: "UserLogins",
                columns: new[] { "LoginProvider", "ProviderKey" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserClaims",
                table: "UserClaims",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RoleClaims",
                table: "RoleClaims",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "Users",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentPlans_PlanNumber",
                table: "TreatmentPlans",
                column: "PlanNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SterilisationCycles_SteriliserId_CycleNumber",
                table: "SterilisationCycles",
                columns: new[] { "SteriliserId", "CycleNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Staff_StaffNumber",
                table: "Staff",
                column: "StaffNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "Roles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_ReferralNumber",
                table: "Referrals",
                column: "ReferralNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_OrderNumber",
                table: "PurchaseOrders",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_PrescriptionNumber",
                table: "Prescriptions",
                column: "PrescriptionNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PostOperativeInstructions_Code",
                table: "PostOperativeInstructions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PeriodontalMeasurements_PeriodontalChartId_ToothId_Site",
                table: "PeriodontalMeasurements",
                columns: new[] { "PeriodontalChartId", "ToothId", "Site" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PaymentNumber",
                table: "Payments",
                column: "PaymentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentPlans_PlanNumber",
                table: "PaymentPlans",
                column: "PlanNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentPlanInstallments_PaymentPlanId_InstallmentNumber",
                table: "PaymentPlanInstallments",
                columns: new[] { "PaymentPlanId", "InstallmentNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Patients_PatientNumber",
                table: "Patients",
                column: "PatientNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Operatories_LocationId_Code",
                table: "Operatories",
                columns: new[] { "LocationId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NumberSequences_Name",
                table: "NumberSequences",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MessageTemplates_Code",
                table: "MessageTemplates",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Locations_Code",
                table: "Locations",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LabCases_CaseNumber",
                table: "LabCases",
                column: "CaseNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_InvoiceNumber",
                table: "Invoices",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_Sku",
                table: "InventoryItems",
                column: "Sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceClaims_ClaimNumber",
                table: "InsuranceClaims",
                column: "ClaimNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InstrumentSets_SetCode",
                table: "InstrumentSets",
                column: "SetCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FeeScheduleItems_FeeScheduleId_ProcedureCodeId",
                table: "FeeScheduleItems",
                columns: new[] { "FeeScheduleId", "ProcedureCodeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConsentFormTemplates_Code",
                table: "ConsentFormTemplates",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppSettings_Key",
                table: "AppSettings",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_AppointmentNumber",
                table: "Appointments",
                column: "AppointmentNumber",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_RoleClaims_Roles_RoleId",
                table: "RoleClaims",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserClaims_Users_UserId",
                table: "UserClaims",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserLogins_Users_UserId",
                table: "UserLogins",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoles_Roles_RoleId",
                table: "UserRoles",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoles_Users_UserId",
                table: "UserRoles",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserTokens_Users_UserId",
                table: "UserTokens",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
