using StockViewer.Worker;
using StockViewer.Core.Data;
using StockViewer.Worker.Scrapers;
using StockViewer.Worker.Jobs;
using Microsoft.EntityFrameworkCore;
using Quartz;

var builder = Host.CreateApplicationBuilder(args);

// Add DbContext
// Note: Use a connection string from configuration or environment variable.
// For dev, we can use a local string or in-memory if needed, but PostgreSQL is requested.
// We'll assume "DefaultConnection" is set.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpClient<TwseScraperService>();
builder.Services.AddSingleton<TwseScraperService>(); // HttpClient is managed by factory, but service can be singleton or scoped. Scoped is safer with DbContext if injected?
// Wait, ScraperJob is Scoped (created by Quartz factory usually).
// TwseScraperService uses HttpClient, so it can be Transient or Scoped.
// Let's make it Transient.
builder.Services.AddTransient<TwseScraperService>();

builder.Services.AddQuartz(q =>
{
    var jobKey = new JobKey("ScraperJob");
    q.AddJob<ScraperJob>(opts => opts.WithIdentity(jobKey));

    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("ScraperJob-trigger")
        // Run every day at 14:00 and 17:00 (TWSE closes at 13:30, data usually ready by 14:00 or later)
        .WithCronSchedule("0 0 14,17 ? * MON-FRI") 
    );
});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

var host = builder.Build();
host.Run();
