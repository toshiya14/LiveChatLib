using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LiveChatLib
{
    public interface ILiveChatListener
    {
        bool StopListenToken { get; set; }
        Task LoopListening();
        Task KeepMessage(MessageBase message);
    }
}
