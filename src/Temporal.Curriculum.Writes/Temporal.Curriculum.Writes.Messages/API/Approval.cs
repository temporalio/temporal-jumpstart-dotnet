using Temporal.Curriculum.Writes.Messages.Values;

namespace Temporal.Curriculum.Writes.Messages.API;


public record Approval(ApprovalStatus Status, string Comment);