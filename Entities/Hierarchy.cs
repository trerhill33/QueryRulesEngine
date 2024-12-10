namespace QueryRulesEngine.Entities
{
    public class Hierarchy
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public virtual ICollection<Approver> Approvers { get; set; }
        public virtual ICollection<Metadata> Metadata { get; set; }
        public virtual ICollection<MetadataKey> AllowedMetadataKeys { get; set; }
    }
}
