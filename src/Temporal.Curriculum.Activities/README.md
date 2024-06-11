# Activities

https://docs.temporal.io/activities

## Goals

- Continue testing, this time with Temporal `ActivityEnvironment` test facility
- Demonstrate how to pass dependencies into Activities
- Execute our first Activity from our Workflow and understand Retries, Idempotency, etc

## Workflow : Introduce our first Activity 

Our requirements specify that we need to register this Entity with our CRM System. 
Calls to APIs, Databases, filesystems, etc should always be done inside an `Activity`.

Let's continue doing top-down development by first Executing this activity in our `OnboardEntity` Workflow.

1. Add the "RegisterCrmEntity" test in our Workflow unit test, asserting that the activity was invoked correctly.
2. See it fail in tests.
3. Update the `OnboardEntity` Workflow to invoke the `RegisterCrmEntity` activity.
4. See it succeed in the tests.

## Activity: Implement our first Activity

Let's introduce the `RegisterCrmEntity` Activity implementation.
There are a few refactorings we will perform once we write our test that proves our behavior.

One of the main things we need to ensure in our Activity implementations is _idempotency_.
Here, we want to verify the `Entity` does not already exist in the CRM before attempting to register it.

### Refactorings

- `Messages.Commands`
- `Domain.Handlers`
- `Domain.Clients.ICrmClient`

_Test-Drive our `RegisterCrmEntity` Activity`_

Let's use our first Activity test to drive out all the third-party Clients, Integrations packages,
and establish the message contracts for our first Activity. 
We will create Namespaces for each of our third-party client dependencies we plan on using in our Activity Handlers.

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

## RetryOptions

Now that we understand the CRM Client integration, we need to think about how we should configure
the RetryOptions for this Activity. 

Should we just let the default retry policy of "try forever" happen?
Or should we instead only try registering the Entity for a period of time before doing something else?
Maybe we want to keep retrying but gradually backoff to ease pressure on our CRM service?


