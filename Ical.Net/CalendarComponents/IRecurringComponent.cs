﻿//
// Copyright ical.net project maintainers and contributors.
// Licensed under the MIT license.
//

using System.Collections.Generic;
using Ical.Net.DataTypes;

namespace Ical.Net.CalendarComponents;

public interface IRecurringComponent : IUniqueComponent, IRecurrable
{
    IList<Attachment> Attachments { get; set; }
    IList<string> Categories { get; set; }
    string? Class { get; set; }
    IList<string> Contacts { get; set; }
    CalDateTime? Created { get; set; }
    string? Description { get; set; }
    CalDateTime? LastModified { get; set; }
    int Priority { get; set; }
    IList<string> RelatedComponents { get; set; }
    int Sequence { get; set; }
    string? Summary { get; set; }
}
