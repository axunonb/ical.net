//
// Copyright ical.net project maintainers and contributors.
// Licensed under the MIT license.
//

#nullable enable
using System;
using System.IO;
using System.Linq;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using NUnit.Framework;

namespace Ical.Net.Tests;

[TestFixture]
public class AlarmTests
{
    #region Examples from RFC 5545

    [Test]
    public void ExactTimeAlarmWithRepeat()
    {
        CalendarEvent e = new()
        {
            Start = new CalDateTime(1997, 3, 18)
        };

        var valarm = """
            BEGIN:VALARM
            TRIGGER;VALUE=DATE-TIME:19970317T133000Z
            REPEAT:4
            DURATION:PT15M
            ACTION:AUDIO
            ATTACH;FMTTYPE=audio/basic:ftp://example.com/pub/
                sounds/bell-01.aud
            END:VALARM
            """;

        var alarm = SimpleDeserializer.Default
            .Deserialize(new StringReader(valarm))
            .Cast<Alarm>()
            .Single();

        e.Alarms.Add(alarm);

        var results = e.PollAlarms(new CalDateTime(1997, 3, 10), null)
            .Select(x => x.DateTime)
            .ToList();

        var expectedAlarms = new[]
        {
            new CalDateTime(new DateTime(1997, 3, 17, 13, 30, 0, DateTimeKind.Utc)),
            new CalDateTime(new DateTime(1997, 3, 17, 13, 45, 0, DateTimeKind.Utc)),
            new CalDateTime(new DateTime(1997, 3, 17, 14, 0, 0, DateTimeKind.Utc)),
            new CalDateTime(new DateTime(1997, 3, 17, 14, 15, 0, DateTimeKind.Utc)),
            new CalDateTime(new DateTime(1997, 3, 17, 14, 30, 0, DateTimeKind.Utc)),
        };

        Assert.That(results, Is.EquivalentTo(expectedAlarms));
    }

    [Test]
    public void RelativeAlarmWithRepeat()
    {
        CalendarEvent e = new()
        {
            Start = new CalDateTime(1997, 3, 18, 8, 30, 0, "America/New_York")
        };

        var valarm = """
            BEGIN:VALARM
            TRIGGER:-PT30M
            REPEAT:2
            DURATION:PT15M
            ACTION:DISPLAY
            DESCRIPTION:Breakfast meeting with executive
                team at 8:30 AM EST.
            END:VALARM
            """;

        var alarm = SimpleDeserializer.Default
            .Deserialize(new StringReader(valarm))
            .Cast<Alarm>()
            .Single();

        e.Alarms.Add(alarm);

        var results = e.PollAlarms(new CalDateTime(1997, 3, 18), null)
            .Select(x => x.DateTime)
            .ToList();

        var expectedAlarms = new[]
        {
            new CalDateTime(1997, 3, 18, 8, 0, 0, "America/New_York"),
            new CalDateTime(1997, 3, 18, 8, 15, 0, "America/New_York"),
            new CalDateTime(1997, 3, 18, 8, 30, 0, "America/New_York"),
        };

        Assert.That(results, Is.EquivalentTo(expectedAlarms));
    }

    [Test]
    public void RelativeAlarmDaysBefore()
    {
        Todo todo = new()
        {
            Start = new CalDateTime(1997, 3, 18, 7, 30, 0, "America/New_York"),
            Due = new CalDateTime(1997, 3, 18, 8, 30, 0, "America/New_York"),
        };

        var valarm = """
            BEGIN:VALARM
            TRIGGER;RELATED=END:-P2D
            ACTION:EMAIL
            ATTENDEE:mailto:john_doe@example.com
            SUMMARY:*** REMINDER: SEND AGENDA FOR WEEKLY STAFF MEETING ***
            DESCRIPTION:A draft agenda needs to be sent out to the attendees
              to the weekly managers meeting (MGR-LIST). Attached is a
              pointer the document template for the agenda file.
            ATTACH;FMTTYPE=application/msword:http://example.com/
             templates/agenda.doc
            END:VALARM
            """;

        var alarm = SimpleDeserializer.Default
            .Deserialize(new StringReader(valarm))
            .Cast<Alarm>()
            .Single();

        todo.Alarms.Add(alarm);

        var results = todo.PollAlarms(new CalDateTime(1997, 3, 10), new CalDateTime(1997, 3, 20))
            .Select(x => x.DateTime)
            .ToList();

        var expectedAlarms = new[]
        {
            new CalDateTime(1997, 3, 16, 8, 30, 0, "America/New_York"),
        };

        Assert.That(results, Is.EquivalentTo(expectedAlarms));
    }

    #endregion


