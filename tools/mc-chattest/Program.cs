// Her Minecraft side alone: MinecraftBodyService, the real one with its pools and rules, against the local test server,
// with Discord left offline (no token here; the service only posts when the client is connected). She's allowed to
// play and sent in at once. Every line pool is written to <out>/pools.json first, so a test can tell which pool each of
// her in-game lines came from. Runs until stdin closes or says « quit ».
//   dotnet run --project tools/mc-chattest -- <out dir>
using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Services;

var outDir = Path.GetFullPath(args.Length > 0 ? args[0] : "mc-chattest-out");
Directory.CreateDirectory(outDir);
var repo = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));

var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
{
    ["Minecraft:Host"] = Environment.GetEnvironmentVariable("MC_HOST") ?? "127.0.0.1",
    ["Minecraft:Port"] = Environment.GetEnvironmentVariable("MC_PORT") ?? "25599",
    ["Minecraft:Username"] = "SYNCS",
    ["Minecraft:Owner"] = "Papa",
    ["Minecraft:Tata"] = "Tata",
    ["Minecraft:BodyScript"] = Path.Combine(repo, "ProjectSYNCS", "MinecraftBody", "index.js"),
    // Its state file (the « allowed » switch, places) lands next to this database path, out of the repo.
    ["Database:Path"] = Path.Combine(outDir, "test.db"),
}).Build();

// The pools, by name: every string[] field of BotResponses, and every dictionary of them (key appended).
var responses = typeof(MinecraftBodyService).Assembly.GetType("ProjectSYNCS.Services.BotResponses")
                ?? throw new InvalidOperationException("BotResponses not found");
var pools = new Dictionary<string, string[]>();
foreach (var field in responses.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
{
    var value = field.GetValue(null);
    if (value is string[] lines) pools[field.Name] = lines;
    else if (value is IDictionary dict)
        foreach (DictionaryEntry e in dict)
            if (e.Value is string[] some) pools[$"{field.Name}[{e.Key}]"] = some;
}
File.WriteAllText(Path.Combine(outDir, "pools.json"), JsonSerializer.Serialize(pools));
Console.WriteLine($"pools: {pools.Count} written to {Path.Combine(outDir, "pools.json")}");

// The Pi's temperature is all the chat reads of PiHealthService: built without its constructor (it wants the database,
// the guild config, the breakdown), with its lock and a reading of 52 °C.
var health = (PiHealthService)RuntimeHelpers.GetUninitializedObject(typeof(PiHealthService));
typeof(PiHealthService).GetField("_gate", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(health, new object());
typeof(PiHealthService).GetField("_currentMilli", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(health, (int?)52000);

using var logs = LoggerFactory.Create(b => b.AddSimpleConsole(o => { o.SingleLine = true; o.TimestampFormat = "HH:mm:ss "; }));
var service = new MinecraftBodyService(new DiscordSocketClient(), new ResponsePicker(), health, config,
    logs.CreateLogger<MinecraftBodyService>());

using var stop = new CancellationTokenSource();
Console.WriteLine(service.SetAllowed(true));
await service.StartAsync(stop.Token);
await Task.Delay(2000);
Console.WriteLine(service.ForceJoin());

string? line;
while ((line = Console.ReadLine()) is not null && line.Trim() != "quit")
    if (line.Trim() == "status") Console.WriteLine(service.StatusText());
Console.WriteLine(service.ForceLeave());
await Task.Delay(3000);
stop.Cancel();
await service.StopAsync(CancellationToken.None);
