# Reflection prompts
Which metric best represents the user experience rather than infrastructure health?
The issuance reliability and system availability are probably the 2 metrics that directly impact the users the most because it shows whether they can accomplish their goal

What behavior would a 100% SLO accidentally discourage?
It may discourage improvement. Even if there is meaningful improvement, not being able to reach a goal cycle to cycle can be demoralizing

Which toil item is frequent but should not be automated because it requires judgment?
Reproducing tickets locally is a critical task and difficult to reproduce with automation

# Knowledge check
1. What is an SLI?
Service Level Indicator: This is a quantitative measurement of metrics of the system. Common metrics include system availability (uptime percentage), latency (how fast a page loads), error rates, and throughput.

2. What is an SLO?
Service Level Objective: This is a target metric that we aim for our system to perform at

3. What is an SLA?
Service level agreement: A contractual agreement for a system's behavior

4. Why should an SLO usually be below 100%?
We want to set realistic goals for how well our system will perform. Practically a system will never be at 100% and we want to set meaningful and achievable target

5. What is an error budget?
This is an acceptable amount of failure for a set amount of time. When a system has budget available for a time period, they are able to make more risky changes and experiments without going over the acceptable limits for the window

6. What characteristics make operational work toil?
Toil are repetitive tasks that require little judgement that have automation opportunities
