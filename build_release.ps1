# KoReadingABook デュアルリリースビルドスクリプト
# 2つのビルドを作成: フレームワーク依存版（軽量）と自己完結型版（単一EXE）

param(
    [string]$Version = "1.0.1"
)

$ErrorActionPreference = "Stop"

Write-Host "🚀 Starting Dual Release Build for KoReadingABook v$Version..." -ForegroundColor Green
Write-Host ""

# 変数定義
$ProjectFile = "KoReadingABook.csproj"
$DistDir = "dist"
$TempFrameworkDir = "$DistDir\temp_framework"
$TempStandaloneDir = "$DistDir\temp_standalone"
$FrameworkZipFile = "$DistDir\KoReadingABook-v$Version-framework-dependent-release.zip"
$StandaloneZipFile = "$DistDir\KoReadingABook-v$Version-standalone-release.zip"

# ビルド開始時刻を記録
$BuildStartTime = Get-Date

# 1. Clean Distribution Directory
if (Test-Path $DistDir) {
    Write-Host "Cleaning dist directory..." -ForegroundColor Cyan
    Remove-Item $DistDir -Recurse -Force
}
New-Item -ItemType Directory -Path $DistDir | Out-Null

# ========================================
# フレームワーク依存ビルド（軽量版）
# ========================================
Write-Host "Building Framework-Dependent (Lightweight)..." -ForegroundColor Cyan
$frameworkBuildSuccess = $false
try {
    New-Item -ItemType Directory -Path $TempFrameworkDir | Out-Null
    
    Write-Host "  Compiling and Publishing..." -ForegroundColor Gray
    dotnet publish $ProjectFile `
        -c Release `
        -r win-x64 `
        --no-self-contained `
        -p:PublishSingleFile=true `
        -o $TempFrameworkDir

    if ($LASTEXITCODE -eq 0) {
        # Copy README
        if (Test-Path "README.md") {
            Copy-Item "README.md" $TempFrameworkDir
        }
        
        # Create ZIP
        Compress-Archive -Path "$TempFrameworkDir\*" -DestinationPath $FrameworkZipFile
        Write-Host "  ✓ Framework-dependent build completed" -ForegroundColor Green
        $frameworkBuildSuccess = $true
    }
    else {
        throw "Build failed with exit code $LASTEXITCODE"
    }
}
catch {
    Write-Host "  ✗ Framework-dependent build failed: $($_.Exception.Message)" -ForegroundColor Red
}

# ========================================
# 自己完結型ビルド（単一EXE版）
# ========================================
Write-Host ""
Write-Host "Building Self-Contained (Single EXE)..." -ForegroundColor Cyan
$standaloneBuildSuccess = $false
try {
    New-Item -ItemType Directory -Path $TempStandaloneDir | Out-Null
    
    Write-Host "  Compiling and Publishing..." -ForegroundColor Gray
    dotnet publish $ProjectFile `
        -c Release `
        -r win-x64 `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -o $TempStandaloneDir

    if ($LASTEXITCODE -eq 0) {
        # Copy README
        if (Test-Path "README.md") {
            Copy-Item "README.md" $TempStandaloneDir
        }
        
        # Create ZIP
        Compress-Archive -Path "$TempStandaloneDir\*" -DestinationPath $StandaloneZipFile
        Write-Host "  ✓ Self-contained build completed" -ForegroundColor Green
        $standaloneBuildSuccess = $true
    }
    else {
        throw "Build failed with exit code $LASTEXITCODE"
    }
}
catch {
    Write-Host "  ✗ Self-contained build failed: $($_.Exception.Message)" -ForegroundColor Red
}

# 両方のビルドが失敗した場合はエラー終了
if (-not $frameworkBuildSuccess -and -not $standaloneBuildSuccess) {
    Write-Error "❌ Both builds failed"
    exit 1
}

# Cleanup Temporary Files
Write-Host ""
Write-Host "Cleaning up temporary files..." -ForegroundColor Cyan
if (Test-Path $TempFrameworkDir) {
    Remove-Item $TempFrameworkDir -Recurse -Force
}
if (Test-Path $TempStandaloneDir) {
    Remove-Item $TempStandaloneDir -Recurse -Force
}
Write-Host "Cleanup completed" -ForegroundColor Green
Write-Host ""

# ビルド結果のサマリー表示
$BuildEndTime = Get-Date
$BuildDuration = $BuildEndTime - $BuildStartTime
$BuildTimeSeconds = [math]::Round($BuildDuration.TotalSeconds, 1)

Write-Host "✅ Build Complete!" -ForegroundColor Green
Write-Host ""

# フレームワーク依存ビルドの情報
if ($frameworkBuildSuccess -and (Test-Path $FrameworkZipFile)) {
    $frameworkZipInfo = Get-Item $FrameworkZipFile
    $frameworkZipHash = Get-FileHash $FrameworkZipFile -Algorithm SHA256
    
    Write-Host "📦 Framework-Dependent Build (Lightweight):" -ForegroundColor Cyan
    Write-Host "   File: $($frameworkZipInfo.Name)" -ForegroundColor White
    Write-Host "   Size: $([math]::Round($frameworkZipInfo.Length / 1MB, 2)) MB" -ForegroundColor White
    Write-Host "   SHA256: $($frameworkZipHash.Hash)" -ForegroundColor Gray
    Write-Host "   ⚠ Requires .NET 10.0 Desktop Runtime" -ForegroundColor Yellow
    Write-Host ""
}

# 自己完結型ビルドの情報
if ($standaloneBuildSuccess -and (Test-Path $StandaloneZipFile)) {
    $standaloneZipInfo = Get-Item $StandaloneZipFile
    $standaloneZipHash = Get-FileHash $StandaloneZipFile -Algorithm SHA256
    
    Write-Host "📦 Self-Contained Build (Single EXE):" -ForegroundColor Cyan
    Write-Host "   File: $($standaloneZipInfo.Name)" -ForegroundColor White
    Write-Host "   Size: $([math]::Round($standaloneZipInfo.Length / 1MB, 2)) MB" -ForegroundColor White
    Write-Host "   SHA256: $($standaloneZipHash.Hash)" -ForegroundColor Gray
    Write-Host "   ✓ No .NET Runtime installation required" -ForegroundColor Green
    Write-Host ""
}

Write-Host "⏱ Total build time: $BuildTimeSeconds seconds" -ForegroundColor White
Write-Host "📦 Output: $DistDir\" -ForegroundColor White
Write-Host ""
