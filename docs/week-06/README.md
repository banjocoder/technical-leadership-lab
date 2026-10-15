# Current Issuance Workflow

### Create Policy
User sends the request 
-> System routes to policies POST endpoint
-> System validated request
-> System verifies issuance db dependency
-> System creates policy record in the PolicyStore (in memory)
-> System retuns result back to user

### Retrieve Policy
User sends the request
-> System routes to policies GET endpoint
-> System attempts to get record from PolicyStore (in memory)
-> If system finds a matching record, return. Otherwise return a 404 response.

## Decision Matrix For Selecting Reliability Pattern

| Capability | Required for useful service? Why? | Dependencies | Acceptable reduced behavior | Unacceptable result |
| --- | --- | --- | --- | --- |
| Accept and validate an issuance request | To decide | To identify | To define | To define |
| Check required database state | To decide | To identify | To define | To define |
| Create the declared policy result | To decide | To identify | To define | To define |
| Optional workflow step, if present | To decide | To identify | To define | To define |

## Three Reliability Questions

- **Availability:** Over what period, and for which capability, can users obtain useful service?
- **Resilience:** What useful behavior can the system preserve while a component is malfunctioning?
- **Recovery:** How does the system return to its normal operating state, and how will you verify that return?