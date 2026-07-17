namespace OreoLeads.Domain.Enums;

public enum EmailEventType
{
    Queued        = 0,
    Sent          = 1,
    Delivered     = 2,
    Opened        = 3,
    Clicked       = 4,
    SoftBounce    = 5,
    HardBounce    = 6,
    Spam          = 7,
    Blocked       = 8,
    Invalid       = 9,
    Deferred      = 10,
    Unsubscribed  = 11,
    Reply         = 12
}
