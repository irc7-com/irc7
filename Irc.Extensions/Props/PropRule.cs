﻿using Irc.Enumerations;
using Irc.Extensions.Interfaces;
using Irc.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Irc.Extensions.Props
{
    public class PropRule : IPropRule
    {
        public PropRule(string name, EnumChannelAccessLevel readAccessLevel, EnumChannelAccessLevel writeAccessLevel, string initialValue, bool readOnly = false)
        {
            Name = name;
            ReadAccessLevel = readAccessLevel;
            WriteLevel = writeAccessLevel;
            Value = initialValue;
            ReadOnly = readOnly;
        }

        public string Name { get; }
        public EnumChannelAccessLevel ReadAccessLevel { get; }
        public EnumChannelAccessLevel WriteLevel { get; }
        public string Value { get; set; }
        public bool ReadOnly { get; }

        public EnumIrcError SetValue(ChatObject source, string value)
        {
            throw new NotImplementedException();
        }
    }
}
