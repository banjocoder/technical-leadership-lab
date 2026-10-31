## Reflection prompts:

R1. Which diagram would you show an executive? Which would you show an engineer implementing the system?

I would show an executive the System Context View, and possibly the Container view as well depending on how much they wanted to understand about the system and their level of technical expertise. 

An engineer would need to see the container view, and if they are also involved in the operations and deployment, the deployment view as well.

R2. What decision is expensive enough to deserve an ADR?

We had 2:  
The independent readiness validation ADR was important because it cleanly separated and decoupled the validation process from deployment, which adds significant maintainability, clarity, and flexibility.  
 
The Delivery Evidence Storage ADR was significant because it adds an additional external dependency that must be owned and maintained, but adds value because it offers simple persistence and access to our evidence records.

Both of these add value but are also costly to reverse. 

R3. Which implementation details are intentionally absent from your diagrams?

Details such as specific classes, methods, configuration, scripts, credentials, and endpoints are specifically left out as details of the system that are left up to the implementer. These can easily be changed and have much less impact on how the different components of the system interact with each other.

## Knowledge check:

1. When should you create an ADR?

When there is a decision that needs to be made which impacts software architecture/structure, non-functional requirements, dependencies, or interfaces. Also, if a decision may impact a significant amount of resources or will be difficult to change at a later date. ADR records the decision, context, consequences, and often alternatives. 

2. Why should an architecture diagram avoid showing every class?

Architecture diagrams are meant to display a high level description of logical boundaries, dependencies, and control flow. Adding in every class just adds noise and reduces the clarity of how components relate to each other

3. What question does a system-context diagram answer?

A system context diagram targets a single system and mainly focuses on its direct dependencies, without exposing its internal workings. It's purpose is to give the context in which the system lives

4. What is a quality attribute?

A measurable, testable property of a system that describes how well it operates, not what it does or how. Also known as a Non-Functional Requirement.

5. Why is maintainability an architecture concern?

Because often the decisions made at the architectural level are the decisions that have the most impact on the cost and ability to maintain the system. How systems are organized, how control flows, and how components are coupled together have a disproportionate impact on maintenance.

6. What is the difference between architecture and implementation?

Architecture is the high level design of a system, and often outlines abstractions, dependencies, and control flow. An implementation is the actual system that is built, with concrete tools, processes. and configurations as they are outlined in the Architecture.
