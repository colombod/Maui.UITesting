﻿using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Automation;
using Microsoft.Maui.Automation.Driver;
using Microsoft.Maui.Automation.Remote;
using Spectre.Console;
using System.Net;


//var driver = new AppDriverBuilder()
//	.AppFilename(@"C:\code\Maui.UITesting\samples\SampleMauiApp\bin\Debug\net7.0-android\com.companyname.samplemauiapp-Signed.apk")
//	.Device("Pixel_5_API_31")
//	//.Device("9B141FFAZ008CJ")
//	.Build();

var driver = new AppDriverBuilder()
	//.AppId("D05ADD49-B96D-49E5-979C-FA3A3F42F8E0_yn9kjvr01ms9j!App")
	//.DevicePlatform(Platform.Winappsdk)
	.AppFilename("/Users/redth/code/Maui.UITesting/samples/SampleMauiApp/bin/Debug/net7.0-ios/iossimulator-x64/SampleMauiApp.app")
	.ConfigureLogging(log =>
	{
		log.ClearProviders();
		log.AddConsole();
	})
	//.Device("83880AF2-46CC-4302-86E9-E17970E3B33D")
	.Device("Pixel_4_API_31")
	.ConfigureDriver(c => c.Set(ConfigurationKeys.GrpcHostLoggingEnabled, true))
	.Build();

Task<string?>? readTask = null;
CancellationTokenSource ctsMain = new CancellationTokenSource();

Console.CancelKeyPress += (s, e) =>
{
	ctsMain.Cancel();
};


await driver.Start();

var mappings = new Dictionary<string, Func<string[], Task>>
{
	{ "tree", Tree },
	{ "windows", Windows },
	{ "test", async (string[] args) =>
		{
			
			await driver
				.First(By.AutomationId("entryUsername"))
				.InputText("xamarin");

			await driver
				.First(By.AutomationId("entryPassword"))
				.InputText("1234");

			await driver
				.First(By.ContainingText("Login"))
				.Tap();

			await driver.None(By.AutomationId("entryUsername"));

			var label = await driver.First(By.ContainingText("Hello, World!"));

			var button = await driver.First(By.AutomationId("buttonOne"));

			await button.Tap();

			await driver.Screenshot();

			await driver.Any(By.Type("Label").ThenContainingText("Current count: 1"));

			Console.WriteLine(label.Text);
		}
	},
	{ "perf", async (string[] args) =>
		{
			var ind = new List<double>();

			var start = DateTime.UtcNow;
			for (int i = 0; i < 1000; i++)
			{
				var indstart = DateTime.UtcNow;
				var f = await driver.First(e => e.Type == "Label" && e.Text.Contains("count"));
				var t = f.Text;
				var indtotal = DateTime.UtcNow - indstart;
				ind.Add(indtotal.TotalMilliseconds);
			}
			var total = DateTime.UtcNow - start;

			var avg = ind.Average();
			Console.WriteLine($"Time: {total.TotalMilliseconds}  Avg: {avg}");
		}
	}
};

//await mappings["test"]();

while (!ctsMain.Token.IsCancellationRequested)
{
	var input = await ReadLineAsync(ctsMain.Token);

	try
	{
		foreach (var kvp in mappings)
		{
			var parts = input?.Split(' ');
			var cmd = parts?[0] ?? string.Empty;
			var extraParts = parts?.Skip(1)?.ToArray() ?? new string[0];

			if (input?.StartsWith(kvp.Key) ?? false)
			{
				_ = Task.Run(async () =>
				{
					try
					{
						await kvp.Value(extraParts);
					}
					catch (Exception ex)
					{
						AnsiConsole.WriteException(ex);
					}
				});

				break;
			}
		}
	}
	catch (Exception ex)
	{
		AnsiConsole.WriteException(ex);
	}

	if (input != null && (input.Equals("quit", StringComparison.OrdinalIgnoreCase)
		|| input.Equals("q", StringComparison.OrdinalIgnoreCase)
		|| input.Equals("exit", StringComparison.OrdinalIgnoreCase)))
	{
		ctsMain.Cancel();
	}
}

driver.Dispose();

Environment.Exit(0);

async Task Tree(params string[] args)
{
	IEnumerable<Element> children;

	if (args?.Length > 0 && Enum.TryParse<Platform>(args[0], true, out var p))
		children = await driver.GetElements(p);
	else
		children = await driver.GetElements();

	foreach (var w in children)
	{
		var tree = new Tree(w.ToTable(ConfigureTable));


		foreach (var d in w.Children)
		{
			PrintTree(tree, d, 1);
		}

		AnsiConsole.Write(tree);
	}
}

async Task Windows(params string[] args)
{
	var children = await driver.GetElements();

	foreach (var w in children)
	{
		var tree = new Tree(w.ToTable(ConfigureTable));

		AnsiConsole.Write(tree);
	}
}

void PrintTree(IHasTreeNodes node, Element element, int depth)
{
	var subnode = node.AddNode(element.ToTable(ConfigureTable));

	foreach (var c in element.Children)
		PrintTree(subnode, c, depth);
}

static void ConfigureTable(Table table)
{
	table.Border(TableBorder.Rounded);
}


async Task<string?> ReadLineAsync(CancellationToken cancellationToken = default)
{
	try
	{
		readTask = Task.Run(() => Console.ReadLine());

		await Task.WhenAny(readTask, Task.Delay(-1, cancellationToken));

		if (readTask.IsCompletedSuccessfully)
			return readTask.Result;
	}
	catch (Exception ex)
	{
		throw ex;
	}

	return null;

}