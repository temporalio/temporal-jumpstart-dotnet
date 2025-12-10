namespace App.Messages;

public class StartMyWorkflowRequest
{

    private string id;
    private string value;

    public StartMyWorkflowRequest(string id, string value)
    {
        this.id = id;
        this.value = value;
    }
}