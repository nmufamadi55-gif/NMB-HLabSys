using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace NMB_HLabSys.Controllers
{
    public class PatientController : Controller
    {
        // GET: Patient/Dashboard
        public IActionResult Dashboard()
        {
            ViewBag.PatientName = "Thabo Mbeki";
            ViewBag.PatientID = "8001015009087";
            ViewBag.PatientSurname = "Mbeki";
            return View();
        }

        // GET: Patient/Profile
        public IActionResult Profile()
        {
            ViewBag.PatientName = "Thabo Mbeki";
            ViewBag.PatientID = "8001015009087";
            ViewBag.PatientSurname = "Mbeki";
            return View();
        }

        // GET: Patient/EditProfile
        public IActionResult EditProfile()
        {
            ViewBag.PatientName = "Thabo Mbeki";
            ViewBag.PatientID = "8001015009087";
            return View();
        }

        [HttpPost]
        public IActionResult EditProfile(string firstName, string lastName, string cellphone, string address)
        {
            TempData["SuccessMessage"] = "Profile updated successfully!";
            return RedirectToAction("Profile");
        }

        // GET: Patient/MedicalHistory
        public IActionResult MedicalHistory()
        {
            ViewBag.PatientName = "Thabo Mbeki";
            ViewBag.PatientID = "8001015009087";
            ViewBag.PatientSurname = "Mbeki";
            return View();
        }

        // POST: Add Condition (AJAX)
        [HttpPost]
        public IActionResult AddCondition(int conditionId, string diagnosedDate, string notes)
        {
            return Json(new { success = true, message = "Condition added successfully" });
        }

        // POST: Remove Condition (AJAX)
        [HttpPost]
        public IActionResult RemoveCondition(int patientConditionId)
        {
            return Json(new { success = true, message = "Condition removed successfully" });
        }

        // POST: Add Allergy (AJAX)
        [HttpPost]
        public IActionResult AddAllergy(int allergyId, string severity, string notes)
        {
            return Json(new { success = true, message = "Allergy added successfully" });
        }

        // POST: Remove Allergy (AJAX)
        [HttpPost]
        public IActionResult RemoveAllergy(int patientAllergyId)
        {
            return Json(new { success = true, message = "Allergy removed successfully" });
        }

        // POST: Add Medication (AJAX)
        [HttpPost]
        public IActionResult AddMedication(int medicationId, string dosage, string frequency, string startDate, string endDate, string notes)
        {
            return Json(new { success = true, message = "Medication added successfully" });
        }

        // POST: Remove Medication (AJAX)
        [HttpPost]
        public IActionResult RemoveMedication(int patientMedicationId)
        {
            return Json(new { success = true, message = "Medication removed successfully" });
        }

        // GET: Patient/Consent
        public IActionResult Consent()
        {
            ViewBag.PatientName = "Thabo Mbeki";
            ViewBag.PatientID = "8001015009087";
            ViewBag.PatientSurname = "Mbeki";
            return View();
        }

        // POST: Grant Consent (AJAX)
        [HttpPost]
        public IActionResult GrantConsent(int doctorId, string notes)
        {
            return Json(new { success = true, message = "Consent granted successfully" });
        }

        // POST: Revoke Consent (AJAX)
        [HttpPost]
        public IActionResult RevokeConsent(int consentId)
        {
            return Json(new { success = true, message = "Consent revoked successfully" });
        }

        // GET: Patient/Trends
        public IActionResult Trends(string testType = null, int months = 6)
        {
            ViewBag.PatientName = "Thabo Mbeki";
            ViewBag.PatientID = "8001015009087";
            ViewBag.SelectedTest = testType ?? "Cholesterol";
            ViewBag.Months = months;
            return View();
        }

        // GET: Patient/DownloadReport
        public IActionResult DownloadReport()
        {
            TempData["InfoMessage"] = "PDF report will be available in the full implementation.";
            return RedirectToAction("Dashboard");
        }
    }
}