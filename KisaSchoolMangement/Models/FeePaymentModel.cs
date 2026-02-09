using System;

namespace KisaSchoolMangement.Models
{
    public class FeePaymentModel
    {
        public int Id { get; set; }
        public int AssignmentId { get; set; }
        public string StudentName { get; set; }
        public string GrNo { get; set; }
        public string ClassName { get; set; }
        public string SectionName { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string PaymentMethod { get; set; }
        public string TransactionRef { get; set; }
        public int? RecordedBy { get; set; }

        public string PaymentDateDisplay => PaymentDate.ToString("yyyy-MM-dd");
    }
}
