using System;
using System.Collections.Generic;
using System.Text;

namespace Fontana.AI.Services
{
    public interface IChatService
    {
        Task<string> GetAiResponseAsync(string userMessage);
    }
}
