using System.Reflection;

using Zastai.NuGet.Server.Auth;
using Zastai.NuGet.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddSingleton<ISettings, Settings>();

builder.Services.AddSingleton<RequestLoggingFilter>();

builder.Services.AddSingleton<IApiKeyStore, InMemoryApiKeyStore>();
builder.Services.AddSingleton<IPackageStore, PackageStore>();
builder.Services.AddSingleton<ISymbolStore, SymbolStore>();
builder.Services.AddSingleton<IUserStore, InMemoryUserStore>();

// Enable API key support
builder.Services.AddAuthentication(options => {
  options.DefaultAuthenticateScheme = ApiKeySupport.Scheme;
  options.DefaultChallengeScheme = ApiKeySupport.Scheme;
}).AddApiKeySupport();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => {
  var xmlDocFile = Path.Combine(AppContext.BaseDirectory, Assembly.GetExecutingAssembly().GetName().Name + ".xml");
  options.IncludeXmlComments(xmlDocFile);
});

var app = builder.Build();

if (app.Environment.IsDevelopment()) {
  app.UseSwagger();
  app.UseSwaggerUI();
}
else {
  app.UseExceptionHandler("/error");
  // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
  app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllers();

app.Run();
