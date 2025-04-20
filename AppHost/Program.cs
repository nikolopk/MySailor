var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("cache");

builder.AddProject<Projects.MySailorApi>("api")
    .WithReference(redis)
    .WaitFor(redis)
    .WithReplicas(1);

builder.Build().Run();