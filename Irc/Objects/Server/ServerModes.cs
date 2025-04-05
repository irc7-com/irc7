using Irc.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Irc.Objects.Collections;

namespace Irc.Objects.Server
{
    public class ServerModes : ModeCollection, IModeCollection
    {
        public ServerModes()
        {
            // No server modes
        }
    }
}
