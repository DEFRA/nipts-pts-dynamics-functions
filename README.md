# nipts-pts-dynamics-functions

The "NIPTS dynamics function" is a supporting function app that processes offline applications for the NIPTS service applied via a dynamics365 endpoint.

## Prerequisites
Before setting up the project, ensure you have the following installed:

.NET SDK 6.0+
Azure Functions Core Tools
Visual Studio 2022 or VS Code
Azure CLI
Access to required environment variables or secrets (e.g., via Azure Key Vault or local settings)

## Setup
1. Clone the repository:
```
git clone https://github.com/DEFRA/nipts-pts-dynamics-functions.git
cd nipts-pts-dynamics-functions
```

2. Restore dependencies:
```
dotnet restore
```

3. Configure local settings: Create a local.settings.json file in the root with the following structure:
```
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_INPROC_NET8_ENABLED": "1",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet"
  }
}
```

### Development
To run the project locally:
```
func start
```
Or from Visual Studio, press F5 to start debugging.

### Test
Unit tests are located in the /test directory.

To run tests:
```
dotnet test
```

Ensure all dependencies are restored and the test project builds successfully.

## Running in development
Use the Azure Functions Core Tools or Visual Studio to run the function app locally. Ensure your local settings file is configured and you have selected the function app as the startup project. Navigating to the localhost url and typing /swagger will provide a UI to interact with the api

## Running tests
Run all tests using:
```
dotnet test
```

## Contributing to this project

Please read the [contribution guidelines](/CONTRIBUTING.md) before submitting a pull request.

## Licence

THIS INFORMATION IS LICENSED UNDER THE CONDITIONS OF THE OPEN GOVERNMENT LICENCE found at:

<http://www.nationalarchives.gov.uk/doc/open-government-licence/version/3>

The following attribution statement MUST be cited in your products and applications when using this information.

>Contains public sector information licensed under the Open Government licence v3

### About the licence

The Open Government Licence (OGL) was developed by the Controller of Her Majesty's Stationery Office (HMSO) to enable information providers in the public sector to license the use and re-use of their information under a common open licence.

It is designed to encourage use and re-use of information freely and flexibly, with only a few conditions.
