﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icepack
{
    internal static class Toolbox
    {
        public const ulong NULL_ID = 0;

        public static bool IsStruct(Type type)
        {
            return type.IsValueType && !type.IsPrimitive && !type.IsEnum && type != typeof(string) && type != typeof(decimal);
        }

        public static bool IsClass(Type type)
        {
            return type.IsClass && type != typeof(string);
        }

        public static string EscapeString(string str)
        {
            StringBuilder builder = new StringBuilder(str.Length * 2);
            foreach (char c in str)
            {
                if (",]\\".Contains(c))
                    builder.Append('\\');

                builder.Append(c);
            }

            return builder.ToString();
        }
    }
}