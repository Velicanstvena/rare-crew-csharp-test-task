﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace EmployeeTimeReport
{
    public class TimeEntry
    {
        public string Id { get; set; }
        public string EmployeeName { get; set; }
        public DateTime StarTimeUtc { get; set; }
        public DateTime EndTimeUtc { get; set; }
        public string EntryNotes { get; set; }
        public DateTime? DeletedOn { get; set; }
    }

    public class ReportsGenerator
    {
        private static readonly string apiUrl = "https://rc-vault-fap-live-1.azurewebsites.net/api/gettimeentries?code=vO17RnE8vuzXzPJo5eaLLjXjmRW07law99QTD90zat9FfOQJKKUcgQ==";
        
        public static async Task Main(string[] args)
        {
            var timeEntries = await GetTimeEntriesAsync();
            var employeeHours = CalculateTotalHours(timeEntries);
            
            // Generate HTML
            var htmlContent = GenerateHtml(employeeHours);
            System.IO.File.WriteAllText("EmployeeReport.html", htmlContent);
            Console.WriteLine("HTML report generated: EmployeeReport.html");
        }

        private static async Task<List<TimeEntry>> GetTimeEntriesAsync()
        {
            using var httpClient = new HttpClient();
            var response = await httpClient.GetStringAsync(apiUrl);
            return JsonConvert.DeserializeObject<List<TimeEntry>>(response);
        }

        private static Dictionary<string, double> CalculateTotalHours(List<TimeEntry> entries)
        {
            var employeeHours = new Dictionary<string, double>();

            foreach (var entry in entries)
            {
                if (entry.DeletedOn != null || string.IsNullOrEmpty(entry.EmployeeName)) continue;

                var startTime = entry.StarTimeUtc;
                var endTime = entry.EndTimeUtc;

                var totalHours = (endTime - startTime).TotalHours;
                if (employeeHours.ContainsKey(entry.EmployeeName))
                {
                    employeeHours[entry.EmployeeName] += totalHours;
                }
                else
                {
                    employeeHours[entry.EmployeeName] = totalHours;
                }
            }

            return employeeHours.OrderByDescending(e => e.Value).ToDictionary(e => e.Key, e => e.Value);
        }

        private static string GenerateHtml(Dictionary<string, double> employeeHours)
        {
            var sb = new System.Text.StringBuilder();

            sb.Append("<html><head><style>");
            sb.Append("table { width: 100%; border-collapse: collapse; }");
            sb.Append("th, td { border: 1px solid black; padding: 8px; text-align: left; }");
            sb.Append("tr.low-hours { background-color: #ffcccc; }");
            sb.Append("</style></head><body>");
            sb.Append("<h1>Employee Time Report</h1>");
            sb.Append("<table>");
            sb.Append("<tr><th>Name</th><th>Total Time Worked (hours)</th></tr>");

            foreach (var entry in employeeHours)
            {
                var rowClass = entry.Value < 100 ? "low-hours" : "";
                sb.Append($"<tr class='{rowClass}'>");
                sb.Append($"<td>{entry.Key}</td>");
                sb.Append($"<td>{entry.Value:F2}</td>");
                sb.Append("</tr>");
            }

            sb.Append("</table>");
            sb.Append("</body></html>");

            return sb.ToString();
        }
    }
}
