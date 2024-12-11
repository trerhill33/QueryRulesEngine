namespace QueryRulesEngine.Persistence
{
    public abstract class AuditableEntity<TId> : IAuditableEntity<TId>, IAuditableEntity, IEntity, IEntity<TId>
    {
        public TId Id { get; set; }

        public string CreatedBy { get; set; }

        public DateTime CreatedOn { get; set; }

        public string LastModifiedBy { get; set; }

        public DateTime? LastModifiedOn { get; set; }
    }
    public interface IAuditableEntity
    {
    }   
    
    public interface IEntity
    {
    } 
    
    public interface IEntity<TId>
    {
    }
    public interface IAuditableEntity<TId> : IAuditableEntity, IEntity, IEntity<TId>
{
}
}
