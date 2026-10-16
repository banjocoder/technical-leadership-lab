# Proposed Improvements

## F06
Currently email failures are silent and do not disrupt the system flow, but do have an impact on business concerns. We should eventually have a system that behaves in the following manner: 

Trigger: more than five failed or unknown email attempts in a rolling 24 hours.
Recipients: IT and Marketing, through an independent delivery path.
Accountable owner: IT acknowledges, investigates, and coordinates recovery.
Marketing: assesses campaign impact and whether delayed sends remain appropriate.