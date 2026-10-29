using System.Diagnostics;
using System.IO;
using System.Text.Json;
using EthStatistics.Models;

namespace EthStatistics.Services
{
    public sealed class PythonMarketDataService
    {
        private static readonly JsonSerializerOptions JsonOptions =
            new()
            {
                PropertyNameCaseInsensitive = true
            };

        public Task<MarketMetadataResult> GetMetadataAsync(
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<MarketMetadataResult>(
                [
                    "--mode",
                    "metadata"
                ],
                cancellationToken
            );
        }

        public Task<MarketDataResult> GetMarketDataAsync(
            string pair,
            int interval,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<MarketDataResult>(
                [
                    "--mode",
                    "ohlcv",
                    "--pair",
                    pair,
                    "--interval",
                    interval.ToString()
                ],
                cancellationToken
            );
        }

        private static async Task<T> ExecuteAsync<T>(
            IReadOnlyList<string> arguments,
            CancellationToken cancellationToken)
            where T : class, new()
        {
            string scriptPath = Path.Combine(
                AppContext.BaseDirectory,
                "Python",
                "MarketData.py"
            );

            if (!File.Exists(scriptPath))
            {
                throw new FileNotFoundException(
                    "MarketData.py was not found.",
                    scriptPath
                );
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = "py",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            startInfo.ArgumentList.Add("-3.14");
            startInfo.ArgumentList.Add(scriptPath);

            foreach (string argument in arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            using var process = new Process
            {
                StartInfo = startInfo
            };

            process.Start();

            Task<string> outputTask =
                process.StandardOutput.ReadToEndAsync(
                    cancellationToken
                );

            Task<string> errorTask =
                process.StandardError.ReadToEndAsync(
                    cancellationToken
                );

            await process.WaitForExitAsync(
                cancellationToken
            );

            string output = await outputTask;
            string standardError = await errorTask;

            if (string.IsNullOrWhiteSpace(output))
            {
                throw new InvalidOperationException(
                    string.IsNullOrWhiteSpace(standardError)
                        ? "Python returned no response."
                        : standardError.Trim()
                );
            }

            T? result = JsonSerializer.Deserialize<T>(
                output,
                JsonOptions
            );

            if (result is null)
            {
                throw new InvalidOperationException(
                    "Python returned invalid JSON."
                );
            }

            return result;
        }
    }
}