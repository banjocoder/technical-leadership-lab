# This is the script to start the Email Service Stub with success response. If you want to return a failure response, change the EmailService:Result value to "failure" or "timeout".
dotnet run --project tools/EmailServiceStub --no-launch-profile -- --urls http://localhost:5098 --EmailService:Result="success"
dotnet run --project tools/EmailServiceStub --no-launch-profile -- --urls http://localhost:5098 --EmailService:Result="failure"
dotnet run --project tools/EmailServiceStub --no-launch-profile -- --urls http://localhost:5098 --EmailService:Result="timeout"