#FMEA Experiments

## Baseline

Start the app, runn /health and /readiness checks first to establish healthy baeline. Stop just the process that handles the api endpoints and attempt to call the endpoints. 

We will be using postman to run the API calls outside of our IDE environment. 
|Run # | Setup | response status / connection error | request duration |
|--|--|--|--|
|1| Health          | 200 | 719ms |
|2| Readiness       | 200 | 554ms |
|3| Policies Post   | 201 | 4.13s |
|4| Policies Get    | 200 | 15ms  |



## F01

Keep an expected policy ID in a file or test harness outside the API process lets the check request that same policy after restart. Since the current API stores policies only in memory, the check would expose the loss; it wouldn’t restore the policy.

|Run # | Setup | response status / connection error | request duration |
|--|--|--|--|
|1| Health                  | ECONNREFUSED 127.0.0.1:5006 | Not Captured |
|2| Readiness               | ECONNREFUSED 127.0.0.1:5006 | Not Captured |
|3| Policies Post (id:1)    | ECONNREFUSED 127.0.0.1:5006 | Not Captured |
|4| Policies Get (id:1)     | ECONNREFUSED 127.0.0.1:5006 | Not Captured |

## F02

Database unavailable: take the database offline and these are the restuls for the endpoints

|Run # | Setup | response status / connection error | request duration |
|--|--|--|--|
|1| Health                          | 200 | 505ms |
|2| Readiness                       | 503 | 15ms |
|3| Policies Get (id:1)             | 200 (from previus tests) | 16ms |
|4| Policies Post (Failed Attempt)  | 503 | 123ms |

## F03

Use a disposable schema fixture and make the workflow query a required table or column. Then a schema-aware check can detect a mismatch while the database itself remains reachable. The database remains reachable, but the required LOB column is missing or renamed in the lookup table. The issuance flow should query it before creating the in-memory policy.

|Run # | Setup | response status / connection error | request duration |
|--|--|--|--|
|1| Health                      | 200 | 13ms |
|2| Readiness                   | 503 | 18ms |
|3| Policies Post (id:1)        | 503 | 106ms |
|4| Policies Get (id:1)         | 200 (from previous tests) | 15ms |

## F04

Input a bad connection string and attempt to create a POST on /policies. In this test, we wrote in a fake database name into the connection string. 

|Run # | Setup | response status / connection error | request duration |
|--|--|--|--|
|1| Health                      | 200 | 396ms |
|2| Readiness                   | 503 (both the DB connection and schema tests failed) | 708ms |
|3| Policies Post (id:1)        | 503 (Cannot open "ClaimsDB") | 90ms |
|4| Policies Get (id:1)         | 404 | 9ms |

## F05

Use a small local HTTP stub with normal and delayed-response modes, and give the PolicyService client a bounded timeout. The lab’s example is a 3-second client timeout and a 5-second stub delay.

A local stub can stand in for a third-party risk-scoring service that is necessary for policy issuance.

|Run # | Setup | response status / connection error | request duration |
|--|--|--|--|
|1| Health                      | 200 | 21ms |
|2| Readiness                   | 200 | 101ms |
|3| Policies Post (id:1)        | 503 ("Risk score service timed out.") | 3.97ms |
|4| Policies Get (id:1)         | 404 | 6ms |

## F06

Issuance succeeds, while an optional marketing email failure is recorded for later handling. For the lab, keep the outcomes distinct: the policy was issued; the email is pending or failed.

In this test, we caused the email service to intentionally delay past the alloted timeout. 

|Run # | Setup | response status / connection error | request duration |
|--|--|--|--|
|1| Health                      | 200 | 12ms |
|2| Readiness                   | 200 | 76ms |
|3| Policies Post (id:1)        | 201 | 6.54ms |
|4| Policies Get (id:1)         | 200 | 18ms |

**Note: Run #3 also included an email request that timed out and threw an exception. This is the behavior we excpect, an issue with the email service should not lead to a bad response for the policy issuance process**