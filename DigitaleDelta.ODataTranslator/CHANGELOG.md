# Changelog

All notable changes to this project will be documented in this file.

## [2.0.0]
### Changed (breaking)
- ODataFilterValidator constructor signature changed: now requires propertyMaps and functionMaps instead of CSDL model
- PropertyMap and FunctionMap dictionaries now require StringComparer.OrdinalIgnoreCase for case-insensitive lookups

### Improved
- Removed CSDL dependencies (DigitaleDelta.CsdlParser) from runtime filter validation (significant performance improvement)
- Case-insensitive property and function name matching throughout
- ODataFilterValidator simplified from ~500 to ~350 lines of code

## [1.0.3]
### Added
- Added option to disallow a property in filter

### Changed
- Update to .NET 10/C# 14

## [1.0.2] - 2025-10-11
### Added
- ODataQueryServiceParameter class for passing additional parameters to the translator

## [1.0.1] - 2025-09-10
### Added
- Changelog file

### Changed
- Corrected Project site and git repository location

## [1.0.0] - 2025-09-09
### Added
- Initial release
