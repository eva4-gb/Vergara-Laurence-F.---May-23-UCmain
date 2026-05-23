using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace EventRegistrationSystem
{
    class Program
    {
        static string dataFolder = "Data";
        static string recordFile = Path.Combine(dataFolder, "events.txt");
        static string auditFile = Path.Combine(dataFolder, "auditlog.txt");

        static void Main(string[] args)
        {
            InitializeStorage();

            int choice = 0;

            while (choice != 8)
            {
                Console.Clear();
                Console.WriteLine("========================================");
                Console.WriteLine("     EVENT REGISTRATION SYSTEM");
                Console.WriteLine("========================================");
                Console.WriteLine("[1] Add Registration");
                Console.WriteLine("[2] View Registrations");
                Console.WriteLine("[3] Search Registration");
                Console.WriteLine("[4] Update Registration");
                Console.WriteLine("[5] Soft Delete Registration");
                Console.WriteLine("[6] Hard Delete Registration");
                Console.WriteLine("[7] Generate Report");
                Console.WriteLine("[8] Exit");
                Console.Write("Enter Choice: ");

                int.TryParse(Console.ReadLine(), out choice);

                try
                {
                    switch (choice)
                    {
                        case 1:
                            AddRecord();
                            break;

                        case 2:
                            ViewRecords();
                            break;

                        case 3:
                            SearchRecord();
                            break;

                        case 4:
                            UpdateRecord();
                            break;

                        case 5:
                            SoftDeleteRecord();
                            break;

                        case 6:
                            HardDeleteRecord();
                            break;

                        case 7:
                            GenerateReport();
                            break;

                        case 8:
                            Console.WriteLine("Exiting Program...");
                            break;

                        default:
                            Console.WriteLine("Invalid Choice.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    LogAudit("ERROR", ex.Message);
                    Console.WriteLine("An error occurred: " + ex.Message);
                }

                if (choice != 8)
                {
                    Console.WriteLine("\nPress any key to continue...");
                    Console.ReadKey();
                }
            }
        }

        static void InitializeStorage()
        {
            if (!Directory.Exists(dataFolder))
            {
                Directory.CreateDirectory(dataFolder);
            }

            if (!File.Exists(recordFile))
            {
                File.Create(recordFile).Close();
            }

            if (!File.Exists(auditFile))
            {
                File.Create(auditFile).Close();
            }

            LogAudit("SYSTEM", "Storage initialized.");
        }

        static void AddRecord()
        {
            Console.Clear();
            Console.WriteLine("===== ADD EVENT REGISTRATION =====");

            string recordId = GenerateRecordId();

            Console.Write("Participant Name: ");
            string participantName = Console.ReadLine();

            while (string.IsNullOrWhiteSpace(participantName))
            {
                Console.Write("Invalid. Enter Participant Name: ");
                participantName = Console.ReadLine();
            }

            Console.Write("Event Name: ");
            string eventName = Console.ReadLine();

            while (string.IsNullOrWhiteSpace(eventName))
            {
                Console.Write("Invalid. Enter Event Name: ");
                eventName = Console.ReadLine();
            }

            Console.Write("Email Address: ");
            string email = Console.ReadLine();

            while (!IsValidEmail(email))
            {
                Console.Write("Invalid Email. Enter Again: ");
                email = Console.ReadLine();
            }

            Console.Write("Contact Number: ");
            string contact = Console.ReadLine();

            while (string.IsNullOrWhiteSpace(contact))
            {
                Console.Write("Invalid. Enter Contact Number: ");
                contact = Console.ReadLine();
            }

            string createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string updatedAt = createdAt;
            bool isActive = true;

            string checksumData = recordId + participantName + eventName + email + contact;
            string checksum = ComputeChecksum(checksumData);

            string record =
                recordId + "|" +
                participantName + "|" +
                eventName + "|" +
                email + "|" +
                contact + "|" +
                createdAt + "|" +
                updatedAt + "|" +
                isActive + "|" +
                checksum;

            File.AppendAllText(recordFile, record + Environment.NewLine);

            LogAudit("ADD", "Added Record ID: " + recordId);

            Console.WriteLine("\nRegistration Added Successfully.");
        }

        static void ViewRecords()
        {
            Console.Clear();
            Console.WriteLine("===== ACTIVE EVENT REGISTRATIONS =====");

            string[] records = File.ReadAllLines(recordFile);

            foreach (string record in records)
            {
                if (string.IsNullOrWhiteSpace(record))
                    continue;

                string[] data = record.Split('|');

                if (data.Length < 9)
                    continue;

                if (data[7].Trim() == "True")
                {
                    DisplayRecord(data);
                }
            }

            LogAudit("READ", "Viewed all active records.");
        }

        static void SearchRecord()
        {
            Console.Clear();
            Console.WriteLine("===== SEARCH REGISTRATION =====");

            Console.Write("Enter Record ID or Participant Name: ");
            string keyword = Console.ReadLine().Trim().ToLower();

            string[] records = File.ReadAllLines(recordFile);

            bool found = false;

            foreach (string record in records)
            {
                if (string.IsNullOrWhiteSpace(record))
                    continue;

                string[] data = record.Split('|');

                if (data.Length < 9)
                    continue;

                if ((data[0].Trim().ToLower().Contains(keyword) ||
                     data[1].Trim().ToLower().Contains(keyword))
                     && data[7].Trim() == "True")
                {
                    DisplayRecord(data);
                    found = true;
                }
            }

            if (!found)
            {
                Console.WriteLine("No matching records found.");
            }

            LogAudit("READ", "Searched records using keyword: " + keyword);
        }

        static void UpdateRecord()
        {
            Console.Clear();
            Console.WriteLine("===== UPDATE REGISTRATION =====");

            Console.Write("Enter Record ID: ");
            string id = Console.ReadLine().Trim();

            List<string> updatedRecords = new List<string>();

            string[] records = File.ReadAllLines(recordFile);

            bool found = false;

            foreach (string record in records)
            {
                if (string.IsNullOrWhiteSpace(record))
                    continue;

                string[] data = record.Split('|');

                if (data.Length < 9)
                {
                    updatedRecords.Add(record);
                    continue;
                }

                if (data[0].Trim().Equals(id, StringComparison.OrdinalIgnoreCase)
                    && data[7].Trim() == "True")
                {
                    found = true;

                    Console.Write("New Participant Name: ");
                    string newName = Console.ReadLine();

                    while (string.IsNullOrWhiteSpace(newName))
                    {
                        Console.Write("Invalid. Enter Again: ");
                        newName = Console.ReadLine();
                    }

                    data[1] = newName;

                    Console.Write("New Event Name: ");
                    string newEvent = Console.ReadLine();

                    while (string.IsNullOrWhiteSpace(newEvent))
                    {
                        Console.Write("Invalid. Enter Again: ");
                        newEvent = Console.ReadLine();
                    }

                    data[2] = newEvent;

                    Console.Write("New Email: ");
                    string newEmail = Console.ReadLine();

                    while (!IsValidEmail(newEmail))
                    {
                        Console.Write("Invalid Email. Enter Again: ");
                        newEmail = Console.ReadLine();
                    }

                    data[3] = newEmail;

                    Console.Write("New Contact Number: ");
                    string newContact = Console.ReadLine();

                    while (string.IsNullOrWhiteSpace(newContact))
                    {
                        Console.Write("Invalid. Enter Again: ");
                        newContact = Console.ReadLine();
                    }

                    data[4] = newContact;

                    data[6] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                    string checksumData =
                        data[0] + data[1] + data[2] + data[3] + data[4];

                    data[8] = ComputeChecksum(checksumData);

                    updatedRecords.Add(string.Join("|", data));

                    LogAudit("UPDATE", "Updated Record ID: " + id);
                }
                else
                {
                    updatedRecords.Add(record);
                }
            }

            File.WriteAllLines(recordFile, updatedRecords);

            if (found)
            {
                Console.WriteLine("Record Updated Successfully.");
            }
            else
            {
                Console.WriteLine("Record Not Found.");
            }
        }

        static void SoftDeleteRecord()
        {
            Console.Clear();
            Console.WriteLine("===== SOFT DELETE REGISTRATION =====");

            Console.Write("Enter Record ID: ");
            string id = Console.ReadLine().Trim();

            List<string> updatedRecords = new List<string>();

            string[] records = File.ReadAllLines(recordFile);

            bool found = false;

            foreach (string record in records)
            {
                if (string.IsNullOrWhiteSpace(record))
                    continue;

                string[] data = record.Split('|');

                if (data.Length < 9)
                {
                    updatedRecords.Add(record);
                    continue;
                }

                if (data[0].Trim().Equals(id, StringComparison.OrdinalIgnoreCase))
                {
                    found = true;

                    data[7] = "False";
                    data[6] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                    updatedRecords.Add(string.Join("|", data));

                    LogAudit("DELETE", "Soft Deleted Record ID: " + id);
                }
                else
                {
                    updatedRecords.Add(record);
                }
            }

            File.WriteAllLines(recordFile, updatedRecords);

            if (found)
            {
                Console.WriteLine("Record Soft Deleted.");
            }
            else
            {
                Console.WriteLine("Record Not Found.");
            }
        }

        static void HardDeleteRecord()
        {
            Console.Clear();
            Console.WriteLine("===== HARD DELETE REGISTRATION =====");

            Console.Write("Enter Record ID: ");
            string id = Console.ReadLine().Trim();

            List<string> updatedRecords = new List<string>();

            string[] records = File.ReadAllLines(recordFile);

            bool found = false;

            foreach (string record in records)
            {
                if (string.IsNullOrWhiteSpace(record))
                    continue;

                string[] data = record.Split('|');

                if (data.Length < 9)
                    continue;

                if (data[0].Trim().Equals(id, StringComparison.OrdinalIgnoreCase))
                {
                    found = true;

                    LogAudit("DELETE", "Hard Deleted Record ID: " + id);

                    continue;
                }

                updatedRecords.Add(record);
            }

            File.WriteAllLines(recordFile, updatedRecords);

            if (found)
            {
                Console.WriteLine("Record Hard Deleted.");
            }
            else
            {
                Console.WriteLine("Record Not Found.");
            }
        }

        static void GenerateReport()
        {
            Console.Clear();
            Console.WriteLine("===== GENERATE REPORT =====");

            string reportPath =
                Path.Combine(dataFolder,
                "Report_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt");

            string[] records = File.ReadAllLines(recordFile);

            int activeCount = 0;
            int inactiveCount = 0;

            StringBuilder report = new StringBuilder();

            report.AppendLine("===== EVENT REGISTRATION REPORT =====");
            report.AppendLine("Generated At: " + DateTime.Now);
            report.AppendLine();

            foreach (string record in records)
            {
                if (string.IsNullOrWhiteSpace(record))
                    continue;

                string[] data = record.Split('|');

                if (data.Length < 9)
                    continue;

                if (data[7].Trim() == "True")
                    activeCount++;
                else
                    inactiveCount++;

                report.AppendLine(
                    "ID: " + data[0] +
                    " | Name: " + data[1] +
                    " | Event: " + data[2] +
                    " | Active: " + data[7]);
            }

            report.AppendLine();
            report.AppendLine("Total Active Records: " + activeCount);
            report.AppendLine("Total Inactive Records: " + inactiveCount);

            File.WriteAllText(reportPath, report.ToString());

            LogAudit("REPORT", "Generated report file.");

            Console.WriteLine("Report Generated Successfully.");
            Console.WriteLine("Saved at: " + reportPath);
        }

        static void DisplayRecord(string[] data)
        {
            Console.WriteLine("----------------------------------------");
            Console.WriteLine("Record ID       : " + data[0]);
            Console.WriteLine("Participant Name: " + data[1]);
            Console.WriteLine("Event Name      : " + data[2]);
            Console.WriteLine("Email           : " + data[3]);
            Console.WriteLine("Contact Number  : " + data[4]);
            Console.WriteLine("Created At      : " + data[5]);
            Console.WriteLine("Updated At      : " + data[6]);
            Console.WriteLine("Is Active       : " + data[7]);
            Console.WriteLine("Checksum        : " + data[8]);
            Console.WriteLine("----------------------------------------");
        }

        static string GenerateRecordId()
        {
            return "EVT-" + DateTime.Now.Ticks;
        }

        static bool IsValidEmail(string email)
        {
            return email.Contains("@") && email.Contains(".");
        }

        static string ComputeChecksum(string input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(
                    Encoding.UTF8.GetBytes(input));

                StringBuilder builder = new StringBuilder();

                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }

                return builder.ToString();
            }
        }

        static void LogAudit(string action, string details)
        {
            string log =
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") +
                " | " + action +
                " | " + details;

            File.AppendAllText(auditFile, log + Environment.NewLine);
        }
    }
}