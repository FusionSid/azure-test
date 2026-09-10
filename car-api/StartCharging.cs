using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Devices;

public class StopCharging
{
    [Function("StopCharging")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "charging/{deviceId}/stop")] HttpRequestData req,
        string deviceId)
    {
        var serviceClient = ServiceClient.CreateFromConnectionString(
            Environment.GetEnvironmentVariable("IoTHubServiceConnectionString"));

        var method = new CloudToDeviceMethod("StopCharging");
        method.SetPayloadJson("{}");

        var result = await serviceClient.InvokeDeviceMethodAsync(deviceId, method);

        var response = req.CreateResponse();
        await response.WriteAsJsonAsync(new { status = result.Status });
        return response;
    }
}