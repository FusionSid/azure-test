using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Devices;

public class GetBatteryStatus
{
    [Function("GetBatteryStatus")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "battery/{deviceId}")] HttpRequestData req,
        string deviceId)
    {
        var registryManager = RegistryManager.CreateFromConnectionString(
            Environment.GetEnvironmentVariable("IoTHubServiceConnectionString"));

        var response = req.CreateResponse();

        try
        {
            var twin = await registryManager.GetTwinAsync(deviceId);
            var reported = twin.Properties.Reported; // the stuff device writes
            var desired = twin.Properties.Desired; // the schedule we wrote from function

            var result = new
            {
                deviceId = deviceId,
                batteryLevel = reported.Contains("batteryLevel") ? (double)reported["batteryLevel"] : 0,
                isCharging = reported.Contains("isCharging") ? (bool)reported["isCharging"] : false,
                lastUpdated = reported.Contains("lastUpdated") ? (string)reported["lastUpdated"] : null,
                scheduledStartTime = desired.Contains("scheduledStartTime") ? (string)desired["scheduledStartTime"] : null
            };

            await response.WriteAsJsonAsync(result);
        }
        catch (Exception ex)
        {
            response.StatusCode = System.Net.HttpStatusCode.NotFound;
            await response.WriteStringAsync($"Device not found or error: {ex.Message}");
        }

        return response;
    }
}