﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;
using System.Threading.Tasks;
using SenseNet.Diagnostics;

namespace SenseNet.Workflow.Tests.Implementations
{
    internal class ObserverTracer : ISnTracer
    {
        private string[] _triggers;
        private Action<string> _callback;

        public ObserverTracer(string[] triggers, Action<string> callback)
        {
            _triggers = triggers;
            _callback = callback;
        }

        public void Write(string line)
        {
            if (Match(line))
                _callback(line);
        }

        public void Flush()
        {
            // do nothing
        }

        private bool Match(string line)
        {
            var message = line.Split('\t').Last();
            return _triggers.Any(s => message.Contains(s));
        }
    }
}
