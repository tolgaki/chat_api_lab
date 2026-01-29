# ============================================================================
# Configure App Settings for Agent Orchestrator (PowerShell)
# ============================================================================
# This script configures Azure App Service settings without redeploying.
# Useful for updating Azure OpenAI endpoints, API keys, or other settings.
#
# Usage:
#   .\scripts\configure-appsettings.ps1 -Help
#   .\scripts\configure-appsettings.ps1 -OpenAIEndpoint URL -OpenAIDeployment NAME
# ============================================================================

[CmdletBinding()]
param(
    [string]$AppName = "agent-orchestrator",
    [string]$ResourceGroup = "rg-agent-orchestrator",
    [string]$OpenAIEndpoint,
    [string]$OpenAIDeployment,
    [string]$OpenAIKey,
    [string]$BotClientId,
    [string]$BotClientSecret,
    [string]$BotTenantId,
    [string]$AadTenantId,
    [string]$AadClientId,
    [string]$AadClientSecret,
    [switch]$ShowCurrent,
    [switch]$DryRun,
    [switch]$Help
)

$ErrorActionPreference = "Stop"

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

function Show-Help {
    Write-Host @"
Configure App Settings for Agent Orchestrator

Usage: .\configure-appsettings.ps1 [OPTIONS]

Azure Resource Options:
  -AppName NAME           App Service name (default: agent-orchestrator)
  -ResourceGroup RG       Resource group (default: rg-agent-orchestrator)

Azure OpenAI Settings:
  -OpenAIEndpoint URL     Azure OpenAI endpoint URL
  -OpenAIDeployment N     Azure OpenAI deployment name
  -OpenAIKey KEY          Azure OpenAI API key

Bot Service Settings:
  -BotClientId ID         Bot Microsoft App ID
  -BotClientSecret S      Bot client secret
  -BotTenantId ID         Bot tenant ID

Azure AD Settings:
  -AadTenantId ID         Azure AD tenant ID
  -AadClientId ID         Azure AD client ID
  -AadClientSecret S      Azure AD client secret

Other Options:
  -ShowCurrent            Show current app settings
  -DryRun                 Show what would be done
  -Help                   Show this help

Examples:
  # Update Azure OpenAI endpoint
  .\configure-appsettings.ps1 -OpenAIEndpoint "https://myresource.openai.azure.com/" ``
     -OpenAIDeployment "gpt-5-chat"

  # Show current settings
  .\configure-appsettings.ps1 -ShowCurrent
"@
}

function Show-CurrentSettings {
    Write-Header "Current App Settings"

    Write-Step "Fetching settings from $AppName..."

    $settings = az webapp config appsettings list `
        --resource-group $ResourceGroup `
        --name $AppName `
        --query "[?contains(name, 'AzureOpenAI') || contains(name, 'AzureAd') || contains(name, 'BotServiceConnection')].{Name:name, Value:value}" `
        --output table

    Write-Host $settings

    Write-Host ""
    Write-Step "Connection strings:"
    $connStrings = az webapp config connection-string list `
        --resource-group $ResourceGroup `
        --name $AppName `
        --output table 2>$null
    
    if ($connStrings) {
        Write-Host $connStrings
    } else {
        Write-Host "  No connection strings configured"
    }
}

# Main
if ($Help) {
    Show-Help
    exit 0
}

Write-Header "Configure App Settings"

Write-Host "Target:"
Write-Host "  App Name:       $AppName"
Write-Host "  Resource Group: $ResourceGroup"
Write-Host ""

# Check Azure CLI
if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    Write-Host "[ERROR] Azure CLI not found" -ForegroundColor Red
    exit 1
}

$account = az account show 2>$null | ConvertFrom-Json
if (-not $account) {
    Write-Host "[ERROR] Not logged in to Azure. Run: az login" -ForegroundColor Red
    exit 1
}

# Show current settings if requested
if ($ShowCurrent) {
    Show-CurrentSettings
    exit 0
}

# Build settings array
$settings = @()

# Azure OpenAI settings
if ($OpenAIEndpoint) { $settings += "AzureOpenAI__Endpoint=$OpenAIEndpoint" }
if ($OpenAIDeployment) { $settings += "AzureOpenAI__DeploymentName=$OpenAIDeployment" }
if ($OpenAIKey) { $settings += "AzureOpenAI__ApiKey=$OpenAIKey" }

# Bot Service settings
if ($BotClientId) { $settings += "Connections__BotServiceConnection__Settings__ClientId=$BotClientId" }
if ($BotClientSecret) { $settings += "Connections__BotServiceConnection__Settings__ClientSecret=$BotClientSecret" }
if ($BotTenantId) { $settings += "Connections__BotServiceConnection__Settings__TenantId=$BotTenantId" }

# Azure AD settings
if ($AadTenantId) { $settings += "AzureAd__TenantId=$AadTenantId" }
if ($AadClientId) { $settings += "AzureAd__ClientId=$AadClientId" }
if ($AadClientSecret) { $settings += "AzureAd__ClientSecret=$AadClientSecret" }

# Check if any settings to apply
if ($settings.Count -eq 0) {
    Write-Host "[WARNING] No settings provided. Use -Help to see available options." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Common usage:"
    Write-Host '  .\configure-appsettings.ps1 -OpenAIEndpoint "https://myresource.openai.azure.com/" -OpenAIDeployment "gpt-5-chat"'
    Write-Host "  .\configure-appsettings.ps1 -ShowCurrent"
    exit 1
}

# Apply settings
Write-Step "Applying settings..."
Write-Host ""

if ($DryRun) {
    Write-Host "[DRY RUN] Would set:"
    foreach ($setting in $settings) {
        $key = $setting.Split('=')[0]
        $value = $setting.Split('=')[1]
        if ($key -match "Secret|Key") {
            Write-Host "  $key = *******"
        } else {
            Write-Host "  $key = $value"
        }
    }
} else {
    az webapp config appsettings set `
        --resource-group $ResourceGroup `
        --name $AppName `
        --settings @settings `
        --output none

    Write-Success "Settings applied"
    Write-Host ""
    Write-Host "Applied settings:"
    foreach ($setting in $settings) {
        $key = $setting.Split('=')[0]
        $value = $setting.Split('=')[1]
        if ($key -match "Secret|Key") {
            Write-Host "  $key = *******"
        } else {
            Write-Host "  $key = $value"
        }
    }
}

Write-Host ""
Write-Step "Restarting app to apply changes..."

if ($DryRun) {
    Write-Host "[DRY RUN] az webapp restart --resource-group $ResourceGroup --name $AppName"
} else {
    az webapp restart `
        --resource-group $ResourceGroup `
        --name $AppName `
        --output none
    Write-Success "App restarted"
}

Write-Host ""
Write-Success "Configuration complete!"
