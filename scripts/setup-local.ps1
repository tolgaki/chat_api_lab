# ============================================================================
# Local Development Setup Script (PowerShell)
# ============================================================================
# Sets up .NET user secrets for local development.
# Usage: .\scripts\setup-local.ps1
# ============================================================================

$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir
$SrcDir = Join-Path $ProjectRoot "src\AgentOrchestrator"

Write-Host "========================================" -ForegroundColor Green
Write-Host "Local Development Setup" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "This script configures .NET user secrets for local development."
Write-Host "You'll need values from Azure Portal."
Write-Host ""

# Check prerequisites
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "Error: .NET SDK not found. Install from: https://dotnet.microsoft.com/download" -ForegroundColor Red
    exit 1
}

Set-Location $SrcDir

# Initialize user secrets if not already done
$secretsList = dotnet user-secrets list 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "Initializing user secrets..." -ForegroundColor Blue
    dotnet user-secrets init
}

Write-Host ""
Write-Host "Enter your configuration values (from Azure Portal):" -ForegroundColor Yellow
Write-Host ""

# Prompt for values
$TenantId = Read-Host "Tenant ID"
$ClientId = Read-Host "Client ID (Azure AD App)"
$ClientSecret = Read-Host "Client Secret" -AsSecureString
$ClientSecretPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($ClientSecret))

$OpenAIEndpoint = Read-Host "Azure OpenAI Endpoint (e.g., https://myresource.openai.azure.com/)"
$OpenAIKey = Read-Host "Azure OpenAI API Key" -AsSecureString
$OpenAIKeyPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($OpenAIKey))

$OpenAIDeployment = Read-Host "Azure OpenAI Deployment Name (e.g., gpt-4o)"

Write-Host ""
Write-Host "Configuring user secrets..." -ForegroundColor Blue

# Set Azure AD secrets
dotnet user-secrets set "AzureAd:TenantId" $TenantId
dotnet user-secrets set "AzureAd:ClientId" $ClientId
dotnet user-secrets set "AzureAd:ClientSecret" $ClientSecretPlain

# Set Azure OpenAI secrets
dotnet user-secrets set "AzureOpenAI:Endpoint" $OpenAIEndpoint
dotnet user-secrets set "AzureOpenAI:ApiKey" $OpenAIKeyPlain
dotnet user-secrets set "AzureOpenAI:DeploymentName" $OpenAIDeployment

# Also set Bot Service connection (uses same credentials for local dev)
dotnet user-secrets set "Connections:BotServiceConnection:Settings:TenantId" $TenantId
dotnet user-secrets set "Connections:BotServiceConnection:Settings:ClientId" $ClientId
dotnet user-secrets set "Connections:BotServiceConnection:Settings:ClientSecret" $ClientSecretPlain

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "Setup Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:"
Write-Host ""
Write-Host "1. Add redirect URI to your Azure AD app:"
Write-Host "   http://localhost:5001/auth/callback"
Write-Host ""
Write-Host "2. Run the application:"
Write-Host "   cd src\AgentOrchestrator"
Write-Host '   dotnet run --urls "http://localhost:5001"'
Write-Host ""
Write-Host "3. Open http://localhost:5001 in your browser"
Write-Host ""
