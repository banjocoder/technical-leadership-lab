# Reflection prompts
1. Which failure surprised you most?

I think the way that we handled the email service was the most surprising to me.

2.  Which failure had the longest detection delay?

The email service has the longest detection delay. 

3.  Which recovery depends on undocumented human knowledge?

Again, the email service recovery depends on undocumented human knowledge.

# Knowledge check
1. What is resilience?

Resilience is the ability of a system to identify and recover from unplanned issues in a meaningful and consistent way, withstanding disruption and preserving useful service where possible.

2. How is resilience different from availability?

Availability is how much of the time a system provides its required service, resilience is how a system behaves when problems do happen. 

3. Why is recovery part of architecture?

How a system is architected will determine the consequences when a problem does happen, and what options are available for recovery. 

4. What is graceful degradation?

When an issue arises in a system, graceful degradation is the process of isolating issues and preserving other functionality of a system until the original issue is resolved.

5. Why should teams intentionally test failure conditions?

Testing failure conditions gives the team actual failure behavior and proves known recovery scenarios before an actual issue arises. This proves recovery plans and behavior, as well as giving the team confidence in the ability to recover from issues

6. Why is simplicity a reliability technique?

The more complex and coupled a system is, the more points of failure it has. Every touchpoint and handoff in a system is an opportunity for error and unexpected results. A complex system is also harder to understand and debug, adding to the difficulty of resolving unexpected behavior and output. A system needs to have the right amount of complexity to meet all user requirements without additional unneccesary complexity. 