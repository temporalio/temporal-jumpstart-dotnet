using System.ComponentModel.DataAnnotations;

namespace Temporal.Curriculum.Starters.Messages.API;

public class OnboardingsPost
{
    public required string OnboardingId { get; set;  }
   
    public required string Value { get; set; }
}