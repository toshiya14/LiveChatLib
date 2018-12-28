using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LiveChatLib
{
    public interface ILiveChatListener
    {
        void LoopListening(ref bool stoptoken);
        Task KeepMessage(MessageBase message);
    }
}
