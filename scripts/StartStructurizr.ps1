# For setup, run this command
# docker pull structurizr/structurizr

# For running Structurizr locally, use this command. Replace {PATH} with the path to your local Structurizr workspace to map to docker instance.
docker run -it --rm -p 8080:8080 -v {PATH}:/usr/local/structurizr structurizr/structurizr local

# After this open http://localhost:8080 in your web browser (or whatever port you mapped)