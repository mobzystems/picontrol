using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

// Map favicon to Not Found
app.MapGet("/favicon.ico", () => TypedResults.NotFound());

// The script route parameters must match "letters, digit or hyphen" followed by .sh or .cmd
app.MapGet("/run/{script:regex(^[a-zA-Z0-9-]+.(sh|cmd)$)}", async (string script, IWebHostEnvironment host) => await RunScriptAsync(script, host, app.Logger));

app.Run();

/// <summary>
/// Attempt to run the specified script from the 'scripts' directory
/// </summary>
async Task<IResult> RunScriptAsync(string script, IWebHostEnvironment host, ILogger logger)
{
  logger.LogInformation($"Starting script '{script}'...");

  var fullPath = Path.Combine(host.ContentRootPath, "scripts", script);
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

    // THIS WORKS
    // try
    // {
    //   var taskIndex = Task.WaitAny(process.WaitForExitAsync(), Task.Delay(2000));
    //   logger.LogDebug($"Index {taskIndex} completed");
    // }
    // catch (Exception ex)
    // {
    //   logger.LogDebug($"Error '{ex.Message}' occurred while running process '{fullPath}'");
    //   // process.Kill();
    //   // logger.LogDebug($"Process '{fullPath}' killed.");
    // }

    // try
    // {
    //   await process.WaitForExitAsync().WaitAsync(TimeSpan.FromMilliseconds(2000));
    // }
    // catch 
    // {
    //   logger.LogDebug($"Process '{fullPath}' took to long to exit.");
    // }

    // process.WaitForExit();

    var output = "";
    var error = "";

    var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(2000));
    try
    {
      await process.WaitForExitAsync(timeout.Token);
    }
    catch (Exception ex)
    {
      logger.LogDebug($"Error '{ex.Message}' occurred while running process '{fullPath}'");
      // process.Kill();
      // logger.LogDebug($"Process '{fullPath}' killed.");
    }

    if (!process.HasExited)
    {
      logger.LogDebug($"Process '{fullPath}' did not finish. Killing...");
      process.Kill(true); // Also child processes
      logger.LogDebug($"Process '{fullPath}' killed.");
    } // else {
      logger.LogDebug($"Reading output of process '{fullPath}'...");
      output = await process.StandardOutput.ReadToEndAsync();
      logger.LogTrace($"Process '{fullPath}': {output}");

      error = await process.StandardError.ReadToEndAsync();
      if (error.Length != 0)
      {
        logger.LogTrace($"Error '{fullPath}': {error}");
      }
    // }

    logger.LogInformation($"Returning {output.Length} chars output, {error.Length} chars error.");
    return TypedResults.Json(new RunResult
    {
      Output = output,
      Error = error
    });
  }

  logger.LogWarning($"File does not exist: '{fullPath}");
  return TypedResults.Content($"File not found: {script}", "text/plain", System.Text.Encoding.UTF8, 404);
  // This will return JSON content in the response, i.e. "..."
  // return TypedResults.NotFound<string>($"File not found: {script}");
}

struct RunResult
{
  public string Output { init; get; }
  public string Error { init; get; }
}

