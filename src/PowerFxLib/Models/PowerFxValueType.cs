using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerFxLib.Models;

public enum PowerFxValueType
{
    Null,
    Number,
    String,
    Boolean,
    DateTime,
    Set,
    Error,
    Formula,
    Record,
    Color,
    Guid
}