using Microsoft.Web.WebView2.Wpf;
using System.Collections.Concurrent;
using System.Text.Json;

namespace QualityControl.WPF
{
    public class WebViewMessenger(WebView2 webView)
                : IWebViewMessenger
    {
        private readonly WebView2 _webView = webView;

        private readonly JsonSerializerOptions _options =
            new()
            {
                PropertyNamingPolicy =
                    JsonNamingPolicy.CamelCase
            };

        private readonly ConcurrentDictionary<
            string,
            Func<object?, Task<object?>>>
            _handlers = new();

        public void RegisterHandler<
            TRequest,
            TResponse>(
            string messageType,
            Func<TRequest?, Task<TResponse>> handler)
        {
            _handlers[messageType] =
                async payload =>
                {
                    var request =
                        Deserialize<TRequest>(
                            payload);

                    return await handler(request);
                };
        }

        public async Task ReceiveMessageAsync(
            string json)
        {
            var message =
                JsonSerializer.Deserialize<WebMessage>(
                    json,
                    _options);

            if (message == null)
                return;

            if (!_handlers.TryGetValue(
                    message.Type,
                    out var handler))
            {
                return;
            }

            var response =
                await handler(message.Payload);

            var responseMessage =
                new WebMessage
                {
                    IsResponse = true,
                    CorrelationId = message.Id,
                    Payload = response
                };

            var responseJson =
                JsonSerializer.Serialize(
                    responseMessage,
                    _options);

            _webView.CoreWebView2
                .PostWebMessageAsJson(
                    responseJson);
        }

        private T? Deserialize<T>(
            object? payload)
        {
            if (payload is JsonElement element)
            {
                return element.Deserialize<T>(
                    _options);
            }

            return default;
        }
    }
}
