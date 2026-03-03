// Copyright (c) 2025 - EcoSys
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using DigitaleDelta.DdApiV3ToolKit.Configuration;

WebApplication
    .CreateBuilder(args)
    .ReadConfigurationFiles()
    .ConfigureLogging()
    .ValidateConfiguration()
    .CompileDefinitions()
    .ConfigureServices()
    .RegisterRequiredCustomServices()
    .Build()
    .ConfigureApplication()
    .Run();
