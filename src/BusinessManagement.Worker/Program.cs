using BusinessManagement.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWorkerServices(builder.Configuration);

builder.Build().Run();