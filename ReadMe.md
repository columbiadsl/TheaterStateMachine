
# TheaterStateMachine
 Source code for a state machine system to drive an immersive theater piece, using Azure cloud services.

# Description
This project contains the web service designed for The Raven, an immersive theatre piece that premiered during The New York Film Festival. After its initial launch, The Raven staged a limited month-long run at the American Irish Historical Society prior to closing on November 5th, 2019. Columbia DSL assisted the creators of the project by staging a series of open prototyping sessions held during our monthly meetups at Lincoln Center. The following code and documentation is an effort to provide open resources to those who are interested in exploring new forms and functions of storytelling. 

# QuickStart

The following pre-requisites are required

- Contributor access or greater to an Azure subscription
- [.NET Core runtime(s) 2.1.x](https://dotnet.microsoft.com/download/dotnet-core/2.1) (both ASP and NET Core)
- [Visual Studio 2019](https://visualstudio.microsoft.com/vs/) (publish operations will be accomplished via Visual Studio)
- A Microsoft Azure cloud account.

## GO

1. Deploy the project to your Azure account.
    - Right-Click the `RavenResourceGroup` project and select "Deploy->New"
    - Select your Azure account and subscription, and under Resource group select "Create New" to create a new resource group on your account. If that succeeds, you can then click "Deploy"
    - Create a device named `proxy-multiplexor` in the deployed IoT Hub (on the Azure dashboard) and copy the connection string for use in the `EdgeProxy` (see below.)
1. Right-Click  the `CloudFsmApi` project and select "Publish" to the app service created from step 1
1. Right-Click the `CloudFsmProcessor` project and select "Publish" to the function app created from step 1
1. From the swagger endpoint of the app service `https://<app-service-url>/swagger` execute the following
    - Post the contents of the Scene.json found under the Data folder of the CloudFsmApi project to `api/v1.0/Scene/scene`  (Note: you must post the scene file first!)
    - Post the contents of the Characters.json found under the Data folder of the CloudFsmApi project to `api/v1.0/Scene/characterconfig`
    -  Post the contents of the LanternToCharacter.json found under the Data folder of the CloudFsmApi project to `api/v1.0/Scene/lanternToCharacter`
    -  Post to `api/v1.0/Scene/run` - the response should be a 200 with the response body of
        ```json
        {
            "Scene": "Reception"
        }
        ```
1. Update line 27 of the PipeServer.cs in the `EdgeProxy` project with the connection string of the `proxy-multiplexor` device created in step 1.
1. Right-Click the `EdgeProxy` project and select "Debug-> Start New Instance"
1. Right-Click the `EdgeDeviceSimulator` project and select "Debug-> Start New Instance"

The state engine will now progress thru the scene script.  Command messages will appear in the Proxy and propagate to the Device Simulator.

You can check which scene is currently active by making a GET request to `api/v1.0/Scene/currentscene`

## Debug Locally

In addition to the steps above, complete the following to debug locally (note the IoT Hub and Storage Account cloud resources are required).

1. Add connection strings to the `CloudFsmApi` project (utilize Manage User Secrets) - the content of secrets.json should look as follows with your resource names and keys filled in (resource names and keys omitted in the example snippet):

    ```json
    {
        "DownlinkManager": {
            "IotHubSvcCnxnString": "HostName=;SharedAccessKeyName=service;SharedAccessKey=",
        },
        "Storage": {
            "StorageCnxnString": "DefaultEndpointsProtocol=https;AccountName=;AccountKey=;EndpointSuffix=core.windows.net"
        }
    }
    ```

1. Add a local.settings.json settings file to the `CloudFsmProcessor` project with content (resource names and keys omitted in the example snippet)

    ```json
    {
        "IsEncrypted": false,
        "Values": {
            "ApiHostname": "<localhost:port of debug api instance>",
            "AzureWebJobsDashboard": "UseDevelopmentStorage=true",
            "AzureWebJobsStorage": "UseDevelopmentStorage=true",
            "FUNCTIONS_WORKER_RUNTIME": "dotnet",
            "IotHubSvcCnxnString": "<IoT Hub's Event Hub-compatible endpoint"
        }
    }
    ```

# Usage

## State Machine API
The API will be available at the url specified for the CloudFsmApi service in your Azure console. (If you are running the through visual studio for local debugging, the url is: http://localhost:50486/).  The api has no default route at /, so you must specify the desired controller. For a list of routes and an interface for testing them, go to the /swagger path.  

More information about Swagger at these links:
* https://docs.microsoft.com/en-us/aspnet/core/tutorials/web-api-help-pages-using-swagger?view=aspnetcore-3.1
* https://github.com/domaindrivendev/Swashbuckle.AspNetCore

Before you can use the service you will need to upload json files that define how the show will run. Read more here:

The json files need to be POSTed to the API. You can use the Swagger interface to do this, as described above. Click on POST for scene, characterconfig, or lanternToCharacter to expand the information about these endpoints.  Then click "Try it Out" to enable the feature to post data.  In the text box under "Edit Value" paste the entire json structure (make sure it is valid json!).  Then click the blue "Execute" button. The data will be posted to the api, and the response will appear below.

You need to post all three data files before you can start running the show. To start the show, use the POST for the /Scene/run endpoint.  This will cue up the first scene, and start running the show. The API will respond to onBeaconChange and jump reuests to move the show forward.  It will also execute timers for any timed scenes, and move those forward automatically.

## Local Server

On your local network you will need to run the ProxyServer.  This sends show events to the API, and receives commands from the state machine.  You will also need your own server to process these commands and dispatch them to the various subsystems running your show (e.g. QLab for lighting or sound, IoT devices, etc.)

# Architecture

### Data Flow

Data flow between the show's systems and the state machine service:

IoT Lanterns/QLab/etc <=> LocalServer <=> EdgeProxy <=> CloudFsmProcessor <=> CloudFsmApi

### Components
__CloudFsmApi__ is the key functional element of the application, where the state machine for the show is maintained and updated.  It also provides a web interface for uploading the data files that describe the show.

The current state of the show is maintained in a Singleton instance of FsmSceneManager.

__CloudFsmProcessor__ is a simple function that runs as an IoT Hub service, and forwards the messages from the show's server into the API, and commands from the state machine out to the ProxyServer. All traffic is piped via IoT Hub mainly because the solution needs unsolicited downlink (not available with simple REST), so we needed glue to talk to the API.

__EdgeProxy__ runs on your local network and is the single connection multiplexer to the cloud. It is transparent and content agnostic.  Its job is to move the messages up and down between the cloud FSM and the local-server using name pipes (LAN) and IoT Hub device client (WAN). The edge proxy can be hosted as an independent App in the local server, or on a different machine in the LAN.

__EdgeDeviceSimulator__ is included to illustrate how the local server connects to the proxy using named pipes. You can use this for testing your Azure deployment. In production, you won't run this, as you'll be running your own local server code.

#### Show Specification Data
There are three json data files that specify how the show should run. Between them they contain information about the show's scenes and characters, the commands that should be sent to the show systems for each step within a scene, and what conditions cause a transition between scene steps for the show as a whole and each character.

- *Scene.json* defines the show's main scenes and steps.
- *Characters.json* contains instructions for each character.
- *LanternToCharacter.json* maps IoT Lantern ids to characters, so the machine knows who's holding which laterns.

Example data files as used for the Raven are located in the CloudFsmApi/Data directory, and these are loaded when the service starts. You can upload new data files through the Swagger interface to the API.

#### Show State

The state of the show at any point has every show participant in a step of a scene, and a timer for how long the step is active.

State changes when:
Users trigger beacons and/or a specific amount of time passes. Either of these will trigger a jump to a new scene step, which will send new commands to the show systems.
