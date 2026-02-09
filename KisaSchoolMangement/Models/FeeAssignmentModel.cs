using System;

namespace KisaSchoolMangement.Models
{
    public class FeeAssignmentModel
    {
        public int Id { get; set; }
        public string StudentName { get; set; }
        public string GrNo { get; set; }
        public string ClassName { get; set; }
        public string SectionName { get; set; }
        public decimal AssignedAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public DateTime? DueDate { get; set; }
        public string Status { get; set; }

        public decimal Balance => AssignedAmount - PaidAmount;
        public string DueDateDisplay => DueDate.HasValue ? DueDate.Value.ToString("yyyy-MM-dd") : "N/A";
    }
}
