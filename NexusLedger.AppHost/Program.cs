var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis");

var kafka = builder.AddKafka("kafka")
                   .WithDataVolume()
                   .WithLifetime(ContainerLifetime.Persistent);

var sql = builder.AddSqlServer("sqlserver")
                 .WithDataVolume()
                 .WithLifetime(ContainerLifetime.Persistent)
                 .AddDatabase("ledgerdb");

var gateway = builder.AddProject<Projects.NexusLedger_PaymentGateway>("paymentgateway")
                     .WithReference(redis)
                     .WithReference(kafka)
                     .WithExternalHttpEndpoints();

var settlement = builder.AddProject<Projects.NexusLedger_SettlementService>("settlementservice")
                        .WithReference(kafka)
                        .WithReference(sql);

var reconciliation = builder.AddProject<Projects.NexusLedger_ReconciliationWorker>("reconciliationworker")
                            .WithReference(sql);

builder.Build().Run();
