## CodeArtifact configuration
The nuget.config file has settings to add "eonsos/kaala" package source. This is needed in order to execute the push command given below
## Push Package
``` bash
dotnet nuget push bin/Debug/ApiInfraStack.1.0.2.nupkg --source "eonsos/kaala"
```