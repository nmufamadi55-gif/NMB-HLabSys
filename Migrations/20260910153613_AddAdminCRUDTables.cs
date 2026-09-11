using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NMB_HLabSys.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminCRUDTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Allergy_LookupCategory_CategoryID",
                table: "Allergy");

            migrationBuilder.DropForeignKey(
                name: "FK_MedicalCondition_LookupCategory_CategoryID",
                table: "MedicalCondition");

            migrationBuilder.DropForeignKey(
                name: "FK_Medication_LookupCategory_CategoryID",
                table: "Medication");

            migrationBuilder.DropForeignKey(
                name: "FK_PatientAllergies_Allergy_AllergyID",
                table: "PatientAllergies");

            migrationBuilder.DropForeignKey(
                name: "FK_PatientConditions_MedicalCondition_ConditionID",
                table: "PatientConditions");

            migrationBuilder.DropForeignKey(
                name: "FK_PatientMedications_Medication_MedicationID",
                table: "PatientMedications");

            migrationBuilder.DropIndex(
                name: "IX_Patients_UserID",
                table: "Patients");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Medication",
                table: "Medication");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MedicalCondition",
                table: "MedicalCondition");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LookupCategory",
                table: "LookupCategory");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Allergy",
                table: "Allergy");

            migrationBuilder.RenameTable(
                name: "Medication",
                newName: "Medications");

            migrationBuilder.RenameTable(
                name: "MedicalCondition",
                newName: "MedicalConditions");

            migrationBuilder.RenameTable(
                name: "LookupCategory",
                newName: "LookupCategories");

            migrationBuilder.RenameTable(
                name: "Allergy",
                newName: "Allergies");

            migrationBuilder.RenameIndex(
                name: "IX_Medication_CategoryID",
                table: "Medications",
                newName: "IX_Medications_CategoryID");

            migrationBuilder.RenameIndex(
                name: "IX_MedicalCondition_CategoryID",
                table: "MedicalConditions",
                newName: "IX_MedicalConditions_CategoryID");

            migrationBuilder.RenameIndex(
                name: "IX_Allergy_CategoryID",
                table: "Allergies",
                newName: "IX_Allergies_CategoryID");

            migrationBuilder.AddColumn<string>(
                name: "AspNetUserID",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactNumber",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Users",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Surname",
                table: "Users",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "LookupCategories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Medications",
                table: "Medications",
                column: "MedicationID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MedicalConditions",
                table: "MedicalConditions",
                column: "ConditionID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LookupCategories",
                table: "LookupCategories",
                column: "CategoryID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Allergies",
                table: "Allergies",
                column: "AllergyID");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_UserID",
                table: "Patients",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_Medication_Name_Unique",
                table: "Medications",
                column: "MedicationName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MedicalCondition_Name_Unique",
                table: "MedicalConditions",
                column: "ConditionName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LookupCategory_Name_Unique",
                table: "LookupCategories",
                column: "CategoryName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Allergy_Name_Unique",
                table: "Allergies",
                column: "AllergyName",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Allergies_LookupCategories_CategoryID",
                table: "Allergies",
                column: "CategoryID",
                principalTable: "LookupCategories",
                principalColumn: "CategoryID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MedicalConditions_LookupCategories_CategoryID",
                table: "MedicalConditions",
                column: "CategoryID",
                principalTable: "LookupCategories",
                principalColumn: "CategoryID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Medications_LookupCategories_CategoryID",
                table: "Medications",
                column: "CategoryID",
                principalTable: "LookupCategories",
                principalColumn: "CategoryID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PatientAllergies_Allergies_AllergyID",
                table: "PatientAllergies",
                column: "AllergyID",
                principalTable: "Allergies",
                principalColumn: "AllergyID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PatientConditions_MedicalConditions_ConditionID",
                table: "PatientConditions",
                column: "ConditionID",
                principalTable: "MedicalConditions",
                principalColumn: "ConditionID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PatientMedications_Medications_MedicationID",
                table: "PatientMedications",
                column: "MedicationID",
                principalTable: "Medications",
                principalColumn: "MedicationID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Allergies_LookupCategories_CategoryID",
                table: "Allergies");

            migrationBuilder.DropForeignKey(
                name: "FK_MedicalConditions_LookupCategories_CategoryID",
                table: "MedicalConditions");

            migrationBuilder.DropForeignKey(
                name: "FK_Medications_LookupCategories_CategoryID",
                table: "Medications");

            migrationBuilder.DropForeignKey(
                name: "FK_PatientAllergies_Allergies_AllergyID",
                table: "PatientAllergies");

            migrationBuilder.DropForeignKey(
                name: "FK_PatientConditions_MedicalConditions_ConditionID",
                table: "PatientConditions");

            migrationBuilder.DropForeignKey(
                name: "FK_PatientMedications_Medications_MedicationID",
                table: "PatientMedications");

            migrationBuilder.DropIndex(
                name: "IX_Patients_UserID",
                table: "Patients");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Medications",
                table: "Medications");

            migrationBuilder.DropIndex(
                name: "IX_Medication_Name_Unique",
                table: "Medications");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MedicalConditions",
                table: "MedicalConditions");

            migrationBuilder.DropIndex(
                name: "IX_MedicalCondition_Name_Unique",
                table: "MedicalConditions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LookupCategories",
                table: "LookupCategories");

            migrationBuilder.DropIndex(
                name: "IX_LookupCategory_Name_Unique",
                table: "LookupCategories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Allergies",
                table: "Allergies");

            migrationBuilder.DropIndex(
                name: "IX_Allergy_Name_Unique",
                table: "Allergies");

            migrationBuilder.DropColumn(
                name: "AspNetUserID",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ContactNumber",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Surname",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "LookupCategories");

            migrationBuilder.RenameTable(
                name: "Medications",
                newName: "Medication");

            migrationBuilder.RenameTable(
                name: "MedicalConditions",
                newName: "MedicalCondition");

            migrationBuilder.RenameTable(
                name: "LookupCategories",
                newName: "LookupCategory");

            migrationBuilder.RenameTable(
                name: "Allergies",
                newName: "Allergy");

            migrationBuilder.RenameIndex(
                name: "IX_Medications_CategoryID",
                table: "Medication",
                newName: "IX_Medication_CategoryID");

            migrationBuilder.RenameIndex(
                name: "IX_MedicalConditions_CategoryID",
                table: "MedicalCondition",
                newName: "IX_MedicalCondition_CategoryID");

            migrationBuilder.RenameIndex(
                name: "IX_Allergies_CategoryID",
                table: "Allergy",
                newName: "IX_Allergy_CategoryID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Medication",
                table: "Medication",
                column: "MedicationID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MedicalCondition",
                table: "MedicalCondition",
                column: "ConditionID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LookupCategory",
                table: "LookupCategory",
                column: "CategoryID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Allergy",
                table: "Allergy",
                column: "AllergyID");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_UserID",
                table: "Patients",
                column: "UserID",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Allergy_LookupCategory_CategoryID",
                table: "Allergy",
                column: "CategoryID",
                principalTable: "LookupCategory",
                principalColumn: "CategoryID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MedicalCondition_LookupCategory_CategoryID",
                table: "MedicalCondition",
                column: "CategoryID",
                principalTable: "LookupCategory",
                principalColumn: "CategoryID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Medication_LookupCategory_CategoryID",
                table: "Medication",
                column: "CategoryID",
                principalTable: "LookupCategory",
                principalColumn: "CategoryID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PatientAllergies_Allergy_AllergyID",
                table: "PatientAllergies",
                column: "AllergyID",
                principalTable: "Allergy",
                principalColumn: "AllergyID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PatientConditions_MedicalCondition_ConditionID",
                table: "PatientConditions",
                column: "ConditionID",
                principalTable: "MedicalCondition",
                principalColumn: "ConditionID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PatientMedications_Medication_MedicationID",
                table: "PatientMedications",
                column: "MedicationID",
                principalTable: "Medication",
                principalColumn: "MedicationID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
