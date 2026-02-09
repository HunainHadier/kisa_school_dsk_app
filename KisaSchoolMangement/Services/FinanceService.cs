using System;
using System.Collections.ObjectModel;
using System.Configuration;
using KisaSchoolMangement.Models;
using MySql.Data.MySqlClient;

namespace KisaSchoolMangement.Services
{
    public class FinanceService
    {
        private readonly string _connectionString;

        public FinanceService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["KisaSchoolDB"].ConnectionString;
        }

        public FeeSummaryModel GetFeeSummary()
        {
            var summary = new FeeSummaryModel();

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();

                    summary.TotalAssigned = ExecuteDecimal(conn,
                        @"SELECT IFNULL(SUM(fs.amount), 0)
                          FROM student_fee_assignments sfa
                          JOIN fee_structures fs ON sfa.fee_structure_id = fs.id");

                    summary.TotalCollected = ExecuteDecimal(conn,
                        "SELECT IFNULL(SUM(amount), 0) FROM fee_payments");

                    summary.TotalPending = Math.Max(0, summary.TotalAssigned - summary.TotalCollected);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading fee summary: {ex.Message}", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }

            return summary;
        }

        public ObservableCollection<FeeAssignmentModel> GetFeeAssignments()
        {
            var assignments = new ObservableCollection<FeeAssignmentModel>();

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT sfa.id,
                               s.student_name,
                               s.gr_no,
                               c.name AS class_name,
                               se.name AS section_name,
                               fs.amount AS assigned_amount,
                               sfa.due_date,
                               sfa.status,
                               IFNULL(SUM(fp.amount), 0) AS paid_amount
                        FROM student_fee_assignments sfa
                        JOIN fee_structures fs ON sfa.fee_structure_id = fs.id
                        LEFT JOIN students s ON sfa.student_id = s.id
                        LEFT JOIN classes c ON s.class_id = c.id
                        LEFT JOIN sections se ON s.section_id = se.id
                        LEFT JOIN fee_payments fp ON fp.student_fee_assignment_id = sfa.id
                        GROUP BY sfa.id, s.student_name, s.gr_no, c.name, se.name, fs.amount, sfa.due_date, sfa.status
                        ORDER BY sfa.due_date DESC, sfa.id DESC;";

                    using (var cmd = new MySqlCommand(query, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            assignments.Add(new FeeAssignmentModel
                            {
                                Id = Convert.ToInt32(reader["id"]),
                                StudentName = reader["student_name"]?.ToString() ?? "Unknown",
                                GrNo = reader["gr_no"]?.ToString() ?? "N/A",
                                ClassName = reader["class_name"]?.ToString() ?? "N/A",
                                SectionName = reader["section_name"]?.ToString() ?? "N/A",
                                AssignedAmount = Convert.ToDecimal(reader["assigned_amount"]),
                                PaidAmount = Convert.ToDecimal(reader["paid_amount"]),
                                DueDate = reader["due_date"] != DBNull.Value
                                    ? Convert.ToDateTime(reader["due_date"])
                                    : (DateTime?)null,
                                Status = reader["status"]?.ToString() ?? "Pending"
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading fee assignments: {ex.Message}", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }

            return assignments;
        }

        public ObservableCollection<FeePaymentModel> GetFeePayments()
        {
            var payments = new ObservableCollection<FeePaymentModel>();

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT fp.id,
                               fp.student_fee_assignment_id,
                               fp.payment_date,
                               fp.amount,
                               fp.payment_method,
                               fp.transaction_ref,
                               fp.recorded_by,
                               s.student_name,
                               s.gr_no,
                               c.name AS class_name,
                               se.name AS section_name
                        FROM fee_payments fp
                        LEFT JOIN student_fee_assignments sfa ON fp.student_fee_assignment_id = sfa.id
                        LEFT JOIN students s ON sfa.student_id = s.id
                        LEFT JOIN classes c ON s.class_id = c.id
                        LEFT JOIN sections se ON s.section_id = se.id
                        ORDER BY fp.payment_date DESC, fp.id DESC;";

                    using (var cmd = new MySqlCommand(query, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            payments.Add(new FeePaymentModel
                            {
                                Id = Convert.ToInt32(reader["id"]),
                                AssignmentId = reader["student_fee_assignment_id"] != DBNull.Value
                                    ? Convert.ToInt32(reader["student_fee_assignment_id"])
                                    : 0,
                                StudentName = reader["student_name"]?.ToString() ?? "Unknown",
                                GrNo = reader["gr_no"]?.ToString() ?? "N/A",
                                ClassName = reader["class_name"]?.ToString() ?? "N/A",
                                SectionName = reader["section_name"]?.ToString() ?? "N/A",
                                Amount = Convert.ToDecimal(reader["amount"]),
                                PaymentDate = reader["payment_date"] != DBNull.Value
                                    ? Convert.ToDateTime(reader["payment_date"])
                                    : DateTime.Now,
                                PaymentMethod = reader["payment_method"]?.ToString() ?? "N/A",
                                TransactionRef = reader["transaction_ref"]?.ToString() ?? "",
                                RecordedBy = reader["recorded_by"] != DBNull.Value
                                    ? Convert.ToInt32(reader["recorded_by"])
                                    : null
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading fee payments: {ex.Message}", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }

            return payments;
        }

        public bool AddFeePayment(int assignmentId, DateTime paymentDate, decimal amount, string method, string reference, int? recordedBy)
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var transaction = conn.BeginTransaction())
                    {
                        string insertQuery = @"
                            INSERT INTO fee_payments (student_fee_assignment_id, payment_date, amount, payment_method, transaction_ref, recorded_by, created_at)
                            VALUES (@AssignmentId, @PaymentDate, @Amount, @Method, @Reference, @RecordedBy, @CreatedAt)";

                        using (var cmd = new MySqlCommand(insertQuery, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@AssignmentId", assignmentId);
                            cmd.Parameters.AddWithValue("@PaymentDate", paymentDate);
                            cmd.Parameters.AddWithValue("@Amount", amount);
                            cmd.Parameters.AddWithValue("@Method", method ?? "");
                            cmd.Parameters.AddWithValue("@Reference", reference ?? "");
                            cmd.Parameters.AddWithValue("@RecordedBy", recordedBy.HasValue ? recordedBy.Value : (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                            cmd.ExecuteNonQuery();
                        }

                        UpdateAssignmentStatus(conn, transaction, assignmentId);
                        transaction.Commit();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error saving fee payment: {ex.Message}", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return false;
            }
        }

        private void UpdateAssignmentStatus(MySqlConnection conn, MySqlTransaction transaction, int assignmentId)
        {
            decimal assignedAmount = 0;
            decimal paidAmount = 0;

            using (var cmd = new MySqlCommand(@"
                    SELECT fs.amount
                    FROM student_fee_assignments sfa
                    JOIN fee_structures fs ON sfa.fee_structure_id = fs.id
                    WHERE sfa.id = @AssignmentId;", conn, transaction))
            {
                cmd.Parameters.AddWithValue("@AssignmentId", assignmentId);
                var result = cmd.ExecuteScalar();
                assignedAmount = result != null ? Convert.ToDecimal(result) : 0;
            }

            using (var cmd = new MySqlCommand(
                       "SELECT IFNULL(SUM(amount), 0) FROM fee_payments WHERE student_fee_assignment_id = @AssignmentId;",
                       conn, transaction))
            {
                cmd.Parameters.AddWithValue("@AssignmentId", assignmentId);
                var result = cmd.ExecuteScalar();
                paidAmount = result != null ? Convert.ToDecimal(result) : 0;
            }

            string status = paidAmount <= 0 ? "Pending" : paidAmount >= assignedAmount ? "Paid" : "Partial";

            using (var cmd = new MySqlCommand(
                       "UPDATE student_fee_assignments SET status = @Status WHERE id = @AssignmentId;", conn, transaction))
            {
                cmd.Parameters.AddWithValue("@Status", status);
                cmd.Parameters.AddWithValue("@AssignmentId", assignmentId);
                cmd.ExecuteNonQuery();
            }
        }

        private static decimal ExecuteDecimal(MySqlConnection conn, string query)
        {
            using (var cmd = new MySqlCommand(query, conn))
            {
                var result = cmd.ExecuteScalar();
                return result != null ? Convert.ToDecimal(result) : 0;
            }
        }
    }
}
