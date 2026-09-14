## Reflection prompts:

1. Which parts of the delivery process are deterministic today?

   The local build process is the most deterministic, because for a specific input it produces the same output every time. The build follows the same 3 steps of restore, compile, and test, and will fail consistently for the same reasons at each step in the process. The other steps in the pipeline are less deterministic because they depend on manual intervention and execution. 

2. Where does your design rely on a person remembering to do something?

   Any step where human intervention is required, but some specific error prone areas are environment configuration and manual testing in the environment. 

4. Which failure could travel the farthest before being detected?

   Environment configuration could travel to the deployment of the release and currently there is no way to even identify that it is not correct outside of reviewing the configuration file. 

## Knowledge check:

1. What is the difference between continuous integration and continuous delivery?

   Continuous integration regularly integrates and verifies source changes to establish that the software can still build and pass its checks. 
    
   Continuous delivery extends that process by making the resulting software ready to move through packaging, deployment, and release in a repeatable manner.

3. Why is a deployment pipeline more than a build script?

   A build script is mainly focused on the state of the code itself and it's dependencies, but a deployment pipeline has a broader scope that includes the changes that happen to an environment and infrastructure

5. Why can human intervention increase deployment risk?

   Because humans are human. We forget things, we make mistakes with manual input, and we often make assumptions about a context that deployment processes may not be able to infer. 

7. What is the difference between preventing a failure and detecting one?

   Prevention attempts to stop an invalid state from occurring; detection determines that an invalid state exists. Detecting a failure usually involves checking an existing state to see if it is valid and healthy. 

9. What information would you need to identify exactly what code is running in QA?

   Artifact/build ID, source commit, branch/source reference, deployment timestamp, and target environment.

11. Why can deployment validation be treated separately from deployment execution?

    The two processes can run independent of one another and serve different purposes. Sometimes a deployment may begin it's life in a healthy state, then become unhealthy due to unforseen circumnstances. 
