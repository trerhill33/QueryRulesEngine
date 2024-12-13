namespace QueryRulesEngine.Persistence.Entities
{
    public class Hierarchy : AuditableEntity<int>
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Tag { get; set; }
        public virtual ICollection<Approver> Approvers { get; set; }
        public virtual ICollection<Metadata> Metadata { get; set; }
        public virtual ICollection<MetadataKey> AllowedMetadataKeys { get; set; }
    }
}
