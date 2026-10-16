<#
.SYNOPSIS
	Scaffolds the new files for a "create"-shaped (POST) vertical slice in InvoiceBuilder,
	modelled on Api/Features/Customers/CustomerCreate.

.DESCRIPTION
	Generates, from ../assets/*.tmpl:
	  Application: request DTO, response DTO, Result record, FluentValidation validator, MediatR command + handler
	  Api:         minimal-API endpoint

	It only creates NEW files and never overwrites an existing one. It does not edit the feature's
	{Entity}Endpoint.cs registration, the domain entity, EF configuration/migrations, or tests -
	those need judgement, so the script prints what is left to do.

	Use -WhatIf to preview without writing anything.

.EXAMPLE
	./new-slice.ps1 -Feature Customers -Entity Customer -Operation Duplicate `
		-Fields "CompanyName:string,CustomerName:string,CustomerEmail:string" -WhatIf
#>
[CmdletBinding(SupportsShouldProcess)]
param(
	# Plural feature name, PascalCase. Matches the folder under Features/ and the DbSet name (Customers, Senders, Invoices).
	[Parameter(Mandatory)]
	[ValidatePattern('^[A-Z][A-Za-z0-9]*$')]
	[string]$Feature,

	# Singular entity name, PascalCase (Customer, Sender, Invoice). Domain/Entities/{Entity}.cs must already exist.
	[Parameter(Mandatory)]
	[ValidatePattern('^[A-Z][A-Za-z0-9]*$')]
	[string]$Entity,

	# Verb, PascalCase (Create, Import, Duplicate ...). Result is {Operation}{Entity}, e.g. CreateCustomer.
	[Parameter(Mandatory)]
	[ValidatePattern('^[A-Z][A-Za-z0-9]*$')]
	[string]$Operation,

	# Comma-separated Name:Type pairs for the request DTO, in the order the entity's Create(...) factory takes them.
	# Types: string, int, long, decimal, bool, Guid, DateTime, DateTimeOffset (append ? for nullable).
	[Parameter(Mandatory)]
	[string]$Fields,

	# Repo root. Defaults to four levels above this script (.claude/skills/create-feature/scripts -> repo root).
	[string]$Root
)

$ErrorActionPreference = 'Stop'

$allowedTypes = 'string', 'int', 'long', 'decimal', 'bool', 'Guid', 'DateTime', 'DateTimeOffset'
$assets = Join-Path $PSScriptRoot '..\assets'
if (-not $Root) { $Root = Join-Path $PSScriptRoot '..\..\..\..' }
$Root = (Resolve-Path $Root).Path

$entityFile = Join-Path $Root "backend\src\InvoiceBuilder.Domain\Entities\$Entity.cs"
if (-not (Test-Path $entityFile)) {
	throw "Domain entity not found: $entityFile. Add or extend the entity first (Create/Update factory pattern), then re-run."
}

$fieldList = foreach ($raw in $Fields.Split(',', [StringSplitOptions]::RemoveEmptyEntries)) {
	$parts = $raw.Trim().Split(':')
	if ($parts.Count -ne 2) { throw "Bad field '$raw'. Expected Name:Type." }
	$fieldName = $parts[0].Trim()
	$fieldType = $parts[1].Trim()
	if ($fieldName -notmatch '^[A-Z][A-Za-z0-9]*$') { throw "Field name '$fieldName' must be PascalCase." }
	if ($allowedTypes -notcontains $fieldType.TrimEnd('?')) { throw "Unsupported type '$fieldType' for '$fieldName'. Allowed: $($allowedTypes -join ', ') (append ? for nullable)." }
	[pscustomobject]@{ Name = $fieldName; Type = $fieldType }
}
if (-not $fieldList) { throw 'At least one field is required.' }

$tab = "`t"
$dtoParams = ($fieldList | ForEach-Object { "$tab$($_.Type) $($_.Name)" }) -join ",`n"
$createArgs = ($fieldList | ForEach-Object { "$tab$tab${tab}cmd.Dto.$($_.Name)" }) -join ",`n"

# Only string and Guid get a default rule; anything else is a domain decision the author should make.
$ruleLines = foreach ($f in $fieldList) {
	if ($f.Type -in 'string', 'Guid') { "$tab${tab}RuleFor(x => x.$($f.Name)).NotEmpty();" }
}
$validatorRules = if ($ruleLines) { $ruleLines -join "`n" } else { "$tab$tab// TODO: add rules" }

$tokens = @{
	'{{Feature}}'        = $Feature
	'{{Entity}}'         = $Entity
	'{{EntityLower}}'    = $Entity.ToLowerInvariant()
	'{{Op}}'             = $Operation
	'{{RouteSegment}}'   = $Feature.ToLowerInvariant()
	'{{DtoParams}}'      = $dtoParams
	'{{CreateArgs}}'     = $createArgs
	'{{ValidatorRules}}' = $validatorRules
}

$src = "backend\src"
$name = "$Operation$Entity"
$plan = @(
	@{ Template = 'Dto.cs.tmpl';         Target = "$src\InvoiceBuilder.Application\Features\$Feature\Models\$Operation\${name}Dto.cs" },
	@{ Template = 'ResponseDto.cs.tmpl'; Target = "$src\InvoiceBuilder.Application\Features\$Feature\Models\$Operation\${name}ResponseDto.cs" },
	@{ Template = 'Result.cs.tmpl';      Target = "$src\InvoiceBuilder.Application\Shared\Responses\$Feature\${name}Result.cs" },
	@{ Template = 'Validator.cs.tmpl';   Target = "$src\InvoiceBuilder.Application\Features\$Feature\Validation\${name}DtoValidator.cs" },
	@{ Template = 'Handler.cs.tmpl';     Target = "$src\InvoiceBuilder.Application\Features\$Feature\$name.Handler.cs" },
	@{ Template = 'Endpoint.cs.tmpl';    Target = "$src\InvoiceBuilder.Api\Features\$Feature\$Entity$Operation\$name.Endpoint.cs" }
)

# .editorconfig: LF, UTF-8, final newline.
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

foreach ($item in $plan) {
	$target = Join-Path $Root $item.Target
	if (Test-Path $target) {
		Write-Warning "Skipped (already exists): $($item.Target)"
		continue
	}

	$text = [System.IO.File]::ReadAllText((Join-Path $assets $item.Template))
	foreach ($key in $tokens.Keys) { $text = $text.Replace($key, $tokens[$key]) }
	$text = $text.Replace("`r`n", "`n")
	if (-not $text.EndsWith("`n")) { $text += "`n" }

	if ($PSCmdlet.ShouldProcess($item.Target, 'Create file')) {
		New-Item -ItemType Directory -Force -Path (Split-Path $target) | Out-Null
		[System.IO.File]::WriteAllText($target, $text, $utf8NoBom)
		Write-Host "Created: $($item.Target)"
	}
}

Write-Host ''
Write-Host 'Still to do by hand:'
Write-Host "  1. Register in $src\InvoiceBuilder.Api\Features\$Feature\${Entity}Endpoint.cs :"
Write-Host "       using InvoiceBuilder.Api.Features.$Feature.$Entity$Operation;"
Write-Host "       group.Map${name}Endpoint();"
Write-Host "  2. Confirm $Entity.Create(...) takes these fields in this order (or adapt the handler)."
Write-Host '  3. Fill in the endpoint WithSummary/WithDescription and any validator rules beyond NotEmpty().'
Write-Host '  4. Add integration tests (assets/EndpointTests.cs.tmpl) and build the solution.'
