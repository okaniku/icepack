﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icepack;

/// <summary> Allows a class or struct to be serialized/deserialized. </summary>
[AttributeUsage(AttributeTargets.Field, Inherited = true)]
public sealed class SerializableFieldAttribute : Attribute { }
