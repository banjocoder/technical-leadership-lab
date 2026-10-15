#FMEA Experiments

## F01

Start the app, runn /health and /readiness checks first to establish healthy baeline. Stop just the process that handles the api endpoints and attempt to call the endpoints. 

We will be using postman to run the API calls outside of our IDE environment. 

|Run #| Setup | response status / connection error | request duration |
|--|--|--|--|
|1| Policy request while active |  |  |
|2| Policy request while stopped|  |  |

## F02

Keep an expected policy ID in a file or test harness outside the API process lets the check request that same policy after restart. Since the current API stores policies only in memory, the check would expose the loss; it wouldn’t restore the policy.

## F03

Use a disposable schema fixture and make the workflow query a required table or column. Then a schema-aware check can detect a mismatch while the database itself remains reachable. The database remains reachable, but the required LOB column is missing or renamed in the lookup table. The issuance flow should query it before creating the in-memory policy.

There should be a warning from the /readiness test if the lookup is not available. 

## F04

Input a bad connection string and attempt to create a POST on /policies

## F05

Use a small local HTTP stub with normal and delayed-response modes, and give the PolicyService client a bounded timeout. The lab’s example is a 2-second client timeout and a 5-second stub delay.

A local stub can stand in for a third-party risk-scoring service that is necessary for policy issuance.

## F06

Issuance succeeds, while an optional marketing email failure is recorded for later handling. For the lab, keep the outcomes distinct: the policy was issued; the email is pending or failed.