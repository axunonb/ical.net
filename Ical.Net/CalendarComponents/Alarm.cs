//
// Copyright ical.net project maintainers and contributors.
// Licensed under the MIT license.
//

using System;
using System.Collections.Generic;
using System.Linq;
using Ical.Net.DataTypes;
using Ical.Net.Evaluation;
using Ical.Net.Utility;

namespace Ical.Net.CalendarComponents;

/// <summary>
/// A class that represents an RFC 2445 VALARM component.
/// FIXME: move GetOccurrences() logic into an AlarmEvaluator.
/// </summary>
public class Alarm : CalendarComponent
{
    public virtual string? Action
    {
        get => Properties.Get<string>(AlarmAction.Key);
        set => Properties.Set(AlarmAction.Key, value);
    }

    public virtual Attachment? Attachment
    {
        get => Properties.Get<Attachment>("ATTACH");
        set => Properties.Set("ATTACH", value);
    }

    public virtual IList<Attendee> Attendees
    {
        get => Properties.GetMany<Attendee>("ATTENDEE");
        set => Properties.Set("ATTENDEE", value);
    }

    public virtual string? Description
    {
        get => Properties.Get<string>("DESCRIPTION");
        set => Properties.Set("DESCRIPTION", value);
    }

    public virtual Duration? Duration
    {
        get => Properties.Get<Duration>("DURATION");
        set => Properties.Set("DURATION", value);
    }

    public virtual int Repeat
    {
        get => Properties.Get<int>("REPEAT");
        set => Properties.Set("REPEAT", value);
    }

    public virtual string? Summary
    {
        get => Properties.Get<string>("SUMMARY");
        set => Properties.Set("SUMMARY", value);
    }

    public virtual Trigger? Trigger
    {
        get => Properties.Get<Trigger>(TriggerRelation.Key);
        set => Properties.Set(TriggerRelation.Key, value);
    }

    public Alarm()
    {
        Name = Components.Alarm;
    }

    /// <summary>
    /// Gets a streaming sequence of alarm occurrences for the given recurring component, <paramref name="rc"/>
    /// that occur at or after <paramref name="fromDate"/>.
    /// </summary>
    public virtual IEnumerable<AlarmOccurrence> GetOccurrences(IRecurringComponent rc, CalDateTime? fromDate, EvaluationOptions? options)
    {
        // Each base alarm occurrence is paired with its repetitions into a time-sorted inner sequence.
        // OrderedNestedMergeMany then merges all inner sequences into a single time-sorted stream.
        // Both the outer and inner sequences are ordered, so the outer can be consumed in a streaming
        // manner, allowing indefinite recurrence rules to be handled without materialising all
        // occurrences upfront.
        return GetOccurrencesUnrepeated(rc, fromDate, options)
            .Select(ao => new[] { ao }.Concat(GetRepeatedItems(ao)))
            .OrderedNestedMergeMany();
    }

    private IEnumerable<AlarmOccurrence> GetOccurrencesUnrepeated(IRecurringComponent rc, CalDateTime? fromDate, EvaluationOptions? options)
    {
        if (Trigger == null)
            yield break;

        // If the trigger is relative, it can recur right along with
        // the recurring items, otherwise, it happens once and
        // only once (at a precise time).
        if (Trigger.IsRelative)
        {
            if (fromDate == null)
                fromDate = rc.Start?.Copy();

            foreach (var o in rc.GetOccurrences(fromDate, options))
            {
                var dt = o.Period.StartTime;

                if (string.Equals(Trigger.Related, TriggerRelation.End, TriggerRelation.Comparison))
                {
                    dt = o.Period.EffectiveEndTime ?? throw new ArgumentException(
                        "Alarm trigger is relative to the END of the occurrence; however, the occurrence has no discernible end.");
                }

                yield return new AlarmOccurrence(this, dt.Add(Trigger.Duration!.Value), rc);
            }
        }
        else
        {
            var dt = Trigger?.DateTime?.Copy();
            if (dt != null)
                yield return new AlarmOccurrence(this, dt, rc);
        }
    }

    /// <summary>
    /// Polls the <see cref="Alarm"/> component for alarms that have been triggered
    /// since the provided <paramref name="start"/> date/time.  If <paramref name="start"/>
    /// is null, all triggered alarms will be returned.
    /// </summary>
    /// <param name="start">The earliest date/time to poll triggered alarms for.</param>
    /// <param name="options"></param>
    /// <returns>A sequence of <see cref="AlarmOccurrence"/> objects, each containing a triggered alarm.</returns>
    public virtual IEnumerable<AlarmOccurrence> Poll(CalDateTime? start, EvaluationOptions? options = null)
    {
        // Evaluate the alarms to determine the recurrences
        if (Parent is not RecurringComponent rc)
            return [];

        return GetOccurrences(rc, start, options);
    }

    /// <summary>
    /// Yields the repetitions that occur from the <c>REPEAT</c> and <c>DURATION</c>
    /// properties for a single base alarm occurrence.
    /// </summary>
    private IEnumerable<AlarmOccurrence> GetRepeatedItems(AlarmOccurrence ao)
    {
        if (ao.DateTime == null || ao.Component == null)
            yield break;

        // RFC 5545 section 3.8.6.2: REPEAT and DURATION must appear together.
        // Properties.Get<Duration> returns default(Duration) for an unset property (a non-null struct),
        // so check property existence rather than the value to distinguish "not set" from zero.
        if (Repeat <= 0 || !Properties.ContainsKey("DURATION"))
            yield break;

        var alarmTime = ao.DateTime.Copy();

        for (var j = 0; j < Repeat; j++)
        {
            alarmTime = alarmTime?.Add(Duration.Value);

            if (alarmTime != null)
                yield return new AlarmOccurrence(this, alarmTime.Copy(), ao.Component);
        }
    }
}