    [Test]
    public void AlarmWithExactTime()
    {
        CalendarEvent e = new()
        {
            Start = new CalDateTime(2026, 4, 7)
        };

        e.Alarms.Add(new Alarm()
        {
            Trigger = new Trigger
            {
                DateTime = new CalDateTime(new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc))
            }
        });

        var alarmOccurrences = e.PollAlarms(e.Start, e.Start.AddDays(1))
            .Select(x => x.DateTime!)
            .ToList();

        var expectedAlarms = new[]
        {
            new CalDateTime(new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc)),
        };

        Assert.That(alarmOccurrences, Is.EquivalentTo(expectedAlarms));
    }

    [Test]
    public void RecurringAlarm()
    {
        CalendarEvent e = new()
        {
            Start = new CalDateTime(2026, 4, 7),
            RecurrenceRule = new RecurrencePattern(FrequencyType.Weekly, 1)
        };

        e.Alarms.Add(new Alarm()
        {
            Trigger = new Trigger(new Duration(days: -1))
        });

        var alarmOccurrences = e.PollAlarms(e.Start, e.Start.AddDays(21))
            .Select(x => x.DateTime!)
            .ToList();

        var expectedAlarms = new[]
        {
            new CalDateTime(2026, 4, 6),
            new CalDateTime(2026, 4, 13),
            new CalDateTime(2026, 4, 20),
            new CalDateTime(2026, 4, 27)
        };

        Assert.That(alarmOccurrences, Is.EquivalentTo(expectedAlarms));
    }

    [Test]
    public void AlarmWithoutParentIsEmpty()
    {
        var alarm = new Alarm()
        {
            Trigger = new()
            {
                DateTime = new CalDateTime(new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc))
            }
        };

        var occurrences = alarm.Poll(null, null);

        Assert.That(occurrences, Is.Empty);
    }

    [Test]
    public void RecurringAlarm_NegativeTrigger_BoundaryOccurrenceShouldBeIncluded()
    {
        // BUG in Alarm.GetOccurrences (Alarm.cs):
        //   TakeWhileBefore(endDate) is applied to component occurrence START times, not to
        //   alarm fire times. For a negative trigger such as TRIGGER:-P1D, the component
        //   occurrence that starts AT endDate fires its alarm at (endDate - 1 day), which IS
        //   within the polling window [startTime, endDate). However, TakeWhileBefore(endDate)
        //   excludes that component occurrence because its start time equals endDate, so the
        //   alarm for that occurrence is silently dropped.
        //
        // FIX NEEDED in Alarm.GetOccurrences:
        //   Expand the upper bound used to query component occurrences to
        //   (endDate - triggerDuration), so that occurrences whose alarms fire before endDate
        //   are not excluded. For a negative trigger the Duration is negative, so subtracting
        //   it produces a later date, correctly widening the component window.

        CalendarEvent e = new()
        {
            Start = new CalDateTime(2026, 4, 7, 9, 0, 0, "UTC"),
            RecurrenceRule = new RecurrencePattern(FrequencyType.Weekly, 1)
        };

        e.Alarms.Add(new Alarm
        {
            Trigger = new Trigger(new Duration(days: -1))
        });

        // Weekly occurrences: Apr 7, Apr 14, Apr 21, Apr 28, ...
        // Alarm fires 1 day before:  Apr 6, Apr 13, Apr 20, Apr 27, ...
        //
        // The Apr 28 occurrence fires its alarm on Apr 27 — within [Apr 7, Apr 28 09:00).
        // Bug: TakeWhileBefore(Apr 28 09:00) excludes the Apr 28 occurrence because
        //      Apr 28 09:00 is NOT strictly before Apr 28 09:00, so the Apr 27 alarm is
        //      never generated.
        var results = e.PollAlarms(
                new CalDateTime(2026, 4, 7, 9, 0, 0, "UTC"),
                new CalDateTime(2026, 4, 28, 9, 0, 0, "UTC"))
            .Select(x => x.DateTime!)
            .OrderBy(x => x)
            .ToList();

        // Currently fails: returns [Apr 6, Apr 13, Apr 20] — Apr 27 is missing.
        Assert.That(results, Is.EqualTo(new[]
        {
            new CalDateTime(2026, 4,  6, 9, 0, 0, "UTC"), // alarm for Apr 7  occurrence
            new CalDateTime(2026, 4, 13, 9, 0, 0, "UTC"), // alarm for Apr 14 occurrence
            new CalDateTime(2026, 4, 20, 9, 0, 0, "UTC"), // alarm for Apr 21 occurrence
            new CalDateTime(2026, 4, 27, 9, 0, 0, "UTC"), // alarm for Apr 28 occurrence — currently MISSING
        }));
    }

    [Test]
    public void RecurringAlarm_WithRepeat_EndTimeCutoffDoesNotDropEarlierRepetitions()
    {
        // BUG in RecurringComponent.PollAlarms (RecurringComponent.cs):
        //   PollAlarms applies the endTime filter with TakeWhile instead of Where.
        //   After Alarm.AddRepeatedItems, the alarm list is ordered by grouping, not by time:
        //     [base₁, base₂, rep₁₁, rep₁₂, rep₂₁, rep₂₂]
        //   If base₂ is >= endTime the TakeWhile stops immediately, never evaluating rep₁₁
        //   or rep₁₂, which may be perfectly valid in-range alarms for occurrence 1.
        //
        // FIX NEEDED in RecurringComponent.PollAlarms:
        //   Replace TakeWhile with Where for the endTime predicate, since the alarm list
        //   returned by Alarm.Poll is not guaranteed to be sorted by fire time.

        CalendarEvent e = new()
        {
            Start = new CalDateTime(2026, 4, 7, 9, 0, 0, "UTC"),
            RecurrenceRule = new RecurrencePattern(FrequencyType.Weekly, 1)
        };

        // TRIGGER:+PT1H, REPEAT:2, DURATION:PT1H
        // Apr  7 09:00 occurrence → alarms: Apr  7 10:00, 11:00, 12:00
        // Apr 14 09:00 occurrence → alarms: Apr 14 10:00, 11:00, 12:00
        e.Alarms.Add(new Alarm
        {
            Trigger = new Trigger(new Duration(hours: 1)),
            Repeat = 2,
            Duration = new Duration(hours: 1)
        });

        // endTime = Apr 14 09:30 UTC.
        // Apr 14 component start (09:00) < Apr 14 09:30 → included by TakeWhileBefore.
        // Its base alarm fires at Apr 14 10:00 → should be excluded by the endTime filter.
        //
        // AddRepeatedItems produces: [Apr 7 10:00, Apr 14 10:00, Apr 7 11:00, Apr 7 12:00, ...]
        // TakeWhile(< Apr 14 09:30): Apr 7 10:00 ✓ | Apr 14 10:00 ✗ → STOPS here.
        // Apr 7 11:00 and Apr 7 12:00 are never evaluated, even though both are < Apr 14 09:30.
        var results = e.PollAlarms(
                new CalDateTime(2026, 4, 7, 9, 0, 0, "UTC"),
                new CalDateTime(2026, 4, 14, 9, 30, 0, "UTC"))
            .Select(x => x.DateTime!)
            .OrderBy(x => x)
            .ToList();

        // Currently fails: returns only [Apr 7 10:00]. Apr 7 11:00 and 12:00 are dropped.
        Assert.That(results, Is.EqualTo(new[]
        {
            new CalDateTime(2026, 4, 7, 10, 0, 0, "UTC"), // base alarm for Apr 7
            new CalDateTime(2026, 4, 7, 11, 0, 0, "UTC"), // 1st repetition — currently MISSING
            new CalDateTime(2026, 4, 7, 12, 0, 0, "UTC"), // 2nd repetition — currently MISSING
        }));
    }

    [Test]
    public void RecurringAlarm_WithRepeat_LongRepDuration_DoesNotDropLaterOccurrenceRepetitions()
    {
        CalendarEvent e = new()
        {
            Start = new CalDateTime(2026, 4, 7, 9, 0, 0, "UTC"),
            RecurrenceRule = new RecurrencePattern(FrequencyType.Weekly, 1)
        };

        // TRIGGER:PT0S (fires at component start), REPEAT:2, DURATION:P10D
        // Component window = endDate - Duration.Zero = Apr 25 → includes Apr 7, Apr 14, Apr 21.
        // Apr  7 occurrence => alarms: Apr  7, Apr 17, Apr 27
        // Apr 14 occurrence => alarms: Apr 14, Apr 24, May  4
        // Apr 21 occurrence => alarms: Apr 21, May  1, May 11
        // AddRepeatedItems list (NOT time-sorted):
        //   [Apr 7, Apr 14, Apr 21, Apr 17, Apr 27, Apr 24, May 4, May 1, May 11]
        e.Alarms.Add(new Alarm
        {
            Trigger = new Trigger(Duration.Zero),
            Repeat = 2,
            Duration = new Duration(days: 10)
        });

        var results = e.PollAlarms(
                new CalDateTime(2026, 4, 7, 9, 0, 0, "UTC"),
                new CalDateTime(2026, 4, 25, 9, 0, 0, "UTC"))
            .Select(x => x.DateTime!)
            .OrderBy(x => x)
            .ToList();

        Assert.That(results, Is.EqualTo(new[]
        {
            new CalDateTime(2026, 4,  7, 9, 0, 0, "UTC"),
            new CalDateTime(2026, 4, 14, 9, 0, 0, "UTC"),
            new CalDateTime(2026, 4, 17, 9, 0, 0, "UTC"),
            new CalDateTime(2026, 4, 21, 9, 0, 0, "UTC"),
            new CalDateTime(2026, 4, 24, 9, 0, 0, "UTC"), // currently MISSING
        }));
    }

    [Test]
    public void RecurringAlarm_RelatedEnd_ReturnsAlarmsRelativeToOccurrenceEnd()
    {
        // RELATED=END triggers fire relative to the END of each occurrence, not the start.
        // GetOccurrences uses (endDate - triggerDuration) as the component window, which is
        // only an approximation for RELATED=END (exact for RELATED=START).
        // This test verifies alarms are computed from EffectiveEndTime, not StartTime.

        CalendarEvent e = new()
        {
            Start = new CalDateTime(2026, 4, 7, 9, 0, 0, "UTC"),
            Duration = new Duration(hours: 1), // each occurrence ends at 10:00 UTC
            RecurrenceRule = new RecurrencePattern(FrequencyType.Weekly, 1)
        };

        // Alarm fires 30 min before each occurrence END → each occurrence end - PT30M
        // Apr  7: end=10:00 => alarm  9:30
        // Apr 14: end=10:00 => alarm  9:30
        // Apr 21: end=10:00 => alarm  9:30
        e.Alarms.Add(new Alarm
        {
            Trigger = new Trigger(new Duration(minutes: -30))
            {
                Related = TriggerRelation.End
            }
        });

        var results = e.PollAlarms(
                new CalDateTime(2026, 4, 7, 9, 0, 0, "UTC"),
                new CalDateTime(2026, 4, 22, 0, 0, 0, "UTC"))
            .Select(x => x.DateTime!)
            .OrderBy(x => x)
            .ToList();

        Assert.That(results, Is.EqualTo(new[]
        {
            new CalDateTime(2026, 4,  7, 9, 30, 0, "UTC"),
            new CalDateTime(2026, 4, 14, 9, 30, 0, "UTC"),
            new CalDateTime(2026, 4, 21, 9, 30, 0, "UTC"),
        }));
    }

    [Test]
    public void AlarmWithRepeatButNoDuration_ShouldNotProduceDuplicates()
    {
        // RFC 5545 section 3.8.6.2: REPEAT and DURATION must appear together.
        // AddRepeatedItems skips the Add() call when Duration is null, but still executes
        // the outer loop, appending `Repeat` identical copies of the base alarm time.
        // This test documents the expected behavior: no duplicates should be produced.

        CalendarEvent e = new()
        {
            Start = new CalDateTime(2026, 4, 7, 9, 0, 0, "UTC")
        };

        e.Alarms.Add(new Alarm
        {
            Trigger = new Trigger(new Duration(minutes: -15)),
            Repeat = 3
            // Duration intentionally omitted
        });

        var results = e.PollAlarms(null, null)
            .Select(x => x.DateTime!)
            .ToList();

        // Currently returns 4 alarms (base + 3 duplicates); should return exactly 1.
        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0], Is.EqualTo(new CalDateTime(2026, 4, 7, 8, 45, 0, "UTC")));
    }

    [Test]
    public void PollAlarms_StartTime_DoesNotFilterAlarmFireTimes()
    {
        // startTime passed to PollAlarms is used to begin iterating component occurrences,
        // NOT as a lower bound on alarm fire times. An alarm whose fire time is before
        // startTime is still returned if its component occurrence starts at/after startTime.
        // This is intentional: callers requiring a fire-time lower bound must filter themselves.

        CalendarEvent e = new()
        {
            Start = new CalDateTime(2026, 4, 7, 9, 0, 0, "UTC")
        };

        e.Alarms.Add(new Alarm
        {
            Trigger = new Trigger(new Duration(days: -1)) // fires Apr 6 for Apr 7 occurrence
        });

        var results = e.PollAlarms(
                new CalDateTime(2026, 4, 7, 9, 0, 0, "UTC"), // startTime = Apr 7
                new CalDateTime(2026, 4, 8, 9, 0, 0, "UTC"))
            .Select(x => x.DateTime!)
            .ToList();

        // Apr 6 alarm is returned even though it is before startTime (Apr 7).
        Assert.That(results, Is.EqualTo(new[]
        {
            new CalDateTime(2026, 4, 6, 9, 0, 0, "UTC"),
        }));
    }
}
