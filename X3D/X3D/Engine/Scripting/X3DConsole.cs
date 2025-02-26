﻿using System;

namespace X3D.Engine
{
    /// <summary>
    ///     Console used by Scripting component
    ///     (via V8.Net Engine)
    /// </summary>
    public class X3DConsole
    {
        public static X3DConsole Current = new X3DConsole();
        public static string LastMessage;

        public void log(string message)
        {
            LastMessage = message;

            Console.WriteLine("(SCRIPT) {0}", message);
        }
    }
}