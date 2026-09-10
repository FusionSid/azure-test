using System.Text;
using System.Text.Json;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;   

double batteryLevel = 50.0;
bool isCharging = false;
string? scheduledStartTime = null;
bool hasChargedToday = false;

string? connectionString = Environment.GetEnvironmentVariable("DEVICE_CONNECTION_STRING");
if (string.IsNullOrEmpty(connectionString))
{
    Console.WriteLine("ERROR: Set the DEVICE_CONNECTION_STRING environment variable before running.");
    return;
}
using var deviceClient = DeviceClient.CreateFromConnectionString(connectionString, TransportType.Mqtt);
Console.WriteLine("Connecting to the hub");

// start charging handler
await deviceClient.SetMethodHandlerAsync("StartCharging", async (request, context) =>
{
    isCharging = true;
   
    Console.WriteLine("=== Received StartCharging Command!!! ===");
    return new MethodResponse(200);
}, null);

// stop charging handler
await deviceClient.SetMethodHandlerAsync("StopCharging", async (request, context) =>
{
    isCharging = false;

    Console.WriteLine("=== Received StopCarging Command!!! ===");
    return new MethodResponse(200);
}, null);


// charging scheduler handler
await deviceClient.SetDesiredPropertyUpdateCallbackAsync(async (desiredProperties, context) =>
{
    if (!desiredProperties.Contains("scheduledStartTime"))
    {
        return;
    }

    scheduledStartTime = desiredProperties["scheduledStartTime"];
    hasChargedToday = false; 
    Console.WriteLine($"=== Received a new charging schedule: {scheduledStartTime} ===");
}, null);


async void initialSchedule()
{
    // this will check if a schedule was added when this device was offline
    var twin = await deviceClient.GetTwinAsync();
    if (!twin.Properties.Desired.Contains("scheduledStartTime"))
    {
        return;
    }
    
    scheduledStartTime = twin.Properties.Desired["scheduledStartTime"];
    Console.WriteLine($"=== Loaded existing schedule: {scheduledStartTime} ===");
    
}

void simulateBatteryTick()
{
     const double drainAmountPerTick = 2;
    const double chargeAmountPerTick = 0.5;

    // if charging, charge 2% per tick
    if (isCharging && batteryLevel < 100)
    {
        batteryLevel += drainAmountPerTick;
        if (batteryLevel > 100) batteryLevel = 100; // clamp
    }
    // if not charging slowly drain it
    else if (!isCharging && batteryLevel > 0)
    {
        batteryLevel -= chargeAmountPerTick;
        if (batteryLevel < 0) batteryLevel = 0; // clamp
    }
}


initialSchedule();
while (true)
{
    simulateBatteryTick();

    // check if its time to charge due to an existing schedule
    if (scheduledStartTime != null && !isCharging && !hasChargedToday)
    {
        var now = DateTime.Now;
        if (TimeSpan.TryParse(scheduledStartTime, out var scheduledTime))
        {
            if (now.Hour == scheduledTime.Hours && now.Minute == scheduledTime.Minutes)
            {
                isCharging = true;
                hasChargedToday = true;
                Console.WriteLine("=== CHarging schedule has triggered: starting to charge automatically ===");
            }
        }
    }

    // build the telemetry payload
    var payload = new
    {
        deviceId = "cool-car-1",
        batteryLevel = Math.Round(batteryLevel, 1),
        isCharging = isCharging,
        timestamp = DateTime.UtcNow
    };

    string json = JsonSerializer.Serialize(payload);
    var message = new Message(Encoding.UTF8.GetBytes(json));

    // send message to iot hub
    await deviceClient.SendEventAsync(message);
    Console.WriteLine($"Sent: {json}");

    // update the twin database data
    var reportedProperties = new TwinCollection();
    reportedProperties["batteryLevel"] = Math.Round(batteryLevel, 1);
    reportedProperties["isCharging"] = isCharging;
    reportedProperties["lastUpdated"] = DateTime.UtcNow.ToString("o");
    await deviceClient.UpdateReportedPropertiesAsync(reportedProperties);

    await Task.Delay(5000); // this is basically our "tick"
}

