using System;
using System.Text.Json;
using System.Threading.Tasks;
using VelocityRivals;

static class RelayIntegration
{
    static async Task<JsonElement> Receive(RelayTransport connection)
    {
        DateTime limit = DateTime.UtcNow.AddSeconds(5);
        while (DateTime.UtcNow < limit)
        {
            if (connection.TryReceive(out string json)) return JsonDocument.Parse(json).RootElement.Clone();
            if (connection.Error != null) throw new Exception(connection.Error);
            await Task.Delay(10);
        }
        throw new TimeoutException("No relay packet received.");
    }
    public static async Task Run(string endpoint)
    {
        using var host = new RelayTransport(endpoint);
        host.Send("{\"kind\":\"create\",\"version\":3}");
        var created = await Receive(host);
        if (created.GetProperty("relay").GetString() != "created") throw new Exception("Host room creation failed.");
        string code = created.GetProperty("code").GetString();
        using var guest = new RelayTransport(endpoint);
        guest.Send(JsonSerializer.Serialize(new { kind = "join", version = 3, code }));
        if ((await Receive(host)).GetProperty("relay").GetString() != "paired" || (await Receive(guest)).GetProperty("relay").GetString() != "paired") throw new Exception("Pairing failed.");
        guest.Send("{\"kind\":\"hello\",\"version\":3,\"guestCar\":2}");
        if ((await Receive(host)).GetProperty("guestCar").GetInt32() != 2) throw new Exception("Guest message failed.");
        host.Send("{\"kind\":\"state\",\"version\":3,\"score0\":5}");
        if ((await Receive(guest)).GetProperty("score0").GetInt32() != 5) throw new Exception("Host message failed.");
        guest.Dispose();
        if ((await Receive(host)).GetProperty("relay").GetString() != "closed") throw new Exception("Disconnect notification failed.");
        Console.WriteLine("PASS actual game RelayTransport connects, creates/joins a room, exchanges packets and receives disconnect through the local Python relay. Unity gameplay and public hosting are not tested.");
    }
}
