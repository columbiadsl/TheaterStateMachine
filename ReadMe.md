# QuickStart

The following pre-requisites are required

- Contributor access or greater to an Azure subscription
- [.NET Core runtime(s) 2.1.x](https://dotnet.microsoft.com/download/dotnet-core/2.1) (both ASP and NET Core)
- [Visual Studio 2019](https://visualstudio.microsoft.com/vs/) (publish operations will be accomplished via Visual Studio)

## GO

1. "Right-Click Deploy" the `RavenResourceGroup` project to Azure
2. "Right-Click Publish" the `CloudFsmApi` project to the app service created from step 1
3. "Right-Click Publish" the `CloudFsmProcessor` project to the function app created from step 1
4. From the swagger endpoint of the app service `https://app-service-url/swagger` execute the following
    - Post the contents of the Characters.json found under the Data folder of the CloudFsmApi project to `api/v1.0/Scene/characterconfig`
    - Post the contents of the Scene.json found under the Data folder of the CloudFsmApi project to `api/v1.0/Scene/scene`
    -  Post the contents of the LanternToCharacter.json found under the Data folder of the CloudFsmApi project to `api/v1.0/Scene/lanternToCharacter`
    -  Post to `api/v1.0/Scene/run` - the response should be a 200 with the response body of
        ```json
        {
            "Scene": "Reception"
        }
        ```
5. "Right-Click Debug" start new instance the `EdgeProxy` project
6. "Right-Click Debug" start new instance the `EdgeDeviceSimulator` project

The state engine will now progress thru the scene script.  C2D messages will appear in the Proxy and propagate to the Device Simulator.

Periodically a GET can be executed on `api/v1.0/Scene/currentscene`
