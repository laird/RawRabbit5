# Migration Guide: Ninject to Microsoft.Extensions.DependencyInjection

## Overview

The `RawRabbit.DependencyInjection.Ninject` package has been deprecated in RawRabbit 3.0 and will be removed in a future version. This guide helps you migrate to `RawRabbit.DependencyInjection.ServiceCollection`, which uses Microsoft's built-in dependency injection container.

## Why Migrate?

1. **Ninject Maintenance**: Ninject has no official .NET Core/.NET 5+ support and uses beta versions for .NET Standard
2. **Built-in Support**: Microsoft.Extensions.DependencyInjection is included with .NET and requires no additional dependencies
3. **Industry Standard**: Microsoft DI is the standard container for modern .NET applications
4. **Better Integration**: Seamless integration with ASP.NET Core and other .NET libraries
5. **Active Maintenance**: Actively maintained by Microsoft with regular updates

## Package Changes

### Before (RawRabbit 2.x with Ninject)

```xml
<PackageReference Include="RawRabbit.DependencyInjection.Ninject" Version="2.0.0" />
<PackageReference Include="Ninject" Version="3.3.6" />
```

### After (RawRabbit 3.0 with ServiceCollection)

```xml
<PackageReference Include="RawRabbit.DependencyInjection.ServiceCollection" Version="3.0.0" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="9.0.0" />
```

Note: `Microsoft.Extensions.DependencyInjection` is included by default in ASP.NET Core projects.

## Code Migration

### Basic Registration

**Before (Ninject):**

```csharp
using Ninject;
using RawRabbit.DependencyInjection.Ninject;
using RawRabbit.Instantiation;

var kernel = new StandardKernel();
kernel.RegisterRawRabbit(new RawRabbitOptions
{
    ClientConfiguration = new RawRabbitConfiguration
    {
        Hostnames = { "localhost" },
        Username = "guest",
        Password = "guest"
    }
});

var client = kernel.Get<IBusClient>();
```

**After (Microsoft.Extensions.DependencyInjection):**

```csharp
using Microsoft.Extensions.DependencyInjection;
using RawRabbit.DependencyInjection.ServiceCollection;
using RawRabbit.Instantiation;

var services = new ServiceCollection();
services.AddRawRabbit(new RawRabbitOptions
{
    ClientConfiguration = new RawRabbitConfiguration
    {
        Hostnames = { "localhost" },
        Username = "guest",
        Password = "guest"
    }
});

var serviceProvider = services.BuildServiceProvider();
var client = serviceProvider.GetRequiredService<IBusClient>();
```

### ASP.NET Core Integration

**Before (Ninject with ASP.NET Core):**

```csharp
// Startup.cs
using Ninject;
using Ninject.AspNetCore;

public void ConfigureServices(IServiceCollection services)
{
    // Had to use Ninject.AspNetCore bridge
    services.AddNinject();
}

public void Configure(IApplicationBuilder app)
{
    var kernel = app.ApplicationServices.GetService<IKernel>();
    kernel.RegisterRawRabbit(new RawRabbitOptions { /* config */ });
}
```

**After (Microsoft DI - Native ASP.NET Core):**

```csharp
// Program.cs (.NET 6+ minimal API)
using RawRabbit.DependencyInjection.ServiceCollection;

var builder = WebApplication.CreateBuilder(args);

// Direct integration with ASP.NET Core DI
builder.Services.AddRawRabbit(new RawRabbitOptions
{
    ClientConfiguration = new RawRabbitConfiguration
    {
        Hostnames = { "localhost" },
        Username = "guest",
        Password = "guest"
    }
});

var app = builder.Build();
app.Run();
```

Or with traditional Startup.cs:

```csharp
// Startup.cs
using RawRabbit.DependencyInjection.ServiceCollection;

public void ConfigureServices(IServiceCollection services)
{
    services.AddRawRabbit(new RawRabbitOptions { /* config */ });
}
```

### Custom Service Registration

**Before (Ninject):**

```csharp
var kernel = new StandardKernel();
kernel.RegisterRawRabbit();

// Register custom services
kernel.Bind<IMyService>().To<MyService>().InSingletonScope();
kernel.Bind<IMyRepository>().To<MyRepository>().InTransientScope();
```

**After (Microsoft DI):**

```csharp
var services = new ServiceCollection();
services.AddRawRabbit();

// Register custom services
services.AddSingleton<IMyService, MyService>();
services.AddTransient<IMyRepository, MyRepository>();
```

### Lifetime Scopes Mapping

| Ninject                  | Microsoft.Extensions.DI |
|--------------------------|-------------------------|
| `.InSingletonScope()`    | `.AddSingleton<>()`     |
| `.InTransientScope()`    | `.AddTransient<>()`     |
| `.InThreadScope()`       | `.AddScoped<>()`        |
| `.InRequestScope()`      | `.AddScoped<>()`        |

### Factory Pattern

**Before (Ninject):**

```csharp
kernel.Bind<IBusClient>()
    .ToMethod(ctx => {
        var options = ctx.Kernel.Get<RawRabbitOptions>();
        return RawRabbitFactory.CreateSingleton(options);
    })
    .InSingletonScope();
```

**After (Microsoft DI):**

