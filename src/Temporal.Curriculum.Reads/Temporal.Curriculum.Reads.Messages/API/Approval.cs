using Temporal.Curriculum.Reads.Messages.Values;

namespace Temporal.Curriculum.Reads.Messages.API;


public record Approval(ApprovalStatus Status, string Comment);