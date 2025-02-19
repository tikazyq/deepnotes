using DeepNotes.Database.SqlServer;
using DeepNotes.Core.Interfaces;
using DeepNotes.LLM;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<DeepNotesDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (connectionString == null || connectionString.Contains("sqlite"))
    {
        options.UseSqlite(connectionString);
    }
    else
    {
        options.UseSqlServer(connectionString);
    }
});

// In your Program.cs
builder.Services.AddSingleton<LLMService>(sp =>
{
    var config = new LLMConfig
    {
        Provider = builder.Configuration["LLM:Provider"],
        Model = builder.Configuration["LLM:Model"],
        Endpoint = builder.Configuration["LLM:Endpoint"],
        ApiKey = builder.Configuration["LLM:ApiKey"],
        ApiVersion = builder.Configuration["LLM:ApiVersion"],
        Language = builder.Configuration["LLM:Language"] ?? "english"
    };
    return new LLMService(config);
});

builder.Services.AddScoped<IDocumentRepository, SqlDocumentRepository>();
// ... register other services

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();