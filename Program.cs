// In Program.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.OpenApi.Models;
using QueryRulesEngine.Features.Rules.AddRuleToLevel;
using QueryRulesEngine.Features.Rules.EditRule;
using QueryRulesEngine.Features.Rules.RemoveRule;
using QueryRulesEngine.Persistence.Repositories;
using QueryRulesEngine.Persistence.Repositories.Interfaces;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddTransient<IRuleRepository, RuleRepository>();
builder.Services.AddTransient<IAddRuleToLevelService, AddRuleToLevelService>();
builder.Services.AddTransient<IAddRuleToLevelService, AddRuleToLevelService>();
builder.Services.AddTransient<IEditRuleService, EditRuleService>();
builder.Services.AddTransient<IRemoveRuleService, RemoveRuleService>();
builder.Services.AddTransient<ILevelRepository, LevelRepository>();
builder.Services.AddTransient<ILevelRepository, LevelRepository>();
builder.Services.AddTransient<ILevelRepository, LevelRepository>();



// Add services to the container
builder.Services.AddControllers();
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});
// Add Basic Swagger
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Approval Hierarchy Manager API",
        Version = "v1"
    });

    // Enable annotations we're using in controllers
    options.EnableAnnotations();
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Approval Hierarchy Manager V1");
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();