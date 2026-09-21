# System Context Diagram Boundary Decisions

|Decision | Reason|
|-- |-- |
|Delivery System rather than PolicyService is the system of interest|We are studying the complete path from source change through validated deployment|
|GitHub is external|It is the platform on which much of our logical system is implemented|
|Approver interacts with the Delivery System|Authorization is the architectural responsibility; GitHub's environment gate is an implementation detail|
|Application runtime dependencies are omitted here|They belong more naturally in lower-level architecture views|