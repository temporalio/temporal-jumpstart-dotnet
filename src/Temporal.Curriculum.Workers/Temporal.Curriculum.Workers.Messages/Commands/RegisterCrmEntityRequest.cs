namespace Temporal.Curriculum.Workers.Messages.Commands;

public record RegisterCrmEntityRequest
{
    public string Id { get; set;  }
    public string Value { get; set;  }

    public RegisterCrmEntityRequest(string id, string value)
    {
        Id = id;
        Value = value;
    }

    public RegisterCrmEntityRequest()
    {
    }
}