```csharp
services.AddSingleton<IBusClient>(provider => {
    var options = provider.GetRequiredService<RawRabbitOptions>();
    return RawRabbitFactory.CreateSingleton(options);
});
```

### Configuration from appsettings.json

**After (Microsoft DI with Configuration):**

```csharp
// appsettings.json
{
  "RawRabbit": {
    "Hostnames": ["localhost"],
    "Username": "guest",
    "Password": "guest",
    "VirtualHost": "/",
    "Port": 5672
  }
}

// Program.cs
using Microsoft.Extensions.Configuration;
using RawRabbit.DependencyInjection.ServiceCollection;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

var services = new ServiceCollection();
services.AddRawRabbit(new RawRabbitOptions
{
    ClientConfiguration = configuration
        .GetSection("RawRabbit")
        .Get<RawRabbitConfiguration>()
});
```

## Dependency Injection in Message Handlers

**Before (Ninject):**

```csharp
kernel.RegisterRawRabbit();
kernel.Bind<IOrderService>().To<OrderService>();

var client = kernel.Get<IBusClient>();
await client.SubscribeAsync<OrderCreated>(async (message) => {
    var orderService = kernel.Get<IOrderService>();
    await orderService.ProcessOrder(message);
});
```

**After (Microsoft DI):**

```csharp
services.AddRawRabbit();
services.AddScoped<IOrderService, OrderService>();

var serviceProvider = services.BuildServiceProvider();
var client = serviceProvider.GetRequiredService<IBusClient>();

await client.SubscribeAsync<OrderCreated>(async (message) => {
    using var scope = serviceProvider.CreateScope();
    var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
    await orderService.ProcessOrder(message);
});
```

## Common Pitfalls

### 1. Forgetting to Build ServiceProvider

**Wrong:**
```csharp
var services = new ServiceCollection();
services.AddRawRabbit();
var client = services.GetRequiredService<IBusClient>(); // ERROR: services is IServiceCollection
```

**Correct:**
```csharp
var services = new ServiceCollection();
services.AddRawRabbit();
var serviceProvider = services.BuildServiceProvider();
var client = serviceProvider.GetRequiredService<IBusClient>();
```

### 2. Not Using Scopes for Scoped Services

**Wrong (potential memory leaks):**
```csharp
var dbContext = serviceProvider.GetRequiredService<MyDbContext>(); // Don't do this for scoped services
```

**Correct:**
```csharp
using var scope = serviceProvider.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<MyDbContext>();
```

### 3. ASP.NET Core - Don't Build ServiceProvider Manually

**Wrong (in ASP.NET Core):**
```csharp
public void ConfigureServices(IServiceCollection services)
{
    var provider = services.BuildServiceProvider(); // Don't do this
    var client = provider.GetRequiredService<IBusClient>();
}
```

**Correct (in ASP.NET Core):**
```csharp
public void Configure(IApplicationBuilder app)
{
    // ASP.NET Core builds the provider for you
    var client = app.ApplicationServices.GetRequiredService<IBusClient>();
}
```

## Testing Changes

### Before (Ninject)

```csharp
[Test]
public void Should_Resolve_IBusClient()
{
    var kernel = new StandardKernel();
    kernel.RegisterRawRabbit();

    var client = kernel.Get<IBusClient>();
    Assert.IsNotNull(client);
}
```

### After (Microsoft DI)

```csharp
[Test]
public void Should_Resolve_IBusClient()
{
    var services = new ServiceCollection();
    services.AddRawRabbit();
    var serviceProvider = services.BuildServiceProvider();

    var client = serviceProvider.GetRequiredService<IBusClient>();
    Assert.IsNotNull(client);
}
```

## Performance Considerations

Microsoft.Extensions.DependencyInjection is generally faster than Ninject for most scenarios:

- **Startup Time**: Faster container build time
- **Resolution Speed**: Comparable or faster service resolution
- **Memory Usage**: Lower memory footprint

## Additional Resources

- [Microsoft.Extensions.DependencyInjection Documentation](https://learn.microsoft.com/dotnet/core/extensions/dependency-injection)
- [RawRabbit.DependencyInjection.ServiceCollection Package](https://www.nuget.org/packages/RawRabbit.DependencyInjection.ServiceCollection)
- [ADR 0005: Deprecated Package Strategy](/docs/adr/ADR%200005%20Deprecated%20Package%20Strategy.md)

## Support Timeline

- **RawRabbit 2.x**: Ninject support continues (security patches only)
- **RawRabbit 3.0**: Ninject package marked as deprecated (still installable)
- **RawRabbit 4.0**: Ninject package will be removed

## Alternative: Autofac

If Microsoft DI doesn't meet your needs, consider `RawRabbit.DependencyInjection.Autofac`:

```xml
<PackageReference Include="RawRabbit.DependencyInjection.Autofac" Version="3.0.0" />
```

Autofac provides more advanced features like:
- Keyed services
- Property injection
- Decorator pattern support
- Module system

## Need Help?

If you encounter issues during migration:

1. Check the [RawRabbit GitHub Issues](https://github.com/pardahlman/RawRabbit/issues)
2. Review the [ServiceCollection integration tests](https://github.com/pardahlman/RawRabbit/tree/master/test/RawRabbit.IntegrationTests)
3. Consult the [Microsoft DI documentation](https://learn.microsoft.com/dotnet/core/extensions/dependency-injection)
