# ============================================================================
# Azure Deployment Script for Agent Orchestrator (PowerShell)
# ============================================================================
# This script deploys the Agent Orchestrator to Azure App Service.
#
# Usage:
#   .\scripts\deploy.ps1                    # Interactive mode
#   .\scripts\deploy.ps1 -AppName myapp     # With app name
#   .\scripts\deploy.ps1 -Help              # Show help
#
# Prerequisites:
#   - Azure CLI installed and logged in (az login)
#   - .NET 10 SDK installed
#   - Azure subscription with appropriate permissions
# ============================================================================

[CmdletBinding()]
param(
    [string]$AppName = "agent-orchestrator",
    [string]$ResourceGroup = "rg-agent-orchestrator",
    [string]$Location = "eastus",
    [string]$PlanName = "plan-agent-orchestrator",
    [string]$Sku = "B1",
    [switch]$CreateResources,
    [switch]$SkipBuild,
    [switch]$UpdateManifest,
    [switch]$PackageManifest,
    [switch]$ConfigureSettings,
    [string]$OpenAIEndpoint,
    [string]$OpenAIDeployment,
    [string]$OpenAIKey,
    [string]$AadTenantId,
    [string]$AadClientId,
    [string]$AadClientSecret,
    [string]$BotClientId,
    [string]$BotClientSecret,
    [string]$BotTenantId,
    [switch]$DryRun,
    [switch]$Help
)

$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir
$AppDir = Join-Path $ProjectRoot "src\AgentOrchestrator"

# ============================================================================
# FUNCTIONS
# ============================================================================

function Write-Header {
    param([string]$Message)
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host $Message -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""
}

function Write-Step {
    param([string]$Message)
    Write-Host "[STEP] $Message" -ForegroundColor Blue
}

function Write-Success {
    param([string]$Message)
    Write-Host "[SUCCESS] $Message" -ForegroundColor Green
}

function Write-Warning {
    param([string]$Message)
    Write-Host "[WARNING] $Message" -ForegroundColor Yellow
}

function Write-Error {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor Red
}

function Show-Help {
    Write-Host @"
Azure Deployment Script for Agent Orchestrator

Usage: .\deploy.ps1 [OPTIONS]

Options:
  -AppName NAME          App Service name (default: agent-orchestrator)
  -ResourceGroup RG      Resource group name (default: rg-agent-orchestrator)
  -Location LOC          Azure region (default: eastus)
  -PlanName PLAN         App Service Plan name (default: plan-agent-orchestrator)
  -Sku SKU               App Service SKU (default: B1)
  -CreateResources       Create Azure resources if they don't exist
  -SkipBuild             Skip dotnet build/publish step
  -UpdateManifest        Update Teams manifest with app details
  -PackageManifest       Create Teams app package (manifest.zip)
  -ConfigureSettings     Configure app settings in Azure

Azure OpenAI Settings:
  -OpenAIEndpoint URL    Azure OpenAI endpoint URL
  -OpenAIDeployment N    Azure OpenAI deployment name
  -OpenAIKey KEY         Azure OpenAI API key

Azure AD Settings (for user auth):
  -AadTenantId ID        Azure AD tenant ID
  -AadClientId ID        Azure AD app client ID
  -AadClientSecret S     Azure AD app client secret

Bot Service Settings:
  -BotClientId ID        Bot Microsoft App ID
  -BotClientSecret S     Bot client secret
  -BotTenantId ID        Bot tenant ID

Other Options:
  -DryRun                Show what would be done without executing
  -Help                  Show this help message

Examples:
  # Quick deploy (existing resources)
  .\deploy.ps1 -AppName my-agent

  # Full deployment with all settings
  .\deploy.ps1 -AppName my-agent -ResourceGroup rg-myagent -CreateResources ``
     -ConfigureSettings ``
     -OpenAIEndpoint "https://myresource.openai.azure.com/" ``
     -OpenAIDeployment "gpt-4o" -OpenAIKey "<key>" ``
     -AadTenantId "<tenant>" -AadClientId "<id>" -AadClientSecret "<secret>"
"@
}

function Test-Prerequisites {
    Write-Step "Checking prerequisites..."

    # Check Azure CLI
    if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
        Write-Error "Azure CLI not found. Install from: https://docs.microsoft.com/cli/azure/install-azure-cli"
        exit 1
    }

    # Check if logged in to Azure
    $account = az account show 2>$null | ConvertFrom-Json
    if (-not $account) {
        Write-Error "Not logged in to Azure. Run: az login"
        exit 1
    }

    # Check .NET SDK
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        Write-Error ".NET SDK not found. Install from: https://dotnet.microsoft.com/download"
        exit 1
    }

    Write-Success "All prerequisites met"
}

