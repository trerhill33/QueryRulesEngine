namespace QueryRulesEngine.Persistence
{
    public interface IResult<out T> : IResult
    {
        T Data { get; }
    }    
    
    public interface IResult
    {
    }
}
