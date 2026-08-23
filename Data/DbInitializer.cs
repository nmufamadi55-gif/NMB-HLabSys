//Data / DbInitializer.cs
//using Microsoft.EntityFrameworkCore;
//using NMB_HLabSys.Models;
//using NMB_HLabSys.Services;

//namespace NMB_HLabSys.Data
//{
//    public static class DbInitializer
//    {
//        public static async Task InitializeAsync(ApplicationDbContext context, IPasswordHasher passwordHasher)
//        {
//            await context.Database.EnsureCreatedAsync();

//            Seed default manager if no users exist
//            if (!await context.Users.AnyAsync())
//            {
//                Create Manager
//                var manager = new User
//                {
//                    Name = "Amanda",
//                    Surname = "Adams",
//                    Email = "manager@nmblab.com",
//                    ContactNumber = "0825551234",
//                    UserRole = "Manager",
//                    IsActive = true,
//                    IsPasswordChanged = false,
//                    CreatedDate = DateTime.Now,
//                    PasswordHash = passwordHasher.HashPassword("Admin@1234")
//                };
//                context.Users.Add(manager);

//                Create Doctor
//                var doctor = new User
//                {
//                    Name = "Sarah",
//                    Surname = "Johnson",
//                    Email = "doctor@nmblab.com",
//                    ContactNumber = "0825555678",
//                    UserRole = "Doctor",
//                    IsActive = true,
//                    IsPasswordChanged = false,
//                    CreatedDate = DateTime.Now,
//                    PasswordHash = passwordHasher.HashPassword("Doctor@1234")
//                };
//                context.Users.Add(doctor);

//                Create Technician
//                var technician = new User
//                {
//                    Name = "Michael",
//                    Surname = "Peters",
//                    Email = "technician@nmblab.com",
//                    ContactNumber = "0825559012",
//                    UserRole = "Technician",
//                    IsActive = true,
//                    IsPasswordChanged = false,
//                    CreatedDate = DateTime.Now,
//                    PasswordHash = passwordHasher.HashPassword("Tech@1234")
//                };
//                context.Users.Add(technician);

//                Create Patient
//                var patient = new User
//                {
//                    FirstName = "Thabo",
//                    LastName = "Mbeki",
//                    Email = "patient@nmblab.com",
//                    Cellphone = "0825553456",
//                    UserRole = "Patient",
//                    IsActive = true,
//                    IsPasswordChanged = false,
//                    CreatedDate = DateTime.Now,
//                    PasswordHash = passwordHasher.HashPassword("Patient@1234")
//                };
//                context.Users.Add(patient);

//                Create Admin
//                var admin = new User
//                {
//                    Name = "Admin",
//                    Surname = "User",
//                    Email = "admin@nmblab.com",
//                    ContactNumber = "0825557890",
//                    UserRole = "Admin",
//                    IsActive = true,
//                    IsPasswordChanged = false,
//                    CreatedDate = DateTime.Now,
//                    PasswordHash = passwordHasher.HashPassword("Admin@1234")
//                };
//                context.Users.Add(admin);

//                await context.SaveChangesAsync();

//                Add Doctor details
//               var doctorUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "doctor@nmblab.com");
//                if (doctorUser != null)
//                {
//                    var doctorDetails = new Doctor
//                    {
//                        UserID = doctorUser.UserID,
//                        HPCSANumber = "HPCSA001",
//                        PracticeName = "Johnson Medical Practice",
//                        PracticeAddress = "123 Medical Street, Port Elizabeth",
//                        Specialization = "Haematology",
//                        IsVerified = true,
//                        RegisteredDate = DateTime.Now
//                    };
//                    context.Doctors.Add(doctorDetails);
//                }

//                Add Technician details
//               var techUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "technician@nmblab.com");
//                if (techUser != null)
//                {
//                    var techDetails = new LabTechnician
//                    {
//                        UserID = techUser.UserID,
//                        EmployeeNumber = "EMP001",
//                        SAIDNumber = "8001015800088",
//                        Specialization = "Haematology",
//                        Qualification = "BSc Medical Laboratory Science",
//                        YearsOfExperience = 5,
//                        HireDate = DateTime.Now.AddYears(-3),
//                        IsActive = true
//                    };
//                    context.LabTechnicians.Add(techDetails);
//                }

//                await context.SaveChangesAsync();
//            }
//        }
//    }
//}