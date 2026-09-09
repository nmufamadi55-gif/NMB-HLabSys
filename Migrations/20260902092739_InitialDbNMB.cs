using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NMB_HLabSys.Migrations
{
    /// <inheritdoc />
    public partial class InitialDbNMB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    AuditLogID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActivityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ActivityDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    UserID = table.Column<int>(type: "int", nullable: true),
                    IPAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.AuditLogID);
                });

            migrationBuilder.CreateTable(
                name: "LookupCategory",
                columns: table => new
                {
                    CategoryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CategoryType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LookupCategory", x => x.CategoryID);
                });

            migrationBuilder.CreateTable(
                name: "SampleTypes",
                columns: table => new
                {
                    SampleTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SampleTypeName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CollectionTubeType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StorageRequirements = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SampleTypes", x => x.SampleTypeID);
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    SupplierID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplierName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ContactPerson = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    EmailAddress = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsPreferred = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.SupplierID);
                });

            migrationBuilder.CreateTable(
                name: "TestCategories",
                columns: table => new
                {
                    CategoryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestCategories", x => x.CategoryID);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UserRole = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsPasswordChanged = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserID);
                });

            migrationBuilder.CreateTable(
                name: "Allergy",
                columns: table => new
                {
                    AllergyID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AllergyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CategoryID = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Allergy", x => x.AllergyID);
                    table.ForeignKey(
                        name: "FK_Allergy_LookupCategory_CategoryID",
                        column: x => x.CategoryID,
                        principalTable: "LookupCategory",
                        principalColumn: "CategoryID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MedicalCondition",
                columns: table => new
                {
                    ConditionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConditionName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CategoryID = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalCondition", x => x.ConditionID);
                    table.ForeignKey(
                        name: "FK_MedicalCondition_LookupCategory_CategoryID",
                        column: x => x.CategoryID,
                        principalTable: "LookupCategory",
                        principalColumn: "CategoryID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Medication",
                columns: table => new
                {
                    MedicationID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MedicationName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CategoryID = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Medication", x => x.MedicationID);
                    table.ForeignKey(
                        name: "FK_Medication_LookupCategory_CategoryID",
                        column: x => x.CategoryID,
                        principalTable: "LookupCategory",
                        principalColumn: "CategoryID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Consumables",
                columns: table => new
                {
                    ConsumableID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConsumableName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SupplierID = table.Column<int>(type: "int", nullable: false),
                    ReorderLevel = table.Column<int>(type: "int", nullable: false),
                    QuantityOnHand = table.Column<int>(type: "int", nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MinimumStockLevel = table.Column<int>(type: "int", nullable: false),
                    StorageLocation = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Consumables", x => x.ConsumableID);
                    table.ForeignKey(
                        name: "FK_Consumables_Suppliers_SupplierID",
                        column: x => x.SupplierID,
                        principalTable: "Suppliers",
                        principalColumn: "SupplierID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TestTypes",
                columns: table => new
                {
                    TestID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TestName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CategoryID = table.Column<int>(type: "int", nullable: false),
                    SampleTypeID = table.Column<int>(type: "int", nullable: false),
                    UnitOfMeasurement = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NormalRangeMin = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    NormalRangeMax = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    TurnaroundTimeMinutes = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestTypes", x => x.TestID);
                    table.ForeignKey(
                        name: "FK_TestTypes_SampleTypes_SampleTypeID",
                        column: x => x.SampleTypeID,
                        principalTable: "SampleTypes",
                        principalColumn: "SampleTypeID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TestTypes_TestCategories_CategoryID",
                        column: x => x.CategoryID,
                        principalTable: "TestCategories",
                        principalColumn: "CategoryID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Doctors",
                columns: table => new
                {
                    DoctorID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    HPCSANumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PracticeName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PracticeAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Specialization = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RegisteredDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doctors", x => x.DoctorID);
                    table.ForeignKey(
                        name: "FK_Doctors_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LabTechnicians",
                columns: table => new
                {
                    TechnicianID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    EmployeeNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SAIDNumber = table.Column<string>(type: "nvarchar(13)", maxLength: 13, nullable: false),
                    Specialization = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Qualification = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    YearsOfExperience = table.Column<int>(type: "int", nullable: false),
                    HireDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabTechnicians", x => x.TechnicianID);
                    table.ForeignKey(
                        name: "FK_LabTechnicians_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Patients",
                columns: table => new
                {
                    PatientID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SAIDNumber = table.Column<string>(type: "nvarchar(13)", maxLength: 13, nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Cellphone = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    HomeAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Patients", x => x.PatientID);
                    table.ForeignKey(
                        name: "FK_Patients_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrders",
                columns: table => new
                {
                    OrderID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SupplierID = table.Column<int>(type: "int", nullable: false),
                    OrderDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    ExpectedDeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CancellationReason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateCompleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateCancelled = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrders", x => x.OrderID);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Suppliers_SupplierID",
                        column: x => x.SupplierID,
                        principalTable: "Suppliers",
                        principalColumn: "SupplierID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TestConsumables",
                columns: table => new
                {
                    TestConsumableID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TestID = table.Column<int>(type: "int", nullable: false),
                    ConsumableID = table.Column<int>(type: "int", nullable: false),
                    QuantityRequired = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestConsumables", x => x.TestConsumableID);
                    table.ForeignKey(
                        name: "FK_TestConsumables_Consumables_ConsumableID",
                        column: x => x.ConsumableID,
                        principalTable: "Consumables",
                        principalColumn: "ConsumableID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TestConsumables_TestTypes_TestID",
                        column: x => x.TestID,
                        principalTable: "TestTypes",
                        principalColumn: "TestID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TechnicianTestAssignments",
                columns: table => new
                {
                    AssignmentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TechnicianID = table.Column<int>(type: "int", nullable: false),
                    TestID = table.Column<int>(type: "int", nullable: false),
                    AssignedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnicianTestAssignments", x => x.AssignmentID);
                    table.ForeignKey(
                        name: "FK_TechnicianTestAssignments_LabTechnicians_TechnicianID",
                        column: x => x.TechnicianID,
                        principalTable: "LabTechnicians",
                        principalColumn: "TechnicianID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TechnicianTestAssignments_TestTypes_TestID",
                        column: x => x.TestID,
                        principalTable: "TestTypes",
                        principalColumn: "TestID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PatientAllergies",
                columns: table => new
                {
                    PatientAllergyID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientID = table.Column<int>(type: "int", nullable: false),
                    AllergyID = table.Column<int>(type: "int", nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientAllergies", x => x.PatientAllergyID);
                    table.ForeignKey(
                        name: "FK_PatientAllergies_Allergy_AllergyID",
                        column: x => x.AllergyID,
                        principalTable: "Allergy",
                        principalColumn: "AllergyID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PatientAllergies_Patients_PatientID",
                        column: x => x.PatientID,
                        principalTable: "Patients",
                        principalColumn: "PatientID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PatientConditions",
                columns: table => new
                {
                    PatientConditionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientID = table.Column<int>(type: "int", nullable: false),
                    ConditionID = table.Column<int>(type: "int", nullable: false),
                    DiagnosedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientConditions", x => x.PatientConditionID);
                    table.ForeignKey(
                        name: "FK_PatientConditions_MedicalCondition_ConditionID",
                        column: x => x.ConditionID,
                        principalTable: "MedicalCondition",
                        principalColumn: "ConditionID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PatientConditions_Patients_PatientID",
                        column: x => x.PatientID,
                        principalTable: "Patients",
                        principalColumn: "PatientID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PatientConsentManagements",
                columns: table => new
                {
                    ConsentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientID = table.Column<int>(type: "int", nullable: false),
                    DoctorID = table.Column<int>(type: "int", nullable: false),
                    GrantedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientConsentManagements", x => x.ConsentID);
                    table.ForeignKey(
                        name: "FK_PatientConsentManagements_Doctors_DoctorID",
                        column: x => x.DoctorID,
                        principalTable: "Doctors",
                        principalColumn: "DoctorID");
                    table.ForeignKey(
                        name: "FK_PatientConsentManagements_Patients_PatientID",
                        column: x => x.PatientID,
                        principalTable: "Patients",
                        principalColumn: "PatientID");
                });

            migrationBuilder.CreateTable(
                name: "PatientMedications",
                columns: table => new
                {
                    PatientMedicationID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientID = table.Column<int>(type: "int", nullable: false),
                    MedicationID = table.Column<int>(type: "int", nullable: false),
                    Dosage = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Frequency = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientMedications", x => x.PatientMedicationID);
                    table.ForeignKey(
                        name: "FK_PatientMedications_Medication_MedicationID",
                        column: x => x.MedicationID,
                        principalTable: "Medication",
                        principalColumn: "MedicationID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PatientMedications_Patients_PatientID",
                        column: x => x.PatientID,
                        principalTable: "Patients",
                        principalColumn: "PatientID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TestRequests",
                columns: table => new
                {
                    RequestID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DoctorID = table.Column<int>(type: "int", nullable: false),
                    PatientID = table.Column<int>(type: "int", nullable: false),
                    RequestDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    Urgency = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ClinicalNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReleasedToPatientDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestRequests", x => x.RequestID);
                    table.ForeignKey(
                        name: "FK_TestRequests_Doctors_DoctorID",
                        column: x => x.DoctorID,
                        principalTable: "Doctors",
                        principalColumn: "DoctorID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TestRequests_Patients_PatientID",
                        column: x => x.PatientID,
                        principalTable: "Patients",
                        principalColumn: "PatientID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrderItems",
                columns: table => new
                {
                    OrderItemID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderID = table.Column<int>(type: "int", nullable: false),
                    ConsumableID = table.Column<int>(type: "int", nullable: false),
                    QuantityOrdered = table.Column<int>(type: "int", nullable: false),
                    QuantityReceived = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CancellationReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateReceived = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateCancelled = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItems", x => x.OrderItemID);
                    table.ForeignKey(
                        name: "FK_OrderItems_Consumables_ConsumableID",
                        column: x => x.ConsumableID,
                        principalTable: "Consumables",
                        principalColumn: "ConsumableID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderItems_PurchaseOrders_OrderID",
                        column: x => x.OrderID,
                        principalTable: "PurchaseOrders",
                        principalColumn: "OrderID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockAdjustmentLogs",
                columns: table => new
                {
                    AdjustmentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConsumableID = table.Column<int>(type: "int", nullable: false),
                    PreviousQuantity = table.Column<int>(type: "int", nullable: false),
                    NewQuantity = table.Column<int>(type: "int", nullable: false),
                    AdjustmentAmount = table.Column<int>(type: "int", nullable: false),
                    AdjustmentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AdjustedBy = table.Column<int>(type: "int", nullable: false),
                    AdjustmentDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    RelatedOrderID = table.Column<int>(type: "int", nullable: true),
                    RelatedTestID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockAdjustmentLogs", x => x.AdjustmentID);
                    table.ForeignKey(
                        name: "FK_StockAdjustmentLogs_Consumables_ConsumableID",
                        column: x => x.ConsumableID,
                        principalTable: "Consumables",
                        principalColumn: "ConsumableID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockAdjustmentLogs_PurchaseOrders_RelatedOrderID",
                        column: x => x.RelatedOrderID,
                        principalTable: "PurchaseOrders",
                        principalColumn: "OrderID");
                    table.ForeignKey(
                        name: "FK_StockAdjustmentLogs_Users_AdjustedBy",
                        column: x => x.AdjustedBy,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConsentTestRequest",
                columns: table => new
                {
                    ConsentTestRequestID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConsentID = table.Column<int>(type: "int", nullable: false),
                    TestRequestID = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsentTestRequest", x => x.ConsentTestRequestID);
                    table.ForeignKey(
                        name: "FK_ConsentTestRequest_PatientConsentManagements_ConsentID",
                        column: x => x.ConsentID,
                        principalTable: "PatientConsentManagements",
                        principalColumn: "ConsentID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConsentTestRequestAccesses",
                columns: table => new
                {
                    ConsentTestRequestAccessID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConsentID = table.Column<int>(type: "int", nullable: false),
                    RequestID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsentTestRequestAccesses", x => x.ConsentTestRequestAccessID);
                    table.ForeignKey(
                        name: "FK_ConsentTestRequestAccesses_PatientConsentManagements_ConsentID",
                        column: x => x.ConsentID,
                        principalTable: "PatientConsentManagements",
                        principalColumn: "ConsentID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConsentTestRequestAccesses_TestRequests_RequestID",
                        column: x => x.RequestID,
                        principalTable: "TestRequests",
                        principalColumn: "RequestID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RequestTestTypes",
                columns: table => new
                {
                    RequestTestID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestID = table.Column<int>(type: "int", nullable: false),
                    TestTypeID = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Submitted"),
                    AssignedTechnicianID = table.Column<int>(type: "int", nullable: true),
                    StartDateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedDateTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestTestTypes", x => x.RequestTestID);
                    table.ForeignKey(
                        name: "FK_RequestTestTypes_TestRequests_RequestID",
                        column: x => x.RequestID,
                        principalTable: "TestRequests",
                        principalColumn: "RequestID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RequestTestTypes_TestTypes_TestTypeID",
                        column: x => x.TestTypeID,
                        principalTable: "TestTypes",
                        principalColumn: "TestID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Samples",
                columns: table => new
                {
                    SampleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestID = table.Column<int>(type: "int", nullable: false),
                    SampleTypeID = table.Column<int>(type: "int", nullable: false),
                    Barcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DateReceived = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReceivedByTechnicianID = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Samples", x => x.SampleID);
                    table.ForeignKey(
                        name: "FK_Samples_SampleTypes_SampleTypeID",
                        column: x => x.SampleTypeID,
                        principalTable: "SampleTypes",
                        principalColumn: "SampleTypeID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Samples_TestRequests_RequestID",
                        column: x => x.RequestID,
                        principalTable: "TestRequests",
                        principalColumn: "RequestID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TestResults",
                columns: table => new
                {
                    ResultID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestTestID = table.Column<int>(type: "int", nullable: false),
                    ResultValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NumericValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsAbnormal = table.Column<bool>(type: "bit", nullable: false),
                    ResultDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestResults", x => x.ResultID);
                    table.ForeignKey(
                        name: "FK_TestResults_RequestTestTypes_RequestTestID",
                        column: x => x.RequestTestID,
                        principalTable: "RequestTestTypes",
                        principalColumn: "RequestTestID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TestVerifications",
                columns: table => new
                {
                    VerificationID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ResultID = table.Column<int>(type: "int", nullable: false),
                    VerifiedByTechnicianID = table.Column<int>(type: "int", nullable: false),
                    VerificationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ReviewNotes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestVerifications", x => x.VerificationID);
                    table.ForeignKey(
                        name: "FK_TestVerifications_TestResults_ResultID",
                        column: x => x.ResultID,
                        principalTable: "TestResults",
                        principalColumn: "ResultID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Allergy_CategoryID",
                table: "Allergy",
                column: "CategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_ConsentTestRequest_ConsentID",
                table: "ConsentTestRequest",
                column: "ConsentID");

            migrationBuilder.CreateIndex(
                name: "IX_ConsentTestRequestAccesses_ConsentID",
                table: "ConsentTestRequestAccesses",
                column: "ConsentID");

            migrationBuilder.CreateIndex(
                name: "IX_ConsentTestRequestAccesses_RequestID",
                table: "ConsentTestRequestAccesses",
                column: "RequestID");

            migrationBuilder.CreateIndex(
                name: "IX_Consumable_Name_Unique",
                table: "Consumables",
                column: "ConsumableName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Consumables_SupplierID",
                table: "Consumables",
                column: "SupplierID");

            migrationBuilder.CreateIndex(
                name: "IX_Doctor_HPCSANumber_Unique",
                table: "Doctors",
                column: "HPCSANumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Doctors_UserID",
                table: "Doctors",
                column: "UserID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LabTechnician_EmployeeNumber_Unique",
                table: "LabTechnicians",
                column: "EmployeeNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LabTechnician_SAIDNumber_Unique",
                table: "LabTechnicians",
                column: "SAIDNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LabTechnicians_UserID",
                table: "LabTechnicians",
                column: "UserID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MedicalCondition_CategoryID",
                table: "MedicalCondition",
                column: "CategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_Medication_CategoryID",
                table: "Medication",
                column: "CategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_ConsumableID",
                table: "OrderItems",
                column: "ConsumableID");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderID",
                table: "OrderItems",
                column: "OrderID");

            migrationBuilder.CreateIndex(
                name: "IX_PatientAllergies_AllergyID",
                table: "PatientAllergies",
                column: "AllergyID");

            migrationBuilder.CreateIndex(
                name: "IX_PatientAllergies_PatientID",
                table: "PatientAllergies",
                column: "PatientID");

            migrationBuilder.CreateIndex(
                name: "IX_PatientConditions_ConditionID",
                table: "PatientConditions",
                column: "ConditionID");

            migrationBuilder.CreateIndex(
                name: "IX_PatientConditions_PatientID",
                table: "PatientConditions",
                column: "PatientID");

            migrationBuilder.CreateIndex(
                name: "IX_PatientConsentManagements_DoctorID",
                table: "PatientConsentManagements",
                column: "DoctorID");

            migrationBuilder.CreateIndex(
                name: "IX_PatientConsentManagements_PatientID",
                table: "PatientConsentManagements",
                column: "PatientID");

            migrationBuilder.CreateIndex(
                name: "IX_PatientMedications_MedicationID",
                table: "PatientMedications",
                column: "MedicationID");

            migrationBuilder.CreateIndex(
                name: "IX_PatientMedications_PatientID",
                table: "PatientMedications",
                column: "PatientID");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_UserID",
                table: "Patients",
                column: "UserID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrder_Number_Unique",
                table: "PurchaseOrders",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_CreatedBy",
                table: "PurchaseOrders",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_SupplierID",
                table: "PurchaseOrders",
                column: "SupplierID");

            migrationBuilder.CreateIndex(
                name: "IX_RequestTestTypes_RequestID",
                table: "RequestTestTypes",
                column: "RequestID");

            migrationBuilder.CreateIndex(
                name: "IX_RequestTestTypes_TestTypeID",
                table: "RequestTestTypes",
                column: "TestTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_Samples_Barcode",
                table: "Samples",
                column: "Barcode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Samples_RequestID",
                table: "Samples",
                column: "RequestID");

            migrationBuilder.CreateIndex(
                name: "IX_Samples_SampleTypeID",
                table: "Samples",
                column: "SampleTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_SampleType_Name_Unique",
                table: "SampleTypes",
                column: "SampleTypeName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustmentLogs_AdjustedBy",
                table: "StockAdjustmentLogs",
                column: "AdjustedBy");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustmentLogs_ConsumableID",
                table: "StockAdjustmentLogs",
                column: "ConsumableID");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustmentLogs_RelatedOrderID",
                table: "StockAdjustmentLogs",
                column: "RelatedOrderID");

            migrationBuilder.CreateIndex(
                name: "IX_Supplier_Name_Unique",
                table: "Suppliers",
                column: "SupplierName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TechnicianTestAssignment_Unique",
                table: "TechnicianTestAssignments",
                columns: new[] { "TechnicianID", "TestID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TechnicianTestAssignments_TestID",
                table: "TechnicianTestAssignments",
                column: "TestID");

            migrationBuilder.CreateIndex(
                name: "IX_TestCategory_Name_Unique",
                table: "TestCategories",
                column: "CategoryName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TestConsumable_Unique",
                table: "TestConsumables",
                columns: new[] { "TestID", "ConsumableID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TestConsumables_ConsumableID",
                table: "TestConsumables",
                column: "ConsumableID");

            migrationBuilder.CreateIndex(
                name: "IX_TestRequests_DoctorID",
                table: "TestRequests",
                column: "DoctorID");

            migrationBuilder.CreateIndex(
                name: "IX_TestRequests_PatientID",
                table: "TestRequests",
                column: "PatientID");

            migrationBuilder.CreateIndex(
                name: "IX_TestRequests_RequestNumber",
                table: "TestRequests",
                column: "RequestNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TestResults_RequestTestID",
                table: "TestResults",
                column: "RequestTestID");

            migrationBuilder.CreateIndex(
                name: "IX_TestType_Name_Unique",
                table: "TestTypes",
                column: "TestName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TestTypes_CategoryID",
                table: "TestTypes",
                column: "CategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_TestTypes_SampleTypeID",
                table: "TestTypes",
                column: "SampleTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_TestVerifications_ResultID",
                table: "TestVerifications",
                column: "ResultID");

            migrationBuilder.CreateIndex(
                name: "IX_User_Email_Unique",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "ConsentTestRequest");

            migrationBuilder.DropTable(
                name: "ConsentTestRequestAccesses");

            migrationBuilder.DropTable(
                name: "OrderItems");

            migrationBuilder.DropTable(
                name: "PatientAllergies");

            migrationBuilder.DropTable(
                name: "PatientConditions");

            migrationBuilder.DropTable(
                name: "PatientMedications");

            migrationBuilder.DropTable(
                name: "Samples");

            migrationBuilder.DropTable(
                name: "StockAdjustmentLogs");

            migrationBuilder.DropTable(
                name: "TechnicianTestAssignments");

            migrationBuilder.DropTable(
                name: "TestConsumables");

            migrationBuilder.DropTable(
                name: "TestVerifications");

            migrationBuilder.DropTable(
                name: "PatientConsentManagements");

            migrationBuilder.DropTable(
                name: "Allergy");

            migrationBuilder.DropTable(
                name: "MedicalCondition");

            migrationBuilder.DropTable(
                name: "Medication");

            migrationBuilder.DropTable(
                name: "PurchaseOrders");

            migrationBuilder.DropTable(
                name: "LabTechnicians");

            migrationBuilder.DropTable(
                name: "Consumables");

            migrationBuilder.DropTable(
                name: "TestResults");

            migrationBuilder.DropTable(
                name: "LookupCategory");

            migrationBuilder.DropTable(
                name: "Suppliers");

            migrationBuilder.DropTable(
                name: "RequestTestTypes");

            migrationBuilder.DropTable(
                name: "TestRequests");

            migrationBuilder.DropTable(
                name: "TestTypes");

            migrationBuilder.DropTable(
                name: "Doctors");

            migrationBuilder.DropTable(
                name: "Patients");

            migrationBuilder.DropTable(
                name: "SampleTypes");

            migrationBuilder.DropTable(
                name: "TestCategories");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
