using Microsoft.ApplicationInsights.Channel;

namespace EmailIntelligence.Tests.TestSupport;

public sealed class StubTelemetryChannel : ITelemetryChannel
{
    public List<ITelemetry> Sent { get; } = [];

    public bool? DeveloperMode { get; set; }
    public string EndpointAddress { get; set; } = string.Empty;

    public void Send(ITelemetry item) => Sent.Add(item);
    public void Flush() { }
    public void Dispose() { }
}
