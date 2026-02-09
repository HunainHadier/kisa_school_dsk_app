using System;
using System.Collections.ObjectModel;
using System.Configuration;
using KisaSchoolMangement.Models;
using MySql.Data.MySqlClient;

namespace KisaSchoolMangement.Services
{
    public class DashboardService
    {
        private readonly string _connectionString;

        public DashboardService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["KisaSchoolDB"].ConnectionString;
        }

        public DashboardCounts GetCounts()
        {
            var counts = new DashboardCounts();

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();

                    counts.TotalStudents = ExecuteCount(conn, "SELECT COUNT(*) FROM students WHERE is_deleted = 0 OR is_deleted IS NULL");
                    counts.TotalTeachers = ExecuteCount(conn, "SELECT COUNT(*) FROM teachers WHERE is_active = 1 OR is_active IS NULL");
                    counts.TotalDonors = ExecuteCount(conn, "SELECT COUNT(*) FROM donors");
                    counts.TotalUsers = ExecuteCount(conn, "SELECT COUNT(*) FROM users WHERE is_deleted = 0 OR is_deleted IS NULL");
                    counts.PendingFees = ExecuteCount(conn, "SELECT COUNT(*) FROM student_fee_assignments WHERE status IS NULL OR status <> 'Paid'");
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading dashboard stats: {ex.Message}", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }

            return counts;
        }

        public ObservableCollection<RecentActivity> GetRecentActivities(int limit = 6)
        {
            var activities = new ObservableCollection<RecentActivity>();

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = @$"
                        SELECT activity, activity_time
                        FROM (
                            SELECT CONCAT('Student registered: ', student_name) AS activity,
                                   created_at AS activity_time
                            FROM students

                            UNION ALL

                            SELECT CONCAT('Fee payment received from ', IFNULL(s.student_name, 'Student')) AS activity,
                                   fp.payment_date AS activity_time
                            FROM fee_payments fp
                            LEFT JOIN student_fee_assignments sfa ON fp.student_fee_assignment_id = sfa.id
                            LEFT JOIN students s ON sfa.student_id = s.id

                            UNION ALL

                            SELECT CONCAT('New donor added: ', name) AS activity,
                                   created_at AS activity_time
                            FROM donors
                        ) activities
                        WHERE activity_time IS NOT NULL
                        ORDER BY activity_time DESC
                        LIMIT {limit};";

                    using (var cmd = new MySqlCommand(query, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string activity = reader["activity"]?.ToString() ?? "Recent update";
                            string timeText = "recently";

                            if (reader["activity_time"] != DBNull.Value &&
                                DateTime.TryParse(reader["activity_time"].ToString(), out var timestamp))
                            {
                                timeText = FormatRelativeTime(timestamp);
                            }

                            activities.Add(new RecentActivity
                            {
                                Activity = activity,
                                Time = timeText
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading recent activities: {ex.Message}", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }

            return activities;
        }

        private static int ExecuteCount(MySqlConnection conn, string query)
        {
            using (var cmd = new MySqlCommand(query, conn))
            {
                var result = cmd.ExecuteScalar();
                return result != null ? Convert.ToInt32(result) : 0;
            }
        }

        private static string FormatRelativeTime(DateTime timestamp)
        {
            var diff = DateTime.Now - timestamp;

            if (diff.TotalMinutes < 1)
                return "Just now";
            if (diff.TotalMinutes < 60)
                return $"{Math.Floor(diff.TotalMinutes)} min ago";
            if (diff.TotalHours < 24)
                return $"{Math.Floor(diff.TotalHours)} hrs ago";
            if (diff.TotalDays < 7)
                return $"{Math.Floor(diff.TotalDays)} days ago";

            return timestamp.ToString("yyyy-MM-dd");
        }
    }
}
