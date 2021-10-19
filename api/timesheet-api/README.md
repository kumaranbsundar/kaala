very important to do this to export variables to environment in order for the SDK to correctly authenticate
```bash
$(yawsso -p kaaladev -e)
```

## CodeArtifact configuration
The nuget.config file has settings to add "eonsos/kaala" package source. This is needed in order to execute the push command given below