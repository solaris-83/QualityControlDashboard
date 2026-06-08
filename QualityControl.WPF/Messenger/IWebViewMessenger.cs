using Microsoft.Web.WebView2.Wpf;

namespace QualityControl.WPF.Messenger
{
    public interface IWebViewMessenger
    {
        void Initialize(WebView2 webView);
        
        void RegisterHandler<TRequest, TResponse>(
            string messageType,
            Func<TRequest?, Task<TResponse>> handler);

        Task ReceiveMessageAsync(string json);
        void Publish(bool isResponse, object? payload, string type = "", string? correlationId = null);
    }
}
