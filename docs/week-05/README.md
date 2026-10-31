# Understanding check

| Concept | What question does it help answer? | Proposed PolicyService example | What does it not establish by itself? |
| --- | --- | --- | --- |
| Log | Is the app functioning? | Database failure event should log errors or timeouts that happen | Doesn't explain the broader system impact over time |
| Metric | How much activity? | App regularly gathers metric data to know how the app is functioning | Does not explain specific instances of failures |
| Trace | How does the end to end process work? | Failed issuance request should show a complete end to end story of what went wrong | Does not explain how often or all possible routes |
| Health/readiness check | Is the app ready to use? | Liveliness would tell us if the app was functional, readiness would tell us if the app was ready to serve actual traffic | Does not explain how failures impact the system |
| Alert | Is everything normal? | App sends out alerts when preconfigured rules have been triggered | Does not explain end to end process |