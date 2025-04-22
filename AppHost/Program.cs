var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("cache");

builder.AddProject<Projects.MySailorApi>("api")
    .WithReference(redis)
    .WaitFor(redis)
    .WithReplicas(5);

builder.Build().Run();