namespace QueryRulesEngine.Persistence.Entities
{
    public class Employee : AuditableEntity<int>
    {
        public string TMID { get; set; }
        public string Name { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Title { get; set; }
        public string JobCode { get; set; }
        public string ReportsTo { get; set; }

        public virtual ICollection<Approver> Approvers { get; set; }
    }
}
