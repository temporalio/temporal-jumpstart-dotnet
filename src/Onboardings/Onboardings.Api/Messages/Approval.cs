using Onboardings.Domain.Messages.Values;

namespace Onboardings.Api.Messages;


public record Approval(ApprovalStatus Status, string Comment);