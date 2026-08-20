namespace Mes.Simulator;

public sealed class Worker(ILogger<Worker> logger, IConfiguration configuration)
    : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var enabled = configuration.GetValue("Simulator:Enabled", defaultValue: false);

        logger.LogInformation(
            "Simulator starting. Enabled={Enabled}. Equipment simulation lands in Sprint 8.",
            enabled);

        return Task.CompletedTask;
    }
}