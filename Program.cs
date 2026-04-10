using ai_clinic.Components;
using ai_clinic.Backend.Services;
using Supabase;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure Supabase
var supabaseUrl = builder.Configuration["Supabase:Url"] 
    ?? Environment.GetEnvironmentVariable("SUPABASE_URL")
    ?? throw new InvalidOperationException("Supabase URL not configured");
var supabaseKey = builder.Configuration["Supabase:Key"] 
    ?? Environment.GetEnvironmentVariable("SUPABASE_KEY")
    ?? throw new InvalidOperationException("Supabase Key not configured");

builder.Services.AddScoped<Supabase.Client>(_ => 
    new Supabase.Client(
        supabaseUrl, 
        supabaseKey,
        new SupabaseOptions
        {
            AutoConnectRealtime = true
        }
    )
);

// Register services
builder.Services.AddScoped<SupabaseService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
