﻿//
// Copyright ical.net project maintainers and contributors.
// Licensed under the MIT license.
//

using System;
using System.Collections.Generic;
using System.Linq;
using Ical.Net.Utility;

namespace Ical.Net.DataTypes;

public class Attendee : EncodableDataType
{
    private Uri? _sentBy;
    /// <summary> SENT-BY, to indicate who is acting on behalf of the ATTENDEE </summary>
    public virtual Uri? SentBy
    {
        get
        {
            if (_sentBy != null)
            {
                return _sentBy;
            }

            var newUrl = Parameters.Get("SENT-BY");
            Uri.TryCreate(newUrl, UriKind.RelativeOrAbsolute, out _sentBy);
            return _sentBy;
        }
        set
        {
            if (value == null || value == _sentBy)
            {
                return;
            }
            _sentBy = value;
            Parameters.Set("SENT-BY", value.OriginalString);
        }
    }

    private string? _commonName;
    /// <summary>
    /// CN: to show the common or displayable name associated with the calendar address.
    /// </summary>
    public virtual string? CommonName
    {
        get
        {
            if (string.IsNullOrEmpty(_commonName))
            {
                _commonName = Parameters.Get("CN");
            }
            return _commonName;
        }
        set
        {
            if (string.Equals(_commonName, value, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            _commonName = value;
            Parameters.Set("CN", value);
        }
    }

    private Uri? _directoryEntry;
    /// <summary>
    /// DIR, to indicate the URI that points to the directory information corresponding to the attendee.
    /// </summary>
    public virtual Uri? DirectoryEntry
    {
        get
        {
            if (_directoryEntry != null)
            {
                return _directoryEntry;
            }

            var newUrl = Parameters.Get("SENT-BY");
            Uri.TryCreate(newUrl, UriKind.RelativeOrAbsolute, out _directoryEntry);
            return _directoryEntry;
        }
        set
        {
            if (value == null || value == _directoryEntry)
            {
                return;
            }
            _directoryEntry = value;
            Parameters.Set("DIR", value.OriginalString);
        }
    }

    private string? _type;
    /// <summary>
    /// CUTYPE: the type of calendar user.
    /// </summary>
    public virtual string? Type
    {
        get
        {
            if (string.IsNullOrEmpty(_type))
            {
                _type = Parameters.Get("CUTYPE");
            }
            return _type;
        }
        set
        {
            if (string.Equals(_type, value, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _type = value;
            Parameters.Set("CUTYPE", value);
        }
    }

    private List<string>? _members;
    /// <summary>
    /// MEMBER: the groups the user belongs to.
    /// </summary>
    public virtual IList<string> Members
    {
        get => _members ?? (_members = new List<string>(Parameters.GetMany("MEMBER")));
        set
        {
            _members = new List<string>(value);
            Parameters.Set("MEMBER", value);
        }
    }

    private string? _role;
    /// <summary>
    /// ROLE: the intended role the attendee will have.
    /// </summary>
    public virtual string? Role
    {
        get
        {
            if (string.IsNullOrEmpty(_role))
            {
                _role = Parameters.Get("ROLE");
            }
            return _role;
        }
        set
        {
            if (string.Equals(_role, value, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            _role = value;
            Parameters.Set("ROLE", value);
        }
    }

    private string? _participationStatus;
    public virtual string? ParticipationStatus
    {
        get
        {
            if (string.IsNullOrEmpty(_participationStatus))
            {
                _participationStatus = Parameters.Get(EventParticipationStatus.Key);
            }
            return _participationStatus;
        }
        set
        {
            if (string.Equals(_participationStatus, value, EventParticipationStatus.Comparison))
            {
                return;
            }
            _participationStatus = value;
            Parameters.Set(EventParticipationStatus.Key, value);
        }
    }

    private bool? _rsvp;
    /// <summary>
    /// RSVP, to indicate whether a reply is requested.
    /// </summary>
    public virtual bool Rsvp
    {
        get
        {
            if (_rsvp != null)
            {
                return _rsvp.Value;
            }

            var rsvp = Parameters.Get("RSVP");
            if (rsvp != null && bool.TryParse(rsvp, out var val))
            {
                _rsvp = val;
                return _rsvp.Value;
            }
            return false;
        }
        set
        {
            _rsvp = value;
            var val = value.ToString().ToUpperInvariant();
            Parameters.Set("RSVP", val);
        }
    }

    private List<string>? _delegatedTo;
    /// <summary>
    /// DELEGATED-TO, to indicate the calendar users that the original request was delegated to.
    /// </summary>
    public virtual IList<string> DelegatedTo
    {
        get => _delegatedTo ?? (_delegatedTo = new List<string>(Parameters.GetMany("DELEGATED-TO")));
        set
        {
            _delegatedTo = new List<string>(value);
            Parameters.Set("DELEGATED-TO", value);
        }
    }

    private List<string>? _delegatedFrom;
    /// <summary>
    /// DELEGATED-FROM, to indicate whom the request was delegated from.
    /// </summary>
    public virtual IList<string> DelegatedFrom
    {
        get => _delegatedFrom ?? (_delegatedFrom = new List<string>(Parameters.GetMany("DELEGATED-FROM")));
        set
        {
            _delegatedFrom = new List<string>(value);
            Parameters.Set("DELEGATED-FROM", value);
        }
    }

    /// <summary>
    /// Uri associated with the attendee, typically an email address.
    /// </summary>
    public virtual Uri? Value { get; set; }

    public Attendee() { }

    public Attendee(Uri attendee)
    {
        Value = attendee;
    }

    public Attendee(string attendeeUri)
    {
        if (!Uri.IsWellFormedUriString(attendeeUri, UriKind.Absolute))
        {
            throw new ArgumentException("attendeeUri");
        }
        Value = new Uri(attendeeUri);
    }

    /// <inheritdoc/>
    public override void CopyFrom(ICopyable obj)
    {
        if (obj is not Attendee atn) return;
        base.CopyFrom(obj);

        Value = atn.Value;

        // String assignments create new instances
        CommonName = atn.CommonName;
        ParticipationStatus = atn.ParticipationStatus;
        Role = atn.Role;
        Type = atn.Type;

        Rsvp = atn.Rsvp;

        SentBy = atn.SentBy;
        DirectoryEntry = atn.DirectoryEntry;
    }
}
