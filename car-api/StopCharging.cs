using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Devices;

public class StartCharging
{
    [Function("StartCharging")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "charging/{deviceId}/start")] HttpRequestData req,
        string deviceId)
    {
        var serviceClient = ServiceClient.CreateFromConnectionString(
            Environment.GetEnvironmentVariable("IoTHubServiceConnectionString"));

        var method = new CloudToDeviceMethod("StartCharging");
        method.SetPayloadJson("{}");

        var result = await serviceClient.InvokeDeviceMethodAsync(deviceId, method);

        var response = req.CreateResponse();
        await response.WriteAsJsonAsync(new { status = result.Status });
        return response;
    }
}