using Microsoft.Web.WebView2.Wpf;
using QualityControl.WPF.Exceptions;
using QualityControl.WPF.Models;
using System;
using System.Collections.Concurrent;
using System.Text.Json;

namespace QualityControl.WPF.Messenger
{
    public class WebViewMessenger : IWebViewMessenger
    {
        private WebView2? _webView;

        private readonly JsonSerializerOptions _options =
            new()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

        private readonly ConcurrentDictionary<string, Func<object?, Task<object?>>> _handlers = new();

        public void Initialize(WebView2 webView)
        {
            _webView = webView ?? throw new ArgumentNullException(nameof(webView));
        }

        public void RegisterHandler<TRequest, TResponse>(string messageType, Func<TRequest?, Task<TResponse>> handler)
        {
            _handlers[messageType] =
                async payload =>
                {
                    var request = Deserialize<TRequest>(payload);

                    return await handler(request);
                };
        }

        public async Task ReceiveMessageAsync(string json)
        {
            if (_webView?.CoreWebView2 == null)
            {
                throw new InvalidOperationException("WebViewMessenger must be initialized with a WebView2 instance before receiving messages.");
            }

            var message = JsonSerializer.Deserialize<WebMessageDto>(json, _options);

            if (message == null)
                return;

            if (!_handlers.TryGetValue(message.Name, out var handler))
                return;

            object? response = null;
            bool isError = false;
            try
            {
               response = await handler(message.Payload);
            }
            catch (ApplicationBaseException ex)
            {
                isError = true;
                response = new ErrorDto(ex.Message, ex.StackTrace, message.Id);
            }
            catch (Exception ex)
            {
                isError = true;
                response = new ErrorDto("An unexpected error occurred.", ex.StackTrace, message.Id);
                Console.Error.WriteLine($"Error handling message '{message.Name}': {ex}");
            }

            Publish(message.Type, response, isError, message.Name, message.Id); 
        }

        public void Publish(TypeEnum type, object? payload, bool isError, string name = "", string? correlationId = null)
        {
            var webMessage = new WebMessageDto
            {
                Type = type,
                CorrelationId = correlationId,
                IsError = isError,
                Name = name,
                Payload = payload
            };

            var responseJson = JsonSerializer.Serialize(webMessage, _options);

            _webView!.CoreWebView2.PostWebMessageAsJson(responseJson);
        }

        private T? Deserialize<T>(object? payload)
        {
            if (payload is JsonElement element)
            {
                return element.Deserialize<T>(_options);
            }

            return default;
        }
    }
}
