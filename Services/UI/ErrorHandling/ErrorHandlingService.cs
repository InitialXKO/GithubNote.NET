using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace GithubNote.NET.Services.UI.ErrorHandling
{
    public class ErrorDetails
    {
        public string Message { get; set; }
        public string Context { get; set; }
        public ErrorSeverity Severity { get; set; }
        public DateTime Timestamp { get; set; }
        public string CorrelationId { get; set; }
        public bool UserNotified { get; set; }
    }

    public enum ErrorSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }

    public interface IErrorHandlingService
    {
        event EventHandler<ErrorDetails> OnError;
        Task HandleErrorAsync(Exception ex, string context, ErrorSeverity severity = ErrorSeverity.Error);
        Task HandleErrorAsync(string message, string context, ErrorSeverity severity = ErrorSeverity.Error);
        Task<ErrorDetails> GetLastErrorAsync(string context);
        Task ClearErrorsAsync(string context);
        bool HasActiveErrors(string context);
    }

    public class ErrorHandlingService : IErrorHandlingService
    {
        private readonly ILogger<ErrorHandlingService> _logger;
        private readonly ConcurrentDictionary<string, ErrorDetails> _activeErrors;
        private readonly IPerformanceMonitor _performanceMonitor;

        public event EventHandler<ErrorDetails> OnError;

        public ErrorHandlingService(
            ILogger<ErrorHandlingService> logger,
            IPerformanceMonitor performanceMonitor)
        {
            _logger = logger;
            _performanceMonitor = performanceMonitor;
            _activeErrors = new ConcurrentDictionary<string, ErrorDetails>();
        }

        public async Task HandleErrorAsync(Exception ex, string context, ErrorSeverity severity = ErrorSeverity.Error)
        {
            var startTime = DateTime.UtcNow;
            try
            {
                var errorDetails = new ErrorDetails
                {
                    Message = ex.Message,
                    Context = context,
                    Severity = severity,
                    Timestamp = DateTime.UtcNow,
                    CorrelationId = Guid.NewGuid().ToString()
                };

                LogError(errorDetails, ex);
                _activeErrors.AddOrUpdate(context, errorDetails, (_, _) => errorDetails);
                OnError?.Invoke(this, errorDetails);

                await Task.CompletedTask;
            }
            finally
            {
                _performanceMonitor.TrackOperation(
                    "ErrorHandlingService.HandleError",
                    DateTime.UtcNow - startTime);
            }
        }

        public async Task HandleErrorAsync(string message, string context, ErrorSeverity severity = ErrorSeverity.Error)
        {
            var startTime = DateTime.UtcNow;
            try
            {
                var errorDetails = new ErrorDetails
                {
                    Message = message,
                    Context = context,
                    Severity = severity,
                    Timestamp = DateTime.UtcNow,
                    CorrelationId = Guid.NewGuid().ToString()
                };

                LogError(errorDetails);
                _activeErrors.AddOrUpdate(context, errorDetails, (_, _) => errorDetails);
                OnError?.Invoke(this, errorDetails);

                await Task.CompletedTask;
            }
            finally
            {
                _performanceMonitor.TrackOperation(
                    "ErrorHandlingService.HandleError",
                    DateTime.UtcNow - startTime);
            }
        }

        public async Task<ErrorDetails> GetLastErrorAsync(string context)
        {
            return await Task.FromResult(_activeErrors.TryGetValue(context, out var error) ? error : null);
        }

        public async Task ClearErrorsAsync(string context)
        {
            _activeErrors.TryRemove(context, out _);
            await Task.CompletedTask;
        }

        public bool HasActiveErrors(string context)
        {
            return _activeErrors.ContainsKey(context);
        }

        private void LogError(ErrorDetails errorDetails, Exception ex = null)
        {
            var logMessage = $"Error in {errorDetails.Context}: {errorDetails.Message} (Correlation ID: {errorDetails.CorrelationId})";
            
            switch (errorDetails.Severity)
            {
                case ErrorSeverity.Info:
                    _logger.LogInformation(ex, logMessage);
                    break;
                case ErrorSeverity.Warning:
                    _logger.LogWarning(ex, logMessage);
                    break;
                case ErrorSeverity.Error:
                    _logger.LogError(ex, logMessage);
                    break;
                case ErrorSeverity.Critical:
                    _logger.LogCritical(ex, logMessage);
                    break;
            }
        }
    }
}
