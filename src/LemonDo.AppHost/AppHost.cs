var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.LemonDo_Api>("api");

builder.AddJavaScriptApp("client", "../client", "dev")
    .WithPnpm()
    .WithReference(api)
    .WithHttpEndpoint(env: "PORT");

builder.Build().Run();
