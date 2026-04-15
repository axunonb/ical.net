//
// Copyright ical.net project maintainers and contributors.
// Licensed under the MIT license.
//

using System.Collections.Generic;
using Ical.Net.DataTypes;

namespace Ical.Net.CalendarComponents;

public interface IAlarmContainer
{
    /// <summary>
    /// A list of <see cref="Components.Alarm"/>s for this recurring component.
    /// </summary>
    ICalendarObjectList<Alarm> Alarms { get; }

    ///  <summary>
    ///  Gets a sequence of <see cref="AlarmOccurrence"/>s produced by all <see cref="Alarms"/>
    ///  on this component, with fire times in the range [<paramref name="startTime"/>, <paramref name="endTime"/>).
    ///  </summary>
    /// <param name="startTime">Lower bound (inclusive) on alarm fire times, or <c>null</c> for no lower bound.</param>
    /// <param name="endTime">Upper bound (exclusive) on alarm fire times, or <c>null</c> for no upper bound.</param>
    /// <returns>A sequence of <see cref="AlarmOccurrence"/> objects, one for each triggered alarm.</returns>
    IEnumerable<AlarmOccurrence> GetAlarmOccurrences(CalDateTime? startTime, CalDateTime? endTime);
}
