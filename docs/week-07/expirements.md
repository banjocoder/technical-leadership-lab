# SLO Expirements

|Measurement|	Counts needed|
|--|--|
|Issuance success|	Valid logical issuances issued and independently retrieved within two hours / eligible logical issuances|
|Timely issuance|	Logical issuances receiving a successful issuance response in under 10 seconds / eligible logical issuances|
|Restart survival|	Policies identity-verified after restart / committed policies selected for the test|

|Check|Result|
|--|--|
|Issuance and independent retrieval within two hours|	Pass, assuming the GET met that deadline|
|Logical issuance under 10 seconds|	Missed: recovery took 90 seconds|
|Same-key retry without duplication|	Pass|
|Retrieval after restart|	Pass|

### 1) [DB Offline Issuance](/docs/week-07/evidence/2026-10-29-local-run/db-offline-issuance-response.txt)
Attempt to issue a policy with the DB offline

Result: Exception

Response Time: 12.14s

### 2) [DB Back Online Issuance Retry](/docs/week-07/evidence/2026-10-29-local-run/db-back-online-issuance-response.json)
Retry same issuance request after database is back online

Result: HTTP 201

Response Time: 465ms

### 3) [Issuance Retry After Success](/docs/week-07/evidence/2026-10-29-local-run/issuance-retry-response.json)
Retry the same issuance request as the previous after a successful created response

Result: HTTP 201 (no new record created, just returned the previous results.)

Response Time: 160ms 

### 4) [Pre Restart Retrieval](/docs/week-07/evidence/2026-10-29-local-run/retrieval-response.json)
Retrieve newly created record

Result: HTTP 200

Response Time: 94ms 

### 5) [Post Restart Retrieval](/docs/week-07/evidence/2026-10-29-local-run/post-restart-retrieval-response.json)
Retrieve newly created record after app restart

Result: HTTP 200

Response Time:  2.06s
