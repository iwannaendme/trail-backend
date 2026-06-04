namespace Trail.Api.Domain.Enums;

public enum SubmissionStatus
{
    Submitted,      // awaiting mentor review
    Approved,       // passes the bar; student can move forward
    NeedsRevision,  // mentor flagged issues; student must re-submit
}
