global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Security.Claims;
global using System.Threading.Tasks;
global using Duende.IdentityServer.Models;
global using Duende.IdentityServer.Validation;
global using eShop.Identity.API.Models;
global using eShop.Identity.API.Services;
global using Microsoft.AspNetCore.Identity;
global using Microsoft.VisualStudio.TestTools.UnitTesting;
global using NSubstitute;

[assembly: Parallelize(Workers = 0, Scope = ExecutionScope.MethodLevel)]
