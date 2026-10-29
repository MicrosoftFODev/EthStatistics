using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EthStatistics.Models;

namespace EthStatistics.Services
{
    public sealed class PythonMarketDataService
    {
        private static readonly JsonSerializerOptions
            JsonOptions =
                new()
                {
                    PropertyNameCaseInsensitive =
                        true
                };


        // ==========================================
        // METADATA
        // ==========================================

        public Task<MarketMetadataResult>
            GetMetadataAsync(
                CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<MarketMetadataResult>(
                new[]
                {
                    "--mode",
                    "metadata"
                },
                cancellationToken
            );
        }


        // ==========================================
        // MARKET DATA
        // ==========================================

        public Task<MarketDataResult>
            GetMarketDataAsync(
                string pair,
                int interval,
                CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(pair))
            {
                throw new ArgumentException(
                    "A market pair is required.",
                    nameof(pair)
                );
            }

            if (interval <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(interval),
                    "The interval must be greater than zero."
                );
            }

            return ExecuteAsync<MarketDataResult>(
                new[]
                {
                    "--mode",
                    "ohlcv",

                    "--pair",
                    pair.Trim(),

                    "--interval",
                    interval.ToString(
                        CultureInfo.InvariantCulture
                    )
                },
                cancellationToken
            );
        }


        // ==========================================
        // EXECUTE PYTHON
        // ==========================================

        private static async Task<T> ExecuteAsync<T>(
            IReadOnlyList<string> arguments,
            CancellationToken cancellationToken)
            where T : class
        {
            string scriptPath =
                ResolveScriptPath();

            var startInfo =
                new ProcessStartInfo
                {
                    FileName =
                        "py",

                    UseShellExecute =
                        false,

                    RedirectStandardOutput =
                        true,

                    RedirectStandardError =
                        true,

                    CreateNoWindow =
                        true,

                    WorkingDirectory =
                        Path.GetDirectoryName(
                            scriptPath
                        )
                        ?? AppContext.BaseDirectory
                };


            // Use the installed Python 3 version.
            // Do not force Python 3.14 unless that
            // specific installation is required.

            startInfo.ArgumentList.Add(
                "-3"
            );

            startInfo.ArgumentList.Add(
                scriptPath
            );


            foreach (string argument in arguments)
            {
                startInfo.ArgumentList.Add(
                    argument
                );
            }


            using var process =
                new Process
                {
                    StartInfo =
                        startInfo
                };


            try
            {
                if (!process.Start())
                {
                    throw new InvalidOperationException(
                        "Python could not be started."
                    );
                }
            }
            catch (Exception error)
            {
                throw new InvalidOperationException(
                    "The Python process could not be started. " +
                    "Confirm that Python is installed and " +
                    "the 'py' launcher is available.",
                    error
                );
            }


            Task<string> outputTask =
                process.StandardOutput
                    .ReadToEndAsync(
                        cancellationToken
                    );

            Task<string> errorTask =
                process.StandardError
                    .ReadToEndAsync(
                        cancellationToken
                    );


            try
            {
                await process.WaitForExitAsync(
                    cancellationToken
                );
            }
            catch (OperationCanceledException)
            {
                TryTerminate(
                    process
                );

                throw;
            }


            string output =
                await outputTask;

            string standardError =
                await errorTask;


            output =
                output.Trim();

            standardError =
                standardError.Trim();


            if (string.IsNullOrWhiteSpace(output))
            {
                throw new InvalidOperationException(
                    string.IsNullOrWhiteSpace(
                        standardError
                    )
                        ? "Python returned no response."
                        : standardError
                );
            }


            T? result;

            try
            {
                result =
                    JsonSerializer.Deserialize<T>(
                        output,
                        JsonOptions
                    );
            }
            catch (JsonException error)
            {
                throw new InvalidOperationException(
                    "Python returned invalid JSON." +
                    Environment.NewLine +
                    Environment.NewLine +
                    "Standard output:" +
                    Environment.NewLine +
                    LimitText(output) +
                    Environment.NewLine +
                    Environment.NewLine +
                    "Standard error:" +
                    Environment.NewLine +
                    LimitText(standardError),
                    error
                );
            }


            if (result is null)
            {
                throw new InvalidOperationException(
                    "Python returned an empty JSON result."
                );
            }


            // Nonzero is acceptable only when Python
            // still returned a structured JSON result.
            // The result model carries Success/Error.

            return result;
        }


        // ==========================================
        // SCRIPT RESOLUTION
        // ==========================================

        private static string ResolveScriptPath()
        {
            string deployedPath =
                Path.Combine(
                    AppContext.BaseDirectory,
                    "Python",
                    "MarketData.py"
                );


            if (File.Exists(deployedPath))
            {
                return deployedPath;
            }


            // Development fallback based on the
            // path you showed:
            // EthStatistics\EthData\MarketData.py

            string developmentPath =
                Path.GetFullPath(
                    Path.Combine(
                        AppContext.BaseDirectory,
                        "..",
                        "..",
                        "..",
                        "EthData",
                        "MarketData.py"
                    )
                );


            if (File.Exists(developmentPath))
            {
                return developmentPath;
            }


            throw new FileNotFoundException(
                "MarketData.py was not found. " +
                "Expected either:" +
                Environment.NewLine +
                deployedPath +
                Environment.NewLine +
                "or:" +
                Environment.NewLine +
                developmentPath
            );
        }


        // ==========================================
        // PROCESS CLEANUP
        // ==========================================

        private static void TryTerminate(
            Process process)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(
                        entireProcessTree: true
                    );
                }
            }
            catch
            {
                // Best-effort cancellation cleanup.
            }
        }


        private static string LimitText(
            string value)
        {
            const int maximumLength =
                4000;

            if (string.IsNullOrEmpty(value))
            {
                return "(empty)";
            }

            if (value.Length <= maximumLength)
            {
                return value;
            }

            return
                value[..maximumLength] +
                Environment.NewLine +
                "... output truncated ...";
        }
    }
}