function New-AzureResources {
    Write-Header "Creating Azure Resources"

    Write-Step "Creating resource group: $ResourceGroup"
    if ($DryRun) {
        Write-Host "  [DRY RUN] az group create --name $ResourceGroup --location $Location"
    } else {
        az group create --name $ResourceGroup --location $Location --output none
        Write-Success "Resource group created"
    }

    Write-Step "Creating App Service Plan: $PlanName"
    if ($DryRun) {
        Write-Host "  [DRY RUN] az appservice plan create --name $PlanName --resource-group $ResourceGroup --sku $Sku --is-linux"
    } else {
        az appservice plan create --name $PlanName --resource-group $ResourceGroup --sku $Sku --is-linux --output none
        Write-Success "App Service Plan created"
    }

    Write-Step "Creating Web App: $AppName"
    if ($DryRun) {
        Write-Host "  [DRY RUN] az webapp create --name $AppName --resource-group $ResourceGroup --plan $PlanName --runtime DOTNETCORE:10.0"
    } else {
        az webapp create --name $AppName --resource-group $ResourceGroup --plan $PlanName --runtime "DOTNETCORE:10.0" --output none
        Write-Success "Web App created"
    }

    Write-Host ""
    Write-Success "Azure resources created successfully"
    Write-Host ""
    Write-Host "Web App URL: https://$AppName.azurewebsites.net"
}

function Build-Application {
    Write-Header "Building Application"

    Set-Location $AppDir

    Write-Step "Cleaning previous builds..."
    if (-not $DryRun) {
        Remove-Item -Path ".\publish" -Recurse -Force -ErrorAction SilentlyContinue
        Remove-Item -Path ".\deploy.zip" -Force -ErrorAction SilentlyContinue
    }

    Write-Step "Publishing application..."
    if ($DryRun) {
        Write-Host "  [DRY RUN] dotnet publish -c Release -o ./publish"
    } else {
        dotnet publish -c Release -o ./publish --nologo
        Write-Success "Application published"
    }

    Write-Step "Creating deployment package..."
    if ($DryRun) {
        Write-Host "  [DRY RUN] Compress-Archive -Path ./publish/* -DestinationPath ./deploy.zip"
    } else {
        Compress-Archive -Path ".\publish\*" -DestinationPath ".\deploy.zip" -Force
        Write-Success "Deployment package created: deploy.zip"
    }

    Set-Location $ProjectRoot
}

function Deploy-Application {
    Write-Header "Deploying to Azure"

    Write-Step "Deploying to App Service: $AppName"
    if ($DryRun) {
        Write-Host "  [DRY RUN] az webapp deployment source config-zip --resource-group $ResourceGroup --name $AppName --src $AppDir\deploy.zip"
    } else {
        az webapp deployment source config-zip --resource-group $ResourceGroup --name $AppName --src "$AppDir\deploy.zip" --output none
        Write-Success "Application deployed"
    }

    Write-Step "Restarting App Service..."
    if ($DryRun) {
        Write-Host "  [DRY RUN] az webapp restart --resource-group $ResourceGroup --name $AppName"
    } else {
        az webapp restart --resource-group $ResourceGroup --name $AppName --output none
        Write-Success "App Service restarted"
    }
}

function Set-AppSettings {
    Write-Header "Configuring App Settings"

    $settings = @()
    $hasSettings = $false

    # Azure OpenAI settings
    if ($OpenAIEndpoint) { $settings += "AzureOpenAI__Endpoint=$OpenAIEndpoint"; $hasSettings = $true }
    if ($OpenAIDeployment) { $settings += "AzureOpenAI__DeploymentName=$OpenAIDeployment"; $hasSettings = $true }
    if ($OpenAIKey) { $settings += "AzureOpenAI__ApiKey=$OpenAIKey"; $hasSettings = $true }

    # Azure AD settings
    if ($AadTenantId) { $settings += "AzureAd__TenantId=$AadTenantId"; $hasSettings = $true }
    if ($AadClientId) { $settings += "AzureAd__ClientId=$AadClientId"; $hasSettings = $true }
    if ($AadClientSecret) { $settings += "AzureAd__ClientSecret=$AadClientSecret"; $hasSettings = $true }
    if ($AadClientId) { $settings += "AzureAd__RedirectUri=https://$AppName.azurewebsites.net/auth/callback" }

    # Bot Service settings
    if ($BotClientId) { $settings += "Connections__BotServiceConnection__Settings__ClientId=$BotClientId"; $hasSettings = $true }
    if ($BotClientSecret) { $settings += "Connections__BotServiceConnection__Settings__ClientSecret=$BotClientSecret"; $hasSettings = $true }
    if ($BotTenantId) { $settings += "Connections__BotServiceConnection__Settings__TenantId=$BotTenantId"; $hasSettings = $true }

    if (-not $hasSettings) {
        Write-Warning "No settings provided. Use -Help to see available options."
        return
    }

    Write-Step "Applying app settings..."

    if ($DryRun) {
        Write-Host "  [DRY RUN] Settings to apply:"
        foreach ($setting in $settings) {
            $key = $setting.Split('=')[0]
            if ($key -match "Secret|Key") {
                Write-Host "    $key = *******"
            } else {
                Write-Host "    $setting"
            }
        }
    } else {
        az webapp config appsettings set --resource-group $ResourceGroup --name $AppName --settings @settings --output none
        Write-Success "App settings configured"
    }

    Write-Host ""
    Write-Host "Settings applied:"
    foreach ($setting in $settings) {
        $key = $setting.Split('=')[0]
        if ($key -match "Secret|Key") {
            Write-Host "  $key = *******"
        } else {
            Write-Host "  $setting"
        }
    }
}

