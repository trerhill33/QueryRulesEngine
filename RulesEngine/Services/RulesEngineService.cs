namespace QueryRulesEngine.RulesEngine.Services
{
    //public class RulesEngineService
    //{
    //    private readonly IQueryProcessor _queryProcessor;
    //    private readonly IMetadataQueryProcessor _metadataQueryProcessor;
    //    private readonly IQueryPersistenceService _persistenceService;

    //    public RulesEngineService(
    //    IQueryProcessor queryProcessor,
    //        IMetadataQueryProcessor metadataQueryProcessor,
    //        IQueryPersistenceService persistenceService)
    //    {
    //        _queryProcessor = queryProcessor;
    //        _metadataQueryProcessor = metadataQueryProcessor;
    //        _persistenceService = persistenceService;
    //    }

    //    public async Task<IEnumerable<T>> ApplyRules<T>(string persistedQuery) where T : class
    //    {
    //        // 1. Parse the persisted query string
    //        var queryMatrix = _persistenceService.ParseFromStorageFormat(persistedQuery);

    //        // 2. Determine if we need metadata handling
    //        var processor = HasMetadataConditions(queryMatrix)
    //            ? _metadataQueryProcessor
    //            : _queryProcessor;

    //        // 3. Apply the query using appropriate processor
    //        return processor.ApplyQuery(source, queryMatrix);
    //    }
    //}
}
