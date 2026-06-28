using Azure.Storage.Blobs;
using Hangfire;
using Hangfire.PostgreSql;
using HireIQ.Application.Interfaces;
using HireIQ.Domain.Interfaces;
using HireIQ.Infrastructure.Ai;
using HireIQ.Infrastructure.Ai.Interview;
using HireIQ.Infrastructure.Cache;
using HireIQ.Infrastructure.Email;
using HireIQ.Infrastructure.Identity;
using HireIQ.Infrastructure.JobBoards;
using HireIQ.Infrastructure.Persistence;
using HireIQ.Infrastructure.Persistence.Repositories;
using HireIQ.Infrastructure.Pdf;
using HireIQ.Infrastructure.Pipeline;
using HireIQ.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace HireIQ.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // === PostgreSQL ===
        services.AddDbContext<AppDbContext>(opt =>
            opt.UseNpgsql(config.GetConnectionString("DefaultConnection"),
                npg => npg.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        // === Settings ===
        services.Configure<JwtSettings>(config.GetSection(JwtSettings.SectionName));
        services.Configure<RedisSettings>(config.GetSection(RedisSettings.SectionName));
        services.Configure<AzureBlobSettings>(config.GetSection(AzureBlobSettings.SectionName));

        // === Redis ===
        var redisConn = config[$"{RedisSettings.SectionName}:ConnectionString"] ?? "localhost:6379";
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConn));
        services.AddSingleton<ICacheService, RedisCacheService>();

        // === Azure Blob ===
        var blobConn = config[$"{AzureBlobSettings.SectionName}:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(blobConn))
        {
            services.AddSingleton(_ => new BlobServiceClient(blobConn));
            services.AddSingleton<IBlobStorageService, AzureBlobStorageService>();
        }

        // === Repositories / UoW ===
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // === Identity / Auth ===
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();

        // === Email === (concrete also registered so controllers can use rich helpers like SendInterviewInviteAsync)
        services.AddScoped<SmtpEmailService>();
        services.AddScoped<IEmailService>(sp => sp.GetRequiredService<SmtpEmailService>());

        // === AI ===
        // GroqService manages its own HttpClient internally (Timeout = 130s for long Groq calls).
        // Use plain Scoped registration, NOT AddHttpClient<T> — typed client requires HttpClient as first ctor param.
        services.AddScoped<GroqService>();
        services.AddScoped<IAiService, GroqAiService>();
        services.AddScoped<MLService>();

        // === PDF === (concrete also registered — legacy controllers inject concrete types)
        services.AddScoped<PdfService>();
        services.AddScoped<IPdfService>(sp => sp.GetRequiredService<PdfService>());
        services.AddScoped<PdfExtractorService>();
        services.AddScoped<IPdfExtractorService>(sp => sp.GetRequiredService<PdfExtractorService>());
        // Unified document parser (PDF, DOCX, XLSX, CSV, TXT) — used by JobPostingAiController
        services.AddScoped<IDocumentParserService, DocumentParserService>();

        // === Mongo (legacy / agent memory) ===
        services.AddSingleton<MongoDbService>();

        // === Flask ML microservice ===
        services.AddHttpClient("FlaskService", c =>
        {
            c.BaseAddress = new Uri(config["FlaskService:BaseUrl"] ?? "http://127.0.0.1:5000");
        });

        // === Hangfire (background jobs — Postgres-backed) ===
        services.AddHangfire(cfg => cfg
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(o => o.UseNpgsqlConnection(config.GetConnectionString("DefaultConnection"))));
        services.AddHangfireServer();

        // === Phase-2 hiring automation services ===
        services.AddScoped<IHiringPipelineService, HiringPipelineService>();
        services.AddScoped<IEmailTemplateService, EmailTemplateService>();
        services.AddScoped<IEmailQueueService, EmailQueueService>();
        services.AddScoped<IApplicationIntakeService, ApplicationIntakeService>();
        services.AddScoped<IAutoScreeningOrchestrator, AutoScreeningOrchestrator>();
        services.AddScoped<AutoScreeningOrchestrator>();

        // Transcription + media server
        services.AddScoped<ITranscriptionService, WhisperTranscriptionService>();
        services.AddScoped<IMediaServerService, LiveKitMediaService>();

        // Job-board connectors
        services.AddScoped<IndeedFeedConnector>();
        services.AddScoped<LinkedInShareConnector>();
        services.AddScoped<NaukriConnector>();
        services.AddScoped<GlassdoorConnector>();
        services.AddScoped<IJobBoardConnector, IndeedFeedConnector>();
        services.AddScoped<IJobBoardConnector, LinkedInShareConnector>();
        services.AddScoped<IJobBoardConnector, NaukriConnector>();
        services.AddScoped<IJobBoardConnector, GlassdoorConnector>();

        // AI Interview swarm (3 agents + orchestrator)
        services.AddScoped<IFastQuestionAgent, GroqFastQuestionAgent>();
        services.AddScoped<IDeepAnswerAgent, GroqDeepAnswerAgent>();
        services.AddScoped<IVisualBehaviorAgent, GeminiVisualBehaviorAgent>();
        services.AddScoped<IInterviewOrchestrator, InterviewOrchestrator>();
        services.AddScoped<InterviewOrchestrator>();  // also concrete — Hangfire jobs use the concrete type

        return services;
    }
}
