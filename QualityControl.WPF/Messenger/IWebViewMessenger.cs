using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QualityControl.WPF.Messenger
{
    public interface IWebViewMessenger
    {
        void RegisterHandler<TRequest, TResponse>(
            string messageType,
            Func<TRequest?, Task<TResponse>> handler);

        Task ReceiveMessageAsync(
            string json);
    }
}
