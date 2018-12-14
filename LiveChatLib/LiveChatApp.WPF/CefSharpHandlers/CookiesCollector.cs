using CefSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveChatApp.CefSharpHandlers
{
    class CookiesCollector :ICookieVisitor
    {
        public event Action<Cookie> SendCookie;

        public void Dispose() { }

        public bool Visit(Cookie cookie, int count, int total, ref bool deleteCookie)
        {
            deleteCookie = false;
            if (SendCookie != null)
            {
                SendCookie(cookie);
            }
            return true;
        }
    }
}
