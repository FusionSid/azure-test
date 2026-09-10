using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;
using System.Text.Json;

public class SetSchedule
{
    [Function("SetSchedule")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "schedule/{deviceId}")] HttpRequestData req,
        string deviceId)
    {
        var body = await new StreamReader(req.Body).ReadToEndAsync();
        var payload = JsonSerializer.Deserialize<ScheduleRequest>(body);

        var registryManager = RegistryManager.CreateFromConnectionString(
            Environment.GetEnvironmentVariable("IoTHubServiceConnectionString"));

        var twin = await registryManager.GetTwinAsync(deviceId);
        var patch = new TwinCollection();
        patch["scheduledStartTime"] = payload?.startTime;
        await registryManager.UpdateTwinAsync(deviceId, new Twin { Properties = { Desired = patch } }, twin.ETag);

        var response = req.CreateResponse();
        await response.WriteAsJsonAsync(new { status = "scheduled", startTime = payload?.startTime });
        return response;
    }
}

public class ScheduleRequest
{
    public string? startTime { get; set; }
}