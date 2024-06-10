# Activities

### Goals

- Continue testing, this time with Temporal `ActivityEnvironment` test facility
- Demonstrate how to pass dependencies into Activities
- Execute our first Activity from our Workflow and understand Retries, Idempotency, etc

### Refactorings

- `Messages.Commands`
- `Domain.Handlers`
- `Domain.Clients.ICrmClient`

_Test-Drive our `RegisterCrmEntity` Activity`_

Let's use our first Activity test to drive out all the third-party Clients, Integrations packages,
and establish the message contracts for our first Activity.

_Introduce `Messages.Commands`_

Our `Commands` package will hold the contracts we use to Request operations with our Activity Handlers.

_Introduce `Domain.Handlers.Integrations`_

Our `Activities` are the atomic operations, the "steps", we will use to get things done in our Use Case.
We will group the calls to our third party dependencies (eg our "CRM" software) into an explicit
namespace called "Integrations" and create the message contracts our Orchestration can use to execute 
these Activity Handlers.

_Introduce `ICrmClient` to `Domain.Clients`_

The CRM Client needs to be created at Application startup and injected into our Activity Handlers.
This client requires an "API Key" to be passed into it upon creation.