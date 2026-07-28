using Serilog;
using Serilog.Events;
using FirstBank.DataAccess.Data;
using FirstBank.DataAccess.Repositories; // Needed for ITransactionRepository
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

//This configures the Serilog logging framework to log to the console and a file
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File("logs/Firstbank-.log", rollingInterval: RollingInterval.Day)
    .WriteTo.Seq("http://localhost:5341")
    .CreateLogger();

try
{
    Log.Information("Starting FirstBank API Host...");
    // Creates the builder.
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, loggerConfiguration) =>
        {
            loggerConfiguration.ReadFrom.Configuration(context.Configuration);
        });

    // Add system controllers
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            // This tells Swagger and the API to default to camelCase
            options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;

            //This makes the API forgiving so it accepts ANY capitalization (camelCase, PascalCase, lowercase)
            options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        });

    // Adding the EF Core Socket
    builder.Services.AddDbContext<FirstDBContext>(options =>
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            sqlServerOptionsAction: sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null);
            }));

    // This is the Dependency Injection socket
    // It says - Whenever a controller asks for ITransactionRepository, give it a new Transaction Repository
    builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();

    // This turns on an In-Memory Cache socket for the controllers to use
    builder.Services.AddMemoryCache();

    // Adding the JWT Authentication Socket
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
            };
        });

    // Configuring Swagger to Accept Jwt Tokens
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer Scheme. Enter 'Bearer' [space] and then your token.",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement{
    {
        new OpenApiSecurityScheme{
            Reference = new OpenApiReference{
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        },
        new string[]{ }
    }});
    });

    builder.Services.AddRateLimiter(options =>
    {
        options.AddFixedWindowLimiter("TransactionPolicy", opt =>
        {
            opt.PermitLimit = 5; // Maximum of 5 requests
            opt.Window = TimeSpan.FromMinutes(1); // Per 1 minute
            opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opt.QueueLimit = 0; // No queuing, reject immediately if limit is reached
        });

        options.OnRejected = async (context, token) =>
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.HttpContext.Response.ContentType = "application/json";

            Log.Warning("Rate limit triggered for IP: {IPAddress} on Endpoint: {Endpoint",
                context.HttpContext.Connection.RemoteIpAddress, context.HttpContext.Request.Path);

            if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            {
                context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
            }

            //This is put here and not in TransactionsController because the error should not get to the transactions controller
            //It gets bounced out right from the middleware, so we have to handle it here.
            var errorResponse = new
            {
                Success = false,
                StatusCode = 429,
                Message = "Too many Requests. Please try again later",
                Data = (object)null!
            };

            await context.HttpContext.Response.WriteAsJsonAsync(errorResponse, token);
        };
    });

    //This tells MediatR to look inside the executing assembly for any Handlers
    builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

    // Build the app pipeline
    var app = builder.Build();

    //This block automatically creates the Database
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
       // try
       // {
            var context = services.GetRequiredService<FirstDBContext>();

            //This applies any pending migrations and creates the database if it doesn't exist
            context.Database.Migrate();
      //  }
       // catch (Exception ex)
       // {
           // var logger = services.GetRequiredService<ILogger<Program>>();
           // logger.LogError(ex, "An error occured while creating/migrating the database");
       // }
    }

    /*
    //Temporary code to get the hash
    Console.WriteLine("\n\n--- MY ADMIN DASH ---");
    Console.WriteLine(BCrypt.Net.BCrypt.HashPassword("FBNAdminPass2026!")); The Email is admin@firstbank.com
    Console.WriteLine("------------------------\n\n");
    */

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        // This builds the webpage at /swagger 
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    //app.UseHttpsRedirection();

    // This is needed for cases where the database goes offline, Dapper will throw a massive SQL exception.
    // If that exception leaks to the client, a hacker would know your exact table names and column structures.
    // We intercept the crash globally and return a safe, generic message
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";

            //This fetches the actual exception that caused the crash
            var exceptionHandlerPathFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
            var exception = exceptionHandlerPathFeature?.Error;

            Log.Error(exception, "An Unhandled Exception occured at {RequestPath}", context.Request.Path);

            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                statusCode = 500,
                message = "An unexpected error occurred."
            });
        });
    });

    app.UseRateLimiter();

    //app.UseDefaultFiles(); // Automatically serves index.html when hitting localhost:<port>
    //app.UseStaticFiles(); // Enables ASP.NET Core to serve files from the wwwroot folder

    // IMPORTANT: Authentication (Who are you?) MUST happen before Authorization (What can you do?)
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    //This logs the exact reason if the app crashes on startup
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    //This ensures all logs are safely written to the log file before the app exits
    Log.CloseAndFlush();
}