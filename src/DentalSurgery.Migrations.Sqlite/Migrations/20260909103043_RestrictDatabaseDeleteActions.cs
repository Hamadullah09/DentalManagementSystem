using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalSurgery.Migrations.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class RestrictDatabaseDeleteActions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AnaesthesiaRecords_Staff_AdministeredByStaffId",
                table: "AnaesthesiaRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_AppointmentProcedures_Teeth_ToothId",
                table: "AppointmentProcedures");

            migrationBuilder.DropForeignKey(
                name: "FK_AppointmentProcedures_TreatmentPlanItems_TreatmentPlanItemId",
                table: "AppointmentProcedures");

            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_Operatories_OperatoryId",
                table: "Appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_Staff_AssistantId",
                table: "Appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_ClinicalNotes_Appointments_AppointmentId",
                table: "ClinicalNotes");

            migrationBuilder.DropForeignKey(
                name: "FK_ClinicalNotes_Staff_ProviderId",
                table: "ClinicalNotes");

            migrationBuilder.DropForeignKey(
                name: "FK_CommunicationLogs_Staff_StaffId",
                table: "CommunicationLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_DentalImplants_Procedures_PlacementProcedureId",
                table: "DentalImplants");

            migrationBuilder.DropForeignKey(
                name: "FK_DentalImplants_Staff_SurgeonStaffId",
                table: "DentalImplants");

            migrationBuilder.DropForeignKey(
                name: "FK_FeeSchedules_InsuranceCarriers_InsuranceCarrierId",
                table: "FeeSchedules");

            migrationBuilder.DropForeignKey(
                name: "FK_InstrumentSets_SterilisationCycles_LastCycleId",
                table: "InstrumentSets");

            migrationBuilder.DropForeignKey(
                name: "FK_InsuranceClaimLines_Procedures_ProcedureId",
                table: "InsuranceClaimLines");

            migrationBuilder.DropForeignKey(
                name: "FK_InsuranceClaimLines_Teeth_ToothId",
                table: "InsuranceClaimLines");

            migrationBuilder.DropForeignKey(
                name: "FK_InsuranceClaims_Invoices_InvoiceId",
                table: "InsuranceClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_InsuranceClaims_Staff_ProviderId",
                table: "InsuranceClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_InsurancePlans_FeeSchedules_FeeScheduleId",
                table: "InsurancePlans");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryItems_Suppliers_PreferredSupplierId",
                table: "InventoryItems");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryLots_PurchaseOrders_PurchaseOrderId",
                table: "InventoryLots");

            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceLines_ProcedureCodes_ProcedureCodeId",
                table: "InvoiceLines");

            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceLines_Teeth_ToothId",
                table: "InvoiceLines");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Locations_LocationId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Patients_GuarantorPatientId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Staff_ProviderId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_LabCases_Staff_ProviderId",
                table: "LabCases");

            migrationBuilder.DropForeignKey(
                name: "FK_MedicalHistoryReviews_Staff_ReviewedByStaffId",
                table: "MedicalHistoryReviews");

            migrationBuilder.DropForeignKey(
                name: "FK_PatientConsents_Staff_ClinicianStaffId",
                table: "PatientConsents");

            migrationBuilder.DropForeignKey(
                name: "FK_PatientConsents_Staff_WitnessStaffId",
                table: "PatientConsents");

            migrationBuilder.DropForeignKey(
                name: "FK_PatientDiagnoses_Staff_DiagnosedByStaffId",
                table: "PatientDiagnoses");

            migrationBuilder.DropForeignKey(
                name: "FK_PatientDiagnoses_Teeth_ToothId",
                table: "PatientDiagnoses");

            migrationBuilder.DropForeignKey(
                name: "FK_Patients_Locations_PreferredLocationId",
                table: "Patients");

            migrationBuilder.DropForeignKey(
                name: "FK_Patients_Patients_GuarantorPatientId",
                table: "Patients");

            migrationBuilder.DropForeignKey(
                name: "FK_Patients_Patients_ReferredByPatientId",
                table: "Patients");

            migrationBuilder.DropForeignKey(
                name: "FK_Patients_Staff_PrimaryHygienistId",
                table: "Patients");

            migrationBuilder.DropForeignKey(
                name: "FK_Patients_Staff_PrimaryProviderId",
                table: "Patients");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_InsuranceClaims_InsuranceClaimId",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_PeriodontalCharts_Staff_ExaminerStaffId",
                table: "PeriodontalCharts");

            migrationBuilder.DropForeignKey(
                name: "FK_Prescriptions_Pharmacies_PharmacyId",
                table: "Prescriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_Prescriptions_Staff_PrescriberStaffId",
                table: "Prescriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_ProcedureMaterialUsages_InventoryLots_InventoryLotId",
                table: "ProcedureMaterialUsages");

            migrationBuilder.DropForeignKey(
                name: "FK_Procedures_Appointments_AppointmentId",
                table: "Procedures");

            migrationBuilder.DropForeignKey(
                name: "FK_Procedures_InvoiceLines_InvoiceLineId",
                table: "Procedures");

            migrationBuilder.DropForeignKey(
                name: "FK_Procedures_Locations_LocationId",
                table: "Procedures");

            migrationBuilder.DropForeignKey(
                name: "FK_Procedures_Staff_AssistantId",
                table: "Procedures");

            migrationBuilder.DropForeignKey(
                name: "FK_Procedures_Teeth_ToothId",
                table: "Procedures");

            migrationBuilder.DropForeignKey(
                name: "FK_Procedures_TreatmentPlanItems_TreatmentPlanItemId",
                table: "Procedures");

            migrationBuilder.DropForeignKey(
                name: "FK_RadiographRecords_PatientDocuments_DocumentId",
                table: "RadiographRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_RadiographRecords_Staff_TakenByStaffId",
                table: "RadiographRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_RecallSchedules_Staff_PreferredProviderId",
                table: "RecallSchedules");

            migrationBuilder.DropForeignKey(
                name: "FK_Referrals_Staff_InternalProviderId",
                table: "Referrals");

            migrationBuilder.DropForeignKey(
                name: "FK_Staff_Locations_DefaultLocationId",
                table: "Staff");

            migrationBuilder.DropForeignKey(
                name: "FK_StaffScheduleSlots_Operatories_DefaultOperatoryId",
                table: "StaffScheduleSlots");

            migrationBuilder.DropForeignKey(
                name: "FK_SterilisationCycles_Staff_OperatorStaffId",
                table: "SterilisationCycles");

            migrationBuilder.DropForeignKey(
                name: "FK_Sterilisers_Locations_LocationId",
                table: "Sterilisers");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_InventoryLots_InventoryLotId",
                table: "StockMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_SurgicalRecords_InstrumentSets_InstrumentSetId",
                table: "SurgicalRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_ToothConditionRecords_Procedures_ProcedureId",
                table: "ToothConditionRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_ToothConditionRecords_Staff_RecordedByStaffId",
                table: "ToothConditionRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_ToothConditionRecords_ToothConditionRecords_SupersedesId",
                table: "ToothConditionRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_TreatmentPlanItems_Staff_ProviderId",
                table: "TreatmentPlanItems");

            migrationBuilder.DropForeignKey(
                name: "FK_TreatmentPlanItems_Teeth_ToothId",
                table: "TreatmentPlanItems");

            migrationBuilder.DropForeignKey(
                name: "FK_TreatmentPlans_FeeSchedules_FeeScheduleId",
                table: "TreatmentPlans");

            migrationBuilder.DropForeignKey(
                name: "FK_TreatmentPlans_Staff_ProviderId",
                table: "TreatmentPlans");

            migrationBuilder.DropForeignKey(
                name: "FK_VitalSignRecords_Staff_RecordedByStaffId",
                table: "VitalSignRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_WaitlistEntries_Locations_LocationId",
                table: "WaitlistEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_WaitlistEntries_Staff_PreferredProviderId",
                table: "WaitlistEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkTasks_Staff_AssignedToStaffId",
                table: "WorkTasks");

            migrationBuilder.AddForeignKey(
                name: "FK_AnaesthesiaRecords_Staff_AdministeredByStaffId",
                table: "AnaesthesiaRecords",
                column: "AdministeredByStaffId",
                principalTable: "Staff",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AppointmentProcedures_Teeth_ToothId",
                table: "AppointmentProcedures",
                column: "ToothId",
                principalTable: "Teeth",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AppointmentProcedures_TreatmentPlanItems_TreatmentPlanItemId",
                table: "AppointmentProcedures",
                column: "TreatmentPlanItemId",
                principalTable: "TreatmentPlanItems",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_Operatories_OperatoryId",
                table: "Appointments",
                column: "OperatoryId",
                principalTable: "Operatories",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_Staff_AssistantId",
                table: "Appointments",
                column: "AssistantId",
                principalTable: "Staff",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ClinicalNotes_Appointments_AppointmentId",
                table: "ClinicalNotes",
                column: "AppointmentId",
                principalTable: "Appointments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ClinicalNotes_Staff_ProviderId",
                table: "ClinicalNotes",
                column: "ProviderId",
                principalTable: "Staff",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CommunicationLogs_Staff_StaffId",
                table: "CommunicationLogs",
                column: "StaffId",
                principalTable: "Staff",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DentalImplants_Procedures_PlacementProcedureId",
                table: "DentalImplants",
                column: "PlacementProcedureId",
                principalTable: "Procedures",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DentalImplants_Staff_SurgeonStaffId",
                table: "DentalImplants",
                column: "SurgeonStaffId",
                principalTable: "Staff",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FeeSchedules_InsuranceCarriers_InsuranceCarrierId",
                table: "FeeSchedules",
                column: "InsuranceCarrierId",
                principalTable: "InsuranceCarriers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InstrumentSets_SterilisationCycles_LastCycleId",
                table: "InstrumentSets",
                column: "LastCycleId",
                principalTable: "SterilisationCycles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InsuranceClaimLines_Procedures_ProcedureId",
                table: "InsuranceClaimLines",
                column: "ProcedureId",
                principalTable: "Procedures",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InsuranceClaimLines_Teeth_ToothId",
                table: "InsuranceClaimLines",
                column: "ToothId",
                principalTable: "Teeth",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InsuranceClaims_Invoices_InvoiceId",
                table: "InsuranceClaims",
                column: "InvoiceId",
                principalTable: "Invoices",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InsuranceClaims_Staff_ProviderId",
                table: "InsuranceClaims",
                column: "ProviderId",
                principalTable: "Staff",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InsurancePlans_FeeSchedules_FeeScheduleId",
                table: "InsurancePlans",
                column: "FeeScheduleId",
                principalTable: "FeeSchedules",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryItems_Suppliers_PreferredSupplierId",
                table: "InventoryItems",
                column: "PreferredSupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryLots_PurchaseOrders_PurchaseOrderId",
                table: "InventoryLots",
                column: "PurchaseOrderId",
                principalTable: "PurchaseOrders",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceLines_ProcedureCodes_ProcedureCodeId",
                table: "InvoiceLines",
                column: "ProcedureCodeId",
                principalTable: "ProcedureCodes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceLines_Teeth_ToothId",
                table: "InvoiceLines",
                column: "ToothId",
                principalTable: "Teeth",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Locations_LocationId",
                table: "Invoices",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Patients_GuarantorPatientId",
                table: "Invoices",
                column: "GuarantorPatientId",
                principalTable: "Patients",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Staff_ProviderId",
                table: "Invoices",
                column: "ProviderId",
                principalTable: "Staff",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LabCases_Staff_ProviderId",
                table: "LabCases",
                column: "ProviderId",
                principalTable: "Staff",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MedicalHistoryReviews_Staff_ReviewedByStaffId",
                table: "MedicalHistoryReviews",
                column: "ReviewedByStaffId",
                principalTable: "Staff",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PatientConsents_Staff_ClinicianStaffId",
                table: "PatientConsents",
                column: "ClinicianStaffId",
                principalTable: "Staff",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PatientConsents_Staff_WitnessStaffId",
                table: "PatientConsents",
                column: "WitnessStaffId",
                principalTable: "Staff",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PatientDiagnoses_Staff_DiagnosedByStaffId",
                table: "PatientDiagnoses",
                column: "DiagnosedByStaffId",
                principalTable: "Staff",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PatientDiagnoses_Teeth_ToothId",
                table: "PatientDiagnoses",
                column: "ToothId",
                principalTable: "Teeth",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Patients_Locations_PreferredLocationId",
                table: "Patients",
                column: "PreferredLocationId",
                principalTable: "Locations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Patients_Patients_GuarantorPatientId",
                table: "Patients",
                column: "GuarantorPatientId",
                principalTable: "Patients",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Patients_Patients_ReferredByPatientId",
                table: "Patients",
                column: "ReferredByPatientId",
                principalTable: "Patients",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Patients_Staff_PrimaryHygienistId",
                table: "Patients",
                column: "PrimaryHygienistId",
                principalTable: "Staff",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Patients_Staff_PrimaryProviderId",
                table: "Patients",
                column: "PrimaryProviderId",
                principalTable: "Staff",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_InsuranceClaims_InsuranceClaimId",
                table: "Payments",
                column: "InsuranceClaimId",
                principalTable: "InsuranceClaims",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PeriodontalCharts_Staff_ExaminerStaffId",
                table: "PeriodontalCharts",
                column: "ExaminerStaffId",
                principalTable: "Staff",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Prescriptions_Pharmacies_PharmacyId",
                table: "Prescriptions",
                column: "PharmacyId",
                principalTable: "Pharmacies",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Prescriptions_Staff_PrescriberStaffId",
                table: "Prescriptions",
                column: "PrescriberStaffId",
                principalTable: "Staff",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProcedureMaterialUsages_InventoryLots_InventoryLotId",
                table: "ProcedureMaterialUsages",
                column: "InventoryLotId",
                principalTable: "InventoryLots",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Procedures_Appointments_AppointmentId",
                table: "Procedures",
                column: "AppointmentId",
                principalTable: "Appointments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Procedures_InvoiceLines_InvoiceLineId",
                table: "Procedures",
                column: "InvoiceLineId",
                principalTable: "InvoiceLines",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Procedures_Locations_LocationId",
                table: "Procedures",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Procedures_Staff_AssistantId",
                table: "Procedures",
                column: "AssistantId",
                principalTable: "Staff",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Procedures_Teeth_ToothId",
                table: "Procedures",
                column: "ToothId",
                principalTable: "Teeth",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Procedures_TreatmentPlanItems_TreatmentPlanItemId",
                table: "Procedures",
                column: "TreatmentPlanItemId",
                principalTable: "TreatmentPlanItems",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RadiographRecords_PatientDocuments_DocumentId",
                table: "RadiographRecords",
                column: "DocumentId",
                principalTable: "PatientDocuments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RadiographRecords_Staff_TakenByStaffId",
                table: "RadiographRecords",
                column: "TakenByStaffId",
                principalTable: "Staff",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RecallSchedules_Staff_PreferredProviderId",
                table: "RecallSchedules",
                column: "PreferredProviderId",
                principalTable: "Staff",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Referrals_Staff_InternalProviderId",
                table: "Referrals",
                column: "InternalProviderId",
                principalTable: "Staff",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Staff_Locations_DefaultLocationId",
                table: "Staff",
                column: "DefaultLocationId",
                principalTable: "Locations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StaffScheduleSlots_Operatories_DefaultOperatoryId",
                table: "StaffScheduleSlots",
                column: "DefaultOperatoryId",
                principalTable: "Operatories",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SterilisationCycles_Staff_OperatorStaffId",
                table: "SterilisationCycles",
                column: "OperatorStaffId",
                principalTable: "Staff",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Sterilisers_Locations_LocationId",
                table: "Sterilisers",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_InventoryLots_InventoryLotId",
                table: "StockMovements",
                column: "InventoryLotId",
                principalTable: "InventoryLots",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SurgicalRecords_InstrumentSets_InstrumentSetId",
                table: "SurgicalRecords",
                column: "InstrumentSetId",
                principalTable: "InstrumentSets",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ToothConditionRecords_Procedures_ProcedureId",
                table: "ToothConditionRecords",
                column: "ProcedureId",
                principalTable: "Procedures",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ToothConditionRecords_Staff_RecordedByStaffId",
                table: "ToothConditionRecords",
                column: "RecordedByStaffId",
                principalTable: "Staff",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ToothConditionRecords_ToothConditionRecords_SupersedesId",
                table: "ToothConditionRecords",
                column: "SupersedesId",
                principalTable: "ToothConditionRecords",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TreatmentPlanItems_Staff_ProviderId",
                table: "TreatmentPlanItems",
                column: "ProviderId",
                principalTable: "Staff",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TreatmentPlanItems_Teeth_ToothId",
                table: "TreatmentPlanItems",
                column: "ToothId",
                principalTable: "Teeth",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TreatmentPlans_FeeSchedules_FeeScheduleId",
                table: "TreatmentPlans",
                column: "FeeScheduleId",
                principalTable: "FeeSchedules",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TreatmentPlans_Staff_ProviderId",
                table: "TreatmentPlans",
                column: "ProviderId",
                principalTable: "Staff",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_VitalSignRecords_Staff_RecordedByStaffId",
                table: "VitalSignRecords",
                column: "RecordedByStaffId",
                principalTable: "Staff",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_WaitlistEntries_Locations_LocationId",
                table: "WaitlistEntries",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_WaitlistEntries_Staff_PreferredProviderId",
                table: "WaitlistEntries",
                column: "PreferredProviderId",
                principalTable: "Staff",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkTasks_Staff_AssignedToStaffId",
                table: "WorkTasks",
                column: "AssignedToStaffId",
                principalTable: "Staff",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AnaesthesiaRecords_Staff_AdministeredByStaffId",
                table: "AnaesthesiaRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_AppointmentProcedures_Teeth_ToothId",
                table: "AppointmentProcedures");

            migrationBuilder.DropForeignKey(
                name: "FK_AppointmentProcedures_TreatmentPlanItems_TreatmentPlanItemId",
                table: "AppointmentProcedures");

            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_Operatories_OperatoryId",
                table: "Appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_Staff_AssistantId",
                table: "Appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_ClinicalNotes_Appointments_AppointmentId",
                table: "ClinicalNotes");

            migrationBuilder.DropForeignKey(
                name: "FK_ClinicalNotes_Staff_ProviderId",
                table: "ClinicalNotes");

            migrationBuilder.DropForeignKey(
                name: "FK_CommunicationLogs_Staff_StaffId",
                table: "CommunicationLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_DentalImplants_Procedures_PlacementProcedureId",
                table: "DentalImplants");

            migrationBuilder.DropForeignKey(
                name: "FK_DentalImplants_Staff_SurgeonStaffId",
                table: "DentalImplants");

            migrationBuilder.DropForeignKey(
                name: "FK_FeeSchedules_InsuranceCarriers_InsuranceCarrierId",
                table: "FeeSchedules");

            migrationBuilder.DropForeignKey(
                name: "FK_InstrumentSets_SterilisationCycles_LastCycleId",
                table: "InstrumentSets");

            migrationBuilder.DropForeignKey(
                name: "FK_InsuranceClaimLines_Procedures_ProcedureId",
                table: "InsuranceClaimLines");

            migrationBuilder.DropForeignKey(
                name: "FK_InsuranceClaimLines_Teeth_ToothId",
                table: "InsuranceClaimLines");

            migrationBuilder.DropForeignKey(
                name: "FK_InsuranceClaims_Invoices_InvoiceId",
                table: "InsuranceClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_InsuranceClaims_Staff_ProviderId",
                table: "InsuranceClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_InsurancePlans_FeeSchedules_FeeScheduleId",
                table: "InsurancePlans");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryItems_Suppliers_PreferredSupplierId",
                table: "InventoryItems");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryLots_PurchaseOrders_PurchaseOrderId",
                table: "InventoryLots");

            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceLines_ProcedureCodes_ProcedureCodeId",
                table: "InvoiceLines");

            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceLines_Teeth_ToothId",
                table: "InvoiceLines");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Locations_LocationId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Patients_GuarantorPatientId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Staff_ProviderId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_LabCases_Staff_ProviderId",
                table: "LabCases");

            migrationBuilder.DropForeignKey(
                name: "FK_MedicalHistoryReviews_Staff_ReviewedByStaffId",
                table: "MedicalHistoryReviews");

            migrationBuilder.DropForeignKey(
                name: "FK_PatientConsents_Staff_ClinicianStaffId",
                table: "PatientConsents");

            migrationBuilder.DropForeignKey(
                name: "FK_PatientConsents_Staff_WitnessStaffId",
                table: "PatientConsents");

            migrationBuilder.DropForeignKey(
                name: "FK_PatientDiagnoses_Staff_DiagnosedByStaffId",
                table: "PatientDiagnoses");

            migrationBuilder.DropForeignKey(
                name: "FK_PatientDiagnoses_Teeth_ToothId",
                table: "PatientDiagnoses");

            migrationBuilder.DropForeignKey(
                name: "FK_Patients_Locations_PreferredLocationId",
                table: "Patients");

            migrationBuilder.DropForeignKey(
                name: "FK_Patients_Patients_GuarantorPatientId",
                table: "Patients");

            migrationBuilder.DropForeignKey(
                name: "FK_Patients_Patients_ReferredByPatientId",
                table: "Patients");

            migrationBuilder.DropForeignKey(
                name: "FK_Patients_Staff_PrimaryHygienistId",
                table: "Patients");

            migrationBuilder.DropForeignKey(
                name: "FK_Patients_Staff_PrimaryProviderId",
                table: "Patients");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_InsuranceClaims_InsuranceClaimId",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_PeriodontalCharts_Staff_ExaminerStaffId",
                table: "PeriodontalCharts");

            migrationBuilder.DropForeignKey(
                name: "FK_Prescriptions_Pharmacies_PharmacyId",
                table: "Prescriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_Prescriptions_Staff_PrescriberStaffId",
                table: "Prescriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_ProcedureMaterialUsages_InventoryLots_InventoryLotId",
                table: "ProcedureMaterialUsages");

            migrationBuilder.DropForeignKey(
                name: "FK_Procedures_Appointments_AppointmentId",
                table: "Procedures");

            migrationBuilder.DropForeignKey(
                name: "FK_Procedures_InvoiceLines_InvoiceLineId",
                table: "Procedures");

            migrationBuilder.DropForeignKey(
                name: "FK_Procedures_Locations_LocationId",
                table: "Procedures");

            migrationBuilder.DropForeignKey(
                name: "FK_Procedures_Staff_AssistantId",
                table: "Procedures");

            migrationBuilder.DropForeignKey(
                name: "FK_Procedures_Teeth_ToothId",
                table: "Procedures");

            migrationBuilder.DropForeignKey(
                name: "FK_Procedures_TreatmentPlanItems_TreatmentPlanItemId",
                table: "Procedures");

            migrationBuilder.DropForeignKey(
                name: "FK_RadiographRecords_PatientDocuments_DocumentId",
                table: "RadiographRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_RadiographRecords_Staff_TakenByStaffId",
                table: "RadiographRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_RecallSchedules_Staff_PreferredProviderId",
                table: "RecallSchedules");

            migrationBuilder.DropForeignKey(
                name: "FK_Referrals_Staff_InternalProviderId",
                table: "Referrals");

            migrationBuilder.DropForeignKey(
                name: "FK_Staff_Locations_DefaultLocationId",
                table: "Staff");

            migrationBuilder.DropForeignKey(
                name: "FK_StaffScheduleSlots_Operatories_DefaultOperatoryId",
                table: "StaffScheduleSlots");

            migrationBuilder.DropForeignKey(
                name: "FK_SterilisationCycles_Staff_OperatorStaffId",
                table: "SterilisationCycles");

            migrationBuilder.DropForeignKey(
                name: "FK_Sterilisers_Locations_LocationId",
                table: "Sterilisers");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_InventoryLots_InventoryLotId",
                table: "StockMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_SurgicalRecords_InstrumentSets_InstrumentSetId",
                table: "SurgicalRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_ToothConditionRecords_Procedures_ProcedureId",
                table: "ToothConditionRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_ToothConditionRecords_Staff_RecordedByStaffId",
                table: "ToothConditionRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_ToothConditionRecords_ToothConditionRecords_SupersedesId",
                table: "ToothConditionRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_TreatmentPlanItems_Staff_ProviderId",
                table: "TreatmentPlanItems");

            migrationBuilder.DropForeignKey(
                name: "FK_TreatmentPlanItems_Teeth_ToothId",
                table: "TreatmentPlanItems");

            migrationBuilder.DropForeignKey(
                name: "FK_TreatmentPlans_FeeSchedules_FeeScheduleId",
                table: "TreatmentPlans");

            migrationBuilder.DropForeignKey(
                name: "FK_TreatmentPlans_Staff_ProviderId",
                table: "TreatmentPlans");

            migrationBuilder.DropForeignKey(
                name: "FK_VitalSignRecords_Staff_RecordedByStaffId",
                table: "VitalSignRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_WaitlistEntries_Locations_LocationId",
                table: "WaitlistEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_WaitlistEntries_Staff_PreferredProviderId",
                table: "WaitlistEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkTasks_Staff_AssignedToStaffId",
                table: "WorkTasks");

            migrationBuilder.AddForeignKey(
                name: "FK_AnaesthesiaRecords_Staff_AdministeredByStaffId",
                table: "AnaesthesiaRecords",
                column: "AdministeredByStaffId",
                principalTable: "Staff",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AppointmentProcedures_Teeth_ToothId",
                table: "AppointmentProcedures",
                column: "ToothId",
                principalTable: "Teeth",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AppointmentProcedures_TreatmentPlanItems_TreatmentPlanItemId",
                table: "AppointmentProcedures",
                column: "TreatmentPlanItemId",
                principalTable: "TreatmentPlanItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_Operatories_OperatoryId",
                table: "Appointments",
                column: "OperatoryId",
                principalTable: "Operatories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_Staff_AssistantId",
                table: "Appointments",
                column: "AssistantId",
                principalTable: "Staff",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ClinicalNotes_Appointments_AppointmentId",
                table: "ClinicalNotes",
                column: "AppointmentId",
                principalTable: "Appointments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ClinicalNotes_Staff_ProviderId",
                table: "ClinicalNotes",
                column: "ProviderId",
                principalTable: "Staff",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_CommunicationLogs_Staff_StaffId",
                table: "CommunicationLogs",
                column: "StaffId",
                principalTable: "Staff",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_DentalImplants_Procedures_PlacementProcedureId",
                table: "DentalImplants",
                column: "PlacementProcedureId",
                principalTable: "Procedures",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_DentalImplants_Staff_SurgeonStaffId",
                table: "DentalImplants",
                column: "SurgeonStaffId",
                principalTable: "Staff",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_FeeSchedules_InsuranceCarriers_InsuranceCarrierId",
                table: "FeeSchedules",
                column: "InsuranceCarrierId",
                principalTable: "InsuranceCarriers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_InstrumentSets_SterilisationCycles_LastCycleId",
                table: "InstrumentSets",
                column: "LastCycleId",
                principalTable: "SterilisationCycles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_InsuranceClaimLines_Procedures_ProcedureId",
                table: "InsuranceClaimLines",
                column: "ProcedureId",
                principalTable: "Procedures",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_InsuranceClaimLines_Teeth_ToothId",
                table: "InsuranceClaimLines",
                column: "ToothId",
                principalTable: "Teeth",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_InsuranceClaims_Invoices_InvoiceId",
                table: "InsuranceClaims",
                column: "InvoiceId",
                principalTable: "Invoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_InsuranceClaims_Staff_ProviderId",
                table: "InsuranceClaims",
                column: "ProviderId",
                principalTable: "Staff",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_InsurancePlans_FeeSchedules_FeeScheduleId",
                table: "InsurancePlans",
                column: "FeeScheduleId",
                principalTable: "FeeSchedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryItems_Suppliers_PreferredSupplierId",
                table: "InventoryItems",
                column: "PreferredSupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryLots_PurchaseOrders_PurchaseOrderId",
                table: "InventoryLots",
                column: "PurchaseOrderId",
                principalTable: "PurchaseOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceLines_ProcedureCodes_ProcedureCodeId",
                table: "InvoiceLines",
                column: "ProcedureCodeId",
                principalTable: "ProcedureCodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceLines_Teeth_ToothId",
                table: "InvoiceLines",
                column: "ToothId",
                principalTable: "Teeth",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Locations_LocationId",
                table: "Invoices",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Patients_GuarantorPatientId",
                table: "Invoices",
                column: "GuarantorPatientId",
                principalTable: "Patients",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Staff_ProviderId",
                table: "Invoices",
                column: "ProviderId",
                principalTable: "Staff",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_LabCases_Staff_ProviderId",
                table: "LabCases",
                column: "ProviderId",
                principalTable: "Staff",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_MedicalHistoryReviews_Staff_ReviewedByStaffId",
                table: "MedicalHistoryReviews",
                column: "ReviewedByStaffId",
                principalTable: "Staff",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PatientConsents_Staff_ClinicianStaffId",
                table: "PatientConsents",
                column: "ClinicianStaffId",
                principalTable: "Staff",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PatientConsents_Staff_WitnessStaffId",
                table: "PatientConsents",
                column: "WitnessStaffId",
                principalTable: "Staff",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PatientDiagnoses_Staff_DiagnosedByStaffId",
                table: "PatientDiagnoses",
                column: "DiagnosedByStaffId",
                principalTable: "Staff",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PatientDiagnoses_Teeth_ToothId",
                table: "PatientDiagnoses",
                column: "ToothId",
                principalTable: "Teeth",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Patients_Locations_PreferredLocationId",
                table: "Patients",
                column: "PreferredLocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Patients_Patients_GuarantorPatientId",
                table: "Patients",
                column: "GuarantorPatientId",
                principalTable: "Patients",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Patients_Patients_ReferredByPatientId",
                table: "Patients",
                column: "ReferredByPatientId",
                principalTable: "Patients",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Patients_Staff_PrimaryHygienistId",
                table: "Patients",
                column: "PrimaryHygienistId",
                principalTable: "Staff",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Patients_Staff_PrimaryProviderId",
                table: "Patients",
                column: "PrimaryProviderId",
                principalTable: "Staff",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_InsuranceClaims_InsuranceClaimId",
                table: "Payments",
                column: "InsuranceClaimId",
                principalTable: "InsuranceClaims",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PeriodontalCharts_Staff_ExaminerStaffId",
                table: "PeriodontalCharts",
                column: "ExaminerStaffId",
                principalTable: "Staff",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Prescriptions_Pharmacies_PharmacyId",
                table: "Prescriptions",
                column: "PharmacyId",
                principalTable: "Pharmacies",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Prescriptions_Staff_PrescriberStaffId",
                table: "Prescriptions",
                column: "PrescriberStaffId",
                principalTable: "Staff",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ProcedureMaterialUsages_InventoryLots_InventoryLotId",
                table: "ProcedureMaterialUsages",
                column: "InventoryLotId",
                principalTable: "InventoryLots",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Procedures_Appointments_AppointmentId",
                table: "Procedures",
                column: "AppointmentId",
                principalTable: "Appointments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Procedures_InvoiceLines_InvoiceLineId",
                table: "Procedures",
                column: "InvoiceLineId",
                principalTable: "InvoiceLines",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Procedures_Locations_LocationId",
                table: "Procedures",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Procedures_Staff_AssistantId",
                table: "Procedures",
                column: "AssistantId",
                principalTable: "Staff",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Procedures_Teeth_ToothId",
                table: "Procedures",
                column: "ToothId",
                principalTable: "Teeth",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Procedures_TreatmentPlanItems_TreatmentPlanItemId",
                table: "Procedures",
                column: "TreatmentPlanItemId",
                principalTable: "TreatmentPlanItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_RadiographRecords_PatientDocuments_DocumentId",
                table: "RadiographRecords",
                column: "DocumentId",
                principalTable: "PatientDocuments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_RadiographRecords_Staff_TakenByStaffId",
                table: "RadiographRecords",
                column: "TakenByStaffId",
                principalTable: "Staff",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_RecallSchedules_Staff_PreferredProviderId",
                table: "RecallSchedules",
                column: "PreferredProviderId",
                principalTable: "Staff",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Referrals_Staff_InternalProviderId",
                table: "Referrals",
                column: "InternalProviderId",
                principalTable: "Staff",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Staff_Locations_DefaultLocationId",
                table: "Staff",
                column: "DefaultLocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_StaffScheduleSlots_Operatories_DefaultOperatoryId",
                table: "StaffScheduleSlots",
                column: "DefaultOperatoryId",
                principalTable: "Operatories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SterilisationCycles_Staff_OperatorStaffId",
                table: "SterilisationCycles",
                column: "OperatorStaffId",
                principalTable: "Staff",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Sterilisers_Locations_LocationId",
                table: "Sterilisers",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_InventoryLots_InventoryLotId",
                table: "StockMovements",
                column: "InventoryLotId",
                principalTable: "InventoryLots",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SurgicalRecords_InstrumentSets_InstrumentSetId",
                table: "SurgicalRecords",
                column: "InstrumentSetId",
                principalTable: "InstrumentSets",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ToothConditionRecords_Procedures_ProcedureId",
                table: "ToothConditionRecords",
                column: "ProcedureId",
                principalTable: "Procedures",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ToothConditionRecords_Staff_RecordedByStaffId",
                table: "ToothConditionRecords",
                column: "RecordedByStaffId",
                principalTable: "Staff",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ToothConditionRecords_ToothConditionRecords_SupersedesId",
                table: "ToothConditionRecords",
                column: "SupersedesId",
                principalTable: "ToothConditionRecords",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TreatmentPlanItems_Staff_ProviderId",
                table: "TreatmentPlanItems",
                column: "ProviderId",
                principalTable: "Staff",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TreatmentPlanItems_Teeth_ToothId",
                table: "TreatmentPlanItems",
                column: "ToothId",
                principalTable: "Teeth",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TreatmentPlans_FeeSchedules_FeeScheduleId",
                table: "TreatmentPlans",
                column: "FeeScheduleId",
                principalTable: "FeeSchedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TreatmentPlans_Staff_ProviderId",
                table: "TreatmentPlans",
                column: "ProviderId",
                principalTable: "Staff",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_VitalSignRecords_Staff_RecordedByStaffId",
                table: "VitalSignRecords",
                column: "RecordedByStaffId",
                principalTable: "Staff",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_WaitlistEntries_Locations_LocationId",
                table: "WaitlistEntries",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_WaitlistEntries_Staff_PreferredProviderId",
                table: "WaitlistEntries",
                column: "PreferredProviderId",
                principalTable: "Staff",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkTasks_Staff_AssignedToStaffId",
                table: "WorkTasks",
                column: "AssignedToStaffId",
                principalTable: "Staff",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
