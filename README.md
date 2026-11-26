# RawRabbit (.NET 9 port)

This repository is a port of [pardahlman/RawRabbit](https://github.com/pardahlman/RawRabbit), a middleware-style .NET client for [RabbitMQ](https://www.rabbitmq.com/). The GitHub description calls this a .NET 9 migration.

**Important:** the default branch, `2.0`, still targets `netstandard1.5` and `net451` — the original 2.x stack. The .NET 9 work (`TargetFramework` `net9.0`, package version 3.0.0) is on the `2.1` branch. Clone that branch unless you specifically want the legacy 2.x sources.

Sibling repos [RawRabbit2](https://github.com/laird/RawRabbit2), [RawRabbit3](https://github.com/laird/RawRabbit3), and [RawRabbit4](https://github.com/laird/RawRabbit4) are earlier iterations of the same effort. Their default branches also still show the upstream 2.x README and targets. Prefer this repo's `2.1` branch as the current port.

## What you need

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) (for the `2.1` branch)
- A running [RabbitMQ](https://www.rabbitmq.com/download.html) broker for the samples and tests

## Install (from source)

This port is not published as a NuGet package under this account. Official `RawRabbit` packages on nuget.org are still the upstream 2.x line. Build from source:

```bash
git clone -b 2.1 https://github.com/laird/RawRabbit5.git
cd RawRabbit5
dotnet build RawRabbit.sln
```

To use it from another project, add a project reference to `src/RawRabbit/RawRabbit.csproj`, or pack and install locally:

```bash
dotnet pack src/RawRabbit/RawRabbit.csproj -c Release
dotnet add YourApp package RawRabbit --source ./src/RawRabbit/bin/Release
```

## Get started

Publish and subscribe with a strongly typed message (broker must be reachable with the usual RabbitMQ defaults, or with a `rawrabbit.json` configuration file):

```csharp
var client = RawRabbitFactory.CreateSingleton();
await client.SubscribeAsync<BasicMessage>(async msg =>
{
    Console.WriteLine($"Received: {msg.Prop}.");
});

await client.PublishAsync(new BasicMessage { Prop = "Hello, world!" });
```

Request/response uses RabbitMQ's [direct reply-to](https://www.rabbitmq.com/docs/direct-reply-to) feature:

```csharp
var client = RawRabbitFactory.CreateSingleton();
await client.RespondAsync<BasicRequest, BasicResponse>(async request =>
{
    return new BasicResponse();
});

var response = await client.RequestAsync<BasicRequest, BasicResponse>();
```

Acknowledgements are return values from the handler, not a side API. Delayed retries are supported too:

```csharp
await client.SubscribeAsync<BasicMessage>(async msg =>
{
    if (UnableToProcessMessage(msg))
    {
        return new Nack(requeue: true);
    }

    try
    {
        ProcessMessage(msg);
        return new Ack();
    }
    catch (Exception)
    {
        return Retry.In(TimeSpan.FromSeconds(30));
    }
});
```

Samples are under `sample/`. Configuration, plugins, and per-call topology overrides are described in `docs/`. For 1.x documentation, see the [upstream stable branch](https://github.com/pardahlman/RawRabbit/tree/stable).

## License

MIT (see `LICENSE`). This is a derivative of pardahlman's RawRabbit; keep that attribution.

## Notes for maintainers

- Point the default branch at `2.1` (or merge it) so the GitHub description, README, and code agree.
- Upstream badges (AppVeyor, nuget.org, the old Slack/Heroku badge at `rawrabbit.herokuapp.com`) describe pardahlman's project, not this fork. Remove or replace them.
- The older wording "Documentation for version 2.x of the is currently found" and the typos "suppoert" and "modifly" should not be copied forward.
