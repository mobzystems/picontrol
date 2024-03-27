using System.Diagnostics;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var app_timeout = app.Configuration.GetValue<int>("AppSettings:Timeout", 30_000);
var app_exension = OperatingSystem.IsWindows() ? "cmd" : "sh";

app.Logger.LogInformation($"Script timeout is {app_timeout} ms, extension *.{app_exension}");

app.UseDefaultFiles();
app.UseStaticFiles();

// Map favicon to Not Found
// app.MapGet("/favicon.ico", () => TypedResults.NotFound());

app.MapGet("/run", (IWebHostEnvironment host) => GetAvailableScripts(host));

// The script route parameters must match "letters, digit, space or hyphen" wihtout an extension
app.MapGet("/run/{script:regex(^[a-zA-Z0-9- ]+$)}", async (
    string script,
    IWebHostEnvironment host
  ) => await RunScriptAsync(script, host, app.Logger)
);

app.Run();

IResult GetAvailableScripts(IWebHostEnvironment host) {
  return TypedResults.Json(
    Directory.GetFiles(Path.Combine(host.ContentRootPath, "scripts"), $"*.{app_exension}", SearchOption.TopDirectoryOnly)
      .Select(p => Path.GetFileNameWithoutExtension(p))
      .ToList(),
    new JsonContext()
  );
}

/// <summary>
/// Attempt to run the specified script from the 'scripts' directory
/// </summary>
async Task<IResult> RunScriptAsync(string script, IWebHostEnvironment host, ILogger logger)
{
  logger.LogInformation($"Starting script '{script}'...");

  var fullPath = $"{Path.Combine(host.ContentRootPath, "scripts", script)}.{app_exension}";
  logger.LogDebug($"Full file name is '{fullPath}'.");

  if (File.Exists(fullPath))
  {
    var startInfo = new ProcessStartInfo(fullPath, "") // No arguments
    {
      CreateNoWindow = true,
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      WorkingDirectory = host.ContentRootPath
    };

    logger.LogDebug($"Creating process for '{fullPath}'...");

    var process = Process.Start(startInfo);
    if (process == null)
    {
      logger.LogDebug($"Process could not be created for '{fullPath}'.");
      return TypedResults.BadRequest($"File could not be started: {script}");
    }

    logger.LogDebug($"Waiting for process '{fullPath}'...");

    string exception;

    var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(app_timeout));
    try
    {
      await process.WaitForExitAsync(timeout.Token);
      exception = "";
    }
    catch (OperationCanceledException)
    {
      logger.LogWarning($"Process '{fullPath}' timed out.");
      exception = "Timeout";
    }
    catch (Exception ex)
    {
      logger.LogError($"Error '{ex.Message}' occurred while running process '{fullPath}'");
      exception = ex.Message;
    }

    if (!process.HasExited)
    {
      logger.LogInformation($"Process '{fullPath}' did not finish. Killing...");
      process.Kill(true); // Also child processes
      logger.LogDebug($"Process '{fullPath}' killed.");
    }

    logger.LogDebug($"Reading output of process '{fullPath}'...");
    var output = await process.StandardOutput.ReadToEndAsync();
    logger.LogTrace($"Process '{fullPath}': {output}");

    var error = await process.StandardError.ReadToEndAsync();
    if (error.Length != 0)
    {
      logger.LogTrace($"Error '{fullPath}': {error}");
    }

    logger.LogInformation($"Returning {output.Length} chars output, {error.Length} chars error, exception is '{exception}'.");
    return TypedResults.Json(new RunResult
    {
      Output = output,
      Error = error,
      Exception = exception
    }, new JsonContext());
  }

  logger.LogWarning($"File does not exist: '{fullPath}");
  return TypedResults.Content($"File not found: {script}", "text/plain", System.Text.Encoding.UTF8, 404);
  // This will return JSON content in the response, i.e. "..."
  // return TypedResults.NotFound<string>($"File not found: {script}");
}

struct RunResult
{
  [JsonPropertyName("output")]
  public string Output { init; get; }

  [JsonPropertyName("error")]
  public string Error { init; get; }

  [JsonPropertyName("exception")]
  public string Exception { init; get; }
}

[JsonSerializable(typeof(RunResult))]
[JsonSerializable(typeof(List<string>))]
partial class JsonContext : JsonSerializerContext { }