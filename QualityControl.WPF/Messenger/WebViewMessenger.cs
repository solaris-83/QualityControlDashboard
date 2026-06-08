using Microsoft.Web.WebView2.Wpf;
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

        public void RegisterHandler<
            TRequest,
            TResponse>(
            string messageType,
            Func<TRequest?, Task<TResponse>> handler)
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
                throw new InvalidOperationException(
                    "WebViewMessenger must be initialized with a WebView2 instance before receiving messages.");
            }

            var message = JsonSerializer.Deserialize<WebMessage>(json, _options);

            if (message == null)
                return;

            if (!_handlers.TryGetValue(message.Type, out var handler))
                return;

            var response = await handler(message.Payload);

            Publish(true, response, "", message.Id);
            var responseMessage =
                new WebMessage
                {
                    IsResponse = true,
                    CorrelationId = message.Id,
                    Payload = response
                };

            var responseJson = JsonSerializer.Serialize(responseMessage, _options);

            _webView.CoreWebView2.PostWebMessageAsJson(responseJson);
        }

        public void Publish(bool isResponse, object? payload, string type = "", string? correlationId = null)
        {
            var webMessage = new WebMessage
            {
                IsResponse = isResponse,
                CorrelationId = correlationId,
                Type = type,
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
