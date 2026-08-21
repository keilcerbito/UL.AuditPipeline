// Parameters allow the CI/CD pipeline to inject values at deployment time
param location string = resourceGroup().location
param environmentName string = 'dev'
param appName string = 'ulauditpipeline'

// Define the Cosmos DB Account (NoSQL)
resource cosmosDbAccount 'Microsoft.DocumentDB/databaseAccounts@2023-11-15' = {
  name: '${appName}-cosmos-${environmentName}'
  location: location
  properties: {
    databaseAccountOfferType: 'Standard'
    locations: [
      {
        locationName: location
        failoverPriority: 0
      }
    ]
  }
}

// Define the App Service Plan (The server farm compute)
resource appServicePlan 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: '${appName}-plan-${environmentName}'
  location: location
  sku: {
    name: 'B1' // Basic Tier
    tier: 'Basic'
  }
}

// Define the Web API App Service (The host for your API)
resource webApiApp 'Microsoft.Web/sites@2022-09-01' = {
  name: '${appName}-api-${environmentName}'
  location: location
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      netFrameworkVersion: 'v8.0' // Aligning with modern .NET
    }
  }
}