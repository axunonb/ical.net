﻿//
// Copyright ical.net project maintainers and contributors.
// Licensed under the MIT license.
//

using System;

namespace Ical.Net.Collections;

public class ObjectEventArgs<T, TU> :
    EventArgs
{
    public T First { get; set; }
    public TU Second { get; set; }

    public ObjectEventArgs(T first, TU second)
    {
        First = first;
        Second = second;
    }
}
