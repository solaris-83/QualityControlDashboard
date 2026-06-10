using Microsoft.Web.WebView2.Wpf;
using QualityControl.WPF.Models;

namespace QualityControl.WPF.Messenger
{
    public interface IWebViewMessenger
    {
        void Initialize(WebView2 webView);
        
        void RegisterHandler<TRequest, TResponse>(string messageType, Func<TRequest?, Task<TResponse>> handler);

        Task ReceiveMessageAsync(string json);
        void Publish(TypeEnum type, object? payload, string name = "", string? correlationId = null);
    }
}
