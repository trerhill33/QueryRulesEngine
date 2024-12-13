using FluentValidation;

namespace QueryRulesEngine.Controllers
{
    public abstract class BaseApiController<T> : BaseVersionedController<T>
    {
        private ILogger<T> _loggerInstance = null!;
        private IValidator<T> _validatorInstance = null!;
        protected ILogger<T> Logger => _loggerInstance ??= HttpContext.RequestServices.GetService<ILogger<T>>() ?? null!;
        protected IValidator<T> Validator => _validatorInstance ??= HttpContext.RequestServices.GetService<IValidator<T>>() ?? null!;
    }
}
