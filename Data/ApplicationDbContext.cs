using Microsoft.EntityFrameworkCore;
using NMB_HLabSys.Models;
using NMB_HLabSys.Models.ViewModels;

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
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<LabTechnician> LabTechnicians { get; set; }
        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<TestRequest> RequestedTests { get; set; }

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

        // Doctor Management
        public DbSet<TestRequest> TestRequests { get; set; }
        public DbSet<RequestTestType> RequestTestTypes { get; set; }
        public DbSet<TestResult> TestResults { get; set; }
        public DbSet<TestVerification> TestVerifications { get; set; }
        public DbSet<Sample> Samples { get; set; }
        public DbSet<ConsentTestRequestAccess> ConsentTestRequestAccesses { get; set; }

        // Patient management
        public DbSet<PatientCondition> PatientConditions { get; set; }
        public DbSet<PatientAllergy> PatientAllergies { get; set; }
        public DbSet<PatientMedication> PatientMedications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =========================
            // COMPOSITE UNIQUE KEYS
            // =========================
            modelBuilder.Entity<TechnicianTestAssignment>()
                .HasIndex(t => new { t.TechnicianID, t.TestID })
                .IsUnique()
                .HasDatabaseName("IX_TechnicianTestAssignment_Unique");

            modelBuilder.Entity<TestConsumable>()
                .HasIndex(t => new { t.TestID, t.ConsumableID })
                .IsUnique()
                .HasDatabaseName("IX_TestConsumable_Unique");

            // =========================
            // UNIQUE CONSTRAINTS
            // =========================
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique()
                .HasDatabaseName("IX_User_Email_Unique");

            // COMMENTED OUT - User model doesn't have AspNetUserID
            // modelBuilder.Entity<User>()
            //     .HasIndex(u => u.AspNetUserID)
            //     .IsUnique()
            //     .HasDatabaseName("IX_User_AspNetUserId_Unique");

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

            // =========================
            // RELATIONSHIPS WITH CASCADE BEHAVIOR
            // =========================

            // -------- ORDER MANAGEMENT --------
            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(p => p.OrderItems)
                .HasForeignKey(oi => oi.OrderID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Consumable)
                .WithMany(c => c.OrderItems)
                .HasForeignKey(oi => oi.ConsumableID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PurchaseOrder>()
                .HasOne(p => p.Supplier)
                .WithMany(s => s.PurchaseOrders)
                .HasForeignKey(p => p.SupplierID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PurchaseOrder>()
                .HasOne(p => p.CreatedByUser)
                .WithMany()
                .HasForeignKey(p => p.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // -------- TEST MANAGEMENT --------
            modelBuilder.Entity<TestType>()
                .HasOne(t => t.Category)
                .WithMany(c => c.TestTypes)
                .HasForeignKey(t => t.CategoryID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TestType>()
                .HasOne(t => t.SampleType)
                .WithMany(s => s.TestTypes)
                .HasForeignKey(t => t.SampleTypeID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TechnicianTestAssignment>()
                .HasOne(tta => tta.TestType)
                .WithMany(t => t.TechnicianTestAssignments)
                .HasForeignKey(tta => tta.TestID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TechnicianTestAssignment>()
                .HasOne(tta => tta.Technician)
                .WithMany(t => t.TestAssignments)
                .HasForeignKey(tta => tta.TechnicianID)
                .OnDelete(DeleteBehavior.Cascade);

            // -------- INVENTORY MANAGEMENT --------
            modelBuilder.Entity<Consumable>()
                .HasOne(c => c.Supplier)
                .WithMany(s => s.Consumables)
                .HasForeignKey(c => c.SupplierID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TestConsumable>()
                .HasOne(tc => tc.TestType)
                .WithMany(t => t.TestConsumables)
                .HasForeignKey(tc => tc.TestID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TestConsumable>()
                .HasOne(tc => tc.Consumable)
                .WithMany(c => c.TestConsumables)
                .HasForeignKey(tc => tc.ConsumableID)
                .OnDelete(DeleteBehavior.Restrict);

            // =========================
            // USER RELATIONSHIPS (COMMENTED OUT - User model doesn't have these navigation properties)
            // =========================

            //modelBuilder.Entity<LabTechnician>()
            //    .HasOne(lt => lt.User)
            //    .WithOne(u => u.LabTechnician)
            //    .HasForeignKey<LabTechnician>(lt => lt.UserID)
            //    .OnDelete(DeleteBehavior.Cascade);

            //modelBuilder.Entity<Doctor>()
            //    .HasOne(d => d.User)
            //    .WithOne(u => u.Doctor)
            //    .HasForeignKey<Doctor>(d => d.UserID)
            //    .OnDelete(DeleteBehavior.Cascade);

            //modelBuilder.Entity<Patient>()
            //    .HasOne(p => p.User)
            //    .WithOne(u => u.Patient)
            //    .HasForeignKey<Patient>(p => p.UserID)
            //    .OnDelete(DeleteBehavior.Cascade);

            // =========================
            // DECIMAL PRECISION
            // =========================
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

            // =========================
            // DEFAULT VALUES
            // =========================
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

            // =========================
            // BOOLEAN DEFAULTS
            // =========================
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

            // =========================
            // DOCTOR RELATED
            // =========================
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
