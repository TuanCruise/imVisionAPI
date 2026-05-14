using ImVision.Host;
using ImVision.Integration.DependencyInjection;

var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(args);

// Đăng ký toàn bộ ImVision Integration
builder.Services.AddImVisionIntegration(builder.Configuration);

// Đăng ký Worker chạy ngầm để test
builder.Services.AddHostedService<ImVisionWorker>();

var host = builder.Build();
host.Run();
