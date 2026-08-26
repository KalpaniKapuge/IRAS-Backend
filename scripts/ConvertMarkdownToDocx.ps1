param(
    [Parameter(Mandatory = $true)]
    [string]$InputPath,

    [Parameter(Mandatory = $true)]
    [string]$OutputPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Escape-Xml {
    param([AllowNull()][string]$Text)
    if ($null -eq $Text) { return "" }
    return [System.Security.SecurityElement]::Escape($Text)
}

function Strip-InlineMarkdown {
    param([string]$Text)
    $value = $Text
    $value = $value -replace '\*\*(.*?)\*\*', '$1'
    $value = $value -replace '`([^`]*)`', '$1'
    return $value
}

function New-Paragraph {
    param(
        [string]$Text,
        [string]$Style = "",
        [switch]$Bullet,
        [int]$NumberLevel = -1
    )

    $styleXml = ""
    if ($Style) {
        $styleXml = "<w:pPr><w:pStyle w:val=""$Style"" /></w:pPr>"
    }
    elseif ($Bullet) {
        $styleXml = "<w:pPr><w:numPr><w:ilvl w:val=""0""/><w:numId w:val=""1""/></w:numPr></w:pPr>"
    }
    elseif ($NumberLevel -ge 0) {
        $styleXml = "<w:pPr><w:numPr><w:ilvl w:val=""0""/><w:numId w:val=""2""/></w:numPr></w:pPr>"
    }

    $escaped = Escape-Xml (Strip-InlineMarkdown $Text)
    return "<w:p>$styleXml<w:r><w:t xml:space=""preserve"">$escaped</w:t></w:r></w:p>"
}

$inputFull = (Resolve-Path -LiteralPath $InputPath).Path
$outputFull = [System.IO.Path]::GetFullPath($OutputPath)
$outputDir = [System.IO.Path]::GetDirectoryName($outputFull)
if (-not [System.IO.Directory]::Exists($outputDir)) {
    [System.IO.Directory]::CreateDirectory($outputDir) | Out-Null
}

$buildRoot = Join-Path $env:TEMP ("iras-docx-" + [System.Guid]::NewGuid().ToString("N"))
[System.IO.Directory]::CreateDirectory($buildRoot) | Out-Null

try {
    $relsDir = Join-Path $buildRoot "_rels"
    $wordDir = Join-Path $buildRoot "word"
    [System.IO.Directory]::CreateDirectory($relsDir) | Out-Null
    [System.IO.Directory]::CreateDirectory($wordDir) | Out-Null

    $lines = Get-Content -LiteralPath $inputFull
    $paragraphs = New-Object System.Collections.Generic.List[string]

    foreach ($line in $lines) {
        $trim = $line.Trim()
        if ($trim.Length -eq 0) {
            $paragraphs.Add("<w:p/>")
            continue
        }
        if ($trim -match '^---+$') {
            $paragraphs.Add("<w:p/>")
            continue
        }
        if ($trim -match '^# (.+)$') {
            $paragraphs.Add((New-Paragraph $Matches[1] "Heading1"))
            continue
        }
        if ($trim -match '^## (.+)$') {
            $paragraphs.Add((New-Paragraph $Matches[1] "Heading2"))
            continue
        }
        if ($trim -match '^### (.+)$') {
            $paragraphs.Add((New-Paragraph $Matches[1] "Heading3"))
            continue
        }
        if ($trim -match '^#### (.+)$') {
            $paragraphs.Add((New-Paragraph $Matches[1] "Heading4"))
            continue
        }
        if ($trim -match '^- (.+)$') {
            $paragraphs.Add((New-Paragraph $Matches[1] -Bullet))
            continue
        }
        if ($trim -match '^\d+\. (.+)$') {
            $paragraphs.Add((New-Paragraph $Matches[1] -NumberLevel 0))
            continue
        }
        $paragraphs.Add((New-Paragraph $trim))
    }

    $contentTypes = @'
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
  <Default Extension="xml" ContentType="application/xml"/>
  <Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/>
  <Override PartName="/word/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml"/>
  <Override PartName="/word/numbering.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.numbering+xml"/>
</Types>
'@

    $packageRels = @'
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/>
</Relationships>
'@

    $docRelsDir = Join-Path $wordDir "_rels"
    [System.IO.Directory]::CreateDirectory($docRelsDir) | Out-Null
    $docRels = @'
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>
  <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/numbering" Target="numbering.xml"/>
</Relationships>
'@

    $styles = @'
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:styles xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
  <w:style w:type="paragraph" w:default="1" w:styleId="Normal">
    <w:name w:val="Normal"/>
    <w:qFormat/>
    <w:pPr><w:spacing w:after="160" w:line="276" w:lineRule="auto"/></w:pPr>
    <w:rPr><w:rFonts w:ascii="Calibri" w:hAnsi="Calibri"/><w:sz w:val="22"/></w:rPr>
  </w:style>
  <w:style w:type="paragraph" w:styleId="Heading1">
    <w:name w:val="heading 1"/><w:basedOn w:val="Normal"/><w:next w:val="Normal"/><w:qFormat/>
    <w:pPr><w:spacing w:before="300" w:after="160"/></w:pPr>
    <w:rPr><w:b/><w:color w:val="172033"/><w:sz w:val="32"/></w:rPr>
  </w:style>
  <w:style w:type="paragraph" w:styleId="Heading2">
    <w:name w:val="heading 2"/><w:basedOn w:val="Normal"/><w:next w:val="Normal"/><w:qFormat/>
    <w:pPr><w:spacing w:before="260" w:after="140"/></w:pPr>
    <w:rPr><w:b/><w:color w:val="1D2A3D"/><w:sz w:val="28"/></w:rPr>
  </w:style>
  <w:style w:type="paragraph" w:styleId="Heading3">
    <w:name w:val="heading 3"/><w:basedOn w:val="Normal"/><w:next w:val="Normal"/><w:qFormat/>
    <w:pPr><w:spacing w:before="220" w:after="120"/></w:pPr>
    <w:rPr><w:b/><w:color w:val="344154"/><w:sz w:val="25"/></w:rPr>
  </w:style>
  <w:style w:type="paragraph" w:styleId="Heading4">
    <w:name w:val="heading 4"/><w:basedOn w:val="Normal"/><w:next w:val="Normal"/><w:qFormat/>
    <w:pPr><w:spacing w:before="180" w:after="100"/></w:pPr>
    <w:rPr><w:b/><w:color w:val="344154"/><w:sz w:val="23"/></w:rPr>
  </w:style>
</w:styles>
'@

    $numbering = @'
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:numbering xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
  <w:abstractNum w:abstractNumId="1">
    <w:multiLevelType w:val="hybridMultilevel"/>
    <w:lvl w:ilvl="0"><w:start w:val="1"/><w:numFmt w:val="bullet"/><w:lvlText w:val="•"/><w:lvlJc w:val="left"/><w:pPr><w:ind w:left="720" w:hanging="360"/></w:pPr></w:lvl>
  </w:abstractNum>
  <w:num w:numId="1"><w:abstractNumId w:val="1"/></w:num>
  <w:abstractNum w:abstractNumId="2">
    <w:multiLevelType w:val="hybridMultilevel"/>
    <w:lvl w:ilvl="0"><w:start w:val="1"/><w:numFmt w:val="decimal"/><w:lvlText w:val="%1."/><w:lvlJc w:val="left"/><w:pPr><w:ind w:left="720" w:hanging="360"/></w:pPr></w:lvl>
  </w:abstractNum>
  <w:num w:numId="2"><w:abstractNumId w:val="2"/></w:num>
</w:numbering>
'@

    $body = [string]::Join("`n", $paragraphs)
    $document = @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
  <w:body>
$body
    <w:sectPr>
      <w:pgSz w:w="12240" w:h="15840"/>
      <w:pgMar w:top="1440" w:right="1440" w:bottom="1440" w:left="1440" w:header="720" w:footer="720" w:gutter="0"/>
    </w:sectPr>
  </w:body>
</w:document>
"@

    Set-Content -LiteralPath (Join-Path $buildRoot "[Content_Types].xml") -Value $contentTypes -Encoding UTF8
    Set-Content -LiteralPath (Join-Path $relsDir ".rels") -Value $packageRels -Encoding UTF8
    Set-Content -LiteralPath (Join-Path $docRelsDir "document.xml.rels") -Value $docRels -Encoding UTF8
    Set-Content -LiteralPath (Join-Path $wordDir "styles.xml") -Value $styles -Encoding UTF8
    Set-Content -LiteralPath (Join-Path $wordDir "numbering.xml") -Value $numbering -Encoding UTF8
    Set-Content -LiteralPath (Join-Path $wordDir "document.xml") -Value $document -Encoding UTF8

    if (Test-Path -LiteralPath $outputFull) {
        Remove-Item -LiteralPath $outputFull -Force
    }

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::CreateFromDirectory($buildRoot, $outputFull)
}
finally {
    if (Test-Path -LiteralPath $buildRoot) {
        Remove-Item -LiteralPath $buildRoot -Recurse -Force
    }
}

Write-Output $outputFull
