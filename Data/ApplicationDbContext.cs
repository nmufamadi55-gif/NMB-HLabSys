using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NMB_HLabSys.Models;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace NMB_HLabSys.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
           : base(options)
        {
        }

        // Core tables
        public DbSet<User> Users { get; set; }
        public DbSet<LabTechnician> LabTechnicians { get; set; }
        public DbSet<Doctor> Doctors { get; set; }

        // Test management
        public DbSet<TestCategory> TestCategories { get; set; }
        public DbSet<TestType> TestTypes { get; set; }
        public DbSet<SampleType> SampleTypes { get; set; }
        public DbSet<TechnicianTestAssignment> TechnicianTestAssignments { get; set; }

        // Inventory management
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Consumable> Consumables { get; set; }
        public DbSet<TestConsumable> TestConsumables { get; set; }

        // Order management
        public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<StockAdjustmentLog> StockAdjustmentLogs { get; set; }

        //Doctor Management
        public DbSet<Patient> Patients { get; set; }
        public DbSet<TestRequest> TestRequests { get; set; }
        public DbSet<RequestTestType> RequestTestTypes { get; set; }
        public DbSet<TestResult> TestResults { get; set; }
        public DbSet<TestVerification> TestVerifications { get; set; }
        public DbSet<Sample> Samples { get; set; }
        public DbSet<ConsentTestRequestAccess> ConsentTestRequestAccesses { get; set; }

        //Patient management
        public DbSet<PatientCondition> PatientConditions { get; set; }
        public DbSet<PatientAllergy> PatientAllergies { get; set; }
        public DbSet<PatientMedication> PatientMedications { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Composite unique keys
            modelBuilder.Entity<TechnicianTestAssignment>()
                .HasIndex(t => new { t.TechnicianID, t.TestID })
                .IsUnique()
                .HasDatabaseName("IX_TechnicianTestAssignment_Unique");

            modelBuilder.Entity<TestConsumable>()
                .HasIndex(t => new { t.TestID, t.ConsumableID })
                .IsUnique()
                .HasDatabaseName("IX_TestConsumable_Unique");


            // Unique constraints
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique()
                .HasDatabaseName("IX_User_Email_Unique");

            modelBuilder.Entity<User>()
                .HasIndex(u => u.AspNetUserID)
                .IsUnique()
                .HasDatabaseName("IX_User_AspNetUserId_Unique");

            modelBuilder.Entity<LabTechnician>()
                .HasIndex(l => l.EmployeeNumber)
                .IsUnique()
                .HasDatabaseName("IX_LabTechnician_EmployeeNumber_Unique");

            modelBuilder.Entity<LabTechnician>()
                .HasIndex(l => l.SAIDNumber)
                .IsUnique()
                .HasDatabaseName("IX_LabTechnician_SAIDNumber_Unique");

            modelBuilder.Entity<Doctor>()
                .HasIndex(d => d.HPCSANumber)
                .IsUnique()
                .HasDatabaseName("IX_Doctor_HPCSANumber_Unique");

            modelBuilder.Entity<Supplier>()
                .HasIndex(s => s.SupplierName)
                .IsUnique()
                .HasDatabaseName("IX_Supplier_Name_Unique");

            modelBuilder.Entity<Consumable>()
                .HasIndex(c => c.ConsumableName)
                .IsUnique()
                .HasDatabaseName("IX_Consumable_Name_Unique");

            modelBuilder.Entity<TestCategory>()
                .HasIndex(t => t.CategoryName)
                .IsUnique()
                .HasDatabaseName("IX_TestCategory_Name_Unique");

            modelBuilder.Entity<TestType>()
                .HasIndex(t => t.TestName)
                .IsUnique()
                .HasDatabaseName("IX_TestType_Name_Unique");

            modelBuilder.Entity<SampleType>()
                .HasIndex(s => s.SampleTypeName)
                .IsUnique()
                .HasDatabaseName("IX_SampleType_Name_Unique");

            modelBuilder.Entity<PurchaseOrder>()
                .HasIndex(p => p.OrderNumber)
                .IsUnique()
                .HasDatabaseName("IX_PurchaseOrder_Number_Unique");

            // Configure decimal precision
            modelBuilder.Entity<TestType>()
                .Property(t => t.NormalRangeMin)
                .HasPrecision(18, 4);

            modelBuilder.Entity<TestType>()
                .Property(t => t.NormalRangeMax)
                .HasPrecision(18, 4);

            modelBuilder.Entity<Consumable>()
                .Property(c => c.UnitPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<PurchaseOrder>()
                .Property(p => p.TotalAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<OrderItem>()
                .Property(o => o.UnitPrice)
                .HasPrecision(18, 2);


            modelBuilder.Entity<TestCategory>()
                .Property(t => t.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<TestType>()
                .Property(t => t.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<Supplier>()
                .Property(s => s.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<Consumable>()
                .Property(c => c.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<PurchaseOrder>()
                .Property(p => p.OrderDate)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<StockAdjustmentLog>()
                .Property(s => s.AdjustmentDate)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<TechnicianTestAssignment>()
                .Property(t => t.AssignedDate)
                .HasDefaultValueSql("GETDATE()");

            // Set default boolean values
            modelBuilder.Entity<User>()
                .Property(u => u.IsActive)
                .HasDefaultValue(true);

            modelBuilder.Entity<TestCategory>()
                .Property(t => t.IsActive)
                .HasDefaultValue(true);

            modelBuilder.Entity<TestType>()
                .Property(t => t.IsActive)
                .HasDefaultValue(true);

            modelBuilder.Entity<SampleType>()
                .Property(s => s.IsActive)
                .HasDefaultValue(true);

            modelBuilder.Entity<Supplier>()
                .Property(s => s.IsActive)
                .HasDefaultValue(true);

            modelBuilder.Entity<Supplier>()
                .Property(s => s.IsPreferred)
                .HasDefaultValue(false);

            modelBuilder.Entity<Consumable>()
                .Property(c => c.IsActive)
                .HasDefaultValue(true);

            modelBuilder.Entity<LabTechnician>()
                .Property(l => l.IsActive)
                .HasDefaultValue(true);

            modelBuilder.Entity<Doctor>()
                .Property(d => d.IsVerified)
                .HasDefaultValue(false);

            modelBuilder.Entity<TechnicianTestAssignment>()
                .Property(t => t.IsActive)
                .HasDefaultValue(true);

            //Doctor related

            modelBuilder.Entity<TestRequest>()
           .HasIndex(t => t.RequestNumber)
           .IsUnique();

            modelBuilder.Entity<Sample>()
                .HasIndex(s => s.Barcode)
                .IsUnique();

            modelBuilder.Entity<RequestTestType>()
                .Property(r => r.Status)
                .HasDefaultValue("Submitted");

            modelBuilder.Entity<TestRequest>()
                .Property(t => t.RequestDate)
                .HasDefaultValueSql("GETDATE()");
        }
    }
}
