workspace "PolicyService Delivery System" {

    model {

        # ---------------------------------------------------------------------
        # People
        # ---------------------------------------------------------------------

        contributor = person "Contributor"
        qaApprover = person "QA Environment Approver"


        # ---------------------------------------------------------------------
        # External systems / logical environment targets
        #
        # These are used by the System Context and Container views.
        # They are NOT Structurizr deployment nodes.
        # ---------------------------------------------------------------------

        gitHub = softwareSystem "GitHub"
        packageRepository = element "Package Repository"
        cloudflare = element "Cloudflare R2" {
            tags "DataStore"
        }

        devEnv = element "Dev Environment"
        qaEnv = element "QA Environment"


        # ---------------------------------------------------------------------
        # PolicyService Delivery System
        # ---------------------------------------------------------------------

        ss = softwareSystem "PolicyService Delivery System" {

            buildProcess = container "Build Process"

            buildArtifact = container "Build Artifact" "Packaged Application" "ZIP file" {
                tags "Artifact"
            }

            devDeploymentProcess = container "Dev Deployment Process"

            qaDeploymentProcess = container "QA Deployment Process"

            devReadinessProcess = container "Dev Deployment Readiness Validation"

            qaReadinessProcess = container "QA Deployment Readiness Validation"

            deliveryEvidenceStore = container "Delivery Evidence" {
                tags "DeliveryEvidence"
            }


            # These exist primarily so the Deployment View can show
            # the actual runtime workload on the DEV and QA runners.
            #
            # We can exclude them from the logical Container View.

            policyServiceRuntime = container "PolicyService" \
                "Running PolicyService application" \
                "ASP.NET Core" {
                tags "DeploymentOnly"
            }

            policyDatabase = container "Policy Database" \
                "Ephemeral database used by the deployed PolicyService" \
                "SQL Server LocalDB" {
                tags "DeploymentOnly"
            }
        }


        # ---------------------------------------------------------------------
        # External relationships
        # ---------------------------------------------------------------------

        contributor -> gitHub "Submits and reviews repository changes"

        gitHub -> buildProcess "Triggers build process"

        qaApprover -> qaDeploymentProcess "Approves QA deployment"

        packageRepository -> buildProcess "Provides dependency packages"


        # ---------------------------------------------------------------------
        # Delivery-system relationships
        # ---------------------------------------------------------------------

        buildProcess -> devDeploymentProcess \
            "Successful build triggers DEV deployment"

        buildProcess -> buildArtifact \
            "Produces immutable artifact"

        devDeploymentProcess -> buildArtifact \
            "Retrieves/consumes immutable artifact"

        devDeploymentProcess -> devEnv \
            "Deploys artifact to"

        devDeploymentProcess -> qaDeploymentProcess \
            "Prompts QA deployment approval"

        qaDeploymentProcess -> buildArtifact \
            "Retrieves/consumes immutable artifact"

        qaDeploymentProcess -> qaEnv \
            "Deploys artifact to"

        devReadinessProcess -> devEnv \
            "Evaluates"

        qaReadinessProcess -> qaEnv \
            "Evaluates"


        # ---------------------------------------------------------------------
        # Runtime relationships
        #
        # These are important for the Deployment View.
        # Deployment groups below will ensure that DEV processes connect to
        # the DEV PolicyService instance and QA processes connect to QA.
        # ---------------------------------------------------------------------

        devDeploymentProcess -> policyServiceRuntime \
            "Deploys and starts"

        qaDeploymentProcess -> policyServiceRuntime \
            "Deploys and starts"

        devReadinessProcess -> policyServiceRuntime \
            "Evaluates health and readiness"

        qaReadinessProcess -> policyServiceRuntime \
            "Evaluates health and readiness"

        policyServiceRuntime -> policyDatabase \
            "Uses"


        # ---------------------------------------------------------------------
        # Delivery evidence relationships
        # ---------------------------------------------------------------------

        buildProcess -> deliveryEvidenceStore \
            "Records artifact identity"

        devDeploymentProcess -> deliveryEvidenceStore \
            "Records DEV deployment result"

        devReadinessProcess -> deliveryEvidenceStore \
            "Records DEV readiness evidence"

        qaDeploymentProcess -> deliveryEvidenceStore \
            "Records QA deployment result"

        qaReadinessProcess -> deliveryEvidenceStore \
            "Records QA readiness evidence"

        deliveryEvidenceStore -> cloudflare \
            "Persists data to"


        # =====================================================================
        # DEPLOYMENT MODEL
        # =====================================================================
        #
        # This represents one execution of our CI/CD topology.
        #
        # DEV and QA are represented as different deployment groups so that
        # Structurizr does not accidentally connect DEV process instances to
        # QA runtime/database instances and vice versa.
        # =====================================================================

        pipelineExecution = deploymentEnvironment "Pipeline Execution" {

            devGroup = deploymentGroup "DEV"
            qaGroup = deploymentGroup "QA"


            githubPlatform = deploymentNode \
                "GitHub Actions" \
                "Hosted CI/CD execution infrastructure" \
                "GitHub Actions" {


                # -------------------------------------------------------------
                # Shared GitHub infrastructure
                # -------------------------------------------------------------

                artifactStorage = infrastructureNode \
                    "Workflow Artifact Storage" \
                    "Stores the immutable PolicyService build artifact" \
                    "GitHub Actions Artifacts"

                qaApprovalGate = infrastructureNode \
                    "QA Environment Approval" \
                    "Requires approval before the QA deployment job executes" \
                    "GitHub Environment"


                # -------------------------------------------------------------
                # BUILD
                # -------------------------------------------------------------

                buildRunner = deploymentNode \
                    "Build Runner" \
                    "Ephemeral GitHub-hosted Windows runner" \
                    "windows-latest" {

                    buildInstance = containerInstance buildProcess
                }


                # -------------------------------------------------------------
                # DEV
                # -------------------------------------------------------------

                devRunner = deploymentNode \
                    "DEV Runner" \
                    "Ephemeral GitHub-hosted Windows runner" \
                    "windows-latest" {

                    deploymentGroup devGroup

                    devDeployInstance = containerInstance devDeploymentProcess

                    devReadinessInstance = containerInstance devReadinessProcess

                    devPolicyServiceInstance = containerInstance policyServiceRuntime

                    devDatabaseInstance = containerInstance policyDatabase
                }


                # -------------------------------------------------------------
                # QA
                # -------------------------------------------------------------

                qaRunner = deploymentNode \
                    "QA Runner" \
                    "Ephemeral GitHub-hosted Windows runner" \
                    "windows-latest" {

                    deploymentGroup qaGroup

                    qaDeployInstance = containerInstance qaDeploymentProcess

                    qaReadinessInstance = containerInstance qaReadinessProcess

                    qaPolicyServiceInstance = containerInstance policyServiceRuntime

                    qaDatabaseInstance = containerInstance policyDatabase
                }


                # -------------------------------------------------------------
                # Deployment-specific relationships
                #
                # These describe GitHub infrastructure rather than logical
                # application relationships, so they belong here.
                # -------------------------------------------------------------

                buildInstance -> artifactStorage \
                    "Publishes immutable artifact"

                artifactStorage -> devDeployInstance \
                    "Supplies immutable artifact"

                devReadinessInstance -> qaApprovalGate \
                    "Makes build eligible for QA approval"

                qaApprovalGate -> qaDeployInstance \
                    "Releases QA deployment after approval"

                artifactStorage -> qaDeployInstance \
                    "Supplies same immutable artifact"
            }
        }
    }


    views {

        # ---------------------------------------------------------------------
        # System Context
        # ---------------------------------------------------------------------

        systemContext ss "SystemContext" {
            include *
            include "->contributor->"
        }


        # ---------------------------------------------------------------------
        # Container / Logical View
        #
        # The runtime and DB containers are omitted because this diagram is
        # focused on delivery-system responsibilities.
        # ---------------------------------------------------------------------

        container ss "ContainerView" {
            include *

            exclude policyServiceRuntime
            exclude policyDatabase
        }


        # ---------------------------------------------------------------------
        # Deployment View
        # ---------------------------------------------------------------------

        deployment ss pipelineExecution "DeploymentView" {
            include *
        }


        # ---------------------------------------------------------------------
        # Styles
        # ---------------------------------------------------------------------

        styles {

            element "Artifact" {
                metadata false
                shape Folder
                background #888888
                color #ffffff
            }

            element "DeliveryEvidence" {
                metadata false
                shape Folder
                background #888888
                color #ffffff
            }

            element "DataStore" {
                metadata false
                shape Cylinder
            }
        }
    }
}