function New-ManifestPackage {
    Write-Header "Creating Teams App Package"

    $manifestDir = Join-Path $AppDir "appPackage\build"
    $outputFile = Join-Path $AppDir "appPackage\manifest.zip"

    if (-not (Test-Path $manifestDir)) {
        Write-Error "Manifest directory not found: $manifestDir"
        return
    }

    if (-not (Test-Path (Join-Path $manifestDir "manifest.json"))) {
        Write-Error "manifest.json not found in $manifestDir"
        return
    }

    Write-Step "Creating manifest.zip..."

    if ($DryRun) {
        Write-Host "  [DRY RUN] Compress-Archive manifest.json, color.png, outline.png to $outputFile"
    } else {
        $filesToZip = @(
            (Join-Path $manifestDir "manifest.json"),
            (Join-Path $manifestDir "color.png"),
            (Join-Path $manifestDir "outline.png")
        )
        Remove-Item -Path $outputFile -Force -ErrorAction SilentlyContinue
        Compress-Archive -Path $filesToZip -DestinationPath $outputFile -Force
        Write-Success "Teams app package created: $outputFile"
    }

    Write-Host ""
    Write-Host "To deploy the Teams app:"
    Write-Host "  1. Go to Teams Admin Center: https://admin.teams.microsoft.com"
    Write-Host "  2. Navigate to Teams apps > Manage apps > Upload new app"
    Write-Host "  3. Upload: $outputFile"
}

function Show-Summary {
    Write-Header "Deployment Summary"

    Write-Host "Configuration:"
    Write-Host "  Resource Group: $ResourceGroup"
    Write-Host "  Location:       $Location"
    Write-Host "  App Name:       $AppName"
    Write-Host "  Plan Name:      $PlanName"
    Write-Host "  SKU:            $Sku"
    Write-Host ""
    Write-Host "URLs:"
    Write-Host "  Web App:        https://$AppName.azurewebsites.net"
    Write-Host "  Health Check:   https://$AppName.azurewebsites.net/health"
    Write-Host "  Swagger:        https://$AppName.azurewebsites.net/swagger"
    Write-Host "  Bot Endpoint:   https://$AppName.azurewebsites.net/api/messages"
    Write-Host ""
    Write-Host "Next Steps:"
    Write-Host "  1. Configure app settings in Azure Portal (see docs/AZURE_DEPLOYMENT.md)"
    Write-Host "  2. Create Azure Bot and link to this App Service"
    Write-Host "  3. Update Teams manifest with Bot App ID"
    Write-Host "  4. Deploy Teams app to your organization"
    Write-Host ""
    Write-Host "View logs:"
    Write-Host "  az webapp log tail --resource-group $ResourceGroup --name $AppName"
}

# ============================================================================
# MAIN EXECUTION
# ============================================================================

if ($Help) {
    Show-Help
    exit 0
}

Write-Header "Agent Orchestrator Deployment"

if ($DryRun) {
    Write-Warning "DRY RUN MODE - No changes will be made"
    Write-Host ""
}

Write-Host "Configuration:"
Write-Host "  App Name:       $AppName"
Write-Host "  Resource Group: $ResourceGroup"
Write-Host "  Location:       $Location"
Write-Host ""

Test-Prerequisites

if ($CreateResources) {
    New-AzureResources
}

if (-not $SkipBuild) {
    Build-Application
}

Deploy-Application

if ($UpdateManifest) {
    Write-Header "Updating Teams Manifest"
    Write-Host "To update the manifest, replace these placeholders:"
    Write-Host "  {{BOT_APP_ID}} -> Your Bot's Microsoft App ID"
    Write-Host "  {{BOT_DOMAIN}} -> $AppName.azurewebsites.net"
}

if ($PackageManifest) {
    New-ManifestPackage
}

if ($ConfigureSettings) {
    Set-AppSettings
}

Show-Summary

Write-Success "Deployment complete!"
