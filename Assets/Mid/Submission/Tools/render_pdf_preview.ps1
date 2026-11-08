param([string]$PdfPath,[string]$OutputFolder)
$ErrorActionPreference='Stop'
$PdfPath=[IO.Path]::GetFullPath($PdfPath)
Add-Type -AssemblyName System.Runtime.WindowsRuntime
$null=[Windows.Storage.StorageFile,Windows.Storage,ContentType=WindowsRuntime]
$null=[Windows.Data.Pdf.PdfDocument,Windows.Data.Pdf,ContentType=WindowsRuntime]
$null=[Windows.Storage.Streams.InMemoryRandomAccessStream,Windows.Storage.Streams,ContentType=WindowsRuntime]
$null=[Windows.Storage.Streams.DataReader,Windows.Storage.Streams,ContentType=WindowsRuntime]
$operationTask=[System.WindowsRuntimeSystemExtensions].GetMethods() | Where-Object { $_.Name -eq 'AsTask' -and $_.IsGenericMethodDefinition -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncOperation`1' } | Select-Object -First 1
$actionTask=[System.WindowsRuntimeSystemExtensions].GetMethods() | Where-Object { $_.Name -eq 'AsTask' -and -not $_.IsGenericMethod -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncAction' } | Select-Object -First 1
function Wait-Operation($operation,[Type]$resultType) {
    $task=$operationTask.MakeGenericMethod($resultType).Invoke($null,@($operation));$task.Wait();return $task.Result
}
$file=Wait-Operation ([Windows.Storage.StorageFile]::GetFileFromPathAsync($PdfPath)) ([Windows.Storage.StorageFile])
$pdf=Wait-Operation ([Windows.Data.Pdf.PdfDocument]::LoadFromFileAsync($file)) ([Windows.Data.Pdf.PdfDocument])
New-Item -ItemType Directory -Path $OutputFolder -Force | Out-Null
for($pageIndex=0;$pageIndex -lt $pdf.PageCount;$pageIndex++) {
    $page=$pdf.GetPage($pageIndex)
    $stream=New-Object Windows.Storage.Streams.InMemoryRandomAccessStream
    $task=$actionTask.Invoke($null,@($page.RenderToStreamAsync($stream)));$task.Wait()
    $reader=New-Object Windows.Storage.Streams.DataReader($stream.GetInputStreamAt(0))
    $null=Wait-Operation ($reader.LoadAsync([uint32]$stream.Size)) ([uint32])
    $bytes=New-Object byte[] ([int]$stream.Size);$reader.ReadBytes($bytes)
    [IO.File]::WriteAllBytes((Join-Path $OutputFolder ('Page_'+($pageIndex+1)+'.png')),$bytes)
    $reader.Dispose();$stream.Dispose();$page.Dispose()
}
Write-Output ('Rendered '+$pdf.PageCount+' PDF pages for visual review.')
