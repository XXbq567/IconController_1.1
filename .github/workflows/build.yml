name: Build and Release

on:
  push:
    tags:
      - 'v*'
  workflow_dispatch:

jobs:
  build:
    runs-on: windows-latest

    steps:
    - uses: actions/checkout@v4

    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '6.0.x'

    - name: Build
      run: dotnet build src/IconController.csproj --configuration Release

    - name: Publish
      run: dotnet publish src/IconController.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ./publish

    - name: Upload artifact
      uses: actions/upload-artifact@v4
      with:
        name: IconController-win-x64
        path: ./publish/IconController.exe

    - name: Create Release
      if: startsWith(github.ref, 'refs/tags/')
      uses: softprops/action-gh-release@v1
      with:
        name: Release ${{ github.ref_name }}
        tag_name: ${{ github.ref }}
        body: |
          桌面图标控制器 ${{ github.ref_name }}
          - 一键切换桌面图标显示/隐藏
          - 默认快捷键: Alt+Ctrl+Q
        draft: false
        prerelease: false
      env:
        GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}

    - name: Upload Release Asset
      if: startsWith(github.ref, 'refs/tags/')
      uses: actions/upload-release-asset@v1
      env:
        GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
      with:
        upload_url: ${{ steps.create_release.outputs.upload_url }}
        asset_path: ./publish/IconController.exe
        asset_name: IconController.exe
        asset_content_type: application/octet-stream
