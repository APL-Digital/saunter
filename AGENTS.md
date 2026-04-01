Check the following things only when the code has been changed:
Run dotnet format --verify-no-changes *.sln for checking the formatting and fixing formatting issues after code is written.
If CodeRabbit CLI is installed then run a local review after code is written.

The project needs to be AsyncAPI Spec version 3.0.0
check the asyncapi spec from https://github.com/asyncapi/spec/blob/v3.0.0/spec/asyncapi.md
Validate it against the current solution and update the ASYNCAPI_3_0_COMPATIBILITY_AUDIT file with new findings