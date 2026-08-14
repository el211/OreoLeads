namespace OreoLeads.Domain.Enums;

public enum LeadStatus
{
    New            = 0,
    Qualified      = 1,
    ReadyToContact = 2,
    EmailPrepared  = 3,
    EmailSent      = 4,
    FollowUp1      = 5,
    FollowUp2      = 6,
    Meeting        = 7,
    ProposalSent   = 8,
    Client         = 9,
    Rejected       = 10,
    DoNotContact   = 11,
    /// <summary>Un SMS de prospection a été envoyé à ce prospect.</summary>
    SmsSent        = 12,
}
