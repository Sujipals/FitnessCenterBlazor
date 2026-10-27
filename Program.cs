using FitnessCenterBlazor.Components;
using FitnessCenterBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// DYNAMIC DATABASE SERVICE
builder.Services.AddScoped<FitnessCenterDbService>();

// LANGUAGE STATE
builder.Services.AddScoped<LanguageState>();

// DATABASE MONITOR

// One shared monitor for the whole application.
builder.Services.AddSingleton<DatabaseMonitorService>();

// Start the monitor automatically.
builder.Services.AddHostedService(
serviceProvider =>
serviceProvider.GetRequiredService<DatabaseMonitorService>());

// BLAZOR
builder.Services.AddRazorComponents().AddInteractiveServerComponents();


// CONTROLLER-BASED-API / WEB API CONTROLLERS
builder.Services.AddControllers();


// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("FileClientPolicy", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});


// BUILD APPLICATION
var app = builder.Build();


// Enable Swagger in Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


// HTTP CONFIGURATION

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors("FileClientPolicy");
app.UseAntiforgery();


// API CONTROLLERS

app.MapControllers();

// BLAZOR COMPONENTS

app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

// START APPLICATION
app.Run();
