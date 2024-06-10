namespace Temporal.Curriculum.Activities.Messages.Commands;

public class RegisterCrmEntityRequest
{
    public string Id { get; set;  }
    public string Value { get; set;  }
    public string Type { get; set;  }

    public RegisterCrmEntityRequest(string id, string value, string type)
    {
        Id = id;
        Value = value;
        Type = type;
    }
}