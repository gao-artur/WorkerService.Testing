# WorkerService.Testing

Adoption of [Microsoft.AspNetCore.Mvc.Testing](https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-3.1) for Worker service. It's compatible with .Net Core 3.1.


## Installation

### Option 1 (preferred):
Pack `WorkerService.Testing` project in nuget package and install it using your preferred way.

### Option 2:
Add `WorkerService.Testing` as a project reference to integration test project and import `WorkerService.Testing.targets`. Add this line to the end of integration tests `csproj` file:

```xml
<Import Project="../WorkerService.Testing/WorkerService.Testing.targets" />
```

## Usage
The usage almost similar to [Microsoft.AspNetCore.Mvc.Testing](https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-3.1). The main difference is that you should call `StartAsync()` method to start the host. Check `WorkerService.Testing.IntegrationTests` project for usage example.
